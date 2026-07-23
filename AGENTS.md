# AgoraFold repository instructions

These instructions apply to all coding agents working in this repository, including Codex and Claude Code.

## What this is

A classifieds board app used as a showroom for ASP.NET web rendering models. One shared `AgoraFold.Core` (EF Core + PostgreSQL domain model plus a business/service layer) powers multiple independent front-end variants, each built with a different ASP.NET approach: MVC, Razor Pages, Blazor Server, Blazor WebAssembly, HTMX, and Web API paired with Vue, React, Svelte, Angular, and SolidJS frontends. All ten variants are implemented, each with the full feature set (accounts, listings with images, browse/search, buyer-seller messaging).

This is a learning exercise / portfolio piece, not a production app — no deadline. Each variant got the full feature set (accounts, images, messaging) before moving to the next. Build order: MVC → Razor Pages → Web API + Vue → HTMX → Blazor Server → Blazor WebAssembly → Web API + React → Web API + Svelte → Web API + Angular → Web API + SolidJS. All variants are now built.

Full domain spec and feature scope: `design/project-spec.md`. Each variant's bullet below links to its own design doc for deeper detail.

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
dotnet run --project src/AgoraFold.BlazorWasm   # http://localhost:5161, https://localhost:7137
```

The Web API variant needs a JS client running alongside it — Vue and React can run concurrently against the same `AgoraFold.WebApi`:

```
cd src/AgoraFold.Vue
npm install     # first time only
npm run dev     # http://localhost:5173

cd src/AgoraFold.React
npm install     # first time only
npm run dev     # http://localhost:5174

cd src/AgoraFold.Svelte
npm install     # first time only
npm run dev     # http://localhost:5175

cd src/AgoraFold.Angular
npm install     # first time only
npm run start   # http://localhost:5176

cd src/AgoraFold.SolidJS
npm install     # first time only
npm run dev     # http://localhost:5177
```

All variants share the same Postgres database and can run concurrently.

EF Core migrations (run from repo root; `dotnet-ef` is a local tool restored via `.config/dotnet-tools.json`; `AgoraFold.Mvc` is the only migrations startup project — `AgoraFold.RazorPages`, `AgoraFold.WebApi`, and `AgoraFold.Htmx` don't need `Microsoft.EntityFrameworkCore.Design` for this reason):

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/AgoraFold.Core --startup-project src/AgoraFold.Mvc
dotnet ef database update --project src/AgoraFold.Core --startup-project src/AgoraFold.Mvc
```

Tests: `dotnet test` runs `tests/AgoraFold.LiveChat.Tests` (xunit); the Vue client has `npm test` (vitest) in `src/AgoraFold.Vue`. After code changes, run `dotnet build` plus the relevant tests, and the relevant frontend build or development command when applicable.

## Architecture

