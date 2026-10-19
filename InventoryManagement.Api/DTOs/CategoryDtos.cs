using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs;

/// <summary>
/// DTO para leer una categoría (salida)
/// </summary>
public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int ProductsCount = 0
);

/// <summary>
/// DTO para crear una categoría (entrada)
/// </summary>
public record CreateCategoryDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null
);

/// <summary>
/// DTO para actualizar una categoría (entrada)
/// </summary>
public record UpdateCategoryDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null,
    bool IsActive = true
);

/// <summary>
/// DTO ligero para referencias a categorías (en productos, etc.)
/// </summary>
public record CategoryRefDto(
    int Id,
    string Name
);