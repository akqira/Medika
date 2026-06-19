import { test, expect } from '@playwright/test';

// Hello-world E2E: log in as the seeded doctor and land on the dashboard.
// Requires BOTH apps running (backend on :5100, frontend on :5000) — see
// playwright.config.ts for the start commands.
const EMAIL = process.env.E2E_EMAIL ?? 'kader.kebir@gmail.com';
const PASSWORD = process.env.E2E_PASSWORD ?? 'Doctor@123';

test('login as seeded doctor lands on dashboard', async ({ page }) => {
  await page.goto('/login');

  await page.getByLabel('Adresse e-mail').fill(EMAIL);
  await page.getByLabel('Mot de passe').fill(PASSWORD);
  await page.getByRole('button', { name: 'Se connecter' }).click();

  await expect(page).toHaveURL(/\/dashboard/);
  await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();
});
