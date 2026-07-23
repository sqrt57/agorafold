# AgoraFold.WebApi Architecture

## Summary

The JSON API backend for the "Web API + JS frontend" family of variants. Consumed by `AgoraFold.Vue` (Vite/Vue 3 + TypeScript SPA), `AgoraFold.React` (Vite/React + TypeScript SPA), `AgoraFold.Svelte` (Vite/Svelte + TypeScript SPA), `AgoraFold.Angular` (Angular CLI + TypeScript SPA), and `AgoraFold.SolidJS` (Vite/Solid + TypeScript SPA) — the last and final planned JS frontend. Ports: `http://localhost:5155`, `https://localhost:7131` (Vue dev server: `http://localhost:5173`, React dev server: `http://localhost:5174`, Svelte dev server: `http://localhost:5175`, Angular dev server: `http://localhost:5176`, SolidJS dev server: `http://localhost:5177`).

No separate spec doc exists for this variant — it predates the current per-variant `-spec.md` convention. This document is the architecture reference; new JS frontend variants should link back here rather than re-documenting the backend.

## Structure

- Controllers (`Controllers/`) map DTOs (`Models/`, organized by feature) to/from the same `AgoraFold.Core` services Mvc/RazorPages use.
- The SPA client is not a .NET project and isn't added to `AgoraFold.slnx` — each future JS frontend variant follows the same pattern (own `src/AgoraFold.<Framework>` directory).

## Vue client specifics

- Type-checking is a separate `vue-tsc -b` project-reference build (`tsconfig.json` → `tsconfig.app.json` for `src/`, `tsconfig.node.json` for `vite.config.ts`), run before `vite build` via the `build` npm script.
- Shared DTO shapes live in `src/api/types.ts`.

## React client specifics

- `AgoraFold.React` ports `AgoraFold.Vue` view-for-view: the framework-agnostic `src/api/*.ts` layer (`client.ts`'s `apiFetch`/CSRF handling, `types.ts`, and the per-feature wrapper modules) is copied near-verbatim, since none of it references Vue.
- Pinia's `useAuthStore` becomes a `src/context/AuthContext.tsx` (`AuthProvider` + `useAuth()`), hydrated once on mount via a promise ref (dedupes React 19 Strict Mode's double-invoked effect).
- `vue-router`'s per-route `meta.requiresAuth` + `beforeEach` guard becomes a `<RequireAuth>` wrapper component (`src/components/RequireAuth.tsx`) used per-route in `App.tsx`'s `<Routes>`.
- The single global `src/style.css` is reused unmodified as `src/index.css` (plain CSS, no Vue-specific scoping to strip).

## Svelte client specifics

- `AgoraFold.Svelte` is a frontend-only Vite project, like Vue and React, and is not added to `AgoraFold.slnx`.
- It ports the same `src/api/*.ts` contract and global CSS, using Svelte stores for authentication and a small history-based router so the feature URLs remain identical across the JS clients.
- Its dev server runs at `http://localhost:5175`; the API origin is configured through `VITE_API_BASE_URL` in `.env`.

## Angular client specifics

