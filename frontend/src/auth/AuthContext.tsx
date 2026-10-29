import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { apiFetch, setOnSessionExpired } from '../api/client'
import { clearSession, getSession, saveSession } from '../api/session'
import type { LoginResponse, UserInfo } from '../api/types'

type AuthContextValue = {
  user: UserInfo | null
  login: (userName: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  // Al recargar la página se recupera el usuario guardado en localStorage.
  const [user, setUser] = useState<UserInfo | null>(() => getSession()?.user ?? null)

  useEffect(() => {
    setOnSessionExpired(() => setUser(null))
  }, [])

  async function login(userName: string, password: string) {
    const data = await apiFetch<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: { userName, password },
    })
    saveSession({
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      user: data.user,
    })
    setUser(data.user)
  }

  async function logout() {
    const session = getSession()
    if (session) {
      try {
        await apiFetch<void>('/api/auth/logout', {
          method: 'POST',
          body: { refreshToken: session.refreshToken },
        })
      } catch {
        // Si el servidor falla, igual cerramos la sesión local.
      }
    }
    clearSession()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider')
  }
  return context
}
