import { test, expect } from '@playwright/test';

// Issue #144 — <html lang="fr"> must be set on every page of the French UI.
// A wrong lang="en" causes:
//   - Native browser validation messages in English
//   - Screen readers announcing page language as English (WCAG 3.1.1)
//
// These tests are static and non-mutating — they only check the DOM attribute.

test.describe('#144 — <html lang="fr"> on all pages', () => {
	test('login page has lang="fr" on the root <html> element', async ({ page }) => {
		await page.goto('/login');
		const lang = await page.evaluate(() => document.documentElement.lang);
		expect(lang).toBe('fr');
	});

	test('forgot-password page has lang="fr"', async ({ page }) => {
		await page.goto('/forgot-password');
		const lang = await page.evaluate(() => document.documentElement.lang);
		expect(lang).toBe('fr');
	});
});
