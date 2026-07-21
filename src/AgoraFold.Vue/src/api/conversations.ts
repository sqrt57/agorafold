import { apiFetch, webSocketUrl } from './client'
import type { ConversationSummary, ConversationThread } from './types'

export interface ConversationWebSocketMessage {
  senderId: string
  senderDisplayName: string
  body: string
  sentAt: string
}

export interface ConversationWebSocketEvent {
  type: 'message' | 'error'
  message?: ConversationWebSocketMessage
  error?: string
}

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

export function openSocket(id: number | string): WebSocket {
  return new WebSocket(webSocketUrl(`/ws/conversations/${id}`))
}
