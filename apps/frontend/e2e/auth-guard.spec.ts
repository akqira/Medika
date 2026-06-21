import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Cross-cutting failing situation for EVERY scenario: an unauthenticated user
// (no medika_token cookie) must never reach an (app) route — the
// +layout.server.ts guard redirects them to /login.

const PROTECTED_ROUTES = [
	'/dashboard',
	'/patients',
	'/patients/new',
	'/consultation',
	'/finance',
	'/schedule',
	'/profile',
];

test.describe('Auth guard (unauthenticated)', () => {
	for (const route of PROTECTED_ROUTES) {
		test(`${route} redirects to /login when not logged in`, async ({ page }) => {
			await page.context().clearCookies();
			await page.goto(route);
			await expect(page).toHaveURL(/\/login/);
		});
	}

	test('logout clears the session and re-protects the routes', async ({ page }) => {
		// Land authenticated (helper waits for the dashboard to fully render)…
		await login(page);

		// …then log out and confirm a protected route bounces back to /login.
		await page.goto('/logout');
		await expect(page).toHaveURL(/\/login/);
		await page.goto('/dashboard');
		await expect(page).toHaveURL(/\/login/);
	});
});
