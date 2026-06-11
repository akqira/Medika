import * as Sentry from '@sentry/sveltekit';
// Dynamic (not static) public env: tolerates missing vars at build time, so CI and
// Vercel builds don't fail when the DSN isn't set. Empty DSN just disables Sentry.
import { env } from '$env/dynamic/public';

Sentry.init({
  dsn: env.PUBLIC_SENTRY_DSN,
  environment: env.PUBLIC_SENTRY_ENV ?? 'development',
  tracesSampleRate: 0.2,
});

export const handleError = Sentry.handleErrorWithSentry();