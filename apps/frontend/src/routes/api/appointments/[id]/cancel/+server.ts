import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';

export const PATCH: RequestHandler = async ({ params, request, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const body = await request.json().catch(() => ({}));

	try {
		const result = await api.patch(`/api/appointments/${params.id}/cancel`, body, token);
		return json(result ?? {});
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
