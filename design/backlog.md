# Backlog

Known issues and follow-ups that aren't part of any current task. Newest entries at the top.

## Add live chat over WebSockets to every showroom variant

**Scope:** Upgrade buyer-seller messaging from request/response refreshes to live chat in all ten showroom variants: MVC, Razor Pages, Vue, HTMX, Blazor Server, Blazor WebAssembly, React, Svelte, Angular, and SolidJS.

**Reference implementation:** Build the Vue variant first and use it as the example for shared behavior, connection lifecycle, message delivery, reconnect handling, unread state, and conversation-thread UX. Reuse the shared backend contract wherever possible, then adapt the client integration idiomatically for each rendering model.

**Acceptance criteria:** Messages appear in an open conversation without a manual refresh; reconnects recover cleanly without duplicate or missing messages; authorization prevents users from subscribing to conversations they do not participate in; normal HTTP loading and sending remain a reliable fallback; and every variant provides equivalent observable behavior.

**Status (2026-07-23):** The Web API + Vue implementation's five review issues (reconnect gap, socket session revocation, ack/idempotency, broadcast ordering, HTTP sending fallback) are fixed; the resulting protocol is documented in `design/webapi-architecture.md` § "WebSocket messaging" and Vue is ready to serve as the reference implementation. Remaining: port live chat to the other nine variants.
