import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ request, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	const body = await request.json();

	try {
		const result = await api.post<{ patientId: string }>('/api/patients', body, token);

		const dob = new Date(body.dateOfBirth);
		const age = Math.max(0, Math.floor((Date.now() - dob.getTime()) / (1000 * 60 * 60 * 24 * 365.25)));

		return json({
			id: result.patientId,
			firstName: body.firstName,
			lastName: body.lastName,
			age,
			gender: body.gender,
			phone: body.phone,
			bloodGroup: body.bloodGroup ?? null,
			lastVisitAt: null,
			allergyCount: 0,
		});
	} catch (e: unknown) {
		console.error('[api/patients] POST failed:', e);
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
