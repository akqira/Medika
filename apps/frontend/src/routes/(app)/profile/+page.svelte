<script lang="ts">
	import { enhance } from '$app/forms';
	import type { PageData, ActionData } from './$types';
	import Icon from '$lib/components/Icon.svelte';
	import Avatar from '$lib/components/Avatar.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let activeTab = $state<'cabinet' | 'compte' | 'securite'>('cabinet');

	// Re-activate the tab that was submitted
	$effect(() => {
		if (form?.tab) activeTab = form.tab as typeof activeTab;
	});

	let showCurrentPwd = $state(false);
	let showNewPwd     = $state(false);

	// Cabinet form fields are bound to local $state (not one-way `value={data...}`).
	// A one-way `value={expr}` input gets re-asserted to `expr` on every component
	// re-render — so editing the phone (which updates `cabinetPhone` on each keystroke)
	// re-rendered the form and silently wiped the user's typed name/order/address back
	// to their loaded values, which then persisted as empty. That was the root cause of
	// #85 ("champs non persistés"). Binding to state makes the inputs survive re-renders.
	let cabinetName    = $state(data.profile.cabinetName ?? '');
	let specialty      = $state(data.profile.specialty ?? '');
	let rppsNumber     = $state(data.profile.rppsNumber ?? '');
	let cabinetAddress = $state(data.profile.cabinetAddress ?? '');
	let cabinetWilaya  = $state(data.profile.cabinetWilaya ?? '');

	// Cabinet phone — numeric-only field, validated as an Algerian number
	// (mobile 05/06/07 or fixed 02/03/04), 10 digits total.
	const DZ_PHONE_RE = /^0[2-7]\d{8}$/;
	let cabinetPhone = $state(data.profile.cabinetPhone ?? '');
	let phoneError   = $state('');
	// Client-side, form-level error shown when the save is cancelled before it
	// reaches the server (e.g. invalid phone). Without it the cancel is silent and
	// the cabinet/order/address edits look like they "didn't persist" (#85).
	let cabinetSaveError = $state('');

	function onPhoneInput(e: Event) {
		// Strip every non-digit so the field can never hold text, cap at 10 digits.
		const el = e.currentTarget as HTMLInputElement;
		const digits = el.value.replace(/\D/g, '').slice(0, 10);
		// Force the DOM in sync even when `digits` equals the current state (e.g.
		// typing a letter after a full strip): no state change → no re-render, so
		// the rejected char would otherwise linger in the input.
		el.value = digits;
		cabinetPhone = digits;
		if (phoneError) phoneError = '';
		if (cabinetSaveError) cabinetSaveError = '';
	}

	function validateCabinetPhone(): boolean {
		const p = cabinetPhone.trim();
		if (p && !DZ_PHONE_RE.test(p)) {
			phoneError = 'Numéro algérien invalide — ex : 0550 12 34 56 ou 023 45 67 89';
			return false;
		}
		phoneError = '';
		return true;
	}

	const SPECIALTIES = [
		'Médecine générale', 'Cardiologie', 'Pédiatrie', 'Gynécologie-Obstétrique',
		'Dermatologie', 'Ophtalmologie', 'ORL', 'Orthopédie', 'Neurologie',
		'Psychiatrie', 'Radiologie', 'Anesthésie-Réanimation', 'Chirurgie générale', 'Autre'
	];

	const WILAYAS = [
		'Adrar','Chlef','Laghouat','Oum El Bouaghi','Batna','Béjaïa','Biskra',
		'Béchar','Blida','Bouira','Tamanrasset','Tébessa','Tlemcen','Tiaret',
		'Tizi Ouzou','Alger','Djelfa','Jijel','Sétif','Saïda','Skikda',
		'Sidi Bel Abbès','Annaba','Guelma','Constantine','Médéa','Mostaganem',
		'M\'Sila','Mascara','Ouargla','Oran','El Bayadh','Illizi','Bordj Bou Arréridj',
		'Boumerdès','El Tarf','Tindouf','Tissemsilt','El Oued','Khenchela',
		'Souk Ahras','Tipaza','Mila','Aïn Defla','Naâma','Aïn Témouchent',
		'Ghardaïa','Relizane'
	];

	const nameParts = $derived(
		data.profile.firstName || data.profile.lastName
			? [data.profile.firstName, data.profile.lastName]
			: ['Dr.', '']
	);
</script>

