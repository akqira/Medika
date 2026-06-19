import { fail } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { FinancialSummary, Charge } from '$lib/types/api';
import type { PageServerLoad, Actions } from './$types';

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

	// The charges endpoint wraps the rows in a result object
	// ({ year, month, totalAmount, items }) and names the id `chargeId` — map it
	// down to the flat Charge[] the page expects.
	type ChargeItem = Omit<Charge, 'id'> & { chargeId: string };
	type ChargesResult = { items: ChargeItem[] };

	const [summary, charges] = await Promise.all([
		api
			.get<FinancialSummary>(`/api/finance/summary?year=${year}&month=${month}`, token)
			.catch(() => empty),
		api
			.get<ChargesResult>(`/api/charges?year=${year}&month=${month}`, token)
			.then(({ items }) =>
				items.map(({ chargeId, ...rest }): Charge => ({ id: chargeId, ...rest }))
			)
			.catch(() => [] as Charge[]),
	]);

	return { summary, charges, year, month };
};

export const actions: Actions = {
	addCharge: async ({ request, cookies, url }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const now = new Date();
		const year = Number(url.searchParams.get('year') ?? now.getFullYear());
		const month = Number(url.searchParams.get('month') ?? now.getMonth() + 1);

		const category    = data.get('category')?.toString() ?? '';
		const description = data.get('description')?.toString() ?? '';
		const amount      = Number(data.get('amount') ?? 0);
		const date        = data.get('date')?.toString() ?? new Date().toISOString().split('T')[0];

		if (!category)    return fail(400, { error: 'La catégorie est requise.', tab: 'charge' });
		if (!description) return fail(400, { error: 'La description est requise.', tab: 'charge' });
		if (amount <= 0)  return fail(400, { error: 'Le montant doit être supérieur à 0.', tab: 'charge' });

		try {
			await api.post('/api/charges', { category, description, amount, date }, token);
			return { success: 'Charge ajoutée avec succès.', year, month };
		} catch (e: unknown) {
			const msg = e instanceof Error ? e.message : 'Erreur serveur.';
			return fail(500, { error: msg, tab: 'charge' });
		}
	},
};
