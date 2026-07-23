# Backlog

Known issues and follow-ups that aren't part of any current task. Newest entries at the top.

## Harden Web API + Vue live-chat concurrency and delivery

**Scope:** Resolve the follow-up review findings in the Web API + Vue reference implementation before porting its live-chat behavior to the other showroom variants.

**Findings:**

1. **High — competing HTTP fallbacks can corrupt a newer send's UI state.** The acknowledgement timeout and socket-close handler can start concurrent HTTP retries for the same `clientMessageId`. Each completion currently clears the shared draft and `pendingSend` state unconditionally, so an older retry can erase or unlock a newer send. Allow only one fallback request per client message and mutate send state only while the client message ID still matches.
2. **Medium — snapshot and live-message ordering use different rules.** `ConversationService` returns messages ordered by `SentAt`, then `Id`, while the Vue merge path re-sorts the thread solely by `Id`. Choose one canonical ordering and apply it consistently so a reload or live connection cannot move messages.
3. **Medium — one slow WebSocket client can stall reply requests after persistence.** Broadcasts await every connection, each socket send has no timeout, and the REST reply endpoint waits for broadcasting before responding. Bound outbound sends or queue them per connection, remove unhealthy connections, and do not let a stalled listener indefinitely delay an already-persisted reply.
4. **Low — an obsolete socket can surface an error in a newly selected thread.** The Vue socket `onerror` handler lacks the connection-version and disposal guards used by the other handlers. Apply the same guards so route changes cannot leak stale connection state into the next conversation.

**Acceptance criteria:** Add regression coverage for concurrent fallback completion, canonical message ordering, stalled WebSocket delivery, and route changes during connection failure. The full .NET build and Vue production build must continue to pass.

## Add live chat over WebSockets to every showroom variant

**Scope:** Upgrade buyer-seller messaging from request/response refreshes to live chat in all ten showroom variants: MVC, Razor Pages, Vue, HTMX, Blazor Server, Blazor WebAssembly, React, Svelte, Angular, and SolidJS.

**Reference implementation:** Build the Vue variant first and use it as the example for shared behavior, connection lifecycle, message delivery, reconnect handling, unread state, and conversation-thread UX. Reuse the shared backend contract wherever possible, then adapt the client integration idiomatically for each rendering model.

**Acceptance criteria:** Messages appear in an open conversation without a manual refresh; reconnects recover cleanly without duplicate or missing messages; authorization prevents users from subscribing to conversations they do not participate in; normal HTTP loading and sending remain a reliable fallback; and every variant provides equivalent observable behavior.

**Status (2026-07-23):** The Web API + Vue implementation's original five review issues (reconnect gap, socket session revocation, ack/idempotency, broadcast ordering, HTTP sending fallback) are fixed; the resulting protocol is documented in `design/webapi-architecture.md` § "WebSocket messaging". Complete the live-chat hardening entry above before treating Vue as the reference implementation. Remaining after that: port live chat to the other nine variants.
