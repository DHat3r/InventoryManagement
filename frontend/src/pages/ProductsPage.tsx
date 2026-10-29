import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { ApiError, apiFetch } from '../api/client'
import type { PagedResult, ProductListItem } from '../api/types'
import { formatMoney } from '../utils/format'

const PAGE_SIZE = 20
const SEARCH_DEBOUNCE_MS = 300
const COLUMN_COUNT = 7

export default function ProductsPage() {
  const [searchText, setSearchText] = useState('')
  const [search, setSearch] = useState('')
  const [lowStockOnly, setLowStockOnly] = useState(false)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<ProductListItem> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Espera a que el usuario deje de escribir antes de consultar al servidor.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchText.trim())
      setPage(1)
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchText])

  useEffect(() => {
    // Si el usuario cambia los filtros antes de que llegue la respuesta, ignoramos la vieja.
    let cancelled = false
    setLoading(true)
    setError(null)

    apiFetch<PagedResult<ProductListItem>>(buildUrl(page, search, lowStockOnly))
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setResult(null)
          setError(errorMessage(err))
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [page, search, lowStockOnly])

  const products = result?.data ?? []

  return (
    <>
      <h1>Productos</h1>
      <p className="muted">Catálogo y niveles de stock</p>

      <div className="toolbar">
        <input
          className="input"
          type="search"
          placeholder="Buscar por nombre o SKU"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <label>
          <input
            type="checkbox"
            checked={lowStockOnly}
            onChange={(e) => {
              setLowStockOnly(e.target.checked)
              setPage(1)
            }}
          />{' '}
          Solo stock bajo
        </label>
        <span className="muted">{result ? `${result.totalCount} productos` : ''}</span>
      </div>

      {error && <div className="alert-error">{error}</div>}

      <div className="card">
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Nombre</th>
                <th>Categoría</th>
                <th className="num">Precio</th>
                <th className="num">Stock</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={COLUMN_COUNT} className="muted">
                    Cargando...
                  </td>
                </tr>
              )}
              {!loading && !error && products.length === 0 && (
                <tr>
                  <td colSpan={COLUMN_COUNT} className="muted">
                    No hay productos que coincidan
                  </td>
                </tr>
              )}
              {!loading &&
                products.map((product) => <ProductRow key={product.id} product={product} />)}
            </tbody>
          </table>
        </div>

        {result && (
          <div className="pagination">
            <button
              type="button"
              className="btn btn-secondary"
              disabled={!result.hasPreviousPage}
              onClick={() => setPage(page - 1)}
            >
              Anterior
            </button>
            <span className="muted">
              Página {page} de {Math.max(result.totalPages, 1)}
            </span>
            <button
              type="button"
              className="btn btn-secondary"
              disabled={!result.hasNextPage}
              onClick={() => setPage(page + 1)}
            >
              Siguiente
            </button>
          </div>
        )}
      </div>
    </>
  )
}

function ProductRow({ product }: { product: ProductListItem }) {
  return (
    <tr>
      <td>{product.sku ?? '—'}</td>
      <td>{product.name}</td>
      <td>{product.category.name}</td>
      <td className="num">{formatMoney(product.price)}</td>
      <td className="num">{product.stockQuantity}</td>
      <td>
        <StatusBadge product={product} />
      </td>
      <td>
        <Link to={`/productos/${product.id}/kardex`} className="btn btn-secondary">
          Ver kardex
        </Link>
      </td>
    </tr>
  )
}

function StatusBadge({ product }: { product: ProductListItem }) {
  if (!product.isActive) {
    return <span className="badge badge-warning">Inactivo</span>
  }
  if (product.isLowStock) {
    return <span className="badge badge-danger">Stock bajo</span>
  }
  return <span className="badge badge-success">OK</span>
}

function buildUrl(page: number, search: string, lowStockOnly: boolean): string {
  let url = `/api/products?page=${page}&pageSize=${PAGE_SIZE}&sortBy=name`
  if (search) {
    url += `&search=${encodeURIComponent(search)}`
  }
  if (lowStockOnly) {
    url += '&lowStockOnly=true'
  }
  return url
}

function errorMessage(err: unknown): string {
  return err instanceof ApiError ? err.message : 'No se pudieron cargar los productos'
}
