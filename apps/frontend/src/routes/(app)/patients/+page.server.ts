import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { PagedResult, PatientSummary } from '$lib/types/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, cookies }) => {
	const token = getToken(cookies)!;
	const term = url.searchParams.get('term') ?? '';
	const page = Math.max(1, Number(url.searchParams.get('page') ?? '1'));

	const empty: PagedResult<PatientSummary> = {
		items: [], totalCount: 0, page, pageSize: 20,
		totalPages: 0, hasNextPage: false, hasPreviousPage: false
	};

	const [result, allResult] = await Promise.all([
		api.get<PagedResult<PatientSummary>>(
			`/api/patients?term=${encodeURIComponent(term)}&page=${page}&pageSize=20`,
			token
		).catch((e) => { console.error('[patients] load failed:', e); return empty; }),
		term
			? api.get<PagedResult<PatientSummary>>(`/api/patients?term=&page=1&pageSize=1`, token)
				.catch(() => empty)
			: Promise.resolve(null)
	]);

	// cabinetTotal: total patients in the cabinet regardless of active filter
	const cabinetTotal = allResult ? allResult.totalCount : result.totalCount;

	return { result, term, page, cabinetTotal };
};
