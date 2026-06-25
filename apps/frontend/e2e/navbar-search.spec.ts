import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #54 — the navbar global search must stop being decorative: typing a term
// and pressing Enter navigates to the Patients page pre-filtered (/patients?term=).
// Patients-only scope (truly-global search is tracked separately).
//
// Failing-path-first: before wiring, the input did nothing on Enter. These assert
// the navigation contract. Non-mutating, data-independent.

const NAV_SEARCH = 'Rechercher un patient…'; // exact navbar placeholder

test.describe('Navbar global search', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		// Start on a non-Patients page so only the navbar input matches the placeholder.
		await page.goto('/dashboard');
		await expect(page.getByPlaceholder(NAV_SEARCH)).toBeVisible();
	});

	test('Enter on a term navigates to the patients page pre-filtered', async ({ page }) => {
		await page.getByPlaceholder(NAV_SEARCH).fill('Dupont');
		await page.getByPlaceholder(NAV_SEARCH).press('Enter');

		await page.waitForURL(/\/patients\?term=Dupont/);
		// The Patients page search box reflects the carried-over term.
		await expect(page.getByPlaceholder(/pour naviguer/)).toHaveValue('Dupont');
	});

	test('a term with spaces/diacritics is URL-encoded', async ({ page }) => {
		await page.getByPlaceholder(NAV_SEARCH).fill('Élodie Martin');
		await page.getByPlaceholder(NAV_SEARCH).press('Enter');

		// Lands on patients with the encoded term; the page box shows the decoded value.
		await page.waitForURL(/\/patients\?term=/);
		await expect(page).toHaveURL(/term=%C3%89lodie(\+|%20)Martin/);
		await expect(page.getByPlaceholder(/pour naviguer/)).toHaveValue('Élodie Martin');
	});

	test('Enter on an empty box goes to the patients list (no crash)', async ({ page }) => {
		await page.getByPlaceholder(NAV_SEARCH).press('Enter');
		await page.waitForURL(/\/patients/);
		await expect(page.getByRole('heading', { name: 'Patients' })).toBeVisible();
	});
});
