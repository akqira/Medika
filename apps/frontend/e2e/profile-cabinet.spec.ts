import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #83 — "Ville" and "Wilaya" designated the same information on the
// "Mon cabinet" tab. The fix removes the redundant free-text "Ville" field and
// keeps the Wilaya dropdown as the single locality field. These assertions are
// read-only (no save), so the suite stays re-runnable.

test.describe('Profile — cabinet locality (issue #83)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/profile');
		// "Mon cabinet" is the default tab.
		await expect(page.getByRole('button', { name: 'Mon cabinet' })).toBeVisible();
	});

	test('the redundant "Ville" field is gone', async ({ page }) => {
		await expect(page.getByLabel('Ville')).toHaveCount(0);
	});

	test('the Wilaya dropdown is kept as the single locality field', async ({ page }) => {
		const wilaya = page.getByLabel('Wilaya');
		await expect(wilaya).toBeVisible();
		// It is a <select>, not a free-text input.
		await expect(wilaya).toHaveJSProperty('tagName', 'SELECT');
	});
});

// Issue #84 — the cabinet phone field accepted free text. The fix strips every
// non-digit on input and rejects anything that is not a valid Algerian number
// (mobile 05/06/07 or fixed 02/03/04, 10 digits) with a red error under the field.
// Non-mutating: the strip test never submits, the invalid-format test is rejected
// client-side before any save, so the suite stays re-runnable.
test.describe('Profile — cabinet phone validation (issue #84)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/profile');
		await expect(page.getByRole('button', { name: 'Mon cabinet' })).toBeVisible();
	});

	test('non-numeric characters cannot be entered', async ({ page }) => {
		const phone = page.getByLabel('Téléphone du cabinet');
		await phone.fill('');
		await phone.pressSequentially('abc023def45ghi');
		// Letters are stripped; only the digits survive.
		await expect(phone).toHaveValue('02345');
	});

	test('an invalid number shows a red error and blocks the save', async ({ page }) => {
		const phone = page.getByLabel('Téléphone du cabinet');
		await phone.fill('');
		await phone.pressSequentially('0123'); // too short / invalid prefix
		await page.getByRole('button', { name: 'Enregistrer le cabinet' }).click();
		await expect(page.getByText('Numéro algérien invalide', { exact: false })).toBeVisible();
		// The success banner never appears because the submit was cancelled.
		await expect(page.getByText('Informations du cabinet mises à jour.')).toHaveCount(0);
	});
});
