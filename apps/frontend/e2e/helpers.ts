import { expect, type Page } from '@playwright/test';

// Seeded doctor credentials. Override on the command line / .env.test if your
// local seed differs: E2E_EMAIL=… E2E_PASSWORD=… pnpm test:e2e
export const EMAIL = process.env.E2E_EMAIL ?? 'kader.kebir@gmail.com';
export const PASSWORD = process.env.E2E_PASSWORD ?? 'Doctor@123';

/**
 * Log in as the seeded doctor and wait until the dashboard is shown.
 * Most negative-path specs start from an authenticated session, so they call
 * this in a beforeEach.
 */
export async function login(page: Page, email = EMAIL, password = PASSWORD) {
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
