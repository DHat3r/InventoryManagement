# Documentación: Implementación de Entity Framework Core

> **Proyecto:** InventoryManagement.Api\
> **Fecha:** 19/10/2026\
> **Versión:** NET 8.0\
> **Base de datos:** PostgreSQL 16 (Docker)

***

## 📋 Resumen de cambios

Se implementó **Entity Framework Core 8.0** con proveedor **Npgsql** para PostgreSQL, incluyendo:

* Entidades de dominio (`Category`, `Product`)

* `DbContext` con configuración Fluent API

* Migración inicial con datos semilla (seed data)

* Endpoints CRUD completos usando Minimal APIs

* Configuración de conexión y migración automática en desarrollo

***

## 📦 Paquetes NuGet instalados

```xml
<!-- InventoryManagement.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.21" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
</ItemGroup>
```

| Paquete                                 | Propósito                      |
| --------------------------------------- | ------------------------------ |
| `Microsoft.EntityFrameworkCore`         | Core de EF Core                |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Proveedor PostgreSQL           |
| `Microsoft.EntityFrameworkCore.Design`  | Herramientas CLI (`dotnet ef`) |

***

## 🗂️ Estructura de archivos creada

```
InventoryManagement.Api/
├── Models/
│   ├── Category.cs              # Entidad Categoría
│   ├── Product.cs               # Entidad Producto
│   └── InventoryDbContext.cs    # DbContext + configuración + seed
├── Migrations/
│   ├── 20261019145915_InitialCreate.cs
│   ├── 20261019145915_InitialCreate.Designer.cs
│   └── InventoryDbContextModelSnapshot.cs
├── appsettings.json             # Connection string producción
├── appsettings.Development.json # Connection string desarrollo
└── Program.cs                   # Registro DI + endpoints + migración auto
```

***

## 🏗️ Entidades de dominio

### Category.cs

```csharp
namespace InventoryManagement.Api.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
```

### Product.cs

```csharp
namespace InventoryManagement.Api.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SKU { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; }

    [Required]
    public int StockQuantity { get; set; } = 0;

    public int MinStockLevel { get; set; } = 10;

    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; } = "UN";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Foreign Key
    public int CategoryId { get; set; }

    // Navigation property
    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;
}
```

***

## 🔧 DbContext y configuración Fluent API

### InventoryDbContext.cs

```csharp
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── Category ───
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ─── Product ───
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SKU).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.Property(e => e.MinStockLevel).HasDefaultValue(10);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(50).HasDefaultValue("UN");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Name);
            
            // Relación: Product -> Category (muchos a uno)
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict); // No borrar categoría si tiene productos
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electrónica", Description = "...", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Ropa", Description = "...", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Hogar", Description = "...", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Deportes", Description = "...", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Gaming", SKU = "LAP-GAM-001", Price = 1299.99m, Cost = 950.00m, StockQuantity = 15, MinStockLevel = 5, CategoryId = 1, ... },
            new Product { Id = 2, Name = "Mouse Inalámbrico", SKU = "MOU-WLS-001", Price = 49.99m, Cost = 25.00m, StockQuantity = 50, CategoryId = 1, ... },
            // ... más productos
        );
    }
}
```

**Puntos clave:**

* `ToTable()`: nombres de tabla en minúscula (convención PostgreSQL)

* `HasDefaultValueSql("CURRENT_TIMESTAMP")`: timestamps manejados por BD

* `DeleteBehavior.Restrict`: evita borrado en cascada accidental

* Índices únicos en `Category.Name` y `Product.SKU`

* Seed data con IDs fijos para consistencia

***

## ⚙️ Configuración de conexión

### appsettings.json / appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_db;Username=postgres;Password=postgres"
  }
}
```

> **Nota:** En producción usar **User Secrets** o variables de entorno para la contraseña.

***

## 🚀 Program.cs - Registro y Endpoints

### Registro de servicios

```csharp
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Migración automática en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.Migrate();
}
```

### Endpoints - Categorías (`/api/categories`)

```csharp
var categoriesGroup = app.MapGroup("/api/categories").WithTags("Categories");

