using System.ComponentModel.DataAnnotations;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.DTOs;

/// <summary>
/// DTO para crear un movimiento de stock (entrada)
/// </summary>
public record CreateStockMovementDto(
    [Required] MovementType Type,
    [Required, Range(1, int.MaxValue)] int Quantity,
    [MaxLength(200)] string? Reason = null,
    [MaxLength(100)] string? Reference = null,
    [Range(0, double.MaxValue)] decimal? UnitCost = null
);

/// <summary>
/// DTO para leer un movimiento de stock (salida completa)
/// </summary>
public record StockMovementDto(
    int Id,
    int ProductId,
    MovementType Type,
    int Quantity,
    string? Reason,
    string? Reference,
    DateTime CreatedAt,
    decimal? UnitCost,
    int PreviousStock,
    int NewStock
);

/// <summary>
/// DTO ligero para listados de kardex
/// </summary>
public record StockMovementListDto(
    int Id,
    MovementType Type,
    int Quantity,
    string? Reason,
    string? Reference,
    DateTime CreatedAt,
    decimal? UnitCost,
    int PreviousStock,
    int NewStock
);

/// <summary>
/// DTO para filtros de búsqueda en kardex
/// </summary>
public record StockMovementFilterDto(
    int Page = 1,
    [Range(1, 100)] int PageSize = 50,
    MovementType? Type = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Reference = null
);

/// <summary>
/// Resumen del kardex para un producto
/// </summary>
public record KardexSummaryDto(
    int ProductId,
    string ProductName,
    string ProductSKU,
    int CurrentStock,
    decimal AverageCost,
    decimal StockValue,
    int TotalEntradas,
    int TotalSalidas,
    int TotalAjustes,
    decimal TotalEntradasValue,
    decimal TotalSalidasValue
);