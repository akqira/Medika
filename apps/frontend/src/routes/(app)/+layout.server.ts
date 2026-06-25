import { redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser } from '$lib/server/session';
import type { PagedResult, PatientSummary } from '$lib/types/api';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ cookies }) => {
	const token = getToken(cookies);
	if (!token) redirect(302, '/login');
	const user = getUser(cookies) ?? { fullName: 'Dr. Dupont', role: 'Doctor', permissions: [] };

	// Lightweight count for the navbar badge (pageSize=1 → only totalCount matters).
	// Skipped when the user can't view patients (avoids a guaranteed 403).
	const canViewPatients = user.permissions.includes('patients_can_view');
	const totalPatients = canViewPatients
		? await api
				.get<PagedResult<PatientSummary>>('/api/patients?page=1&pageSize=1', token)
				.then((r) => r.totalCount)
				.catch(() => 0)
		: 0;

	return { user, totalPatients };
};