// GET all
categoriesGroup.MapGet("/", async (InventoryDbContext db) =>
    await db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync());

// GET by ID
categoriesGroup.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
    await db.Categories.FindAsync(id) is Category c ? Results.Ok(c) : Results.NotFound());

// POST create
categoriesGroup.MapPost("/", async (Category category, InventoryDbContext db) =>
{
    category.CreatedAt = DateTime.UtcNow;
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/api/categories/{category.Id}", category);
});

// PUT update
categoriesGroup.MapPut("/{id:int}", async (int id, Category updated, InventoryDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();
    
    category.Name = updated.Name;
    category.Description = updated.Description;
    category.IsActive = updated.IsActive;
    category.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(category);
});

// DELETE (soft delete)
categoriesGroup.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();
    category.IsActive = false;
    category.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
});
```

### Endpoints - Productos (`/api/products`)

```csharp
var productsGroup = app.MapGroup("/api/products").WithTags("Products");

// GET con filtros y paginación
productsGroup.MapGet("/", async (
    InventoryDbContext db,
    int? categoryId,
    bool? isActive,
    string? search,
    int page = 1,
    int pageSize = 20) =>
{
    var query = db.Products.Include(p => p.Category).AsQueryable();

    if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
    if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
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
});

// GET low stock
productsGroup.MapGet("/low-stock", async (InventoryDbContext db) =>
    await db.Products
        .Include(p => p.Category)
        .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
        .OrderBy(p => p.StockQuantity)
        .ToListAsync());

// POST create (con validación de categoría)
productsGroup.MapPost("/", async (Product product, InventoryDbContext db) =>
{
    var category = await db.Categories.FindAsync(product.CategoryId);
    if (category is null) return Results.BadRequest("Category not found");

    product.CreatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync();
    await db.Entry(product).Reference(p => p.Category).LoadAsync();a
    return Results.Created($"/api/products/{product.Id}", product);
});

// PATCH stock (actualización parcial)
productsGroup.MapPatch("/{id:int}/stock", async (int id, int quantity, InventoryDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();
    product.StockQuantity = quantity;
    product.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(product);
});
```

***

## 🗃️ Migración generada

### 20261019145915\_InitialCreate.cs (fragmento)

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "categories",
        columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        },
        constraints: table => { table.PrimaryKey("PK_categories", x => x.Id); });

    migrationBuilder.CreateTable(
        name: "products",
        columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            // ... más columnas
            CategoryId = table.Column<int>(type: "integer", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_products", x => x.Id);
            table.ForeignKey(
                name: "FK_products_categories_CategoryId",
                column: x => x.CategoryId,
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        });

    // Índices
    migrationBuilder.CreateIndex(name: "IX_categories_Name", table: "categories", column: "Name", unique: true);
    migrationBuilder.CreateIndex(name: "IX_products_SKU", table: "products", column: "SKU", unique: true);
    migrationBuilder.CreateIndex(name: "IX_products_CategoryId", table: "products", column: "CategoryId");
    migrationBuilder.CreateIndex(name: "IX_products_Name", table: "products", column: "Name");

    // Seed data (4 categorías, 6 productos)
    migrationBuilder.InsertData(...);
}
```

***

## 🐳 Docker Compose (PostgreSQL)

```yaml
# docker-compose.yml (raíz del proyecto)
services:
  postgres:
    image: postgres:16
    container_name: inventory-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

Variables de entorno requeridas (`.env` o exportadas):

```bash
POSTGRES_DB=inventory_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
```

***

## 🛠️ Comandos útiles

```bash
# 1. Iniciar PostgreSQL
docker-compose up -d

# 2. Crear migración (desde carpeta InventoryManagement.Api)
dotnet ef migrations add NombreMigracion

# 3. Aplicar migración a BD
dotnet ef database update

# 4. Revertir última migración
dotnet ef migrations remove

# 5. Ver migraciones aplicadas
dotnet ef migrations list

# 6. Generar script SQL
dotnet ef migrations script

