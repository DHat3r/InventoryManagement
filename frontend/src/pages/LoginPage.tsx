import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'

export default function LoginPage() {
  const { user, login } = useAuth()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Con sesión iniciada (también tras login exitoso) se va a la app.
  if (user) {
    return <Navigate to="/productos" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await login(userName, password)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo conectar con el servidor')
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-page">
      <section className="card auth-card">
        <h1>InventoryManagement</h1>
        <p className="muted">Inicia sesión para continuar</p>

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="userName">Usuario</label>
            <input
              id="userName"
              className="input"
              autoComplete="username"
              value={userName}
              onChange={(event) => setUserName(event.target.value)}
              required
            />
          </div>

          <div className="field">
            <label htmlFor="password">Contraseña</label>
            <input
              id="password"
              type="password"
              className="input"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </div>

          {error && <div className="alert-error" role="alert">{error}</div>}

          <button type="submit" className="btn btn-block" disabled={isSubmitting}>
            {isSubmitting ? 'Ingresando...' : 'Ingresar'}
          </button>
        </form>
      </section>
    </div>
  )
}
