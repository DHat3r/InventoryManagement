import type { UserInfo } from './types'

export type Session = {
  accessToken: string
  refreshToken: string
  user: UserInfo
}

const STORAGE_KEY = 'im.session'

export function getSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as Session) : null
  } catch {
    // Storage no disponible o JSON corrupto: se trata como sin sesión.
    return null
  }
}

export function saveSession(session: Session): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    // Storage no disponible: la sesión sólo vive en memoria.
  }
}

export function updateTokens(accessToken: string, refreshToken: string): void {
  const session = getSession()
  if (session) {
    saveSession({ ...session, accessToken, refreshToken })
  }
}

export function clearSession(): void {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Storage no disponible: nada que limpiar.
  }
}
