import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #106 — /patients: the "N au total" counter was tracking filtered results
// instead of the cabinet's real total. These specs assert the correct split:
//   - header "N au total"  → cabinet total, immutable during search
//   - filter indicator     → filtered count, shown only when a term is active

test.describe('Patients total counter', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/patients');
		await expect(page.getByRole('heading', { name: 'Patients' })).toBeVisible();
	});

	test('header total does not change when search filters results', async ({ page }) => {
		const headerTotal = page.locator('text=/\\d+ au total/');
		await expect(headerTotal).toBeVisible();
		const initialText = await headerTotal.textContent();

		// Search for a term that matches some but not all patients
		const search = page.getByRole('searchbox', { name: /Rechercher un patient/ });
		await search.fill('a');
		// Wait for the search to settle (spinner gone or results updated)
		await page.waitForTimeout(400);

		// Header total must be unchanged
		await expect(headerTotal).toHaveText(initialText!);
	});

	test('header total does not change when search returns no results', async ({ page }) => {
		const headerTotal = page.locator('text=/\\d+ au total/');
		await expect(headerTotal).toBeVisible();
		const initialText = await headerTotal.textContent();

		const search = page.getByRole('searchbox', { name: /Rechercher un patient/ });
		await search.fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();

		// Header must still reflect the real total
		await expect(headerTotal).toHaveText(initialText!);
	});

	test('shows filtered result count when a search term is active', async ({ page }) => {
		// No filter → no result indicator
		await expect(page.locator('text=/résultat/')).not.toBeVisible();

		const search = page.getByRole('searchbox', { name: /Rechercher un patient/ });
		await search.fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();

		// Filter indicator must now be visible with "0 résultat"
		await expect(page.locator('text=/0 résultat/')).toBeVisible();
	});

	test('filter indicator disappears when search is cleared', async ({ page }) => {
		const search = page.getByRole('searchbox', { name: /Rechercher un patient/ });
		await search.fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();
		await expect(page.locator('text=/résultat/')).toBeVisible();

		// Clear the search
		await search.fill('');
		await page.waitForTimeout(400);

		// Filter indicator should be gone
		await expect(page.locator('text=/résultat/')).not.toBeVisible();
	});
});
