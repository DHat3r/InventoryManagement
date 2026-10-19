using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Api.Models;

/// <summary>
/// Refresh token para rotación JWT (tabla propia, sin Identity)
/// </summary>
public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string TokenHash { get; set; } = string.Empty;  // SHA256 del token

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? ReplacedByTokenHash { get; set; }  // Para detectar reuso (rotación)

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>Indica si el token está revocado o expirado</summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}