import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Phase 4 — Catalogue d'actes (acts/tariffs). The happy path is mutating, so it
// cleans up after itself by deleting the act it creates (re-runnable).

test.describe('Catalogue d\'actes', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/actes');
		await expect(page.getByRole('heading', { name: "Catalogue d'actes" })).toBeVisible();
	});

	test('reachable from the Finances page', async ({ page }) => {
		await page.goto('/finance');
		await page.getByRole('link', { name: "Catalogue d'actes" }).click();
		await expect(page).toHaveURL(/\/actes/);
		await expect(page.getByRole('heading', { name: "Catalogue d'actes" })).toBeVisible();
	});

	test('an act with no name is blocked (required)', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter' }).click();
		await expect(page.locator('#act-name')).toHaveJSProperty('validity.valid', false);
	});

	test('a valid act is created and listed, then removed (cleanup)', async ({ page }) => {
		const name = `E2E acte ${Date.now()}`;
		await page.locator('#act-name').fill(name);
		await page.locator('#act-tariff').fill('1500');
		await page.getByRole('button', { name: 'Ajouter' }).click();

		await expect(page.getByText('Acte ajouté.')).toBeVisible();
		const row = page.locator('tr[data-act-id]').filter({ hasText: name });
		await expect(row).toBeVisible();
		await expect(row).toContainText('DA'); // tariff rendered (fr-DZ spacing varies)

		// Cleanup via the row's delete button (also covers the delete action).
		await row.getByRole('button', { name: "Supprimer l'acte" }).click();
		await expect(page.getByText('Acte supprimé.')).toBeVisible();
		await expect(page.locator('tr[data-act-id]').filter({ hasText: name })).toHaveCount(0);
	});
});
