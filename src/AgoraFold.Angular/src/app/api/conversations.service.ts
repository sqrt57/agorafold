import { Injectable, inject } from '@angular/core';
import { ApiClient } from './client';
import type { ConversationSummary, ConversationThread } from './types';

@Injectable({ providedIn: 'root' })
export class ConversationsApi {
  private readonly api = inject(ApiClient);

  getInbox(): Promise<ConversationSummary[]> {
    return this.api.request<ConversationSummary[]>('/api/conversations');
  }

  getThread(id: number | string): Promise<ConversationThread> {
    return this.api.request<ConversationThread>(`/api/conversations/${id}`);
  }

  start(listingId: number): Promise<ConversationThread> {
    return this.api.request<ConversationThread>('/api/conversations', { method: 'POST', body: { listingId } });
  }

  reply(id: number | string, body: string): Promise<ConversationThread> {
    return this.api.request<ConversationThread>(`/api/conversations/${id}/replies`, { method: 'POST', body: { body } });
  }
}
