import type { ProductListItem } from '../api/types'

type StatusBadgeInfo = {
  label: string
  className: string
}

// Muestra un solo estado principal, el más importante para quien despacha: lo que impide vender va antes que el stock.
export default function ProductStatusBadge({ product }: { product: ProductListItem }) {
  const status = mainStatus(product)

  return (
    <>
      <span className={status.className}>{status.label}</span>
      {product.hasPendingEdit && (
        <>
          {' '}
          <span className="badge">Cambio en revisión</span>
        </>
      )}
    </>
  )
}

function mainStatus(product: ProductListItem): StatusBadgeInfo {
  if (product.approvalStatus === 'Pendiente') {
    return { label: 'Pendiente de aprobación', className: 'badge badge-warning' }
  }
  if (product.approvalStatus === 'Rechazado') {
    return { label: 'Rechazado', className: 'badge badge-danger' }
  }
  if (product.lifecycle === 'Retirado') {
    return { label: 'Retirado', className: 'badge badge-danger' }
  }
  if (product.isPossiblyUnavailable) {
    return { label: 'Posiblemente no disponible', className: 'badge badge-warning' }
  }
  if (product.lifecycle === 'Descontinuado') {
    return { label: 'Descontinuado', className: 'badge' }
  }
  if (product.isOutOfStock) {
    return { label: 'Sin stock', className: 'badge badge-danger' }
  }
  if (product.isLowStock) {
    return { label: 'Stock bajo', className: 'badge badge-warning' }
  }
  return { label: 'OK', className: 'badge badge-success' }
}
