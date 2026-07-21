import { Component, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiError } from '../../api/client';
import { ConversationsApi } from '../../api/conversations.service';
import type { ConversationThread as ConversationThreadModel } from '../../api/types';

@Component({
  selector: 'app-conversation-thread',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './conversation-thread.html',
})
export class ConversationThread {
  private readonly conversationsApi = inject(ConversationsApi);
  private readonly route = inject(ActivatedRoute);

  protected readonly thread = signal<ConversationThreadModel | null>(null);
  protected readonly replyBody = signal('');
  protected readonly error = signal('');
  protected readonly sending = signal(false);

  private loadedId: string | null = null;

  private readonly paramMap = toSignal(this.route.paramMap, { initialValue: this.route.snapshot.paramMap });

  constructor() {
    effect(() => {
      const id = this.paramMap().get('id');
      if (id && id !== this.loadedId) {
        this.loadedId = id;
        this.thread.set(null);
        this.error.set('');
        void this.conversationsApi.getThread(id).then((loaded) => this.thread.set(loaded));
      }
    });
  }

  async sendReply(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    const thread = this.thread();
    if (!thread) return;
    this.sending.set(true);
    this.error.set('');
    try {
      this.thread.set(await this.conversationsApi.reply(thread.id, this.replyBody()));
      this.replyBody.set('');
    } catch (exception) {
      this.error.set(exception instanceof ApiError ? (exception.errors[0] ?? exception.message) : (exception as Error).message);
    } finally {
      this.sending.set(false);
    }
  }
}
