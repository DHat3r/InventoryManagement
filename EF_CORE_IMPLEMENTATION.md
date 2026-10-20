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

* [x] Entidad `StockMovement` (Entrada/Salida/Ajuste)
* [x] Endpoint `POST /api/products/{id}/movements`
* [x] Validación: no permitir salida si stock insuficiente
* [x] Kardex / Historial: `GET /api/products/{id}/kardex`
* [x] Resumen valorado: `GET /api/products/{id}/kardex/summary`
* [x] Migración EF Core

***

## 📦 FASE 3 - Stock Movements / Kardex (Agregado 19/10/2026)

### Objetivo

Implementar trazabilidad completa de movimientos de stock (Kardex) con convención contable:
- **Quantity siempre positivo** — el signo lo determina `MovementType`
- **Valoración histórica** — `UnitCost` guardado en cada movimiento para PEPS/UEPS/Promedio
- **Transaccionalidad** — movimiento + actualización stock en misma transacción

### Paquetes adicionales

> **Sin paquetes nuevos** — Solo entidades, DTOs, validadores y endpoints.

### Nuevos archivos creados

```
InventoryManagement.Api/
├── Models/
│   ├── MovementType.cs          # enum: Entrada=1, Salida=2, Ajuste=3
│   └── StockMovement.cs         # Entidad Kardex
├── DTOs/
│   └── StockMovementDtos.cs     # CreateStockMovementDto, StockMovementDto, StockMovementListDto, StockMovementFilterDto, KardexSummaryDto
├── Validators/
│   └── StockMovementValidators.cs  # CreateStockMovementValidator (BD), StockMovementFilterValidator
└── Mapping/
    └── MapsterConfig.cs         # + configs StockMovement ↔ DTOs
```

### Entidad StockMovement

```csharp
public class StockMovement
{
    [Key] public int Id { get; set; }
    [Required] public int ProductId { get; set; }
    [Required] public MovementType Type { get; set; }        // Entrada / Salida / Ajuste
    [Required, Range(1, int.MaxValue)] public int Quantity { get; set; }  // Siempre > 0
    [MaxLength(200)] public string? Reason { get; set; }     // "Compra", "Venta", "Ajuste inventario", "Merma"
    [MaxLength(100)] public string? Reference { get; set; }  // Nº factura, orden, nota crédito
    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,2)")] public decimal? UnitCost { get; set; }  // Costo unitario al momento
    [Required] public int PreviousStock { get; set; }        // Stock antes
    [Required] public int NewStock { get; set; }             // Stock después

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}

public enum MovementType : byte
{
    Entrada = 1,   // Compra, devolución proveedor, ajuste +
    Salida = 2,    // Venta, consumo interno, merma, ajuste -
    Ajuste = 3     // Inventario físico, recuento (dirección por UnitCost)
}
```

### Convención contable (clave para el contador)

| Tipo | Quantity | UnitCost | Efecto Stock | Caso de uso |
|------|----------|----------|--------------|-------------|
| **Entrada** | +N | **Requerido** | `+N` | Compra, devolución proveedor |
| **Salida** | +N | Opcional (usa costo promedio) | `-N` | Venta, consumo, merma |
| **Ajuste** | +N | **Con valor = Entrada**, **Sin valor = Salida** | `±N` | Inventario físico, recuento, merma detectada |

> **Ventaja**: El contador ve movimientos claros: "Entrada 10 @ $900", "Salida 5 @ $950", "Ajuste +3 @ $950", "Ajuste -2". No hay cantidades negativas confusas.

### DTOs principales

```csharp
// Entrada - crear movimiento
public record CreateStockMovementDto(
    [Required] MovementType Type,
    [Required, Range(1, int.MaxValue)] int Quantity,
    [MaxLength(200)] string? Reason = null,
    [MaxLength(100)] string? Reference = null,
    [Range(0, double.MaxValue)] decimal? UnitCost = null
);

// Salida - kardex completo
public record StockMovementDto(
    int Id, int ProductId, MovementType Type, int Quantity,
    string? Reason, string? Reference, DateTime CreatedAt,
    decimal? UnitCost, int PreviousStock, int NewStock
);

// Salida - listado ligero
public record StockMovementListDto(
    int Id, MovementType Type, int Quantity,
    string? Reason, string? Reference, DateTime CreatedAt,
    decimal? UnitCost, int PreviousStock, int NewStock
);

// Filtros kardex
public record StockMovementFilterDto(
    int Page = 1,
    [Range(1, 100)] int PageSize = 50,
    MovementType? Type = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Reference = null
);

// Resumen valorado
public record KardexSummaryDto(
    int ProductId, string ProductName, string ProductSKU,
    int CurrentStock, decimal AverageCost, decimal StockValue,
    int TotalEntradas, int TotalSalidas, int TotalAjustes,
    decimal TotalEntradasValue, decimal TotalSalidasValue
);
```

