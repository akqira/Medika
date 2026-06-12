import { redirect } from "@sveltejs/kit";
import { clearToken, clearUser } from "$lib/server/session";
import type { RequestHandler } from "./$types";
import * as Sentry from "@sentry/sveltekit";

export const GET: RequestHandler = ({ cookies }) => {
  clearToken(cookies);
  clearUser(cookies);
  redirect(302, "/login");
};
