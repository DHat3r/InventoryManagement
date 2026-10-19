using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

// DbContext
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Mapster
builder.Services.AddMapster();
MapsterConfig.RegisterMappings();

var app = builder.Build();

// ─── Pipeline ───
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

// Auto-migrate on startup (dev)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}

// ─── Helpers ───
static IResult ValidationProblem(FluentValidation.Results.ValidationResult result)
    => Results.ValidationProblem(result.ToDictionary());

static ApiResponse<T> OkResponse<T>(T data, string? message = null, object? meta = null)
    => ApiResponse<T>.Ok(data, message, meta);

// ============================================================
// CATEGORIES
// ============================================================
var categories = app.MapGroup("/api/categories").WithTags("Categories");

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
.WithName("GetCategoryById")
.WithOpenApi();

// POST create
categories.MapPost("/", async (CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = dto.Adapt<Category>();
    db.Categories.Add(entity);
    await db.SaveChangesAsync();

    var result = entity.Adapt<CategoryDto>();
    return Results.Created($"/api/categories/{entity.Id}", OkResponse(result, "Categoría creada"));
})
.WithName("CreateCategory")
.WithOpenApi();

// PUT update
categories.MapPut("/{id:int}", async (int id, UpdateCategoryDto dto, IValidator<UpdateCategoryDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = await db.Categories.FindAsync(id);
    if (entity is null) throw new NotFoundException("Category", id);

    dto.Adapt(entity);
    await db.SaveChangesAsync();

    var result = entity.Adapt<CategoryDto>();
    return Results.Ok(OkResponse(result, "Categoría actualizada"));
})
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
.WithName("DeleteCategory")
.WithOpenApi();


// ============================================================
// PRODUCTS
// ============================================================
var products = app.MapGroup("/api/products").WithTags("Products");

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

    // Ordenamiento dinámico
    query = (filter.SortBy?.ToLower(), filter.SortDesc) switch
    {
        ("price", false) => query.OrderBy(p => p.Price),
        ("price", true) => query.OrderByDescending(p => p.Price),
        ("stock", false) => query.OrderBy(p => p.StockQuantity),
        ("stock", true) => query.OrderByDescending(p => p.StockQuantity),
        ("createdat", false) => query.OrderBy(p => p.CreatedAt),
        ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
        _ => query.OrderBy(p => p.Name)
    };

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
.WithName("GetLowStockProducts")
.WithOpenApi();

// POST create
products.MapPost("/", async (CreateProductDto dto, IValidator<CreateProductDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = dto.Adapt<Product>();
    db.Products.Add(entity);
    await db.SaveChangesAsync();

    // Recargar con Category para el DTO de salida
    await db.Entry(entity).Reference(p => p.Category).LoadAsync();
    var result = entity.Adapt<ProductDto>();

    return Results.Created($"/api/products/{entity.Id}", OkResponse(result, "Producto creado"));
})
.WithName("CreateProduct")
.WithOpenApi();

// PUT update
products.MapPut("/{id:int}", async (int id, UpdateProductDto dto, IValidator<UpdateProductDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = await db.Products.FindAsync(id);
    if (entity is null) throw new NotFoundException("Product", id);

    // Pasar ID al validador para unicidad de SKU
    if (validator is UpdateProductValidator upv) upv.SetProductId(id);

    dto.Adapt(entity);
    await db.SaveChangesAsync();

    await db.Entry(entity).Reference(p => p.Category).LoadAsync();
    var result = entity.Adapt<ProductDto>();

    return Results.Ok(OkResponse(result, "Producto actualizado"));
})
.WithName("UpdateProduct")
.WithOpenApi();

// PATCH stock
products.MapPatch("/{id:int}/stock", async (int id, UpdateStockDto dto, IValidator<UpdateStockDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = await db.Products.FindAsync(id);
    if (entity is null) throw new NotFoundException("Product", id);

    dto.Adapt(entity);
    await db.SaveChangesAsync();

    var result = entity.Adapt<ProductDto>();
    return Results.Ok(OkResponse(result, "Stock actualizado"));
})
.WithName("UpdateProductStock")
.WithOpenApi();

// DELETE (soft)
products.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var entity = await db.Products.FindAsync(id);
    if (entity is null) throw new NotFoundException("Product", id);

    entity.IsActive = false;
    entity.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithOpenApi();


app.Run();