using System.ComponentModel.DataAnnotations;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.DTOs;

/// <summary>
/// Solicitud de cambio (edición o retiro) para que Admin la revise.
/// Incluye los valores actuales del producto para mostrar "actual vs propuesto".
/// </summary>
public record ProductChangeRequestDto(
    int Id,
    int ProductId,
    string ProductName,
    ChangeRequestType Type,
    ChangeRequestStatus Status,
    string? Name,
    string? SKU,
    string? Description,
    decimal? Cost,
    int? CategoryId,
    int? MinStockLevel,
    string? UnitOfMeasure,
    string? Reason,
    int RequestedBy,
    DateTime RequestedAt,
    int? ReviewedBy,
    DateTime? ReviewedAt,
    string? ReviewComment,
    ProductCurrentValuesDto Current
);

/// <summary>Valores actuales del producto, para comparar con lo propuesto.</summary>
public record ProductCurrentValuesDto(
    string Name,
    string? SKU,
    string? Description,
    decimal Cost,
    int CategoryId,
    int MinStockLevel,
    string? UnitOfMeasure
);

/// <summary>Comentario opcional de Admin al aprobar o rechazar una solicitud.</summary>
public record ReviewChangeRequestDto(
    [MaxLength(500)] string? Comment = null
);
