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

**Stop the user's local servers before issue work that needs screenshots.** When fixing
an issue where you'll switch branches and capture screenshots, first **stop the user's
running dev servers** (frontend `:5000`, backend `:5100`) and run your own throwaway
instances on the branch under test — then stop them when done. Switching branches under a
running Vite dev server corrupts its `.svelte-kit/types` and makes every route return 500
(recover with: kill the `:5000` process → `pnpm dev` → `pnpm exec svelte-kit sync`). Don't
silently take over a server the user is watching — say you're doing it.

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

### Verifying before you push (non-negotiable)

Static checks (`pnpm check`, `dotnet build`, unit tests) do **not** catch runtime
locator/selector problems or cross-page regressions. CI is a **backstop, not** your
verification step. Before pushing a branch:

- **If your diff adds or changes a Playwright spec, RUN that spec locally** — start both
  apps and run it (see the E2E section). A written-but-unrun spec is unverified. Type-checks
  cannot see that `getByLabel('Nom')` also matches `'Prénom'`, or that a locator resolves to
  two elements.
- **If you touch a shared surface** (the `(app)` layout/nav, `app.css`, a `$lib/components`
  widget), grep for its other consumers and re-run the specs that exercise them. A global
  element can collide with a per-page element of the same **accessible name/role** and break
  unrelated specs (e.g. the navbar search vs. the patients-page search).
- **Check the baseline.** When branching off `dev`, confirm dev's own e2e is green
  (`gh run list --branch dev --workflow e2e.yml`). Don't build on a red baseline, attribute
  the red to yourself, or miss that you must fix it.
- **UI/UX changes ship a validation screenshot in the PR.** After verifying the change
  visually against the reference (browser/Playwright screenshot of the running app — or, for
  non-page surfaces like an email template, the actual produced HTML), **post that screenshot
  into the PR conversation**. Mechanism: commit it under `docs/ui-validation/` and embed it in a
  PR comment via a commit-SHA-pinned `raw.githubusercontent.com` URL (survives branch deletion;
  the repo is public). A UI/UX PR without its screenshot is not ready for review — and never
  claim "verified / pixel-perfect" without showing the proof.

---

## Commands

### Frontend (`apps/frontend/`)

```bash
pnpm dev          # dev server on http://localhost:5173
pnpm build        # production build
pnpm check        # svelte-check + TS type checking
pnpm check:watch  # same, watch mode
pnpm test         # vitest unit tests (run once)
pnpm test:e2e     # Playwright E2E (needs BOTH apps running — see below)
pnpm test:e2e:ui  # Playwright in interactive UI mode (recommended)
```

Run from the repo root or from `apps/frontend/` — pnpm workspace resolves either way.

#### End-to-end tests (Playwright)

E2E specs live in `apps/frontend/e2e/`. The config (`playwright.config.ts`) has **no
`webServer`** on purpose — start both apps yourself first (backend needs MongoDB + the
HTTPS dev profile), then run the suite against `https://localhost:5000`:

```powershell
dotnet run --project apps/backend/src/Medika.Api --launch-profile https   # :5100
cd apps/frontend; pnpm dev                                                 # :5000
pnpm test:e2e:ui
```

Seeded-doctor credentials come from `e2e/.env.test` (gitignored) or the `E2E_EMAIL` /
`E2E_PASSWORD` env vars; `e2e/helpers.ts` exposes a shared `login(page)` helper. The
suite is **failing-situation-first** (TDD-style): each manual scenario in
`docs/manual-test-scenarios.md` has a spec covering its observable failure modes.

| Spec | Covers (scenario) | Failure modes asserted |
|------|-------------------|------------------------|
| `login.spec.ts` | Login | wrong password, unknown email (no enumeration), empty/partial fields, already-auth redirect |
| `auth-guard.spec.ts` | All `(app)` routes | unauthenticated access → `/login`; logout re-protects |
| `patient-new.spec.ts` | 3 — Add patient | per-step wizard validation: required, letters-only name, phone/email format, 15-digit NSS |
| `consultation.spec.ts` | 4/5 — Consultation | save with no patient rejected; finalize confirmation; add/remove médicament |
| `finance-charges.spec.ts` | 6/9 — Charges | required category/description, amount `min=1` (0 / negative) |
| `schedule-booking.spec.ts` | 7 — Agenda | no-patient guard, empty search, min-2-char search, Escape/Cancel close |
| `patient-search.spec.ts` | 8 — Search | empty result state, keyboard nav no-op on empty list |
| `profile-security.spec.ts` | 2/10 — Password | mismatch, `< 8` chars, wrong current password, required fields |

