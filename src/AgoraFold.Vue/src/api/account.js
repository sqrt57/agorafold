import { apiFetch } from './client'

export function register(email, displayName, password) {
  return apiFetch('/api/account/register', { method: 'POST', body: { email, displayName, password } })
}

export function login(email, password, rememberMe) {
  return apiFetch('/api/account/login', { method: 'POST', body: { email, password, rememberMe } })
}

export function logout() {
  return apiFetch('/api/account/logout', { method: 'POST' })
}

export async function me() {
  try {
    return await apiFetch('/api/account/me')
  } catch (err) {
    if (err.status === 401) {
      return null
    }
    throw err
  }
}
