import { dev } from "$app/environment";
import { error, isRedirect, redirect } from "@sveltejs/kit";
import { env } from '$env/dynamic/private';

/**
 * Central BFF → .NET API proxy (pattern ported from eGestion).
 *
 * Every outgoing call to the backend goes through this class and gets:
 *  - X-API-KEY           — proves the call comes from our BFF (env.API_SECRET, server-only)
 *  - X-Request-Timestamp — ISO 8601, anti-replay (backend rejects if > 5 min old)
 *  - Authorization       — Bearer JWT when a session token is supplied
 *
 * This module is server-only (lives under src/lib/server/). Never import it
 * from client code, and never expose API_SECRET as a PUBLIC_ env var.
 */

const TOKEN_REQUIRED_REDIRECT = "/login?session_revoked=1";

function baseUrl(): string {
  return env.API_URL ?? "http://localhost:5100";
}

function joinUrl(base: string, path: string): string {
  return `${base.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;
}

export class RemoteApi {
  /**
   * Shared helper: applies security headers and returns the raw fetch Response.
   * The body stream is NOT consumed here, so callers choose how to read it.
   */
  private static async buildAndFetch(args: { url: string; options: RequestInit; token?: string; clientIp?: string }): Promise<{ response: Response; fullUrl: string }> {
    const headers: Record<string, string> = {
      "X-API-KEY": env.API_SECRET ?? "",
      "X-Request-Timestamp": new Date().toISOString(),
      // End-user IP for per-client backend throttling. A custom header (not
      // X-Forwarded-For, which Azure App Service overwrites with the BFF's IP).
      ...(args.clientIp ? { "X-Client-IP": args.clientIp } : {}),
      ...(args.options.body !== undefined && !(args.options.body instanceof FormData) ? { "Content-Type": "application/json; charset=utf-8" } : {}),
      ...(args.token ? { Authorization: `Bearer ${args.token}` } : {}),
      ...(args.options.headers as Record<string, string>),
    };

    const fullUrl = joinUrl(baseUrl(), args.url);

    if (dev) {
      console.debug("[RemoteApi] →", args.options.method ?? "GET", fullUrl);
    }

    const response = await fetch(fullUrl, { ...args.options, headers });
    return { response, fullUrl };
  }

  /**
   * Standard JSON API calls. Maps auth failures to SvelteKit redirects/errors.
   */
  static async fetchThenRetrieveJson<T>(args: { url: string; options?: RequestInit; token?: string; clientIp?: string }): Promise<T> {
    try {
      const { response, fullUrl } = await RemoteApi.buildAndFetch({
        url: args.url,
        options: args.options ?? {},
        token: args.token,
        clientIp: args.clientIp,
      });

      if (response.status === 401) {
        if (args.token) {
          // Authenticated request rejected → session expired/revoked → force re-login
          console.warn(`[RemoteApi] 401 on authenticated request to ${args.url} — resetting session`);
          redirect(307, TOKEN_REQUIRED_REDIRECT);
        }
        // Unauthenticated 401 (e.g. /api/auth/login): could be bad credentials OR a
        // rejected API key / stale timestamp from ApiKeyMiddleware — both return 401.
        // Log the backend body + URL so the two are distinguishable in Vercel/Sentry.
        const body = await response.text().catch(() => "");
        console.warn("[RemoteApi] 401 (unauthenticated)", { url: fullUrl, body });
        error(401, body || "Unauthorized");
      }
      if (response.status === 403) error(403, "Forbidden");
      if (response.status === 404) error(404, "Not found");

      if (!response.ok) {
        const body = await response.text().catch(() => "");
        console.error("[RemoteApi] Non-OK response", {
          url: fullUrl,
          status: response.status,
          body,
        });
        error(response.status, body || `API error ${response.status}`);
      }

      if (response.status === 204) return undefined as T;
      return (await response.json()) as T;
    } catch (err) {
      // Re-throw SvelteKit redirects and error() results as-is
      if (isRedirect(err)) throw err;
      if (err && typeof err === "object" && "status" in err && "body" in err) throw err;

      // Network errors (backend down, DNS failure, timeout, ...)
      console.error("[RemoteApi] Backend unreachable", { url: args.url, err });
      error(503, "SERVICE_UNAVAILABLE");
    }
  }

  /**
   * Binary / stream responses (PDF downloads, file attachments, ...).
   * Pipes the upstream stream directly to the client — zero buffering.
   *
   * Usage in a +server.ts:
   *   return RemoteApi.fetchThenRetrieveStream({ url, token, downloadFilename: 'ordonnance.pdf' });
   */
  static async fetchThenRetrieveStream(args: {
    url: string;
    options?: RequestInit;
    token?: string;
    /** When set, overrides the Content-Disposition filename sent to the browser. */
    downloadFilename?: string;
  }): Promise<Response> {
    const { response, fullUrl } = await RemoteApi.buildAndFetch({
      url: args.url,
      options: args.options ?? {},
      token: args.token,
    });

    if (response.status === 401) redirect(307, TOKEN_REQUIRED_REDIRECT);
    if (response.status === 403) error(403, "Forbidden");

    if (!response.ok) {
      const body = await response.text().catch(() => "");
      console.error("[RemoteApi] Non-OK stream response", {
        url: fullUrl,
        status: response.status,
        body,
      });
      error(response.status, `Erreur lors du téléchargement : ${response.statusText}`);
    }

    const forwardedHeaders: Record<string, string> = {};

    const contentType = response.headers.get("Content-Type");
    if (contentType) forwardedHeaders["Content-Type"] = contentType;

    // Do NOT forward Content-Length: Node's fetch auto-decompresses gzip but keeps
    // the original (compressed) Content-Length, which truncates the download.
    // Omitting it forces chunked transfer, which is always correct.

    if (args.downloadFilename) {
      forwardedHeaders["Content-Disposition"] = `attachment; filename="${args.downloadFilename}"`;
    } else {
      const cd = response.headers.get("Content-Disposition");
      if (cd) forwardedHeaders["Content-Disposition"] = cd;
    }

    return new Response(response.body, { status: 200, headers: forwardedHeaders });
  }
}
