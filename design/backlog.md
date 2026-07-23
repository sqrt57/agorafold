# Backlog

Known issues and follow-ups that aren't part of any current task. Newest entries at the top.

## Fix remaining Web API + Vue live-chat concurrency issues

**Scope:** Resolve the latest review findings in the Web API + Vue reference implementation before porting live chat to the other showroom variants.

**Findings:**

1. **High — a new connection can become permanently orphaned.** `ConversationWebSocketManager.Remove` can observe a conversation's connection dictionary as empty, then race with `Add`: the new connection is inserted into that same dictionary before the top-level `TryRemove`, which removes the dictionary containing it. The client receives `connected` but future broadcasts cannot find it, violating the no-missed-messages guarantee until reconnection. Keep empty conversation buckets or synchronize `Add`/`Remove` per conversation.
2. **Medium — client ordering loses timestamp precision.** `ConversationThreadView.vue` sorts with `Date.parse`, which retains only milliseconds, while backend timestamps can differ within a millisecond. Such messages compare equal in JavaScript and fall back to ID order, potentially disagreeing with `ConversationService`'s full-precision `SentAt`, then `Id` ordering. Preserve enough timestamp precision to apply the server's canonical order.
3. **Medium — final socket closure can overlap an outbound send.** Endpoint cleanup calls `WebSocket.CloseAsync` directly before waiting for the connection's outbound pump. This can overlap a pump `SendAsync`, despite `ConversationWebSocketConnection.CloseAsync` existing to serialize close frames with sends. Route all socket writes, including final and oversized-payload closure, through the connection's send lock and keep cleanup bounded.
4. **Low — live delivery never recovers after eight failed reconnects.** Once `MAX_RECONNECT_ATTEMPTS` is reached, the Vue thread schedules no further attempt and does not react when the browser comes back online. HTTP replies still work, but incoming live messages remain unavailable until navigation or refresh. Add a delayed retry, an `online` listener, or an explicit reconnect action.

**Acceptance criteria:** Add regression coverage for concurrent last-remove/new-add registration, sub-millisecond message ordering, close/send serialization, and recovery after the reconnect limit. The Web API tests, Vue test suite, full .NET build, and Vue production build must pass.

**Status (2026-07-23):** Complete. (1) `ConversationWebSocketManager` serializes bucket membership under a lock, so a concurrent last-`Remove`/new-`Add` can no longer orphan a registered connection. (2) The Vue thread compares `sentAt` at full 100ns-tick precision (`compareSentAt`) instead of `Date.parse`. (3) Endpoint cleanup drains the outbound pump (`DisposeAsync`) before closing the socket, the oversized-payload close goes through the connection's send lock, and the pump contains unexpected send faults instead of faulting the cleanup path. (4) The Vue thread leaves the offline state via a Retry control, the browser's `online` event, or a successful HTTP reply, and a `PolicyViolation` close (revoked session) stops the reconnect loop instead of retrying into 401s. Regression coverage: `tests/AgoraFold.WebApi.Tests` (orphan race, pump-fault containment, and — via an instrumented stub socket plus `internal`-visible `ConversationWebSocketEndpoint.CloseSocketAsync` — that the oversized-payload close and endpoint cleanup's final socket close can never overlap an in-flight pump send) and the Vue vitest suite (sub-millisecond ordering, session revocation, offline recovery via Retry and via HTTP reply) — each verified to fail against the pre-fix code. .NET build, Web API tests, Vue suite, and Vue production build all pass. Documented in `design/webapi-architecture.md` § "WebSocket messaging".

## Add live chat over WebSockets to every showroom variant

**Scope:** Upgrade buyer-seller messaging from request/response refreshes to live chat in all ten showroom variants: MVC, Razor Pages, Vue, HTMX, Blazor Server, Blazor WebAssembly, React, Svelte, Angular, and SolidJS.

**Reference implementation:** Build the Vue variant first and use it as the example for shared behavior, connection lifecycle, message delivery, reconnect handling, unread state, and conversation-thread UX. Reuse the shared backend contract wherever possible, then adapt the client integration idiomatically for each rendering model.

**Acceptance criteria:** Messages appear in an open conversation without a manual refresh; reconnects recover cleanly without duplicate or missing messages; authorization prevents users from subscribing to conversations they do not participate in; normal HTTP loading and sending remain a reliable fallback; and every variant provides equivalent observable behavior.

**Status (2026-07-23):** The Web API + Vue implementation is complete and hardened — the original five review issues and the four follow-up hardening findings (competing HTTP fallbacks, canonical `sentAt`/`id` ordering, stalled-listener broadcast isolation, stale `onerror` state) are all fixed with regression coverage (`src/AgoraFold.Vue` vitest suite, `tests/AgoraFold.WebApi.Tests` xunit suite). The protocol and delivery model are documented in `design/webapi-architecture.md` § "WebSocket messaging"; Vue is now the reference implementation. Remaining: port live chat to the other nine variants.
