namespace InventoryManagement.Api.Models;

/// <summary>
/// Tipo de movimiento de stock (convención contable: Quantity siempre positivo)
/// </summary>
public enum MovementType : byte
{
    /// <summary>Entrada: compra, devolución proveedor, ajuste positivo</summary>
    Entrada = 1,

    /// <summary>Salida: venta, consumo interno, merma, ajuste negativo</summary>
    Salida = 2,

    /// <summary>Ajuste: inventario físico, recuento (puede ser + o - según Quantity)</summary>
    Ajuste = 3
}