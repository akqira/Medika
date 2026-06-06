import { redirect } from '@sveltejs/kit';
import { clearToken, clearUser } from '$lib/server/session';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies }) => {
	clearToken(cookies);
	clearUser(cookies);
	redirect(302, '/login');
};
