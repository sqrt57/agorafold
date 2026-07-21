import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ConversationsApi } from '../../api/conversations.service';
import type { ConversationSummary } from '../../api/types';

@Component({
  selector: 'app-conversations-inbox',
  imports: [RouterLink],
  templateUrl: './conversations-inbox.html',
})
export class ConversationsInbox {
  private readonly conversationsApi = inject(ConversationsApi);

  protected readonly conversations = signal<ConversationSummary[]>([]);

  constructor() {
    void this.conversationsApi.getInbox().then((loaded) => this.conversations.set(loaded));
  }
}
