import { json, error } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';
import type { ConsultationDetail } from '$lib/types/api';

export const GET: RequestHandler = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	try {
		const result = await api.get<ConsultationDetail>(
			`/api/patients/${params.id}/consultations/${params.consultationId}`,
			token
		);
		return json(result);
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : 'Erreur serveur';
		return json({ error: msg }, { status: 500 });
	}
};
