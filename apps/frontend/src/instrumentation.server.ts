import * as Sentry from '@sentry/sveltekit';
Sentry.init({
  dsn: process.env.PUBLIC_SENTRY_DSN,
  environment: process.env.PUBLIC_SENTRY_ENV ?? 'development',
  tracesSampleRate: 0.2,
  enableLogs: true,
});