<div style="max-width:760px;margin:0 auto;padding:28px 24px">

	<!-- Page header -->
	<div style="display:flex;align-items:center;gap:16px;margin-bottom:28px">
		<Avatar
			nom={data.profile.lastName || nameParts[1]}
			prenom={data.profile.firstName || nameParts[0]}
			sexe="M"
			size={52}
		/>
		<div>
			<h1 style="font-size:20px;font-weight:700;letter-spacing:-0.3px">Mon profil</h1>
			<p style="font-size:13.5px;color:var(--text-muted);margin-top:3px">
				{data.profile.firstName} {data.profile.lastName} · {data.profile.specialty || 'Médecin'}
			</p>
		</div>
	</div>

	<!-- Tab strip -->
	<div class="card" style="padding:0;overflow:hidden">
		<div style="display:flex;border-bottom:1px solid var(--border);padding:0 4px">
			{#each [
				{ id: 'cabinet',  label: 'Mon cabinet',  icon: 'mapPin'      },
				{ id: 'compte',   label: 'Mon compte',   icon: 'user'        },
				{ id: 'securite', label: 'Sécurité',     icon: 'shieldCheck' },
			] as tab}
				<button
					onclick={() => activeTab = tab.id as typeof activeTab}
					class="mk-tab {activeTab === tab.id ? 'active' : ''}"
					style="display:flex;align-items:center;gap:7px"
				>
					<Icon name={tab.icon} size={14} color={activeTab === tab.id ? 'var(--primary)' : 'var(--text-muted)'} />
					{tab.label}
				</button>
			{/each}
		</div>

		<!-- Feedback banner -->
		{#if form?.success && !cabinetSaveError}
			<div style="padding:12px 20px;background:var(--success-light);border-bottom:1px solid #A7F3D0;display:flex;align-items:center;gap:8px">
				<Icon name="checkCircle" size={15} color="var(--success)" />
				<span style="font-size:13.5px;color:var(--success);font-weight:500">{form.success}</span>
			</div>
		{/if}
		{#if form?.error}
			<div style="padding:12px 20px;background:var(--danger-light);border-bottom:1px solid #FECACA;display:flex;align-items:center;gap:8px">
				<Icon name="alertCircle" size={15} color="var(--danger)" />
				<span style="font-size:13.5px;color:var(--danger);font-weight:500">{form.error}</span>
			</div>
		{/if}
		{#if cabinetSaveError && activeTab === 'cabinet'}
			<div style="padding:12px 20px;background:var(--danger-light);border-bottom:1px solid #FECACA;display:flex;align-items:center;gap:8px">
				<Icon name="alertCircle" size={15} color="var(--danger)" />
				<span style="font-size:13.5px;color:var(--danger);font-weight:500">{cabinetSaveError}</span>
			</div>
		{/if}

		<div style="padding:24px">

			<!-- ─── TAB: Mon cabinet ─── -->
			{#if activeTab === 'cabinet'}
				<form method="POST" action="?/saveCabinet" use:enhance={({ cancel }) => {
					if (!validateCabinetPhone()) {
						// Don't save an invalid phone — but make the block VISIBLE so the
						// user's cabinet/order/address edits don't silently vanish (#85).
						// The typed values stay in the inputs; the user fixes the phone
						// and re-saves.
						cancel();
						cabinetSaveError = 'Enregistrement annulé : corrigez le numéro de téléphone du cabinet, puis réessayez. Vos autres modifications sont conservées.';
						const phoneEl = document.getElementById('cabinetPhone');
						phoneEl?.scrollIntoView({ block: 'center', behavior: 'smooth' });
						phoneEl?.focus();
						return;
					}
					cabinetSaveError = '';
					return async ({ update }) => update();
				}}>
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:20px">

						<div style="grid-column:1/-1">
							<label for="cabinetName" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Nom du cabinet</label>
							<input id="cabinetName" name="cabinetName" class="mk-input" bind:value={cabinetName} placeholder="Cabinet médical Dr. …" />
						</div>

						<div>
							<label for="specialty" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Spécialité</label>
							<select id="specialty" name="specialty" class="mk-input" bind:value={specialty}>
								<option value="">— Choisir —</option>
								{#each SPECIALTIES as s}
									<option value={s}>{s}</option>
								{/each}
							</select>
						</div>

						<div>
							<label for="rppsNumber" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">N° RPPS / Ordre médical</label>
							<input id="rppsNumber" name="rppsNumber" class="mk-input" bind:value={rppsNumber} placeholder="ex: 10003456789" />
						</div>

						<div style="grid-column:1/-1">
							<label for="cabinetAddress" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Adresse</label>
							<input id="cabinetAddress" name="cabinetAddress" class="mk-input" bind:value={cabinetAddress} placeholder="Rue, numéro, quartier…" />
						</div>

						<div>
							<label for="cabinetWilaya" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Wilaya</label>
							<select id="cabinetWilaya" name="cabinetWilaya" class="mk-input" bind:value={cabinetWilaya}>
								<option value="">— Choisir —</option>
								{#each WILAYAS as w}
									<option value={w}>{w}</option>
								{/each}
							</select>
						</div>

						<div>
							<label for="cabinetPhone" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Téléphone du cabinet</label>
							<input id="cabinetPhone" name="cabinetPhone" type="tel" inputmode="numeric" maxlength="10" class="mk-input" class:input-err={phoneError} value={cabinetPhone} oninput={onPhoneInput} placeholder="ex: 023 45 67 89" />
							{#if phoneError}<p class="field-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {phoneError}</p>{/if}
						</div>

					</div>
					<button type="submit" style="padding:10px 22px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer">
						Enregistrer le cabinet
					</button>
				</form>

			<!-- ─── TAB: Mon compte ─── -->
			{:else if activeTab === 'compte'}
				<form method="POST" action="?/saveAccount" use:enhance>
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:20px">

						<div>
							<label for="firstName" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Prénom</label>
							<input id="firstName" name="firstName" class="mk-input" value={data.profile.firstName} placeholder="ex: Karim" />
						</div>

						<div>
							<label for="lastName" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Nom</label>
							<input id="lastName" name="lastName" class="mk-input" value={data.profile.lastName} placeholder="ex: Benali" />
						</div>

						<div style="grid-column:1/-1">
							<label for="email" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Email de connexion</label>
							<div style="position:relative">
								<div style="position:absolute;left:11px;top:50%;transform:translateY(-50%)">
									<Icon name="mail" size={14} color="var(--text-muted)" />
								</div>
								<input id="email" name="email" type="email" class="mk-input" value={data.profile.email} placeholder="docteur@exemple.com" style="padding-left:34px" />
							</div>
							<p style="font-size:12px;color:var(--text-muted);margin-top:5px">
								Modifier l'email nécessite une confirmation par lien envoyé à la nouvelle adresse.
							</p>
						</div>

					</div>
					<button type="submit" style="padding:10px 22px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer">
						Enregistrer le compte
					</button>
				</form>

			<!-- ─── TAB: Sécurité ─── -->
			{:else}
				<form method="POST" action="?/changePassword" use:enhance>
					<div style="max-width:380px;display:flex;flex-direction:column;gap:16px;margin-bottom:20px">

						<div>
							<label for="currentPassword" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Mot de passe actuel</label>
							<input id="currentPassword" name="currentPassword" type="password" required autocomplete="current-password" class="mk-input" />
						</div>

						<div>
							<label for="newPassword" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Nouveau mot de passe</label>
							<div style="position:relative">
								<input id="newPassword" name="newPassword" type={showNewPwd ? 'text' : 'password'} required autocomplete="new-password" class="mk-input" style="padding-right:40px" />
								<button type="button" onclick={() => showNewPwd = !showNewPwd}
									style="position:absolute;right:10px;top:50%;transform:translateY(-50%);background:none;border:none;cursor:pointer;color:var(--text-muted);display:flex;align-items:center">
									<Icon name="eye" size={15} />
								</button>
							</div>
							<p style="font-size:12px;color:var(--text-muted);margin-top:5px">Minimum 8 caractères.</p>
						</div>

						<div>
							<label for="confirmPassword" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Confirmer le nouveau mot de passe</label>
							<input id="confirmPassword" name="confirmPassword" type="password" required autocomplete="new-password" class="mk-input" />
						</div>

					</div>
					<button type="submit" style="padding:10px 22px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer">
						Modifier le mot de passe
					</button>
				</form>
			{/if}

		</div>
	</div>

</div>

<style>
	.input-err { border-color: var(--danger) !important; background: var(--danger-light); }
	.input-err:focus { box-shadow: 0 0 0 3px rgba(220,38,38,0.12) !important; }
	.field-error {
		display: flex; align-items: center; gap: 5px;
		font-size: 12px; color: var(--danger); margin-top: 6px;
	}
</style>
