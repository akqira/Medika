# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## How we work together

### Running the app

Ports:
- **Frontend**: `https://localhost:5000` — Vite dev server, HTTPS via local cert in `apps/frontend/.cert/`
- **Backend**: `https://localhost:5100` — .NET HTTPS profile (`launchSettings.json` profile `https`)

Start both, each watching their own output:

```powershell
# Backend — watch API output
dotnet run --project apps/backend/src/Medika.Api --launch-profile https

# Frontend — watch Vite/SvelteKit logs
cd apps/frontend
pnpm dev
```

**"Show me the app"** → run both commands above in background (watching output), then open the browser:

```powershell
Start-Process "https://localhost:5000"
```

Wait for both servers to print their "ready" lines before opening the browser.

Never run `dotnet build` while Visual Studio has the backend open — DLLs are locked and the build will fail with MSB3027 errors. Use VS hot-reload or restart the project instead.

### Task sequencing

- Fix bugs **before** adding features or setting up infrastructure.
- When a request has multiple parts, complete and verify each part before moving to the next.
- For bugs: read the error message + relevant file(s) first. Don't open files speculatively.

### Diagnosing bugs efficiently

Before reading any code, ask for (or check):
1. The exact error message / network response body
2. Browser console output for frontend issues
3. What the user has already checked

For frontend form bugs: check whether all field `name` attributes survive to submission (multi-step forms with `{#if}` blocks remove inputs from the DOM — use persistent hidden inputs).

For CSS/overlay bugs: check stacking contexts before assuming a JS/framework issue. A `position: fixed` element with `z-index` creates a stacking context — child z-indexes are relative to it, not the page.

### Catch blocks

Always capture and log the real error — never `catch {}` or `catch (e) { /* swallow */ }`:

```typescript
} catch (e) {
    console.error('[context]', e);
    return fail(500, { error: e instanceof Error ? e.message : 'Erreur inattendue.' });
}
```

---

## Commands

### Frontend (`apps/frontend/`)

```bash
pnpm dev          # dev server on http://localhost:5173
pnpm build        # production build
pnpm check        # svelte-check + TS type checking
pnpm check:watch  # same, watch mode
```

Run from the repo root or from `apps/frontend/` — pnpm workspace resolves either way.

### Backend (`apps/backend/`)

```bash
dotnet run --project apps/backend/src/Medika.Api
# Swagger UI → http://localhost:5000/swagger
```

Build the whole solution: `dotnet build apps/backend/Medika.slnx`

There are **no tests** in the project yet.

---

## Architecture

### Monorepo

```
apps/
  backend/     .NET 10 solution (Medika.slnx)
  frontend/    SvelteKit 2 app (pnpm)
```

Only `apps/frontend` is listed in `pnpm-workspace.yaml`.

### Backend — Clean Architecture

```
Medika.Domain/        Entities, value objects, domain events, repository interfaces
Medika.Application/   CQRS commands/queries + handlers (FastEndpoints command bus — NOT MediatR), interfaces
Medika.Infrastructure/ MongoDB repos, JWT, Cloudflare R2 storage, audit
Medika.Api/           FastEndpoints endpoints, Program.cs, GlobalExceptionHandler
```

### Multi-tenancy — cabinet scoping (non-negotiable, ported from eGestion)

- Every business document (patients, appointments, consultations, invoices, charges, users) carries `cabinetId`. It comes from the **JWT claim** `cabinetId` (via `ICurrentUserService.CabinetId`) — never from request body or URL.
- `cabinetId` is ALWAYS the **first parameter** of repository query methods, the **first filter clause** in Mongo queries, and the **first field** in compound indexes.
- Handlers must guard: empty `CabinetId` claim → `UnauthorizedAccessException` ("please re-login"). Cross-cabinet load-by-id → `KeyNotFoundException` (404, no information leak).
- Invoice numbers (`F-YYYY-NNN`) are unique **per cabinet** (compound unique index `cabinetId + number`).
- `MongoDbInitializer.BackfillCabinetIdAsync` stamps legacy documents with the first doctor's cabinetId at startup (idempotent).
- After deploying this change, existing JWTs lack the claim — users must re-login.

### Validation convention

- Input validation = FluentValidation via `FastEndpoints.Validator<TRequest>` classes, auto-discovered and run by the pipeline (400 ProblemDetails). Never validate manually in `HandleAsync`.
- Validators must target the endpoint's **request type** (most endpoints use the Application Command directly as request DTO).
- Business-rule validation (status transitions, ownership) stays in domain methods / handlers.

