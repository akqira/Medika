import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';
import type { PagedResult, ConsultationSummary } from '$lib/types/api';

export const GET: RequestHandler = async ({ params, url, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const page     = url.searchParams.get('page')     ?? '1';
	const pageSize = url.searchParams.get('pageSize') ?? '10';

	try {
		const result = await api.get<PagedResult<ConsultationSummary>>(
			`/api/patients/${params.id}/consultations?page=${page}&pageSize=${pageSize}`,
			token
		);
		return json(result);
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
