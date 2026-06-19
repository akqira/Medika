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
  retries: process.env.CI ? 1 : 0,
  // HTML report + concise console list, so every run is visualisable after the fact.
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'https://localhost:5000',
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
