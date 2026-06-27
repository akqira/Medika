<script lang="ts">
	import { enhance } from '$app/forms';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let step = $state(1);
	let loading = $state(false);
	let errors = $state<Record<string, string>>({});

	// Step 1 — Identité
	let firstName = $state('');
	let lastName = $state('');
	let dateOfBirth = $state('');
	let gender = $state<'M' | 'F'>('M');
	let bloodGroup = $state('');

	// Step 2 — Coordonnées
	let phone = $state('');
	let email = $state('');
	let address = $state('');
	let wilaya = $state('');
	let emergencyContactName = $state('');
	let emergencyContactPhone = $state('');

	// Step 3 — Médical
	let allergies = $state('');
	let medicalHistory = $state('');
	let currentTreatment = $state('');

	// Step 4 — Assurance
	let nss = $state('');
	let insuranceProvider = $state('CNAS');
	let mutualInsurance = $state('');

	const STEPS = [
		{ num: 1, label: 'Identité', icon: 'user' },
		{ num: 2, label: 'Coordonnées', icon: 'mapPin' },
		{ num: 3, label: 'Médical', icon: 'stethoscope' },
		{ num: 4, label: 'Assurance', icon: 'fileText' },
	] as const;

	const STEP_INFO: Record<number, { icon: string; title: string; subtitle: string }> = {
		1: { icon: 'user', title: 'Identité', subtitle: "Informations d'identité du patient" },
		2: { icon: 'mapPin', title: 'Coordonnées', subtitle: 'Numéros de contact et adresse' },
		3: { icon: 'stethoscope', title: 'Médical', subtitle: 'Antécédents, allergies et traitement en cours' },
		4: { icon: 'fileText', title: 'Assurance', subtitle: 'Numéro de sécurité sociale et couverture' },
	};

	const BLOOD_GROUPS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

	const WILAYA_NAMES = [
		'Adrar', 'Chlef', 'Laghouat', 'Oum El Bouaghi', 'Batna', 'Béjaïa', 'Biskra',
		'Béchar', 'Blida', 'Bouira', 'Tamanrasset', 'Tébessa', 'Tlemcen', 'Tiaret',
		'Tizi Ouzou', 'Alger', 'Djelfa', 'Jijel', 'Sétif', 'Saïda', 'Skikda',
		'Sidi Bel Abbès', 'Annaba', 'Guelma', 'Constantine', 'Médéa', 'Mostaganem',
		"M'Sila", 'Mascara', 'Ouargla', 'Oran', 'El Bayadh', 'Illizi', 'Bordj Bou Arréridj',
		'Boumerdès', 'El Tarf', 'Tindouf', 'Tissemsilt', 'El Oued', 'Khenchela',
		'Souk Ahras', 'Tipaza', 'Mila', 'Aïn Defla', 'Naâma', 'Aïn Témouchent',
		'Ghardaïa', 'Relizane'
	];
	const WILAYAS = WILAYA_NAMES.map((name, i) => `${name} (${i + 1})`);

	const INSURANCE_OPTIONS = [
		{ value: 'CNAS', label: 'CNAS', description: "Caisse Nationale d'Assurances Sociales des Travailleurs Salariés" },
		{ value: 'CASNOS', label: 'CASNOS', description: "Caisse d'Assurance Sociale des Non-Salariés" },
		{ value: 'Military', label: 'Militaire / Sécurité', description: 'Couverture militaire ou forces de sécurité' },
		{ value: 'None', label: 'Sans couverture', description: 'Patient sans assurance' },
	];

	const today = new Date();
	const maxDate = today.toISOString().slice(0, 10);
	const minDate = new Date(today.getFullYear() - 100, today.getMonth(), today.getDate()).toISOString().slice(0, 10);

	const age = $derived.by(() => {
		if (!dateOfBirth) return 0;
		const dob = new Date(dateOfBirth);
		if (Number.isNaN(dob.getTime())) return 0;
		return Math.max(0, Math.floor((Date.now() - dob.getTime()) / (1000 * 60 * 60 * 24 * 365.25)));
	});

	function formatDob(iso: string) {
		if (!iso) return '—';
		const [y, m, d] = iso.split('-');
		return `${d}/${m}/${y}`;
	}

	// Latin letters, Arabic letters, spaces, hyphens, apostrophes
	const NAME_RE = /^[\p{L}\s'\-]{2,}$/u;

	const NAME_MAX = 100;

	function validateStep1() {
		const e: Record<string, string> = {};
		const fn = firstName.trim();
		const ln = lastName.trim();
		if (!fn) {
			e.firstName = 'Champ requis';
		} else if (!NAME_RE.test(fn)) {
			e.firstName = 'Lettres uniquement, 2 caractères minimum';
		} else if (fn.length > NAME_MAX) {
			e.firstName = '100 caractères maximum';
		}
		if (!ln) {
			e.lastName = 'Champ requis';
		} else if (!NAME_RE.test(ln)) {
			e.lastName = 'Lettres uniquement, 2 caractères minimum';
		} else if (ln.length > NAME_MAX) {
			e.lastName = '100 caractères maximum';
		}
		if (!dateOfBirth) {
			e.dateOfBirth = 'Date de naissance requise';
		} else if (dateOfBirth > maxDate) {
			e.dateOfBirth = 'La date ne peut pas être dans le futur';
		} else if (dateOfBirth < minDate) {
			e.dateOfBirth = 'Âge maximum 100 ans';
		}
		errors = e;
		return Object.keys(e).length === 0;
	}

	const PHONE_RE = /^0[5-7]\d{8}$/;
	const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

	function validateStep2() {
		const e: Record<string, string> = {};
		const p = phone.trim();
		if (!p) {
			e.phone = 'Champ requis';
		} else if (!PHONE_RE.test(p.replace(/\s/g, ''))) {
			e.phone = 'Format invalide — ex: 0555 12 34 56';
		}
		const ep = emergencyContactPhone.trim();
		if (ep && !PHONE_RE.test(ep.replace(/\s/g, ''))) {
			e.emergencyContactPhone = 'Format invalide — ex: 0555 12 34 56';
		}
		const em = email.trim();
		if (em && !EMAIL_RE.test(em)) {
			e.email = 'Adresse email invalide';
		}
		errors = e;
		return Object.keys(e).length === 0;
	}

	const NSS_RE = /^\d{15}$/;

	function validateStep4() {
		const e: Record<string, string> = {};
		const n = nss.trim();
		if (n && !NSS_RE.test(n)) {
			e.nss = 'Le NSS doit contenir exactement 15 chiffres';
		}
		errors = e;
		return Object.keys(e).length === 0;
	}

	function nextStep() {
		if (step === 1 && !validateStep1()) return;
		if (step === 2 && !validateStep2()) return;
		errors = {};
		step = Math.min(4, step + 1);
	}

	function prevStep() {
		errors = {};
		step = Math.max(1, step - 1);
	}
</script>

<div style="max-width:880px;margin:0 auto;padding:28px 24px 60px">

	<!-- Page header -->
	<div style="display:flex;align-items:center;gap:16px;margin-bottom:22px">
		<a href="/patients" style="display:flex;align-items:center;gap:6px;padding:8px 14px;background:var(--surface);border:1px solid var(--border);border-radius:8px;text-decoration:none;color:var(--text);font-size:13.5px;font-weight:500">
			<Icon name="chevronLeft" size={15} color="var(--text-muted)" />
			Patients
		</a>
		<div>
			<h1 style="font-size:21px;font-weight:700;letter-spacing:-0.3px">Nouveau patient</h1>
			<p style="font-size:13.5px;color:var(--text-muted);margin-top:2px">Renseignez le dossier complet en 4 étapes</p>
		</div>
	</div>

	<!-- Stepper -->
	<div class="card" style="padding:18px 22px;margin-bottom:18px">
		<div style="display:flex;align-items:center">
			{#each STEPS as s, i}
				<div style="display:flex;align-items:center;gap:11px;{i < STEPS.length - 1 ? 'flex:1' : ''}">
					<div class="step-circle {step > s.num ? 'done' : step === s.num ? 'active' : ''}">
						{#if step > s.num}
							<Icon name="check" size={16} color="white" />
						{:else}
							<Icon name={s.icon} size={16} color={step === s.num ? '#0F766E' : '#9CA3AF'} />
						{/if}
					</div>
					<div style="flex-shrink:0">
						<div style="font-size:10.5px;font-weight:700;letter-spacing:0.6px;color:{step === s.num ? '#0F766E' : 'var(--text-light)'}">ÉTAPE {s.num}</div>
						<div style="font-size:14px;font-weight:{step === s.num ? '700' : '500'};color:{step >= s.num ? 'var(--text)' : 'var(--text-muted)'}">{s.label}</div>
					</div>
					{#if i < STEPS.length - 1}
						<div class="step-connector {step > s.num ? 'done' : ''}" style="flex:1"></div>
					{/if}
				</div>
			{/each}
		</div>
	</div>

	<!-- Form error -->
	{#if form?.error}
		<div style="margin-bottom:16px;padding:12px 14px;background:#FEE2E2;border:1px solid #FECACA;border-radius:8px;font-size:13.5px;color:#DC2626;display:flex;align-items:center;gap:8px">
			<Icon name="alertCircle" size={15} color="#DC2626" />
			{form.error}
		</div>
	{/if}

	<form
		method="POST"
		use:enhance={() => {
			if (!validateStep4()) return () => {}; // abort submit
			loading = true;
			return async ({ update }) => {
				loading = false;
				await update();
			};
		}}
	>
		<!-- Persist all field values across step transitions — only the active step's
		     inputs are in the DOM, so hidden inputs carry the other steps' data. -->
		<input type="hidden" name="firstName" value={firstName} />
		<input type="hidden" name="lastName" value={lastName} />
		<input type="hidden" name="dateOfBirth" value={dateOfBirth} />
		<input type="hidden" name="gender" value={gender} />
		<input type="hidden" name="bloodGroup" value={bloodGroup} />
		<input type="hidden" name="phone" value={phone} />
		<input type="hidden" name="email" value={email} />
		<input type="hidden" name="address" value={address} />
		<input type="hidden" name="wilaya" value={wilaya} />
		<input type="hidden" name="emergencyContactName" value={emergencyContactName} />
		<input type="hidden" name="emergencyContactPhone" value={emergencyContactPhone} />
		<input type="hidden" name="allergies" value={allergies} />
		<input type="hidden" name="medicalHistory" value={medicalHistory} />
		<input type="hidden" name="currentTreatment" value={currentTreatment} />
		<input type="hidden" name="nss" value={nss} />
		<input type="hidden" name="insuranceProvider" value={insuranceProvider} />
		<input type="hidden" name="mutualInsurance" value={mutualInsurance} />

		<div class="card" style="overflow:hidden">

			<!-- Section header -->
			<div style="padding:18px 22px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:16px">
				<div style="display:flex;align-items:center;gap:13px;min-width:0">
					<div style="width:38px;height:38px;border-radius:9px;background:var(--primary-50);border:1px solid var(--primary-light);display:flex;align-items:center;justify-content:center;flex-shrink:0">
						<Icon name={STEP_INFO[step].icon} size={17} color="var(--primary)" />
					</div>
					<div style="min-width:0">
						<div style="font-size:15px;font-weight:700">{STEP_INFO[step].title}</div>
						<div style="font-size:12.5px;color:var(--text-muted)">{STEP_INFO[step].subtitle}</div>
					</div>
				</div>
				{#if step > 1 && (firstName || lastName)}
					<div style="display:flex;align-items:center;gap:9px;flex-shrink:0">
						<Avatar nom={lastName} prenom={firstName} sexe={gender} size={32} />
						<div>
							<div style="font-size:13px;font-weight:600;text-align:right">{firstName} {lastName}</div>
							<div style="font-size:11.5px;color:var(--text-muted);text-align:right">{age} ans · {gender === 'F' ? 'Féminin' : 'Masculin'}</div>
						</div>
					</div>
				{/if}
			</div>

			<!-- Step content -->
			<div style="padding:24px">

				{#if step === 1}
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:18px">
						<div>
							<label for="firstName" class="field-label" class:err={errors.firstName}>PRÉNOM *</label>
							<input id="firstName" name="firstName" bind:value={firstName} placeholder="Ahmed" maxlength={NAME_MAX} class="mk-input" class:input-err={errors.firstName} />
							{#if errors.firstName}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.firstName}</p>{/if}
						</div>
						<div>
							<label for="lastName" class="field-label" class:err={errors.lastName}>NOM *</label>
							<input id="lastName" name="lastName" bind:value={lastName} placeholder="Benali" maxlength={NAME_MAX} class="mk-input" class:input-err={errors.lastName} />
							{#if errors.lastName}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.lastName}</p>{/if}
						</div>
						<div>
							<label for="dateOfBirth" class="field-label" class:err={errors.dateOfBirth}>DATE DE NAISSANCE *</label>
							<input id="dateOfBirth" name="dateOfBirth" type="date" bind:value={dateOfBirth} min={minDate} max={maxDate} class="mk-input" class:input-err={errors.dateOfBirth} />
							{#if errors.dateOfBirth}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.dateOfBirth}</p>{/if}
						</div>
						<div>
							<span class="field-label">SEXE</span>
							<div style="display:flex;gap:10px">
								<button type="button" class="seg-btn {gender === 'M' ? 'active' : ''}" onclick={() => gender = 'M'}>
									<span class="radio-dot {gender === 'M' ? 'checked' : ''}"></span> Masculin
								</button>
								<button type="button" class="seg-btn {gender === 'F' ? 'active' : ''}" onclick={() => gender = 'F'}>
									<span class="radio-dot {gender === 'F' ? 'checked' : ''}"></span> Féminin
								</button>
							</div>
						</div>
						<div>
							<label for="bloodGroup" class="field-label">GROUPE SANGUIN</label>
							<select id="bloodGroup" name="bloodGroup" bind:value={bloodGroup} class="mk-input">
								<option value="">Inconnu</option>
								{#each BLOOD_GROUPS as bg}
									<option value={bg}>{bg}</option>
								{/each}
							</select>
						</div>
						<div>
							<span class="field-label">MÉDECIN RÉFÉRENT</span>
							<input value="Dr. {data.doctorName} (par défaut)" disabled class="mk-input" style="color:var(--text-muted);background:var(--bg)" />
						</div>
					</div>
				{/if}

				{#if step === 2}
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:18px">
						<div>
							<label for="phone" class="field-label" class:err={errors.phone}>TÉLÉPHONE *</label>
							<div style="position:relative">
								<div class="field-icon"><Icon name="phone" size={14} color="#9CA3AF" /></div>
								<input id="phone" name="phone" bind:value={phone} placeholder="0555 XX XX XX" class="mk-input" class:input-err={errors.phone} style="padding-left:36px" />
							</div>
							{#if errors.phone}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.phone}</p>{/if}
						</div>
						<div>
							<label for="email" class="field-label">EMAIL</label>
							<div style="position:relative">
								<div class="field-icon"><Icon name="mail" size={14} color="#9CA3AF" /></div>
								<input id="email" name="email" type="email" bind:value={email} placeholder="patient@email.com" class="mk-input" class:input-err={errors.email} style="padding-left:36px" />
							{#if errors.email}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.email}</p>{/if}
							</div>
						</div>
						<div style="grid-column:1 / -1">
							<label for="address" class="field-label">ADRESSE</label>
							<input id="address" name="address" bind:value={address} placeholder="N°, Rue, Cité…" class="mk-input" />
						</div>
						<div>
							<label for="wilaya" class="field-label">WILAYA</label>
							<select id="wilaya" name="wilaya" bind:value={wilaya} class="mk-input">
								<option value="">Sélectionner…</option>
								{#each WILAYAS as w}
									<option value={w}>{w}</option>
								{/each}
							</select>
						</div>
					</div>

					<div style="margin:22px 0 16px;padding-top:18px;border-top:1px solid var(--border);display:flex;align-items:center;gap:7px">
						<Icon name="alertCircle" size={14} color="var(--accent)" />
						<span style="font-size:13px;font-weight:600;color:var(--text)">Contact en cas d'urgence</span>
						<span style="font-size:12.5px;color:var(--text-muted)">(optionnel)</span>
					</div>
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:18px">
						<div>
							<label for="emergencyContactName" class="field-label">NOM &amp; PRÉNOM</label>
							<input id="emergencyContactName" name="emergencyContactName" bind:value={emergencyContactName} placeholder="Ex: Benali Fatima (épouse)" class="mk-input" />
						</div>
						<div>
							<label for="emergencyContactPhone" class="field-label">TÉLÉPHONE URGENCE</label>
							<div style="position:relative">
								<div class="field-icon"><Icon name="phone" size={14} color="#9CA3AF" /></div>
								<input id="emergencyContactPhone" name="emergencyContactPhone" bind:value={emergencyContactPhone} placeholder="0555 XX XX XX" class="mk-input" class:input-err={errors.emergencyContactPhone} style="padding-left:36px" />
								{#if errors.emergencyContactPhone}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.emergencyContactPhone}</p>{/if}
							</div>
						</div>
					</div>
				{/if}

				{#if step === 3}
					<div style="display:flex;gap:10px;padding:13px 16px;background:var(--danger-light);border:1px solid #FCA5A5;border-radius:8px;margin-bottom:20px">
						<Icon name="alertCircle" size={16} color="var(--danger)" style="margin-top:1px" />
						<p style="font-size:13px;color:#991B1B;line-height:1.55">
							<strong>Important :</strong> Les allergies connues seront affichées en rouge sur chaque ordonnance pour éviter les prescriptions dangereuses.
						</p>
					</div>
					<div style="display:flex;flex-direction:column;gap:18px">
						<div>
							<label for="allergies" class="field-label">ALLERGIES CONNUES</label>
							<input id="allergies" name="allergies" bind:value={allergies} placeholder="Ex: Pénicilline, Aspirine, Codéine (séparer par virgule)" class="mk-input" />
						</div>
						<div>
							<label for="medicalHistory" class="field-label">ANTÉCÉDENTS MÉDICAUX</label>
							<textarea id="medicalHistory" name="medicalHistory" bind:value={medicalHistory} rows="3" placeholder="Ex: Hypertension artérielle, Diabète type 2, Asthme… (séparer par virgule)" class="mk-input"></textarea>
						</div>
						<div>
							<label for="currentTreatment" class="field-label">TRAITEMENT EN COURS</label>
							<textarea id="currentTreatment" name="currentTreatment" bind:value={currentTreatment} rows="3" placeholder="Médicaments pris régulièrement, posologies…" class="mk-input"></textarea>
						</div>
					</div>
				{/if}

				{#if step === 4}
					<div style="display:flex;flex-direction:column;gap:20px">
						<div>
							<label for="nss" class="field-label">NUMÉRO DE SÉCURITÉ SOCIALE (NSS)</label>
							<input id="nss" name="nss" bind:value={nss} placeholder="Ex: 175031600012345" class="mk-input" class:input-err={errors.nss} />
						{#if errors.nss}<p class="field-error"><Icon name="alertCircle" size={12} color="#DC2626" /> {errors.nss}</p>{/if}
						</div>

						<div>
							<span class="field-label">COUVERTURE SOCIALE</span>
							<div style="display:flex;flex-direction:column;gap:9px">
								{#each INSURANCE_OPTIONS as opt}
									<label class="insurance-card {insuranceProvider === opt.value ? 'active' : ''}">
										<input type="radio" name="insuranceProvider" value={opt.value} bind:group={insuranceProvider} class="radio-dot {insuranceProvider === opt.value ? 'checked' : ''}" style="appearance:none;margin-top:2px" />
										<div>
											<div style="font-size:13.5px;font-weight:600;color:{insuranceProvider === opt.value ? 'var(--primary)' : 'var(--text)'}">{opt.label}</div>
											<div style="font-size:12px;color:var(--text-muted);margin-top:1px">{opt.description}</div>
										</div>
									</label>
								{/each}
							</div>
						</div>

						<div>
							<label for="mutualInsurance" class="field-label">MUTUELLE COMPLÉMENTAIRE (OPTIONNEL)</label>
							<input id="mutualInsurance" name="mutualInsurance" bind:value={mutualInsurance} placeholder="Ex: GAM, AXA, CAAR…" class="mk-input" />
						</div>

						<!-- Récapitulatif -->
						<div style="background:var(--bg);border:1px solid var(--border);border-radius:9px;padding:16px 18px">
							<p style="font-size:12.5px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Récapitulatif du dossier</p>
							<div style="display:grid;grid-template-columns:1fr 1fr;gap:10px 24px">
								<div class="recap-row"><span>Patient :</span><strong>{firstName || '—'} {lastName}</strong></div>
								<div class="recap-row"><span>Date naissance :</span><strong>{formatDob(dateOfBirth)}</strong></div>
								<div class="recap-row"><span>Sexe :</span><strong>{gender === 'F' ? 'Féminin' : 'Masculin'}</strong></div>
								<div class="recap-row"><span>Groupe sanguin :</span><strong>{bloodGroup || 'Inconnu'}</strong></div>
								<div class="recap-row"><span>Téléphone :</span><strong>{phone || '—'}</strong></div>
								<div class="recap-row"><span>Wilaya :</span><strong>{wilaya || '—'}</strong></div>
							</div>
						</div>
					</div>
				{/if}

			</div>

			<!-- Footer -->
			<div style="padding:16px 22px;border-top:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
				{#if step > 1}
					<button type="button" onclick={prevStep} style="display:flex;align-items:center;gap:6px;padding:9px 16px;background:var(--surface);border:1px solid var(--border);border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:500;color:var(--text);cursor:pointer">
						<Icon name="chevronLeft" size={14} color="var(--text-muted)" /> Précédent
					</button>
				{:else}
					<span></span>
				{/if}

				<div style="display:flex;align-items:center;gap:14px">
					<span style="font-size:12.5px;color:var(--text-muted)">Étape {step} sur 4</span>
					{#if step < 4}
						<button type="button" onclick={nextStep} style="display:flex;align-items:center;gap:6px;padding:9px 18px;background:var(--primary);border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;color:white;cursor:pointer">
							Continuer <Icon name="chevronRight" size={14} color="white" />
						</button>
					{:else}
						<button type="submit" disabled={loading} style="display:flex;align-items:center;gap:7px;padding:9px 18px;background:{loading ? '#5BA8A2' : 'var(--primary)'};border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;color:white;cursor:{loading ? 'not-allowed' : 'pointer'}">
							{#if loading}
								<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5" style="animation:spin 0.8s linear infinite">
									<path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
								</svg>
								Création…
							{:else}
								<Icon name="user" size={14} color="white" /> Créer le dossier
							{/if}
						</button>
					{/if}
				</div>
			</div>
		</div>
	</form>

	<p style="text-align:center;margin-top:18px;font-size:12.5px;color:var(--text-muted);line-height:1.6;max-width:560px;margin-left:auto;margin-right:auto">
		Les champs marqués <strong style="color:var(--danger)">*</strong> sont obligatoires. Vous pourrez compléter les autres informations plus tard depuis le dossier patient.
	</p>
</div>

<style>
	@keyframes spin { to { transform: rotate(360deg); } }

	.field-label {
		display: block; font-size: 11.5px; font-weight: 700; letter-spacing: 0.5px;
		color: var(--text-muted); text-transform: uppercase; margin-bottom: 7px;
	}
	.field-label.err { color: var(--danger); }

	.field-icon {
		position: absolute; left: 11px; top: 50%; transform: translateY(-50%); pointer-events: none;
	}

	.input-err { border-color: var(--danger) !important; background: var(--danger-light); }
	.input-err:focus { box-shadow: 0 0 0 3px rgba(220,38,38,0.12) !important; }

	.field-error {
		display: flex; align-items: center; gap: 5px;
		font-size: 12px; color: var(--danger); margin-top: 6px;
	}

	.step-circle {
		width: 38px; height: 38px; border-radius: 50%; flex-shrink: 0;
		display: flex; align-items: center; justify-content: center;
		background: var(--bg); border: 2px solid var(--border);
	}
	.step-circle.active { background: var(--primary-50); border-color: var(--primary); }
	.step-circle.done { background: var(--primary); border-color: var(--primary); }

	.step-connector { height: 2px; background: var(--border); margin: 0 14px; min-width: 30px; }
	.step-connector.done { background: var(--primary); }

	.seg-btn {
		display: flex; align-items: center; gap: 8px; flex: 1; justify-content: center;
		padding: 10px 16px; border-radius: 8px; cursor: pointer; font-family: inherit;
		font-size: 13.5px; font-weight: 500; color: var(--text-muted);
		background: var(--surface); border: 1.5px solid var(--border); transition: all 0.12s;
	}
	.seg-btn.active { color: var(--primary); border-color: var(--primary); background: var(--primary-50); font-weight: 600; }

	.radio-dot {
		width: 16px; height: 16px; border-radius: 50%; flex-shrink: 0;
		border: 2px solid var(--border-strong); background: white; display: inline-block; position: relative;
	}
	.radio-dot.checked { border-color: var(--primary); }
	.radio-dot.checked::after {
		content: ''; position: absolute; inset: 3px; border-radius: 50%; background: var(--primary);
	}

	.insurance-card {
		display: flex; align-items: flex-start; gap: 12px; padding: 13px 16px;
		border: 1.5px solid var(--border); border-radius: 9px; cursor: pointer; transition: all 0.12s;
	}
	.insurance-card.active { border-color: var(--primary); background: var(--primary-50); }
	.insurance-card input[type="radio"] { cursor: pointer; }

	.recap-row { display: flex; justify-content: space-between; gap: 10px; font-size: 13px; }
	.recap-row span { color: var(--text-muted); }
	.recap-row strong { font-weight: 600; text-align: right; }
</style>
