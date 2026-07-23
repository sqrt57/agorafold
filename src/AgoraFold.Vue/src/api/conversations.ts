import { apiFetch, webSocketUrl } from './client'
import type { ConversationSummary, ConversationThread } from './types'

export interface ConversationWebSocketMessage {
  id: number
  senderId: string
  senderDisplayName: string
  body: string
  sentAt: string
}

export interface ConversationWebSocketEvent {
  // 'connected' — the server has registered this socket for broadcasts; a thread snapshot
  // fetched after this event, merged by message id, cannot miss messages.
  // 'ack' — the send identified by clientMessageId was persisted (message carries the result).
  type: 'connected' | 'message' | 'ack' | 'error'
  message?: ConversationWebSocketMessage
  error?: string
  clientMessageId?: string
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

export function reply(id: number | string, body: string, clientMessageId?: string): Promise<ConversationThread> {
  return apiFetch<ConversationThread>(`/api/conversations/${id}/replies`, {
    method: 'POST',
    body: { body, clientMessageId },
  })
}

export function openSocket(id: number | string): WebSocket {
  return new WebSocket(webSocketUrl(`/ws/conversations/${id}`))
}
