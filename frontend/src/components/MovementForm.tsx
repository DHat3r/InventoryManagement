import { useState, type FormEvent } from 'react'
import { ApiError, apiFetch } from '../api/client'
import type { CreateMovement, MovementType } from '../api/types'

type AdjustmentDirection = 'Sumar' | 'Restar'

type MovementFormProps = {
  productId: number
  onCreated: () => void
}

// Regla del backend para Ajustes: si se envía unitCost, el ajuste SUMA stock;
// si se omite, RESTA stock. Por eso "Sumar" exige costo y "Restar" nunca lo envía.
export default function MovementForm({ productId, onCreated }: MovementFormProps) {
  const [type, setType] = useState<MovementType>('Entrada')
  const [direction, setDirection] = useState<AdjustmentDirection>('Sumar')
  const [quantity, setQuantity] = useState('')
  const [unitCost, setUnitCost] = useState('')
  const [reason, setReason] = useState('')
  const [reference, setReference] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const isAddingAdjustment = type === 'Ajuste' && direction === 'Sumar'
  const showsUnitCost = type === 'Entrada' || isAddingAdjustment

  function buildBody(): CreateMovement {
    const body: CreateMovement = { type, quantity: Number(quantity) }
    if (reason.trim() !== '') {
      body.reason = reason.trim()
    }
    if (reference.trim() !== '') {
      body.reference = reference.trim()
    }
    if (showsUnitCost && unitCost !== '') {
      body.unitCost = Number(unitCost)
    }
    return body
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    setSuccess(false)
    try {
      await apiFetch(`/api/products/${productId}/movements`, { method: 'POST', body: buildBody() })
      setSuccess(true)
      setQuantity('')
      setUnitCost('')
      setReason('')
      setReference('')
      onCreated()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Error inesperado al registrar el movimiento')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <h2>Registrar movimiento</h2>
      <form className="toolbar" onSubmit={handleSubmit}>
        <div className="field">
          <label htmlFor="movement-type">Tipo</label>
          <select
            id="movement-type"
            className="select"
            value={type}
            onChange={(event) => setType(event.target.value as MovementType)}
          >
            <option value="Entrada">Entrada</option>
            <option value="Salida">Salida</option>
            <option value="Ajuste">Ajuste</option>
          </select>
        </div>

        {type === 'Ajuste' && (
          <div className="field">
            <label htmlFor="movement-direction">Dirección</label>
            <select
              id="movement-direction"
              className="select"
              value={direction}
              onChange={(event) => setDirection(event.target.value as AdjustmentDirection)}
            >
              <option value="Sumar">Sumar al stock</option>
              <option value="Restar">Restar del stock</option>
            </select>
          </div>
        )}

        <div className="field">
          <label htmlFor="movement-quantity">Cantidad</label>
          <input
            id="movement-quantity"
            className="input"
            type="number"
            min={1}
            required
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>

        {showsUnitCost && (
          <div className="field">
            <label htmlFor="movement-unit-cost">Costo unitario</label>
            <input
              id="movement-unit-cost"
              className="input"
              type="number"
              step={0.01}
              min={0}
              required={isAddingAdjustment}
              value={unitCost}
              onChange={(event) => setUnitCost(event.target.value)}
            />
          </div>
        )}

        <div className="field">
          <label htmlFor="movement-reason">Motivo</label>
          <input
            id="movement-reason"
            className="input"
            type="text"
            maxLength={200}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="movement-reference">Referencia</label>
          <input
            id="movement-reference"
            className="input"
            type="text"
            maxLength={100}
            placeholder="Ej: factura 123"
            value={reference}
            onChange={(event) => setReference(event.target.value)}
          />
        </div>

        <button type="submit" className="btn" disabled={submitting}>
          {submitting ? 'Registrando...' : 'Registrar'}
        </button>
      </form>

      {error && <div className="alert-error">{error}</div>}
      {success && <div className="alert-success">Movimiento registrado</div>}
    </>
  )
}
