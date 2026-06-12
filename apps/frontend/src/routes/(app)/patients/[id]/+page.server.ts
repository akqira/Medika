import { error, isRedirect, redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import { loadPatientFile, PatientNotFoundError } from '$lib/server/patientFile';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) redirect(302, '/login');

	try {
		return await loadPatientFile(api, token, params.id);
	} catch (e) {
		// Auth redirects from the proxy (e.g. 401 → re-login) must pass through untouched.
		if (isRedirect(e)) throw e;
		// Unknown id, malformed id, or another cabinet's patient — identical clean 404, no leak (AC-7/AC-10).
		if (e instanceof PatientNotFoundError) error(404, 'Patient introuvable');
		// Any other failure (incl. a raw backend 500) — friendly, recoverable page error (AC-8).
		// We deliberately do NOT re-throw the backend's HttpError: its body would leak raw
		// technical JSON to the doctor.
		console.error('[patient-file] load failed:', e);
		error(500, 'Erreur lors du chargement du dossier patient');
	}
};
