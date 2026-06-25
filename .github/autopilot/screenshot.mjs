// Logs in with the seeded doctor and screenshots the feature route the agent
// reported in autopilot-result.json (SHOT_ROUTE). Writes feature-screenshot.png
// at the repo root. Best-effort: failure here must not fail the pipeline.
import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
// Resolve Playwright from the frontend workspace where it's installed.
const require = createRequire(path.join(root, 'apps/frontend/package.json'));
const { chromium } = require('playwright');

const base = process.env.E2E_BASE_URL || 'http://localhost:5000';
const email = process.env.E2E_EMAIL;
const password = process.env.E2E_PASSWORD;
const route = process.env.SHOT_ROUTE || '/dashboard';

const out = path.join(root, 'feature-screenshot.png');

const browser = await chromium.launch();
try {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();

  // Seeded-doctor login (mirrors e2e/helpers.ts login()).
  await page.goto(`${base}/login`, { waitUntil: 'networkidle' });
  await page.getByLabel(/e-?mail/i).fill(email);
  await page.getByLabel(/mot de passe|password/i).fill(password);
  await page.getByRole('button', { name: /se connecter|login|connexion/i }).click();
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 15000 });

  await page.goto(`${base}${route}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200); // let async data render
  await page.screenshot({ path: out, fullPage: false });
  console.log(`Screenshot saved: ${out} (route ${route})`);
} finally {
  await browser.close();
}
