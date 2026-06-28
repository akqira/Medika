import { test, expect, type Page } from '@playwright/test';
import { login } from './helpers';

// Keyboard shortcuts are handled by a <svelte:window> listener, which only exists
// once the page has hydrated. page.keyboard.press() does NOT wait for hydration
// (unlike a click, which auto-waits for actionability), so on a slower CI box the
// keypress can race the listener attach and be lost. Focusing a real form input
// first is a user gesture that guarantees the client is interactive.
async function ensureHydrated(page: Page) {
	await page.getByPlaceholder('Motif…').click();
}

// Helper: open the ordonnance overlay from the main page.
async function openOrdonnance(page: Page) {
	// When no prescription exists the CTA is shown; when one exists the summary shows.
	// Either way, clicking the button opens the overlay.
	const cta = page.getByRole('button', { name: 'Créer une ordonnance' });
	const edit = page.getByRole('button', { name: /Modifier l'ordonnance/i });
	const btn = (await cta.isVisible()) ? cta : edit;
	await btn.click();
	await expect(page.getByRole('dialog', { name: 'Ordonnance' })).toBeVisible();
}

// Scenario 4 / 5 — Consultation & ordonnance (MediKa brand redesign, issue #135).
// Layout: 2-col (patient context + clinical) + sticky bottom action bar carrying
// honoraires and the primary save CTA.  The ordonnance is a dedicated full-screen
// overlay opened from a dashed CTA (empty) or a green "Ordonnance prête" summary.

test.describe('Consultation — save failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('saving with no patient is blocked inline before reaching the server', async ({ page }) => {
		// The save button lives in the sticky bottom action bar.
		await page.getByRole('button', { name: /Enregistrer/i }).first().click();
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
});

// New: Ordonnance dedicated full-screen window (issue #135).
test.describe('Consultation — ordonnance window', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('page shows a dashed "Créer une ordonnance" CTA when no prescription', async ({ page }) => {
		await expect(page.getByRole('button', { name: 'Créer une ordonnance' })).toBeVisible();
	});

	test('clicking the CTA opens the ordonnance overlay', async ({ page }) => {
		await page.getByRole('button', { name: 'Créer une ordonnance' }).click();
		await expect(page.getByRole('dialog', { name: 'Ordonnance' })).toBeVisible();
	});

	test('ordonnance overlay has a medication catalogue search', async ({ page }) => {
		await openOrdonnance(page);
		await expect(page.getByPlaceholder(/Rechercher un médicament/i)).toBeVisible();
	});

	test('ordonnance overlay has an "Imprimer l\'ordonnance" button in the dark header', async ({ page }) => {
		await openOrdonnance(page);
		await expect(page.getByRole('button', { name: /Imprimer l'ordonnance/i })).toBeVisible();
	});

	test('adding then removing a medication line leaves an empty ordonnance', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();

		await page.getByRole('button', { name: 'Retirer le médicament' }).click();
		await expect(page.getByText('Aucun médicament')).toBeVisible();
	});

	test('closing the ordonnance overlay with meds shows "Ordonnance prête" in center', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: 'Ajouter un médicament' }).click();
		await page.getByRole('button', { name: 'Fermer' }).click();
		await expect(page.getByRole('dialog', { name: 'Ordonnance' })).not.toBeVisible();
		await expect(page.getByText('Ordonnance prête')).toBeVisible();
	});

	test('searching the catalogue filters medications', async ({ page }) => {
		await openOrdonnance(page);
		const search = page.getByPlaceholder(/Rechercher un médicament/i);
		await search.fill('ASPIRINE');
		// At least one catalogue row should appear for a common medication.
		await expect(page.locator('[data-testid="med-catalogue-row"]').first()).toBeVisible();
	});

	test('clicking a catalogue row adds the medication to the prescription panel', async ({ page }) => {
		await openOrdonnance(page);
		const search = page.getByPlaceholder(/Rechercher un médicament/i);
		await search.fill('PARACÉTAMOL');
		await page.locator('[data-testid="med-catalogue-row"]').first().click();
		// A medication card should now appear in the prescription panel.
		await expect(page.locator('.med-card').first()).toBeVisible();
	});

	test('a frequent-medication chip adds a pre-filled line', async ({ page }) => {
		await openOrdonnance(page);
		await page.getByRole('button', { name: '+ METFORMINE 1000mg' }).click();
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();
		// The medication name input (Combobox) of the new line is pre-filled.
		await expect(page.locator('.med-card input').first()).toHaveValue('METFORMINE 1000mg');
	});
});

// Keyboard shortcuts — Alt+N opens the ordonnance and adds a medication line,
// Alt+2/Alt+1 switch clinical tabs (unaffected by the new layout).
test.describe('Consultation — keyboard shortcuts', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
	});

	test('Alt+N opens the ordonnance overlay and adds a medication line', async ({ page }) => {
		await ensureHydrated(page);
		await page.keyboard.press('Alt+n');
		await expect(page.getByRole('dialog', { name: 'Ordonnance' })).toBeVisible();
		await expect(page.getByText('1 médicament', { exact: true })).toBeVisible();
	});

	test('Alt+2 / Alt+1 switch between the Diagnostic and Motif tabs', async ({ page }) => {
		const diagnosis = page.getByPlaceholder('Diagnostic principal…');
		const motif = page.getByPlaceholder('Motif…');
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
});

// Ordonnance — posologie is a free-text field enterable directly in the med card.
test.describe('Consultation — ordonnance posologie', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/consultation');
		await expect(page.getByRole('heading', { name: /Consultation du/ })).toBeVisible();
		await openOrdonnance(page);
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
		// Blur normalises the shortcut in place.
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

// Act catalogue → consultation: an honoraire chip carries the act's tariff and
// pre-fills the montant (chips now live in the sticky bottom action bar).
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