### Validaciones de negocio (CreateStockMovementValidator)

```csharp
public class CreateStockMovementValidator : AbstractValidator<CreateStockMovementDto>
{
    private readonly InventoryDbContext _db;
    private int _productId;

    public CreateStockMovementValidator(InventoryDbContext db) { _db = db; ... }

    // Reglas:
    // - ProductId existe y está activo
    // - Entrada: Quantity > 0, UnitCost requerido
    // - Salida: Quantity > 0, StockQuantity >= Quantity (bloquea si insuficiente)
    // - Ajuste: Quantity > 0, stock final >= 0
    // - UnitCost >= 0 cuando presente
}
```

### Fluent API en InventoryDbContext

```csharp
modelBuilder.Entity<StockMovement>(entity =>
{
    entity.ToTable("stock_movements");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Type).HasConversion<byte>().IsRequired();
    entity.Property(e => e.Quantity).IsRequired();
    entity.Property(e => e.Reason).HasMaxLength(200);
    entity.Property(e => e.Reference).HasMaxLength(100);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
    entity.Property(e => e.PreviousStock).IsRequired();
    entity.Property(e => e.NewStock).IsRequired();
    
    // Índices para consultas kardex
    entity.HasIndex(e => new { e.ProductId, e.CreatedAt });
    entity.HasIndex(e => e.Type);
    entity.HasIndex(e => e.Reference);
    
    entity.HasOne(e => e.Product)
          .WithMany()
          .HasForeignKey(e => e.ProductId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

### Migración generada (20261019214749_AddStockMovements)

```sql
CREATE TABLE stock_movements (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "ProductId" integer NOT NULL,
    "Type" smallint NOT NULL,
    "Quantity" integer NOT NULL,
    "Reason" character varying(200),
    "Reference" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UnitCost" numeric(18,2),
    "PreviousStock" integer NOT NULL,
    "NewStock" integer NOT NULL,
    CONSTRAINT "FK_stock_movements_products_ProductId" 
        FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_stock_movements_ProductId_CreatedAt" ON stock_movements ("ProductId", "CreatedAt");
CREATE INDEX "IX_stock_movements_Reference" ON stock_movements ("Reference");
CREATE INDEX "IX_stock_movements_Type" ON stock_movements ("Type");
```

### Endpoints implementados

#### 1. POST /api/products/{id}/movements
Registra movimiento y actualiza stock atómicamente.

**Request:**
```json
{
  "type": "Entrada",
  "quantity": 10,
  "reason": "Compra inicial",
  "reference": "FC-001",
  "unitCost": 900.00
}
```

**Response 201:**
```json
{
  "success": true,
  "data": {
    "id": 2, "productId": 1, "type": "Entrada", "quantity": 10,
    "reason": "Compra inicial", "reference": "FC-001",
    "createdAt": "2026-10-19T21:57:01.952Z",
    "unitCost": 900.00, "previousStock": 25, "newStock": 35
  },
  "message": "Movimiento registrado",
  "errors": []
}
```

**Validaciones:**
- 404 si producto no existe
- 400 si stock insuficiente para Salida/Ajuste-
- 400 si Entrada sin UnitCost
- Transacción atómica (movimiento + stock update)

#### 2. GET /api/products/{id}/kardex
Historial paginado con filtros.

**Query params:** `page=1&pageSize=50&type=Entrada&dateFrom=2026-01-01&dateTo=2026-12-31&reference=FC-001`

**Response 200:**
```json
{
  "success": true,
  "data": {
    "data": [...],
    "totalCount": 5, "page": 1, "pageSize": 50,
    "totalPages": 1, "hasPreviousPage": false, "hasNextPage": false
  },
  "meta": { "pagination": {...} }
}
```

#### 3. GET /api/products/{id}/kardex/summary
Resumen valorado para contador.

**Response 200:**
```json
{
  "success": true,
  "data": {
    "productId": 1, "productName": "Laptop Gaming", "productSKU": "LAP-GAM-001",
    "currentStock": 31, "averageCost": 900.00, "stockValue": 27900.00,
    "totalEntradas": 20, "totalSalidas": 5, "totalAjustes": 5,
    "totalEntradasValue": 18000.00, "totalSalidasValue": 4750.00
  }
}
```

# 3.8 Validación stock insuficiente (debe fallar 400)
POST http://localhost:5263/api/products/1/movements
{ "type": "Salida", "quantity": 999, "reason": "Imposible" }

### 3.3 Búsqueda y Filtros Avanzados (completado)

#### Filtros de fechas
- `CreatedAtFrom` / `CreatedAtTo` — rango de fecha de creación
- `UpdatedAtFrom` / `UpdatedAtTo` — rango de fecha de actualización

```bash
# Productos creados desde una fecha
GET http://localhost:5263/api/products?CreatedAtFrom=2026-10-19

# Productos creados en rango
GET http://localhost:5263/api/products?CreatedAtFrom=2026-10-19&CreatedAtTo=2026-10-20

# Productos actualizados desde una fecha
GET http://localhost:5263/api/products?UpdatedAtFrom=2026-10-19
```

#### Ordenamiento multi-campo
Sintaxis: `?sort=campo1:direccion,campo2:direccion`

```bash
# Precio descendente, luego nombre ascendente
GET http://localhost:5263/api/products?sortBy=price:desc,name:asc

# Stock ascendente, luego fecha creación descendente
GET http://localhost:5263/api/products?sortBy=stock:asc,createdAt:desc

# Solo nombre (compatible con anterior)
GET http://localhost:5263/api/products?sortBy=name
```

**Campos soportados:** `name`, `price`, `stock`, `createdAt`, `updatedAt`

**Direcciones:** `asc` / `desc` (default: asc)

#### Validaciones
- `CreatedAtFrom` ≤ `CreatedAtTo` (400 si inválido)
- `UpdatedAtFrom` ≤ `UpdatedAtTo` (400 si inválido)

#### Implementación técnica
- Conversión automática a UTC para PostgreSQL (`timestamp with time zone`)
- Helper `ApplyMultiSort()` con `OrderBy` + `ThenBy` encadenados
- Validación en `ProductFilterValidator` (FluentValidation)

***

### Próximos pasos (Fase 4)

* [x] Identity + JWT + Roles (Admin, Inventario, Contador, Comprador)
* [x] UserId en StockMovement (auditoría quién hizo el movimiento)
* [x] CreatedBy/ModifiedBy en todas las entidades
* [x] Políticas de autorización por endpoint
* [x] Refresh tokens + revocación + rotación

***

## 📦 FASE 4 - Auth JWT + Users + Roles + Admin Seed (Agregado 19/10/2026)

### Objetivo

Implementar autenticación y autorización completa sin ASP.NET Core Identity, usando JWT Bearer tokens, refresh tokens con rotación, y un sistema de roles personalizado (Admin, Inventario, Contador, Comprador).

### Paquetes adicionales

```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.10" />
```

### Nuevos archivos creados

```
InventoryManagement.Api/
├── Models/
│   ├── UserRole.cs              # enum: Admin=1, Inventario=2, Contador=3, Comprador=4
│   ├── User.cs                  # Entidad usuario
│   └── RefreshToken.cs          # Entidad refresh token
├── DTOs/
│   └── AuthDtos.cs              # LoginDto, LoginResponseDto, RefreshTokenDto, CreateUserDto, UpdateUserDto, ChangePasswordDto, UserListDto, UserFilterDto, UserInfoDto
├── Validators/
│   └── AuthValidators.cs        # LoginValidator, CreateUserValidator, UpdateUserValidator, ChangePasswordValidator, RefreshTokenValidator, UserFilterValidator
├── Services/
│   ├── AuthService.cs           # Login, Refresh, Revoke, ChangePassword, JWT generation, BCrypt
│   ├── CurrentUserService.cs    # Acceso al usuario autenticado actual (claims)
│   └── AdminSeedService.cs      # Seed admin al arrancar la app
└── Mapping/
    └── MapsterConfig.cs         # + configs User ↔ DTOs
```

### Entidad User

```csharp
public class User
{
    [Key] public int Id { get; set; }
    [Required, MaxLength(50)] public string UserName { get; set; }
    [MaxLength(12)] public string? Rut { get; set; }  // Normalizado sin puntos/guión
    [Required, MaxLength(500)] public string PasswordHash { get; set; }
    [Required, MaxLength(200)] public string FullName { get; set; }
    [Required] public UserRole Role { get; set; } = UserRole.Comprador;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }

    // Navigation
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
    public virtual ICollection<StockMovement> StockMovements { get; set; }
    public virtual ICollection<Category> CategoriesCreated { get; set; }
    public virtual ICollection<Category> CategoriesModified { get; set; }
    public virtual ICollection<Product> ProductsCreated { get; set; }
    public virtual ICollection<Product> ProductsModified { get; set; }
}

