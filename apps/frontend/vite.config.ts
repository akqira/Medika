import tailwindcss from "@tailwindcss/vite";
import { sveltekit } from "@sveltejs/kit/vite";
import { defineConfig } from "vite";
import { existsSync, readFileSync } from "fs";
import { resolve } from "path";
import { sentrySvelteKit } from '@sentry/sveltekit';

// Local HTTPS dev cert. These files exist only on dev machines (the .cert
// directory is gitignored and never deployed). On Vercel the cert is absent,
// so we must NOT readFileSync it at config-load time — that runs during
// `vite build` too and would fail the production build with ENOENT.
const certDir = resolve(import.meta.dirname, ".cert");
const keyPath = resolve(certDir, "key.pem");
const certPath = resolve(certDir, "cert.pem");
const hasLocalCert = existsSync(keyPath) && existsSync(certPath);

export default defineConfig({
  plugins: [
    sentrySvelteKit({
      adapter: "vercel",
      org: "oritek",
      project: "medika-frontend",
      authToken: process.env.SENTRY_AUTH_TOKEN,
    }),
    tailwindcss(),
    sveltekit(),
  ],
  server: {
    host: true,
    port: 5000,
    ...(hasLocalCert
      ? {
          https: {
            key: readFileSync(keyPath),
            cert: readFileSync(certPath),
          },
        }
      : {}),
  },
});
