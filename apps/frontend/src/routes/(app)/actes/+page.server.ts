import { fail } from '@sveltejs/kit';
import { api } from '$lib/server/api';
import { getToken } from '$lib/server/session';
import type { Act } from '$lib/types/api';
import type { PageServerLoad, Actions } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	const token = getToken(cookies)!;
	// The acts endpoint wraps rows in { items } and names the id `actId` — flatten it.
	type ActItem = Omit<Act, 'id'> & { actId: string };
	const acts = await api
		.get<{ items: ActItem[] }>('/api/acts', token)
		.then(({ items }) => items.map(({ actId, ...rest }): Act => ({ id: actId, ...rest })))
		.catch(() => [] as Act[]);
	return { acts };
};

export const actions: Actions = {
	add: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const data = await request.formData();
		const name = data.get('name')?.toString().trim() ?? '';
		const tariff = Number(data.get('tariff') ?? 0);

		if (!name) return fail(400, { error: "Le nom de l'acte est requis." });
		if (!Number.isFinite(tariff) || tariff < 0) return fail(400, { error: 'Le tarif doit être positif.' });

		try {
			await api.post('/api/acts', { name, tariff }, token);
			return { success: 'Acte ajouté.' };
		} catch (e: unknown) {
			return fail(500, { error: e instanceof Error ? e.message : 'Erreur serveur.' });
		}
	},

	delete: async ({ request, cookies }) => {
		const token = getToken(cookies)!;
		const id = (await request.formData()).get('id')?.toString() ?? '';
		if (!id) return fail(400, { error: 'Acte introuvable.' });

		try {
			await api.del(`/api/acts/${id}`, token);
			return { success: 'Acte supprimé.' };
		} catch (e: unknown) {
			return fail(500, { error: e instanceof Error ? e.message : 'Erreur serveur.' });
		}
	}
};
