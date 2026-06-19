import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 2 / 10 — Change password & profile: the failing situations the
// changePassword action guards against. None of these actually mutate the
// password (they all fail before or at the backend), so the seeded account is
// left untouched and the suite stays re-runnable.

test.describe('Profile — password change failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/profile');
		await page.getByRole('button', { name: 'Sécurité' }).click();
		await expect(page.getByLabel('Mot de passe actuel')).toBeVisible();
	});

	test('mismatched new/confirm passwords are rejected', async ({ page }) => {
		await page.getByLabel('Mot de passe actuel').fill('Doctor@123');
		await page.getByLabel('Nouveau mot de passe', { exact: true }).fill('NewPass@123');
		await page.getByLabel('Confirmer le nouveau mot de passe').fill('DifferentPass@123');
		await page.getByRole('button', { name: 'Modifier le mot de passe' }).click();

		await expect(page.getByText('Les mots de passe ne correspondent pas.')).toBeVisible();
	});

	test('a new password shorter than 8 characters is rejected', async ({ page }) => {
		await page.getByLabel('Mot de passe actuel').fill('Doctor@123');
		await page.getByLabel('Nouveau mot de passe', { exact: true }).fill('short');
		await page.getByLabel('Confirmer le nouveau mot de passe').fill('short');
		await page.getByRole('button', { name: 'Modifier le mot de passe' }).click();

		await expect(
			page.getByText('Le nouveau mot de passe doit contenir au minimum 8 caractères.')
		).toBeVisible();
	});

	test('a wrong current password is rejected by the backend', async ({ page }) => {
		await page.getByLabel('Mot de passe actuel').fill('WrongCurrent@999');
		await page.getByLabel('Nouveau mot de passe', { exact: true }).fill('BrandNew@123');
		await page.getByLabel('Confirmer le nouveau mot de passe').fill('BrandNew@123');
		await page.getByRole('button', { name: 'Modifier le mot de passe' }).click();

		await expect(page.getByText('Mot de passe actuel incorrect.')).toBeVisible();
	});

	test('empty required fields are blocked by the browser — no request fires', async ({ page }) => {
		await page.getByRole('button', { name: 'Modifier le mot de passe' }).click();

		// `required` on all three inputs prevents submission, so no banner appears.
		await expect(page.getByText('Les mots de passe ne correspondent pas.')).toHaveCount(0);
		await expect(page.getByLabel('Mot de passe actuel')).toBeFocused();
	});
});
