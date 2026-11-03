import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError, apiFetch } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { KardexSummary, MovementType, PagedResult, Product, StockMovement } from '../api/types'
import MovementForm from '../components/MovementForm'
import ProductStatusBadge from '../components/ProductStatusBadge'
import { formatDateTime, formatMoney } from '../utils/format'

const PAGE_SIZE = 10

const badgeClasses: Record<MovementType, string> = {
  Entrada: 'badge badge-success',
  Salida: 'badge badge-danger',
  Ajuste: 'badge badge-warning',
}

function fetchMovementsPage(productId: number, pageNumber: number) {
  return apiFetch<PagedResult<StockMovement>>(
    `/api/products/${productId}/kardex?page=${pageNumber}&pageSize=${PAGE_SIZE}`,
  )
}

function messageFrom(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Error inesperado al cargar el kardex'
}

export default function KardexPage() {
  const { id } = useParams()
  const { user } = useAuth()
  // El resumen valorizado (costo promedio, valor del stock) es contable: solo Admin y Contador
  const canSeeValuation = user?.role === 'Admin' || user?.role === 'Contador'
  // Contador solo lee: la API rechaza sus movimientos, así que no se le muestra el formulario
  const canRegisterMovements = user?.role === 'Admin' || user?.role === 'Inventario'
  // Editar y retirar son acciones de catálogo: las comparten Admin e Inventario
  const canEditCatalog = user?.role === 'Admin' || user?.role === 'Inventario'
  const productId = Number(id)
  const isValidId = Number.isInteger(productId)

  const [product, setProduct] = useState<Product | null>(null)
  const [summary, setSummary] = useState<KardexSummary | null>(null)
  const [movements, setMovements] = useState<PagedResult<StockMovement> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    try {
      const [productData, summaryData, movementsData] = await Promise.all([
        apiFetch<Product>(`/api/products/${productId}`),
        canSeeValuation ? apiFetch<KardexSummary>(`/api/products/${productId}/kardex/summary`) : Promise.resolve(null),
        fetchMovementsPage(productId, 1),
      ])
      setProduct(productData)
      setSummary(summaryData)
      setMovements(movementsData)
      setError(null)
    } catch (err) {
      setError(messageFrom(err))
    }
  }, [productId, canSeeValuation])

  useEffect(() => {
    if (isValidId) {
      void reload()
    }
  }, [isValidId, reload])

  async function goToPage(pageNumber: number) {
    try {
      setMovements(await fetchMovementsPage(productId, pageNumber))
      setError(null)
    } catch (err) {
      setError(messageFrom(err))
    }
  }

  async function runAction(action: () => Promise<unknown>, successMessage: string) {
    setActionMessage(null)
    setActionError(null)
    try {
      await action()
      setActionMessage(successMessage)
      await reload()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'No se pudo completar la acción')
    }
  }

  async function withdraw() {
    const reason = window.prompt('Motivo del retiro (por ejemplo, lote con falla)')
    if (!reason) return
    await runAction(
      () => apiFetch(`/api/products/${productId}/withdraw`, { method: 'POST', body: { reason } }),
      user?.role === 'Admin'
        ? 'Producto retirado de la venta'
        : 'Retiro solicitado: el producto queda sin salidas hasta que Admin decida',
    )
  }

  async function reactivate() {
    await runAction(
      () => apiFetch(`/api/products/${productId}/reactivate`, { method: 'POST' }),
      'Producto de nuevo a la venta',
    )
  }

  if (!isValidId) {
    return <div className="alert-error">Producto no válido</div>
  }
  if (!product || !movements || (canSeeValuation && !summary)) {
    return error ? <div className="alert-error">{error}</div> : <p>Cargando...</p>
  }

  return (
    <>
      <Link to="/productos" className="muted">
        ← Volver a productos
      </Link>
      <h1>Kardex: {product.name}</h1>
      <p className="muted">
        SKU {product.sku ?? '—'} · Categoría {product.category.name} <ProductStatusBadge product={product} />
      </p>

      {product.rejectionReason && (
        <div className="alert-error">Rechazado por Admin: {product.rejectionReason}</div>
      )}

      {canEditCatalog && (
        <div className="toolbar">
          <Link to={`/productos/${productId}/editar`} className="btn btn-secondary">
            Editar
          </Link>
          {product.lifecycle === 'Activo' && !product.isPossiblyUnavailable && (
            <button type="button" className="btn btn-secondary" onClick={withdraw}>
              Retirar de la venta
            </button>
          )}
          {user?.role === 'Admin' && product.lifecycle === 'Retirado' && (
            <button type="button" className="btn" onClick={reactivate}>
              Reactivar
            </button>
          )}
        </div>
      )}

      {actionMessage && <div className="alert-success">{actionMessage}</div>}
      {actionError && <div className="alert-error">{actionError}</div>}

      {error && <div className="alert-error">{error}</div>}

      <div className="grid-stats" style={{ marginTop: 16 }}>
        <div className="stat">
          <div className="stat-label">Stock actual</div>
          <div className="stat-value">{product.stockQuantity}</div>
        </div>
        {summary && (
          <>
            <div className="stat">
              <div className="stat-label">Costo promedio</div>
              <div className="stat-value">{formatMoney(summary.averageCost)}</div>
            </div>
            <div className="stat">
              <div className="stat-label">Valor del stock</div>
              <div className="stat-value">{formatMoney(summary.stockValue)}</div>
            </div>
            <div className="stat">
              <div className="stat-label">Total entradas</div>
              <div className="stat-value">{summary.totalEntradas}</div>
            </div>
            <div className="stat">
              <div className="stat-label">Total salidas</div>
              <div className="stat-value">{summary.totalSalidas}</div>
            </div>
            <div className="stat">
              <div className="stat-label">Total ajustes</div>
              <div className="stat-value">{summary.totalAjustes}</div>
            </div>
          </>
        )}
      </div>

      {canRegisterMovements && (
        <div className="card" style={{ marginTop: 16 }}>
          <MovementForm productId={productId} onCreated={reload} />
        </div>
      )}

      <div className="card" style={{ marginTop: 16 }}>
        {movements.data.length === 0 ? (
          <p className="muted">Este producto aún no tiene movimientos</p>
        ) : (
          <>
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Tipo</th>
                    <th>Cantidad</th>
                    <th>Stock anterior</th>
                    <th>Stock nuevo</th>
                    <th>Costo unit.</th>
                    <th>Motivo</th>
                    <th>Referencia</th>
                  </tr>
                </thead>
                <tbody>
                  {movements.data.map((movement) => (
                    <tr key={movement.id}>
                      <td>{formatDateTime(movement.createdAt)}</td>
                      <td>
                        <span className={badgeClasses[movement.type]}>{movement.type}</span>
                      </td>
                      <td className="num">{movement.quantity}</td>
                      <td className="num">{movement.previousStock}</td>
                      <td className="num">{movement.newStock}</td>
                      <td className="num">
                        {movement.unitCost === null ? '—' : formatMoney(movement.unitCost)}
                      </td>
                      <td>{movement.reason ?? '—'}</td>
                      <td>{movement.reference ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="pagination">
              <button
                type="button"
                className="btn btn-secondary"
                disabled={!movements.hasPreviousPage}
                onClick={() => goToPage(movements.page - 1)}
              >
                Anterior
              </button>
              <span>
                Página {movements.page} de {Math.max(movements.totalPages, 1)}
              </span>
              <button
                type="button"
                className="btn btn-secondary"
                disabled={!movements.hasNextPage}
                onClick={() => goToPage(movements.page + 1)}
              >
                Siguiente
              </button>
            </div>
          </>
        )}
      </div>
    </>
  )
}