# 7. Ejecutar API
dotnet run
# Swagger: https://localhost:7279/swagger
```

***

## 📚 Próximos pasos sugeridos

| Área                   | Pendiente                                         |
| ---------------------- | ------------------------------------------------- |
| **Validación**         | FluentValidation o DataAnnotations completos      |
| **DTOs**               | Separar entidades de modelos de entrada/salida    |
| **Autenticación**      | JWT Bearer + roles (Admin, User)                  |
| **Tests**              | xUnit + TestContainers para integration tests     |
| **Logging**            | Serilog + Seq / Elasticsearch                     |
| **Caché**              | Redis para consultas frecuentes                   |
| **Auditoría**          | Interceptors para CreatedAt/UpdatedAt automáticos |
| **Soft delete global** | Query filters en `OnModelCreating`                |
| **Health checks**      | `/health` endpoint con DB check                   |

***

## 🔗 Referencias útiles

* [EF Core Docs](https://learn.microsoft.com/ef/core/)

* [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)

* [Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)

* [PostgreSQL Data Types](https://www.postgresql.org/docs/current/datatype.html)

***

## 📦 FASE 1 - DTOs, Validación y Mapeo (Agregado 19/10/2026)

### Paquetes adicionales instalados

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
  <PackageReference Include="Mapster" Version="7.4.0" />
  <PackageReference Include="Mapster.DependencyInjection" Version="1.0.1" />
</ItemGroup>
```

| Paquete                       | Propósito                                         |
| ----------------------------- | ------------------------------------------------- |
| `FluentValidation.AspNetCore` | Validación declarativa + integración ASP.NET Core |
| `Mapster`                     | Mapeo objeto-objeto de alto rendimiento           |
| `Mapster.DependencyInjection` | Registro DI automático de Mapster                 |

### Nueva estructura de carpetas

```
InventoryManagement.Api/
├── Common/
│   └── ApiResponse.cs           # ApiResponse<T>, PagedResult<T>, PaginationMeta
├── DTOs/
│   ├── CategoryDtos.cs          # CategoryDto, CreateCategoryDto, UpdateCategoryDto, CategoryRefDto
│   └── ProductDtos.cs           # ProductDto, ProductListDto, CreateProductDto, UpdateProductDto, UpdateStockDto, ProductFilterDto
├── Validators/
│   ├── CategoryValidators.cs    # CreateCategoryValidator, UpdateCategoryValidator
│   └── ProductValidators.cs     # CreateProductValidator, UpdateProductValidator, UpdateStockValidator, ProductFilterValidator
├── Mapping/
│   └── MapsterConfig.cs         # Configuración completa Mapster (Entity ↔ DTO)
└── Models/ (existente)
```

### DTOs - Principales

#### CategoryDtos.cs

```csharp
public record CategoryDto(int Id, string Name, string? Description, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt, int ProductsCount);
public record CreateCategoryDto([Required, MaxLength(100)] string Name, [MaxLength(500)] string? Description);
public record UpdateCategoryDto([Required, MaxLength(100)] string Name, [MaxLength(500)] string? Description, bool IsActive);
public record CategoryRefDto(int Id, string Name); // Ligero para referencias
```

#### ProductDtos.cs

```csharp
// Salida completa
public record ProductDto(int Id, string Name, string? SKU, string? Description, decimal Price, decimal Cost, int StockQuantity, int MinStockLevel, string? UnitOfMeasure, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt, CategoryRefDto Category, bool IsLowStock, decimal MarginPercentage);

// Salida ligera (listados)
public record ProductListDto(int Id, string Name, string? SKU, decimal Price, int StockQuantity, bool IsActive, bool IsLowStock, CategoryRefDto Category);

// Entrada
public record CreateProductDto([Required, MaxLength(200)] string Name, [Required, Range(0.01, double.MaxValue)] decimal Price, [Required, Range(0, double.MaxValue)] decimal Cost, [Required] int CategoryId, [MaxLength(50)] string? SKU, [MaxLength(1000)] string? Description, [Range(0, int.MaxValue)] int StockQuantity, [Range(0, int.MaxValue)] int MinStockLevel, [MaxLength(50)] string? UnitOfMeasure);

public record UpdateProductDto(/* mismos campos + IsActive */);
public record UpdateStockDto([Required, Range(0, int.MaxValue)] int Quantity);

// Filtros avanzados
public record ProductFilterDto(int Page = 1, [Range(1, 100)] int PageSize = 20, int? CategoryId, bool? IsActive, string? Search, decimal? MinPrice, decimal? MaxPrice, bool? LowStockOnly, string? SortBy, bool SortDesc);
```

