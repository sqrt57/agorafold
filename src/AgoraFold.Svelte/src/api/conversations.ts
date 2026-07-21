import { apiFetch } from './client'
import type { ConversationSummary, ConversationThread } from './types'

export function getInbox(): Promise<ConversationSummary[]> {
  return apiFetch<ConversationSummary[]>('/api/conversations')
}

export function getThread(id: number | string): Promise<ConversationThread> {
  return apiFetch<ConversationThread>(`/api/conversations/${id}`)
}

export function start(listingId: number): Promise<ConversationThread> {
  return apiFetch<ConversationThread>('/api/conversations', { method: 'POST', body: { listingId } })
}

export function reply(id: number | string, body: string): Promise<ConversationThread> {
  return apiFetch<ConversationThread>(`/api/conversations/${id}/replies`, { method: 'POST', body: { body } })
}
