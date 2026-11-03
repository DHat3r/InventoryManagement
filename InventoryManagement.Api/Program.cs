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
using InventoryManagement.Api.Endpoints;
using static InventoryManagement.Api.Endpoints.EndpointHelpers;

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
    .AddPolicy("RequireInventarioOrContadorOrAdmin", p => p.RequireRole("Admin", "Inventario", "Contador"))
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
else
{
    // HSTS: el navegador recordará usar solo HTTPS con este dominio
    app.UseHsts();
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

// Auto-migrate on startup: siempre en Development; fuera de él solo si
// Database:MigrateOnStartup=true (docker-compose lo activa para que arranque con un comando)
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
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

app.MapProductEndpoints();
app.MapKardexEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();

// Make Program class public for testing
public partial class Program { }