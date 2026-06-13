import { redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser } from '$lib/server/session';
import type { PagedResult, PatientSummary } from '$lib/types/api';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ cookies }) => {
	const token = getToken(cookies);
	if (!token) redirect(302, '/login');
	const user = getUser(cookies) ?? { fullName: 'Dr. Dupont', role: 'Doctor' };

	// Lightweight count for the navbar badge (pageSize=1 → only totalCount matters)
	const totalPatients = await api
		.get<PagedResult<PatientSummary>>('/api/patients?page=1&pageSize=1', token)
		.then((r) => r.totalCount)
		.catch(() => 0);

	return { user, totalPatients };
};
