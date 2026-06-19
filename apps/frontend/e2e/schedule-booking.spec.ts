import { test, expect } from '@playwright/test';
import { login } from './helpers';

// Scenario 7 — Agenda / appointments: the failing situations when booking a RDV
// through the modal (BookingModal.submit guards). These don't depend on seeded
// data, so they run anywhere.

test.describe('Schedule — booking validation failures', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
		await page.goto('/schedule');
		await expect(page.getByRole('heading', { name: 'Agenda' })).toBeVisible();
		await page.getByRole('button', { name: 'Nouveau rendez-vous' }).click();
		await expect(page.getByRole('dialog', { name: 'Nouveau rendez-vous' })).toBeVisible();
	});

	test('confirming with no patient selected shows a patient error', async ({ page }) => {
		await page.getByRole('button', { name: 'Confirmer le rendez-vous' }).click();
		await expect(page.getByText('Veuillez sélectionner un patient.')).toBeVisible();
		// Modal stays open — nothing was booked.
		await expect(page.getByRole('dialog', { name: 'Nouveau rendez-vous' })).toBeVisible();
	});

	test('searching a nonsense term shows "Aucun patient trouvé"', async ({ page }) => {
		await page.getByLabel('Patient *').fill('zzzqqxnotapatient');
		await expect(page.getByText('Aucun patient trouvé')).toBeVisible();
	});

	test('typing a single character does not trigger a search (min 2 chars)', async ({ page }) => {
		await page.getByLabel('Patient *').fill('z');
		// Below the 2-char threshold the dropdown never opens.
		await expect(page.getByText('Aucun patient trouvé')).toHaveCount(0);
	});

	test('Escape closes the modal without booking', async ({ page }) => {
		await page.keyboard.press('Escape');
		await expect(page.getByRole('dialog', { name: 'Nouveau rendez-vous' })).toHaveCount(0);
	});

	test('Cancel button closes the modal without booking', async ({ page }) => {
		await page.getByRole('button', { name: 'Annuler' }).click();
		await expect(page.getByRole('dialog', { name: 'Nouveau rendez-vous' })).toHaveCount(0);
	});
});
