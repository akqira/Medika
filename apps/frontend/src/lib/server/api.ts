import { env } from '$env/dynamic/private';
import { error } from '@sveltejs/kit';

const BASE = env.API_URL ?? 'http://localhost:5000';

async function request<T>(
	path: string,
	options: RequestInit = {},
	token?: string
): Promise<T> {
	const headers: Record<string, string> = {
		...(options.body !== undefined ? { 'Content-Type': 'application/json' } : {}),
		...(token ? { Authorization: `Bearer ${token}` } : {}),
		...(options.headers as Record<string, string>),
	};

	const res = await fetch(`${BASE}${path}`, { ...options, headers });

	if (res.status === 401) error(401, 'Unauthorized');
	if (res.status === 403) error(403, 'Forbidden');
	if (res.status === 404) error(404, 'Not found');

	if (!res.ok) {
		const body = await res.text().catch(() => '');
		error(res.status, body || `API error ${res.status}`);
	}

	if (res.status === 204) return undefined as T;
	return res.json() as Promise<T>;
}

export const api = {
	get: <T>(path: string, token?: string) =>
		request<T>(path, { method: 'GET' }, token),

	post: <T>(path: string, body: unknown, token?: string) =>
		request<T>(path, { method: 'POST', body: JSON.stringify(body) }, token),

	patch: <T>(path: string, body: unknown, token?: string) =>
		request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }, token),

	del: <T = void>(path: string, token?: string) =>
		request<T>(path, { method: 'DELETE' }, token),
};