- Endpoints inherit `Endpoint<TReq, TRes>` (FastEndpoints convention).
- MongoDB Atlas for persistence (dev DB: `medika_dev`). Startup auto-initialises indexes and seeds data via `MongoDbInitializer`.
- JWT Bearer auth. Rate limiter on login (5 req/min fixed window).
- CORS allowed origins configured via `Cors:AllowedOrigins` in appsettings; defaults to `http://localhost:5173`.

### Request security (pattern ported from eGestion, see its ADR-008)

- `ApiKeyMiddleware` (`Medika.Api/Middleware/`): **all** endpoints (including login) require `X-API-KEY` matching `ApiSettings:ApiKey` — the .NET API is only callable by our BFF. Exceptions: `/api/health`, `/swagger`, OPTIONS preflight.
- All non-login endpoints additionally require `X-Request-Timestamp` (ISO 8601, rejected if > 5 min from UtcNow — anti-replay).
- JWT is signed **only by the .NET API**; the secret lives in `appsettings.Development.json` (gitignored) locally and Azure App Service settings in prod — never in Vercel or `.env`.
- `ApiSettings:ApiKey` (backend) must equal `API_SECRET` (frontend `.env` / Vercel env var). Neither may be committed or `PUBLIC_`-prefixed.
- Frontend: **every** backend call goes through `src/lib/server/remoteApiProxy.ts` (`RemoteApi`), which injects API key + timestamp + Bearer token. `src/lib/server/api.ts` is a thin facade over it (`api.get/.post/.patch/.del`) — keep using it; never call `fetch` to the backend directly. `RemoteApi.fetchThenRetrieveStream()` exists for file/PDF downloads.

### Frontend — SvelteKit 2 / Svelte 5

- **Svelte 5 runes mode enforced globally** (via `svelte.config.js`). Use `$props()`, `$state()`, `$derived()` — never the legacy options API.
- Deployed to **Vercel** (`@sveltejs/adapter-vercel`).
- All backend calls go through `src/lib/server/api.ts` (`api.get / .post / .patch / .del`), which delegates to `src/lib/server/remoteApiProxy.ts` (central proxy adding `X-API-KEY` + `X-Request-Timestamp` + JWT). Always pass the JWT from `getToken(cookies)`. These modules are server-only.

**Route groups:**

```
routes/
  (app)/              Auth-guarded group
    +layout.server.ts Reads medika_token cookie → redirects to /login if absent
    +layout.svelte    Top nav bar + user dropdown
    dashboard / schedule / patients / consultation / finance / profile
  login/              Public — form POST → calls /api/auth/login → sets cookies
  logout/+server.ts   GET → clears cookies → redirect 302 /login
  api/                SvelteKit server routes that proxy to the .NET backend
                      (appointments CRUD, patient search, consultation detail)
```

**Session cookies** (managed in `src/lib/server/session.ts`):
- `medika_token` — JWT, httpOnly, 8h
- `medika_user` — `{ fullName, role }` JSON, httpOnly, 8h

### Styling

Global CSS variables and utility classes live in `src/app.css`. Always use them:

```
--primary / --primary-dark / --primary-light / --primary-50   teal brand
--accent / --accent-light                                       amber CTA
--nav-bg                                                        #1C2B3A dark navy
--bg / --surface / --border / --border-strong
--text / --text-muted / --text-light
--success / --warning / --danger / --info  (each has a -light variant)
```

Utility classes: `.card`, `.mk-nav`, `.mk-content`, `.mk-input`, `.mk-tab`, `.mk-table`, `.truncate`. Font: DM Sans.

---

## Branches & deployment

`feature/*` → `dev` (staging: `dev-medika-api` App Service + Vercel Preview) → `main` (production: `medika-api` + Vercel Production). Workflows: `.github/workflows/backend.yml` (main) and `backend-dev.yml` (dev). Setup guide: `docs/setup/azure-app-service-migration.md`.

## Known gotchas

**Logout link requires two attributes:**

```svelte
<a href="/logout" data-sveltekit-preload-data="off" data-sveltekit-reload>
```

- `data-sveltekit-preload-data="off"` — prevents hover-preload from firing the GET handler accidentally.
- `data-sveltekit-reload` — forces full browser navigation; without it, SvelteKit's client router doesn't follow the server-side `redirect(302)` response from a `+server.ts` endpoint.

**`.mk-nav` creates a stacking context** (`position: fixed; z-index: 200`). Any backdrop/overlay rendered as a sibling of `<nav>` in the DOM must use `z-index < 200`; otherwise it sits above the nav's children (dropdowns, etc.) and swallows pointer events. Current backdrop for the user-menu dropdown: `z-index: 190`.
