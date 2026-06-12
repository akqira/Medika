import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { AppointmentSlot } from '$lib/types/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, cookies }) => {
	const token = getToken(cookies)!;
	const today = new Date().toISOString().split('T')[0];
	const date = url.searchParams.get('date') ?? today;

	const appointments = await api
		.get<AppointmentSlot[]>(`/api/schedule/${date}`, token)
		.catch(() => [] as AppointmentSlot[]);

	return { appointments, date };
};
