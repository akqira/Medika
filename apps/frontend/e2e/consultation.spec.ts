import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 4 / 5 — Consultation & ordonnance: the failing situations when saving.
// The server action rejects a consultation with no patient, and finalising must
// go through an explicit confirmation step.

test.describe('Consultation — save failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: 'Nouvelle consultation' })).toBeVisible();
	});

	test('saving a draft with no patient selected is rejected by the server', async ({ page }) => {
		await page.getByRole('button', { name: 'Enregistrer brouillon' }).click();
		await expect(page.getByText('Veuillez sélectionner un patient.')).toBeVisible();
		// Still on the consultation page — nothing was created.
		await expect(page).toHaveURL(/\/consultation/);
	});

	test('finalising asks for confirmation; dismissing it does not submit', async ({ page }) => {
		let dialogMessage = '';
		// Playwright auto-dismisses dialogs (confirm → false), which cancels finalize.
		page.on('dialog', (dialog) => {
			dialogMessage = dialog.message();
			dialog.dismiss();
		});

		await page.getByRole('button', { name: 'Finaliser la consultation' }).click();

		await expect.poll(() => dialogMessage).toContain('Finaliser la consultation');
		// Dismissed → no submit, no patient error, still on the page.
		await expect(page.getByText('Veuillez sélectionner un patient.')).toHaveCount(0);
		await expect(page).toHaveURL(/\/consultation/);
	});

	test('adding then removing a medication line leaves an empty ordonnance', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();

		await page.getByRole('button', { name: 'Retirer le médicament' }).click();
		await expect(page.getByText('Aucun médicament')).toBeVisible();
	});
});

// Ordonnance — posologie is a free-text field (the ambiguous "Prise"/"Fréquence"
// column was dropped; dosage/posologie is now directly enterable, no longer hidden).
test.describe('Consultation — ordonnance posologie', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: 'Nouvelle consultation' })).toBeVisible();
	});

	test('posologie is a visible free-text input on a medication line', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();

		const posologie = page.getByPlaceholder('1 cp matin et soir');
		await expect(posologie).toBeVisible();
		await posologie.fill('1 comprimé matin et soir');
		await expect(posologie).toHaveValue('1 comprimé matin et soir');

		// Column header is "Posologie"; the old ambiguous "Prise" header is gone.
		await expect(page.getByText('Posologie', { exact: true })).toBeVisible();
		await expect(page.getByText('Prise', { exact: true })).toHaveCount(0);
	});
});
