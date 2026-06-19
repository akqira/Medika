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
});
