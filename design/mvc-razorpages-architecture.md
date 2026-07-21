# AgoraFold.Mvc & AgoraFold.RazorPages Architecture

## Summary

Two front-end variants sharing the same feature set and `AgoraFold.Core` services: `AgoraFold.Mvc` (the first implemented, the reference every later variant ports its feature set from) and `AgoraFold.RazorPages` (a feature-parity port of it onto Razor Pages, own `wwwroot`/uploads, own ports, same shared Postgres DB/migrations). Neither has a forward-looking spec doc — Mvc predates the `design/` convention entirely, and RazorPages was intentionally not spec'd since a port with no new domain behavior would just restate `design/project-spec.md` and drift out of sync. This document records both variants' structure instead.

Ports: Mvc `http://localhost:5151` / `https://localhost:7127`; RazorPages `http://localhost:5153` / `https://localhost:7129`.

## AgoraFold.Mvc

- Controllers map view models to/from `AgoraFold.Core` entities and services.
- `Filters/AgoraFoldExceptionFilter` maps Core's typed exceptions to HTTP status codes (`NotFoundException` → 404, `ForbiddenException` → 403, `ValidationException` → 400), paired with `UseStatusCodePagesWithReExecute` + `HomeController.StatusCode` for friendly error pages.
- Most actions let Core's exceptions bubble to the filter rather than catching them locally. The exception: POST actions that need to redisplay a form with field errors catch `ValidationException` locally and merge it into `ModelState`, so the user's just-typed input isn't lost.
- Auth is a custom `AccountController` (not the Identity Razor Pages UI scaffold) on top of `AddIdentity<AppUser, IdentityRole>()`.
- `Listings/Index` is the site root (default route).

## AgoraFold.RazorPages

- Browse/search lives at `Pages/Index.cshtml` (Razor Pages' root-of-`Pages/`-is-`/` convention); everything else is under `Pages/Listings/`, `Pages/Account/`, `Pages/Conversations/`.
- Pages with more than one POST action (`Listings/Edit` — save fields, add images, delete image; `Conversations/Details` — reply) use named handlers (`asp-page-handler`, `OnPost<Name>Async`) on one `PageModel`, mirroring Mvc's multiple-actions-per-controller shape instead of splitting into separate pages.
- `Conversations/Details.OnPostReplyAsync` deliberately returns `Page()` (not `RedirectToPage()`) on validation failure, re-rendering the thread with the just-typed reply intact — the one place RP's usual POST-redirect-GET default is overridden, to match Mvc's non-PRG `Reply` behavior.
- `ConversationsController`'s whole-controller `[Authorize]` becomes `options.Conventions.AuthorizeFolder("/Conversations")` in `Program.cs`.
- `AgoraFoldExceptionFilter` is duplicated (not shared) into `AgoraFold.RazorPages.Filters`, registered via `options.Conventions.ConfigureFilter(new AgoraFoldExceptionFilter())` — `RazorPagesOptions` has no `.Filters` collection the way `MvcOptions` does.
- ViewModels aren't shared with Mvc — folded directly onto `PageModel`s as properties. Mvc's `[ValidateNever]` on server-populated/file fields has no RP equivalent needed: simply don't mark those properties `[BindProperty]`.
