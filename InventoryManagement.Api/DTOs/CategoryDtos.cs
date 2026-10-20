using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

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
    int ProductsCount = 0,
    int? ParentCategoryId = null,
    string? ParentCategoryName = null
);

/// <summary>
/// DTO para árbol de categorías (jerarquía)
/// </summary>
public record CategoryTreeDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    List<CategoryTreeDto> Children,
    int? ParentCategoryId = null  // Solo para uso interno al construir el árbol
);

/// <summary>
/// DTO para crear una categoría (entrada)
/// </summary>
public record CreateCategoryDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null,
    int? ParentCategoryId = null
);

/// <summary>
/// DTO para actualizar una categoría (entrada)
/// </summary>
public record UpdateCategoryDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null,
    bool IsActive = true,
    int? ParentCategoryId = null
);

/// <summary>
/// DTO ligero para referencias a categorías (en productos, etc.)
/// </summary>
public record CategoryRefDto(
    int Id,
    string Name
);