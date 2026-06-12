import { redirect } from '@sveltejs/kit';
import { getToken, getUser } from '$lib/server/session';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ cookies }) => {
	if (!getToken(cookies)) redirect(302, '/login');
	const user = getUser(cookies) ?? { fullName: 'Dr. Dupont', role: 'Doctor' };
	return { user };
};
