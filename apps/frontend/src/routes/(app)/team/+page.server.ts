import { fail, redirect } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken, getUser } from '$lib/server/session';
import { PERMISSIONS, can } from '$lib/permissions';
import type { Actions, PageServerLoad } from './$types';

interface CabinetUser {
	id: string;
	email: string;
	firstName: string;
	lastName: string;
	role: string;
	isActive: boolean;
	permissions: string[];
	lastLoginAt: string | null;
}

interface PermissionItem {
	key: string;
	label: string;
	description: string;
}
interface PermissionCategory {
	key: string;
	label: string;
	icon: string;
	permissions: PermissionItem[];
}

export const load: PageServerLoad = async ({ cookies }) => {
	const token = getToken(cookies)!;
	const user = getUser(cookies);

	// Admin-only screen — a Secretary without users_can_view never reaches it.
	if (!can(user?.permissions, PERMISSIONS.users.view)) redirect(302, '/dashboard');

	const canManage = can(user?.permissions, PERMISSIONS.users.managePermissions);
	const canAdd = can(user?.permissions, PERMISSIONS.users.add);

	const [users, categories] = await Promise.all([
		api.get<CabinetUser[]>('/api/users', token).catch(() => [] as CabinetUser[]),
		api.get<PermissionCategory[]>('/api/permissions/metadata', token).catch(() => [] as PermissionCategory[])
	]);

	return { users, categories, canManage, canAdd };
};

export const actions: Actions = {
	createStaff: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const firstName = data.get('firstName')?.toString().trim() ?? '';
		const lastName = data.get('lastName')?.toString().trim() ?? '';
		const email = data.get('email')?.toString().trim() ?? '';
		const password = data.get('password')?.toString() ?? '';
		const permissions = data.getAll('permissions').map((p) => p.toString());

		if (!firstName || !lastName || !email || !password)
			return fail(400, { action: 'create', error: 'Tous les champs sont requis.' });
		if (password.length < 8)
			return fail(400, { action: 'create', error: 'Le mot de passe doit contenir au minimum 8 caractères.' });

		try {
			await api.post('/api/users', { email, password, firstName, lastName, permissions }, token);
			return { action: 'create', success: `${firstName} ${lastName} a été ajouté(e) à l'équipe.` };
		} catch (e: unknown) {
			if (e && typeof e === 'object' && 'status' in e) {
				const httpErr = e as { status: number; body?: { message?: string } };
				if (httpErr.status === 400)
					return fail(400, { action: 'create', error: httpErr.body?.message ?? 'Cette adresse email est déjà utilisée.' });
				throw e;
			}
			return fail(500, { action: 'create', error: 'Erreur serveur. Veuillez réessayer.' });
		}
	},

	savePermissions: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const userId = data.get('userId')?.toString() ?? '';
		const permissions = data.getAll('permissions').map((p) => p.toString());

		try {
			await api.patch(`/api/users/${userId}/permissions`, { permissions }, token);
			return { action: 'permissions', success: 'Permissions mises à jour. Elles seront effectives à la prochaine connexion du membre.' };
		} catch (e: unknown) {
			if (e && typeof e === 'object' && 'status' in e) {
				const httpErr = e as { status: number; body?: { message?: string } };
				if (httpErr.status === 400)
					return fail(400, { action: 'permissions', error: httpErr.body?.message ?? 'Modification impossible.' });
				throw e;
			}
			return fail(500, { action: 'permissions', error: 'Erreur serveur. Veuillez réessayer.' });
		}
	},

	setActive: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const userId = data.get('userId')?.toString() ?? '';
		const isActive = data.get('isActive')?.toString() === 'true';

		try {
			await api.patch(`/api/users/${userId}/active`, { isActive }, token);
			return { action: 'active', success: isActive ? 'Compte réactivé.' : 'Compte désactivé.' };
		} catch (e: unknown) {
			if (e && typeof e === 'object' && 'status' in e) {
				const httpErr = e as { status: number; body?: { message?: string } };
				if (httpErr.status === 400)
					return fail(400, { action: 'active', error: httpErr.body?.message ?? 'Opération impossible.' });
				throw e;
			}
			return fail(500, { action: 'active', error: 'Erreur serveur. Veuillez réessayer.' });
		}
	}
};
