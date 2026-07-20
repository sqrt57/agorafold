import { apiFetch } from './client'

export function getInbox() {
  return apiFetch('/api/conversations')
}

export function getThread(id) {
  return apiFetch(`/api/conversations/${id}`)
}

export function start(listingId) {
  return apiFetch('/api/conversations', { method: 'POST', body: { listingId } })
}

export function reply(id, body) {
  return apiFetch(`/api/conversations/${id}/replies`, { method: 'POST', body: { body } })
}
