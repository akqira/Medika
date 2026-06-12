import { fail, redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { PagedResult, PatientSummary } from '$lib/types/api';
import type { PageServerLoad, Actions } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	const token = getToken(cookies)!;
	const patients = await api
		.get<PagedResult<PatientSummary>>('/api/patients?page=1&pageSize=100', token)
		.catch((): PagedResult<PatientSummary> => ({
			items: [], totalCount: 0, page: 1, pageSize: 100,
			totalPages: 0, hasNextPage: false, hasPreviousPage: false
		}));
	return { patients: patients.items };
};

export const actions: Actions = {
	default: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();

		const patientId     = data.get('patientId')?.toString() ?? '';
		const appointmentId = data.get('appointmentId')?.toString() || undefined;
		const reason        = data.get('reason')?.toString() ?? '';
		const clinicalExam  = data.get('clinicalExam')?.toString() ?? '';
		const diagnosis     = data.get('diagnosis')?.toString() ?? '';
		const notes         = data.get('notes')?.toString() ?? '';
		const tariff        = Number(data.get('tariff') ?? 0);
		const finalize      = data.get('finalize') === 'true';

		const prescriptionRaw = data.get('prescription')?.toString() ?? '[]';
		let prescription: unknown[] = [];
		try {
			prescription = JSON.parse(prescriptionRaw);
		} catch {
			return fail(400, { error: 'Données d\'ordonnance invalides.' });
		}

		const vitalSigns = {
			bloodPressure: data.get('bloodPressure')?.toString() || undefined,
			pulseRate:     data.get('pulseRate')?.toString()     || undefined,
			temperature:   data.get('temperature')?.toString()   || undefined,
			weight:        data.get('weight')?.toString()        || undefined,
			height:        data.get('height')?.toString()        || undefined,
			spO2:          data.get('spO2')?.toString()          || undefined,
		};

		if (!patientId) {
			return fail(400, { error: 'Veuillez sélectionner un patient.' });
		}

		let result: { consultationId: string };
		try {
			result = await api.post<{ consultationId: string }>('/api/consultations', {
				patientId,
				appointmentId,
				reason,
				clinicalExam,
				diagnosis,
				notes,
				vitalSigns,
				prescription,
				tariff,
				finalize,
			}, token);
		} catch (e: unknown) {
			const msg = e instanceof Error ? e.message : 'Erreur serveur.';
			return fail(500, { error: msg });
		}

		if (finalize) {
			redirect(303, `/patients?created=${result.consultationId}`);
		}

		return { success: 'Consultation enregistrée en brouillon.' };
	},
};
