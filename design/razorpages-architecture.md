# AgoraFold.RazorPages Architecture

## Summary

A feature-parity port of `AgoraFold.Mvc` onto Razor Pages — own `wwwroot`/uploads, own ports (`http://localhost:5153`, `https://localhost:7129`), same shared Postgres DB/migrations. No new domain behavior versus Mvc, only a different rendering model.

No separate spec doc was written for this variant: it's a pure feature-parity port with no new domain behavior, so a spec would just restate `design/project-spec.md` and drift out of sync. This document records the RP-specific structural choices instead.

## Structure

- Browse/search lives at `Pages/Index.cshtml` (Razor Pages' root-of-`Pages/`-is-`/` convention); everything else is under `Pages/Listings/`, `Pages/Account/`, `Pages/Conversations/`.
- Pages with more than one POST action (`Listings/Edit` — save fields, add images, delete image; `Conversations/Details` — reply) use named handlers (`asp-page-handler`, `OnPost<Name>Async`) on one `PageModel`, mirroring Mvc's multiple-actions-per-controller shape instead of splitting into separate pages.
- `Conversations/Details.OnPostReplyAsync` deliberately returns `Page()` (not `RedirectToPage()`) on validation failure, re-rendering the thread with the just-typed reply intact — the one place RP's usual POST-redirect-GET default is overridden, to match Mvc's non-PRG `Reply` behavior.
- `ConversationsController`'s whole-controller `[Authorize]` becomes `options.Conventions.AuthorizeFolder("/Conversations")` in `Program.cs`.
- `AgoraFoldExceptionFilter` is duplicated (not shared) into `AgoraFold.RazorPages.Filters`, registered via `options.Conventions.ConfigureFilter(new AgoraFoldExceptionFilter())` — `RazorPagesOptions` has no `.Filters` collection the way `MvcOptions` does.
- ViewModels aren't shared with Mvc — folded directly onto `PageModel`s as properties. Mvc's `[ValidateNever]` on server-populated/file fields has no RP equivalent needed: simply don't mark those properties `[BindProperty]`.
