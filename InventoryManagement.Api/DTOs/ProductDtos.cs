using System.ComponentModel.DataAnnotations;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.DTOs;

/// <summary>
/// DTO para leer un producto (salida completa)
/// </summary>
public record ProductDto(
    int Id,
    string Name,
    string? SKU,
    string? Description,
    decimal? Price, // null si aún no se aprueba, o para quien no ve el precio de venta (Inventario)
    decimal Cost,
    int StockQuantity,
    int MinStockLevel,
    string? UnitOfMeasure,
    ProductApprovalStatus ApprovalStatus,
    ProductLifecycle Lifecycle,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CategoryRefDto Category,
    bool IsLowStock,
    bool IsOutOfStock,
    bool IsPossiblyUnavailable, // retiro solicitado por Inventario, esperando a Admin: sin salidas
    bool HasPendingEdit,        // edición propuesta por Inventario, esperando a Admin
    string? RejectionReason,
    decimal? MarginPercentage // null si no hay precio, o para quien no ve el precio de venta
);

/// <summary>
/// DTO ligero para listados de productos
/// </summary>
public record ProductListDto(
    int Id,
    string Name,
    string? SKU,
    decimal? Price, // null si aún no se aprueba, o para quien no ve el precio de venta
    int StockQuantity,
    ProductApprovalStatus ApprovalStatus,
    ProductLifecycle Lifecycle,
    bool IsLowStock,
    bool IsOutOfStock,
    bool IsPossiblyUnavailable,
    bool HasPendingEdit,
    CategoryRefDto Category
);

/// <summary>
/// DTO para crear un producto (entrada).
/// Admin envía el precio y el producto queda Aprobado; Inventario no lo envía y queda Pendiente.
/// </summary>
public record CreateProductDto(
    [Required, MaxLength(200)] string Name,
    [Range(0.01, double.MaxValue)] decimal? Price,
    [Required, Range(0, double.MaxValue)] decimal Cost,
    [Required] int CategoryId,
    [MaxLength(50)] string? SKU = null,
    [MaxLength(1000)] string? Description = null,
    [Range(0, int.MaxValue)] int StockQuantity = 0,
    [Range(0, int.MaxValue)] int MinStockLevel = 10,
    [MaxLength(50)] string? UnitOfMeasure = "UN"
);

/// <summary>
/// DTO para actualizar un producto (entrada).
/// Price: solo Admin; null deja el precio como está. La vigencia se cambia con sus propios endpoints.
/// </summary>
public record UpdateProductDto(
    [Required, MaxLength(200)] string Name,
    [Range(0.01, double.MaxValue)] decimal? Price,
    [Required, Range(0, double.MaxValue)] decimal Cost,
    [Required] int CategoryId,
    [MaxLength(50)] string? SKU = null,
    [MaxLength(1000)] string? Description = null,
    [Range(0, int.MaxValue)] int StockQuantity = 0,
    [Range(0, int.MaxValue)] int MinStockLevel = 10,
    [MaxLength(50)] string? UnitOfMeasure = "UN"
);

/// <summary>
/// DTO para actualización parcial de stock
/// </summary>
public record UpdateStockDto(
    [Required, Range(0, int.MaxValue)] int Quantity
);

/// <summary>Admin aprueba un producto pendiente y define su precio de venta.</summary>
public record ApproveProductDto(
    [Required, Range(0.01, double.MaxValue)] decimal Price
);

/// <summary>Motivo obligatorio: rechazo de un producto o retiro de la venta.</summary>
public record ReasonDto(
    [Required, MaxLength(500)] string Reason
);

/// <summary>
/// DTO para filtros de búsqueda de productos
/// </summary>
public record ProductFilterDto(
    int Page = 1,
    [Range(1, 100)] int PageSize = 20,
    int? CategoryId = null,
    ProductLifecycle? Lifecycle = null,
    ProductApprovalStatus? ApprovalStatus = null,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? LowStockOnly = null,
    bool? OutOfStockOnly = null,
    DateTime? CreatedAtFrom = null,
    DateTime? CreatedAtTo = null,
    DateTime? UpdatedAtFrom = null,
    DateTime? UpdatedAtTo = null,
    string? SortBy = null, // name, price, stock, createdAt, updatedAt (multi: "price:desc,name:asc")
    bool SortDesc = false
);
