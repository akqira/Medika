import { error, fail, isRedirect, redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import { loadPatientFile, PatientNotFoundError } from '$lib/server/patientFile';
import type { PageServerLoad, Actions } from './$types';

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

export const actions: Actions = {
	updatePatient: async ({ params, cookies, request }) => {
		const token = getToken(cookies);
		if (!token) redirect(302, '/login');

		const data = await request.formData();

		const firstName = (data.get('firstName') as string)?.trim();
		const lastName = (data.get('lastName') as string)?.trim();
		const dateOfBirth = data.get('dateOfBirth') as string;
		const gender = data.get('gender') as string;
		const phone = (data.get('phone') as string)?.replace(/\s/g, '');
		const email = (data.get('email') as string)?.trim() || null;
		const address = (data.get('address') as string)?.trim() || null;
		const wilaya = (data.get('wilaya') as string)?.trim() || null;
		const nss = (data.get('nss') as string)?.trim() || null;
		const bloodGroup = (data.get('bloodGroup') as string)?.trim() || null;
		const emergencyContactName = (data.get('emergencyContactName') as string)?.trim() || null;
		const emergencyContactPhone = (data.get('emergencyContactPhone') as string)?.replace(/\s/g, '') || null;
		const insuranceProvider = (data.get('insuranceProvider') as string)?.trim() || null;
		const mutualInsurance = (data.get('mutualInsurance') as string)?.trim() || null;
		const currentTreatment = (data.get('currentTreatment') as string)?.trim() || null;
		const allergies = (data.getAll('allergies') as string[]).filter(Boolean);
		const medicalHistory = (data.getAll('medicalHistory') as string[]).filter(Boolean);

		const PHONE_RE = /^0[5-7]\d{8}$/;
		const errors: Record<string, string> = {};

		if (!firstName) errors.firstName = 'Champ requis';
		if (!lastName) errors.lastName = 'Champ requis';
		if (!dateOfBirth) errors.dateOfBirth = 'Champ requis';
		if (!gender) errors.gender = 'Champ requis';
		if (!phone) {
			errors.phone = 'Champ requis';
		} else if (!PHONE_RE.test(phone)) {
			errors.phone = 'Format invalide — ex: 0555 12 34 56';
		}
		if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
			errors.email = 'Adresse email invalide';
		}
		if (nss && !/^\d{15}$/.test(nss)) {
			errors.nss = 'Le NSS doit contenir exactement 15 chiffres';
		}

		if (Object.keys(errors).length > 0) {
			return fail(422, {
				errors,
				values: {
					firstName, lastName, dateOfBirth, gender, phone,
					email, address, wilaya, nss, bloodGroup,
					emergencyContactName, emergencyContactPhone,
					insuranceProvider, mutualInsurance, currentTreatment,
					allergies, medicalHistory
				}
			});
		}

		try {
			await api.patch(`/api/patients/${params.id}`, {
				firstName, lastName, dateOfBirth, gender, phone,
				email, address, wilaya, nss, bloodGroup,
				emergencyContactName, emergencyContactPhone,
				insuranceProvider, mutualInsurance, currentTreatment,
				allergies, medicalHistory
			}, token);
		} catch (e) {
			if (isRedirect(e)) throw e;
			console.error('[patient-edit] update failed:', e);
			return fail(500, { error: 'Erreur lors de la mise à jour du dossier patient.' });
		}

		redirect(303, `/patients/${params.id}?toast=patient-updated`);
	}
};
