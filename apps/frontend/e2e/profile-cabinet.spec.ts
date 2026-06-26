import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #83 — "Ville" and "Wilaya" designated the same information on the
// "Mon cabinet" tab. The fix removes the redundant free-text "Ville" field and
// keeps the Wilaya dropdown as the single locality field. These assertions are
// read-only (no save), so the suite stays re-runnable.

test.describe('Profile — cabinet locality (issue #83)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/profile');
		// "Mon cabinet" is the default tab.
		await expect(page.getByRole('button', { name: 'Mon cabinet' })).toBeVisible();
	});

	test('the redundant "Ville" field is gone', async ({ page }) => {
		await expect(page.getByLabel('Ville')).toHaveCount(0);
	});

	test('the Wilaya dropdown is kept as the single locality field', async ({ page }) => {
		const wilaya = page.getByLabel('Wilaya');
		await expect(wilaya).toBeVisible();
		// It is a <select>, not a free-text input.
		await expect(wilaya).toHaveJSProperty('tagName', 'SELECT');
	});
});
