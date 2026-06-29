import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 6 / 9 — Finances & charges: the failing situations when recording an
// expense. The inline charge form relies on HTML5 constraints (required +
// min=1), so an invalid submit must NOT close the form (showChargeForm stays
// true) and must NOT show the success banner.

test.describe('Finance — add charge validation failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/finance');
		await expect(page.getByRole('heading', { name: 'Finances' })).toBeVisible();
		await page.getByRole('button', { name: 'Ajouter une charge' }).first().click();
		await expect(page.getByRole('heading', { name: 'Nouvelle charge' })).toBeVisible();
	});

	test('submitting an empty form is blocked (category required)', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		// The form is not submitted: still open, no success banner.
		await expect(page.getByRole('heading', { name: 'Nouvelle charge' })).toBeVisible();
		await expect(page.locator('#charge-category')).toHaveJSProperty('validity.valid', false);
	});

	test('amount of 0 is rejected (min=1)', async ({ page }) => {
		await page.locator('#charge-category').selectOption('Loyer');
		await page.locator('#charge-description').fill('Loyer du cabinet');
		await page.locator('#charge-amount').fill('0');
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		await expect(page.getByRole('heading', { name: 'Nouvelle charge' })).toBeVisible();
		await expect(page.locator('#charge-amount')).toHaveJSProperty('validity.valid', false);
	});

	test('a negative amount is rejected (min=1)', async ({ page }) => {
		await page.locator('#charge-category').selectOption('Loyer');
		await page.locator('#charge-description').fill('Loyer du cabinet');
		await page.locator('#charge-amount').fill('-500');
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		await expect(page.getByRole('heading', { name: 'Nouvelle charge' })).toBeVisible();
		await expect(page.locator('#charge-amount')).toHaveJSProperty('validity.valid', false);
	});

	test('a missing description is blocked (required)', async ({ page }) => {
		await page.locator('#charge-category').selectOption('Loyer');
		await page.locator('#charge-amount').fill('5000');
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		await expect(page.getByRole('heading', { name: 'Nouvelle charge' })).toBeVisible();
		await expect(page.locator('#charge-description')).toHaveJSProperty('validity.valid', false);
	});

	// Issue #101 — a whitespace-only description passes the HTML5 `required`
	// constraint (validity.valid === true) but must NOT reach the server as a
	// blank charge. The action trims the input, so it is rejected with a 400 FR
	// message — never the generic 500 "Erreur serveur.".
	test('a whitespace-only description is rejected with a 400 FR message', async ({ page }) => {
		await page.locator('#charge-category').selectOption('Loyer');
		await page.locator('#charge-description').fill('   ');
		await page.locator('#charge-amount').fill('5000');

		// HTML5 thinks the field is filled — the guard must come from the server.
		await expect(page.locator('#charge-description')).toHaveJSProperty('validity.valid', true);
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		await expect(page.getByText('La description est requise.')).toBeVisible();
		await expect(page.getByText('Erreur serveur.')).toHaveCount(0);
		await expect(page.getByText('Charge ajoutée avec succès.')).toHaveCount(0);
	});

	// Happy path — guards the FR-label/EN-enum regression: "Loyer" must persist as
	// ChargeCategory.Rent (previously Enum.Parse rejected the French label → 400).
	test('a valid charge is created and shown, then removed (cleanup)', async ({ page }) => {
		const desc = `E2E loyer ${Date.now()}`;
		await page.locator('#charge-category').selectOption({ label: 'Loyer' });
		await page.locator('#charge-description').fill(desc);
		await page.locator('#charge-amount').fill('5000');
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		await expect(page.getByText('Charge ajoutée avec succès.')).toBeVisible();
		const row = page.locator('tr[data-charge-id]').filter({ hasText: desc });
		await expect(row).toBeVisible();
		await expect(row).toContainText('Loyer'); // category rendered in French

		// Self-clean so the suite stays re-runnable (also covers the delete endpoint).
		const id = await row.getAttribute('data-charge-id');
		const res = await page.request.delete(`/api/charges/${id}`);
		expect(res.status(), `cleanup delete of ${id}`).toBe(204);
	});
});
