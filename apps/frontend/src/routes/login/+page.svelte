<script lang="ts">
	import { enhance } from '$app/forms';
	import Icon from '$lib/components/Icon.svelte';
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();
	let loading = $state(false);
	let showPassword = $state(false);

	const FEATURES = [
		'Dossiers patients complets et sécurisés',
		'Agenda intelligent avec vue semaine',
		'Ordonnances et impression en 1 clic',
		'Tableau de bord financier en temps réel',
	];
</script>

<div style="min-height:100vh;display:flex;font-family:'DM Sans',-apple-system,sans-serif">

	<!-- Left panel -->
	<div style="width:42%;background:#1C2B3A;display:flex;flex-direction:column;padding:44px 48px;position:relative;overflow:hidden;flex-shrink:0">

		<!-- Decorative background circle -->
		<div style="position:absolute;bottom:-120px;right:-120px;width:400px;height:400px;border-radius:50%;border:1px solid rgba(255,255,255,0.04)"></div>
		<div style="position:absolute;bottom:-80px;right:-80px;width:280px;height:280px;border-radius:50%;border:1px solid rgba(255,255,255,0.04)"></div>
		<div style="position:absolute;top:80px;left:-60px;width:180px;height:180px;border-radius:50%;background:rgba(15,118,110,0.08)"></div>

		<!-- Logo -->
		<div style="display:flex;align-items:center;gap:10px;position:relative">
			<div style="width:36px;height:36px;border-radius:10px;background:#0F766E;display:flex;align-items:center;justify-content:center;flex-shrink:0">
				<Icon name="activity" size={18} color="white" />
			</div>
			<span style="color:white;font-size:19px;font-weight:700;letter-spacing:-0.3px">
				Medi<span style="color:#5EE7D0">Ka</span>
			</span>
		</div>

		<!-- Main copy -->
		<div style="flex:1;display:flex;flex-direction:column;justify-content:center;padding:60px 0 40px;position:relative">
			<p style="color:#5EE7D0;font-size:12px;font-weight:600;letter-spacing:1.5px;text-transform:uppercase;margin-bottom:16px">
				Logiciel médical
			</p>
			<h1 style="color:white;font-size:30px;font-weight:700;line-height:1.25;letter-spacing:-0.5px;margin-bottom:16px">
				Gérez votre cabinet<br>en toute simplicité.
			</h1>
			<p style="color:rgba(255,255,255,0.45);font-size:14.5px;line-height:1.65;margin-bottom:40px">
				Une solution pensée pour les médecins<br>
				exerçant en cabinet indépendant.
			</p>

			<!-- Features -->
			<div style="display:flex;flex-direction:column;gap:11px">
				{#each FEATURES as feat}
					<div style="display:flex;align-items:center;gap:11px">
						<div style="width:20px;height:20px;border-radius:50%;background:rgba(94,231,208,0.12);display:flex;align-items:center;justify-content:center;flex-shrink:0">
							<Icon name="check" size={10} color="#5EE7D0" />
						</div>
						<span style="color:rgba(255,255,255,0.65);font-size:13.5px">{feat}</span>
					</div>
				{/each}
			</div>
		</div>

		<!-- Footer -->
		<p style="color:rgba(255,255,255,0.2);font-size:12px;position:relative">
			© 2025 MediKa · Données médicales sécurisées
		</p>
	</div>

	<!-- Right panel -->
	<div style="flex:1;display:flex;align-items:center;justify-content:center;background:#F8F7F4;padding:40px">
		<div style="width:100%;max-width:380px">

			<!-- Form header -->
			<div style="margin-bottom:36px">
				<h2 style="font-size:26px;font-weight:700;color:#1A1D23;letter-spacing:-0.5px">Connexion</h2>
				<p style="font-size:14.5px;color:#6B7282;margin-top:7px">
					Accédez à votre espace médecin
				</p>
			</div>

			<!-- Error -->
			{#if form?.error}
				<div style="margin-bottom:20px;padding:12px 14px;background:#FEE2E2;border:1px solid #FECACA;border-radius:8px;font-size:13.5px;color:#DC2626;display:flex;align-items:center;gap:8px">
					<Icon name="alertCircle" size={15} color="#DC2626" />
					{form.error}
				</div>
			{/if}

			<!-- Form -->
			<form
				method="POST"
				use:enhance={() => {
					loading = true;
					return async ({ update }) => {
						loading = false;
						await update();
					};
				}}
			>
				<div style="display:flex;flex-direction:column;gap:18px;margin-bottom:26px">

					<!-- Email -->
					<div>
						<label for="email" style="display:block;font-size:13px;font-weight:500;color:#1A1D23;margin-bottom:7px">
							Email professionnel
						</label>
						<div style="position:relative">
							<div style="position:absolute;left:12px;top:50%;transform:translateY(-50%);pointer-events:none">
								<Icon name="mail" size={15} color="#9CA3AF" />
							</div>
							<input
								id="email"
								name="email"
								type="email"
								required
								autocomplete="email"
								placeholder="docteur@exemple.com"
								class="mk-input login-field"
								style="padding-left:38px"
							/>
						</div>
					</div>

					<!-- Password -->
					<div>
						<label for="password" style="display:block;font-size:13px;font-weight:500;color:#1A1D23;margin-bottom:7px">
							Mot de passe
						</label>
						<div style="position:relative">
							<div style="position:absolute;left:12px;top:50%;transform:translateY(-50%);pointer-events:none">
								<Icon name={showPassword ? 'eyeOff' : 'eye'} size={15} color="#9CA3AF" />
							</div>
							<input
								id="password"
								name="password"
								type={showPassword ? 'text' : 'password'}
								required
								autocomplete="current-password"
								class="mk-input login-field"
								style="padding-left:38px;padding-right:40px"
							/>
							<button
								type="button"
								onclick={() => showPassword = !showPassword}
								style="position:absolute;right:12px;top:50%;transform:translateY(-50%);background:none;border:none;cursor:pointer;color:#9CA3AF;display:flex;align-items:center;padding:0"
								aria-label={showPassword ? 'Masquer' : 'Afficher'}
							>
								<Icon name={showPassword ? 'eyeOff' : 'eye'} size={15} />
							</button>
						</div>
					</div>

				</div>

				<!-- Submit -->
				<button
					type="submit"
					disabled={loading}
					style="
						width:100%;padding:12px;
						background:{loading ? '#5BA8A2' : '#0F766E'};color:white;
						border:none;border-radius:8px;font-family:inherit;font-size:14.5px;font-weight:600;
						cursor:{loading ? 'not-allowed' : 'pointer'};
						transition:background 0.15s;
						display:flex;align-items:center;justify-content:center;gap:8px;
					"
				>
					{#if loading}
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5" style="animation:spin 0.8s linear infinite">
							<path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
						</svg>
						Connexion en cours…
					{:else}
						Se connecter
					{/if}
				</button>
			</form>

			<div style="text-align:center;margin-top:20px">
				<a href="#" style="font-size:13.5px;color:#6B7282;text-decoration:none">
					Mot de passe oublié ?
				</a>
			</div>

		</div>
	</div>
</div>

<style>
	@keyframes spin { to { transform: rotate(360deg); } }
	:global(.login-field) {
		border: 1.5px solid #E8E3DA !important;
		border-radius: 8px !important;
		font-size: 14px !important;
		transition: border-color 0.15s, box-shadow 0.15s !important;
	}
	:global(.login-field:focus) {
		border-color: #0F766E !important;
		box-shadow: 0 0 0 3px rgba(15,118,110,0.12) !important;
	}
</style>
