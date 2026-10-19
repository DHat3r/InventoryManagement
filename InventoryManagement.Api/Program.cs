using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply migrations automatically on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.Migrate();
}

// === CATEGORIES ENDPOINTS ===
var categoriesGroup = app.MapGroup("/api/categories").WithTags("Categories");

// GET all categories
categoriesGroup.MapGet("/", async (InventoryDbContext db) =>
{
    var categories = await db.Categories
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        .ToListAsync();
    return Results.Ok(categories);
})
.WithName("GetCategories")
.WithOpenApi();

// GET category by ID
categoriesGroup.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
})
.WithName("GetCategoryById")
.WithOpenApi();

// POST create category
categoriesGroup.MapPost("/", async (Category category, InventoryDbContext db) =>
{
    category.CreatedAt = DateTime.UtcNow;
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/api/categories/{category.Id}", category);
})
.WithName("CreateCategory")
.WithOpenApi();

// PUT update category
categoriesGroup.MapPut("/{id:int}", async (int id, Category updatedCategory, InventoryDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();

    category.Name = updatedCategory.Name;
    category.Description = updatedCategory.Description;
    category.IsActive = updatedCategory.IsActive;
    category.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(category);
})
.WithName("UpdateCategory")
.WithOpenApi();

// DELETE category (soft delete)
categoriesGroup.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();

    category.IsActive = false;
    category.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteCategory")
.WithOpenApi();


// === PRODUCTS ENDPOINTS ===
var productsGroup = app.MapGroup("/api/products").WithTags("Products");

// GET all products (with optional filtering)
productsGroup.MapGet("/", async (
    InventoryDbContext db,
    int? categoryId,
    bool? isActive,
    string? search,
    int page = 1,
    int pageSize = 20) =>
{
    var query = db.Products.Include(p => p.Category).AsQueryable();

    if (categoryId.HasValue)
        query = query.Where(p => p.CategoryId == categoryId.Value);

    if (isActive.HasValue)
        query = query.Where(p => p.IsActive == isActive.Value);

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)));

    var totalCount = await query.CountAsync();
    var products = await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new
    {
        Data = products,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    });
})
.WithName("GetProducts")
.WithOpenApi();

// GET product by ID
productsGroup.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithOpenApi();

// GET low stock products
productsGroup.MapGet("/low-stock", async (InventoryDbContext db) =>
{
    var products = await db.Products
        .Include(p => p.Category)
        .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
        .OrderBy(p => p.StockQuantity)
        .ToListAsync();
    return Results.Ok(products);
})
.WithName("GetLowStockProducts")
.WithOpenApi();

// POST create product
productsGroup.MapPost("/", async (Product product, InventoryDbContext db) =>
{
    // Validate category exists
    var category = await db.Categories.FindAsync(product.CategoryId);
    if (category is null) return Results.BadRequest("Category not found");

    product.CreatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync();

    // Reload with category
    await db.Entry(product).Reference(p => p.Category).LoadAsync();
    return Results.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

// PUT update product
productsGroup.MapPut("/{id:int}", async (int id, Product updatedProduct, InventoryDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    // Validate category exists
    var category = await db.Categories.FindAsync(updatedProduct.CategoryId);
    if (category is null) return Results.BadRequest("Category not found");

    product.Name = updatedProduct.Name;
    product.SKU = updatedProduct.SKU;
    product.Description = updatedProduct.Description;
    product.Price = updatedProduct.Price;
    product.Cost = updatedProduct.Cost;
    product.StockQuantity = updatedProduct.StockQuantity;
    product.MinStockLevel = updatedProduct.MinStockLevel;
    product.UnitOfMeasure = updatedProduct.UnitOfMeasure;
    product.IsActive = updatedProduct.IsActive;
    product.CategoryId = updatedProduct.CategoryId;
    product.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    await db.Entry(product).Reference(p => p.Category).LoadAsync();
    return Results.Ok(product);
})
.WithName("UpdateProduct")
.WithOpenApi();

// PATCH update stock
productsGroup.MapPatch("/{id:int}/stock", async (int id, int quantity, InventoryDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.StockQuantity = quantity;
    product.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(product);
})
.WithName("UpdateProductStock")
.WithOpenApi();

// DELETE product (soft delete)
productsGroup.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.IsActive = false;
    product.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithOpenApi();


app.Run();