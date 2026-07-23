# agorafold

One EF Core/PostgreSQL backend, many ASP.NET frontends — a classifieds board built across MVC, Razor Pages, Blazor, HTMX, and Web API (with React, Svelte, Angular, and SolidJS frontends).

## What is this?

A classifieds board app used as a showroom for ASP.NET web rendering models. A shared EF Core + PostgreSQL core powers multiple independent front-end variants, each built with a different ASP.NET approach.

## Variants

| Variant | Description |
|---|---|
| ASP.NET MVC Core | Traditional MVC with server-rendered views |
| Razor Pages | Page-centric server rendering |
| Blazor Server | Interactive server-side components |
| Blazor WebAssembly | Client-side .NET SPA |
| Web API + Vue | REST API with a Vue 3 + TypeScript frontend |
| Web API + React | REST API with a React frontend |
| Web API + Svelte | REST API with a Svelte frontend |
| Web API + Angular | REST API with an Angular frontend |
| Web API + SolidJS | REST API with a SolidJS frontend |
| HTMX | Hypermedia-driven progressive enhancement |

## Running the projects

Start PostgreSQL before running any project:

```sh
docker compose up -d
```

Build the solution with `dotnet build`, then run any server-rendered variant:

```sh
dotnet run --project src/AgoraFold.Mvc          # http://localhost:5151
dotnet run --project src/AgoraFold.RazorPages   # http://localhost:5153
dotnet run --project src/AgoraFold.WebApi       # http://localhost:5155
dotnet run --project src/AgoraFold.Htmx         # http://localhost:5157
dotnet run --project src/AgoraFold.BlazorServer # http://localhost:5159
dotnet run --project src/AgoraFold.BlazorWasm   # http://localhost:5161
```

The JavaScript clients use the shared Web API. Run `AgoraFold.WebApi` first, then start the client you want (run `npm install` once per client before its first run):

```sh
cd src/AgoraFold.Vue && npm run dev       # http://localhost:5173
cd src/AgoraFold.React && npm run dev     # http://localhost:5174
cd src/AgoraFold.Svelte && npm run dev    # http://localhost:5175
cd src/AgoraFold.Angular && npm run start # http://localhost:5176
cd src/AgoraFold.SolidJS && npm run dev   # http://localhost:5177
```

## Repo layout

- `src/` — the core library and all ten front-end variant projects
- `tools/` — local Identity admin utilities (CLI and TUI), not one of the showroom variants
- `design/` — architecture notes, project spec, and backlog ([contents](design/README.md))
- `docker-compose.yml` — local Postgres for development
- `AgoraFold.slnx` — solution file
- `AGENTS.md` — repo instructions for coding agents (Claude Code, Codex, etc.)

## License

MIT — see [LICENSE](LICENSE).