#### Common/ApiResponse.cs

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public object? Meta { get; set; }
    // Factory methods: Ok(), Fail()
}

public class PagedResult<T>
{
    public List<T> Data { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    // Factory: Create()
}
```

### Validación (FluentValidation)

#### Validators/CategoryValidators.cs

```csharp
public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).Matches(@"^[\p{L}\p{N}\s\-']+$");
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));
    }
}
```

#### Validators/ProductValidators.cs

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    private readonly InventoryDbContext _db;
    public CreateProductValidator(InventoryDbContext db) { _db = db; ... }
    
    // Valida: Name, SKU (formato + unicidad en BD), Description, Price > 0, Cost >= 0, Stock >= 0, MinStockLevel >= 0, UnitOfMeasure, CategoryId (existe en BD)
}

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    private int? _productId;
    public UpdateProductValidator(InventoryDbContext db) { _db = db; ... }
    public UpdateProductValidator SetProductId(int id) { _productId = id; return this; }
    // Valida SKU único excluyendo el producto actual
}
```

**Validaciones de BD integradas:**

* `CategoryId` → `MustAsync(CategoryExists)` verifica FK en base de datos

* `SKU` → `MustAsync(SkuUnique)` verifica unicidad en tabla products

#### Helpers en Program.cs

```csharp
static IResult ValidationProblem(ValidationResult result)
    => Results.ValidationProblem(result.ToDictionary());

static ApiResponse<T> OkResponse<T>(T data, string? message = null, object? meta = null)
    => ApiResponse<T>.Ok(data, message, meta);

static ApiResponse<T> FailResponse<T>(string error, string? message = null)
    => ApiResponse<T>.Fail(error, message);
```

### Mapeo (Mapster) - Mapping/MapsterConfig.cs

```csharp
public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        // Category -> CategoryDto (incluye ProductsCount)
        TypeAdapterConfig<Category, CategoryDto>.NewConfig()
            .Map(dest => dest.ProductsCount, src => src.Products?.Count ?? 0);

        // Category -> CategoryRefDto
        TypeAdapterConfig<Category, CategoryRefDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name);

        // Create/Update DTOs -> Entity (ignora Id, timestamps, nav props)
        TypeAdapterConfig<CreateCategoryDto, Category>.NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.IsActive, _ => true)
            .Ignore(dest => dest.Id).Ignore(dest => dest.UpdatedAt).Ignore(dest => dest.Products);

        // Product -> ProductDto (completo: Category, IsLowStock, MarginPercentage)
        TypeAdapterConfig<Product, ProductDto>.NewConfig()
            .Map(dest => dest.Category, src => MapCategoryRef(src.Category))
            .Map(dest => dest.IsLowStock, src => src.StockQuantity <= src.MinStockLevel)
            .Map(dest => dest.MarginPercentage, src => src.Cost > 0 ? Math.Round(((src.Price - src.Cost) / src.Cost) * 100, 2) : 0);

        // Product -> ProductListDto (ligero)
        TypeAdapterConfig<Product, ProductListDto>.NewConfig()
            .Map(dest => dest.Category, src => MapCategoryRef(src.Category))
            .Map(dest => dest.IsLowStock, src => src.StockQuantity <= src.MinStockLevel);

        // Create/Update ProductDto -> Entity
        TypeAdapterConfig<CreateProductDto, Product>.NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.IsActive, _ => true)
            .Ignore(dest => dest.Id, dest.UpdatedAt, dest.Category);
        
        TypeAdapterConfig<UpdateProductDto, Product>.NewConfig()
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id, dest.CreatedAt, dest.Category);
    }

    private static CategoryRefDto MapCategoryRef(Category? category)
        => category != null ? new CategoryRefDto(category.Id, category.Name) : new CategoryRefDto(0, string.Empty);
}
```

