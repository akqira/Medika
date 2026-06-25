import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Issue #57 — Agenda: the day timeline must start at 07:00 (was 08:00).
// Failing-path-first: assert the 07:00 marker is present and that no hour
// earlier than 07:00 (e.g. 06:00) leaks in. Non-mutating, data-independent.

test.describe('Schedule — timeline starts at 07:00', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/schedule');
		await expect(page.getByRole('heading', { name: 'Agenda' })).toBeVisible();
	});

	test('shows the 07:00 hour marker', async ({ page }) => {
		await expect(page.getByText('07:00', { exact: true })).toBeVisible();
	});

	test('does not show any hour before 07:00', async ({ page }) => {
		await expect(page.getByText('06:00', { exact: true })).toHaveCount(0);
		await expect(page.getByText('05:00', { exact: true })).toHaveCount(0);
	});

	test('still shows the rest of the working day (08:00, 20:00)', async ({ page }) => {
		await expect(page.getByText('08:00', { exact: true })).toBeVisible();
		await expect(page.getByText('20:00', { exact: true })).toBeVisible();
	});
});
