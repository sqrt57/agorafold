# HTMX Variant Spec

## Summary

Fourth front-end variant: `AgoraFold.Htmx`, an ASP.NET Core MVC project (controllers + Razor views, same pairing as `AgoraFold.Mvc`) where HTMX drives partial-page updates instead of full page loads for interaction-heavy flows. Domain, feature scope, and data model are already fully specified in `project-spec.md` — this doc only covers what's different: how HTMX changes the interaction and controller/view architecture.

## Goals

- Same full feature set as every other variant: accounts, listings with images, browse/search, buyer-seller messaging.
- Demonstrate hypermedia-driven UI: server returns HTML fragments, HTMX swaps them in — no client-side framework, minimal hand-written JS.
- Reuse `AgoraFold.Core` as-is, same as MVC/Razor Pages/Web API + Vue.

## Non-goals

- WebSockets/SSE for live messaging — polling is enough to demonstrate the pattern for v1 (see open questions).
- A JS framework or build step (no Vite/npm bundling) — HTMX is vendored as a static file like Bootstrap/jQuery already are in `AgoraFold.Mvc`.
- Re-specifying the domain model or feature list — see `project-spec.md`.

## Architecture

- Project shape mirrors `AgoraFold.Mvc`: controllers map view models to/from `AgoraFold.Core` services; own `wwwroot`/uploads, own ports (proposed `5157`/`7133`, next in the existing sequence). `AgoraFoldExceptionFilter` gets duplicated in, same as it was for Razor Pages.
- **Fragment vs. full page**: actions that render both as a normal page nav and as an HTMX-triggered partial detect the request via the `HX-Request` request header (HTMX sets it on every request it issues) and return `PartialView()` instead of `View()`. A small controller-base helper (`IsHtmx()`) or action filter centralizes the check rather than repeating it in every action.
- **CSRF**: stays `[ValidateAntiForgeryToken]` (this is MVC, not the API-only project, so the framework attribute works as-is) — the wrinkle is that HTMX requests aren't always `<form>` submits (a bare `hx-post` button has no form to carry a hidden antiforgery field). Token gets attached via an `htmx:configRequest` listener in `site.js` that reads the token from a `<meta>` tag in `_Layout.cshtml` and sets it as a request header on every htmx-issued request, rather than relying on a hidden form field per action.
- **Destructive actions** (delete listing, delete image): replace the MVC variant's separate `Delete.cshtml` confirmation page with `hx-confirm` (HTMX's built-in browser `confirm()` prompt) on the delete control, posting straight to the delete action and swapping the result in — one less full navigation for something that doesn't need its own page.
- HTMX itself vendored under `wwwroot/lib/htmx` (matches how Bootstrap/jQuery are already vendored locally in `AgoraFold.Mvc`, not pulled from a CDN).

## Features — HTMX-specific interaction notes

### Browse / search (`Listings/Index`)
- Keyword field: `hx-get` on the results partial with `hx-trigger="keyup changed delay:300ms"` — live filtering as you type, no submit button round-trip.
- Category filter and pagination links: `hx-get` targeting the same results partial.
- `hx-push-url="true"` on all of the above so the URL (and back/forward/refresh) stays in sync with the current filter/page — a plain HTML link/form still works if JS is disabled, HTMX just intercepts it.

### Listing create / edit
- Core form (title/description/price/category) stays a normal full-page POST-redirect-GET — no benefit to fragmenting it.
- Image gallery on `Edit`: upload and delete swap just the gallery partial in place (`hx-post`/`hx-delete` targeting the gallery `<div>`) instead of a full page reload — the closest HTMX analogue to the named-handler pattern Razor Pages used for the same "multiple actions on one edit page" shape.

### Messaging
- Posting a reply appends the new message to the thread via `hx-post` + `hx-swap="beforeend"`, clearing the reply box in the same response.
- Thread container polls for new messages (`GET /Conversations/{id}/Poll?sinceId=`) with `hx-trigger="every 5s"` while the conversation view is open — server returns only messages newer than `sinceId`, keeping the poll payload small instead of re-rendering the whole thread (simplest way to show near-real-time updates without SSE/WebSockets; `Message` has no read/unread tracking in `AgoraFold.Core`, so a true unread badge isn't in scope here).
- Both the reply response and each poll response carry an out-of-band swap (`hx-swap-oob`) updating a hidden `#last-message-id` marker used as the next poll's `sinceId` — a deliberate, minimal showcase of `hx-swap-oob` (update an element outside the main target as a side effect of the same response) without inventing new domain state.

## Open questions

- Polling interval for the message thread (proposed 5s) — may need tuning once it's actually running, not a blocker to start building.
- Whether `hx-boost` gets applied globally (boosting all normal links/forms into AJAX navigations that swap `<body>`) or left off in favor of only using HTMX explicitly where a partial genuinely helps. Leaning toward leaving it off — the point of this variant is showing deliberate partial updates, not turning the whole app into a pseudo-SPA.
