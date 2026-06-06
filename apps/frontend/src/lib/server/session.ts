import { dev } from '$app/environment';
import type { Cookies } from '@sveltejs/kit';

const COOKIE = 'medika_token';
const USER_COOKIE = 'medika_user';

export interface SessionUser {
	fullName: string;
	role: string;
}

export function getToken(cookies: Cookies): string | undefined {
	return cookies.get(COOKIE);
}

export function setToken(cookies: Cookies, token: string) {
	cookies.set(COOKIE, token, {
		path: '/',
		httpOnly: true,
		secure: !dev,
		sameSite: 'lax',
		maxAge: 60 * 60 * 8,
	});
}

export function clearToken(cookies: Cookies) {
	cookies.delete(COOKIE, { path: '/' });
}

export function getUser(cookies: Cookies): SessionUser | null {
	const raw = cookies.get(USER_COOKIE);
	if (!raw) return null;
	try {
		return JSON.parse(raw) as SessionUser;
	} catch {
		return null;
	}
}

export function setUser(cookies: Cookies, user: SessionUser) {
	cookies.set(USER_COOKIE, JSON.stringify(user), {
		path: '/',
		httpOnly: true,
		secure: !dev,
		sameSite: 'lax',
		maxAge: 60 * 60 * 8,
	});
}

export function clearUser(cookies: Cookies) {
	cookies.delete(USER_COOKIE, { path: '/' });
}
