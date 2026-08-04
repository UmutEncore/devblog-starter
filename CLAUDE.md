# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

DevBlog is a minimal blog application with two independent projects in one repo:

- `backend/` — .NET 10 Web API (`DevBlog.Api`), minimal APIs, EF Core + SQLite
- `frontend/devblog-ui/` — Angular 22 standalone-component app

There is no shared build; each side is developed and run independently.

## Common commands

### Backend (`backend/`)

```bash
dotnet run --project backend/src/DevBlog.Api/DevBlog.Api.csproj   # run the API
dotnet build backend/DevBlog.slnx                                  # build
```

No test project exists yet in the solution (`backend/DevBlog.slnx` only contains `DevBlog.Api`).

EF Core migrations live in `backend/src/DevBlog.Api/Migrations/`. Migrations are applied automatically on startup (`db.Database.Migrate()` in `Program.cs`), and `DataSeeder.Seed(db)` seeds demo data (admin user + 3 posts + comments) if the `Users` table is empty. To add a new migration:

```bash
dotnet ef migrations add <Name> --project backend/src/DevBlog.Api/DevBlog.Api.csproj
```

The SQLite DB file (`devblog.db`) is created next to the running process per `appsettings.json`'s `ConnectionStrings:DefaultConnection`.

### Frontend (`frontend/devblog-ui/`)

```bash
cd frontend/devblog-ui
npm install
npm start          # ng serve
npm run build      # ng build
npm run watch      # ng build --watch --configuration development
```

No test setup (no `*.spec.ts` files, no test script) is currently configured.

## Architecture

### Backend

Minimal-API style, wired up entirely in `Program.cs`:

- Endpoints are grouped into static classes under `Endpoints/` (`PostsEndpoint`, `CommentsEndpoint`, `AuthEndpoint`), each exposing a static `Map(WebApplication app)` that registers its routes. New endpoint groups should follow this same pattern and be called from `Program.cs`.
- `Data/AppDbContext.cs` defines the EF Core model (`Users`, `Posts`, `Comments`) and relationships (`Post.Author` → `User`, `Post.Comments` → `Comment`, unique-ish index on `Post.Slug`).
- `Data/DataSeeder.cs` seeds an admin user and sample posts/comments on first run.
- Auth is JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`); only `POST /posts` currently requires authorization (`.RequireAuthorization()`). Claims carry `NameIdentifier` (user id), `Name`, and `Role`.
- OpenAPI is exposed via `AddOpenApi()`/`MapOpenApi()` in Development only, with Scalar.AspNetCore referenced for API docs UI.

Known rough edges already flagged with `TODO` comments in the code — be aware of these when touching related areas rather than treating them as new findings:
- Password hashing is a placeholder (`Convert.ToBase64String` of UTF8 bytes, in both `AuthEndpoint` and `DataSeeder`) — not real hashing.
- JWT signing secret is hardcoded in `Program.cs` and `AuthEndpoint.cs` (must stay in sync if changed) rather than read from config.
- CORS allows any origin/method/header.
- `GET /posts` has no pagination.
- `POST /posts` doesn't validate slug uniqueness.

### Frontend

Angular 22 standalone-components app (no NgModules):

- `app.config.ts` wires up router + `HttpClient` with a functional interceptor (`authInterceptor` in `services/auth.service.ts`) that attaches `Authorization: Bearer <token>` from `localStorage` to every outgoing request.
- Routes (`app.routes.ts`) are lazy-loaded via `loadComponent`: `/posts` (list), `/posts/:slug` (detail), `/login`.
- `services/post.service.ts` and `services/auth.service.ts` are the only two services; both call the backend directly through `environment.apiUrl` (no shared API client abstraction).
- `AuthService` stores the raw JWT in `localStorage` (flagged in code as TODO to move to an httpOnly cookie) and exposes `isLoggedIn()`/`getToken()` used by the interceptor and pages.
- `environment.ts` / `environment.development.ts` hold `apiUrl` pointing at the backend (`http://localhost:5000` in production config — confirm the dev value matches whatever port the API actually runs on before assuming it's correct).

### Contract between frontend and backend

The frontend's `PostSummary`/`PostDetail`/`Comment` TypeScript interfaces in `post.service.ts` must stay in sync with the anonymous projections returned by `PostsEndpoint` (`GET /posts`, `GET /posts/{slug}`) — there's no shared schema/codegen between the two projects, so changes to one side's response/request shape need a manual matching update on the other side.

## Architecture decisions (backend layering)

This is a binding convention for backend code going forward, not a description of the current state:

- Endpoints must **not** inject `AppDbContext` directly. Endpoints depend on a service.
- Services contain the business logic and depend on repositories, **not** on `AppDbContext` directly.
- Repositories are the only layer that talks to `AppDbContext`. Default to a **generic repository** implementation (e.g. `IRepository<T>` / `Repository<T>`) rather than a bespoke interface per entity; only reach for a dedicated, entity-specific repository when an entity's query needs don't fit the generic shape.

Target dependency direction: `Endpoint → Service → Repository → AppDbContext`.

### Technical debt: endpoints not yet on this architecture

None of the current endpoints follow the layering above — all of them inject `AppDbContext` directly and query it inline, with no service or repository layer in between:

- `Endpoints/PostsEndpoint.cs` — `GET /posts`, `GET /posts/{slug}`, `POST /posts`
- `Endpoints/CommentsEndpoint.cs` — `POST /posts/{slug}/comments`
- `Endpoints/AuthEndpoint.cs` — `POST /auth/login`

Treat these as technical debt, not as the pattern to copy. When adding a new endpoint, build it on a service + (generic) repository from the start. When materially touching one of the endpoints above, migrate the touched flow onto the service/repository layering rather than adding more direct `AppDbContext` usage to it.

**Testing** — there is no test project and no test strategy today (see [Common commands](#common-commands)). The expectation going forward is an xUnit test project for the backend with **70% coverage**. This is not in place yet; don't assume backend changes are covered by tests, and flag new backend work that lacks xUnit tests as debt rather than as done.

## Code style

Apply Clean Code principles across both projects:

- **DRY** — don't duplicate logic across layers. Query/business logic belongs in exactly one place (a repository method, a service method) and is called from there, not copy-pasted into each endpoint or component that needs it.
- **Naming conventions** — follow the convention already used in each language, and don't mix conventions within it: PascalCase for C# types and members (`PostsEndpoint`, `AppDbContext`, `CreatePostRequest`); camelCase for TypeScript members and functions (`getPosts`, `authInterceptor`); kebab-case for Angular file names and selectors (`post-detail.component.ts`).
- **KISS** — implement the simplest solution that satisfies the actual requirement; don't add abstraction, configurability, or generality for needs the codebase doesn't have yet.
