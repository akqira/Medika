import * as Sentry from '@sentry/sveltekit';
import type { HandleServerError } from '@sveltejs/kit';

const myErrorHandler: HandleServerError = ({ error, event }) => {
  console.error(`[server] ${event.request.method} ${event.url.pathname}`, error);
  return { message: 'Une erreur inattendue est survenue.' };
};

export const handleError = Sentry.handleErrorWithSentry(myErrorHandler);
export const handle = Sentry.sentryHandle();