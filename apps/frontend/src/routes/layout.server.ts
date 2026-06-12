import { API_URL } from "$env/static/private";
import type { LayoutServerLoad } from "./(app)/$types";
import * as Sentry from "@sentry/sveltekit";

export const load: LayoutServerLoad = async () => {
  Sentry.logger.info("Medika frontend app starting up...");
  return {
    API_URL,
  };
};
