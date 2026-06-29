<script lang="ts">
	import { enhance } from '$app/forms';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import Badge from '$lib/components/Badge.svelte';
	import { toast } from '$lib/stores/toast.svelte';
	import { isValidDzPhone, DZ_PHONE_ERROR } from '$lib/phone';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	const p = data.patient;

	// ── Field state, prefilled from the existing dossier ──
	let firstName = $state(p.firstName);
	let lastName = $state(p.lastName);
	let dateOfBirth = $state(p.dateOfBirth ?? '');
	let gender = $state<'M' | 'F'>(p.gender);
	let bloodGroup = $state(p.bloodGroup ?? '');
	let phone = $state(p.phone);
	let email = $state(p.email ?? '');
	let address = $state(p.address ?? '');
	let wilaya = $state(p.wilaya ?? '');
	let emergencyContactName = $state(p.emergencyContactName ?? '');
	let emergencyContactPhone = $state(p.emergencyContactPhone ?? '');
	const initAllergies = p.allergies.join(', ');
	const initMedicalHistory = p.medicalHistory.join(', ');
	let allergies = $state(initAllergies);
	let medicalHistory = $state(initMedicalHistory);
	let currentTreatment = $state(p.currentTreatment ?? '');
	let nss = $state(p.nss ?? '');
	let insuranceProvider = $state(p.insuranceProvider ?? '');
	let mutualInsurance = $state(p.mutualInsurance ?? '');

	let loading = $state(false);
	let errors = $state<Record<string, string>>({});

	// Unsaved-changes indicator in the action header.
	const dirty = $derived(
		firstName !== p.firstName ||
			lastName !== p.lastName ||
			dateOfBirth !== (p.dateOfBirth ?? '') ||
			gender !== p.gender ||
			bloodGroup !== (p.bloodGroup ?? '') ||
			phone !== p.phone ||
			email !== (p.email ?? '') ||
			address !== (p.address ?? '') ||
			wilaya !== (p.wilaya ?? '') ||
			emergencyContactName !== (p.emergencyContactName ?? '') ||
			emergencyContactPhone !== (p.emergencyContactPhone ?? '') ||
			allergies !== initAllergies ||
			medicalHistory !== initMedicalHistory ||
			currentTreatment !== (p.currentTreatment ?? '') ||
			nss !== (p.nss ?? '') ||
			insuranceProvider !== (p.insuranceProvider ?? '') ||
			mutualInsurance !== (p.mutualInsurance ?? '')
	);

	const SECTIONS = [
		{ id: 'identite', label: 'Identité', icon: 'user', desc: 'Nom, naissance, groupe sanguin' },
		{ id: 'coordonnees', label: 'Coordonnées', icon: 'mapPin', desc: 'Téléphone, adresse, urgence' },
		{ id: 'medical', label: 'Médical', icon: 'stethoscope', desc: 'Allergies, antécédents, traitement' },
		{ id: 'assurance', label: 'Assurance', icon: 'fileText', desc: 'NSS et couverture sociale' },
	] as const;

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
		{ value: 'CNAS', label: 'CNAS', desc: 'Travailleurs salariés' },
		{ value: 'CASNOS', label: 'CASNOS', desc: 'Non-salariés' },
		{ value: 'Military', label: 'Militaire', desc: 'Forces de sécurité' },
		{ value: 'None', label: 'Sans couverture', desc: 'Patient non assuré' },
	];

	const today = new Date();
	const maxDate = today.toISOString().slice(0, 10);
	const minDate = new Date(today.getFullYear() - 100, today.getMonth(), today.getDate())
		.toISOString().slice(0, 10);

	const age = $derived.by(() => {
		if (!dateOfBirth) return p.age;
		const dob = new Date(dateOfBirth);
		if (Number.isNaN(dob.getTime())) return p.age;
		return Math.max(0, Math.floor((Date.now() - dob.getTime()) / (1000 * 60 * 60 * 24 * 365.25)));
	});

	// ── Validation (runs on save; same rules as the create form) ──
	const NAME_RE = /^[\p{L}\s'\-]{2,}$/u;
	const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
	const NSS_RE = /^\d{15}$/;

	// Maps each error field to the section it lives in, so we can scroll to the first one.
	const ERROR_SECTION: Record<string, string> = {
		firstName: 'identite', lastName: 'identite', dateOfBirth: 'identite',
		phone: 'coordonnees', email: 'coordonnees', emergencyContactPhone: 'coordonnees',
		nss: 'assurance',
	};

	function validate() {
		const e: Record<string, string> = {};
		const fn = firstName.trim();
		const ln = lastName.trim();
		if (!fn) e.firstName = 'Champ requis';
		else if (!NAME_RE.test(fn)) e.firstName = 'Lettres uniquement, 2 caractères minimum';
		else if (fn.length > 100) e.firstName = '100 caractères maximum';
		if (!ln) e.lastName = 'Champ requis';
		else if (!NAME_RE.test(ln)) e.lastName = 'Lettres uniquement, 2 caractères minimum';
		else if (ln.length > 100) e.lastName = '100 caractères maximum';
		if (!dateOfBirth) e.dateOfBirth = 'Date de naissance requise';
		else if (dateOfBirth > maxDate) e.dateOfBirth = 'La date ne peut pas être dans le futur';
		else if (dateOfBirth < minDate) e.dateOfBirth = 'Âge maximum 100 ans';
		const ph = phone.trim();
		if (!ph) e.phone = 'Champ requis';
		else if (!isValidDzPhone(ph)) e.phone = DZ_PHONE_ERROR;
		const ep = emergencyContactPhone.trim();
		if (ep && !isValidDzPhone(ep)) e.emergencyContactPhone = DZ_PHONE_ERROR;
		const em = email.trim();
		if (em && !EMAIL_RE.test(em)) e.email = 'Adresse email invalide';
		const n = nss.trim();
		if (n && !NSS_RE.test(n)) e.nss = 'Le NSS doit contenir exactement 15 chiffres';
		errors = e;
		return Object.keys(e).length === 0;
	}

	// ── Single-page scroll + section-nav active tracking ──
	let scrollEl = $state<HTMLDivElement>();
	const secEls: Record<string, HTMLElement> = {};
	let active = $state<string>('identite');

	function onScroll() {
		if (!scrollEl) return;
		const top = scrollEl.scrollTop + 120;
		let cur: string = SECTIONS[0].id;
		for (const s of SECTIONS) {
			const el = secEls[s.id];
			if (el && el.offsetTop <= top) cur = s.id;
		}
		active = cur;
	}
	function jumpTo(id: string) {
		const el = secEls[id];
		if (scrollEl && el) scrollEl.scrollTo({ top: el.offsetTop - 16, behavior: 'smooth' });
	}
</script>

<svelte:head>
	<title>Modifier · {p.firstName} {p.lastName}</title>
</svelte:head>

<form
	method="POST"
	use:enhance={({ cancel }) => {
		if (!validate()) {
			// Invalid input never reaches the server; scroll to the first offending section.
			const firstKey = Object.keys(errors)[0];
			const sec = firstKey ? ERROR_SECTION[firstKey] : undefined;
			if (sec) jumpTo(sec);
			cancel();
			return;
		}
		loading = true;
		return async ({ result, update }) => {
			loading = false;
			// Success redirects to the dossier (toast fires there). A failed save returns here.
			if (result.type === 'failure') {
				const msg = (result.data?.error as string | undefined) ?? 'Échec de la mise à jour du patient.';
				toast.error(msg);
			} else if (result.type === 'error') {
				toast.error('Échec de la mise à jour du patient.');
			}
			await update();
		};
	}}
	style="display:flex;flex-direction:column;height:calc(100vh - 58px);background:var(--bg)"
>
	<!-- Custom controls (button groups) post their values via hidden inputs. -->
	<input type="hidden" name="gender" value={gender} />
	<input type="hidden" name="insuranceProvider" value={insuranceProvider} />

	<!-- ── Sticky action header ── -->
	<div style="flex-shrink:0;background:var(--surface);border-bottom:1px solid var(--border);box-shadow:0 1px 4px rgba(0,0,0,0.04);padding:13px 28px;display:flex;align-items:center;gap:16px">
		<a href="/patients/{p.id}" style="display:flex;align-items:center;gap:6px;background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:8px 13px;text-decoration:none;font-family:inherit;font-size:13.5px;color:var(--text-muted);font-weight:500">
			<Icon name="chevronLeft" size={15} color="var(--text-muted)" /> Retour au dossier
		</a>
		<div style="display:flex;align-items:center;gap:12px;min-width:0">
			<Avatar nom={lastName} prenom={firstName} sexe={gender} size={40} />
			<div style="min-width:0">
				<h1 class="truncate" style="font-size:17px;font-weight:700;letter-spacing:-0.3px;line-height:1.2">
					Modifier le dossier — {firstName} {lastName}
				</h1>
				<p style="font-size:12.5px;color:var(--text-muted)">
					{age >= 0 && age < 130 ? `${age} ans · ` : ''}{gender === 'F' ? 'Femme' : 'Homme'} · Mise à jour de toutes les informations
				</p>
			</div>
		</div>

		<div style="flex:1"></div>

		<div style="display:flex;align-items:center;gap:12px">
			<span style="font-size:12.5px;font-weight:600;display:flex;align-items:center;gap:6px;color:{dirty ? 'var(--accent)' : 'var(--text-light)'}">
				<span style="width:8px;height:8px;border-radius:50%;background:{dirty ? 'var(--accent)' : 'var(--border-strong)'}"></span>
				{dirty ? 'Modifications non enregistrées' : 'Aucune modification'}
			</span>
			<a href="/patients/{p.id}" style="padding:9px 16px;border-radius:8px;border:1px solid var(--border);background:var(--surface);text-decoration:none;font-family:inherit;font-size:13.5px;font-weight:500;color:var(--text)">
				Annuler
			</a>
			<button type="submit" disabled={loading} style="display:flex;align-items:center;gap:8px;padding:10px 22px;border-radius:8px;border:none;font-family:inherit;font-size:14.5px;font-weight:700;background:{loading ? '#5BA8A2' : 'var(--primary)'};color:white;cursor:{loading ? 'not-allowed' : 'pointer'};box-shadow:0 3px 12px rgba(15,118,110,0.28)">
				{#if loading}
					<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5" style="animation:spin 0.8s linear infinite">
						<path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
					</svg>
					Enregistrement…
				{:else}
					<Icon name="check" size={17} color="white" /> Enregistrer les modifications
				{/if}
			</button>
		</div>
	</div>

	<!-- ── Scrollable body ── -->
	<div bind:this={scrollEl} onscroll={onScroll} style="flex:1;overflow:auto">
		<div style="max-width:1040px;margin:0 auto;padding:24px 28px 80px;display:grid;grid-template-columns:212px 1fr;gap:28px;align-items:start">

			<!-- Section nav (sticky, scroll-spy) -->
			<aside style="position:sticky;top:0;display:flex;flex-direction:column;gap:4px">
				{#each SECTIONS as s}
					{@const on = active === s.id}
					<button type="button" onclick={() => jumpTo(s.id)}
						style="display:flex;align-items:center;gap:11px;width:100%;text-align:left;padding:11px 13px;border-radius:9px;cursor:pointer;font-family:inherit;transition:all .12s;border:1px solid {on ? 'var(--primary)' : 'transparent'};background:{on ? 'var(--surface)' : 'transparent'};box-shadow:{on ? '0 1px 4px rgba(0,0,0,0.05)' : 'none'}">
						<div style="width:30px;height:30px;border-radius:8px;flex-shrink:0;display:flex;align-items:center;justify-content:center;background:{on ? 'var(--primary)' : 'var(--surface)'};border:{on ? 'none' : '1px solid var(--border)'}">
							<Icon name={s.icon} size={15} color={on ? 'white' : 'var(--text-muted)'} />
						</div>
						<div style="font-size:13.5px;font-weight:{on ? 700 : 500};color:{on ? 'var(--primary)' : 'var(--text)'}">{s.label}</div>
					</button>
				{/each}
				<div style="margin-top:14px;padding:12px 14px;border-radius:9px;background:var(--primary-50);border:1px solid var(--primary-light)">
					<div style="font-size:12px;color:var(--primary);font-weight:600;line-height:1.5">
						Toutes les sections sont sur une seule page. Modifiez ce dont vous avez besoin, puis enregistrez.
					</div>
				</div>
			</aside>

			<!-- Sections -->
			<div style="display:flex;flex-direction:column;gap:20px;min-width:0">

				<!-- IDENTITÉ -->
				<div bind:this={secEls['identite']} class="card sec-card">
					<div class="sec-head">
						<div class="sec-icon"><Icon name="user" size={17} color="var(--primary)" /></div>
						<div>
							<div class="sec-title">{SECTIONS[0].label}</div>
							<div class="sec-desc">{SECTIONS[0].desc}</div>
						</div>
					</div>
					<div class="sec-body" style="display:flex;flex-direction:column;gap:18px">
						<div class="grid-2">
							<div>
								<label for="firstName" class="fld-label" class:err={errors.firstName}>Prénom</label>
								<input id="firstName" name="firstName" bind:value={firstName} class="mk-input" class:input-err={errors.firstName} />
								{#if errors.firstName}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.firstName}</p>{/if}
							</div>
							<div>
								<label for="lastName" class="fld-label" class:err={errors.lastName}>Nom</label>
								<input id="lastName" name="lastName" bind:value={lastName} class="mk-input" class:input-err={errors.lastName} />
								{#if errors.lastName}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.lastName}</p>{/if}
							</div>
						</div>
						<div class="grid-2">
							<div>
								<label for="dateOfBirth" class="fld-label" class:err={errors.dateOfBirth}>Date de naissance</label>
								<input id="dateOfBirth" name="dateOfBirth" type="date" bind:value={dateOfBirth} min={minDate} max={maxDate} class="mk-input" class:input-err={errors.dateOfBirth} />
								{#if errors.dateOfBirth}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.dateOfBirth}</p>{/if}
							</div>
							<div>
								<span class="fld-label">Sexe</span>
								<div style="display:flex;gap:8px">
									{#each [{ v: 'M', l: 'Masculin' }, { v: 'F', l: 'Féminin' }] as opt}
										<button type="button" class="seg-btn {gender === opt.v ? 'active' : ''}" onclick={() => gender = opt.v as 'M' | 'F'}>
											<span class="radio-dot {gender === opt.v ? 'checked' : ''}"></span> {opt.l}
										</button>
									{/each}
								</div>
							</div>
						</div>
						<div class="grid-2">
							<div>
								<label for="bloodGroup" class="fld-label">Groupe sanguin</label>
								<select id="bloodGroup" name="bloodGroup" bind:value={bloodGroup} class="mk-input">
									<option value="">Inconnu</option>
									{#each BLOOD_GROUPS as bg}<option value={bg}>{bg}</option>{/each}
								</select>
							</div>
							<div>
								<span class="fld-label">Médecin référent</span>
								<input value="Dr. {data.doctorName} (par défaut)" disabled class="mk-input" style="color:var(--text-muted);background:var(--bg)" />
							</div>
						</div>
					</div>
				</div>

				<!-- COORDONNÉES -->
				<div bind:this={secEls['coordonnees']} class="card sec-card">
					<div class="sec-head">
						<div class="sec-icon"><Icon name="mapPin" size={17} color="var(--primary)" /></div>
						<div>
							<div class="sec-title">{SECTIONS[1].label}</div>
							<div class="sec-desc">{SECTIONS[1].desc}</div>
						</div>
					</div>
					<div class="sec-body" style="display:flex;flex-direction:column;gap:18px">
						<div class="grid-2">
							<div>
								<label for="phone" class="fld-label" class:err={errors.phone}>Téléphone</label>
								<div style="position:relative">
									<div class="fld-icon"><Icon name="phone" size={15} color="var(--text-muted)" /></div>
									<input id="phone" name="phone" type="tel" bind:value={phone} class="mk-input" class:input-err={errors.phone} style="padding-left:40px" />
								</div>
								{#if errors.phone}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.phone}</p>{/if}
							</div>
							<div>
								<label for="email" class="fld-label" class:err={errors.email}>Email</label>
								<div style="position:relative">
									<div class="fld-icon"><Icon name="mail" size={15} color="var(--text-muted)" /></div>
									<input id="email" name="email" type="email" bind:value={email} placeholder="patient@email.com" class="mk-input" class:input-err={errors.email} style="padding-left:40px" />
								</div>
								{#if errors.email}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.email}</p>{/if}
							</div>
						</div>
						<div style="display:grid;grid-template-columns:2fr 1fr;gap:16px">
							<div>
								<label for="address" class="fld-label">Adresse</label>
								<input id="address" name="address" bind:value={address} placeholder="N°, Rue, Cité…" class="mk-input" />
							</div>
							<div>
								<label for="wilaya" class="fld-label">Wilaya</label>
								<select id="wilaya" name="wilaya" bind:value={wilaya} class="mk-input">
									<option value="">Sélectionner…</option>
									{#each WILAYAS as w}<option value={w}>{w}</option>{/each}
								</select>
							</div>
						</div>
						<div style="border-top:1px solid var(--border);padding-top:18px">
							<div style="font-size:13px;font-weight:700;margin-bottom:14px;display:flex;align-items:center;gap:8px">
								<Icon name="alertCircle" size={15} color="var(--accent)" /> Contact en cas d'urgence
								<span style="font-weight:400;font-size:12px;color:var(--text-muted)">(optionnel)</span>
							</div>
							<div class="grid-2">
								<div>
									<label for="emergencyContactName" class="fld-label">Nom &amp; prénom</label>
									<input id="emergencyContactName" name="emergencyContactName" bind:value={emergencyContactName} placeholder="Ex : Benali Fatima (épouse)" class="mk-input" />
								</div>
								<div>
									<label for="emergencyContactPhone" class="fld-label" class:err={errors.emergencyContactPhone}>Téléphone urgence</label>
									<div style="position:relative">
										<div class="fld-icon"><Icon name="phone" size={15} color="var(--text-muted)" /></div>
										<input id="emergencyContactPhone" name="emergencyContactPhone" type="tel" bind:value={emergencyContactPhone} placeholder="0555 XX XX XX" class="mk-input" class:input-err={errors.emergencyContactPhone} style="padding-left:40px" />
									</div>
									{#if errors.emergencyContactPhone}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.emergencyContactPhone}</p>{/if}
								</div>
							</div>
						</div>
					</div>
				</div>

				<!-- MÉDICAL -->
				<div bind:this={secEls['medical']} class="card sec-card">
					<div class="sec-head">
						<div class="sec-icon"><Icon name="stethoscope" size={17} color="var(--primary)" /></div>
						<div>
							<div class="sec-title">{SECTIONS[2].label}</div>
							<div class="sec-desc">{SECTIONS[2].desc}</div>
						</div>
					</div>
					<div class="sec-body" style="display:flex;flex-direction:column;gap:18px">
						<div style="background:var(--danger-light);border:1px solid #FECACA;border-radius:9px;padding:11px 14px;display:flex;gap:10px">
							<Icon name="alertCircle" size={15} color="var(--danger)" style="flex-shrink:0;margin-top:1px" />
							<div style="font-size:12.5px;color:#991B1B;line-height:1.5">
								Les allergies sont affichées en rouge sur chaque ordonnance pour éviter toute prescription dangereuse.
							</div>
						</div>
						<div>
							<label for="allergies" class="fld-label">Allergies connues <span class="fld-hint">séparées par une virgule</span></label>
							<input id="allergies" name="allergies" bind:value={allergies} placeholder="Ex : Pénicilline, Aspirine, Codéine" class="mk-input" />
							{#if allergies.trim()}
								<div style="display:flex;gap:6px;flex-wrap:wrap;margin-top:9px">
									{#each allergies.split(',').map((a) => a.trim()).filter(Boolean) as a}<Badge variant="danger">{a}</Badge>{/each}
								</div>
							{/if}
						</div>
						<div>
							<label for="medicalHistory" class="fld-label">Antécédents médicaux <span class="fld-hint">séparés par une virgule</span></label>
							<textarea id="medicalHistory" name="medicalHistory" bind:value={medicalHistory} rows="2" placeholder="Ex : Hypertension artérielle, Diabète type 2, Asthme…" class="mk-input" style="resize:vertical;line-height:1.6"></textarea>
							{#if medicalHistory.trim()}
								<div style="display:flex;gap:6px;flex-wrap:wrap;margin-top:9px">
									{#each medicalHistory.split(',').map((a) => a.trim()).filter(Boolean) as a}<Badge variant="primary">{a}</Badge>{/each}
								</div>
							{/if}
						</div>
						<div>
							<label for="currentTreatment" class="fld-label">Traitement en cours</label>
							<textarea id="currentTreatment" name="currentTreatment" bind:value={currentTreatment} rows="3" placeholder="Médicaments pris régulièrement, posologies…" class="mk-input" style="resize:vertical;line-height:1.6"></textarea>
						</div>
					</div>
				</div>

				<!-- ASSURANCE -->
				<div bind:this={secEls['assurance']} class="card sec-card">
					<div class="sec-head">
						<div class="sec-icon"><Icon name="fileText" size={17} color="var(--primary)" /></div>
						<div>
							<div class="sec-title">{SECTIONS[3].label}</div>
							<div class="sec-desc">{SECTIONS[3].desc}</div>
						</div>
					</div>
					<div class="sec-body" style="display:flex;flex-direction:column;gap:18px">
						<div>
							<label for="nss" class="fld-label" class:err={errors.nss}>Numéro de sécurité sociale (NSS)</label>
							<input id="nss" name="nss" bind:value={nss} placeholder="Ex : 175031600012345" class="mk-input" class:input-err={errors.nss} />
							{#if errors.nss}<p class="fld-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.nss}</p>{/if}
						</div>
						<div>
							<span class="fld-label">Couverture sociale</span>
							<div style="display:grid;grid-template-columns:1fr 1fr;gap:10px">
								{#each INSURANCE_OPTIONS as opt}
									<button type="button" class="ins-card {insuranceProvider === opt.value ? 'active' : ''}" onclick={() => insuranceProvider = opt.value}>
										<span class="radio-dot {insuranceProvider === opt.value ? 'checked' : ''}"></span>
										<span>
											<span style="display:block;font-size:14px;font-weight:{insuranceProvider === opt.value ? 700 : 500};color:{insuranceProvider === opt.value ? 'var(--primary)' : 'var(--text)'}">{opt.label}</span>
											<span style="display:block;font-size:12px;color:var(--text-muted);margin-top:1px">{opt.desc}</span>
										</span>
									</button>
								{/each}
							</div>
						</div>
						<div>
							<label for="mutualInsurance" class="fld-label">Mutuelle complémentaire <span class="fld-hint">optionnel</span></label>
							<input id="mutualInsurance" name="mutualInsurance" bind:value={mutualInsurance} placeholder="Ex : GAM, AXA, CAAR…" class="mk-input" />
						</div>
					</div>
				</div>

				<!-- Bottom save (so the user doesn't need to scroll back up) -->
				{#if form?.error}
					<div style="padding:12px 14px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;font-size:13.5px;color:var(--danger);display:flex;align-items:center;gap:8px">
						<Icon name="alertCircle" size={15} color="var(--danger)" /> {form.error}
					</div>
				{/if}
				<div style="display:flex;justify-content:flex-end;gap:10px;margin-top:2px">
					<a href="/patients/{p.id}" style="padding:11px 18px;border-radius:8px;border:1px solid var(--border);background:var(--surface);text-decoration:none;font-family:inherit;font-size:14px;font-weight:500;color:var(--text)">
						Annuler
					</a>
					<button type="submit" disabled={loading} style="display:flex;align-items:center;gap:8px;padding:11px 24px;border-radius:8px;border:none;font-family:inherit;font-size:14.5px;font-weight:700;background:{loading ? '#5BA8A2' : 'var(--primary)'};color:white;cursor:{loading ? 'not-allowed' : 'pointer'};box-shadow:0 3px 12px rgba(15,118,110,0.28)">
						<Icon name="check" size={17} color="white" /> Enregistrer les modifications
					</button>
				</div>
			</div>
		</div>
	</div>
</form>

<style>
	@keyframes spin { to { transform: rotate(360deg); } }

	.sec-card { padding: 0; overflow: hidden; scroll-margin-top: 16px; }
	.sec-head {
		padding: 16px 24px; border-bottom: 1px solid var(--border); background: var(--bg);
		display: flex; align-items: center; gap: 12px;
	}
	.sec-icon {
		width: 36px; height: 36px; border-radius: 9px; background: var(--primary-50);
		border: 1px solid var(--primary-light); display: flex; align-items: center; justify-content: center;
	}
	.sec-title { font-weight: 700; font-size: 15px; }
	.sec-desc { font-size: 12.5px; color: var(--text-muted); margin-top: 1px; }
	.sec-body { padding: 22px 24px; }

	.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }

	.fld-label {
		display: block; font-size: 12px; font-weight: 600; color: var(--text-muted);
		margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.3px;
	}
	.fld-label.err { color: var(--danger); }
	.fld-hint { font-weight: 400; text-transform: none; letter-spacing: 0; margin-left: 6px; color: var(--text-light); }

	.fld-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); pointer-events: none; }

	.input-err { border-color: var(--danger) !important; background: var(--danger-light); }
	.input-err:focus { box-shadow: 0 0 0 3px rgba(220,38,38,0.12) !important; }

	.fld-error { display: flex; align-items: center; gap: 5px; font-size: 12px; color: var(--danger); margin-top: 6px; }

	.seg-btn {
		display: flex; align-items: center; justify-content: center; gap: 8px; flex: 1;
		padding: 10px 14px; border-radius: 8px; cursor: pointer; font-family: inherit; font-size: 14px;
		color: var(--text); background: white; border: 1.5px solid var(--border); transition: all 0.12s;
	}
	.seg-btn.active { color: var(--primary); border-color: var(--primary); background: var(--primary-50); font-weight: 600; }

	.radio-dot {
		width: 15px; height: 15px; border-radius: 50%; flex-shrink: 0;
		border: 2px solid var(--border-strong); background: white; display: inline-block; position: relative;
	}
	.radio-dot.checked { border-color: var(--primary); }
	.radio-dot.checked::after { content: ''; position: absolute; inset: 3px; border-radius: 50%; background: var(--primary); }

	.ins-card {
		display: flex; align-items: center; gap: 11px; padding: 12px 15px; border-radius: 9px;
		cursor: pointer; text-align: left; font-family: inherit;
		border: 1.5px solid var(--border); background: white; transition: all 0.12s;
	}
	.ins-card.active { border-color: var(--primary); background: var(--primary-50); }
</style>
