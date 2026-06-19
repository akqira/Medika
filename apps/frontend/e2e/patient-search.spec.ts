import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 8 — Patient search: the edge / failing situations of the search list
// (empty results, keyboard nav with nothing to select).

test.describe('Patient search — edge cases', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/patients');
		await expect(page.getByRole('heading', { name: 'Patients' })).toBeVisible();
	});

	test('a nonsense query shows the empty state', async ({ page }) => {
		await page.getByRole('searchbox', { name: /Rechercher un patient/ }).fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();
	});

	test('keyboard navigation on an empty result set is a no-op (no crash)', async ({ page }) => {
		const search = page.getByRole('searchbox', { name: /Rechercher un patient/ });
		await search.fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();

		// onSearchKeydown bails out when patients.length === 0 — Enter must not navigate.
		await search.press('ArrowDown');
		await search.press('Enter');
		await expect(page).toHaveURL(/\/patients(\?|$)/);
	});
});
