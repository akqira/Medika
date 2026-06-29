import { test, expect } from '@playwright/test';

// Issue #144 — the root document must declare lang="fr" so that native browser
// validation messages render in French and screen readers announce the correct
// language (WCAG 3.1.1). The template lives in src/app.html and applies to every
// route, so a public page is enough to guard the regression.

test.describe('Document language', () => {
	test('<html> declares lang="fr" on a public page', async ({ page }) => {
		await page.goto('/login');
		await expect(page.locator('html')).toHaveAttribute('lang', 'fr');
	});

	test('<html> declares lang="fr" on the forgot-password page', async ({ page }) => {
		await page.goto('/forgot-password');
		await expect(page.locator('html')).toHaveAttribute('lang', 'fr');
	});
});
