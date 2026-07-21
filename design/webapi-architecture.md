# AgoraFold.WebApi Architecture

## Summary

The JSON API backend for the "Web API + JS frontend" family of variants. Consumed by `AgoraFold.Vue` (Vite/Vue 3 + TypeScript SPA), `AgoraFold.React` (Vite/React + TypeScript SPA), and `AgoraFold.Svelte` (Vite/Svelte + TypeScript SPA); the planned Angular and SolidJS variants will consume this same API rather than getting their own backend, so the auth/CORS/CSRF design below applies to all of them. Ports: `http://localhost:5155`, `https://localhost:7131` (Vue dev server: `http://localhost:5173`, React dev server: `http://localhost:5174`, Svelte dev server: `http://localhost:5175`).

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

## Auth

Cookie-based `AddIdentity`, but `ConfigureApplicationCookie`'s `OnRedirectToLogin`/`OnRedirectToAccessDenied` events are overridden to return raw 401/403 instead of Identity's default 302-to-a-login-page — without this, every `[Authorize]` failure would come back as a redirect the SPA can't sensibly follow.

## CORS

`AddCors`/`UseCors` (registered before `UseAuthentication`) allows a configurable list of JS client dev origins (`Cors:JsClientOrigins` in `appsettings.json`, currently Vue's `5173`, React's `5174`, and Svelte's `5175`) with `AllowCredentials()`. Each new JS variant needs its own dev origin added to that array.

## CSRF

Parity with Mvc/RazorPages's `[ValidateAntiForgeryToken]` uses a custom `Filters/ValidateCsrfTokenAttribute` instead of the framework attribute — the framework's `[ValidateAntiForgeryToken]` resolves DI services that only `AddControllersWithViews`/`AddMvc` register, which this API-only (`AddControllers`) project doesn't have. The custom filter calls `IAntiforgery.ValidateRequestAsync` directly.

A `GET /api/antiforgery/token` endpoint (`AntiforgeryController`) hands the client a token to echo back as `X-CSRF-TOKEN`.

**Non-obvious gotcha**: ASP.NET's antiforgery token is bound to whichever identity (anonymous or a specific user) was active when it was issued, so the Vue, React, and Svelte clients' `src/api/client.ts` fetch a fresh token before every mutating request rather than caching one — a token cached from before login/register/logout gets rejected on the next mutating call once the identity changes. Any future JS frontend consuming this API needs the same fetch-fresh-token-per-mutation pattern.

## Gotchas

**`POST /api/conversations/{id}/replies` can return messages out of chronological order in its own response.** `ConversationsController.Reply` calls `PostReplyAsync` (which adds the new `Message` via `db.Messages.Add(...)` and saves) and then `GetThreadAsync` (which re-queries the same conversation with `.Include(c => c.Messages.OrderBy(m => m.SentAt))`) — both against the same scoped `DbContext` within one request. Because the `Conversation`/new `Message` are already tracked from the first call, EF Core's change-tracker fixup can leave the just-added message in its fixup-assigned position within the `Messages` collection rather than the position the `OrderBy(SentAt)` subquery would place it in, so the POST response's message list can render the new message out of order. A plain `GET /api/conversations/{id}` immediately after (a fresh `DbContext`) always returns the correct chronological order — confirmed while browser-testing `AgoraFold.React`'s reply flow. Not something a single JS client can fix; if it matters, it needs fixing backend-side (e.g. re-sort `conversation.Messages` by `SentAt` before mapping to `ConversationThreadResponse` in `Reply`), which would fix it for every JS frontend at once.
