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

	const profile = await api
		.get<DoctorProfile>('/api/profile', token)
		.catch(() => fallback);

	return { profile };
};

export const actions: Actions = {
	saveCabinet: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		try {
			await api.patch('/api/profile/cabinet', {
				cabinetName:    data.get('cabinetName'),
				cabinetAddress: data.get('cabinetAddress'),
				cabinetCity:    data.get('cabinetCity'),
				cabinetWilaya:  data.get('cabinetWilaya'),
				cabinetPhone:   data.get('cabinetPhone'),
				specialty:      data.get('specialty'),
				rppsNumber:     data.get('rppsNumber'),
			}, token);
			return { tab: 'cabinet', success: 'Informations du cabinet mises à jour.' };
		} catch {
			return fail(500, { tab: 'cabinet', error: 'Erreur lors de la sauvegarde.' });
		}
	},

	saveAccount: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const firstName = data.get('firstName')?.toString() ?? '';
		const lastName  = data.get('lastName')?.toString() ?? '';
		try {
			await api.patch('/api/profile/account', { firstName, lastName, email: data.get('email') }, token);
			const user = getUser(cookies);
			if (user) setUser(cookies, { ...user, fullName: `${firstName} ${lastName}`.trim() });
			return { tab: 'compte', success: 'Informations du compte mises à jour.' };
		} catch {
			return fail(500, { tab: 'compte', error: 'Erreur lors de la sauvegarde.' });
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
			return fail(400, { tab: 'securite', error: 'Minimum 8 caractères requis.' });
		try {
			await api.post('/api/profile/change-password', { currentPassword: data.get('currentPassword'), newPassword: newPwd }, token);
			return { tab: 'securite', success: 'Mot de passe modifié avec succès.' };
		} catch {
			return fail(400, { tab: 'securite', error: 'Mot de passe actuel incorrect.' });
		}
	},
};
