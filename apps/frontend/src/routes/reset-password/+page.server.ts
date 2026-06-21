import { fail, isHttpError, isRedirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url }) => ({
	email: url.searchParams.get('email') ?? '',
	token: url.searchParams.get('token') ?? ''
});

export const actions: Actions = {
	default: async ({ request }) => {
		const data = await request.formData();
		const email = data.get('email')?.toString() ?? '';
		const token = data.get('token')?.toString() ?? '';
		const newPassword = data.get('newPassword')?.toString() ?? '';
		const confirmPassword = data.get('confirmPassword')?.toString() ?? '';

		if (!token || !email) return fail(400, { error: 'Lien invalide. Veuillez refaire une demande.' });
		if (newPassword.length < 8)
			return fail(400, { error: 'Le mot de passe doit contenir au moins 8 caractères.' });
		if (newPassword !== confirmPassword)
			return fail(400, { error: 'Les mots de passe ne correspondent pas.' });

		try {
			const res = await api.post<{ message: string }>('/api/auth/reset-password', {
				email,
				token,
				newPassword
			});
			return { success: true, message: res.message };
		} catch (err) {
			if (isRedirect(err)) throw err;
			const status = isHttpError(err) ? err.status : 500;
			// 400 from the backend = invalid/expired token. Show a single generic message.
			if (status === 400)
				return fail(400, { error: 'Lien invalide ou expiré. Veuillez refaire une demande.' });
			console.error('[reset-password] backend error', { status });
			return fail(503, { error: 'Service momentanément indisponible. Réessayez dans un instant.' });
		}
	}
};
