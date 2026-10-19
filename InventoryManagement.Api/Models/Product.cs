using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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