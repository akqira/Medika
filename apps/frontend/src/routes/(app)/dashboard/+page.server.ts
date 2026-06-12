import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { AppointmentSlot, PagedResult, PatientSummary, FinancialSummary } from '$lib/types/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	const token = getToken(cookies)!;
	const now = new Date();
	const today = now.toISOString().split('T')[0];

	const emptySummary: FinancialSummary = {
		totalIncome: 0, totalCharges: 0, netIncome: 0,
		paidInvoices: 0, pendingInvoices: 0, pendingAmount: 0,
		monthlyTrend: [], breakdownByType: []
	};

	const [appointments, patientsResult, summary] = await Promise.all([
		api.get<AppointmentSlot[]>(`/api/schedule/${today}`, token)
			.catch(() => [] as AppointmentSlot[]),
		api.get<PagedResult<PatientSummary>>('/api/patients?page=1&pageSize=6', token)
			.catch((): PagedResult<PatientSummary> => ({
				items: [],