### Program.cs actualizado - Endpoints refactorizados

#### Servicios registrados

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMapster();
MapsterConfig.RegisterMappings();
```

#### Ejemplo: POST /api/categories

```csharp
categories.MapPost("/", async (CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, InventoryDbContext db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid) return ValidationProblem(validation);

    var entity = dto.Adapt<Category>();
    db.Categories.Add(entity);
    await db.SaveChangesAsync();

    var result = entity.Adapt<CategoryDto>();
    return Results.Created($"/api/categories/{entity.Id}", OkResponse(result, "Categoría creada"));
});
```

#### Ejemplo: GET /api/products (paginado + filtros + orden)

```csharp
products.MapGet("/", async (InventoryDbContext db, IValidator<ProductFilterDto> filterValidator, ProductFilterDto filter) =>
{
    var validation = await filterValidator.ValidateAsync(filter);
    if (!validation.IsValid) return ValidationProblem(validation);

    var query = db.Products.Include(p => p.Category).AsQueryable();
    // ... filtros: CategoryId, IsActive, Search, MinPrice, MaxPrice, LowStockOnly
    // ... orden dinámico: SortBy (name/price/stock/createdat) + SortDesc

    var total = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ProjectToType<ProductListDto>() // Mapster projection a DB
        .ToListAsync();

    var paged = PagedResult<ProductListDto>.Create(items, total, filter.Page, filter.PageSize);
    return Results.Ok(OkResponse(paged, meta: new { pagination = new { paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages, paged.HasPreviousPage, paged.HasNextPage } }));
});
```

### Respuestas de ejemplo

#### Éxito (200/201)

```json
{
  "success": true,
  "data": { "id": 1, "name": "Laptop Gaming", "price": 1299.99, ... },
  "message": "Producto creado",
  "errors": [],
  "meta": null
}
```

#### Lista paginada

```json
{
  "success": true,
  "data": {
    "data": [...],
    "totalCount": 25,
    "page": 1,
    "pageSize": 20,
    "totalPages": 2,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "errors": [],
  "meta": {
    "pagination": { "totalCount": 25, "page": 1, "pageSize": 20, "totalPages": 2, "hasPreviousPage": false, "hasNextPage": true }
  }
}
```

#### Error de validación (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["El nombre es obligatorio"],
    "Price": ["El precio debe ser mayor a 0"],
    "CategoryId": ["La categoría seleccionada no existe"]
  }
}
```

#### No encontrado (404)

```json
{
  "success": false,
  "data": null,
  "message": "Producto no encontrado",
  "errors": ["Producto no encontrado"],
  "meta": null
}
```

### Migración de base de datos

> **Sin cambios** - La Fase 1 solo afecta capa de aplicación (DTOs, validación, mapeo, endpoints). La BD y entidades siguen igual.

### Para probar

```bash
# 1. Iniciar PostgreSQL
docker-compose up -d

# 2. Aplicar migración
cd InventoryManagement.Api
dotnet ef database update

# 3. Ejecutar
dotnet run
# Swagger: http://localhost:5263/swagger
```

### Próximos pasos (Fase 2) - **COMPLETADA ✅**

## 📦 FASE 2 - Error Handling Global + Logging (Agregado 19/10/2026)

### Paquetes adicionales instalados

```xml
<ItemGroup>
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.1" />
  <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  <PackageReference Include="Serilog.Enrichers.CorrelationId" Version="3.0.1" />
  <PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
  <PackageReference Include="Serilog.Enrichers.Process" Version="3.0.0" />
  <PackageReference Include="Serilog.Enrichers.Thread" Version="3.0.0" />
  <PackageReference Include="Serilog.Formatting.Compact" Version="2.0.0" />
</ItemGroup>
```

| Paquete                           | Propósito                                         |
| --------------------------------- | ------------------------------------------------- |
| `Serilog.AspNetCore`              | Integración Serilog con ASP.NET Core              |
| `Serilog.Sinks.Console`           | Log a consola (formato legible)                   |
| `Serilog.Sinks.File`              | Log a archivo (JSON compacto, rolling daily)      |
| `Serilog.Enrichers.CorrelationId` | Correlation ID automático (RequestId)             |
| `Serilog.Enrichers.Environment`   | MachineName, Environment, UserName                |
| `Serilog.Enrichers.Process`       | ProcessId                                         |
| `Serilog.Enrichers.Thread`        | ThreadId                                          |
| `Serilog.Formatting.Compact`      | Formatter JSON compacto para archivos             |

### Nueva estructura de carpetas (Fase 2)

```
InventoryManagement.Api/
├── Common/
│   ├── ApiResponse.cs
│   └── Exceptions/                # ✅ NUEVO
│       ├── NotFoundException.cs
│       ├── BusinessRuleException.cs
│       ├── ConcurrencyException.cs
│       └── ValidationException.cs
├── Middleware/                    # ✅ NUEVO
│   └── ExceptionHandlingMiddleware.cs
├── Logging/                       # ✅ NUEVO
│   └── EfCoreSlowQueryInterceptor.cs
└── ... (existente)
```

### Excepciones de dominio

#### Common/Exceptions/NotFoundException.cs
```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object resourceKey)
        : base($"{resourceName} con clave '{resourceKey}' no encontrado")
    {
        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }
    public string ResourceName { get; }
    public object ResourceKey { get; }
}
```

#### Common/Exceptions/BusinessRuleException.cs
```csharp
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string rule, string? detail = null)
        : base($"Regla de negocio violada: {rule}" + (detail != null ? $" - {detail}" : ""))
    {
        Rule = rule;
        Detail = detail;
    }
    public string Rule { get; }
    public string? Detail { get; }
}
```

#### Common/Exceptions/ConcurrencyException.cs
```csharp
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string resourceName, object resourceKey)
        : base($"Conflicto de concurrencia: {resourceName} con clave '{resourceKey}' fue modificado por otro usuario")
    {
        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }
    public string ResourceName { get; }
    public object ResourceKey { get; }
}
```

#### Common/Exceptions/ValidationException.cs
```csharp
public class ValidationException : Exception
{
    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
    public IDictionary<string, string[]> Errors { get; }
}
```

### Middleware global - Middleware/ExceptionHandlingMiddleware.cs

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        _logger.LogError(exception, "Excepción no controlada | CorrelationId: {CorrelationId} | Path: {Path} | Method: {Method}", 
            correlationId, context.Request.Path, context.Request.Method);

        var problemDetails = CreateProblemDetails(context, exception, correlationId);
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? 500;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions 
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, string correlationId)
    {
        return exception switch
        {
            NotFoundException ex => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Recurso no encontrado",
                Status = 404,
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["resourceName"] = ex.ResourceName,
                    ["resourceKey"] = ex.ResourceKey
                }
            },
            BusinessRuleException ex => new ProblemDetails { /* ... 409 Conflict ... */ },
            ConcurrencyException ex => new ProblemDetails { /* ... 409 Conflict ... */ },
            ValidationException ex => new ValidationProblemDetails(ex.Errors) { /* ... 400 ... */ },
            FluentValidation.ValidationException ex => new ValidationProblemDetails(...) { /* ... 400 ... */ },
            DbUpdateConcurrencyException => new ProblemDetails { /* ... 409 ... */ },
            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Error interno del servidor",
                Status = 500,
                Detail = "Ocurrió un error inesperado. Por favor intente más tarde.",
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?> { ["correlationId"] = correlationId }
            }
        };
    }
}
```

### Registro en Program.cs

```csharp
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

