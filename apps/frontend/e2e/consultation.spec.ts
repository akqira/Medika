import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 4 / 5 — Consultation & ordonnance (redesigned cockpit, issue #79).
// Saving is a single fast action ("Enregistrer la consultation"): client-side
// validation blocks an incomplete consultation inline (no native confirm dialog),
// and the server still guards the no-patient case as a backstop.

test.describe('Consultation — save failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('saving with no patient is blocked inline before reaching the server', async ({ page }) => {
		await page.getByRole('button', { name: 'Enregistrer la consultation' }).click();
		// Inline, UI-styled validation message — no dialog, no navigation.
		await expect(page.getByText('Sélectionnez un patient')).toBeVisible();
		await expect(page).toHaveURL(/\/consultation/);
	});

	test('Ctrl+Enter triggers the same inline validation', async ({ page }) => {
		await page.keyboard.press('Control+Enter');
		await expect(page.getByText('Sélectionnez un patient')).toBeVisible();
		await expect(page).toHaveURL(/\/consultation/);
	});

	test('adding then removing a medication line leaves an empty ordonnance', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();

		await page.getByRole('button', { name: 'Retirer le médicament' }).click();
		await expect(page.getByText('Aucun médicament')).toBeVisible();
	});
});

// Keyboard shortcuts — Alt+N adds a medication line, Alt+2/Alt+1 switch clinical tabs.
test.describe('Consultation — keyboard shortcuts', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('Alt+N adds a medication line', async ({ page }) => {
		await expect(page.getByText('Aucun médicament')).toBeVisible();
		await page.keyboard.press('Alt+n');
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();
	});

	test('Alt+2 / Alt+1 switch between the Diagnostic and Motif tabs', async ({ page }) => {
		const diagnosis = page.getByPlaceholder('Diagnostic principal…');
		const motif = page.getByPlaceholder('Motif…');
		// Motif panel is active by default; diagnostic panel is in the DOM but hidden.
		await expect(motif).toBeVisible();
		await expect(diagnosis).toBeHidden();

		await page.keyboard.press('Alt+2');
		await expect(diagnosis).toBeVisible();

		await page.keyboard.press('Alt+1');
		await expect(motif).toBeVisible();
		await expect(diagnosis).toBeHidden();
	});
});

// Ordonnance — posologie is a free-text field (the ambiguous "Prise"/"Fréquence"
// column was dropped; dosage/posologie is now directly enterable).
test.describe('Consultation — ordonnance posologie', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('posologie is a visible free-text input on a medication line', async ({ page }) => {
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();

		const posologie = page.getByPlaceholder('1-0-1-0 ou texte libre');
		await expect(posologie).toBeVisible();
		await posologie.fill('1 comprimé au besoin');
		await expect(posologie).toHaveValue('1 comprimé au besoin');

		// The old ambiguous "Prise" column header is gone.
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

	test('a frequent-medication chip adds a pre-filled line', async ({ page }) => {
		await page.getByRole('button', { name: '+ METFORMINE 1000mg' }).click();
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();
		// The medication name input (Combobox) of the new line is pre-filled.
		await expect(page.locator('.med-card input').first()).toHaveValue('METFORMINE 1000mg');
	});
});

// Act catalogue → consultation: an honoraire chip carries the act's tariff and
// pre-fills the montant (the act picker is now a quick-pick chip row).
test.describe('Consultation — honoraire chips', () => {
	test('clicking a catalogue-act chip pre-fills the montant', async ({ page }) => {
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
			await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();

			await page.locator(`.fee-chip[data-act-id="${actId}"]`).click();
			// Montant auto-filled from the act's tariff.
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
