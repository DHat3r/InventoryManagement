import { useCallback, useEffect, useState } from 'react'
import { ApiError, apiFetch } from '../api/client'
import type {
  Category,
  PagedResult,
  Product,
  ProductChangeRequest,
  ProductListItem,
} from '../api/types'
import { formatDateTime, formatMoney } from '../utils/format'

type FieldChange = { label: string; before: string; after: string }

export default function ApprovalsPage() {
  const [pendingProducts, setPendingProducts] = useState<Product[]>([])
  const [requests, setRequests] = useState<ProductChangeRequest[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [prices, setPrices] = useState<Record<number, string>>({})
  // Claves de las acciones en curso ("product-3", "request-7"): solo se bloquea su fila o solicitud
  const [busyKeys, setBusyKeys] = useState<Set<string>>(() => new Set())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const reload = useCallback(async () => {
    const [pendingPage, requestList, categoryList] = await Promise.all([
      apiFetch<PagedResult<ProductListItem>>('/api/products?approvalStatus=Pendiente&pageSize=100'),
      apiFetch<ProductChangeRequest[]>('/api/product-change-requests'),
      apiFetch<Category[]>('/api/categories'),
    ])
    // Admin necesita el costo para fijar el precio, por eso pedimos el detalle de cada pendiente
    const details = await Promise.all(
      pendingPage.data.map((product) => apiFetch<Product>(`/api/products/${product.id}`)),
    )
    setPendingProducts(details)
    setRequests(requestList)
    setCategories(categoryList)
  }, [])

  useEffect(() => {
    reload()
      .catch((err: unknown) => setError(errorMessage(err)))
      .finally(() => setLoading(false))
  }, [reload])

  function setBusy(key: string, busy: boolean) {
    setBusyKeys((keys) => {
      const next = new Set(keys)
      if (busy) {
        next.add(key)
      } else {
        next.delete(key)
      }
      return next
    })
  }

  async function runAction(key: string, action: () => Promise<unknown>, successMessage: string) {
    setBusy(key, true)
    setError(null)
    setSuccess(null)
    try {
      await action()
      await reload()
      setSuccess(successMessage)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(key, false)
    }
  }

  async function approveProduct(product: Product) {
    const value = prices[product.id] ?? ''
    const price = Number(value)
    if (!value || price <= 0) {
      setError('Indica el precio de venta')
      setSuccess(null)
      return
    }
    await runAction(
      `product-${product.id}`,
      () =>
        apiFetch(`/api/products/${product.id}/approve`, {
          method: 'POST',
          body: { price },
        }),
      'Producto aprobado',
    )
  }

  async function rejectProduct(product: Product) {
    const reason = window.prompt('Motivo del rechazo')
    if (!reason) return
    await runAction(
      `product-${product.id}`,
      () =>
        apiFetch(`/api/products/${product.id}/reject`, {
          method: 'POST',
          body: { reason },
        }),
      'Producto rechazado',
    )
  }

  async function approveRequest(request: ProductChangeRequest) {
    await runAction(
      `request-${request.id}`,
      () =>
        apiFetch(`/api/product-change-requests/${request.id}/approve`, {
          method: 'POST',
          body: {},
        }),
      'Solicitud aprobada',
    )
  }

  async function rejectRequest(request: ProductChangeRequest) {
    const comment = window.prompt('Comentario para Inventario (opcional)')
    if (comment === null) return
    await runAction(
      `request-${request.id}`,
      () =>
        apiFetch(`/api/product-change-requests/${request.id}/reject`, {
          method: 'POST',
          body: comment ? { comment } : {},
        }),
      'Solicitud rechazada',
    )
  }

  return (
    <>
      <h1>Aprobaciones</h1>
      <p className="muted">Productos nuevos y cambios que propuso Inventario</p>

      {error && <div className="alert-error">{error}</div>}
      {success && <div className="alert-success">{success}</div>}

      <section className="card">
        <h2>Productos pendientes</h2>
        {loading && <p className="muted">Cargando...</p>}
        {!loading && pendingProducts.length === 0 && (
          <p className="muted">No hay productos pendientes</p>
        )}
        {!loading && pendingProducts.length > 0 && (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Nombre</th>
                  <th>Categoría</th>
                  <th className="num">Costo</th>
                  <th className="num">Stock</th>
                  <th>Precio de venta</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {pendingProducts.map((product) => {
                  const busy = busyKeys.has(`product-${product.id}`)
                  return (
                    <tr key={product.id}>
                      <td>{product.sku ?? '—'}</td>
                      <td>{product.name}</td>
                      <td>{product.category.name}</td>
                      <td className="num">{formatMoney(product.cost)}</td>
                      <td className="num">{product.stockQuantity}</td>
                      <td>
                        <input
                          className="input"
                          type="number"
                          step="0.01"
                          min="0.01"
                          value={prices[product.id] ?? ''}
                          onChange={(e) =>
                            setPrices((current) => ({ ...current, [product.id]: e.target.value }))
                          }
                        />
                      </td>
                      <td>
                        <button
                          type="button"
                          className="btn"
                          disabled={busy}
                          onClick={() => approveProduct(product)}
                        >
                          Aprobar
                        </button>{' '}
                        <button
                          type="button"
                          className="btn btn-secondary"
                          disabled={busy}
                          onClick={() => rejectProduct(product)}
                        >
                          Rechazar
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card">
        <h2>Solicitudes de Inventario</h2>
        {loading && <p className="muted">Cargando...</p>}
        {!loading && requests.length === 0 && (
          <p className="muted">No hay solicitudes pendientes</p>
        )}
        {!loading &&
          requests.map((request) => {
            const busy = busyKeys.has(`request-${request.id}`)
            return (
              <div key={request.id} className="card">
                <h3>
                  {request.productName}{' '}
                  {request.type === 'Edicion' ? (
                    <span className="badge badge-warning">Edición</span>
                  ) : (
                    <span className="badge badge-danger">Retiro</span>
                  )}
                </h3>
                <p className="muted">{formatDateTime(request.requestedAt)}</p>

                <ChangeRequestDetail request={request} categories={categories} />

                <button
                  type="button"
                  className="btn"
                  disabled={busy}
                  onClick={() => approveRequest(request)}
                >
                  Aprobar
                </button>{' '}
                <button
                  type="button"
                  className="btn btn-secondary"
                  disabled={busy}
                  onClick={() => rejectRequest(request)}
                >
                  Rechazar
                </button>
              </div>
            )
          })}
      </section>
    </>
  )
}

function ChangeRequestDetail({
  request,
  categories,
}: {
  request: ProductChangeRequest
  categories: Category[]
}) {
  if (request.type === 'Retiro') {
    return (
      <>
        <p>Retirar de la venta. Motivo: {request.reason ?? '—'}</p>
        <p className="muted">
          Mientras esperas, el producto ya está sin salidas (posiblemente no disponible).
        </p>
      </>
    )
  }

  const changes = buildFieldChanges(request, categories)
  if (changes.length === 0) {
    return <p className="muted">Sin cambios respecto al producto actual</p>
  }

  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Campo</th>
            <th>Actual</th>
            <th>Propuesto</th>
          </tr>
        </thead>
        <tbody>
          {changes.map((change) => (
            <tr key={change.label}>
              <td>{change.label}</td>
              <td>{change.before}</td>
              <td>{change.after}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// En una Edición el propuesto es el valor completo: null y '' significan "vacío" y se comparan como iguales
function buildFieldChanges(request: ProductChangeRequest, categories: Category[]): FieldChange[] {
  const { current } = request
  const candidates: Array<[string, string | null, string | null]> = [
    ['Nombre', current.name, request.name],
    ['SKU', current.sku, request.sku],
    ['Descripción', current.description, request.description],
    [
      'Costo',
      formatMoney(current.cost),
      request.cost === null ? null : formatMoney(request.cost),
    ],
    [
      'Categoría',
      categoryName(current.categoryId, categories),
      request.categoryId === null ? null : categoryName(request.categoryId, categories),
    ],
    [
      'Stock mínimo',
      String(current.minStockLevel),
      request.minStockLevel === null ? null : String(request.minStockLevel),
    ],
    ['Unidad', current.unitOfMeasure, request.unitOfMeasure],
  ]

  return candidates
    .filter(([, before, after]) => (before ?? '') !== (after ?? ''))
    .map(([label, before, after]) => ({
      label,
      before: before || '—',
      after: after || '—',
    }))
}

function categoryName(categoryId: number, categories: Category[]): string {
  return categories.find((category) => category.id === categoryId)?.name ?? String(categoryId)
}

function errorMessage(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Ocurrió un error inesperado'
}
