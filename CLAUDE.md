# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A classifieds board app used as a showroom for ASP.NET web rendering models. One shared `AgoraFold.Core` (EF Core + PostgreSQL domain model plus a business/service layer) powers multiple independent front-end variants, each built with a different ASP.NET approach: MVC, Razor Pages, Blazor Server, Blazor WebAssembly, Web API + Vue, and HTMX. MVC, Razor Pages, Web API + Vue, and HTMX are implemented so far, each with the full feature set (accounts, listings with images, browse/search, buyer-seller messaging).

This is a learning exercise / portfolio piece, not a production app — no deadline. Each variant gets the full feature set (accounts, images, messaging) before moving to the next. Planned build order: MVC → Razor Pages → Web API + Vue → HTMX → Blazor.

Full domain spec and feature scope: `design/project-spec.md`. The HTMX variant's own interaction/architecture notes: `design/htmx-spec.md`.

## Commands

Postgres must be running before anything that touches the database (`dotnet run`, migrations):

```
docker compose up -d
```

Build / run:

```
dotnet build
dotnet run --project src/AgoraFold.Mvc          # http://localhost:5151, https://localhost:7127
dotnet run --project src/AgoraFold.RazorPages   # http://localhost:5153, https://localhost:7129
dotnet run --project src/AgoraFold.WebApi       # http://localhost:5155, https://localhost:7131
dotnet run --project src/AgoraFold.Htmx         # http://localhost:5157, https://localhost:7133
dotnet run --project src/AgoraFold.BlazorServer # http://localhost:5159, https://localhost:7135
```

The Web API variant needs its Vue client running alongside it:

```
cd src/AgoraFold.Vue
npm install     # first time only
npm run dev     # http://localhost:5173
```

All variants share the same Postgres database and can run concurrently.

