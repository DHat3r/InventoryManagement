import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError, apiFetch } from '../api/client'
import type { Category, Product, ProductChangeRequest, ProductInput } from '../api/types'
import { useAuth } from '../auth/AuthContext'

type FormValues = {
  name: string
  sku: string
  categoryId: string
  cost: string
  price: string
  minStockLevel: string
  unitOfMeasure: string
  description: string
}

const emptyForm: FormValues = {
  name: '',
  sku: '',
  categoryId: '',
  cost: '',
  price: '',
  minStockLevel: '10',
  unitOfMeasure: 'UN',
  description: '',
}

// Sirve para crear (sin id en la ruta) y para editar (con id). Ambos usan el mismo formulario.
export default function ProductFormPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const isEdit = id !== undefined
  const productId = Number(id)
  // El precio de venta lo define Admin: a Inventario no se le muestra ni se le envía
  const isAdmin = user?.role === 'Admin'

  const [categories, setCategories] = useState<Category[]>([])
  const [product, setProduct] = useState<Product | null>(null)
  const [form, setForm] = useState<FormValues>(emptyForm)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    // Si la página cambia antes de que llegue la respuesta, ignoramos la vieja.
    let cancelled = false

    Promise.all([
      apiFetch<Category[]>('/api/categories'),
      isEdit ? apiFetch<Product>(`/api/products/${productId}`) : Promise.resolve(null),
    ])
      .then(([categoryData, productData]) => {
        if (cancelled) return
        setCategories(categoryData)
        if (productData) {
          setProduct(productData)
          setForm(toFormValues(productData))
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(errorMessage(err))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [isEdit, productId])

  function updateField(field: keyof FormValues, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function buildBody(): ProductInput {
    const body: ProductInput = {
      name: form.name,
      cost: Number(form.cost),
      categoryId: Number(form.categoryId),
      minStockLevel: Number(form.minStockLevel),
      // Al crear el stock es 0; al editar se conserva, porque el stock solo cambia con movimientos
      stockQuantity: product?.stockQuantity ?? 0,
    }
    if (form.sku) body.sku = form.sku
    if (form.description) body.description = form.description
    if (form.unitOfMeasure) body.unitOfMeasure = form.unitOfMeasure
    if (isAdmin && form.price) body.price = Number(form.price)
    return body
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    setMessage(null)
    try {
      if (isEdit) {
        const result = await apiFetch<Product | ProductChangeRequest>(`/api/products/${productId}`, {
          method: 'PUT',
          body: buildBody(),
        })
        // Inventario sobre un producto aprobado recibe una solicitud (202) en vez del producto (200)
        setMessage('type' in result ? 'Cambio enviado a Admin para aprobación' : 'Producto actualizado')
      } else {
        await apiFetch<Product>('/api/products', { method: 'POST', body: buildBody() })
        setMessage(
          user?.role === 'Inventario'
            ? 'Producto creado. Queda pendiente hasta que Admin lo apruebe y defina el precio.'
            : 'Producto creado',
        )
      }
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <p>Cargando...</p>
  }
  if (isEdit && !product) {
    return <div className="alert-error">{error}</div>
  }

  return (
    <>
      <Link to="/productos" className="muted">
        ← Volver a productos
      </Link>
      <h1>{isEdit ? 'Editar producto' : 'Nuevo producto'}</h1>

      <form className="card" onSubmit={handleSubmit}>
        {!isAdmin && <p className="muted">El precio de venta lo define Admin al aprobar el producto.</p>}
        {!isAdmin && isEdit && product?.approvalStatus === 'Aprobado' && (
          <p className="muted">Los cambios quedarán pendientes hasta que Admin los apruebe.</p>
        )}

        <div className="field">
          <label htmlFor="product-name">Nombre</label>
          <input
            id="product-name"
            className="input"
            type="text"
            required
            maxLength={200}
            value={form.name}
            onChange={(event) => updateField('name', event.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="product-sku">SKU</label>
          <input
            id="product-sku"
            className="input"
            type="text"
            pattern="[A-Z0-9-]+"
            title="Mayúsculas, números y guiones"
            value={form.sku}
            onChange={(event) => updateField('sku', event.target.value.toUpperCase())}
          />
        </div>

        <div className="field">
          <label htmlFor="product-category">Categoría</label>
          <select
            id="product-category"
            className="select"
            required
            value={form.categoryId}
            onChange={(event) => updateField('categoryId', event.target.value)}
          >
            <option value="">Selecciona una categoría</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label htmlFor="product-cost">Costo</label>
          <input
            id="product-cost"
            className="input"
            type="number"
            step={0.01}
            min={0}
            required
            value={form.cost}
            onChange={(event) => updateField('cost', event.target.value)}
          />
        </div>

        {isAdmin && (
          <div className="field">
            <label htmlFor="product-price">Precio de venta</label>
            <input
              id="product-price"
              className="input"
              type="number"
              step={0.01}
              min={0.01}
              required={!isEdit}
              value={form.price}
              onChange={(event) => updateField('price', event.target.value)}
            />
          </div>
        )}

        <div className="field">
          <label htmlFor="product-min-stock">Stock mínimo</label>
          <input
            id="product-min-stock"
            className="input"
            type="number"
            min={0}
            value={form.minStockLevel}
            onChange={(event) => updateField('minStockLevel', event.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="product-unit">Unidad de medida</label>
          <input
            id="product-unit"
            className="input"
            type="text"
            value={form.unitOfMeasure}
            onChange={(event) => updateField('unitOfMeasure', event.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="product-description">Descripción</label>
          <textarea
            id="product-description"
            className="input"
            maxLength={1000}
            value={form.description}
            onChange={(event) => updateField('description', event.target.value)}
          />
        </div>

        <div className="toolbar">
          <button type="submit" className="btn" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to="/productos" className="btn btn-secondary">
            Cancelar
          </Link>
        </div>
      </form>

      {error && <div className="alert-error">{error}</div>}
      {message && (
        <>
          <div className="alert-success">{message}</div>
          <Link to="/productos">Volver a productos</Link>
        </>
      )}
    </>
  )
}

function toFormValues(product: Product): FormValues {
  return {
    name: product.name,
    sku: product.sku ?? '',
    categoryId: String(product.category.id),
    cost: String(product.cost),
    price: product.price === null ? '' : String(product.price),
    minStockLevel: String(product.minStockLevel),
    unitOfMeasure: product.unitOfMeasure ?? '',
    description: product.description ?? '',
  }
}

function errorMessage(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Error inesperado al cargar o guardar el producto'
}
