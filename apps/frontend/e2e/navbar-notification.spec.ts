import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #82 — the navbar had a decorative notification bell whose amber status
// dot was always lit, implying unread notifications. The app has no notification
// system, so the icon was misleading and was removed.
//
// Regression anchor: the bell's always-on amber dot (#F59E0B) lived as a <span>
// inside the navbar. Before removal this locator matched; it must now be absent.
// Data-independent / non-mutating — no seeded data required.

test.describe('Navbar notification bell (removed — issue #82)', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/dashboard');
	});

	test('the navbar shows no notification bell / status dot', async ({ page }) => {
		// The nav still renders its real controls…
		await expect(page.getByPlaceholder('Rechercher un patient…')).toBeVisible();
		await expect(page.getByRole('link', { name: 'Consultation', exact: true })).toBeVisible();

		// …but the misleading always-on amber notification dot is gone.
		await expect(page.locator('nav span[style*="F59E0B"]')).toHaveCount(0);
	});
});
