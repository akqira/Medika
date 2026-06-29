import { fail } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser, setUser } from '$lib/server/session';
import type { Actions, PageServerLoad } from './$types';

interface DoctorProfile {
	firstName: string;
	lastName: string;
	email: string;
	specialty: string;
	rppsNumber: string;
	cabinetName: string;
	cabinetAddress: string;
	cabinetCity: string;
	cabinetWilaya: string;
	cabinetPhone: string;
}

export const load: PageServerLoad = async ({ cookies }) => {
	const token = getToken(cookies)!;
	const user = getUser(cookies);

	const nameParts = user?.fullName?.split(' ') ?? [];
	const fallback: DoctorProfile = {
		firstName:      nameParts[0] ?? '',
		lastName:       nameParts.slice(1).join(' ') ?? '',
		email:          '',
		specialty:      '',
		rppsNumber:     '',
		cabinetName:    '',
		cabinetAddress: '',
		cabinetCity:    '',
		cabinetWilaya:  '',
		cabinetPhone:   '',
	};

	// Try to fetch the real profile; fall back gracefully if backend unavailable.
	// Do NOT swallow auth errors — propagate 401/403 so the layout redirect handles them.
	let profile = fallback;
	try {
		profile = await api.get<DoctorProfile>('/api/profile', token);
	} catch (e: unknown) {
		// Re-throw SvelteKit HTTP errors (401, 403, etc.) — don't swallow them.
		if (e && typeof e === 'object' && 'status' in e) {
			throw e;
		}
		// Network / 5xx: use fallback so the page still renders.
	}

	return { profile };
};

export const actions: Actions = {
	saveCabinet: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const cabinetName = data.get('cabinetName')?.toString().trim() ?? '';
		if (!cabinetName)
			return fail(400, { tab: 'cabinet', error: 'Le nom du cabinet est requis.' });
		try {
			await api.patch('/api/profile/cabinet', {
				cabinetName:    data.get('cabinetName'),
				cabinetAddress: data.get('cabinetAddress'),
				cabinetWilaya:  data.get('cabinetWilaya'),
				cabinetPhone:   data.get('cabinetPhone'),
				specialty:      data.get('specialty'),
				rppsNumber:     data.get('rppsNumber'),
			}, token);
			return { tab: 'cabinet', success: 'Informations du cabinet mises à jour.' };
		} catch (e: unknown) {
			// Propagate SvelteKit errors (401, 403); surface backend validation (400)
			if (e && typeof e === 'object' && 'status' in e) {
				const httpErr = e as { status: number; body?: { message?: string } };
				if (httpErr.status === 400)
					return fail(400, { tab: 'cabinet', error: httpErr.body?.message ?? 'Le nom du cabinet est requis.' });
				throw e;
			}
			return fail(500, { tab: 'cabinet', error: 'Erreur lors de la sauvegarde. Veuillez réessayer.' });
		}
	},

	saveAccount: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const firstName = data.get('firstName')?.toString() ?? '';
		const lastName  = data.get('lastName')?.toString() ?? '';
		const email     = data.get('email')?.toString() ?? '';

		try {
			await api.patch('/api/profile/account', { firstName, lastName, email }, token);
			const user = getUser(cookies);
			if (user) setUser(cookies, { ...user, fullName: `${firstName} ${lastName}`.trim() });
			return { tab: 'compte', success: 'Informations du compte mises à jour.' };
		} catch (e: unknown) {
			// Propagate SvelteKit errors (401, 403)
			if (e && typeof e === 'object' && 'status' in e) {
				const httpErr = e as { status: number; body?: { message?: string } };
				if (httpErr.status === 400) {
					// "Email already in use" or other validation from backend
					const msg = httpErr.body?.message ?? 'Cette adresse email est déjà utilisée.';
					return fail(400, { tab: 'compte', error: msg });
				}
				throw e;
			}
			return fail(500, { tab: 'compte', error: 'Erreur lors de la sauvegarde. Veuillez réessayer.' });
		}
	},

	changePassword: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const newPwd     = data.get('newPassword')?.toString() ?? '';
		const confirmPwd = data.get('confirmPassword')?.toString() ?? '';

		if (newPwd !== confirmPwd)
			return fail(400, { tab: 'securite', error: 'Les mots de passe ne correspondent pas.' });
		if (newPwd.length < 8)
			return fail(400, { tab: 'securite', error: 'Le nouveau mot de passe doit contenir au minimum 8 caractères.' });

		try {
			await api.post('/api/profile/change-password', {
				currentPassword: data.get('currentPassword'),
				newPassword:     newPwd,
			}, token);
			return { tab: 'securite', success: 'Mot de passe modifié avec succès.' };
		} catch (e: unknown) {
			// Propagate SvelteKit errors (401, 403)
			if (e && typeof e === 'object' && 'status' in e) {
				const httpErr = e as { status: number };
				if (httpErr.status === 422 || httpErr.status === 400) {
					return fail(400, { tab: 'securite', error: 'Mot de passe actuel incorrect.' });
				}
				throw e;
			}
			return fail(500, { tab: 'securite', error: 'Erreur serveur. Veuillez réessayer.' });
		}
	},
};
