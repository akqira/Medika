import { error } from '@sveltejs/kit';
import { RemoteApi } from '$lib/server/remoteApiProxy';
import { getToken } from '$lib/server/session';
import type { RequestHandler } from './$types';

/**
 * Streams the server-generated ordonnance PDF for a consultation.
 * Binary passthrough via RemoteApi.fetchThenRetrieveStream — the .NET API sets
 * the Content-Disposition filename, which we forward to the browser.
 */
export const GET: RequestHandler = async ({ params, cookies }) => {
	const token = getToken(cookies);
	if (!token) error(401, 'Non authentifié');

	return RemoteApi.fetchThenRetrieveStream({
		url: `/api/patients/${params.id}/consultations/${params.consultationId}/ordonnance`,
		token
	});
};
