import * as Sentry from "@sentry/sveltekit";
import { env } from "$env/dynamic/public";

Sentry.init({
  dsn: env.PUBLIC_SENTRY_DSN,
  environment: env.PUBLIC_SENTRY_ENV ?? "development",
  integrations: [
    // send console.log, console.warn, and console.error calls as logs to Sentry
    Sentry.consoleLoggingIntegration({ levels: ["log", "warn", "error"] }),
  ],
  tracesSampleRate: 0.2,
  enableLogs: true,
});
