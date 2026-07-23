# Backlog

Known issues and follow-ups that aren't part of any current task. Newest entries at the top.

## Add live chat over WebSockets to every showroom variant

**Scope:** Upgrade buyer-seller messaging from request/response refreshes to live chat in all ten showroom variants: MVC, Razor Pages, Vue, HTMX, Blazor Server, Blazor WebAssembly, React, Svelte, Angular, and SolidJS.

**Reference implementation:** Build the Vue variant first and use it as the example for shared behavior, connection lifecycle, message delivery, reconnect handling, unread state, and conversation-thread UX. Reuse the shared backend contract wherever possible, then adapt the client integration idiomatically for each rendering model.

**Acceptance criteria:** Messages appear in an open conversation without a manual refresh; reconnects recover cleanly without duplicate or missing messages; authorization prevents users from subscribing to conversations they do not participate in; normal HTTP loading and sending remain a reliable fallback; and every variant provides equivalent observable behavior.

### Existing Web API + Vue implementation issues

Fix these before treating Vue as the reference implementation:

1. **High — reconnect recovery can miss messages.** Vue loads the persisted thread before opening the socket, both initially and after a disconnect. A message committed after the HTTP response but before socket registration appears in neither the snapshot nor the live stream. Introduce a race-free subscribe-and-recover protocol, using stable message IDs or a server-assigned conversation sequence so the client can request and merge everything after its last received message.
2. **High — account deactivation does not revoke established sockets.** Authentication is evaluated during the handshake, but subsequent messages only verify conversation participation and that the user record still exists. Revalidate the account's lockout/security-stamp state during a long-lived connection, and close or reject messages from sessions invalidated by logout, password/security-stamp changes, account deactivation, or deletion.
3. **Medium — sending has no acknowledgment or idempotency.** Vue clears the reply immediately after `WebSocket.send()`, while the protocol has no client request ID, persisted message ID, or correlated acknowledgment. Preserve the draft until persistence is acknowledged and make retries idempotent so an ambiguous disconnect cannot silently lose or duplicate a message.
4. **Medium — concurrent broadcasts can render out of persisted order.** The per-connection send lock prevents overlapping socket writes but does not guarantee that concurrent broadcasts acquire it in database order. Include a stable message ID or conversation sequence in every event and have clients merge, deduplicate, and order messages by that value rather than blindly appending arrival order.
5. **Low — Vue has no HTTP sending fallback.** The HTTP reply endpoint remains available, but Vue refuses to send while the socket is unavailable. Fall back to the existing HTTP reply API when live transport is not connected, while ensuring the HTTP response and any later socket broadcast are deduplicated through the same message identity.
