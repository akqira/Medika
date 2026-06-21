import { test, expect } from '@playwright/test';

// Scenario 1/10 — Password reset by email. Public pages (no login needed).
// All cases here are non-mutating and re-runnable: they use a non-existent email
// or a bogus token, so no real account is ever changed.

test.describe('Password reset — request link', () => {
	test('the login page links to /forgot-password', async ({ page }) => {
		await page.goto('/login');
		await page.getByRole('link', { name: 'Mot de passe oublié ?' }).click();
		await expect(page).toHaveURL(/\/forgot-password/);
		await expect(page.getByRole('heading', { name: 'Mot de passe oublié' })).toBeVisible();
	});

	test('a request for any email returns a generic message (no account enumeration)', async ({ page }) => {
		await page.goto('/forgot-password');
		// A well-formed address that does not belong to any account.
		await page.getByLabel('Adresse e-mail').fill('nobody.unknown@example.com');
		await page.getByRole('button', { name: 'Envoyer le lien' }).click();

		await expect(page.getByText('un lien de réinitialisation a été envoyé')).toBeVisible();
	});
});

test.describe('Password reset — set new password', () => {
	test('an incomplete link (no token/email) is rejected', async ({ page }) => {
		await page.goto('/reset-password');
		await expect(page.getByText('Ce lien de réinitialisation est invalide ou incomplet.')).toBeVisible();
	});

	test('a bogus token is rejected with a generic error', async ({ page }) => {
		await page.goto('/reset-password?token=deadbeef&email=nobody.unknown@example.com');
		await page.locator('#newPassword').fill('NewPass123');
		await page.locator('#confirmPassword').fill('NewPass123');
		await page.getByRole('button', { name: 'Réinitialiser le mot de passe' }).click();

		await expect(page.getByText('Lien invalide ou expiré. Veuillez refaire une demande.')).toBeVisible();
	});

	test('mismatched passwords are rejected before any backend call', async ({ page }) => {
		await page.goto('/reset-password?token=deadbeef&email=nobody.unknown@example.com');
		await page.locator('#newPassword').fill('NewPass123');
		await page.locator('#confirmPassword').fill('Different123');
		await page.getByRole('button', { name: 'Réinitialiser le mot de passe' }).click();

		await expect(page.getByText('Les mots de passe ne correspondent pas.')).toBeVisible();
	});
});
