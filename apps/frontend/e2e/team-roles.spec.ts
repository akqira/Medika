import { test, expect } from '@playwright/test';
import { login, EMAIL } from './helpers';

// Issue #24 — Role & permission system. The doctor is the cabinet admin: they alone
// see the "Équipe" section and can add secretaries with a customised permission set.
// These specs are failing-path-first and NON-mutating (no secretary is actually created,
// so the seed stays clean and the suite is re-runnable): they assert the admin-only
// visibility and the create-form guards (browser + backend validation).

test.describe('Team & roles — admin surface', () => {
	test.beforeEach(async ({ page }) => {
		await login(page);
	});

	test('the doctor (admin) sees the Équipe section in the nav and can open it', async ({ page }) => {
		await expect(page.getByRole('link', { name: 'Équipe' })).toBeVisible();
		await page.getByRole('link', { name: 'Équipe' }).click();
		await expect(page).toHaveURL(/\/team/);
		await expect(page.getByRole('heading', { name: 'Équipe' })).toBeVisible();
	});

	test('the roster lists the doctor as an administrator with full access', async ({ page }) => {
		await page.goto('/team');
		// The seeded doctor row — admin label and "Accès complet" instead of edit actions.
		await expect(page.getByText('Administrateur').first()).toBeVisible();
		await expect(page.getByText('Accès complet').first()).toBeVisible();
		// A doctor row never exposes a permission editor or a deactivate control.
		await expect(page.getByRole('button', { name: 'Désactiver' })).toHaveCount(0);
	});

	test('adding a member: empty required fields are blocked by the browser — no request fires', async ({ page }) => {
		await page.goto('/team');
		await page.getByRole('button', { name: 'Ajouter un membre' }).click();
		await expect(page.getByRole('heading', { name: 'Nouveau secrétaire' })).toBeVisible();

		await page.getByRole('button', { name: 'Créer le compte' }).click();
		// `required` stops submission, so no success/error banner appears and we stay on the form.
		await expect(page.getByRole('heading', { name: 'Nouveau secrétaire' })).toBeVisible();
		await expect(page.getByText("a été ajouté(e) à l'équipe.")).toHaveCount(0);
	});

	test('adding a member with an already-registered email is rejected by the backend', async ({ page }) => {
		await page.goto('/team');
		await page.getByRole('button', { name: 'Ajouter un membre' }).click();

		await page.getByLabel('Prénom').fill('Test');
		await page.getByLabel('Nom', { exact: true }).fill('Doublon'); // exact: 'Nom' also matches 'Prénom'
		await page.getByLabel('Email').fill(EMAIL); // the seeded doctor's email already exists
		await page.getByLabel('Mot de passe provisoire').fill('Password@123');
		await page.getByRole('button', { name: 'Créer le compte' }).click();

		// Backend uniqueness guard — non-mutating: no account is created.
		await expect(page.getByText(/déjà utilisée|already registered/i)).toBeVisible();
	});
});
