import { test, expect } from '@playwright/test';
import { EMAIL, PASSWORD } from './helpers';

// Authentication — happy path + every observable failing situation.
// Requires BOTH apps running (backend on :5100, frontend on :5000) — see
// playwright.config.ts for the start commands.

test.describe('Login', () => {
	test('seeded doctor lands on dashboard (happy path)', async ({ page }) => {
		await page.goto('/login');
		await page.getByLabel('Adresse e-mail').fill(EMAIL);
		await page.getByLabel('Mot de passe').fill(PASSWORD);
		await page.getByRole('button', { name: 'Se connecter' }).click();

		await expect(page).toHaveURL(/\/dashboard/);
		await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();
	});

	test('wrong password is rejected with a credential error and stays on /login', async ({ page }) => {
		await page.goto('/login');
		await page.getByLabel('Adresse e-mail').fill(EMAIL);
		await page.getByLabel('Mot de passe').fill('definitely-not-the-password');
		await page.getByRole('button', { name: 'Se connecter' }).click();

		await expect(page.getByText('Email ou mot de passe incorrect.')).toBeVisible();
		await expect(page).toHaveURL(/\/login/);
	});

	test('unknown email is rejected with the same generic error (no account enumeration)', async ({ page }) => {
		await page.goto('/login');
		await page.getByLabel('Adresse e-mail').fill('nobody-here@example.com');
		await page.getByLabel('Mot de passe').fill('whatever123');
		await page.getByRole('button', { name: 'Se connecter' }).click();

		// Same message as wrong-password — the UI must not reveal whether the email exists.
		await expect(page.getByText('Email ou mot de passe incorrect.')).toBeVisible();
		await expect(page).toHaveURL(/\/login/);
	});

	test('empty fields are blocked by the browser (required) — no navigation', async ({ page }) => {
		await page.goto('/login');
		await page.getByRole('button', { name: 'Se connecter' }).click();

		// HTML5 `required` stops submission; we never leave the login page.
		await expect(page).toHaveURL(/\/login/);
		await expect(page.getByLabel('Adresse e-mail')).toBeFocused();
	});

	test('password-only (missing email) does not authenticate', async ({ page }) => {
		await page.goto('/login');
		await page.getByLabel('Mot de passe').fill(PASSWORD);
		await page.getByRole('button', { name: 'Se connecter' }).click();

		await expect(page).toHaveURL(/\/login/);
	});

	test('an already-authenticated user visiting /login is redirected to the dashboard', async ({ page }) => {
		// Authenticate first.
		await page.goto('/login');
		await page.getByLabel('Adresse e-mail').fill(EMAIL);
		await page.getByLabel('Mot de passe').fill(PASSWORD);
		await page.getByRole('button', { name: 'Se connecter' }).click();
		await expect(page).toHaveURL(/\/dashboard/);
		// Wait for the dashboard to fully render before navigating again.
		await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();

		// The login load() guard bounces logged-in users away.
		await page.goto('/login');
		await expect(page).toHaveURL(/\/dashboard/);
	});
});
