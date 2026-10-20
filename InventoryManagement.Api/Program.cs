using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Mapster;
using Serilog;
using Serilog.Enrichers.CorrelationId;
using InventoryManagement.Api.Models;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Validators;
using InventoryManagement.Api.Mapping;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.Common.Exceptions;
using InventoryManagement.Api.Middleware;
using InventoryManagement.Api.Services;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog Configuration ───
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName());

// ─── Services ───
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SupportNonNullableReferenceTypes();
});

// JSON: enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// DbContext
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Mapster
builder.Services.AddMapster();
MapsterConfig.RegisterMappings();

// ─── Authentication & Authorization (Fase 4) ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAdminSeedService, AdminSeedService>();
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));

// JWT Bearer
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret no configurado");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "InventoryManagement.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "InventoryManagement.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

// Authorization Policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireRole("Admin"))
    .AddPolicy("RequireInventarioOrAdmin", p => p.RequireRole("Admin", "Inventario"))
    .AddPolicy("RequireContadorOrAdmin", p => p.RequireRole("Admin", "Contador"))
    .AddPolicy("RequireCompradorOrInventarioOrAdmin", p => p.RequireRole("Admin", "Inventario", "Comprador"));

// ─── Health Checks ───
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHealthChecks()
    .AddNpgSql(cs!, name: "postgresql", tags: new[] { "db", "ready" })
    .AddDbContextCheck<InventoryDbContext>(name: "efcore", tags: new[] { "db", "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

// ─── Rate Limiting ───
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        }));

    options.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter("auth", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 5
        }));

    options.AddPolicy("api", ctx =>
        RateLimitPartition.GetFixedWindowLimiter("api", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 20
        }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ─── CORS ───
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                ?? new[] { "http://localhost:3000", "http://localhost:5173", "https://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ─── Swagger/OpenAPI mejorado ───
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SupportNonNullableReferenceTypes();
    c.EnableAnnotations();

    // Documentación XML
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    // JWT Bearer auth en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese el token JWT: Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JSON: enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

// ─── Pipeline ───
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

// Exception Handling Middleware (must be early in pipeline)
app.UseExceptionHandling();

app.UseHttpsRedirection();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
    };
});

// CORS (antes de Auth para preflight)
app.UseCors("frontend");

// Rate Limiting
app.UseRateLimiter();

// Auth middleware
app.UseAuthentication();
app.UseAuthorization();

// Health Checks endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Auto-migrate on startup (dev)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}

// ─── Admin Seed on Startup ───
if (app.Configuration.GetValue<bool>("AdminSeed:RunOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var seedService = scope.ServiceProvider.GetRequiredService<IAdminSeedService>();
    await seedService.EnsureAdminExistsAsync();
}

// ─── Helpers ───
static IResult ValidationProblem(FluentValidation.Results.ValidationResult result)
    => Results.ValidationProblem(result.ToDictionary());

static ApiResponse<T> OkResponse<T>(T data, string? message = null, object? meta = null)
    => ApiResponse<T>.Ok(data, message, meta);

/// <summary>
/// Aplica ordenamiento multi-campo: "price:desc,name:asc,stock"
/// </summary>
static IQueryable<Product> ApplyMultiSort(IQueryable<Product> query, string? sortBy, bool sortDesc)
{
    if (string.IsNullOrWhiteSpace(sortBy))
        return sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);

    var sorts = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
    IOrderedQueryable<Product>? ordered = null;

    foreach (var sort in sorts)
    {
        var parts = sort.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var field = parts[0].Trim().ToLower();
        var direction = parts.Length > 1 ? parts[1].Trim().ToLower() : (sortDesc ? "desc" : "asc");
        var descending = direction == "desc";

        var isFirst = ordered == null;
        ordered = (field, descending, isFirst) switch
        {
            ("price", true, true) => query.OrderByDescending(p => p.Price),
            ("price", false, true) => query.OrderBy(p => p.Price),
            ("stock", true, true) => query.OrderByDescending(p => p.StockQuantity),
            ("stock", false, true) => query.OrderBy(p => p.StockQuantity),
            ("createdat", true, true) => query.OrderByDescending(p => p.CreatedAt),
            ("createdat", false, true) => query.OrderBy(p => p.CreatedAt),
            ("updatedat", true, true) => query.OrderByDescending(p => p.UpdatedAt),
            ("updatedat", false, true) => query.OrderBy(p => p.UpdatedAt),
            ("name", true, true) => query.OrderByDescending(p => p.Name),
            ("name", false, true) => query.OrderBy(p => p.Name),
            ("price", true, false) => ordered!.ThenByDescending(p => p.Price),
            ("price", false, false) => ordered!.ThenBy(p => p.Price),
            ("stock", true, false) => ordered!.ThenByDescending(p => p.StockQuantity),
            ("stock", false, false) => ordered!.ThenBy(p => p.StockQuantity),
            ("createdat", true, false) => ordered!.ThenByDescending(p => p.CreatedAt),
            ("createdat", false, false) => ordered!.ThenBy(p => p.CreatedAt),
            ("updatedat", true, false) => ordered!.ThenByDescending(p => p.UpdatedAt),
            ("updatedat", false, false) => ordered!.ThenBy(p => p.UpdatedAt),
            ("name", true, false) => ordered!.ThenByDescending(p => p.Name),
            ("name", false, false) => ordered!.ThenBy(p => p.Name),
            _ => isFirst ? (descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)) : ordered!
        };
    }

    return ordered ?? (sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name));
}

