import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #126 — Patient dossier: make all fields editable.
// Failing-path-first: missing "Modifier" button, invalid phone blocked, save persists.

test.describe('Patient edit — dossier édition (#126)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		// Navigate to the patient list and open the first patient.
		await page.goto('/patients');
		await expect(page.getByRole('heading', { name: 'Patients' })).toBeVisible();
		const firstPatient = page.locator('a[href^="/patients/"]').first();
		await expect(firstPatient).toBeVisible();
		await firstPatient.click();
		await expect(page).toHaveURL(/\/patients\/.+/);
	});

	test('dossier has a "Modifier" button in read mode', async ({ page }) => {
		await expect(page.getByRole('button', { name: 'Modifier' })).toBeVisible();
	});

	test('clicking Modifier shows editable fields and Enregistrer / Annuler buttons', async ({
		page
	}) => {
		await page.getByRole('button', { name: 'Modifier' }).click();

		await expect(page.getByLabel('Prénom *')).toBeVisible();
		await expect(page.getByLabel('Nom *')).toBeVisible();
		await expect(page.getByLabel('Téléphone *')).toBeVisible();
		await expect(page.getByRole('button', { name: 'Enregistrer' })).toBeVisible();
		await expect(page.getByRole('button', { name: 'Annuler' })).toBeVisible();
	});

	test('Annuler returns to read mode without saving', async ({ page }) => {
		await page.getByRole('button', { name: 'Modifier' }).click();
		await expect(page.getByRole('button', { name: 'Enregistrer' })).toBeVisible();

		await page.getByRole('button', { name: 'Annuler' }).click();

		await expect(page.getByRole('button', { name: 'Modifier' })).toBeVisible();
		await expect(page.getByRole('button', { name: 'Enregistrer' })).toBeHidden();
	});

	test('invalid phone format blocks save with validation message', async ({ page }) => {
		await page.getByRole('button', { name: 'Modifier' }).click();

		await page.getByLabel('Téléphone *').clear();
		await page.getByLabel('Téléphone *').fill('12345');

		await page.getByRole('button', { name: 'Enregistrer' }).click();

		await expect(page.getByText('Format invalide — ex: 0555 12 34 56')).toBeVisible();
		// Still in edit mode — form was NOT submitted.
		await expect(page.getByRole('button', { name: 'Enregistrer' })).toBeVisible();
	});

	test('empty required fields block save', async ({ page }) => {
		await page.getByRole('button', { name: 'Modifier' }).click();

		await page.getByLabel('Prénom *').clear();
		await page.getByRole('button', { name: 'Enregistrer' }).click();

		await expect(page.getByText('Champ requis').first()).toBeVisible();
		await expect(page.getByRole('button', { name: 'Enregistrer' })).toBeVisible();
	});

	test('edit phone, save, reload → updated value persisted', async ({ page }) => {
		await page.getByRole('button', { name: 'Modifier' }).click();

		const phoneInput = page.getByLabel('Téléphone *');
		await phoneInput.clear();
		await phoneInput.fill('0661 11 22 33');

		await page.getByRole('button', { name: 'Enregistrer' }).click();

		// Success toast appears.
		await expect(
			page.getByRole('status').filter({ hasText: 'Dossier patient mis à jour.' })
		).toBeVisible();

		// After save the page reloads — verify the new phone is visible.
		await expect(page.getByText('0661 11 22 33')).toBeVisible();
	});
});