Negative tests are written to be **non-mutating and re-runnable** (e.g. the wrong-current-password
case never actually changes the seeded password).

### Backend (`apps/backend/`)

```bash
dotnet run --project apps/backend/src/Medika.Api
# Swagger UI → http://localhost:5000/swagger
```

Build the whole solution: `dotnet build apps/backend/Medika.slnx`

The **backend** has a small xUnit suite (`apps/backend/tests/Medika.Tests/`) — run with
`dotnet test apps/backend/Medika.slnx`. It covers domain/handler logic (Finance, Medical,
Patients), the HTTP-logging redaction middleware, and the Brevo email provider
(`Infrastructure/BrevoEmailServiceTests.cs`, HTTP layer stubbed). The **frontend** has
vitest unit tests and a Playwright E2E suite (`apps/frontend/e2e/`) — see the Frontend
Commands section above.

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
Medika.Infrastructure/ MongoDB repos, JWT, Cloudflare R2 storage, transactional email (Brevo), audit
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

### Transactional email (Brevo, pattern ported from eGestion)

- `IEmailService` (`Medika.Application/Common/Interfaces/`) is the provider-agnostic email abstraction (single / multi recipient, `isHtml`). Inject it; never talk to a provider SDK directly.
- The only implementation is `BrevoEmailService` (`Medika.Infrastructure/Email/`): a **typed `HttpClient`** (`AddHttpClient<IEmailService, BrevoEmailService>` in `DependencyInjection.AddEmail`) that `POST`s to Brevo's `/v3/smtp/email` with an `api-key` header. **Brevo is the sole provider** — there is no provider switch.
- Config lives in the `Brevo` section (`BrevoSettings`, bound as a singleton POCO like `JwtSettings`/`R2Settings`). `ApiKey`/`FromEmail` are secrets: **empty locally and in CI** (`appsettings.json` ships empty), set in prod via `Brevo__ApiKey` (Azure App Service); `FromEmail` must be a verified Brevo sender.
- **When unconfigured, sending is a no-op** (logs a warning, returns) — it never throws. This keeps email-driven flows working end-to-end in dev/CI without a provider account.
- Password reset uses this: `IPasswordResetSender` → `EmailPasswordResetSender` builds the link, **logs it** (so the flow stays testable without a real inbox), then sends via `IEmailService`. (The earlier log-only `LoggingPasswordResetSender` is superseded but kept for reference.)
- Tests: `BrevoEmailServiceTests` (xUnit, HTTP layer stubbed) cover endpoint/payload/headers, html-vs-text, multi-recipient, error status, and the unconfigured no-op. The password-reset Playwright spec (`e2e/password-reset.spec.ts`) guards the user-facing flow (non-mutating, email-independent).

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

### Post-merge rule (non-negotiable)

**Every time a PR is merged, the same change-set must, before the work is considered done:**

1. **Update GitHub** — move the matching **issue** to its new status (`status:` label + column on the [Project board](https://github.com/users/akqira/projects/3)), close it if shipped, and create one for any new remaining work. GitHub is the source of truth for state and must never lag behind `dev`. **`ROADMAP.md` stays light** — only edit it when the **vision**, a **phase**, or an **architecture decision** changes (not for per-item detail).
2. **Have passing E2E coverage** — every merged feature/bugfix must have a Playwright spec in `apps/frontend/e2e/` that exercises it (failing-path first, per the existing convention) and the suite must pass. If a PR ships behaviour with no e2e spec, add one in the same PR (or an immediate follow-up before the next merge).

A local `gh pr merge` triggers a reminder via the `PostToolUse` hook in `.claude/settings.json`, but the rule applies to **all** merges (including those done from the GitHub web UI). The Playwright suite runs in CI on every PR/push touching `apps/**` via `.github/workflows/e2e.yml` (fresh Mongo + API + Vite), so "passing e2e" is enforced automatically.

## Known gotchas

**Logout link requires two attributes:**

```svelte
<a href="/logout" data-sveltekit-preload-data="off" data-sveltekit-reload>
```

- `data-sveltekit-preload-data="off"` — prevents hover-preload from firing the GET handler accidentally.
- `data-sveltekit-reload` — forces full browser navigation; without it, SvelteKit's client router doesn't follow the server-side `redirect(302)` response from a `+server.ts` endpoint.

**`.mk-nav` creates a stacking context** (`position: fixed; z-index: 200`). Any backdrop/overlay rendered as a sibling of `<nav>` in the DOM must use `z-index < 200`; otherwise it sits above the nav's children (dropdowns, etc.) and swallows pointer events. Current backdrop for the user-menu dropdown: `z-index: 190`.
