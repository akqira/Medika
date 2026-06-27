import { test, expect, type Page } from '@playwright/test';
import { login } from './helpers';

// Issue #126 — the patient dossier must be editable. The "Modifier" button on the
// dossier opens a prefilled 4-step wizard (mirrors the create form); saving persists
// the change cabinet-scoped, and invalid input is rejected per-step like the create
// wizard. CI seeds only the doctor (no patients), so we create our own subject first.

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

	test('"Modifier" on the dossier opens the prefilled edit wizard', async ({ page }) => {
		await page.goto(`/patients/${patientId}`);
		await page.getByRole('link', { name: 'Modifier' }).click();

		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}/edit`));
		await expect(page.getByRole('heading', { name: 'Modifier le dossier' })).toBeVisible();
		// Identity is prefilled from the existing dossier.
		await expect(page.getByLabel('PRÉNOM *')).toHaveValue('Sarah');
		await expect(page.getByLabel('NOM *', { exact: true })).toHaveValue('TestEdit');
	});

	test('editing a field saves, toasts, and persists across a reload', async ({ page }) => {
		const treatment = 'Amlodipine 5mg (édité E2E)';

		await page.goto(`/patients/${patientId}/edit`);
		await expect(page.getByRole('heading', { name: 'Modifier le dossier' })).toBeVisible();

		// Steps 1 & 2 are already valid (prefilled) → advance to the Médical step.
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 2
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 3
		await expect(page.getByText('Étape 3 sur 4')).toBeVisible();

		await page.getByLabel('TRAITEMENT EN COURS').fill(treatment);
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 4
		await page.getByRole('button', { name: 'Enregistrer les modifications' }).click();

		// Back on the dossier with the success toast.
		await expect(page).toHaveURL(new RegExp(`/patients/${patientId}(\\?|$)`));
		await expect(page.getByText('Dossier patient mis à jour.')).toBeVisible();

		// Persisted: a fresh load still shows the edited treatment.
		await page.goto(`/patients/${patientId}`);
		await expect(page.getByText(treatment)).toBeVisible();
	});

	test('step 1: clearing a required name blocks advancing', async ({ page }) => {
		await page.goto(`/patients/${patientId}/edit`);
		await expect(page.getByRole('heading', { name: 'Modifier le dossier' })).toBeVisible();

		await page.getByLabel('PRÉNOM *').fill('');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Champ requis')).toBeVisible();
		await expect(page.getByText('Étape 1 sur 4')).toBeVisible();
	});

	test('step 2: an invalid phone is rejected', async ({ page }) => {
		await page.goto(`/patients/${patientId}/edit`);
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 2
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();

		await page.getByLabel('TÉLÉPHONE *').fill('12345');
		await page.getByRole('button', { name: 'Continuer' }).click();

		await expect(page.getByText('Format invalide — ex: 0555 12 34 56').first()).toBeVisible();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();
	});

	test('step 4: a NSS that is not exactly 15 digits blocks the save', async ({ page }) => {
		await page.goto(`/patients/${patientId}/edit`);
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 2
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 3
		await page.getByRole('button', { name: 'Continuer' }).click(); // → step 4
		await expect(page.getByText('Étape 4 sur 4')).toBeVisible();

		await page.getByLabel(/NUMÉRO DE SÉCURITÉ SOCIALE/).fill('123');
		await page.getByRole('button', { name: 'Enregistrer les modifications' }).click();

		await expect(page.getByText('Le NSS doit contenir exactement 15 chiffres')).toBeVisible();
		await expect(page.getByText('Étape 4 sur 4')).toBeVisible();
	});
});
