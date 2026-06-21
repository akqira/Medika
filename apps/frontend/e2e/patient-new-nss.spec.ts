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
// Returns the new patient id parsed from the success redirect (/patients?created=<id>).
async function createPatientWithoutNss(page: Page, firstName: string, lastName: string): Promise<string> {
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

	await expect(page).toHaveURL(/\/patients\?created=/);
	await expect(
		page.getByText('Une erreur est survenue lors de la création du dossier.')
	).toHaveCount(0);

	const created = new URL(page.url()).searchParams.get('created');
	expect(created, 'success redirect should carry the new patient id').toBeTruthy();
	return created!;
}

test.describe('New patient — NSS-null duplicate-key regression', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
	});

	test('two patients created without an NSS both succeed (no dup-key 500)', async ({ page }) => {
		const uid = uniqueLetters();
		const created: string[] = [];
		try {
			// First NSS-less patient — fine even before the fix.
			created.push(await createPatientWithoutNss(page, 'Test', `Nssone${uid}`));
			// Second NSS-less patient — this is the one that used to 500 on { nss: null }.
			created.push(await createPatientWithoutNss(page, 'Test', `Nsstwo${uid}`));
		} finally {
			// Self-clean so the suite stays re-runnable. The DELETE proxy also gives
			// the new delete-patient endpoint its happy-path e2e coverage (expect 204).
			for (const id of created) {
				const res = await page.request.delete(`/api/patients/${id}`);
				expect(res.status(), `cleanup delete of ${id}`).toBe(204);
			}
		}
	});
});
