import tailwindcss from "@tailwindcss/vite";
import { sveltekit } from "@sveltejs/kit/vite";
import { defineConfig } from "vite";
import { readFileSync } from "fs";
import { resolve } from "path";

const certDir = resolve(import.meta.dirname, ".cert");

export default defineConfig({
  plugins: [tailwindcss(), sveltekit()],
  server: {
    https: {
      key: readFileSync(resolve(certDir, "key.pem")),
      cert: readFileSync(resolve(certDir, "cert.pem")),
    },
    host: true,
    port: 5000,
  },
});
