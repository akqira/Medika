import { test, expect } from '@playwright/test';
import { login } from '../helpers';

// JOURNEY-02 — New patient, consultation, ordonnance & honoraire.
//
// Verbatim scenario (see docs/e2e-scenarios.md):
//   As a doctor, I want to register a brand-new patient and, in the same visit,
//   open a consultation, note the motif and diagnostic, prescribe their médicaments
//   and bill the honoraire, so that a first-time patient's visit is fully captured —
//   dossier, clinical record, prescription and billing — in one flow.
//
//   1. Log in (seeded doctor).
//   2. From the patient list, add a new patient: Kaki Kebir, ~44 ans (né le
//      01/01/1982), groupe sanguin A+, tél 0555 45 45 45, adresse Cité des
//      enseignants, Oran.
//   3. Open a consultation for that newly-created patient.
//   4. Record the motif/symptômes (fièvre, vomissements, diarrhées) and the
//      diagnostic (Intoxication alimentaire).
//   5. Prescribe two médicaments — Paracétamol 500mg (2 fois par jour) and Smecta
//      (1 fois le soir après repas).
//   6. Set the honoraire to 2 000 DA and save (the consultation is finalized — this
//      is the "encaissement"; the cockpit has no separate "payé sur place" field).
//
// Mutating, so it creates its OWN patient. Teardown is BEST-EFFORT: once the
// consultation is finalized the API (correctly) refuses to hard-delete a patient with
// clinical history (400), and there is no consultation-delete endpoint to undo it. CI
// runs on a fresh Mongo per PR so nothing accumulates; seed data is never touched. The
// consultation amount/ordonnance are asserted via the same API routes the app uses,
// not just by reading the DOM.

