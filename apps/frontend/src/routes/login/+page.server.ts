import { fail, redirect, isHttpError, isRedirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { setToken, setUser, getToken } from '$lib/server/session';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	if (getToken(cookies)) redirect(302, '/dashboard');
};

export const actions: Actions = {
	default: async ({ request, cookies }) => {
		const data = await request.formData();
		const email = data.get('email')?.toString() ?? '';
		const password = data.get('password')?.toString() ?? '';

		if (!email || !password) {
			return fail(400, { error: 'Email et mot de passe requis.' });
		}

		try {
			const result = await api.post<{
				token: string;
				userId: string;
				role: string;
				fullName: string;
			}>('/api/auth/login', { email, password });

			setToken(cookies, result.token);
			setUser(cookies, { fullName: result.fullName, role: result.role });
		} catch (err) {
			if (isRedirect(err)) throw err; // never swallow a redirect

			const status = isHttpError(err) ? err.status : 500;

			// Only a genuine 401 means wrong email/password. Anything else
			// (503 backend unreachable, 500, API-key/config error) is a server-side
			// problem — log it and say so, instead of blaming the user's credentials.
			if (status === 401) {
				return fail(401, { error: 'Email ou mot de passe incorrect.' });
			}

			console.error('[login] backend error', {
				status,
				message: isHttpError(err) ? err.body?.message : String(err)
			});
			return fail(503, {
				error: 'Service momentanément indisponible. Réessayez dans un instant.'
			});
		}

		redirect(302, '/dashboard');
	}
};
