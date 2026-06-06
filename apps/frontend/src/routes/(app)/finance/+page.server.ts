import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { FinancialSummary } from '$lib/types/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, cookies }) => {
	const token = getToken(cookies)!;
	const now = new Date();
	const year = Number(url.searchParams.get('year') ?? now.getFullYear());
	const month = Number(url.searchParams.get('month') ?? now.getMonth() + 1);

	const empty: FinancialSummary = {
		totalIncome: 0, totalCharges: 0, netIncome: 0,
		paidInvoices: 0, pendingInvoices: 0, pendingAmount: 0,
		monthlyTrend: [], breakdownByType: []
	};

	const summary = await api
		.get<FinancialSummary>(`/api/finance/summary?year=${year}&month=${month}`, token)
		.catch(() => empty);

	return { summary, year, month };
};
