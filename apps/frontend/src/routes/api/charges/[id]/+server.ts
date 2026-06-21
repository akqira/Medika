import { error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';

export const DELETE: RequestHandler = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	// Backend returns 204; auth/404 surface as SvelteKit errors thrown by the proxy.
	await api.del(`/api/charges/${params.id}`, token);
	return new Response(null, { status: 204 });
};
