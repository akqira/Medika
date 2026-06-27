import { test, expect, type Page } from '@playwright/test';
import { login } from './helpers';

// Issue #126 — the patient dossier must be editable. The "Modifier" button on the
// dossier opens a single-page edit form (all sections on one page, section nav on the
// left); saving persists the change cabinet-scoped, and invalid input is rejected on
// save. CI seeds only the doctor (no patients), so we create our own subject first.

/** Run the create wizard with valid data and return the new patient id. */
async function createPatient(page: Page): Promise<string> {
	await page.goto('/patients/new');
	await expect(page.getByRole('heading', { name: 'Nouveau patient' })).toBeVisible();

	await page.getByLabel('PRÉNOM *').fill('Sarah');
	await page.getByLabel('NOM *', { exact: true }).fill('TestEdit');
	await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
	await page.getByRole('button', { name: 'Continuer' }).click();

	await page.getByLabel('TÉLÉPHONE *').fill('0555123456');
	await page.getByRole('button', { name: 'Continuer' }).click(); // → step 3
	await page.getByRole('button', { name: 'Continuer' }).click(); // → step 4
	await page.getByRole('button', { name: 'Créer le dossier' }).click();

	await expect(page).toHaveURL(/\/patients\?created=/);
	const id = new URL(page.url()).searchParams.get('created');
	expect(id).toBeTruthy();
	return id!;
}

// One shared patient for the whole spec (created once, then edited).
let patientId: string;

test.beforeAll(async ({ browser }) => {
	const page = await browser.newPage();
	await login(page);
	patientId = await createPatient(page);
	await page.close();
});

test.describe('Patient dossier — edit', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
	});

	test('"Modifier" opens the single-page edit form, prefilled, all sections present', async ({ page }) => {
		await page.goto(`/patients/${patientId}`);
		await page.getByRole('link', { name: 'Modifier' }).click();

		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}/edit`));
		await expect(page.getByRole('heading', { name: /Modifier le dossier/ })).toBeVisible();

		// Identity is prefilled from the existing dossier.
		await expect(page.getByLabel('Prénom', { exact: true })).toHaveValue('Sarah');
		await expect(page.getByLabel('Nom', { exact: true })).toHaveValue('TestEdit');

		// All four sections live on one page (no wizard steps) — assert each section's
		// unique descriptor is on screen at once.
		for (const desc of [
			'Nom, naissance, groupe sanguin',
			'Téléphone, adresse, urgence',
			'Allergies, antécédents, traitement',
			'NSS et couverture sociale',
		]) {
			await expect(page.getByText(desc, { exact: true })).toBeVisible();
		}
	});

	test('editing a field saves, toasts, and persists across a reload', async ({ page }) => {
		const treatment = 'Amlodipine 5mg (édité E2E)';

		await page.goto(`/patients/${patientId}/edit`);
		await expect(page.getByRole('heading', { name: /Modifier le dossier/ })).toBeVisible();

		await page.getByLabel('Traitement en cours').fill(treatment);
		await page.getByRole('button', { name: 'Enregistrer les modifications' }).first().click();

		// Back on the dossier with the success toast.
		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}(\\?|$)`));
		await expect(page.getByText('Dossier patient mis à jour.')).toBeVisible();

		// Persisted: a fresh load still shows the edited treatment.
		await page.goto(`/patients/${patientId}`);
		await expect(page.getByText(treatment)).toBeVisible();
	});

	test('clearing a required name blocks the save', async ({ page }) => {
		await page.goto(`/patients/${patientId}/edit`);
		await page.getByLabel('Prénom', { exact: true }).fill('');
		await page.getByRole('button', { name: 'Enregistrer les modifications' }).first().click();

		await expect(page.getByText('Champ requis')).toBeVisible();
		// Save is cancelled client-side — we stay on the edit page.
		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}/edit`));
	});

	test('an invalid phone is rejected on save', async ({ page }) => {
		await page.goto(`/patients/${patientId}/edit`);
		await page.getByLabel('Téléphone', { exact: true }).fill('12345');
		await page.getByRole('button', { name: 'Enregistrer les modifications' }).first().click();

		await expect(page.getByText('Format invalide — ex: 0555 12 34 56').first()).toBeVisible();
		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}/edit`));
	});

	test('a NSS that is not exactly 15 digits is rejected on save', async ({ page }) => {
		await page.goto(`/patients/${patientId}/edit`);
		await page.getByLabel(/Numéro de sécurité sociale/).fill('123');
		await page.getByRole('button', { name: 'Enregistrer les modifications' }).first().click();

		await expect(page.getByText('Le NSS doit contenir exactement 15 chiffres')).toBeVisible();
		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}/edit`));
	});
});
