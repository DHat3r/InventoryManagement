using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Api.Models;

/// <summary>
/// Cambio que Inventario propone sobre un producto aprobado y que Admin aprueba o rechaza.
/// - Edicion: guarda los valores propuestos; al aprobar se copian al producto.
/// - Retiro: mientras está pendiente el producto queda "posiblemente no disponible" (sin salidas).
/// </summary>
public class ProductChangeRequest
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ChangeRequestType Type { get; set; }

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pendiente;

    // Valores propuestos (solo para Edicion; en Retiro quedan en null)
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? SKU { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; }

    public int? CategoryId { get; set; }

    public int? MinStockLevel { get; set; }

    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; }

    /// <summary>Motivo del retiro o comentario de quien solicita.</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    // Auditoría: quién pidió y quién revisó
    public int RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewComment { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}
