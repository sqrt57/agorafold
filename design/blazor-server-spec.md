# Blazor Server Variant Spec

## Summary

Fifth front-end variant: `AgoraFold.BlazorServer`, built on the .NET 8+ "Blazor Web App" template shape (Interactive Server render mode over a persistent SignalR circuit, not the legacy pre-.NET-8 "Blazor Server" template). Domain, feature scope, and data model are already fully specified in `project-spec.md` — this doc only covers what's different: the render-mode/circuit split, DI-lifetime trade-off, and the mechanisms that replace what MVC got from its request/response pipeline for free.

## Goals

- Same full feature set as every other variant: accounts, listings with images, browse/search, buyer-seller messaging — parity with `AgoraFold.Mvc`, not beyond it.
- Demonstrate component-based, stateful server rendering: UI updates flow over a live connection instead of full page POST/redirect/GET cycles.
- Reuse `AgoraFold.Core` as-is, same as every other variant.

## Non-goals

- Blazor WebAssembly — a separate future variant, not part of this one.
- Live/pushed message delivery — a circuit could trivially support it, but doing so would exceed MVC's reference feature set. Punted as a stretch goal (see Messaging below).
- Re-specifying the domain model or feature list — see `project-spec.md`.

## Architecture

- Project shape: own `wwwroot`/uploads, own ports (`5159`/`7135`, next in the existing sequence). `Program.cs` copies the `AddDbContext`/`AddIdentity`/`AddAgoraFoldCore`/`Configure<ListingImageStorageOptions>` block verbatim from `AgoraFold.Htmx` — Core's DI contract is render-model-agnostic.
- **DI lifetime**: Core's services (and `AppDbContext`) stay `Scoped`, unchanged. In Blazor Server, `Scoped` = per-circuit (one browser tab's SignalR connection), not per-HTTP-request — a long-open tab holds one `AppDbContext` instance for its whole session. This is a deliberate trade-off, not an oversight: a circuit's renderer already serializes event handling (no concurrent access to that instance from app code), and each tab is its own circuit with its own scope, so there's no cross-user contention. `IDbContextFactory<AppDbContext>` was considered and rejected — it would require wrapping every Core service to accept a factory instead of a constructor-injected context, real complexity for a portfolio piece whose point is idiomatic-per-variant code, not defensive engineering against a load profile this app doesn't have.
- **Render-mode split**: most pages carry `@rendermode InteractiveServer`. Login/Register (`Components/Account/Pages/`) stay static SSR (no `@rendermode`) because signing in/out writes a `Set-Cookie` header, which needs a real HTTP response — unavailable once a component is running inside a persistent circuit. Logout is a plain minimal API endpoint (`POST /Account/Logout`, `Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`) for the same reason, not a routable component.
- **Auth state**: components read identity via a cascaded `AuthenticationState` (`AddCascadingAuthenticationState()` + `IdentityRevalidatingAuthenticationStateProvider`, which periodically re-checks the security stamp so a long-open circuit notices e.g. a password change elsewhere), not `HttpContext.User` directly. A small `CurrentUserAccessor` scoped service wraps the async lookup, mirroring Mvc's synchronous `CurrentUserId` controller property.
- **`[Authorize]`**: applied per-page via `@attribute [Authorize]` (Razor Components routing has no folder-level convention the way Razor Pages' `AuthorizeFolder` does). `Routes.razor`'s `<AuthorizeRouteView>` renders a `<RedirectToLogin />` component on `NotAuthorized`, forcing a real navigation to `/Account/Login?returnUrl=...` so the login page's cookie write lands on a fresh request.

## Error handling

No MVC-filter equivalent exists for Blazor Server. Two patterns, matching how Mvc itself already splits "bubble" vs. "catch-and-redisplay":

- **GET-style loads** (listing/conversation detail, edit, delete): plain `try/catch (Exception ex) when (ex is NotFoundException or ForbiddenException)` in `OnParametersSetAsync`, storing the caught exception and rendering it via `Components/Shared/DomainErrorAlert.razor` — a presentational component that switches on exception type (`NotFoundException` → "not found" alert, `ForbiddenException` → "no permission" alert, anything else → generic failure alert). **Not** an `<ErrorBoundary>`: `ErrorBoundary` only catches exceptions thrown by its *descendant* components during rendering, never exceptions from the ancestor component that hosts it — and the fetch/ownership-check exceptions here are thrown by the page's own `OnParametersSetAsync`, one level above where a boundary could be placed in the markup. An initial implementation used `ErrorBoundary` and it silently failed to catch a 404 on `/Listings/{nonexistentId}` (verified in-browser: the exception surfaced as an unhandled 500 instead), which is what prompted switching to explicit try/catch.
- **POST-style forms** (create/edit save, reply, register): local `try/catch (ValidationException ex)` in the submit handler, rendered via `Components/Shared/ErrorList.razor` — a flat bullet list, since `ValidationException.Errors` is a flat `IReadOnlyList<string>`, not field-keyed (same shape Mvc merges into `ModelState` under an empty key).

## Image upload

`InputFile`/`IBrowserFile` streams over the SignalR circuit rather than a multipart HTTP POST, so it has its own transport-level size ceiling independent of Core's validation: `AddHubOptions(o => o.MaximumReceiveMessageSize = 6 MB)` in `Program.cs` gives headroom over Core's 5 MB/file cap. Each selected file is copied via `IBrowserFile.OpenReadStream(maxAllowedSize)` into a seekable `MemoryStream` before being wrapped as a `ListingImageUpload` — Core's `AddImagesAsync` needs a seekable stream to read the magic-byte header then rewind.

## CSRF

No extra mechanism for interactive circuits: a Blazor Server circuit is a single, already-authenticated, same-origin SignalR connection established after the page loaded under the user's own cookie — there's no discrete cross-site-forgeable request for an attacker to replay against it. Static SSR Account forms and the plain Logout `<form>` in the nav *are* ordinary HTTP POSTs and do need protection; `EditForm`/`<AntiforgeryToken />` supply it automatically, and `app.UseAntiforgery()` is the only pipeline wiring required (no custom filter/attribute the way `AgoraFold.WebApi` needed).

## Messaging

Reply-triggers-local-refresh only: posting a reply re-fetches the thread and lets normal re-rendering show the new message, no polling, no SignalR-native push. A circuit's live connection could trivially support pushed updates, but that would exceed Mvc's reference feature set and require inventing signaling plumbing Core doesn't have — explicitly a stretch goal, not a v1 deliverable.

## Open questions

- Whether pagination/filter state on Browse should sync to the URL via `NavigationManager.NavigateTo` (implemented) is worth keeping long-term vs. simplifying to component-local state only — not a blocker, current behavior gives shareable/bookmarkable browse links same as Mvc's query-string approach.