- **`AgoraFold.Core`** — persistence/domain plus business logic: entities (`Entities/`), `AppDbContext`, EF configuration, migrations, and a `Services/` layer (`ICategoryService`, `IListingService`, `IListingImageService`, `IConversationService`) that implements every business operation once so each variant just maps view models to/from it. Services throw typed exceptions (`Exceptions/`: `NotFoundException`, `ForbiddenException`, `ValidationException`) rather than returning result objects. Image files are handled through an `IListingImageStorage` abstraction (`Storage/`, default `LocalDiskListingImageStorage`) — Core never references `wwwroot` or any ASP.NET-hosting type; each variant configures the storage root path itself via `AddAgoraFoldCore()` + `Configure<ListingImageStorageOptions>`. **Core must never reference MVC types** (`ViewResult`, `IActionResult`, etc.); it is reused as-is by every future front-end variant.
- **`AgoraFold.LiveChat`** — shared hosting-layer project for live conversation-thread updates, extracted from `AgoraFold.WebApi`'s original WebSocket transport (Phase 1 of `design/live-chat-porting-plan.md`). References `AgoraFold.Core` plus the ASP.NET Core shared framework via `<FrameworkReference>`; Core never references it. Owns the wire protocol records (`Protocol/`), the bounded-queue connection manager and native WebSocket endpoint (`Transport/`), a pluggable origin-check abstraction (`Origin/ILiveChatOriginPolicy`, with an allowlist `ConfiguredOriginPolicy` and a `SameOriginPolicy` for later same-origin hosts), and an `IConversationEventPublisher` abstraction hosts publish through after a persisted reply. Registered via `AddLiveChat()` + `MapConversationLiveChatEndpoint()`. Currently powers only `AgoraFold.WebApi`; the other nine variants port to it in later phases per `design/live-chat-porting-plan.md`.
- **`AgoraFold.Mvc`** + **`AgoraFold.RazorPages`** — the MVC front-end variant (first implemented, the reference other variants port for feature parity) and its feature-parity Razor Pages port. Full architecture notes for both: `design/mvc-razorpages-architecture.md`.
- **Static files gotcha**: Mvc's and RazorPages' `Program.cs` register `app.UseStaticFiles()` and `app.MapStaticAssets()` — not redundant. `MapStaticAssets()` only serves assets known at build time from a manifest; it does NOT serve listing images uploaded at runtime under `wwwroot/uploads/listings/`, so `UseStaticFiles()` must stay for those to be servable. `AgoraFold.WebApi` has no build-time static assets of its own, so it only needs `UseStaticFiles()`.
- **`AgoraFold.WebApi`** + **`AgoraFold.Vue`** + **`AgoraFold.React`** + **`AgoraFold.Svelte`** + **`AgoraFold.Angular`** + **`AgoraFold.SolidJS`** — the Web API + JS variants: a shared JSON API project plus separate SPA clients (Vite/Vue 3 + TypeScript, Vite/React + TypeScript, Vite/Svelte + TypeScript, Angular CLI/Angular + TypeScript, and Vite/Solid + TypeScript — none are `.slnx` projects) that consume it. The backend's auth/CORS/CSRF design (`Cors:JsClientOrigins` in `appsettings.json`) is shared across every Web API + JS frontend, not just one client — each new client's dev origin gets added to that array. `AgoraFold.React` is a straight feature-parity port of `AgoraFold.Vue` (same views, same `api/*.ts` client layer ported near-verbatim, same global `style.css`), just idiomatic React (Context+hooks instead of Pinia, React Router instead of vue-router). `AgoraFold.Svelte` is the corresponding Svelte feature-parity client, using Svelte stores and a small history-based router. `AgoraFold.Angular` is the corresponding Angular feature-parity client, using standalone components, signals for local/auth state, injectable services for the `api/*.ts` port, and the Angular Router (with functional `CanActivateFn` guards) in place of the hand-rolled router the other JS clients use. `AgoraFold.SolidJS` is the corresponding Solid feature-parity client (last of the JS variants), using `createSignal`/`createContext` for auth state and `@solidjs/router` (`<Router root={...}>`, `<A>`, `<Navigate>`) in place of the other clients' routers. Full architecture notes: `design/webapi-architecture.md`.
- **`AgoraFold.Htmx`** — the HTMX front-end variant, MVC-paired (controllers + Razor views, own `wwwroot`/uploads, own ports) like `AgoraFold.Mvc`, but interaction-heavy flows swap HTML fragments in place instead of doing full page navigations. Full architecture notes: `design/htmx-architecture.md`.
- **`AgoraFold.BlazorServer`** — the Blazor Server front-end variant, built on the .NET 8+ "Blazor Web App" template shape (Interactive Server render mode over a persistent SignalR circuit, not the legacy pre-.NET-8 "Blazor Server" template), own `wwwroot`/uploads, own ports. Full architecture notes and gotchas: `design/blazor-server-architecture.md`.
- **`AgoraFold.BlazorWasm`** + **`AgoraFold.BlazorWasm.Client`** — the Blazor WebAssembly front-end variant: a hosted-WASM project pair (`dotnet new blazor --interactivity WebAssembly` shape), unlike the Angular/SolidJS variants, gets its own backend referencing `AgoraFold.Core` directly rather than consuming `AgoraFold.WebApi`. `AgoraFold.BlazorWasm` (host) owns Identity/`AppDbContext`/`AddAgoraFoldCore()` plus a hand-rolled JSON API mirroring `AgoraFold.WebApi`'s controllers/CSRF pattern; `AgoraFold.BlazorWasm.Client` is the browser-side WASM assembly (pages, HttpClient-based API wrappers, a custom `AuthenticationStateProvider`) with no reference to Core. Full architecture notes and gotchas: `design/blazor-wasm-architecture.md`.
- All ten planned variants are now built. The React, Svelte, Angular, and SolidJS variants are frontend-only SPAs analogous to `AgoraFold.Vue` — each its own `src/AgoraFold.<Framework>` client (not a `.slnx` project, same as Vue) consuming the existing `AgoraFold.WebApi` rather than getting its own API backend.
- **`tools/AgoraFold.Admin`** + **`tools/AgoraFold.Admin.Tui`** — local Identity administration utilities (list/add/activate/deactivate/set-password/delete users), not one of the ten showroom variants. Both reference `AgoraFold.Core` directly (own `UserManager<AppUser>`/`AppDbContext` wiring via `AddAgoraFoldAdmin()` in `AdminServices.cs`, relaxed dev-only password policy) rather than going through `AgoraFold.WebApi`. `AgoraFold.Admin` is a plain console CLI; `AgoraFold.Admin.Tui` is an interactive Terminal.Gui front-end over the same `AdminUserService` — run with `dotnet run --project tools/AgoraFold.Admin.Tui`.

### Domain model

Full ERD and entity relationships: `design/project-spec.md`.

- Auth is ASP.NET Identity (`IdentityDbContext<AppUser>`), cookie-based.
- EF table/column names are snake_case (`EFCore.NamingConventions`, `UseSnakeCaseNamingConvention()` — applied both in `Program.cs` and `DesignTimeDbContextFactory`).
- `Listing.Price` is `numeric(18,2)`, nullable.
- Delete behavior: `ListingImage`, `Conversation`, `Message` cascade from their parent; `Listing.Owner`, `Conversation.Participant`, `Message.Sender` are `Restrict` (users aren't deleted out from under their content).
- `Category` rows are seeded via migration `HasData` (fixed IDs 1–7) — a listing always needs a valid `CategoryId`, so this seed is required for the app to be usable, not just sample data.

### Image storage

Local filesystem under `wwwroot/uploads/listings/{listingId}/{guid}{ext}` — one such folder per hosting project's own `wwwroot` (each variant has its own, not shared). Full storage rules (limits, validation, thumbnail logic): `design/project-spec.md`.

### Dev datastore

Postgres via `docker-compose.yml` is the only supported dev datastore (port **5433**, not the default 5432 — this avoids colliding with a native Postgres service some dev machines run on 5432). No SQLite/in-memory fallback. Connection string lives in `appsettings.json` under `ConnectionStrings:Default` and is duplicated in `DesignTimeDbContextFactory` for design-time migration commands.
