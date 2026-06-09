import type { HandleServerError } from '@sveltejs/kit';

export const handleError: HandleServerError = ({ error, event }) => {
	console.error(`[server] ${event.request.method} ${event.url.pathname}`, error);

	// TODO: replace console.error with Sentry when DSN is configured:
	// Sentry.captureException(error, { extra: { url: event.url.pathname } });

	return {
		message: 'Une erreur inattendue est survenue.',
	};
};
