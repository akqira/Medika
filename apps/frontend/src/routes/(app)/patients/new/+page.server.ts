import { fail, redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser } from '$lib/server/session';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	const user = getUser(cookies);
	return { doctorName: user?.fullName ?? 'Médecin' };
};

export const actions: Actions = {
	default: async ({ request, cookies }) => {
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

		let created: { patientId: string };
		try {
			created = await api.post<{ patientId: string }>('/api/patients', {
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
			console.error('[patients/new] create failed:', e);
			const message = e instanceof Error ? e.message : 'Une erreur est survenue lors de la création du dossier.';
			return fail(500, { error: message });
		}

		redirect(303, `/patients?created=${created.patientId}`);
	},
};
