# Medika Frontend — Vercel Deployment

The frontend (`apps/frontend`) is a **SvelteKit 2 app running as a BFF** (backend-for-frontend),
built with `@sveltejs/adapter-vercel` and deployed to **Vercel**. The browser only ever talks to
the Vercel domain; all calls to the .NET API on Azure go **server-side** through
`src/lib/server/remoteApiProxy.ts` (`RemoteApi`), which injects the API key, an anti-replay
timestamp, and the user's Bearer token.

This document covers a first-time deploy.

---

## 0. Prerequisite fix (already applied)

`vite.config.ts` previously read `.cert/key.pem` unconditionally. That code runs during
`vite build` too, so on Vercel (where `.cert` does not exist) the build would have failed with
`ENOENT`. It is now guarded to load the local cert only when present, so dev keeps HTTPS and the
Vercel build works. `.cert/` was also added to `.gitignore` so the private key can never be
committed.

---

## 1. Import the project into Vercel

From the Vercel dashboard → **Add New… → Project** → import `akqira/Medika`.

Because this is a monorepo (pnpm workspace), set:

| Setting | Value |
|---|---|
| **Root Directory** | `apps/frontend` |
| **Framework Preset** | SvelteKit (auto-detected) |
| **Build Command** | *default* (`vite build`) |
| **Install Command** | *default* — Vercel detects `pnpm-lock.yaml` and the workspace at the repo root |
| **Output Directory** | *default* — `adapter-vercel` handles this |
| **Node.js Version** | **22.x** (Settings → General). Vite 8 requires Node ≥ 20.19 / 22.12 |

You do **not** need a `vercel.json`; the SvelteKit adapter configures the build.

---

## 2. Environment variables

Set these in **Project Settings → Environment Variables**. Both are **server-only** secrets —
they are read via `$env/dynamic/private` and must **never** be given a `PUBLIC_` prefix (that would
ship them to the browser).

| Variable | Value | Environments |
|---|---|---|
| `API_URL` | `https://<your-azure-app>.azurewebsites.net` (no trailing slash needed) | Production (+ Preview if you have a staging API) |
| `API_SECRET` | The **production** API key — must equal the backend's `ApiSettings:ApiKey` on Azure | Production (separate value for Preview) |

Notes:

- Generate a fresh secret per environment: `openssl rand -base64 48`. Do **not** reuse the local
  dev value or the eGestion value.
- Scope each value to the right environment. Don't let a Preview deployment point `API_URL` at the
  production API with the production key.
- These never go in `.env` in the repo (`.env*` is gitignored; `.env.example` is the only committed
  template).

---

## 3. The real Vercel ↔ Azure wiring (this is *not* CORS)

A common misconception: you do **not** need to add the Vercel domain to the backend's
`Cors:AllowedOrigins`. CORS is a *browser* protection, and in the BFF model the browser never calls
Azure directly — it calls SvelteKit server routes on the Vercel domain (same origin), which then
call Azure server-to-server. Server-to-server requests are not subject to CORS.

What actually has to line up:

1. **API key match** — Vercel `API_SECRET` **must equal** Azure App Service config
   `ApiSettings__ApiKey`. The backend's `ApiKeyMiddleware` rejects the request (and the BFF returns
   a 5xx) if they differ. In Azure, set it under **App Service → Configuration → Application
   settings** as `ApiSettings__ApiKey` (double underscore = nested key).
2. **Clock sync** — `RemoteApi` sends `X-Request-Timestamp`, and the backend rejects requests older
   than ~5 minutes. Both Vercel and Azure run on UTC, so this is normally fine; just don't hardcode
   timezones anywhere.
3. **Reachability** — Azure App Service is public over HTTPS by default, which is all the BFF needs.
   If you ever lock the API down with Access Restrictions, you must allow Vercel's egress (Vercel
   does not publish fixed IPs on Hobby/Pro — prefer keeping the API-key gate rather than IP
   allow-listing, or front it with a fixed-IP gateway).
4. **JWT secret stays on Azure only** — the API signs/validates JWTs; that secret lives in Azure App
   settings, never in Vercel or `.env`.

You *can* still set `Cors:AllowedOrigins` to your Vercel domain as harmless defense-in-depth, but it
is not what makes the app work.

---

## 4. Deploy & verify

1. Trigger the first deploy (push to the production branch, or **Deploy** in the dashboard).
2. Open the Vercel URL and log in. A successful login proves the full chain:
   browser → Vercel server route → `RemoteApi` → Azure (API key + timestamp accepted) → Mongo.
3. If login fails, check **Vercel → Deployment → Functions logs**. `[RemoteApi]` lines tell you
   which leg broke:
   - `401 on authenticated request` → JWT / session issue.
   - `Non-OK response … 401/403` with no token → **API key mismatch** (fix `API_SECRET` vs
     `ApiSettings__ApiKey`).
   - `Backend unreachable` / `503 SERVICE_UNAVAILABLE` → wrong `API_URL` or Azure app down.

---

## 5. Optional follow-ups

- **Custom domain** — add it under Settings → Domains, point DNS, then update any absolute URLs and
  (if you add direct browser→API calls later) the backend CORS list.
- **Preview environments** — point Preview `API_URL`/`API_SECRET` at a staging API + key, not prod.
- **Sentry** — `hooks.server.ts` has a TODO to wire `Sentry.captureException`; add the DSN as a
  (server-side) env var when ready.

---

## Side note (backend, not blocking)

`apps/backend/src/Medika.Api/appsettings.json` has `MongoDB:DatabaseName = "medik_prod"` — missing
the `a` vs the Atlas database `medika_prod` you created. If Azure App settings override it with the
correct name you're fine; otherwise correct the default to avoid pointing prod at the wrong DB name.
