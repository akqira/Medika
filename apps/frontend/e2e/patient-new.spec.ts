import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 3 — Add a new patient: every client-side validation that should BLOCK
// the 4-step wizard from advancing or submitting. The form validates per step in
// nextStep()/validateStep1/2/4, so a failing step must keep us on that step.

test.describe('New patient — validation failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/patients/new');
		await expect(page.getByRole('heading', { name: 'Nouveau patient' })).toBeVisible();
	});

	test('step 1: empty required fields keep us on step 1 with "Champ requis"', async ({ page }) => {
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Champ requis').first()).toBeVisible();
		await expect(page.getByText('Date de naissance requise')).toBeVisible();
		await expect(page.getByText('Étape 1 sur 4')).toBeVisible();
	});

	test('step 1: digits in the name are rejected (letters only)', async ({ page }) => {
		await page.getByLabel('PRÉNOM *').fill('Ahmed123');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Lettres uniquement, 2 caractères minimum')).toBeVisible();
		await expect(page.getByText('Étape 1 sur 4')).toBeVisible();
	});

	test('step 1: a single-letter name is too short', async ({ page }) => {
		await page.getByLabel('PRÉNOM *').fill('A');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Lettres uniquement, 2 caractères minimum')).toBeVisible();
		await expect(page.getByText('Étape 1 sur 4')).toBeVisible();
	});

	test('step 1: a name exceeding 100 characters is rejected', async ({ page }) => {
		const longName = 'A'.repeat(101);
		await page.getByLabel('PRÉNOM *').fill(longName);
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('100 caractères maximum')).toBeVisible();
		await expect(page.getByText('Étape 1 sur 4')).toBeVisible();
	});

	test('step 2: invalid phone format blocks advancing to step 3', async ({ page }) => {
		// Pass step 1 with valid identity.
		await page.getByLabel('PRÉNOM *').fill('Ahmed');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();

		// A non-Algerian number is rejected.
		await page.getByLabel('TÉLÉPHONE *').fill('12345');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Numéro algérien invalide', { exact: false }).first()).toBeVisible();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
	});

	test('step 2: empty phone is required', async ({ page }) => {
		await page.getByLabel('PRÉNOM *').fill('Ahmed');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();

		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Champ requis')).toBeVisible();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
	});

	test('step 2: malformed email is rejected', async ({ page }) => {
		await page.getByLabel('PRÉNOM *').fill('Ahmed');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await page.getByLabel('TÉLÉPHONE *').fill('0555123456');
		await page.getByLabel('EMAIL', { exact: true }).fill('not-an-email');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Adresse email invalide')).toBeVisible();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
	});

	test('step 4: a NSS that is not exactly 15 digits blocks submission', async ({ page }) => {
		// Walk through valid steps 1→4.
		await page.getByLabel('PRÉNOM *').fill('Ahmed');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await page.getByLabel('TÉLÉPHONE *').fill('0555123456');
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 3
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 4
		await expect(page.getByText('Étape 4 sur 4')).toBeVisible();

		await page.getByLabel(/NUMÉRO DE SÉCURITÉ SOCIALE/).fill('123'); // too short
		await page.getByRole('button', { name: 'Créer le dossier' }).click();

		await expect(page.getByText('Le NSS doit contenir exactement 15 chiffres')).toBeVisible();
		// validateStep4 aborts the submit — we stay on the wizard, still step 4.
		await expect(page.getByText('Étape 4 sur 4')).toBeVisible();
	});
});

// Issue #124 — phone validation must accept landlines (021…) and mobiles of every
// operator, not just the old 05/06/07-only rule. These are NON-MUTATING: advancing
// past step 2 (→ step 3) proves the number was accepted; the form is never submitted.
test.describe('New patient — phone formats (#124)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/patients/new');
		await page.getByLabel('PRÉNOM *').fill('Ahmed');
		await page.getByLabel('NOM *', { exact: true }).fill('Benali');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
	});

	// One mobile per operator + a landline — all must let the wizard advance.
	for (const { label, value } of [
		{ label: 'mobile Ooredoo (05)', value: '0555 12 34 56' },
		{ label: 'mobile Mobilis (06)', value: '0661 23 45 67' },
		{ label: 'mobile Djezzy (07)', value: '0791 23 45 67' },
		{ label: 'fixe Alger (021…)', value: '021 23 45 67' }
	]) {
		test(`accepts a ${label}`, async ({ page }) => {
			await page.getByLabel('TÉLÉPHONE *').fill(value);
			await page.getByRole('button', { name: 'Continuer' }).click();

			// Advanced to step 3 → phone accepted, no error shown.
			await expect(page.getByText('Étape 3 sur 4')).toBeVisible();
			await expect(page.getByText('Numéro algérien invalide', { exact: false })).toHaveCount(0);
		});
	}

	test('rejects a number that is neither mobile nor landline', async ({ page }) => {
		await page.getByLabel('TÉLÉPHONE *').fill('0812345678'); // 08 prefix
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Numéro algérien invalide', { exact: false }).first()).toBeVisible();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
	});
});
