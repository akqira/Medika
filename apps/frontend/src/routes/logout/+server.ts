import { redirect } from '@sveltejs/kit';
import { clearToken, clearUser } from '$lib/server/session';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = ({ cookies }) => {
	clearToken(cookies);
	clearUser(cookies);
	redirect(302, '/login');
};