// ... DbContext, FluentValidation, Mapster ...

var app = builder.Build();

// ─── Pipeline ───
app.UseExceptionHandling();  // ← Debe ir ANTES de UseHttpsRedirection
app.UseHttpsRedirection();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (dc, httpContext) =>
    {
        dc.Set("CorrelationId", httpContext.TraceIdentifier);
        dc.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
    };
});

// Auto-migrate
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}
```

### Slow Query Interceptor - Logging/EfCoreSlowQueryInterceptor.cs

```csharp
public class EfCoreSlowQueryInterceptor : IObserver<DbCommandEventData>
{
    private readonly ILogger<EfCoreSlowQueryInterceptor> _logger;
    private readonly int _slowQueryThresholdMs;

    public EfCoreSlowQueryInterceptor(ILogger<EfCoreSlowQueryInterceptor> logger, int slowQueryThresholdMs = 500)
    {
        _logger = logger;
        _slowQueryThresholdMs = slowQueryThresholdMs;
    }

    public void OnNext(DbCommandEventData value)
    {
        if (value is DbCommandEventData.CommandExecutedEventData executed)
        {
            var elapsedMs = executed.Duration.TotalMilliseconds;
            if (elapsedMs >= _slowQueryThresholdMs)
            {
                var contextName = value.Context?.GetType().Name ?? "UnknownContext";
                var commandText = value.Command.CommandText;
                if (commandText.Length > 2000) commandText = commandText[..2000] + "... [TRUNCATED]";

                var parameters = value.Command.Parameters.Cast<DbParameter>()
                    .Select(p => $"{p.ParameterName} = {p.Value ?? "NULL"}")
                    .ToList();

                _logger.LogWarning(
                    "⚠️ SLOW QUERY DETECTED | Context: {Context} | Duration: {Duration}ms | Threshold: {Threshold}ms | Command: {Command} | Parameters: [{Parameters}]",
                    contextName, elapsedMs, _slowQueryThresholdMs, commandText, string.Join(", ", parameters));
            }
        }
    }
    public void OnCompleted() { }
    public void OnError(Exception error) { }
}
```

### Configuración Serilog - appsettings.json

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": [ "FromLogContext", "WithCorrelationId", "WithMachineName", "WithProcessId", "WithThreadId" ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext} - {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/inventory-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Properties": { "Application": "InventoryManagement.Api" }
  },
  "EfCore": { "SlowQueryThresholdMs": 500 }
}
```

