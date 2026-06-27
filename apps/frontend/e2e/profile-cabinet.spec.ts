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

// Issue #85 — "Champs non persistés". The cabinet name / order number / address
// inputs used a one-way `value={data.profile.…}` binding. Editing the phone field
// updates component state and re-renders the form, and that re-render re-asserted
// the one-way bindings — silently wiping the three text fields back to their loaded
// (often empty) values, which then persisted as empty. The fix binds every cabinet
// field to local $state so user input survives re-renders.
test.describe('Profile — cabinet fields persist (issue #85)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/profile');
		await expect(page.getByRole('button', { name: 'Mon cabinet' })).toBeVisible();
	});

	// Core regression — non-mutating (never saves), so the suite stays re-runnable.
	test('editing the phone last does not wipe the name/order/address fields', async ({ page }) => {
		const name = page.getByLabel('Nom du cabinet');
		const order = page.getByLabel('N° RPPS / Ordre médical');
		const address = page.getByLabel('Adresse');
		const phone = page.getByLabel('Téléphone du cabinet');

		await name.fill('Cabinet Régression 85');
		await order.fill('ORD-85');
		await address.fill('1 rue de la Régression');

		// Phone is the last field a user typically fills. Each keystroke here updates
		// state and re-renders the form — the exact trigger that used to wipe the
		// three text fields above.
		await phone.fill('');
		await phone.pressSequentially('0550123456');

		await expect(name).toHaveValue('Cabinet Régression 85');
		await expect(order).toHaveValue('ORD-85');
		await expect(address).toHaveValue('1 rue de la Régression');
	});

	// End-to-end contract: the fields actually reach the DB and survive a reload.
	// Mutating, so it captures the originals up front and restores them at the end.
	test('saved cabinet fields survive a reload', async ({ page }) => {
		const name = page.getByLabel('Nom du cabinet');
		const order = page.getByLabel('N° RPPS / Ordre médical');
		const address = page.getByLabel('Adresse');
		const phone = page.getByLabel('Téléphone du cabinet');

		const original = {
			name: await name.inputValue(),
			order: await order.inputValue(),
			address: await address.inputValue(),
			phone: await phone.inputValue(),
		};

		await name.fill('Cabinet Persisté 85');
		await order.fill('ORD-PERSIST-85');
		await address.fill('2 rue Persistée');
		await phone.fill('');
		await phone.pressSequentially('0560111213');
		await page.getByRole('button', { name: 'Enregistrer le cabinet' }).click();
		await expect(page.getByText('Informations du cabinet mises à jour.')).toBeVisible();

		await page.reload();
		await expect(page.getByLabel('Nom du cabinet')).toHaveValue('Cabinet Persisté 85');
		await expect(page.getByLabel('N° RPPS / Ordre médical')).toHaveValue('ORD-PERSIST-85');
		await expect(page.getByLabel('Adresse')).toHaveValue('2 rue Persistée');

		// Restore the seeded state so the spec is re-runnable. The seeded phone is
		// empty (valid) so the restoring save is never cancelled.
		await page.getByLabel('Nom du cabinet').fill(original.name);
		await page.getByLabel('N° RPPS / Ordre médical').fill(original.order);
		await page.getByLabel('Adresse').fill(original.address);
		const phoneRestore = page.getByLabel('Téléphone du cabinet');
		await phoneRestore.fill('');
		if (original.phone) await phoneRestore.pressSequentially(original.phone);
		await page.getByRole('button', { name: 'Enregistrer le cabinet' }).click();
		await expect(page.getByText('Informations du cabinet mises à jour.')).toBeVisible();
	});
});
