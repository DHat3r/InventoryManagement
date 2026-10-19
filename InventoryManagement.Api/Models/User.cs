using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Api.Models;

/// <summary>
/// Usuario del sistema (Admin, Inventario, Contador, Comprador)
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(12)]  // Normalizado: sin puntos ni guión (ej: 765432105)
    public string? Rut { get; set; }

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Comprador;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    // Navigation properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<Category> CategoriesCreated { get; set; } = new List<Category>();
    public virtual ICollection<Category> CategoriesModified { get; set; } = new List<Category>();
    public virtual ICollection<Product> ProductsCreated { get; set; } = new List<Product>();
    public virtual ICollection<Product> ProductsModified { get; set; } = new List<Product>();
}