### Respuestas de error unificadas (RFC 7807)

#### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Recurso no encontrado",
  "status": 404,
  "detail": "Category con clave '999' no encontrado",
  "instance": "/api/categories/999",
  "correlationId": "0HNPE4RR2A1MD:00000001",
  "resourceName": "Category",
  "resourceKey": 999
}
```

#### 400 Validation Error
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["El nombre es obligatorio"],
    "CategoryId": ["La categoría seleccionada no existe"]
  },
  "correlationId": "0HNPE4RR2A1MD:00000002"
}
```

#### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Error interno del servidor",
  "status": 500,
  "detail": "Ocurrió un error inesperado. Por favor intente más tarde.",
  "instance": "/api/products",
  "correlationId": "0HNPE4RR2A1MD:00000003"
}
```

### Verificación funcional

```bash
# 1. Build
dotnet build  # ✅ Compila sin errores

# 2. PostgreSQL
docker-compose up -d  # ✅ Levanta BD

# 3. Ejecutar
dotnet run --urls "http://localhost:5263"  # ✅ API corriendo

# 4. Probar endpoints
GET  http://localhost:5263/api/categories           # 200 OK
GET  http://localhost:5263/api/products             # 200 OK (paginado)
GET  http://localhost:5263/api/categories/999       # 404 ProblemDetails + correlationId
POST http://localhost:5263/api/categories (name="") # 400 ValidationProblemDetails
```

### Próximos pasos (Fase 3)

* [ ] Entidad `StockMovement` (Entrada/Salida/Ajuste/Transferencia)
* [ ] Endpoint `POST /api/products/{id}/movements`
* [ ] Validación: no permitir salida si stock insuficiente
* [ ] Kardex / Historial: `GET /api/products/{id}/kardex`
* [ ] Migración EF Core
