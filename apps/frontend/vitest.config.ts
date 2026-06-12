import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

// Standalone Vitest config (does NOT load the SvelteKit/Sentry/mkcert plugins) so unit
// tests run fast and without dev-cert / build-time concerns. The `$lib` alias mirrors
// SvelteKit so server-lib modules resolve in tests.
export default defineConfig({
	resolve: {
		alias: {
			$lib: resolve(__dirname, 'src/lib')
		}
	},
	test: {
		environment: 'node',
		include: ['src/**/*.{test,spec}.ts'],
		globals: false
	}
});
