import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';
import type { PatientDetail } from '$lib/types/api';

export const GET: RequestHandler = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	try {
		const result = await api.get<PatientDetail>(`/api/patients/${params.id}`, token);
		return json(result);
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};

export const PATCH: RequestHandler = async ({ params, cookies, request }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const body = await request.json();
	await api.patch(`/api/patients/${params.id}`, body, token);
	return new Response(null, { status: 204 });
};

export const DELETE: RequestHandler = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	// Backend returns 204; RemoteApi maps that to undefined. Auth/404/409 surface
	// as SvelteKit errors thrown by the proxy, so let them propagate.
	await api.del(`/api/patients/${params.id}`, token);
	return new Response(null, { status: 204 });
};
