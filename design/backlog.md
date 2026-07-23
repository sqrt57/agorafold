# Backlog

Known issues and follow-ups that aren't part of any current task. Newest entries at the top.

## Add live chat over WebSockets to every showroom variant

**Scope:** Upgrade buyer-seller messaging from request/response refreshes to live chat in all ten showroom variants: MVC, Razor Pages, Vue, HTMX, Blazor Server, Blazor WebAssembly, React, Svelte, Angular, and SolidJS.

**Reference implementation:** Build the Vue variant first and use it as the example for shared behavior, connection lifecycle, message delivery, reconnect handling, unread state, and conversation-thread UX. Reuse the shared backend contract wherever possible, then adapt the client integration idiomatically for each rendering model.

**Acceptance criteria:** Messages appear in an open conversation without a manual refresh; reconnects recover cleanly without duplicate or missing messages; authorization prevents users from subscribing to conversations they do not participate in; normal HTTP loading and sending remain a reliable fallback; and every variant provides equivalent observable behavior.

**Status (2026-07-23):** The Web API + Vue implementation is complete and hardened — the original five review issues and the four follow-up hardening findings (competing HTTP fallbacks, canonical `sentAt`/`id` ordering, stalled-listener broadcast isolation, stale `onerror` state) are all fixed with regression coverage (`src/AgoraFold.Vue` vitest suite, `tests/AgoraFold.WebApi.Tests` xunit suite). The protocol and delivery model are documented in `design/webapi-architecture.md` § "WebSocket messaging"; Vue is now the reference implementation. Remaining: port live chat to the other nine variants.
