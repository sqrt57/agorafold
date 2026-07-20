import { apiFetch } from './client'

export function getAll() {
  return apiFetch('/api/categories')
}
