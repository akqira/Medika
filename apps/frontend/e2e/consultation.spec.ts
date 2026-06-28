import { test, expect, type Page } from '@playwright/test';
import { login } from './helpers';

// Keyboard shortcuts are handled by a <svelte:window> listener, which only exists
// once the page has hydrated. page.keyboard.press() does NOT wait for hydration
// (unlike a click, which auto-waits for actionability), so on a slower CI box the
// keypress can race the listener attach and be lost. Focusing a real form input
// first is a user gesture that guarantees the client is interactive.
async function ensureHydrated(page: Page) {
	await page.getByPlaceholder('Saisissez le motif…').click();
}

// The seeded DB has NO patients (only the doctor), so the suite must create its
// own. The consultation page's inline "Nouveau" quick-add creates a patient and
// auto-selects it — that's both hydration-safe and independent of seed data.
async function createAndSelectPatient(page: Page) {
	await page.goto('/consultation');
	await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	await ensureHydrated(page);
	await page.getByRole('button', { name: 'Nouveau' }).click();
	await page.locator('#qap-firstName').fill('Test');
	await page.locator('#qap-lastName').fill('Ordonnance');
	await page.locator('#qap-phone').fill('0555123456');
	await page.locator('#qap-dob').fill('1990-05-15');
	await page.getByRole('button', { name: 'Créer le patient' }).click();
	// Auto-selected → the ordonnance CTA flips enabled.
	await expect(page.getByRole('button', { name: 'Créer une ordonnance', exact: true })).toBeEnabled();
}

// Opens the dedicated ordonnance window (search-driven, new MediKa brand design).
async function openOrdonnance(page: Page) {
	await page.getByRole('button', { name: 'Créer une ordonnance', exact: true }).click();
	await expect(page.getByRole('dialog', { name: 'Nouvelle ordonnance' })).toBeVisible();
}

// Scenario 4 / 5 — Consultation & ordonnance (new brand design, issue #135).
// Saving is a single fast action ("Enregistrer & encaisser …"): client-side
// validation blocks an incomplete consultation inline (no native confirm dialog),
// and the server still guards the no-patient case as a backstop. The ordonnance
// now lives in a dedicated full-screen, search-driven window.

test.describe('Consultation — save failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('saving with no patient is blocked inline before reaching the server', async ({ page }) => {
		await ensureHydrated(page); // the save handler is client-side — wait for hydration
		await page.getByRole('button', { name: /Enregistrer & encaisser/ }).click();
		// Inline, UI-styled validation message — no dialog, no navigation.
		await expect(page.getByText('Sélectionnez un patient')).toBeVisible();
		await expect(page).toHaveURL(/\/consultation/);
	});

	test('Ctrl+Enter triggers the same inline validation', async ({ page }) => {
		await ensureHydrated(page);
		await page.keyboard.press('Control+Enter');
		await expect(page.getByText('Sélectionnez un patient')).toBeVisible();
		await expect(page).toHaveURL(/\/consultation/);
	});

	test('the ordonnance CTA is disabled until a patient is picked', async ({ page }) => {
		// Fresh load, no patient → the action-bar CTA is disabled.
		await expect(page.getByRole('button', { name: 'Créer une ordonnance', exact: true })).toBeDisabled();
	});
});

// Keyboard shortcuts — Alt+2/Alt+1 switch clinical tabs; Alt+N opens the ordonnance window.
test.describe('Consultation — keyboard shortcuts', () => {
	test('Alt+2 / Alt+1 switch between the Diagnostic and Motif tabs', async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();

		const diagnosis = page.getByPlaceholder('Diagnostic retenu…');
		const motif = page.getByPlaceholder('Saisissez le motif…');
		// Motif panel is active by default; diagnostic panel is in the DOM but hidden.
		await expect(motif).toBeVisible();
		await expect(diagnosis).toBeHidden();
		await ensureHydrated(page);

		await page.keyboard.press('Alt+2');
		await expect(diagnosis).toBeVisible();

		await page.keyboard.press('Alt+1');
		await expect(motif).toBeVisible();
		await expect(diagnosis).toBeHidden();
	});

	test('Alt+N opens the ordonnance window once a patient is selected', async ({ page }) => {
		await login(page);
		await createAndSelectPatient(page);
		await page.keyboard.press('Alt+n');
		await expect(page.getByRole('dialog', { name: 'Nouvelle ordonnance' })).toBeVisible();
	});
});

