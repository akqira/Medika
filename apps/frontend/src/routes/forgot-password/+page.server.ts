import { fail, isHttpError, isRedirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import type { Actions } from './$types';

export const actions: Actions = {
	default: async ({ request }) => {
		const data = await request.formData();
		const email = data.get('email')?.toString().trim() ?? '';

		if (!email) return fail(400, { error: 'Adresse e-mail requise.', email });

		try {
			const res = await api.post<{ message: string }>('/api/auth/forgot-password', { email });
			// The backend always returns the same generic message (no account enumeration).
			return { success: true, message: res.message };
		} catch (err) {
			if (isRedirect(err)) throw err;
			const status = isHttpError(err) ? err.status : 500;
			if (status === 400) return fail(400, { error: 'Adresse e-mail invalide.', email });
			console.error('[forgot-password] backend error', { status });
			return fail(503, {
				error: 'Service momentanément indisponible. Réessayez dans un instant.',
				email
			});
		}
	}
};
