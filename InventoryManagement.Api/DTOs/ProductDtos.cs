using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs;

/// <summary>
/// DTO para leer un producto (salida completa)
/// </summary>
public record ProductDto(
    int Id,
    string Name,
    string? SKU,
    string? Description,
    decimal Price,
    decimal Cost,
    int StockQuantity,
    int MinStockLevel,
    string? UnitOfMeasure,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CategoryRefDto Category,
    bool IsLowStock,
    decimal MarginPercentage
);

/// <summary>
/// DTO ligero para listados de productos
/// </summary>
public record ProductListDto(
    int Id,
    string Name,
    string? SKU,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    bool IsLowStock,
    CategoryRefDto Category
);

/// <summary>
/// DTO para crear un producto (entrada)
/// </summary>
public record CreateProductDto(
    [Required, MaxLength(200)] string Name,
    [Required, Range(0.01, double.MaxValue)] decimal Price,
    [Required, Range(0, double.MaxValue)] decimal Cost,
    [Required] int CategoryId,
    [MaxLength(50)] string? SKU = null,
    [MaxLength(1000)] string? Description = null,
    [Range(0, int.MaxValue)] int StockQuantity = 0,
    [Range(0, int.MaxValue)] int MinStockLevel = 10,
    [MaxLength(50)] string? UnitOfMeasure = "UN"
);

/// <summary>
/// DTO para actualizar un producto (entrada)
/// </summary>
public record UpdateProductDto(
    [Required, MaxLength(200)] string Name,
    [Required, Range(0.01, double.MaxValue)] decimal Price,
    [Required, Range(0, double.MaxValue)] decimal Cost,
    [Required] int CategoryId,
    [MaxLength(50)] string? SKU = null,
    [MaxLength(1000)] string? Description = null,
    [Range(0, int.MaxValue)] int StockQuantity = 0,
    [Range(0, int.MaxValue)] int MinStockLevel = 10,
    [MaxLength(50)] string? UnitOfMeasure = "UN",
    bool IsActive = true
);

/// <summary>
/// DTO para actualización parcial de stock
/// </summary>
public record UpdateStockDto(
    [Required, Range(0, int.MaxValue)] int Quantity
);

/// <summary>
/// DTO para filtros de búsqueda de productos
/// </summary>
public record ProductFilterDto(
    int Page = 1,
    [Range(1, 100)] int PageSize = 20,
    int? CategoryId = null,
    bool? IsActive = null,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? LowStockOnly = null,
    string? SortBy = null, // name, price, stock, createdAt
    bool SortDesc = false
);