import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError, apiFetch } from '../api/client'
import type { KardexSummary, MovementType, PagedResult, Product, StockMovement } from '../api/types'
import MovementForm from '../components/MovementForm'
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
  const productId = Number(id)
  const isValidId = Number.isInteger(productId)

  const [product, setProduct] = useState<Product | null>(null)
  const [summary, setSummary] = useState<KardexSummary | null>(null)
  const [movements, setMovements] = useState<PagedResult<StockMovement> | null>(null)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    try {
      const [productData, summaryData, movementsData] = await Promise.all([
        apiFetch<Product>(`/api/products/${productId}`),
        apiFetch<KardexSummary>(`/api/products/${productId}/kardex/summary`),
        fetchMovementsPage(productId, 1),
      ])
      setProduct(productData)
      setSummary(summaryData)
      setMovements(movementsData)
      setError(null)
    } catch (err) {
      setError(messageFrom(err))
    }
  }, [productId])

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

  if (!isValidId) {
    return <div className="alert-error">Producto no válido</div>
  }
  if (!product || !summary || !movements) {
    return error ? <div className="alert-error">{error}</div> : <p>Cargando...</p>
  }

  return (
    <>
      <Link to="/productos" className="muted">
        ← Volver a productos
      </Link>
      <h1>Kardex: {product.name}</h1>
      <p className="muted">
        SKU {product.sku ?? '—'} · Categoría {product.category.name}
      </p>

      {error && <div className="alert-error">{error}</div>}

      <div className="grid-stats" style={{ marginTop: 16 }}>
        <div className="stat">
          <div className="stat-label">Stock actual</div>
          <div className="stat-value">{summary.currentStock}</div>
        </div>
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
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <MovementForm productId={productId} onCreated={reload} />
      </div>

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
