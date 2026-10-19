using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Api.Models;

/// <summary>
/// Movimiento de stock (Kardex) - trazabilidad completa para valoración contable
/// </summary>
public class StockMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public MovementType Type { get; set; }

    /// <summary>
    /// Cantidad absoluta (siempre > 0). El signo lo determina <see cref="Type"/>.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; } // Nº factura, orden, nota crédito, etc.

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Costo unitario al momento del movimiento (para valoración histórica PEPS/UEPS/Promedio)
    /// Requerido para Entrada y Ajuste+; opcional para Salida (usa costo promedio actual)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitCost { get; set; }

    /// <summary>Stock antes de aplicar el movimiento</summary>
    [Required]
    public int PreviousStock { get; set; }

    /// <summary>Stock después de aplicar el movimiento</summary>
    [Required]
    public int NewStock { get; set; }

    // User FK (Fase 4 - Auditoría)
    public int? UserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}