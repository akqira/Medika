import type { LayoutServerLoad } from "./$types";
import * as Sentry from "@sentry/sveltekit";

export const load: LayoutServerLoad = async () => {
  Sentry.logger.info("Medika frontend app starting up...");
};
