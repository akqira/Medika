import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #54 (revised) — the navbar global search is a live patient autocomplete:
// typing (debounced, min 2 chars) shows a dropdown of matching patients directly
// under the input; selecting a row opens that patient's file (/patients/[id]).
// It must NOT navigate to the full patients list on Enter (the earlier, wrong
// contract). Patients-only scope (truly-global search is tracked separately).
//
// Data-independent / non-mutating: uses the min-chars hint and a nonsense query,
// and asserts the no-navigation contract — no seeded patient required.

const NAV_SEARCH = 'Rechercher un patient…'; // exact navbar placeholder

test.describe('Navbar global search', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		// Stay off /patients so only the navbar input carries this placeholder.
		await page.goto('/dashboard');
		await expect(page.getByPlaceholder(NAV_SEARCH)).toBeVisible();
	});

	test('one character shows the min-length hint, not results', async ({ page }) => {
		await page.getByPlaceholder(NAV_SEARCH).fill('a');
		await expect(page.getByText('Tapez au moins 2 caractères…')).toBeVisible();
	});

	test('a nonsense term shows the empty dropdown and does NOT navigate', async ({ page }) => {
		await page.getByPlaceholder(NAV_SEARCH).fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();

		// The old contract navigated to /patients?term= on Enter — it must not anymore.
		await page.getByPlaceholder(NAV_SEARCH).press('Enter');
		await expect(page).toHaveURL(/\/dashboard/);
	});

	test('Escape closes the dropdown', async ({ page }) => {
		await page.getByPlaceholder(NAV_SEARCH).fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();
		await page.getByPlaceholder(NAV_SEARCH).press('Escape');
		await expect(page.getByText('Aucun patient trouvé')).not.toBeVisible();
	});
});