// Ordonnance window — the centerpiece of the redesign: a search-driven catalogue
// on the left, the prescription being built on the right.
test.describe('Consultation — ordonnance window', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await createAndSelectPatient(page);
	});

	test('opening shows the dedicated window with the searchable catalogue', async ({ page }) => {
		await openOrdonnance(page);
		await expect(page.getByRole('textbox', { name: 'Rechercher un médicament' })).toBeVisible();
		await expect(page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first()).toBeVisible();
		// Empty prescription panel guides the user.
		await expect(page.getByText('Recherchez un médicament à gauche')).toBeVisible();
		// Print is disabled while the ordonnance is empty.
		await expect(page.getByRole('button', { name: "Imprimer l'ordonnance" }).first()).toBeDisabled();
	});

	test('search narrows the catalogue', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('textbox', { name: 'Rechercher un médicament' }).fill('amox');
		await expect(page.getByRole('button', { name: /AMOXICILLINE 1g/ }).first()).toBeVisible();
		await expect(page.getByRole('button', { name: /PARACÉTAMOL/ })).toHaveCount(0);
	});

	test('picking a catalogue medication fills the prescription panel and enables print', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first().click();

		// Editable line pre-filled from the catalogue (name + posology).
		await expect(page.getByRole('textbox', { name: 'Nom + dosage' })).toHaveValue('PARACÉTAMOL 1g');
		await expect(page.getByPlaceholder('Posologie (ex. 1-0-1-0 ou 1 comprimé matin et soir)')).toHaveValue(
			'1 comprimé 3 fois par jour si douleur'
		);
		// Print is now available.
		await expect(page.getByRole('button', { name: "Imprimer l'ordonnance" }).first()).toBeEnabled();
	});

	test('the prescription line exposes no quantity (boîtes) field (issue #125)', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first().click();

		// The line is present (name + posology), but the old "Quantité (boîtes)"
		// stepper — the only number input inside the ordonnance window — must be
		// gone. Scope to the dialog so the page's Taille / honoraires number
		// inputs behind the modal don't false-positive.
		const dialog = page.getByRole('dialog', { name: 'Nouvelle ordonnance' });
		await expect(dialog.getByRole('textbox', { name: 'Nom + dosage' })).toBeVisible();
		await expect(dialog.getByRole('spinbutton')).toHaveCount(0);
	});

	test('a 1-0-1-0 posology expands to readable moments on blur', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first().click();

		const posologie = page.getByPlaceholder('Posologie (ex. 1-0-1-0 ou 1 comprimé matin et soir)');
		await posologie.fill('1-0-1-0');
		await posologie.blur();
		await expect(posologie).toHaveValue('1 matin, 1 soir');
	});

	test('removing the line empties the ordonnance again', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first().click();
		await expect(page.getByRole('textbox', { name: 'Nom + dosage' })).toBeVisible();

		await page.getByRole('button', { name: 'Retirer' }).click();
		await expect(page.getByText('Recherchez un médicament à gauche')).toBeVisible();
		await expect(page.getByRole('button', { name: "Imprimer l'ordonnance" }).first()).toBeDisabled();
	});

	test('closing with a medication surfaces the "Ordonnance prête" summary', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first().click();
		// exact: a "Patient créé" toast also exposes a "Fermer la notification" button.
		await page.getByRole('button', { name: 'Fermer', exact: true }).click();

		await expect(page.getByText('Ordonnance prête')).toBeVisible();
		await expect(page.getByText('1 médicament prescrit')).toBeVisible();
	});

	test('the print preview renders the ordonnance médicale', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: /PARACÉTAMOL 1g/ }).first().click();
		await page.getByRole('button', { name: "Imprimer l'ordonnance" }).first().click();

		await expect(page.getByText('ORDONNANCE MÉDICALE')).toBeVisible();
		await expect(page.getByText('Aperçu avant impression')).toBeVisible();
	});
});

// Act catalogue → consultation: an honoraire chip carries the act's tariff and
// pre-fills the montant (the act picker is a quick-pick chip row in the action bar).
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
