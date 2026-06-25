import { defineConfig, devices } from '@playwright/test';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

// E2E config values (base URL + seeded test credentials) live in .env.test.
// Tiny dependency-free loader: existing process.env always wins so you can
// override any value on the command line (e.g. E2E_EMAIL=... pnpm test:e2e).
const envPath = resolve(import.meta.dirname, '.env.test');
if (existsSync(envPath)) {
  for (const line of readFileSync(envPath, 'utf8').split('\n')) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const eq = trimmed.indexOf('=');
    if (eq === -1) continue;
    const key = trimmed.slice(0, eq).trim();
    const value = trimmed.slice(eq + 1).trim().replace(/^["']|["']$/g, '');
    if (!(key in process.env)) process.env[key] = value;
  }
}

// IMPORTANT: there is no `webServer` here on purpose — start both apps yourself
// before running tests (the backend needs MongoDB + the HTTPS dev profile):
//   1. dotnet run --project apps/backend/src/Medika.Api --launch-profile https
//   2. cd apps/frontend && pnpm dev      (https://localhost:5000)
// Then: pnpm test:e2e:ui  (interactive, recommended) — see package.json scripts.

export default defineConfig({
  testDir: './e2e',
  // Fail fast in CI if someone leaves a .only in a spec.
  forbidOnly: !!process.env.CI,
  // The whole suite runs against ONE shared Vite dev server (no per-test app
  // isolation). With many parallel workers, a route Vite is still cold-compiling
  // races the test's first interaction (fill/click) before SvelteKit hydrates,
  // which surfaces as flaky, run-to-run-different failures. Serialise the workers
  // so each route compiles + hydrates once before it's driven; keep one retry as
  // a safety net for any residual timing blip.
  workers: 1,
  retries: 2,
  // Auth round-trips (login → backend → redirect) and SSR loads are slow on a COLD
  // dev server: the FIRST specs pay Vite's on-demand compilation of the login action +
  // (app) layout + dashboard, which was clocking ~17s and just overran the old 15s
  // timeout (stuck on /login → toHaveURL fails). Serialised workers alone don't help —
  // the first spec always eats the cold cost — so give web-first assertions and
  // navigation enough room to absorb the compile, plus an extra retry.
  expect: { timeout: 30000 },
  // HTML report + concise console list, so every run is visualisable after the fact.
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'https://localhost:5000',
    // Cold-compile headroom for navigations/actions (see retries/expect note above).
    navigationTimeout: 30000,
    actionTimeout: 20000,
    // The frontend dev server uses a self-signed local cert (apps/frontend/.cert).
    ignoreHTTPSErrors: true,
    // Capture everything — these power the trace viewer / UI mode time-travel.
    trace: 'on',
    screenshot: 'on',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
