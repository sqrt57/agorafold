# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A classifieds board app used as a showroom for ASP.NET web rendering models. One shared `AgoraFold.Core` (EF Core + PostgreSQL domain model plus a business/service layer) powers multiple independent front-end variants, each built with a different ASP.NET approach: MVC, Razor Pages, Blazor Server, Blazor WebAssembly, Web API + Vue, HTMX, and Web API paired with React, Svelte, Angular, and SolidJS frontends. MVC, Razor Pages, Web API + Vue, HTMX, Blazor Server, and Blazor WebAssembly are implemented so far, each with the full feature set (accounts, listings with images, browse/search, buyer-seller messaging).

This is a learning exercise / portfolio piece, not a production app — no deadline. Each variant gets the full feature set (accounts, images, messaging) before moving to the next. Planned build order: MVC → Razor Pages → Web API + Vue → HTMX → Blazor Server → Blazor WebAssembly → Web API + React → Web API + Svelte → Web API + Angular → Web API + SolidJS.

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
- **`AgoraFold.Mvc`** + **`AgoraFold.RazorPages`** — the MVC front-end variant (first implemented, the reference other variants port for feature parity) and its feature-parity Razor Pages port. Full architecture notes for both: `design/mvc-razorpages-architecture.md`.
- **Static files gotcha**: Mvc's and RazorPages' `Program.cs` register `app.UseStaticFiles()` and `app.MapStaticAssets()` — not redundant. `MapStaticAssets()` only serves assets known at build time from a manifest; it does NOT serve listing images uploaded at runtime under `wwwroot/uploads/listings/`, so `UseStaticFiles()` must stay for those to be servable. `AgoraFold.WebApi` has no build-time static assets of its own, so it only needs `UseStaticFiles()`.
- **`AgoraFold.WebApi`** + **`AgoraFold.Vue`** — the Web API + Vue variant: a JSON API project plus a separate Vite/Vue 3 + TypeScript SPA (not a `.slnx` project) that consumes it. The backend's auth/CORS/CSRF design is shared by every future Web API + JS frontend (React, Svelte, Angular, SolidJS planned), not just Vue. Full architecture notes: `design/webapi-architecture.md`.
- **`AgoraFold.Htmx`** — the HTMX front-end variant, MVC-paired (controllers + Razor views, own `wwwroot`/uploads, own ports) like `AgoraFold.Mvc`, but interaction-heavy flows swap HTML fragments in place instead of doing full page navigations. Full architecture notes: `design/htmx-architecture.md`.
- **`AgoraFold.BlazorServer`** — the Blazor Server front-end variant, built on the .NET 8+ "Blazor Web App" template shape (Interactive Server render mode over a persistent SignalR circuit, not the legacy pre-.NET-8 "Blazor Server" template), own `wwwroot`/uploads, own ports. Full architecture notes and gotchas: `design/blazor-server-architecture.md`.
- **`AgoraFold.BlazorWasm`** + **`AgoraFold.BlazorWasm.Client`** — the Blazor WebAssembly front-end variant: a hosted-WASM project pair (`dotnet new blazor --interactivity WebAssembly` shape), unlike the future React/Svelte/Angular/SolidJS variants, gets its own backend referencing `AgoraFold.Core` directly rather than consuming `AgoraFold.WebApi`. `AgoraFold.BlazorWasm` (host) owns Identity/`AppDbContext`/`AddAgoraFoldCore()` plus a hand-rolled JSON API mirroring `AgoraFold.WebApi`'s controllers/CSRF pattern; `AgoraFold.BlazorWasm.Client` is the browser-side WASM assembly (pages, HttpClient-based API wrappers, a custom `AuthenticationStateProvider`) with no reference to Core. Full architecture notes and gotchas: `design/blazor-wasm-architecture.md`.
- Each future variant will be its own project alongside the existing ones, added to `AgoraFold.slnx`. The React, Svelte, Angular, and SolidJS variants are frontend-only SPAs analogous to `AgoraFold.Vue` — each its own `src/AgoraFold.<Framework>` client (not a `.slnx` project, same as Vue) consuming the existing `AgoraFold.WebApi` rather than getting its own API backend.

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
