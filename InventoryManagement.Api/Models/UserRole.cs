namespace InventoryManagement.Api.Models;

/// <summary>
/// Roles del sistema (ordenados por nivel de privilegio)
/// </summary>
public enum UserRole : byte
{
    /// <summary>Acceso total: gestión usuarios, configuración, todo</summary>
    Admin = 1,

    /// <summary>Gestión de stock: movimientos, kardex, productos, categorías</summary>
    Inventario = 2,

    /// <summary>Reportes y valoración: kardex summary, costos, márgenes</summary>
    Contador = 3,

    /// <summary>Solo entradas: crear órdenes de compra, registrar entradas de stock</summary>
    Comprador = 4
}