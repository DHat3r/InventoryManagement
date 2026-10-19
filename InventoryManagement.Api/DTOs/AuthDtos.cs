using System.ComponentModel.DataAnnotations;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.DTOs;

/// <summary>
/// Request de login
/// </summary>
public record LoginDto(
    [Required, MaxLength(50)] string UserName,
    [Required, MaxLength(100)] string Password
);

/// <summary>
/// Response de login exitoso
/// </summary>
public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User
);

/// <summary>
/// Info del usuario logueado
/// </summary>
public record UserInfoDto(
    int Id,
    string UserName,
    string? Rut,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

/// <summary>
/// Request para refresh token
/// </summary>
public record RefreshTokenDto(
    [Required] string RefreshToken
);

/// <summary>
/// Response de refresh exitoso
/// </summary>
public record RefreshResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

/// <summary>
/// DTO para crear usuario (solo Admin)
/// </summary>
public record CreateUserDto(
    [Required, MaxLength(50)] string UserName,
    [Required, MaxLength(100), MinLength(8)] string Password,
    [Required, MaxLength(200)] string FullName,
    [MaxLength(12)] string? Rut = null,
    [Required] UserRole Role = UserRole.Comprador
);

/// <summary>
/// DTO para actualizar usuario (solo Admin)
/// </summary>
public record UpdateUserDto(
    [Required, MaxLength(50)] string UserName,
    [Required] UserRole Role = UserRole.Comprador,
    [MaxLength(12)] string? Rut = null,
    [MaxLength(200)] string? FullName = null,
    bool IsActive = true
);

/// <summary>
/// DTO para cambiar password (usuario autenticado)
/// </summary>
public record ChangePasswordDto(
    [Required, MaxLength(100)] string CurrentPassword,
    [Required, MaxLength(100), MinLength(8)] string NewPassword
);

/// <summary>
/// DTO para listado de usuarios (paginado)
/// </summary>
public record UserListDto(
    int Id,
    string UserName,
    string? Rut,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

/// <summary>
/// Filtros para listado de usuarios
/// </summary>
public record UserFilterDto(
    int Page = 1,
    [Range(1, 100)] int PageSize = 20,
    UserRole? Role = null,
    bool? IsActive = null,
    string? Search = null
);