// ============================================================
// CATEGORIES
// ============================================================
var categories = app.MapGroup("/api/categories").WithTags("Categories").RequireRateLimiting("api");

// GET all
categories.MapGet("/", async (InventoryDbContext db) =>
{
    var list = await db.Categories
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        .ProjectToType<CategoryDto>()
        .ToListAsync();
    return Results.Ok(OkResponse(list));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Ver categorías: Inventario, Admin
.WithName("GetCategories")
.WithOpenApi();

// GET by ID
categories.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var entity = await db.Categories
        .Where(c => c.Id == id)
        .ProjectToType<CategoryDto>()
        .FirstOrDefaultAsync();
    return entity is not null 
        ? Results.Ok(OkResponse(entity)) 
        : throw new NotFoundException("Category", id);
})
.RequireAuthorization("RequireInventarioOrAdmin")
.WithName("GetCategoryById")
.WithOpenApi();

// POST create
categories.MapPost("/", async (CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = dto.Adapt<Category>();
    entity.CreatedBy = currentUser.UserId;
    db.Categories.Add(entity);
    await db.SaveChangesAsync();

    var result = entity.Adapt<CategoryDto>();
    return Results.Created($"/api/categories/{entity.Id}", OkResponse(result, "Categoría creada"));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Crear categorías: Inventario, Admin
.WithName("CreateCategory")
.WithOpenApi();

// PUT update
categories.MapPut("/{id:int}", async (int id, UpdateCategoryDto dto, IValidator<UpdateCategoryDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
{
    // Pasar el ID al validador ANTES de validar (unicidad y referencias circulares)
    if (validator is UpdateCategoryValidator ucv) ucv.SetCategoryId(id);

    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = await db.Categories.FindAsync(id);
    if (entity is null) throw new NotFoundException("Category", id);

    dto.Adapt(entity);
    entity.ModifiedBy = currentUser.UserId;
    await db.SaveChangesAsync();

    var result = entity.Adapt<CategoryDto>();
    return Results.Ok(OkResponse(result, "Categoría actualizada"));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Actualizar categorías: Inventario, Admin
.WithName("UpdateCategory")
.WithOpenApi();

// DELETE (soft)
categories.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var entity = await db.Categories.FindAsync(id);
    if (entity is null) throw new NotFoundException("Category", id);

    entity.IsActive = false;
    entity.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization("RequireAdmin") // Eliminar categorías: solo Admin
.WithName("DeleteCategory")
.WithOpenApi();


// GET tree (jerarquía de categorías)
categories.MapGet("/tree", async (InventoryDbContext db) =>
{
    // Cargar todas las categorías activas
    var allCategories = await db.Categories
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        .ProjectToType<CategoryTreeDto>()
        .ToListAsync();

    // Construir árbol manualmente (evita recursión de Mapster)
    var lookup = allCategories.ToDictionary(c => c.Id);
    var roots = new List<CategoryTreeDto>();

    foreach (var cat in allCategories)
    {
        if (cat.ParentCategoryId.HasValue && lookup.TryGetValue(cat.ParentCategoryId.Value, out var parent))
        {
            parent.Children.Add(cat);
        }
        else
        {
            roots.Add(cat);
        }
    }

    return Results.Ok(OkResponse(roots));
})
.RequireAuthorization("RequireInventarioOrAdmin")
.WithName("GetCategoriesTree")
.WithOpenApi();


// ============================================================
// PRODUCTS
// ============================================================
var products = app.MapGroup("/api/products").WithTags("Products").RequireRateLimiting("api");

// GET all (paginado, filtros, orden)
products.MapGet("/", async (
    InventoryDbContext db,
    IValidator<ProductFilterDto> filterValidator,
    [AsParameters] ProductFilterDto filter) =>
{
    var validation = await filterValidator.ValidateAsync(filter);
    if (!validation.IsValid) return ValidationProblem(validation);

    var query = db.Products.Include(p => p.Category).AsQueryable();

    if (filter.CategoryId.HasValue)
        query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

    if (filter.IsActive.HasValue)
        query = query.Where(p => p.IsActive == filter.IsActive.Value);

    if (!string.IsNullOrWhiteSpace(filter.Search))
        query = query.Where(p => p.Name.Contains(filter.Search!) || (p.SKU != null && p.SKU.Contains(filter.Search!)));

    if (filter.MinPrice.HasValue)
        query = query.Where(p => p.Price >= filter.MinPrice.Value);

    if (filter.MaxPrice.HasValue)
        query = query.Where(p => p.Price <= filter.MaxPrice.Value);

    if (filter.LowStockOnly == true)
        query = query.Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel);

    // Filtros por rango de fechas (convertir a UTC para PostgreSQL)
    if (filter.CreatedAtFrom.HasValue)
    {
        var utc = filter.CreatedAtFrom.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(filter.CreatedAtFrom.Value, DateTimeKind.Utc)
            : filter.CreatedAtFrom.Value.ToUniversalTime();
        query = query.Where(p => p.CreatedAt >= utc);
    }

    if (filter.CreatedAtTo.HasValue)
    {
        var utc = filter.CreatedAtTo.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(filter.CreatedAtTo.Value, DateTimeKind.Utc)
            : filter.CreatedAtTo.Value.ToUniversalTime();
        query = query.Where(p => p.CreatedAt <= utc);
    }

    if (filter.UpdatedAtFrom.HasValue)
    {
        var utc = filter.UpdatedAtFrom.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(filter.UpdatedAtFrom.Value, DateTimeKind.Utc)
            : filter.UpdatedAtFrom.Value.ToUniversalTime();
        query = query.Where(p => p.UpdatedAt >= utc);
    }

    if (filter.UpdatedAtTo.HasValue)
    {
        var utc = filter.UpdatedAtTo.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(filter.UpdatedAtTo.Value, DateTimeKind.Utc)
            : filter.UpdatedAtTo.Value.ToUniversalTime();
        query = query.Where(p => p.UpdatedAt <= utc);
    }

    // Ordenamiento dinámico multi-campo
    query = ApplyMultiSort(query, filter.SortBy, filter.SortDesc);

    var total = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ProjectToType<ProductListDto>()
        .ToListAsync();

    var paged = PagedResult<ProductListDto>.Create(items, total, filter.Page, filter.PageSize);
    return Results.Ok(OkResponse(paged, meta: new
    {
        pagination = new
        {
            paged.TotalCount,
            paged.Page,
            paged.PageSize,
            paged.TotalPages,
            paged.HasPreviousPage,
            paged.HasNextPage
        }
    }));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Ver productos: Inventario, Admin
.WithName("GetProducts")
.WithOpenApi();

// GET by ID (completo)
products.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var entity = await db.Products
        .Include(p => p.Category)
        .Where(p => p.Id == id)
        .ProjectToType<ProductDto>()
        .FirstOrDefaultAsync();
    return entity is not null 
        ? Results.Ok(OkResponse(entity)) 
        : throw new NotFoundException("Product", id);
})
.RequireAuthorization("RequireInventarioOrAdmin")
.WithName("GetProductById")
.WithOpenApi();

// GET low stock
products.MapGet("/low-stock", async (InventoryDbContext db) =>
{
    var list = await db.Products
        .Include(p => p.Category)
        .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
        .OrderBy(p => p.StockQuantity)
        .ProjectToType<ProductListDto>()
        .ToListAsync();
    return Results.Ok(OkResponse(list));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Stock bajo: Inventario, Admin
.WithName("GetLowStockProducts")
.WithOpenApi();

// POST create
products.MapPost("/", async (CreateProductDto dto, IValidator<CreateProductDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = dto.Adapt<Product>();
    entity.CreatedBy = currentUser.UserId;
    db.Products.Add(entity);
    await db.SaveChangesAsync();

    // Recargar con Category para el DTO de salida
    await db.Entry(entity).Reference(p => p.Category).LoadAsync();
    var result = entity.Adapt<ProductDto>();

    return Results.Created($"/api/products/{entity.Id}", OkResponse(result, "Producto creado"));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Crear productos: Inventario, Admin
.WithName("CreateProduct")
.WithOpenApi();

// PUT update
products.MapPut("/{id:int}", async (int id, UpdateProductDto dto, IValidator<UpdateProductDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
{
    // Pasar ID al validador ANTES de validar para unicidad de SKU
    if (validator is UpdateProductValidator upv) upv.SetProductId(id);

    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = await db.Products.FindAsync(id);
    if (entity is null) throw new NotFoundException("Product", id);

    dto.Adapt(entity);
    entity.ModifiedBy = currentUser.UserId;
    await db.SaveChangesAsync();

    await db.Entry(entity).Reference(p => p.Category).LoadAsync();
    var result = entity.Adapt<ProductDto>();

    return Results.Ok(OkResponse(result, "Producto actualizado"));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Actualizar productos: Inventario, Admin
.WithName("UpdateProduct")
.WithOpenApi();

// PATCH stock
products.MapPatch("/{id:int}/stock", async (int id, UpdateStockDto dto, IValidator<UpdateStockDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = await db.Products.FindAsync(id);
    if (entity is null) throw new NotFoundException("Product", id);

    dto.Adapt(entity);
    entity.ModifiedBy = currentUser.UserId;
    await db.SaveChangesAsync();

    var result = entity.Adapt<ProductDto>();
    return Results.Ok(OkResponse(result, "Stock actualizado"));
})
.RequireAuthorization("RequireInventarioOrAdmin") // Actualizar stock: Inventario, Admin
.WithName("UpdateProductStock")
.WithOpenApi();

// DELETE (soft)
products.MapDelete("/{id:int}", async (int id, InventoryDbContext db, ICurrentUserService currentUser) =>
{
    var entity = await db.Products.FindAsync(id);
    if (entity is null) throw new NotFoundException("Product", id);

    entity.IsActive = false;
    entity.UpdatedAt = DateTime.UtcNow;
    entity.ModifiedBy = currentUser.UserId;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization("RequireAdmin") // Eliminar productos: solo Admin
.WithName("DeleteProduct")
.WithOpenApi();


// ============================================================
// STOCK MOVEMENTS (Kardex) - Fase 3
// ============================================================

// POST /api/products/{id}/movements - Registrar movimiento de stock
products.MapPost("/{id:int}/movements", async (
    int id,
    CreateStockMovementDto dto,
    IValidator<CreateStockMovementDto> validator,
    InventoryDbContext db,
    ICurrentUserService currentUser) =>
{
    // Autorización según tipo de movimiento
    if (dto.Type == MovementType.Entrada)
    {
        if (!currentUser.IsInRole(UserRole.Comprador, UserRole.Inventario, UserRole.Admin))
            return Results.Forbid();
    }
    else if (dto.Type == MovementType.Salida)
    {
        if (!currentUser.IsInRole(UserRole.Inventario, UserRole.Admin))
            return Results.Forbid();
    }
    else if (dto.Type == MovementType.Ajuste)
    {
        if (!currentUser.IsInRole(UserRole.Inventario, UserRole.Admin))
            return Results.Forbid();
    }

    // Pasar productId al validador ANTES de validar (para validaciones de negocio con BD)
    if (validator is CreateStockMovementValidator csv) csv.SetProductId(id);

    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var product = await db.Products.FindAsync(id);
    if (product is null) throw new NotFoundException("Product", id);

    // Calcular stock anterior y nuevo según tipo de movimiento
    int previousStock = product.StockQuantity;
    int newStock;
    decimal? unitCost = dto.UnitCost;

    switch (dto.Type)
    {
        case MovementType.Entrada:
            newStock = previousStock + dto.Quantity;
            // UnitCost requerido para valoración de entrada
            if (!unitCost.HasValue) unitCost = product.Cost;
            break;

        case MovementType.Salida:
            if (previousStock < dto.Quantity)
                throw new BusinessRuleException("STOCK_INSUFICIENTE",
                    $"Stock insuficiente. Disponible: {previousStock}, Solicitado: {dto.Quantity}");
            newStock = previousStock - dto.Quantity;
            // Para salidas usamos costo promedio actual si no se provee
            unitCost ??= product.Cost;
            break;

        case MovementType.Ajuste:
            // Ajuste: Quantity positivo = entrada, negativo = salida
            // Usamos UnitCost como indicador: si tiene valor = entrada, si no = salida
            if (unitCost.HasValue)
            {
                newStock = previousStock + dto.Quantity;
            }
            else
            {
                if (previousStock < dto.Quantity)
                    throw new BusinessRuleException("STOCK_INSUFICIENTE",
                        $"Stock insuficiente para ajuste negativo. Disponible: {previousStock}");
                newStock = previousStock - dto.Quantity;
            }
            unitCost ??= product.Cost;
            break;

        default:
            throw new BusinessRuleException("TIPO_MOVIMIENTO_INVALIDO", "Tipo de movimiento no reconocido");
    }

    // Crear movimiento en transacción
    using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        var movement = dto.Adapt<StockMovement>();
        movement.ProductId = id;
        movement.PreviousStock = previousStock;
        movement.NewStock = newStock;
        movement.UnitCost = unitCost;
        movement.UserId = currentUser.UserId; // Auditoría: quién registró el movimiento

        db.StockMovements.Add(movement);

        // Actualizar stock del producto
        product.StockQuantity = newStock;
        product.UpdatedAt = DateTime.UtcNow;
        product.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var result = movement.Adapt<StockMovementDto>();
        return Results.Created($"/api/products/{id}/movements/{movement.Id}", OkResponse(result, "Movimiento registrado"));
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
})
// .RequireAuthorization("RequireCompradorOrInventarioOrAdmin") // Manejado dentro del endpoint según tipo
.WithName("CreateStockMovement")
.WithOpenApi();

// GET /api/products/{id}/kardex - Historial paginado de movimientos
products.MapGet("/{id:int}/kardex", async (
    int id,
    InventoryDbContext db,
    IValidator<StockMovementFilterDto> filterValidator,
    [AsParameters] StockMovementFilterDto filter) =>
{
    var validation = await filterValidator.ValidateAsync(filter);
    if (!validation.IsValid) return ValidationProblem(validation);

    var product = await db.Products.FindAsync(id);
    if (product is null) throw new NotFoundException("Product", id);

    var query = db.StockMovements
        .Where(m => m.ProductId == id)
        .OrderByDescending(m => m.CreatedAt)
        .AsQueryable();

    if (filter.Type.HasValue)
        query = query.Where(m => m.Type == filter.Type.Value);

    if (filter.DateFrom.HasValue)
        query = query.Where(m => m.CreatedAt >= filter.DateFrom.Value);

    if (filter.DateTo.HasValue)
        query = query.Where(m => m.CreatedAt <= filter.DateTo.Value);

    if (!string.IsNullOrWhiteSpace(filter.Reference))
        query = query.Where(m => m.Reference != null && m.Reference.Contains(filter.Reference!));

    var total = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ProjectToType<StockMovementListDto>()
        .ToListAsync();

    var paged = PagedResult<StockMovementListDto>.Create(items, total, filter.Page, filter.PageSize);
    return Results.Ok(OkResponse(paged, meta: new
    {
        pagination = new
        {
            paged.TotalCount,
            paged.Page,
            paged.PageSize,
            paged.TotalPages,
            paged.HasPreviousPage,
            paged.HasNextPage
        }
    }));
})
.RequireAuthorization("RequireContadorOrAdmin") // Ver kardex: Contador, Inventario, Admin
.WithName("GetProductKardex")
.WithOpenApi();

// GET /api/products/{id}/kardex/summary - Resumen valorado del kardex
products.MapGet("/{id:int}/kardex/summary", async (int id, InventoryDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) throw new NotFoundException("Product", id);

    var movements = await db.StockMovements
        .Where(m => m.ProductId == id)
        .OrderBy(m => m.CreatedAt)
        .ToListAsync();

    int totalEntradas = movements.Where(m => m.Type == MovementType.Entrada).Sum(m => m.Quantity);
    int totalSalidas = movements.Where(m => m.Type == MovementType.Salida).Sum(m => m.Quantity);
    int totalAjustes = movements.Where(m => m.Type == MovementType.Ajuste).Sum(m => m.Quantity);

    decimal totalEntradasValue = movements
        .Where(m => m.Type == MovementType.Entrada)
        .Sum(m => m.Quantity * (m.UnitCost ?? 0));

    decimal totalSalidasValue = movements
        .Where(m => m.Type == MovementType.Salida)
        .Sum(m => m.Quantity * (m.UnitCost ?? 0));

    // Costo promedio ponderado de las entradas
    decimal averageCost = totalEntradas > 0
        ? Math.Round(totalEntradasValue / totalEntradas, 2)
        : product.Cost;

    var summary = new KardexSummaryDto(
        ProductId: product.Id,
        ProductName: product.Name,
        ProductSKU: product.SKU ?? string.Empty,
        CurrentStock: product.StockQuantity,
        AverageCost: averageCost,
        StockValue: Math.Round(product.StockQuantity * averageCost, 2),
        TotalEntradas: totalEntradas,
        TotalSalidas: totalSalidas,
        TotalAjustes: totalAjustes,
        TotalEntradasValue: Math.Round(totalEntradasValue, 2),
        TotalSalidasValue: Math.Round(totalSalidasValue, 2)
    );

    return Results.Ok(OkResponse(summary));
})
.RequireAuthorization("RequireContadorOrAdmin") // Resumen valorado: Contador, Admin
.WithName("GetProductKardexFiltered")
.WithOpenApi();

// GET /api/products/{id}/kardex/summary - Resumen valorado del kardex
// ============================================================

var auth = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous().RequireRateLimiting("auth");

// POST /api/auth/login
auth.MapPost("/login", async (LoginDto dto, IValidator<LoginDto> validator, IAuthService authService) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var result = await authService.LoginAsync(dto);
    return result is not null
        ? Results.Ok(OkResponse(result, "Login exitoso"))
        : Results.Unauthorized();
})
.WithName("Login")
.WithOpenApi();

// POST /api/auth/refresh
auth.MapPost("/refresh", async (RefreshTokenDto dto, IValidator<RefreshTokenDto> validator, IAuthService authService) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var result = await authService.RefreshAsync(dto.RefreshToken);
    return result is not null
        ? Results.Ok(OkResponse(result, "Token renovado"))
        : Results.Unauthorized();
})
.WithName("RefreshToken")
.WithOpenApi();

// POST /api/auth/logout
auth.MapPost("/logout", async (RefreshTokenDto dto, IValidator<RefreshTokenDto> validator, IAuthService authService) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    await authService.RevokeRefreshTokenAsync(dto.RefreshToken);
    return Results.Ok(OkResponse(true, "Logout exitoso"));
})
.RequireAuthorization() // Requiere estar autenticado
.WithName("Logout")
.WithOpenApi();

// GET /api/auth/me
auth.MapGet("/me", async (ICurrentUserService currentUser, IAuthService authService) =>
{
    if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
        return Results.Unauthorized();

    var userInfo = await authService.GetUserInfoAsync(currentUser.UserId.Value);
    return userInfo is not null
        ? Results.Ok(OkResponse(userInfo))
        : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithOpenApi();

// POST /api/auth/change-password
auth.MapPost("/change-password", async (ChangePasswordDto dto, IValidator<ChangePasswordDto> validator, IAuthService authService, ICurrentUserService currentUser) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    if (!currentUser.UserId.HasValue) return Results.Unauthorized();

    var success = await authService.ChangePasswordAsync(currentUser.UserId.Value, dto);
    return success
        ? Results.Ok(OkResponse(true, "Contraseña cambiada exitosamente"))
        : Results.BadRequest(OkResponse<bool>(false, "Contraseña actual incorrecta"));
})
.RequireAuthorization()
.WithName("ChangePassword")
.WithOpenApi();


// ============================================================
// USERS ENDPOINTS (Admin only) - Fase 4
// ============================================================

var users = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization("RequireAdmin").RequireRateLimiting("api");

// GET all users (paginado)
users.MapGet("/", async (
    InventoryDbContext db,
    IValidator<UserFilterDto> filterValidator,
    [AsParameters] UserFilterDto filter) =>
{
    var validation = await filterValidator.ValidateAsync(filter);
    if (!validation.IsValid) return ValidationProblem(validation);

    var query = db.Users.AsQueryable();

    if (filter.Role.HasValue)
        query = query.Where(u => u.Role == filter.Role.Value);

    if (filter.IsActive.HasValue)
        query = query.Where(u => u.IsActive == filter.IsActive.Value);

    if (!string.IsNullOrWhiteSpace(filter.Search))
        query = query.Where(u => u.UserName.Contains(filter.Search!) || u.FullName.Contains(filter.Search!) || (u.Rut != null && u.Rut.Contains(filter.Search!)));

    query = query.OrderBy(u => u.UserName);

    var total = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ProjectToType<UserListDto>()
        .ToListAsync();

    var paged = PagedResult<UserListDto>.Create(items, total, filter.Page, filter.PageSize);
    return Results.Ok(OkResponse(paged, meta: new
    {
        pagination = new
        {
            paged.TotalCount,
            paged.Page,
            paged.PageSize,
            paged.TotalPages,
            paged.HasPreviousPage,
            paged.HasNextPage
        }
    }));
})
.WithName("GetUsers")
.WithOpenApi();

// GET user by ID
users.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var user = await db.Users
        .Where(u => u.Id == id)
        .ProjectToType<UserInfoDto>()
        .FirstOrDefaultAsync();
    return user is not null
        ? Results.Ok(OkResponse(user))
        : throw new NotFoundException("User", id);
})
.WithName("GetUserById")
.WithOpenApi();

// POST create user
users.MapPost("/", async (CreateUserDto dto, IValidator<CreateUserDto> validator, InventoryDbContext db, IAuthService authService) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var user = dto.Adapt<User>();
    user.PasswordHash = authService.HashPassword(dto.Password);
    user.CreatedAt = DateTime.UtcNow;

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var result = user.Adapt<UserInfoDto>();
    return Results.Created($"/api/users/{user.Id}", OkResponse(result, "Usuario creado"));
})
.WithName("CreateUser")
.WithOpenApi();

// PUT update user
users.MapPut("/{id:int}", async (int id, UpdateUserDto dto, IValidator<UpdateUserDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var user = await db.Users.FindAsync(id);
    if (user is null) throw new NotFoundException("User", id);

    dto.Adapt(user);
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var result = user.Adapt<UserInfoDto>();
    return Results.Ok(OkResponse(result, "Usuario actualizado"));
})
.WithName("UpdateUser")
.WithOpenApi();

// DELETE (soft delete) user
users.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) throw new NotFoundException("User", id);

    user.IsActive = false;
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteUser")
.WithOpenApi();


app.Run();

// Make Program class public for testing
public partial class Program { }