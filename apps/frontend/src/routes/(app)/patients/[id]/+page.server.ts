import { error, redirect } from '@sveltejs/kit';
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
		// Unknown id or another cabinet's patient — identical clean 404, no leak (AC-7/AC-10).
		if (e instanceof PatientNotFoundError) error(404, 'Patient introuvable');
		// SvelteKit redirects/HttpErrors thrown by the proxy (e.g. 401 → re-login) pass through.
		if (e && typeof e === 'object' && 'status' in e) throw e;
		// Anything else — recoverable page error (AC-8).
		console.error('[patient-file] load failed:', e);
		error(500, 'Erreur lors du chargement du dossier patient');
	}
};
