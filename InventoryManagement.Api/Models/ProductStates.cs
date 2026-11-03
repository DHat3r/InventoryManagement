namespace InventoryManagement.Api.Models;

// Un producto tiene tres dimensiones independientes (un producto puede estar
// aprobado y sin stock a la vez), por eso no se mezclan en un solo estado:
// - Aprobación (ApprovalStatus): ¿fue aprobado para la venta? Lo decide Admin.
// - Vigencia (Lifecycle): ¿se sigue vendiendo? Activo, retirado o descontinuado.
// - Disponibilidad: se calcula del stock (sin stock / stock bajo); no se guarda.

/// <summary>Aprobación del producto. Inventario crea en Pendiente; Admin aprueba (y define el precio) o rechaza.</summary>
public enum ProductApprovalStatus : byte
{
    Pendiente = 1,
    Aprobado = 2,
    Rechazado = 3
}

/// <summary>Vigencia del producto.</summary>
public enum ProductLifecycle : byte
{
    /// <summary>A la venta.</summary>
    Activo = 1,

    /// <summary>Sacado de la venta temporalmente (falla, lote dañado, recall). Se revierte con reactivar.</summary>
    Retirado = 2,

    /// <summary>Ya no se vende ni se compra (reemplaza al antiguo IsActive = false).</summary>
    Descontinuado = 3
}

/// <summary>Qué propone una solicitud de cambio de Inventario.</summary>
public enum ChangeRequestType : byte
{
    /// <summary>Cambiar datos de un producto aprobado (nunca el precio).</summary>
    Edicion = 1,

    /// <summary>Retirar el producto de la venta.</summary>
    Retiro = 2
}

/// <summary>Estado de una solicitud de cambio.</summary>
public enum ChangeRequestStatus : byte
{
    Pendiente = 1,
    Aprobada = 2,
    Rechazada = 3
}
