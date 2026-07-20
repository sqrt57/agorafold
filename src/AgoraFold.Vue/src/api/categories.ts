import { apiFetch } from './client'
import type { Category } from './types'

export function getAll(): Promise<Category[]> {
  return apiFetch<Category[]>('/api/categories')
}
