import { test, expect, type Locator } from '@playwright/test';
import { login } from './helpers';

// Issue #104 — native HTML5 validation bubbles must read in FRENCH, regardless of
// the browser's UI language. The `frValidation` action sets a French
// setCustomValidity() message per failure type. We assert the resulting
// `validationMessage` (which reflects the custom message) on the first invalid
// control after a blocked submit.
//
// All cases are NEGATIVE and non-mutating: every submit is rejected by the
// browser before it reaches the server, so nothing is created — the suite stays
// re-runnable.

// Read the live validationMessage off a control (custom message wins once set).
const validationMessage = (loc: Locator) =>
	loc.evaluate((el) => (el as HTMLInputElement | HTMLSelectElement).validationMessage);

test.describe('#104 — French native validation messages', () => {
	test('finance: empty required <select> → French "valueMissing"', async ({ page }) => {
		await login(page);
		await page.goto('/finance');
		// Wait for the page to hydrate before clicking the toggle button (a cold
		// first navigation can otherwise click an un-hydrated button that no-ops).
		await expect(page.getByRole('heading', { name: 'Finances' })).toBeVisible();
		await page.getByRole('button', { name: 'Ajouter une charge' }).first().click();
		await expect(page.getByRole('heading', { name: 'Nouvelle charge' })).toBeVisible();

		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		await expect(page.locator('#charge-category')).toHaveJSProperty('validity.valid', false);
		expect(await validationMessage(page.locator('#charge-category')))
			.toBe('Veuillez sélectionner un élément dans la liste.');
	});

	test('finance: amount below min → French "rangeUnderflow"', async ({ page }) => {
		await login(page);
		await page.goto('/finance');
		await expect(page.getByRole('heading', { name: 'Finances' })).toBeVisible();
		await page.getByRole('button', { name: 'Ajouter une charge' }).first().click();

		await page.locator('#charge-category').selectOption('Loyer');
		await page.locator('#charge-description').fill('Loyer du cabinet');
		await page.locator('#charge-amount').fill('0');
		await page.getByRole('button', { name: 'Ajouter la charge' }).click();

		expect(await validationMessage(page.locator('#charge-amount')))
			.toBe('La valeur doit être supérieure ou égale à 1.');
	});

	test('actes: empty required <input> → French "valueMissing"', async ({ page }) => {
		await login(page);
		await page.goto('/actes');
		await page.getByRole('button', { name: 'Ajouter' }).click();

		expect(await validationMessage(page.locator('#act-name')))
			.toBe('Veuillez renseigner ce champ.');
	});

	test('team: invalid email → French "typeMismatch"', async ({ page }) => {
		await login(page);
		await page.goto('/team');
		await expect(page.getByRole('heading', { name: 'Équipe' })).toBeVisible();
		await page.getByRole('button', { name: 'Ajouter un membre' }).click();

		await page.locator('input[name="firstName"]').fill('Test');
		await page.locator('input[name="lastName"]').fill('User');
		await page.locator('input[name="password"]').fill('motdepasse123');
		await page.locator('input[name="email"]').fill('pas-un-email');
		await page.getByRole('button', { name: 'Créer le compte' }).click();

		expect(await validationMessage(page.locator('input[name="email"]')))
			.toBe('Veuillez saisir une adresse e-mail valide.');
	});

	test('team: too-short password → French "tooShort"', async ({ page }) => {
		await login(page);
		await page.goto('/team');
		await expect(page.getByRole('heading', { name: 'Équipe' })).toBeVisible();
		await page.getByRole('button', { name: 'Ajouter un membre' }).click();

		await page.locator('input[name="firstName"]').fill('Test');
		await page.locator('input[name="lastName"]').fill('User');
		await page.locator('input[name="email"]').fill('test.user@example.com');
		await page.locator('input[name="password"]').fill('123');
		await page.getByRole('button', { name: 'Créer le compte' }).click();

		expect(await validationMessage(page.locator('input[name="password"]')))
			.toBe('Veuillez saisir au moins 8 caractères.');
	});

	test('profile: empty required password → French "valueMissing"', async ({ page }) => {
		await login(page);
		await page.goto('/profile');
		await page.getByRole('button', { name: 'Sécurité' }).click();

		await page.getByRole('button', { name: 'Modifier le mot de passe' }).click();

		expect(await validationMessage(page.locator('#currentPassword')))
			.toBe('Veuillez renseigner ce champ.');
	});
});