public enum UserRole : byte
{
    Admin = 1,
    Inventario = 2,
    Contador = 3,
    Comprador = 4
}
```

### Entidad RefreshToken

```csharp
public class RefreshToken
{
    [Key] public int Id { get; set; }
    [Required] public int UserId { get; set; }
    [Required, MaxLength(500)] public string TokenHash { get; set; }  // SHA256
    [Required] public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    [MaxLength(500)] public string? ReplacedByTokenHash { get; set; }
    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
```

### Convención RUT según Rol

| Rol | RUT | Descripción |
|-----|-----|-------------|
| **Admin** | **Obligatorio** | Acceso total |
| **Inventario** | **Obligatorio** | Gestión stock |
| **Contador** | **Obligatorio** | Reportes y valoración |
| **Comprador** | **Opcional** | Solo entradas |

Validación en `CreateUserValidator` / `UpdateUserValidator`:
```csharp
RuleFor(x => x.Rut)
    .NotEmpty().When(x => x.Role != UserRole.Comprador)
        .WithMessage("El RUT es obligatorio para Admin, Inventario y Contador")
    .Must(BeValidRut).When(x => !string.IsNullOrEmpty(x.Rut))
        .WithMessage("El RUT no tiene un formato válido (ej: 12.345.678-9)");
```

### Admin Seed Service (App Startup)

El admin se crea/actualiza **cada vez que la app arranca**, leyendo config de `appsettings.json` / variables de entorno:

```json
"AdminSeed": {
  "UserName": "admin",
  "Rut": "765432105",
  "FullName": "Administrador del Sistema",
  "Password": "Admin123!",
  "RotateOnStartup": true,
  "RunOnStartup": true
}
```

- Si no existe → **crea** con password de config
- Si existe y `RotateOnStartup=true` + password cambió → **rota** hash
- Si existe y password igual → **no toca nada**

> Ventaja: Password configurable por env var / secret manager, rotación automática en deploy.

### JWT + Refresh Tokens (Rotación)

| Token | Expiración | Almacenamiento |
|-------|------------|----------------|
| Access Token | 15 min (configurable) | Solo memoria cliente |
| Refresh Token | 7 días (configurable) | BD (tabla `refresh_tokens`) |

**Rotación**: Cada refresh invalida el token anterior y genera uno nuevo (previene replay attacks).

```csharp
// Refresh endpoint
POST /api/auth/refresh { "refreshToken": "..." }
// Response: nuevo accessToken + nuevo refreshToken
```

### Políticas de Autorización

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireRole("Admin"))
    .AddPolicy("RequireInventarioOrAdmin", p => p.RequireRole("Admin", "Inventario"))
    .AddPolicy("RequireContadorOrAdmin", p => p.RequireRole("Admin", "Contador"))
    .AddPolicy("RequireCompradorOrInventarioOrAdmin", p => p.RequireRole("Admin", "Inventario", "Comprador"));
```

### Aplicación en Endpoints

| Endpoint | Policy | Quién puede |
|----------|--------|-------------|
| `POST /api/products/{id}/movements` (Entrada) | `RequireCompradorOrInventarioOrAdmin` | Comprador, Inventario, Admin |
| `POST /api/products/{id}/movements` (Salida) | *Validación interna* | Solo Inventario, Admin |
| `POST /api/products/{id}/movements` (Ajuste) | *Validación interna* | Solo Inventario, Admin |
| `GET /api/products/{id}/kardex` | `RequireContadorOrAdmin` | Contador, Inventario, Admin |
| `GET /api/products/{id}/kardex/summary` | `RequireContadorOrAdmin` | Contador, Inventario, Admin |
| `POST /api/products` | `RequireInventarioOrAdmin` | Inventario, Admin |
| `POST /api/users`, `DELETE /api/users/{id}` | `RequireAdmin` | Solo Admin |

> **Nota**: La validación de Salida/Ajuste se hace dentro del endpoint (no policy) porque depende del `Type` del DTO.

### Auditoría (CreatedBy / ModifiedBy)

Todas las entidades principales tienen columnas de auditoría:

```csharp
// Category, Product
public int? CreatedBy { get; set; }
public int? ModifiedBy { get; set; }

[ForeignKey(nameof(CreatedBy))]
public virtual User? CreatedByUser { get; set; }
[ForeignKey(nameof(ModifiedBy))]
public virtual User? ModifiedByUser { get; set; }

// StockMovement
public int? UserId { get; set; }  // Quién registró el movimiento
```

### Endpoints Auth

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `POST` | `/api/auth/login` | AllowAnonymous | Login → JWT + RefreshToken |
| `POST` | `/api/auth/refresh` | AllowAnonymous | Renovar access token |
| `POST` | `/api/auth/logout` | Authenticated | Revocar refresh token |
| `GET` | `/api/auth/me` | Authenticated | Info usuario actual |
| `POST` | `/api/auth/change-password` | Authenticated | Cambiar password (revoca tokens) |

### Endpoints Users (Admin only)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/users` | Listar usuarios (paginado, filtros) |
| `GET` | `/api/users/{id}` | Ver usuario |
| `POST` | `/api/users` | Crear usuario (hash password + validación RUT) |
| `PUT` | `/api/users/{id}` | Actualizar usuario |
| `DELETE` | `/api/users/{id}` | Soft delete (IsActive=false) |

### Migración generada (20261019231116_AddUsersAndAuth)

```sql
-- Tabla users
CREATE TABLE users (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "UserName" character varying(50) NOT NULL UNIQUE,
    "Rut" character varying(12) NULL,
    "PasswordHash" character varying(500) NOT NULL,
    "FullName" character varying(200) NOT NULL,
    "Role" smallint NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    "LastLoginAt" timestamp with time zone,
    "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
    "LockedUntil" timestamp with time zone
);

-- Índices
CREATE UNIQUE INDEX "IX_users_UserName" ON users ("UserName");
CREATE UNIQUE INDEX "IX_users_Rut" ON users ("Rut") WHERE "Rut" IS NOT NULL;
CREATE INDEX "IX_users_Role_IsActive" ON users ("Role", "IsActive");

-- Tabla refresh_tokens
CREATE TABLE refresh_tokens (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "UserId" integer NOT NULL,
    "TokenHash" character varying(500) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    "ReplacedByTokenHash" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_refresh_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
);

-- Auditoría en tablas existentes
ALTER TABLE products ADD "CreatedBy" integer, ADD "ModifiedBy" integer;
ALTER TABLE categories ADD "CreatedBy" integer, ADD "ModifiedBy" integer;
ALTER TABLE stock_movements ADD "UserId" integer;

-- FKs auditoría (SET NULL)
ALTER TABLE products ADD CONSTRAINT "FK_products_users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES users ("Id") ON DELETE SET NULL;
-- ... igual para ModifiedBy, categories, stock_movements
```

### Verificación funcional completa

```bash
# 1. Build y migración
dotnet build
dotnet ef migrations add AddUsersAndAuth
dotnet ef database update

# 2. Ejecutar
dotnet run --urls "http://localhost:5263"

# 3. Login admin (password de appsettings)
POST http://localhost:5263/api/auth/login
{ "userName": "admin", "password": "Admin123!" }

# 4. Usar accessToken en Authorization: Bearer <token>

# 5. Crear usuario Inventario
POST http://localhost:5263/api/users
{ "userName": "inventario1", "rut": "12345678-5", "password": "Test123!", "fullName": "Usuario Inventario", "role": 2 }

# 6. Login Inventario y crear movimiento Entrada
POST http://localhost:5263/api/products/1/movements
{ "type": "Entrada", "quantity": 10, "reason": "Compra", "reference": "FC-001", "unitCost": 900 }

# 7. Login Comprador y probar Entrada (OK) vs Salida (403)
POST http://localhost:5263/api/products/1/movements
{ "type": "Entrada", "quantity": 5 }  # 200 OK

POST http://localhost:5263/api/products/1/movements
{ "type": "Salida", "quantity": 5 }   # 403 Forbidden

# 8. Login Contador y ver kardex/summary (OK)
GET http://localhost:5263/api/products/1/kardex/summary

# 9. Refresh token
POST http://localhost:5263/api/auth/refresh
{ "refreshToken": "..." }

# 10. Logout (revoca refresh token)
POST http://localhost:5263/api/auth/logout
{ "refreshToken": "..." }
```

### Próximos pasos (Fase 5+)

* [x] Tests unitarios (xUnit + FluentAssertions + EF InMemory)
* [x] Tests de integración (TestContainers + WebApplicationFactory)
* [ ] Health Checks (/health, /health/ready, /health/live)
* [ ] OpenAPI mejorado (ejemplos, XML docs, annotations)
* [ ] Rate Limiting
* [ ] CORS configurado para frontend
* [ ] Dockerfile multi-stage para deploy

***

## 🧪 FASE 5.1 - Tests unitarios e integración (Agregado 19/10/2026)

### Estado

```bash
cd InventoryManagement.Tests
dotnet test
# Correctas! - Con error: 0, Superado: 36, Omitido: 0, Total: 36
```

| Tipo | Cantidad | Herramientas |
|------|----------|--------------|
| Unitarios (validadores) | 16 | xUnit + FluentAssertions + EF Core InMemory |
| Integración (API completa) | 20 | TestContainers PostgreSQL + WebApplicationFactory |

### Estructura de tests

```
InventoryManagement.Tests/
├── Infrastructure/
│   ├── TestWebApplicationFactory.cs   # Contenedor PostgreSQL + migración
│   └── IntegrationTestBase.cs         # Cliente autenticado como admin
├── Unit/Validators/
│   ├── CategoryValidatorsTests.cs
│   └── ProductValidatorsTests.cs
└── Integration/
    ├── Auth/AuthIntegrationTests.cs
    ├── Categories/CategoriesIntegrationTests.cs
    ├── Products/ProductsIntegrationTests.cs
    └── StockMovements/StockMovementsIntegrationTests.cs
```

### Decisión clave: `TargetFramework = net8.0`

El proyecto de tests **debe** apuntar a `net8.0` igual que la API. Con `net10.0` y
`Microsoft.AspNetCore.Mvc.Testing 8.0.10`, el `TestServer` usa un `PipeWriter` de
TestHost 8 que no implementa `PipeWriter.UnflushedBytes`, y `System.Text.Json` 10
lanza:

```
System.InvalidOperationException: The PipeWriter 'ResponseBodyPipeWriter'
does not implement PipeWriter.UnflushedBytes.
```

Todas las respuestas JSON terminaban en 500. Al alinear el TFM con la API se resuelve.

### TestWebApplicationFactory

- Inicia el contenedor en `InitializeAsync` (xUnit class fixture).
- Reemplaza el `DbContext` de producción por el del contenedor.
- Aplica `Database.Migrate()` durante `ConfigureWebHost`, antes de que
  `Program.cs` ejecute el seed del admin.
- El seed del admin lo sigue haciendo la aplicación al arrancar
  (`AdminSeed:RunOnStartup = true`), reutilizando la misma configuración.

### IntegrationTestBase

Los endpoints de categorías, productos y kardex requieren JWT. La clase base:

1. Crea el cliente con `WebApplicationFactory.CreateClient()`.
2. Hace `POST /api/auth/login` con `admin / Admin123!`.
3. Agrega `Authorization: Bearer <accessToken>` por defecto.
4. Configura `JsonStringEnumConverter` (la API serializa enums como texto).

### Correcciones de lógica detectadas por los tests

1. **Unicidad de nombre de categoría**: se agregó `NameUnique` / `NameUniqueForUpdate`
   a `CreateCategoryValidator` y `UpdateCategoryValidator` (antes la BD lanzaba una
   violación de índice único y el endpoint devolvía 500 en lugar de 400).
2. **Orden de validación en updates**: `SetCategoryId(id)` y `SetProductId(id)` ahora
   se ejecutan **antes** de `ValidateAsync`. Antes la unicidad de SKU/nombre y la
   detección de referencias circulares se saltaban en los PUT.
3. **Tests unitarios con InMemory**: los datos de prueba se siembran en el **mismo**
   `InventoryDbContext` que usa el validador (antes cada test creaba dos contextos
   distintos y las validaciones contra BD nunca encontraban datos).

