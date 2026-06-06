import { fail, redirect } from '@sveltejs/kit';
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
		} catch {
			return fail(401, { error: 'Email ou mot de passe incorrect.' });
		}

		redirect(302, '/dashboard');
	}
};
