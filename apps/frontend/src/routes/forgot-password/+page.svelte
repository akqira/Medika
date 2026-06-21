<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData } from './$types';
	import Icon from '$lib/components/Icon.svelte';

	let { form }: { form: ActionData } = $props();
	let loading = $state(false);
	// Controlled via $state so the typed value survives hydration/re-renders
	// (a bare `value={form?.email}` gets reset to empty on hydrate, wiping input).
	let email = $state(form?.email ?? '');
</script>

<svelte:head><title>Mot de passe oublié · MediKa</title></svelte:head>

<div class="auth-wrap">
	<div class="card auth-card">
		<div style="text-align:center;margin-bottom:6px">
			<span style="font-size:20px;font-weight:800;color:var(--primary)">MediKa</span>
		</div>
		<h1 style="font-size:18px;font-weight:700;text-align:center;margin:0 0 4px">Mot de passe oublié</h1>
		<p style="font-size:13.5px;color:var(--text-muted);text-align:center;margin:0 0 20px">
			Saisissez votre adresse e-mail pour recevoir un lien de réinitialisation.
		</p>

		{#if form?.success}
			<div style="display:flex;gap:8px;padding:12px 14px;background:var(--success-light);border:1px solid #A7F3D0;border-radius:8px;font-size:13.5px;color:var(--success);margin-bottom:18px">
				<Icon name="checkCircle" size={16} color="var(--success)" />
				<span>{form.message}</span>
			</div>
			<a href="/login" style="display:block;text-align:center;font-size:13.5px;color:var(--primary);font-weight:600;text-decoration:none">
				← Retour à la connexion
			</a>
		{:else}
			{#if form?.error}
				<div style="display:flex;gap:8px;padding:12px 14px;background:#FEE2E2;border:1px solid #FECACA;border-radius:8px;font-size:13.5px;color:#DC2626;margin-bottom:18px">
					<Icon name="alertCircle" size={16} color="#DC2626" />
					<span>{form.error}</span>
				</div>
			{/if}

			<form method="POST" use:enhance={() => { loading = true; return async ({ update }) => { loading = false; await update(); }; }}>
				<label for="email" style="display:block;font-size:13px;font-weight:500;margin-bottom:7px">Adresse e-mail</label>
				<input
					id="email" name="email" type="email" required autocomplete="email"
					bind:value={email}
					class="mk-input" placeholder="vous@exemple.com" style="margin-bottom:18px" />

				<button type="submit" disabled={loading}
					style="width:100%;padding:11px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:14px;font-weight:600;cursor:pointer;opacity:{loading ? 0.6 : 1}">
					{loading ? 'Envoi…' : 'Envoyer le lien'}
				</button>
			</form>

			<a href="/login" style="display:block;text-align:center;font-size:13px;color:var(--text-muted);text-decoration:none;margin-top:16px">
				← Retour à la connexion
			</a>
		{/if}
	</div>
</div>

<style>
	.auth-wrap {
		min-height: 100vh;
		display: flex;
		align-items: center;
		justify-content: center;
		background: var(--bg);
		padding: 20px;
	}
	.auth-card {
		width: 100%;
		max-width: 400px;
		padding: 32px 28px;
	}
</style>
