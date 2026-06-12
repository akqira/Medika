import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ request, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const body = await request.json();

	try {
		const result = await api.post('/api/appointments', body, token);
		return json(result, { status: 201 });
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
