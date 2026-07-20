import { apiFetch, ApiError } from './client'
import type { AuthUser } from './types'

export function register(email: string, displayName: string, password: string): Promise<AuthUser> {
  return apiFetch<AuthUser>('/api/account/register', { method: 'POST', body: { email, displayName, password } })
}

export function login(email: string, password: string, rememberMe: boolean): Promise<AuthUser> {
  return apiFetch<AuthUser>('/api/account/login', { method: 'POST', body: { email, password, rememberMe } })
}

export function logout(): Promise<null> {
  return apiFetch<null>('/api/account/logout', { method: 'POST' })
}

export async function me(): Promise<AuthUser | null> {
  try {
    return await apiFetch<AuthUser>('/api/account/me')
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      return null
    }
    throw err
  }
}
