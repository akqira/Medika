<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData, PageData } from './$types';
	import Icon from '$lib/components/Icon.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();
	let loading = $state(false);
	let showPassword = $state(false);

	// A usable link must carry both token and email.
	const hasLink = $derived(!!data.token && !!data.email);
</script>

<svelte:head><title>Réinitialiser le mot de passe · MediKa</title></svelte:head>

<div class="auth-wrap">
	<div class="card auth-card">
		<div style="text-align:center;margin-bottom:6px">
			<span style="font-size:20px;font-weight:800;color:var(--primary)">MediKa</span>
		</div>
		<h1 style="font-size:18px;font-weight:700;text-align:center;margin:0 0 4px">Nouveau mot de passe</h1>

		{#if form?.success}
			<div style="display:flex;gap:8px;padding:12px 14px;background:var(--success-light);border:1px solid #A7F3D0;border-radius:8px;font-size:13.5px;color:var(--success);margin:16px 0 18px">
				<Icon name="checkCircle" size={16} color="var(--success)" />
				<span>{form.message}</span>
			</div>
			<a href="/login" style="display:block;text-align:center;padding:11px;background:var(--primary);color:white;border-radius:8px;font-size:14px;font-weight:600;text-decoration:none">
				Se connecter
			</a>
		{:else if !hasLink}
			<p style="font-size:13.5px;color:var(--text-muted);text-align:center;margin:8px 0 18px">
				Ce lien de réinitialisation est invalide ou incomplet.
			</p>
			<a href="/forgot-password" style="display:block;text-align:center;font-size:13.5px;color:var(--primary);font-weight:600;text-decoration:none">
				Refaire une demande
			</a>
		{:else}
			<p style="font-size:13.5px;color:var(--text-muted);text-align:center;margin:0 0 20px">
				Choisissez un nouveau mot de passe (8 caractères minimum).
			</p>

			{#if form?.error}
				<div style="display:flex;gap:8px;padding:12px 14px;background:#FEE2E2;border:1px solid #FECACA;border-radius:8px;font-size:13.5px;color:#DC2626;margin-bottom:18px">
					<Icon name="alertCircle" size={16} color="#DC2626" />
					<span>{form.error}</span>
				</div>
			{/if}

			<form method="POST" use:enhance={() => { loading = true; return async ({ update }) => { loading = false; await update(); }; }}>
				<input type="hidden" name="email" value={data.email} />
				<input type="hidden" name="token" value={data.token} />

				<label for="newPassword" style="display:block;font-size:13px;font-weight:500;margin-bottom:7px">Nouveau mot de passe</label>
				<div style="position:relative;margin-bottom:14px">
					<input
						id="newPassword" name="newPassword" type={showPassword ? 'text' : 'password'}
						required minlength="8" autocomplete="new-password"
						class="mk-input" placeholder="••••••••" style="padding-right:40px" />
					<button type="button" onclick={() => showPassword = !showPassword}
						aria-label={showPassword ? 'Masquer' : 'Afficher'}
						style="position:absolute;right:12px;top:50%;transform:translateY(-50%);background:none;border:none;cursor:pointer;color:#9CA3AF;display:flex;align-items:center;padding:0">
						<Icon name={showPassword ? 'eyeOff' : 'eye'} size={15} />
					</button>
				</div>

				<label for="confirmPassword" style="display:block;font-size:13px;font-weight:500;margin-bottom:7px">Confirmer le mot de passe</label>
				<input
					id="confirmPassword" name="confirmPassword" type="password"
					required minlength="8" autocomplete="new-password"
					class="mk-input" placeholder="••••••••" style="margin-bottom:18px" />

				<button type="submit" disabled={loading}
					style="width:100%;padding:11px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:14px;font-weight:600;cursor:pointer;opacity:{loading ? 0.6 : 1}">
					{loading ? 'Réinitialisation…' : 'Réinitialiser le mot de passe'}
				</button>
			</form>
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
