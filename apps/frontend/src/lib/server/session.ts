import type { Cookies } from '@sveltejs/kit';

const COOKIE = 'medika_token';

export function getToken(cookies: Cookies): string | undefined {
	return cookies.get(COOKIE);
}

export function setToken(cookies: Cookies, token: string) {
	cookies.set(COOKIE, token, {
		path: '/',
		httpOnly: true,
		secure: true,
		sameSite: 'lax',
		maxAge: 60 * 60 * 8, // 8h — matches JWT expiry
	});
}

export function clearToken(cookies: Cookies) {
	cookies.delete(COOKIE, { path: '/' });
}
