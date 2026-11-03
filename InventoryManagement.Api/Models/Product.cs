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

    /// <summary>Precio de venta. Null mientras el producto espera aprobación: lo define Admin al aprobar.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; }

    [Required]
    public int StockQuantity { get; set; } = 0;

    public int MinStockLevel { get; set; } = 10;

    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; } = "UN";

    // Ver ProductStates.cs: aprobación y vigencia son dimensiones separadas.
    // Por defecto Pendiente: quien crea un producto aprobado lo tiene que decir explícitamente.
    public ProductApprovalStatus ApprovalStatus { get; set; } = ProductApprovalStatus.Pendiente;
    public ProductLifecycle Lifecycle { get; set; } = ProductLifecycle.Activo;

    // Auditoría de la aprobación
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Foreign Key
    public int CategoryId { get; set; }

    // Audit columns (Fase 4)
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }

    // Navigation property
    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;

    // Audit navigation
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifiedBy))]
    public virtual User? ModifiedByUser { get; set; }

    public virtual ICollection<ProductChangeRequest> ChangeRequests { get; set; } = new List<ProductChangeRequest>();
}