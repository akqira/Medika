import { error, fail, isRedirect, redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser } from '$lib/server/session';
import type { PatientDetail } from '$lib/types/api';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) redirect(302, '/login');

	const user = getUser(cookies);

	let patient: PatientDetail;
	try {
		patient = await api.get<PatientDetail>(`/api/patients/${params.id}`, token);
	} catch (e) {
		if (isRedirect(e)) throw e;
		// Unknown / malformed / cross-cabinet id all surface as a clean 404 (no leak).
		const status = (e as { status?: number })?.status;
		if (status === 404) error(404, 'Patient introuvable');
		console.error('[patients/edit] load failed:', e);
		error(500, 'Erreur lors du chargement du dossier patient');
	}

	return { patient, doctorName: user?.fullName ?? 'Médecin' };
};

export const actions: Actions = {
	default: async ({ request, params, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();

		const field = (name: string) => data.get(name)?.toString().trim() || undefined;

		const firstName = field('firstName');
		const lastName = field('lastName');
		const dateOfBirth = field('dateOfBirth');
		const gender = field('gender');
		const phone = field('phone');

		if (!firstName || !lastName || !dateOfBirth || !gender || !phone) {
			return fail(400, { error: 'Veuillez renseigner tous les champs obligatoires.' });
		}

		const allergies = field('allergies')
			?.split(',').map((a) => a.trim()).filter(Boolean);
		const antecedents = field('medicalHistory')
			?.split(',').map((a) => a.trim()).filter(Boolean);

		try {
			await api.patch(`/api/patients/${params.id}`, {
				firstName,
				lastName,
				dateOfBirth,
				gender,
				phone,
				email: field('email'),
				address: field('address'),
				nss: field('nss'),
				bloodGroup: field('bloodGroup'),
				allergies,
				medicalHistory: antecedents,
				wilaya: field('wilaya'),
				emergencyContactName: field('emergencyContactName'),
				emergencyContactPhone: field('emergencyContactPhone'),
				insuranceProvider: field('insuranceProvider'),
				mutualInsurance: field('mutualInsurance'),
				currentTreatment: field('currentTreatment'),
			}, token);
		} catch (e) {
			console.error('[patients/edit] update failed:', e);
			const message = e instanceof Error ? e.message : 'Une erreur est survenue lors de la mise à jour du dossier.';
			return fail(500, { error: message });
		}

		redirect(303, `/patients/${params.id}?toast=patient-updated`);
	},
};
