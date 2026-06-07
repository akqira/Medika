import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';
import type { PagedResult, PatientSummary } from '$lib/types/api';

export const GET: RequestHandler = async ({ url, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const term     = url.searchParams.get('term')     ?? '';
	const page     = url.searchParams.get('page')     ?? '1';
	const pageSize = url.searchParams.get('pageSize') ?? '20';

	try {
		const result = await api.get<PagedResult<PatientSummary>>(
			`/api/patients?term=${encodeURIComponent(term)}&page=${page}&pageSize=${pageSize}`,
			token
		);
		return json(result);
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