- `AgoraFold.Angular` is a frontend-only Angular CLI project (`ng new`, standalone components, no NgModules), like the other three, and is not added to `AgoraFold.slnx`.
- It ports the same `src/api/*.ts` contract as an injectable `ApiClient` service (`src/app/api/client.ts`) plus per-feature services (`AccountApi`, `CategoriesApi`, `ListingsApi`, `ConversationsApi`), and the same global CSS unmodified as `src/styles.css`.
- Auth/hydration state lives in an injectable `AuthService` (`src/app/auth/auth.service.ts`) using signals for `user`/`hydrated`, with the same memoized-hydrate-promise pattern the other clients use to dedupe the initial `/api/account/me` call.
- Unlike the other three clients' hand-rolled routers, `AgoraFold.Angular` uses the real Angular Router (`src/app/app.routes.ts`) — protected routes carry a functional `authGuard: CanActivateFn` that awaits `AuthService.hydrate()` before deciding, redirecting to `/login?returnUrl=...` on failure, same as the others' behavior.
- Its dev server runs at `http://localhost:5176` (`ng serve`, configured for this fixed port in `angular.json`); like the other three clients it calls `AgoraFold.WebApi` directly cross-origin rather than through a dev-server proxy, via `environment.apiBaseUrl` (`http://localhost:5155` in dev, same pattern as the other clients' `VITE_API_BASE_URL`) prefixed onto image URLs by the ported `imageUrl()` helper.

## SolidJS client specifics

- `AgoraFold.SolidJS` is a frontend-only Vite project (`solid-ts` template), like Vue/React/Svelte, and is not added to `AgoraFold.slnx`. It's the last planned JS frontend variant.
- It ports the same `src/api/*.ts` contract and global CSS (`src/style.css`, copied from `AgoraFold.Svelte`'s version) unmodified.
- Routing uses `@solidjs/router`'s `<Router root={Layout}>` (root wraps every route in `AuthProvider` + `NavBar`) with a flat list of `<Route>` elements in `App.tsx`; `<A>` replaces `<Link>`/anchor components from the other clients, and `<Navigate>` replaces `RequireAuth`'s redirect.
- Auth lives in `src/context/AuthContext.tsx`, a `createContext`/`useContext` pair using `createSignal` for `user`/`hydrated` state. Unlike React's `AuthProvider`, no ref/memo dedupe trick is needed for the initial `/api/account/me` hydration call — Solid component functions run once (not once per render), so a plain `onMount` is sufficient.
- Solid's fine-grained reactivity means props must stay as accessors: components read `props.foo` directly (never destructured) and signals are called as functions (`user()`, not `user`) at the point of use, including inside JSX — destructuring either breaks reactivity.
- Its dev server runs at `http://localhost:5177` (`vite`, `server.port` fixed in `vite.config.ts`), calling `AgoraFold.WebApi` directly cross-origin like the other four clients, via `VITE_API_BASE_URL`.

## Auth

Cookie-based `AddIdentity`, but `ConfigureApplicationCookie`'s `OnRedirectToLogin`/`OnRedirectToAccessDenied` events are overridden to return raw 401/403 instead of Identity's default 302-to-a-login-page — without this, every `[Authorize]` failure would come back as a redirect the SPA can't sensibly follow.

## CORS

`AddCors`/`UseCors` (registered before `UseAuthentication`) allows a configurable list of JS client dev origins (`Cors:JsClientOrigins` in `appsettings.json`, currently Vue's `5173`, React's `5174`, Svelte's `5175`, Angular's `5176`, and SolidJS's `5177`) with `AllowCredentials()`. Each new JS variant needs its own dev origin added to that array.

The policy (`Options/ConfigureCorsOptions.cs`, an `IConfigureOptions<CorsOptions>` that takes a DI-injected `IOptionsMonitor<JsClientCorsOptions>`) checks each request's origin against `JsClientCorsOptions.CurrentValue.JsClientOrigins` via `SetIsOriginAllowed` rather than baking a fixed array into the policy with `WithOrigins` at startup. Because `IOptionsMonitor` re-reads `appsettings.json` on file change, editing `Cors:JsClientOrigins` takes effect on the next request — no need to restart the running `dotnet run` process to add a new client's dev origin.

## CSRF

Parity with Mvc/RazorPages's `[ValidateAntiForgeryToken]` uses a custom `Filters/ValidateCsrfTokenAttribute` instead of the framework attribute — the framework's `[ValidateAntiForgeryToken]` resolves DI services that only `AddControllersWithViews`/`AddMvc` register, which this API-only (`AddControllers`) project doesn't have. The custom filter calls `IAntiforgery.ValidateRequestAsync` directly.

A `GET /api/antiforgery/token` endpoint (`AntiforgeryController`) hands the client a token to echo back as `X-CSRF-TOKEN`.

**Non-obvious gotcha**: ASP.NET's antiforgery token is bound to whichever identity (anonymous or a specific user) was active when it was issued, so the Vue, React, Svelte, and Angular clients' `src/api/client.ts` fetch a fresh token before every mutating request rather than caching one — a token cached from before login/register/logout gets rejected on the next mutating call once the identity changes. Any future JS frontend consuming this API needs the same fetch-fresh-token-per-mutation pattern.

## WebSocket messaging

The transport described in this section — the wire protocol records, the bounded-queue connection manager, and the endpoint itself — lives in the shared `AgoraFold.LiveChat` project, not directly in `AgoraFold.WebApi`. It was extracted there (Phase 1 of `design/live-chat-porting-plan.md`) so the other showroom variants can reuse the same hardened transport instead of each reimplementing it; `AgoraFold.WebApi` references `AgoraFold.LiveChat` and supplies the pieces that stay host-specific: `JsClientCorsOptions`/`ConfigureCorsOptions` (still WebApi-owned, since they also drive the unrelated HTTP `UseCors("JsClients")` policy) are adapted into a `LiveChat.Origin.ConfiguredOriginPolicy` for the handshake's origin check, and `ConversationsController.Reply` publishes through the shared `IConversationEventPublisher` abstraction rather than depending on the connection manager directly. Regression tests for the transport moved with it, to `tests/AgoraFold.LiveChat.Tests`.

The API exposes an authenticated native WebSocket endpoint at `GET /ws/conversations/{conversationId}`. The browser's Identity cookie authenticates the handshake, and the endpoint also checks the request origin — via the injected `ILiveChatOriginPolicy`, `ConfiguredOriginPolicy` for this host — against `Cors:JsClientOrigins` before accepting it. A connected user must be the listing owner or conversation participant.

The wire protocol:

- Client to server: `{ "type": "message", "body": "...", "clientMessageId": "<guid>" }`. `clientMessageId` is an optional idempotency key — retrying a send with the same key returns the already-persisted message instead of inserting a duplicate.
- Server to the new connection, sent only after it is registered for broadcasts: `{ "type": "connected" }`. Once a client observes this event, every later commit is guaranteed to reach it as a broadcast, so a thread snapshot fetched from that point on (merged by message id) cannot miss messages — this closes the load/subscribe race on both initial connect and reconnect.
- Server to all participants: `{ "type": "message", "message": { "id": ..., "senderId": "...", "senderDisplayName": "...", "body": "...", "sentAt": "..." } }`. `id` is the persisted message id; clients merge and dedupe by it rather than trusting arrival order (concurrent broadcasts don't guarantee database order), then sort the thread by `sentAt` with `id` as tiebreak — the same canonical order `ConversationService` gives snapshots, so a reload or reconnect cannot move messages. Clients must compare `sentAt` at full precision: it carries the server's 100ns ticks with trailing fractional zeros trimmed, so `Date.parse` (millisecond truncation) and raw string comparison (varying fraction lengths) both mis-order sub-millisecond neighbors — the Vue client's `compareSentAt` pads the fractions before comparing.
- Server to the sending connection only: `{ "type": "ack", "clientMessageId": "...", "message": { ... } }`, confirming persistence of that send.
- Validation or protocol failures: `{ "type": "error", "error": "...", "clientMessageId": ... }` (`clientMessageId` echoed when the failure relates to a specific send).

Send idempotency is database-enforced: `messages.client_message_id` (nullable `uuid`) with a filtered unique index on `(conversation_id, sender_id, client_message_id)`; `ConversationService.PostReplyAsync` returns the existing row for a retried key, including when it loses a concurrent-insert race on the index. `POST /api/conversations/{id}/replies` accepts the same optional `clientMessageId`, and the thread/reply responses include each message's `id`, so socket and HTTP deliveries dedupe against each other.

Session revocation: cookie authentication only revalidates the Identity security stamp on HTTP requests, which never covers an established socket. The endpoint therefore revalidates the session itself — `SignInManager.ValidateSecurityStampAsync` plus a lockout check — at the handshake, on every incoming message, and on a 1-minute timer for idle listeners, closing the socket with `PolicyViolation` when the session was invalidated by a password change, admin deactivation (which rotates the stamp and sets lockout), or account deletion.

`AgoraFold.LiveChat`'s in-memory `ConversationWebSocketManager` tracks connections by conversation. Each incoming message gets a fresh DI scope, is authorized and persisted through `IConversationService`, and is broadcast only after `SaveChangesAsync` succeeds. The long-lived socket therefore never holds a scoped `AppDbContext`.

Delivery is isolated per connection: a broadcast only enqueues onto each connection's bounded outbound queue (drained by a per-connection pump task), so a stalled listener never delays the REST reply endpoint, the sending participant's receive loop, or other participants. Every socket write — including close frames — is bounded by a send timeout; a timed-out write, or a queue overflow, marks the peer unhealthy and aborts the socket rather than silently dropping events (a drop would break the `connected` no-missed-messages guarantee — an aborted client instead reconnects and reloads the snapshot). Connection registration is serialized under a membership lock: removing a conversation's last connection must not race an incoming `Add` into removing the bucket that now holds it, which would orphan a connection that was already promised delivery. Endpoint cleanup drains the connection's outbound pump before sending the close frame, so queued events flush while the socket is still open and the close never overlaps a pump write. Covered by `tests/AgoraFold.LiveChat.Tests`.

The Vue conversation thread uses the socket for replies and live delivery. It keeps the reply draft in the textarea until the server acknowledges persistence; an ambiguous outcome (disconnect or ack timeout while a send is pending) is resolved by retrying over HTTP with the same `clientMessageId`, and when the socket is down entirely it falls back to plain HTTP sends. Initial load and reconnect recovery use `GET /api/conversations/{id}` merged by message id, re-fetched on every `connected` event. Reconnects back off exponentially for eight attempts before parking in an offline state, which is left three ways: an explicit Retry control, the browser's `online` event, or a successful HTTP reply (proof the server is reachable again). A server close with `PolicyViolation` (revoked session) skips reconnecting entirely — the handshake would just repeat the 401 — and tells the user to sign in again, keeping any draft.

## Gotchas

**A plain `Microsoft.NET.Sdk` project with `<FrameworkReference Include="Microsoft.AspNetCore.App" />` does not get ASP.NET Core's implicit usings.** `AgoraFold.LiveChat` needs this shape (a library, not a runnable app, but one that uses `HttpContext`, `IServiceScopeFactory`, minimal-API mapping, etc.) — but the implicit-usings list that makes those types resolve without a `using` in a project is only added by `Microsoft.NET.Sdk.Web`, not by the presence of the framework reference. The symptom is `CS0246: The type or namespace name 'HttpContext' could not be found` at build time even though the assembly reference is clearly there; the fix is explicit `using Microsoft.AspNetCore.Http;` / `using Microsoft.Extensions.DependencyInjection;` in every file that touches those types. Any future project following this same "shared library that needs ASP.NET Core hosting types" pattern will hit the same thing.

**(Fixed) `POST /api/conversations/{id}/replies` used to be able to return messages out of chronological order in its own response.** `ConversationsController.Reply` calls `PostReplyAsync` (which adds the new `Message` via `db.Messages.Add(...)` and saves) and then `GetThreadAsync` (which re-queries the same conversation with `.Include(c => c.Messages.OrderBy(m => m.SentAt))`) — both against the same scoped `DbContext` within one request. Because the `Conversation`/new `Message` were already tracked from the first call, EF Core's change-tracker fixup left the just-added message in its fixup-assigned position within the `Messages` collection rather than the position the `OrderBy(SentAt)` subquery would place it in, so the POST response's message list could render the new message out of order (a plain `GET /api/conversations/{id}` — a fresh `DbContext` — was never affected). Confirmed via `AgoraFold.React`'s reply flow, but backend-side and shared by every JS frontend. Fixed in `ConversationService.GetThreadAsync` (`AgoraFold.Core`), which now re-sorts `conversation.Messages` by `SentAt` before returning, so the guarantee no longer depends on EF's tracking/fixup behavior.
