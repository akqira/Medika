import { expect, type Page, type Cookie } from '@playwright/test';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

// Seeded doctor credentials. Override on the command line / .env.test if your
// local seed differs: E2E_EMAIL=… E2E_PASSWORD=… pnpm test:e2e
export const EMAIL = process.env.E2E_EMAIL ?? 'kader.kebir@gmail.com';
export const PASSWORD = process.env.E2E_PASSWORD ?? 'Doctor@123';

// Where we cache the authenticated session cookies between specs/runs (gitignored).
const STATE_FILE = join(import.meta.dirname, '.auth-state.json');

/** The real form login: navigate /login, submit, wait for the dashboard. */
async function formLogin(page: Page, email: string, password: string) {
	await page.goto('/login');
	await page.getByLabel('Adresse e-mail').fill(email);
	await page.getByLabel('Mot de passe').fill(password);
	await page.getByRole('button', { name: 'Se connecter' }).click();
	// The login round-trip + redirect can be slow on a cold dev server. Wait for
	// the URL to flip AND for the dashboard to actually render, so callers start
	// from a fully-loaded page rather than racing the navigation.
	await expect(page).toHaveURL(/\/dashboard/);
	await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();
}

/**
 * Log in as the seeded doctor and land on the dashboard.
 *
 * To avoid paying the /login compile + auth round-trip in EVERY spec (the dev
 * server compiles routes on demand, so a fresh form-login per test is the main
 * source of cold-start slowness/flake), we cache the authenticated session
 * cookies after the first real login and **reuse** them in subsequent specs/runs.
 * If the cached session is missing or no longer valid (e.g. the 8h JWT expired,
 * or a different DB), we transparently fall back to a real form login and
 * refresh the cache. Only the default seeded credentials are cached.
 */
export async function login(page: Page, email = EMAIL, password = PASSWORD) {
	const usingDefaults = email === EMAIL && password === PASSWORD;

	// Fast path — restore the saved session and verify it still authenticates.
	if (usingDefaults && existsSync(STATE_FILE)) {
		try {
			const cookies: Cookie[] = JSON.parse(readFileSync(STATE_FILE, 'utf8'));
			await page.context().addCookies(cookies);
			await page.goto('/dashboard');
			if (/\/dashboard/.test(page.url())) {
				await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();
				return;
			}
		} catch {
			// Corrupt/expired cache → fall through to a real login.
		}
	}

	await formLogin(page, email, password);

	// Persist the fresh session for the next spec (best-effort; default creds only).
	if (usingDefaults) {
		try {
			const { cookies } = await page.context().storageState();
			writeFileSync(STATE_FILE, JSON.stringify(cookies));
		} catch {
			// A failed cache write must never fail the test — login already succeeded.
		}
	}
}
