import { RemoteApi } from './remoteApiProxy';

/**
 * Thin facade over RemoteApi (see remoteApiProxy.ts) — keeps the familiar
 * api.get / api.post / api.patch / api.del call-sites while ALL outgoing
 * requests are centralized in RemoteApi, which injects:
 *   X-API-KEY + X-Request-Timestamp + Authorization (when token given).
 *
 * Server-only module.
 */
export const api = {
	get: <T>(path: string, token?: string) =>
		RemoteApi.fetchThenRetrieveJson<T>({ url: path, options: { method: 'GET' }, token }),

	post: <T>(path: string, body: unknown, token?: string, clientIp?: string) =>
		RemoteApi.fetchThenRetrieveJson<T>({
			url: path,
			options: { method: 'POST', body: JSON.stringify(body) },
			token,
			clientIp
		}),

	patch: <T>(path: string, body: unknown, token?: string) =>
		RemoteApi.fetchThenRetrieveJson<T>({
			url: path,
			options: { method: 'PATCH', body: JSON.stringify(body) },
			token
		}),

	del: <T = void>(path: string, token?: string) =>
		RemoteApi.fetchThenRetrieveJson<T>({ url: path, options: { method: 'DELETE' }, token })
};
