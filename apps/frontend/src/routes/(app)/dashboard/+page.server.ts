import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { AppointmentSlot, PagedResult, PatientSummary } from '$lib/types/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	const token = getToken(cookies)!;
	const today = new Date().toISOString().split('T')[0];

	const [appointments, patientsResult] = await Promise.all([
		api.get<AppointmentSlot[]>(`/api/schedule/${today}`, token)
			.catch(() => [] as AppointmentSlot[]),
		api.get<PagedResult<PatientSummary>>('/api/patients?page=1&pageSize=6', token)
			.catch((): PagedResult<PatientSummary> => ({
				items: [], totalCount: 0, page: 1, pageSize: 6,
				totalPages: 0, hasNextPage: false, hasPreviousPage: false
			}))
	]);

	return {
		appointments,
		recentPatients: patientsResult.items,
		totalPatients: patientsResult.totalCount,
		today
	};
};
