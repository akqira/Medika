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

	test('finalising with no patient selected is rejected by the server', async ({ page }) => {
		// The draft path is gone — finalising is the only submit. Accept the
		// confirmation so the form actually submits and hits the server guard.
		page.on('dialog', (dialog) => dialog.accept());
		await page.getByRole('button', { name: 'Finaliser la consultation' }).click();
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

		const posologie = page.getByPlaceholder('1-0-1-0 ou texte libre');
		await expect(posologie).toBeVisible();
		await posologie.fill('1 comprimé au besoin');
		await expect(posologie).toHaveValue('1 comprimé au besoin');

		// Column header is "Posologie"; the old ambiguous "Prise" header is gone.
		await expect(page.getByText('Posologie', { exact: true })).toBeVisible();
		await expect(page.getByText('Prise', { exact: true })).toHaveCount(0);
	});

	test('a 1-0-1-0 pattern expands to readable moments on blur', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();

		const posologie = page.getByPlaceholder('1-0-1-0 ou texte libre');
		await posologie.fill('1-0-1-0');
		// Live hint previews the expansion before committing.
		await expect(page.getByText('= 1 matin, 1 soir')).toBeVisible();
		// Blur normalises the shortcut in place (matin-midi-soir-coucher).
		await posologie.blur();
		await expect(posologie).toHaveValue('1 matin, 1 soir');
	});

	test('free-text posology is left untouched (shortcut is additive)', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();

		const posologie = page.getByPlaceholder('1-0-1-0 ou texte libre');
		await posologie.fill('1 cp au besoin');
		await posologie.blur();
		await expect(posologie).toHaveValue('1 cp au besoin');
	});
});

// Act catalogue → consultation: selecting an act pre-fills the honoraires.
test.describe('Consultation — act picker', () => {
	test('selecting a catalogue act pre-fills the honoraires', async ({ page }) => {
		await login(page);

		// Seed an act in the catalogue and grab its id.
		const name = `E2E acte ${Date.now()}`;
		await page.goto('/actes');
		await page.locator('#act-name').fill(name);
		await page.locator('#act-tariff').fill('3000');
		await page.getByRole('button', { name: 'Ajouter' }).click();
		const row = page.locator('tr[data-act-id]').filter({ hasText: name });
		await expect(row).toBeVisible();
		const actId = await row.getAttribute('data-act-id');

		try {
			await page.goto('/consultation');
			await expect(page.getByRole('heading', { name: 'Nouvelle consultation' })).toBeVisible();

			await page.locator('#act').selectOption(actId!);
			// Honoraires auto-filled from the act's tariff.
			await expect(page.locator('#tariff')).toHaveValue('3000');
		} finally {
			// Cleanup so the suite stays re-runnable.
			await page.goto('/actes');
			await page
				.locator('tr[data-act-id]')
				.filter({ hasText: name })
				.getByRole('button', { name: "Supprimer l'acte" })
				.click();
		}
	});
});
