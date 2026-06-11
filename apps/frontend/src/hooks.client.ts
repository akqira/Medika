import * as Sentry from '@sentry/sveltekit';
import { PUBLIC_SENTRY_DSN, PUBLIC_SENTRY_ENV } from '$env/static/public';

Sentry.init({
  dsn: PUBLIC_SENTRY_DSN,
  environment: PUBLIC_SENTRY_ENV ?? 'development',
  tracesSampleRate: 0.2,
});

export const handleError = Sentry.handleErrorWithSentry();