test('JOURNEY-02 — register a patient, consult, prescribe two meds, bill 2 000 DA', async ({
	page
}) => {
	// Symptoms/diagnostic/meds kept in constants so the form-fill and the API
	// assertions stay in sync.
	const MOTIF = 'fièvre, vomissements, diarrhées';
	const DIAGNOSTIC = 'Intoxication alimentaire';
	const MED_1 = { name: 'Paracétamol 500mg', poso: '2 fois par jour' };
	const MED_2 = { name: 'Smecta', poso: '1 fois le soir après repas' };
	const HONORAIRE = 2000;

	// Full happy-path flow (cold-start login + 4-step wizard + consultation cockpit +
	// API assertions + cleanup) comfortably exceeds Playwright's 30s default.
	test.setTimeout(120_000);

	let patientId = '';

	try {
		// ── 1. Login ─────────────────────────────────────────────────────────────
		await login(page);

		// ── 2. Patient list → add a new patient ─────────────────────────────────
		await page.goto('/patients');
		await page.getByRole('link', { name: 'Nouveau patient' }).click();
		await expect(page.getByRole('heading', { name: 'Nouveau patient' })).toBeVisible();

		// Step 1 — Identité
		await page.getByLabel('PRÉNOM *').fill('Kaki');
		await page.getByLabel('NOM *', { exact: true }).fill('Kebir');
		await page.getByLabel('DATE DE NAISSANCE *').fill('1982-01-01'); // ≈ 44 ans (le form n'a pas de champ "âge")
		await page.locator('#bloodGroup').selectOption('A+');
		await expect(page.locator('#bloodGroup')).toHaveValue('A+');
		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Étape 2 sur 4')).toBeVisible();

		// Step 2 — Coordonnées
		await page.locator('#phone').fill('0555 45 45 45');
		await page.locator('#address').fill('Cité des enseignants, Oran');
		await page.locator('#wilaya').selectOption({ label: 'Oran (31)' });
		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Étape 3 sur 4')).toBeVisible();

		// Step 3 — Médical (rien à saisir pour ce parcours)
		await page.getByRole('button', { name: 'Continuer' }).click();
		await expect(page.getByText('Étape 4 sur 4')).toBeVisible();

		// Step 4 — Assurance: NSS facultatif, on crée le dossier directement.
		await page.getByRole('button', { name: 'Créer le dossier' }).click();

		// Création OK → redirection vers /patients?created=<patientId>.
		await expect(page).toHaveURL(/\/patients\?created=/);
		patientId = new URL(page.url()).searchParams.get('created') ?? '';
		expect(patientId, 'patientId captured from the create redirect').toBeTruthy();

		// ── 3. Open a consultation for that patient ─────────────────────────────
		// Arrive with ?patientId= so the cockpit preselects the patient on init (the
		// route reads it from the URL). Driving the <select> client-side instead races
		// hydration, which re-initialises selectedPatientId from the URL ('') and snaps
		// the dropdown back to its placeholder. A toPass loop also absorbs any brief
		// list read-lag right after creation by re-navigating.
		await expect(async () => {
			await page.goto(`/consultation?patientId=${patientId}`);
			// The selected patient's dossier renders in the left rail (heading, not
			// getByText — the latter would also match the patient <select> options).
			await expect(page.getByRole('heading', { name: 'Kaki Kebir' })).toBeVisible({
				timeout: 5000
			});
		}).toPass({ timeout: 30000 });

		// ── 4. Motif/symptômes + diagnostic ─────────────────────────────────────
		await page.getByPlaceholder('Saisissez le motif…').fill(MOTIF);

		await page.getByRole('tab', { name: 'Diagnostic' }).click();
		const diagnosis = page.getByPlaceholder('Diagnostic retenu…');
		await expect(diagnosis).toBeVisible();
		await diagnosis.fill(DIAGNOSTIC);

		// ── 5. Ordonnance — deux médicaments (fenêtre dédiée, nouveau design) ────
		// L'ordonnance vit désormais dans une fenêtre plein écran pilotée par la
		// recherche : on ouvre la fenêtre, on ajoute deux lignes depuis le catalogue,
		// puis on précise nom + posologie dans le panneau éditable de droite.
		await page.getByRole('button', { name: 'Créer une ordonnance', exact: true }).click();
		await expect(page.getByRole('dialog', { name: 'Nouvelle ordonnance' })).toBeVisible();

		// Deux clics sur le catalogue créent deux lignes éditables.
		await page.getByRole('button', { name: /PARACÉTAMOL 500mg/ }).first().click();
		await page.getByRole('button', { name: /OMÉPRAZOLE/ }).first().click();

		const medNames = page.getByRole('textbox', { name: 'Nom + dosage' });
		const medPosos = page.getByPlaceholder('Posologie (ex. 1-0-1-0 ou 1 comprimé matin et soir)');
		await expect(medNames).toHaveCount(2);
		for (const [i, med] of [MED_1, MED_2].entries()) {
			await medNames.nth(i).fill(med.name);
			await medPosos.nth(i).fill(med.poso);
		}

		// Retour à la consultation — les lignes restent en mémoire.
		await page.getByRole('button', { name: 'Retour' }).click();
		await expect(page.getByText('Ordonnance prête')).toBeVisible();

		// ── 6. Honoraire 2 000 DA + enregistrement (finalize) ───────────────────
		await page.locator('#tariff').fill(String(HONORAIRE));
		await page.getByRole('button', { name: /Enregistrer & encaisser/ }).click();

		// Enregistrement OK → redirection vers /patients?created=<consultationId>.
		await expect(page).toHaveURL(/\/patients\?created=/);
		const consultationId = new URL(page.url()).searchParams.get('created') ?? '';
		expect(consultationId, 'consultationId captured from the save redirect').toBeTruthy();

		// ── Assert the persisted outcome via the API (intent, not just the DOM) ──
		const res = await page.request.get(
			`/api/patients/${patientId}/consultations/${consultationId}`
		);
		expect(res.ok(), `GET consultation ${consultationId} → ${res.status()}`).toBeTruthy();
		const detail = await res.json();

		expect(detail.tariff, 'honoraire encaissé').toBe(HONORAIRE);
		expect(detail.isFinalized, 'consultation finalisée').toBe(true);
		expect(detail.reason).toContain('fièvre');
		expect(detail.reason).toContain('vomissements');
		expect(detail.diagnosis).toBe(DIAGNOSTIC);

		const meds: { medication: string }[] = detail.prescription ?? [];
		expect(meds, 'deux lignes d’ordonnance').toHaveLength(2);
		const names = meds.map((m) => m.medication).join(' | ');
		expect(names).toContain('Paracétamol');
		expect(names).toContain('Smecta');
	} finally {
		// Best-effort teardown. A finalized consultation makes the patient
		// un-deletable by design (400 "a des consultations…"), and no endpoint can
		// delete the consultation first — so 400 here is expected, not a failure. Any
		// other non-2xx status is a real cleanup regression and must surface.
		if (patientId) {
			const del = await page.request.delete(`/api/patients/${patientId}`);
			expect(
				[200, 204, 400],
				`cleanup delete of patient ${patientId} → ${del.status()}`
			).toContain(del.status());
		}
	}
});
