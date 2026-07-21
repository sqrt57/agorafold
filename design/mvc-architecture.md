# AgoraFold.Mvc Architecture

## Summary

The first implemented front-end variant: ASP.NET Core MVC (controllers + Razor views). It's the reference implementation — every later variant ports its feature set from this one. Ports: `http://localhost:5151`, `https://localhost:7127`.

No separate spec doc exists for this variant — it predates the `design/` convention established for later variants, and its full feature scope is `design/project-spec.md` itself.

## Structure

- Controllers map view models to/from `AgoraFold.Core` entities and services.
- `Filters/AgoraFoldExceptionFilter` maps Core's typed exceptions to HTTP status codes (`NotFoundException` → 404, `ForbiddenException` → 403, `ValidationException` → 400), paired with `UseStatusCodePagesWithReExecute` + `HomeController.StatusCode` for friendly error pages.
- Most actions let Core's exceptions bubble to the filter rather than catching them locally. The exception: POST actions that need to redisplay a form with field errors catch `ValidationException` locally and merge it into `ModelState`, so the user's just-typed input isn't lost.
- Auth is a custom `AccountController` (not the Identity Razor Pages UI scaffold) on top of `AddIdentity<AppUser, IdentityRole>()`.
- `Listings/Index` is the site root (default route).
