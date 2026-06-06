import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { PagedResult, PatientSummary } from '$lib/types/api';
import type { PageServerLoad } from './$types';

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