EF Core migrations (run from repo root; `dotnet-ef` is a local tool restored via `.config/dotnet-tools.json`; `AgoraFold.Mvc` is the only migrations startup project — `AgoraFold.RazorPages`, `AgoraFold.WebApi`, and `AgoraFold.Htmx` don't need `Microsoft.EntityFrameworkCore.Design` for this reason):

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/AgoraFold.Core --startup-project src/AgoraFold.Mvc
dotnet ef database update --project src/AgoraFold.Core --startup-project src/AgoraFold.Mvc
```

There is no test project yet.

## Architecture

- **`AgoraFold.Core`** — persistence/domain plus business logic: entities (`Entities/`), `AppDbContext`, EF configuration, migrations, and a `Services/` layer (`ICategoryService`, `IListingService`, `IListingImageService`, `IConversationService`) that implements every business operation once so each variant just maps view models to/from it. Services throw typed exceptions (`Exceptions/`: `NotFoundException`, `ForbiddenException`, `ValidationException`) rather than returning result objects. Image files are handled through an `IListingImageStorage` abstraction (`Storage/`, default `LocalDiskListingImageStorage`) — Core never references `wwwroot` or any ASP.NET-hosting type; each variant configures the storage root path itself via `AddAgoraFoldCore()` + `Configure<ListingImageStorageOptions>`. Must never reference MVC types (`ViewResult`, `IActionResult`, etc.) — it's reused as-is by every future front-end variant.
- **`AgoraFold.Mvc`** — the MVC front-end variant. Depends on `AgoraFold.Core`; controllers map view models to/from Core entities and services. A global `AgoraFoldExceptionFilter` (`Filters/`) maps Core's typed exceptions to HTTP status codes (404/403/400), paired with `UseStatusCodePagesWithReExecute` + `HomeController.StatusCode` for friendly error pages — most actions let these exceptions bubble rather than catching them locally, except POST actions that need to redisplay a form with field errors (those catch `ValidationException` and merge it into `ModelState`). Auth is a custom `AccountController` (not the Identity Razor Pages UI scaffold) on top of `AddIdentity<AppUser, IdentityRole>()`. `Listings/Index` is the site root (default route).
- **`AgoraFold.RazorPages`** — the Razor Pages front-end variant, feature-parity port of the MVC variant onto Razor Pages (own `wwwroot`/uploads, own ports, same shared Postgres DB/migrations). Browse/search lives at `Pages/Index.cshtml` (RP's root-of-`Pages/`-is-`/` convention); everything else under `Pages/Listings/`, `Pages/Account/`, `Pages/Conversations/`. Pages with more than one POST action (`Listings/Edit` — save fields, add images, delete image; `Conversations/Details` — reply) use named handlers (`asp-page-handler`, `OnPost<Name>Async`) on one `PageModel` rather than separate pages, mirroring Mvc's multiple-actions-per-controller. `Conversations/Details.OnPostReplyAsync` deliberately returns `Page()` (not `RedirectToPage()`) on validation failure, re-rendering the thread with the just-typed reply intact — the one place RP's usual POST-redirect-GET default is overridden, to match Mvc's non-PRG `Reply` behavior. `ConversationsController`'s whole-controller `[Authorize]` becomes `options.Conventions.AuthorizeFolder("/Conversations")` in `Program.cs`. `AgoraFoldExceptionFilter` is duplicated (not shared) into `AgoraFold.RazorPages.Filters`, registered via `options.Conventions.ConfigureFilter(new AgoraFoldExceptionFilter())` (`RazorPagesOptions` has no `.Filters` collection the way `MvcOptions` does). ViewModels aren't shared with Mvc — folded directly onto PageModels as properties (Mvc's `[ValidateNever]` on server-populated/file fields has no RP equivalent needed: simply don't mark those properties `[BindProperty]`).
- **Static files gotcha**: Mvc's and RazorPages' `Program.cs` register `app.UseStaticFiles()` and `app.MapStaticAssets()` — not redundant. `MapStaticAssets()` only serves assets known at build time from a manifest; it does NOT serve listing images uploaded at runtime under `wwwroot/uploads/listings/`, so `UseStaticFiles()` must stay for those to be servable. `AgoraFold.WebApi` has no build-time static assets of its own, so it only needs `UseStaticFiles()`.
- **`AgoraFold.WebApi`** + **`AgoraFold.Vue`** — the Web API + Vue variant, split into a JSON API project and a separate Vite/Vue 3 + TypeScript SPA that consumes it; the SPA is not a .NET project and isn't added to `AgoraFold.slnx`. Type-checking is a separate `vue-tsc -b` project-reference build (`tsconfig.json` → `tsconfig.app.json` for `src/`, `tsconfig.node.json` for `vite.config.ts`), run before `vite build` via the `build` npm script; shared DTO shapes live in `src/api/types.ts`. Controllers (`Controllers/`) map DTOs (`Models/`, organized by feature) to/from the same `AgoraFold.Core` services Mvc/RazorPages use. Auth stays cookie-based `AddIdentity`, but `ConfigureApplicationCookie`'s `OnRedirectToLogin`/`OnRedirectToAccessDenied` events are overridden to return raw 401/403 instead of Identity's default 302-to-a-login-page — without this, every `[Authorize]` failure would come back as a redirect the SPA can't sensibly follow. CORS (`AddCors`/`UseCors`, before `UseAuthentication`) allows the Vue dev origin with `AllowCredentials()`. CSRF parity with Mvc/RazorPages's `[ValidateAntiForgeryToken]` uses a custom `Filters/ValidateCsrfTokenAttribute` instead of the framework attribute — the framework's `[ValidateAntiForgeryToken]` resolves DI services that only `AddControllersWithViews`/`AddMvc` register, which this API-only (`AddControllers`) project doesn't have; the custom filter calls `IAntiforgery.ValidateRequestAsync` directly. A `GET /api/antiforgery/token` endpoint (`AntiforgeryController`) hands the Vue client a token to echo back as `X-CSRF-TOKEN`. **Non-obvious gotcha**: ASP.NET's antiforgery token is bound to whichever identity (anonymous or a specific user) was active when it was issued, so the Vue client (`src/api/client.ts`) fetches a fresh token before every mutating request rather than caching one — a token cached from before login/register/logout gets rejected on the next mutating call once the identity changes.
- **`AgoraFold.Htmx`** — the HTMX front-end variant, MVC-paired (controllers + Razor views, own `wwwroot`/uploads, own ports) like `AgoraFold.Mvc`, but interaction-heavy flows swap HTML fragments in place instead of doing full page navigations; `htmx.min.js` is vendored under `wwwroot/lib/htmx` like Bootstrap/jQuery rather than pulled from a CDN. Full interaction-pattern rationale: `design/htmx-spec.md`. Controllers detect an htmx-issued request via the `HX-Request` header (`Controllers/HttpRequestExtensions.IsHtmx()`) and return `PartialView()` instead of `View()` for actions that serve both a full page and a fragment (`Listings/Index`'s live search/category filter/pagination, swapped into `#results`). Destructive actions (`DeleteListing`, image delete) use `hx-confirm` for the browser's native confirm prompt, and `DeleteListing` responds with the `HX-Redirect` header (htmx does a full `window.location` navigation) rather than a normal MVC redirect when htmx-issued, falling back to a real `RedirectToAction` otherwise so the same action still works with JS/htmx disabled. `Listings/Edit`'s image gallery (`Views/Listings/_Gallery.cshtml`) is swapped as one `outerHTML` unit on upload/delete instead of a full page reload. Conversation replies append via `hx-swap="beforeend"`; the thread polls `Conversations/Poll` every 5s (`hx-trigger="every 5s"`), which returns only messages newer than a client-tracked `sinceId` — both the poll and a successful reply carry an out-of-band swap (`hx-swap-oob`) updating a hidden `#last-message-id` marker the next poll reads via `hx-vals='js:{...}'`, plus a `hx-swap-oob="delete"` to remove the "no messages yet" placeholder once real messages exist. A failed reply (validation error) is retargeted into `#reply-error` via the `HX-Retarget`/`HX-Reswap` response headers, since a successful reply and a failed one need to land in different places from the same endpoint. CSRF: `AntiforgeryOptions.HeaderName` is configured so the standard `[ValidateAntiForgeryToken]` accepts a header, not just a form field — `wwwroot/js/site.js`'s `htmx:configRequest` listener attaches the token (read from a `<meta name="csrf-token">` tag `_Layout.cshtml` renders fresh per request) to every htmx-issued request, covering bare `hx-post`/`hx-delete` buttons that have no surrounding `<form>` to carry a hidden antiforgery field. **Non-obvious gotcha** (same root cause as the Vue client's, see above): that antiforgery token is bound to the identity active when the page was rendered, so a tab left open across a login/logout in *another* tab (shared cookies, same browser) will get its next htmx POST/DELETE rejected with 400 until the page is reloaded — reload to fetch a token matching the current identity.
- **`AgoraFold.BlazorServer`** — the Blazor Server front-end variant, built on the .NET 8+ "Blazor Web App" template shape (Interactive Server render mode over a persistent SignalR circuit, not the legacy pre-.NET-8 "Blazor Server" template), own `wwwroot`/uploads, own ports. Full rationale: `design/blazor-server-spec.md`. Most pages carry `@rendermode InteractiveServer`; `Components/Account/Pages/Login.razor` and `Register.razor` stay static SSR (no `@rendermode`) because signing in/out writes a `Set-Cookie` header, which needs a real HTTP response — unavailable from inside a live circuit. Logout is a plain minimal API endpoint (`Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`), not a routable component, for the same reason. Core's services (and `AppDbContext`) stay `Scoped` unchanged — in Blazor Server that's per-circuit, not per-request, a deliberate trade-off (not an oversight) explained in the design doc; `IDbContextFactory` was considered and rejected. No MVC-filter equivalent exists for error handling: GET-style loads catch `NotFoundException`/`ForbiddenException` explicitly in `OnParametersSetAsync` and render them via `Components/Shared/DomainErrorAlert.razor` — **not** an `<ErrorBoundary>`, which only catches exceptions thrown by its descendants, never by the ancestor page hosting it (an earlier `ErrorBoundary`-based attempt silently failed to catch a 404). **Non-obvious gotchas**: (1) component attributes bound to `string`-typed parameters need an explicit `@` prefix to be treated as C# expressions — `Title="listing.Title"` silently renders the literal text, not the property value; (2) `@(x).Method()` only wraps the parenthesized part in the C# expression, so a trailing method chain after the closing paren renders as literal text; (3) `App.razor` needs an explicit `<base href="/" />` or relative asset paths 404 on any route deeper than `/`; (4) `app.UseRouting()` must be explicit and placed after `app.UseStaticFiles()` — otherwise ASP.NET Core auto-inserts routing at the very front of the pipeline, so endpoint matching runs before `UseStaticFiles()` can serve the runtime-written files under `wwwroot/uploads/listings/`; (5) a reusable child component must not independently query a Scoped-DbContext-backed service if it can render concurrently with a parent's own in-flight query — `Components/Shared/CategorySelect.razor` takes categories as a `[Parameter]` instead of self-fetching, for exactly this reason.
- Each future variant (Blazor WebAssembly) will be its own project alongside the existing ones, depending on the same `AgoraFold.Core` (services included), added to `AgoraFold.slnx`.

### Domain model

`User` (`AppUser : IdentityUser`) owns `Listing`s, which belong to a `Category` and have many `ListingImage`s. A `Conversation` is scoped to one `Listing`, between the listing's owner and one other `User`, and contains `Message`s.

```mermaid
erDiagram
    User ||--o{ Listing : owns
    Category ||--o{ Listing : contains
    Listing ||--o{ ListingImage : has
    Listing ||--o{ Conversation : "discussed in"
    User ||--o{ Conversation : participates
    Conversation ||--o{ Message : contains
    User ||--o{ Message : sends
```

- Auth is ASP.NET Identity (`IdentityDbContext<AppUser>`), cookie-based.
- EF table/column names are snake_case (`EFCore.NamingConventions`, `UseSnakeCaseNamingConvention()` — applied both in `Program.cs` and `DesignTimeDbContextFactory`).
- `Listing.Price` is `numeric(18,2)`, nullable.
- Delete behavior: `ListingImage`, `Conversation`, `Message` cascade from their parent; `Listing.Owner`, `Conversation.Participant`, `Message.Sender` are `Restrict` (users aren't deleted out from under their content).
- `Category` rows are seeded via migration `HasData` (fixed IDs 1–7) — a listing always needs a valid `CategoryId`, so this seed is required for the app to be usable, not just sample data.

### Image storage

Local filesystem under `wwwroot/uploads/listings/{listingId}/{guid}{ext}`, one such folder per hosting project's own `wwwroot` (each variant has its own, not shared) — no cloud storage. GUID filenames avoid collisions and path traversal. Server-side validation required: file signature (magic bytes), not just extension/content-type. Limits: 5 MB/file, 8 images/listing. First image by `SortOrder` is the thumbnail. No resize pipeline for v1. Deleting a `Listing`/`ListingImage` must delete its file(s) from disk.

### Dev datastore

Postgres via `docker-compose.yml` is the only supported dev datastore (port **5433**, not the default 5432 — this avoids colliding with a native Postgres service some dev machines run on 5432). No SQLite/in-memory fallback. Connection string lives in `appsettings.json` under `ConnectionStrings:Default` and is duplicated in `DesignTimeDbContextFactory` for design-time migration commands.
