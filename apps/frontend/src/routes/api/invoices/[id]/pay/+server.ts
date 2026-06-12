import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';

export const PATCH: RequestHandler = async ({ params, request, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const body = await request.json().catch(() => ({}));
	const paymentMethod = body?.paymentMethod ?? 'Cash';

	try {
		await api.patch(`/api/invoices/${params.id}/pay`, { paymentMethod }, token);
		return json({ ok: true });
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
