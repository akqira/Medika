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
		{#if form?.success}
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

		<div style="padding:24px">

			<!-- ─── TAB: Mon cabinet ─── -->
			{#if activeTab === 'cabinet'}
				<form method="POST" action="?/saveCabinet" use:enhance>
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:20px">

						<div style="grid-column:1/-1">
							<label for="cabinetName" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Nom du cabinet</label>
							<input id="cabinetName" name="cabinetName" class="mk-input" value={data.profile.cabinetName} placeholder="Cabinet médical Dr. …" />
						</div>

						<div>
							<label for="specialty" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Spécialité</label>
							<select id="specialty" name="specialty" class="mk-input">
								<option value="">— Choisir —</option>
								{#each SPECIALTIES as s}
									<option value={s} selected={data.profile.specialty === s}>{s}</option>
								{/each}
							</select>
						</div>

						<div>
							<label for="rppsNumber" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">N° RPPS / Ordre médical</label>
							<input id="rppsNumber" name="rppsNumber" class="mk-input" value={data.profile.rppsNumber} placeholder="ex: 10003456789" />
						</div>

						<div style="grid-column:1/-1">
							<label for="cabinetAddress" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Adresse</label>
							<input id="cabinetAddress" name="cabinetAddress" class="mk-input" value={data.profile.cabinetAddress} placeholder="Rue, numéro, quartier…" />
						</div>

						<div>
							<label for="cabinetCity" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Ville</label>
							<input id="cabinetCity" name="cabinetCity" class="mk-input" value={data.profile.cabinetCity} placeholder="ex: Alger" />
						</div>

						<div>
							<label for="cabinetWilaya" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Wilaya</label>
							<select id="cabinetWilaya" name="cabinetWilaya" class="mk-input">
								<option value="">— Choisir —</option>
								{#each WILAYAS as w}
									<option value={w} selected={data.profile.cabinetWilaya === w}>{w}</option>
								{/each}
							</select>
						</div>

						<div>
							<label for="cabinetPhone" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Téléphone du cabinet</label>
							<input id="cabinetPhone" name="cabinetPhone" type="tel" class="mk-input" value={data.profile.cabinetPhone} placeholder="ex: 023 XX XX XX" />
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
