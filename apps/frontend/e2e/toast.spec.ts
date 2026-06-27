import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #129 — Toaster ported from eGestion and wired to the mutating flows.
// Acceptance criterion: "un toast de succès apparaît après création patient".
//
// Unlike the validation specs (non-mutating), this walks the full happy path and
// DOES create a patient, then asserts the global toast fires on /patients. The
// seeded cabinet tolerates throwaway rows; /reset-newjoiner clears them if needed.

test.describe('Toaster — success notifications (#129)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
	});

	test('creating a patient shows a success toast on /patients', async ({ page }) => {
		await page.goto('/patients/new');
		await expect(page.getByRole('heading', { name: 'Nouveau patient' })).toBeVisible();

		// Step 1 — Identité
		await page.getByLabel('PRÉNOM *').fill('Toast');
		await page.getByLabel('NOM *', { exact: true }).fill('Notification');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
		await page.getByRole('button', { name: 'Continuer' }).click();

		// Step 2 — Coordonnées
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
		await page.getByLabel('TÉLÉPHONE *').fill('0555 12 34 56');
		await page.getByRole('button', { name: 'Continuer' }).click();

		// Step 3 — Médical (no required fields)
		await expect(page.getByText('Étape 3 sur 4')).toBeVisible();
		await page.getByRole('button', { name: 'Continuer' }).click();

		// Step 4 — Assurance (NSS optional) → submit
		await expect(page.getByText('Étape 4 sur 4')).toBeVisible();
		await page.getByRole('button', { name: 'Créer le dossier' }).click();

		// Lands on the patients list and the success toast is shown.
		await page.waitForURL('**/patients**');
		await expect(page.getByRole('status').filter({ hasText: 'Patient créé avec succès.' })).toBeVisible();
	});

	test('the success toast auto-dismisses', async ({ page }) => {
		await page.goto('/patients/new');
		await page.getByLabel('PRÉNOM *').fill('Toast');
		await page.getByLabel('NOM *', { exact: true }).fill('Autodismiss');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1985-03-04');
		await page.getByRole('button', { name: 'Continuer' }).click();
		await page.getByLabel('TÉLÉPHONE *').fill('0661 22 33 44');
		await page.getByRole('button', { name: 'Continuer' }).click();
		await page.getByRole('button', { name: 'Continuer' }).click();
		await page.getByRole('button', { name: 'Créer le dossier' }).click();

		const toastEl = page.getByRole('status').filter({ hasText: 'Patient créé avec succès.' });
		await expect(toastEl).toBeVisible();
		// Auto-dismiss is 4s; allow margin.
		await expect(toastEl).toBeHidden({ timeout: 7000 });
	});
});
