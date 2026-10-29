import { clearSession, getSession, updateTokens } from './session'
import type { ApiResponse, LoginResponse } from './types'

type RefreshData = Omit<LoginResponse, 'user'>

type RequestOptions = {
  method?: string
  body?: unknown
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

const GENERIC_MESSAGES: Record<number, string> = {
  401: 'Usuario o contraseña incorrectos',
  403: 'No tienes permiso para esta acción',
  429: 'Demasiadas peticiones, espera un momento',
  502: 'No se pudo conectar con la API',
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

let onSessionExpired: (() => void) | undefined
// Los refresh tokens rotan: si dos peticiones refrescan a la vez, la segunda
// usaría un token ya revocado. Por eso todas comparten la misma promesa.
let refreshing: Promise<boolean> | null = null

export function setOnSessionExpired(callback: () => void): void {
  onSessionExpired = callback
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await send(path, options)
  const isAuthRoute = path.startsWith('/api/auth/')

  if (response.status !== 401 || isAuthRoute) {
    return readResult<T>(response)
  }

  const refreshed = await refreshTokens()
  if (!refreshed) {
    clearSession()
    onSessionExpired?.()
    return readResult<T>(response)
  }

  return readResult<T>(await send(path, options))
}

function send(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = {}
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  const session = getSession()
  if (session) {
    headers.Authorization = `Bearer ${session.accessToken}`
  }

  return fetch(path, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  })
}

function refreshTokens(): Promise<boolean> {
  if (!refreshing) {
    refreshing = doRefresh().finally(() => {
      refreshing = null
    })
  }
  return refreshing
}

async function doRefresh(): Promise<boolean> {
  const session = getSession()
  if (!session) {
    return false
  }

  const response = await fetch('/api/auth/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken: session.refreshToken }),
  })
  if (!response.ok) {
    return false
  }

  const json = (await response.json()) as ApiResponse<RefreshData>
  updateTokens(json.data.accessToken, json.data.refreshToken)
  return true
}

async function readResult<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, await errorMessage(response))
  }
  if (response.status === 204) {
    return undefined as T
  }

  const json = (await response.json()) as ApiResponse<T>
  return json.data
}

async function errorMessage(response: Response): Promise<string> {
  // 401 y 403 llegan con cuerpo vacío: response.json() falla y se usa null.
  const problem = (await response.json().catch(() => null)) as ProblemDetails | null

  if (problem?.detail) {
    return problem.detail
  }
  const validationMessages = Object.values(problem?.errors ?? {}).flat()
  if (validationMessages.length > 0) {
    return validationMessages.join(' ')
  }
  if (problem?.title) {
    return problem.title
  }
  return GENERIC_MESSAGES[response.status] ?? 'Error inesperado del servidor'
}
