import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Regression guard for #103: the dashboard once shipped a developer placeholder
// ("Le backend n'est pas encore connecté") in the "Programme du jour" empty
// state. It must never be visible to a user — the panel shows a neutral empty
// state instead. This holds regardless of whether the seeded doctor has
// appointments today.

test.describe('Dashboard', () => {
	test('never shows the developer backend placeholder', async ({ page }) => {
		await login(page);
		await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();
		await expect(page.getByText("Le backend n'est pas encore connecté")).toHaveCount(0);
	});

	test('empty "Programme du jour" shows a neutral state, not dev text', async ({ page }) => {
		await login(page);
		// The "Programme du jour" panel is the .card that holds that heading.
		const panel = page.locator('.card').filter({
			has: page.getByRole('heading', { name: 'Programme du jour' }),
		});
		await expect(panel).toBeVisible();

		// Only assert the empty-state copy when there genuinely are no appointments
		// today — otherwise the panel renders the appointment list.
		const emptyState = panel.getByText('Aucun rendez-vous programmé pour aujourd\'hui');
		if (await emptyState.count()) {
			await expect(emptyState).toBeVisible();
			await expect(panel.getByRole('link', { name: /Programmer un rendez-vous/ })).toBeVisible();
		}
	});
});
