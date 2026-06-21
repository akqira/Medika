import { test, expect, type Page } from '@playwright/test';
import { login } from './helpers';

// Regression — Scenario 3 (Add patient), the NSS-null duplicate-key bug.
//
// NSS is OPTIONAL. The patients collection had a Unique+Sparse index on `nss`,
// but a sparse index skips only MISSING fields, not explicit nulls — and the
// Patient document serializes Nss as an explicit null. So the SECOND patient
// created without an NSS collided (E11000 dup key { nss: null }) and the wizard
// showed "Une erreur est survenue lors de la création du dossier." (HTTP 500).
//
// Fixed by a per-cabinet PARTIAL unique index (unique on cabinetId+nss only when
// nss is a string). This spec creates TWO patients with no NSS and asserts both
// reach the success redirect — the second create is the one that used to fail.
//
// NOTE: unlike the validation specs, this one is MUTATING — there is no delete
// endpoint yet, so it leaves two patient records per run. Names are letters-only
// (the form rejects digits) and unique per run, so re-runs never collide.

// Map a timestamp to a letters-only string so names pass the "lettres uniquement"
// validator while staying unique across runs.
function uniqueLetters(): string {
	return String(Date.now())
		.split('')
		.map((d) => 'abcdefghij'[Number(d)])
		.join('');
}

// Walk the 4-step wizard with a valid identity + phone and NO NSS, then submit.
async function createPatientWithoutNss(page: Page, firstName: string, lastName: string) {
	await page.goto('/patients/new');
	await expect(page.getByRole('heading', { name: 'Nouveau patient' })).toBeVisible();

	// Step 1 — identity
	await page.getByLabel('PRÉNOM *').fill(firstName);
	await page.getByLabel('NOM *', { exact: true }).fill(lastName);
	await page.getByLabel('DATE DE NAISSANCE *').fill('1990-05-12');
	await page.getByRole('button', { name: 'Continuer' }).click();
	await expect(page.getByText('Étape 2 sur 4')).toBeVisible();

	// Step 2 — coordonnées (phone required; everything else left blank)
	await page.getByLabel('TÉLÉPHONE *').fill('0555123456');
	await page.getByRole('button', { name: 'Continuer' }).click();
	await expect(page.getByText('Étape 3 sur 4')).toBeVisible();

	// Step 3 — médical (nothing required)
	await page.getByRole('button', { name: 'Continuer' }).click();
	await expect(page.getByText('Étape 4 sur 4')).toBeVisible();

	// Step 4 — assurance: deliberately leave the NSS field EMPTY, then submit.
	await page.getByRole('button', { name: 'Créer le dossier' }).click();
}

test.describe('New patient — NSS-null duplicate-key regression', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
	});

	test('two patients created without an NSS both succeed (no dup-key 500)', async ({ page }) => {
		const uid = uniqueLetters();

		// First NSS-less patient — fine even before the fix.
		await createPatientWithoutNss(page, 'Test', `Nssone${uid}`);
		await expect(page).toHaveURL(/\/patients\?created=/);
		await expect(
			page.getByText('Une erreur est survenue lors de la création du dossier.')
		).toHaveCount(0);

		// Second NSS-less patient — this is the one that used to 500 on { nss: null }.
		await createPatientWithoutNss(page, 'Test', `Nsstwo${uid}`);
		await expect(page).toHaveURL(/\/patients\?created=/);
		await expect(
			page.getByText('Une erreur est survenue lors de la création du dossier.')
		).toHaveCount(0);
	});
});
