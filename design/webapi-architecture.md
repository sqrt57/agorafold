# AgoraFold.WebApi Architecture

## Summary

The JSON API backend for the "Web API + JS frontend" family of variants. Currently consumed by `AgoraFold.Vue` (Vite/Vue 3 + TypeScript SPA); the planned React, Svelte, Angular, and SolidJS variants will consume this same API rather than getting their own backend, so the auth/CORS/CSRF design below applies to all of them, not just Vue. Ports: `http://localhost:5155`, `https://localhost:7131` (Vue dev server: `http://localhost:5173`).

No separate spec doc exists for this variant — it predates the current per-variant `-spec.md` convention. This document is the architecture reference; new JS frontend variants should link back here rather than re-documenting the backend.

## Structure

- Controllers (`Controllers/`) map DTOs (`Models/`, organized by feature) to/from the same `AgoraFold.Core` services Mvc/RazorPages use.
- The SPA client is not a .NET project and isn't added to `AgoraFold.slnx` — each future JS frontend variant follows the same pattern (own `src/AgoraFold.<Framework>` directory).

## Vue client specifics

- Type-checking is a separate `vue-tsc -b` project-reference build (`tsconfig.json` → `tsconfig.app.json` for `src/`, `tsconfig.node.json` for `vite.config.ts`), run before `vite build` via the `build` npm script.
- Shared DTO shapes live in `src/api/types.ts`.

## Auth

Cookie-based `AddIdentity`, but `ConfigureApplicationCookie`'s `OnRedirectToLogin`/`OnRedirectToAccessDenied` events are overridden to return raw 401/403 instead of Identity's default 302-to-a-login-page — without this, every `[Authorize]` failure would come back as a redirect the SPA can't sensibly follow.

## CORS

`AddCors`/`UseCors` (registered before `UseAuthentication`) allows the Vue dev origin with `AllowCredentials()`. Future JS variants need their own dev origin added here.

## CSRF

Parity with Mvc/RazorPages's `[ValidateAntiForgeryToken]` uses a custom `Filters/ValidateCsrfTokenAttribute` instead of the framework attribute — the framework's `[ValidateAntiForgeryToken]` resolves DI services that only `AddControllersWithViews`/`AddMvc` register, which this API-only (`AddControllers`) project doesn't have. The custom filter calls `IAntiforgery.ValidateRequestAsync` directly.

A `GET /api/antiforgery/token` endpoint (`AntiforgeryController`) hands the client a token to echo back as `X-CSRF-TOKEN`.

**Non-obvious gotcha**: ASP.NET's antiforgery token is bound to whichever identity (anonymous or a specific user) was active when it was issued, so the Vue client (`src/api/client.ts`) fetches a fresh token before every mutating request rather than caching one — a token cached from before login/register/logout gets rejected on the next mutating call once the identity changes. Any future JS frontend consuming this API needs the same fetch-fresh-token-per-mutation pattern.
