# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A classifieds board app used as a showroom for ASP.NET web rendering models. One shared `AgoraFold.Core` (EF Core + PostgreSQL domain model) powers multiple independent front-end variants, each built with a different ASP.NET approach: MVC, Razor Pages, Blazor Server, Blazor WebAssembly, Web API + JS, and HTMX. Only the MVC variant exists so far.

This is a learning exercise / portfolio piece, not a production app — no deadline. Each variant gets the full feature set (accounts, images, messaging) before moving to the next. Planned build order: MVC → Razor Pages → Web API + JS → HTMX → Blazor.

Full domain spec and feature scope: `design/project-spec.md`.

## Commands

Postgres must be running before anything that touches the database (`dotnet run`, migrations):

```
docker compose up -d
```

Build / run:

```
dotnet build
dotnet run --project src/AgoraFold.Mvc
```

EF Core migrations (run from repo root; `dotnet-ef` is a local tool restored via `.config/dotnet-tools.json`):

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/AgoraFold.Core --startup-project src/AgoraFold.Mvc
dotnet ef database update --project src/AgoraFold.Core --startup-project src/AgoraFold.Mvc
```

There is no test project yet.

## Architecture

- **`AgoraFold.Core`** — persistence/domain only: entities (`Entities/`), `AppDbContext`, EF configuration, migrations. Must never reference MVC types (`ViewResult`, `IActionResult`, etc.) — it's reused as-is by every future front-end variant.
- **`AgoraFold.Mvc`** — the MVC front-end variant. Depends on `AgoraFold.Core`; controllers map view models to/from Core entities.
- Each future variant (Razor Pages, Blazor, Web API + JS, HTMX) will be its own project alongside `AgoraFold.Mvc`, depending on the same `AgoraFold.Core`, added to `AgoraFold.slnx`.

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

### Image storage (MVC variant)

Local filesystem under `wwwroot/uploads/listings/{listingId}/{guid}{ext}` — no cloud storage. GUID filenames avoid collisions and path traversal. Server-side validation required: file signature (magic bytes), not just extension/content-type. Limits: 5 MB/file, 8 images/listing. First image by `SortOrder` is the thumbnail. No resize pipeline for v1. Deleting a `Listing`/`ListingImage` must delete its file(s) from disk.

### Dev datastore

Postgres via `docker-compose.yml` is the only supported dev datastore (port **5433**, not the default 5432 — this avoids colliding with a native Postgres service some dev machines run on 5432). No SQLite/in-memory fallback. Connection string lives in `appsettings.json` under `ConnectionStrings:Default` and is duplicated in `DesignTimeDbContextFactory` for design-time migration commands.
