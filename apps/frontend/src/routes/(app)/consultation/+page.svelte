<script lang="ts">
	import { enhance } from '$app/forms';
	import { onMount, tick } from 'svelte';
	import { page } from '$app/state';
	import type { PageData, ActionData } from './$types';
	import type { PatientSummary, PatientDetail, ConsultationSummary } from '$lib/types/api';
	import { MEDICAMENTS } from '$lib/data/medicaments';
	import Icon from '$lib/components/Icon.svelte';
	import Avatar from '$lib/components/Avatar.svelte';
	import QuickAddPatientModal from '$lib/components/QuickAddPatientModal.svelte';
	import OrdonnanceWindow from '$lib/components/OrdonnanceWindow.svelte';
	import OrdonnancePrint from '$lib/components/OrdonnancePrint.svelte';
	import type { Med, OrdonnancePatient } from '$lib/types/consultation';
	import { toast } from '$lib/stores/toast.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	const appointmentId = $derived(page.url.searchParams.get('appointmentId') ?? '');

	let patients = $state<PatientSummary[]>(data.patients);
	// Preselect from ?patientId= when arriving from a patient file ("Ajouter une consultation").
	let selectedPatientId = $state(page.url.searchParams.get('patientId') ?? '');
	let showQuickAdd = $state(false);

	const selectedPatient = $derived(
		patients.find((p) => p.id === selectedPatientId) ?? null
	);

	function onPatientAdded(patient: PatientSummary) {
		patients = [patient, ...patients];
		selectedPatientId = patient.id;
		showQuickAdd = false;
		toast.success('Patient créé avec succès.');
	}

	// ── Patient clinical context (allergies / antécédents / dernières consultations) ──
	// Lazy-loaded client-side when a patient is picked; the summary list doesn't carry it.
	let ctx = $state<{
		loading: boolean;
		error: boolean;
		detail: PatientDetail | null;
		recent: ConsultationSummary[];
	}>({ loading: false, error: false, detail: null, recent: [] });

	$effect(() => {
		const id = selectedPatientId;
		if (!id) {
			ctx = { loading: false, error: false, detail: null, recent: [] };
			return;
		}
		let cancelled = false;
		ctx = { loading: true, error: false, detail: null, recent: [] };
		Promise.all([
			fetch(`/api/patients/${id}`).then((r) => (r.ok ? r.json() : Promise.reject(r))),
			fetch(`/api/patients/${id}/consultations?pageSize=3`).then((r) =>
				r.ok ? r.json() : Promise.reject(r)
			)
		])
			.then(([detail, cons]) => {
				if (cancelled) return;
				ctx = { loading: false, error: false, detail, recent: cons?.items ?? [] };
			})
			.catch((e) => {
				if (cancelled) return;
				console.error('[consultation:patient-context]', e);
				ctx = { loading: false, error: true, detail: null, recent: [] };
			});
		return () => {
			cancelled = true;
		};
	});

	// ── Header date (today) and doctor ──
	const today = new Intl.DateTimeFormat('fr-FR', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	}).format(new Date());

	function fmtDate(iso: string): string {
		try {
			return new Intl.DateTimeFormat('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(
				new Date(iso)
			);
		} catch {
			return iso;
		}
	}

	// ── Vitals ──
	let bp = $state('');
	let pulse = $state('');
	let temp = $state('');
	let weight = $state('');
	let height = $state('');
	let satO2 = $state('');

	// Status calculated automatically from each value (empty → neutral, out-of-range → warn/danger).
	type Status = 'empty' | 'ok' | 'warn' | 'danger';
	const STATUS_COLOR: Record<Status, string> = {
		empty: 'var(--border-strong)',
		ok: 'var(--success)',
		warn: 'var(--warning)',
		danger: 'var(--danger)'
	};
	const STATUS_LABEL: Record<Status, string> = {
		empty: '',
		ok: 'normal',
		warn: 'à surveiller',
		danger: 'anormal'
	};
	function num(v: string): number | null {
		const n = parseFloat(String(v).replace(',', '.'));
		return Number.isFinite(n) ? n : null;
	}
	function bpStatus(v: string): Status {
		if (!v.trim()) return 'empty';
		const m = v.match(/^\s*(\d{2,3})\s*\/\s*(\d{2,3})\s*$/);
		if (!m) return 'warn';
		const s = +m[1];
		const d = +m[2];
		if (s >= 180 || d >= 110 || s < 90 || d < 60) return 'danger';
		if (s >= 140 || d >= 90) return 'warn';
		return 'ok';
	}
	function rangeStatus(v: string, lowOk: number, highOk: number, lowCrit: number, highCrit: number): Status {
		if (!v.trim()) return 'empty';
		const n = num(v);
		if (n === null) return 'warn';
		if (n <= lowCrit || n >= highCrit) return 'danger';
		if (n < lowOk || n > highOk) return 'warn';
		return 'ok';
	}
	const pulseStatus = $derived(rangeStatus(pulse, 60, 100, 40, 130));
	const tempStatus = $derived(rangeStatus(temp, 36, 37.5, 35, 39.5));
	const spo2Status = $derived(satO2.trim() ? rangeStatus(satO2, 95, 100, 90, 101) : 'empty');

	type Vital = {
		key: string;
		label: string;
		name: string;
		unit: string;
		placeholder: string;
		get: () => string;
		set: (v: string) => void;
		status: () => Status;
		type?: string;
	};
	const vitals = $derived<Vital[]>([
		{ key: 'bp', label: 'Tension', name: 'bloodPressure', unit: 'mmHg', placeholder: '120/80', get: () => bp, set: (v) => (bp = v), status: () => bpStatus(bp) },
		{ key: 'pulse', label: 'Pouls', name: 'pulseRate', unit: 'bpm', placeholder: '72', get: () => pulse, set: (v) => (pulse = v), status: () => pulseStatus },
		{ key: 'temp', label: 'Temp.', name: 'temperature', unit: '°C', placeholder: '37.0', get: () => temp, set: (v) => (temp = v), status: () => tempStatus },
		{ key: 'weight', label: 'Poids', name: 'weight', unit: 'kg', placeholder: '70', get: () => weight, set: (v) => (weight = v), status: () => (weight.trim() ? 'ok' : 'empty') },
		{ key: 'height', label: 'Taille', name: 'height', unit: 'cm', placeholder: '175', type: 'number', get: () => height, set: (v) => (height = v), status: () => (height.trim() ? 'ok' : 'empty') },
		{ key: 'spo2', label: 'SpO₂', name: 'spO2', unit: '%', placeholder: '98', get: () => satO2, set: (v) => (satO2 = v), status: () => spo2Status }
	]);

	// ── Clinical (tabs keep every field in the DOM so all submit, hidden ones included) ──
	let chiefComplaint = $state('');
	let clinicalNotes = $state('');
	let diagnosis = $state('');
	let notes = $state('');
	let fee = $state('');
	// `fee` is bound to a <input type="number">, so Svelte coerces it to a number (or
	// null when emptied) the moment a value is typed. String methods like .trim() must
	// therefore go through this normalised view — calling fee.trim() directly threw
	// "fee.trim is not a function" and silently killed the save handler.
	const feeStr = $derived(String(fee ?? ''));

	type Tab = 'motif' | 'diag' | 'notes';
	let activeTab = $state<Tab>('motif');

	// Quick-fill motifs — one click drops a common reason into the field.
	const MOTIFS = [
		'Bilan diabète trimestriel',
		'Contrôle TA',
		'Renouvellement ordonnance',
		'Syndrome grippal',
		'Certificat médical'
	];
	function applyMotif(label: string) {
		chiefComplaint = chiefComplaint.trim() ? `${chiefComplaint.trim()}, ${label.toLowerCase()}` : label;
	}

	// ── Honoraires: chips come from the act catalogue (preserves act → fee link); fall
	// back to common preset amounts when the catalogue is empty. ──
	type FeeChip = { amount: number; actId: string; actName: string };
	const feeChips = $derived<FeeChip[]>(
		data.acts.length
			? data.acts.map((a) => ({ amount: a.tariff, actId: a.id, actName: a.name }))
			: [2000, 2500, 3000, 4000].map((amount) => ({ amount, actId: '', actName: '' }))
	);
	let selectedActName = $state('');
	function applyFeeChip(chip: FeeChip) {
		fee = String(chip.amount);
		selectedActName = chip.actName;
	}

	// ── Prescription ──
	let medList = $state<string[]>(MEDICAMENTS);
	onMount(async () => {
		try {
			const res = await fetch('/medicaments.json');
			if (!res.ok) return;
			const full = await res.json();
			if (Array.isArray(full) && full.length) medList = full as string[];
		} catch {
			/* connexion indisponible → on garde la liste de démarrage */
		}
	});

	let medications = $state<Med[]>([]);
	let nextId = $state(1);

	function addMed(init: Partial<Med> = {}) {
		medications = [
			...medications,
			{ id: nextId, medication: '', dosage: '', frequency: '', duration: '', quantity: 1, ...init }
		];
		nextId++;
	}
	function removeMed(id: number) {
		medications = medications.filter((m) => m.id !== id);
	}
	function updateMed(id: number, field: keyof Omit<Med, 'id'>, val: string | number) {
		medications = medications.map((m) => (m.id === id ? { ...m, [field]: val } : m));
	}

	// Nombre de lignes effectivement prescrites (nom renseigné).
	const validMedCount = $derived(medications.filter((m) => m.medication.trim()).length);

	// ── Fenêtre Ordonnance (dédiée) + aperçu impression ──
	let showOrdo = $state(false);
	let showPrint = $state(false);

	// Patient transmis à la fenêtre / l'impression (avec allergies du dossier chargé).
	const ordoPatient = $derived<OrdonnancePatient | null>(
		selectedPatient
			? {
					firstName: selectedPatient.firstName,
					lastName: selectedPatient.lastName,
					age: selectedPatient.age,
					gender: selectedPatient.gender,
					bloodGroup: ctx.detail?.bloodGroup ?? selectedPatient.bloodGroup,
					allergies: ctx.detail?.allergies ?? []
				}
			: null
	);

	function openOrdo() {
		showOrdo = true;
	}

	const prescriptionJson = $derived(
		JSON.stringify(
			medications.map(({ medication, dosage, frequency, duration, quantity }) => ({
				medication,
				dosage,
				frequency,
				duration,
				quantity
			}))
		)
	);

	// ── Submit + inline validation (no native confirm dialog — keeps the flow fast) ──
	let submitting = $state(false);
	let formEl = $state<HTMLFormElement | null>(null);
	let patientSelectEl = $state<HTMLSelectElement | null>(null);
	let feeInputEl = $state<HTMLInputElement | null>(null);
	let errors = $state<{ patient?: string; fee?: string }>({});

	function validate(): boolean {
		const e: { patient?: string; fee?: string } = {};
		if (!selectedPatientId) e.patient = 'Sélectionnez un patient avant d’enregistrer.';
		const f = num(fee);
		if (feeStr.trim() && (f === null || f < 0)) e.fee = 'Montant invalide.';
		errors = e;
		return Object.keys(e).length === 0;
	}

	async function handleSave() {
		if (!validate()) {
			await tick();
			if (errors.patient) patientSelectEl?.focus();
			else if (errors.fee) feeInputEl?.focus();
			return;
		}
		formEl?.requestSubmit();
	}

	// ── Keyboard shortcuts ──
	function onKeydown(ev: KeyboardEvent) {
		// Ctrl/Cmd+Enter → save from anywhere.
		if ((ev.ctrlKey || ev.metaKey) && ev.key === 'Enter') {
			ev.preventDefault();
			handleSave();
			return;
		}
		if (ev.altKey && !ev.ctrlKey && !ev.metaKey) {
			if (ev.key === '1') { ev.preventDefault(); activeTab = 'motif'; }
			else if (ev.key === '2') { ev.preventDefault(); activeTab = 'diag'; }
			else if (ev.key === '3') { ev.preventDefault(); activeTab = 'notes'; }
			else if (ev.key.toLowerCase() === 'n') { ev.preventDefault(); if (selectedPatient) openOrdo(); }
			else if (ev.key.toLowerCase() === 'p') { ev.preventDefault(); patientSelectEl?.focus(); }
		}
	}
</script>

<svelte:window onkeydown={onKeydown} />

{#if showQuickAdd}
	<QuickAddPatientModal {onPatientAdded} onClose={() => (showQuickAdd = false)} />
{/if}

<form
	bind:this={formEl}
	method="POST"
	use:enhance={() => {
		submitting = true;
		return async ({ result, update }) => {
			submitting = false;
			// Success is signalled by the redirect to /patients (toast fires there).
			// A failed save returns here — surface the error as a toast (issue #129).
			if (result.type === 'failure') {
				const msg = (result.data?.error as string | undefined) ?? 'Échec de l’enregistrement de la consultation.';
				toast.error(msg);
			} else if (result.type === 'error') {
				toast.error('Échec de l’enregistrement de la consultation.');
			}
			await update();
		};
	}}
>
	<!-- Hidden fields -->
	<input type="hidden" name="appointmentId" value={appointmentId} />
	<input type="hidden" name="patientId" value={selectedPatientId} />
	<input type="hidden" name="prescription" value={prescriptionJson} />
	<input type="hidden" name="actName" value={selectedActName} />

	<div class="cockpit">
		<!-- ════════ LEFT — patient context ════════ -->
		<aside class="rail-left card">
			{#if selectedPatient}
				<div class="patient-head">
					<Avatar nom={selectedPatient.lastName} prenom={selectedPatient.firstName} sexe={selectedPatient.gender} size={64} />
					<h2 class="patient-name">{selectedPatient.firstName} {selectedPatient.lastName}</h2>
					<p class="patient-sub">
						{selectedPatient.age} ans · {selectedPatient.gender === 'F' ? 'Femme' : 'Homme'}
					</p>
					<div class="patient-meta">
						{#if ctx.detail?.bloodGroup ?? selectedPatient.bloodGroup}
							<span class="badge-blood">Groupe {ctx.detail?.bloodGroup ?? selectedPatient.bloodGroup}</span>
						{/if}
						{#if selectedPatient.phone}
							<span class="patient-phone"><Icon name="phone" size={11} color="var(--text-muted)" /> {selectedPatient.phone}</span>
						{/if}
					</div>
				</div>

				<select bind:value={selectedPatientId} bind:this={patientSelectEl} class="mk-input patient-switch" aria-label="Patient">
					<option value="">— Sélectionner un patient —</option>
					{#each patients as p}
						<option value={p.id}>{p.firstName} {p.lastName} ({p.age} ans)</option>
					{/each}
				</select>

				<div class="rail-scroll">
					{#if ctx.loading}
						<p class="muted-note">Chargement du dossier…</p>
					{:else if ctx.error}
						<p class="muted-note" style="color:var(--danger)">Dossier indisponible.</p>
					{:else}
						<!-- Allergies -->
						<section class="ctx-block">
							<div class="ctx-title ctx-title-danger"><Icon name="alertCircle" size={13} color="var(--danger)" /> Allergies</div>
							{#if ctx.detail && ctx.detail.allergies.length}
								<div class="chips">
									{#each ctx.detail.allergies as a}
										<span class="chip-allergy">{a}</span>
									{/each}
								</div>
							{:else}
								<p class="muted-note">Aucune allergie connue</p>
							{/if}
						</section>

						<!-- Antécédents -->
						<section class="ctx-block">
							<div class="ctx-title">Antécédents</div>
							{#if ctx.detail && ctx.detail.medicalHistory.length}
								<ul class="ctx-list">
									{#each ctx.detail.medicalHistory as h}
										<li>{h}</li>
									{/each}
								</ul>
							{:else}
								<p class="muted-note">Aucun antécédent</p>
							{/if}
						</section>

						<!-- Dernières consultations -->
						<section class="ctx-block">
							<div class="ctx-title">Dernières consultations</div>
							{#if ctx.recent.length}
								<ul class="recent-list">
									{#each ctx.recent as c, i}
										<li>
											<span class="recent-dot" class:first={i === 0}></span>
											<span class="recent-date">{fmtDate(c.date)}</span>
											<span class="recent-reason">{c.reason || c.diagnosis || 'Consultation'}</span>
										</li>
									{/each}
								</ul>
							{:else}
								<p class="muted-note">Première consultation</p>
							{/if}
						</section>
					{/if}
				</div>
			{:else}
				<!-- No patient yet -->
				<div class="patient-empty">
					<div class="patient-empty-head">
						<span class="ctx-title">Patient</span>
						<button type="button" class="btn-ghost" onclick={() => (showQuickAdd = true)}>
							<Icon name="plus" size={12} color="var(--primary)" /> Nouveau
						</button>
					</div>
					<select bind:value={selectedPatientId} bind:this={patientSelectEl} class="mk-input" aria-label="Patient">
						<option value="">— Sélectionner un patient —</option>
						{#each patients as p}
							<option value={p.id}>{p.firstName} {p.lastName} ({p.age} ans)</option>
						{/each}
					</select>
					{#if errors.patient}
						<p class="field-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.patient}</p>
					{:else if patients.length === 0}
						<p class="muted-note" style="margin-top:8px">Aucun patient. Utilisez « Nouveau » pour en créer un.</p>
					{:else}
						<p class="muted-note" style="margin-top:8px">Sélectionnez un patient pour afficher son dossier.</p>
					{/if}
				</div>
			{/if}
		</aside>

		<!-- ════════ CENTER — clinical + ordonnance + action bar ════════ -->
		<section class="center">
			<div class="center-scroll">
				<div class="center-inner">
					<header class="center-head">
						<div>
							<h1 class="center-title">Consultation du {today}</h1>
							<p class="center-sub">Suivi{data.doctorName ? ` · Dr. ${data.doctorName}` : ''}</p>
						</div>
						{#if selectedPatient}
							<a class="btn-outline" href={`/patients/${selectedPatientId}`}>
								<Icon name="eye" size={14} color="var(--text-muted)" /> Dossier complet
							</a>
						{/if}
					</header>

					{#if form?.error}
						<div class="banner-error">
							<Icon name="alertCircle" size={15} color="var(--danger)" />
							<span>{form.error}</span>
						</div>
					{/if}

					<!-- Constantes vitales -->
					<div class="vitals-head">
						<Icon name="activity" size={16} color="var(--primary)" />
						<span class="vitals-title">Constantes vitales</span>
						<span class="vitals-hint">· saisie directe, statut calculé automatiquement</span>
					</div>
					<div class="vitals-row">
						{#each vitals as v (v.key)}
							{@const st = v.status()}
							{@const col = STATUS_COLOR[st]}
							<div class="vital-card" style="border-top-color:{col}">
								<label class="vital-label" for={`vital-${v.key}`}>{v.label}</label>
								<input
									id={`vital-${v.key}`}
									name={v.name}
									type={v.type ?? 'text'}
									value={v.get()}
									oninput={(e) => v.set((e.target as HTMLInputElement).value)}
									class="vital-input"
									style="color:{st === 'empty' ? 'var(--text)' : col}"
									placeholder={v.placeholder}
									autocomplete="off"
								/>
								<span class="vital-unit" style="color:{st === 'empty' ? 'var(--text-light)' : col}">
									{v.unit}{STATUS_LABEL[st] ? ` · ${STATUS_LABEL[st]}` : ''}
								</span>
							</div>
						{/each}
					</div>

					<!-- Clinical tabbed card -->
					<div class="card clinical-card">
						<div class="tabs" role="tablist">
							<button type="button" class="mk-tab" class:active={activeTab === 'motif'} role="tab" aria-selected={activeTab === 'motif'} onclick={() => (activeTab = 'motif')}>Motif &amp; examen</button>
							<button type="button" class="mk-tab" class:active={activeTab === 'diag'} role="tab" aria-selected={activeTab === 'diag'} onclick={() => (activeTab = 'diag')}>Diagnostic</button>
							<button type="button" class="mk-tab" class:active={activeTab === 'notes'} role="tab" aria-selected={activeTab === 'notes'} onclick={() => (activeTab = 'notes')}>Notes</button>
						</div>

						<div class="clinical-body">
							<!-- Tab panels: hidden (not removed) so every field still submits -->
							<div class="panel" style:display={activeTab === 'motif' ? '' : 'none'}>
								<div class="motif-chips">
									{#each MOTIFS as m}
										<button type="button" class="chip-add" onclick={() => applyMotif(m)}>＋ {m}</button>
									{/each}
								</div>
								<label class="field-label" for="reason">Motif de consultation</label>
								<textarea id="reason" name="reason" bind:value={chiefComplaint} class="mk-input motif-area" placeholder="Saisissez le motif…"></textarea>
								<label class="field-label" for="clinicalExam" style="margin-top:14px">Examen clinique</label>
								<textarea id="clinicalExam" name="clinicalExam" bind:value={clinicalNotes} class="mk-input exam-area" placeholder="Observations de l’examen…"></textarea>
							</div>

							<div class="panel" style:display={activeTab === 'diag' ? '' : 'none'}>
								<label class="field-label" for="diagnosis">Diagnostic</label>
								<textarea id="diagnosis" name="diagnosis" bind:value={diagnosis} class="mk-input exam-area" placeholder="Diagnostic retenu…"></textarea>
							</div>

							<div class="panel" style:display={activeTab === 'notes' ? '' : 'none'}>
								<label class="field-label" for="notes">Notes &amp; recommandations</label>
								<textarea id="notes" name="notes" bind:value={notes} class="mk-input exam-area" placeholder="Conseils, suivi, prochain RDV…"></textarea>
							</div>
						</div>
					</div>

					<!-- ORDONNANCE — bloc guidé (optionnel) -->
					<div class="ordo-block">
						{#if validMedCount === 0}
							<button type="button" class="ordo-cta" onclick={openOrdo} disabled={!selectedPatient}>
								<span class="ordo-cta-icon"><Icon name="fileText" size={22} color="var(--primary)" /></span>
								<span class="ordo-cta-text">
									<span class="ordo-cta-title">Créer une ordonnance</span>
									<span class="ordo-cta-sub">Optionnel — recherchez et prescrivez des médicaments dans une fenêtre dédiée.</span>
								</span>
								<span class="ordo-cta-open">Ouvrir <Icon name="chevronRight" size={16} color="var(--primary)" /></span>
							</button>
						{:else}
							<div class="card ordo-ready">
								<span class="ordo-ready-icon"><Icon name="checkCircle" size={22} color="var(--success)" /></span>
								<div class="ordo-ready-text">
									<div class="ordo-ready-title">Ordonnance prête</div>
									<div class="ordo-ready-sub">{validMedCount} médicament{validMedCount > 1 ? 's' : ''} prescrit{validMedCount > 1 ? 's' : ''}.</div>
								</div>
								<button type="button" class="btn-outline" onclick={() => (showPrint = true)}>
									<Icon name="printer" size={14} color="var(--text-muted)" /> Imprimer
								</button>
								<button type="button" class="btn-primary-sm" onclick={() => (showOrdo = true)}>
									<Icon name="edit" size={14} color="white" /> Modifier
								</button>
							</div>
						{/if}
					</div>
				</div>
			</div>

			<!-- ════ BARRE D'ACTION — honoraires + enregistrer ════ -->
			<div class="action-bar">
				<div class="action-inner">
					<div class="fee-group">
						<div class="fee-title"><Icon name="dollar" size={15} color="var(--primary)" /> Honoraires</div>
						<div class="fee-line">
							<div class="chips fee-chips">
								{#each feeChips as chip}
									<button
										type="button"
										class="fee-chip"
										class:active={feeStr.trim() !== '' && num(fee) === chip.amount}
										data-act-id={chip.actId}
										title={chip.actName || `${chip.amount} DA`}
										onclick={() => applyFeeChip(chip)}
									>{chip.amount.toLocaleString('fr-FR')}</button>
								{/each}
							</div>
							<div class="fee-amount">
								<input
									id="tariff"
									name="tariff"
									bind:this={feeInputEl}
									bind:value={fee}
									oninput={() => (selectedActName = '')}
									type="number"
									min="0"
									class="fee-input"
									placeholder="0"
								/>
								<span class="fee-currency">DA</span>
							</div>
						</div>
						{#if errors.fee}
							<p class="field-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.fee}</p>
						{/if}
					</div>

					<div class="action-spacer"></div>

					<div class="action-btns">
						{#if validMedCount === 0}
							<button type="button" class="btn-ordo-secondary" onclick={openOrdo} disabled={!selectedPatient}>
								<Icon name="fileText" size={15} color="var(--primary)" /> Créer une ordonnance
							</button>
						{/if}
						<button type="button" class="btn-save" disabled={submitting} onclick={handleSave}>
							<Icon name={submitting ? 'check' : 'dollar'} size={16} color="white" />
							{submitting
								? 'Enregistrement…'
								: `Enregistrer & encaisser ${(feeStr.trim() ? Number(fee) || 0 : 0).toLocaleString('fr-FR')} DA`}
							<span class="kbd">⌘↵</span>
						</button>
					</div>
				</div>
			</div>
		</section>
	</div>
</form>

{#if showOrdo && ordoPatient}
	<OrdonnanceWindow
		patient={ordoPatient}
		{medications}
		{medList}
		{addMed}
		{updateMed}
		{removeMed}
		onPrint={() => (showPrint = true)}
		onClose={() => (showOrdo = false)}
	/>
{/if}

{#if showPrint && ordoPatient}
	<OrdonnancePrint
		patient={ordoPatient}
		{medications}
		doctorName={data.doctorName}
		{today}
		onClose={() => (showPrint = false)}
	/>
{/if}

<style>
	.cockpit {
		height: calc(100vh - 58px);
		display: flex;
		gap: 14px;
		padding: 14px;
		background: var(--bg);
		overflow: hidden;
	}

	/* ── Left rail ── */
	.rail-left {
		width: 300px;
		flex-shrink: 0;
		display: flex;
		flex-direction: column;
		min-height: 0;
		overflow: hidden;
		padding: 18px 16px;
	}
	.rail-scroll {
		overflow-y: auto;
		min-height: 0;
		flex: 1;
	}

	.patient-head {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		padding-bottom: 16px;
		border-bottom: 1px solid var(--border);
	}
	.patient-name {
		font-size: 17px;
		font-weight: 700;
		margin-top: 10px;
		line-height: 1.25;
	}
	.patient-sub {
		font-size: 13px;
		color: var(--text-muted);
		margin-top: 2px;
	}
	.patient-meta {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 6px;
		flex-wrap: wrap;
		margin-top: 10px;
	}
	.badge-blood {
		background: var(--danger-light);
		color: var(--danger);
		font-size: 11.5px;
		font-weight: 700;
		padding: 3px 10px;
		border-radius: 20px;
	}
	.patient-phone {
		font-size: 11.5px;
		color: var(--text-muted);
		display: inline-flex;
		align-items: center;
		gap: 4px;
		border: 1px solid var(--border);
		padding: 2px 9px;
		border-radius: 20px;
	}
	.patient-switch {
		font-size: 13px;
		padding: 7px 10px;
		margin: 12px 0;
		background: var(--bg);
	}
	.patient-empty {
		padding: 2px;
	}
	.patient-empty-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 10px;
	}

	.ctx-block {
		padding: 16px 0;
		border-bottom: 1px solid var(--border);
	}
	.ctx-block:last-child {
		border-bottom: none;
	}
	.ctx-title {
		font-size: 11px;
		font-weight: 700;
		color: var(--text-muted);
		text-transform: uppercase;
		letter-spacing: 0.6px;
		display: flex;
		align-items: center;
		gap: 5px;
		margin-bottom: 8px;
	}
	.ctx-title-danger {
		color: var(--danger);
	}
	.chips {
		display: flex;
		flex-wrap: wrap;
		gap: 6px;
	}
	.chip-allergy {
		background: var(--danger-light);
		color: var(--danger);
		font-size: 12.5px;
		font-weight: 600;
		padding: 5px 11px;
		border-radius: 7px;
	}
	.ctx-list {
		list-style: none;
		display: flex;
		flex-direction: column;
		gap: 7px;
	}
	.ctx-list li {
		font-size: 13px;
		color: var(--text);
		padding-left: 15px;
		position: relative;
	}
	.ctx-list li::before {
		content: '';
		position: absolute;
		left: 2px;
		top: 7px;
		width: 7px;
		height: 7px;
		border-radius: 50%;
		background: var(--info);
	}
	.recent-list {
		list-style: none;
		display: flex;
		flex-direction: column;
		gap: 14px;
		border-left: 2px solid var(--border);
		padding-left: 14px;
		margin-left: 4px;
	}
	.recent-list li {
		display: flex;
		flex-direction: column;
		gap: 1px;
		position: relative;
	}
	.recent-dot {
		position: absolute;
		left: -19px;
		top: 3px;
		width: 9px;
		height: 9px;
		border-radius: 50%;
		border: 2px solid var(--surface);
		background: var(--border-strong);
	}
	.recent-dot.first {
		background: var(--primary);
	}
	.recent-date {
		font-size: 12.5px;
		font-weight: 600;
	}
	.recent-reason {
		font-size: 12px;
		color: var(--text-muted);
		line-height: 1.45;
		margin-top: 2px;
	}
	.muted-note {
		font-size: 12px;
		color: var(--text-light);
	}

	/* ── Center ── */
	.center {
		flex: 1;
		display: flex;
		flex-direction: column;
		min-width: 0;
		min-height: 0;
	}
	.center-scroll {
		flex: 1;
		overflow-y: auto;
		min-height: 0;
		padding: 4px 4px 24px;
	}
	.center-inner {
		max-width: 880px;
		margin: 0 auto;
	}
	.center-head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 12px;
		margin-bottom: 18px;
	}
	.center-title {
		font-size: 20px;
		font-weight: 700;
		letter-spacing: -0.3px;
		line-height: 1.2;
	}
	.center-sub {
		font-size: 13px;
		color: var(--text-muted);
		margin-top: 2px;
	}

	.banner-error {
		display: flex;
		align-items: center;
		gap: 8px;
		padding: 10px 14px;
		background: var(--danger-light);
		border: 1px solid #fecaca;
		border-radius: 8px;
		margin-bottom: 16px;
	}
	.banner-error span {
		font-size: 13px;
		color: var(--danger);
		font-weight: 500;
	}

	.vitals-head {
		display: flex;
		align-items: center;
		gap: 9px;
		margin-bottom: 12px;
	}
	.vitals-title {
		font-size: 14px;
		font-weight: 700;
	}
	.vitals-hint {
		font-size: 12px;
		color: var(--text-light);
		font-weight: 500;
	}
	.vitals-row {
		display: grid;
		grid-template-columns: repeat(6, minmax(0, 1fr));
		gap: 12px;
		margin-bottom: 22px;
	}
	.vital-card {
		background: var(--surface);
		border: 1px solid var(--border);
		border-top: 3px solid var(--border-strong);
		border-radius: 10px;
		padding: 13px;
		display: flex;
		flex-direction: column;
		transition: border-top-color 0.2s;
	}
	.vital-label {
		font-size: 12px;
		font-weight: 600;
		color: var(--text-muted);
		margin-bottom: 8px;
	}
	.vital-input {
		border: none;
		outline: none;
		background: transparent;
		font-family: inherit;
		font-size: 21px;
		font-weight: 700;
		text-align: center;
		color: var(--text);
		width: 100%;
		padding: 0;
		line-height: 1.1;
	}
	.vital-input::placeholder {
		color: var(--text-light);
		font-weight: 500;
	}
	.vital-unit {
		font-size: 11px;
		margin-top: 6px;
		font-weight: 600;
		text-align: center;
	}

	/* ── Clinical card ── */
	.clinical-card {
		padding: 0;
		overflow: hidden;
	}
	.tabs {
		display: flex;
		gap: 4px;
		border-bottom: 1px solid var(--border);
		padding: 0 8px;
	}
	.tabs .mk-tab {
		padding: 12px 16px;
		font-size: 13.5px;
	}
	.clinical-body {
		padding: 18px 20px;
	}
	.panel {
		display: flex;
		flex-direction: column;
	}
	.motif-chips {
		display: flex;
		flex-wrap: wrap;
		gap: 8px;
		margin-bottom: 16px;
	}
	.field-label {
		display: block;
		margin-bottom: 6px;
		font-size: 12.5px;
		font-weight: 600;
		color: var(--text);
	}
	.motif-area {
		min-height: 56px;
		resize: vertical;
	}
	.exam-area {
		min-height: 120px;
		resize: vertical;
	}
	.chip-add {
		background: var(--surface);
		border: 1px solid var(--border-strong);
		color: var(--text-muted);
		font-family: inherit;
		font-size: 12.5px;
		font-weight: 600;
		padding: 6px 12px;
		border-radius: 20px;
		cursor: pointer;
		transition: all 0.12s;
		white-space: nowrap;
	}
	.chip-add:hover {
		border-color: var(--primary);
		color: var(--primary);
	}

	/* ── Ordonnance block ── */
	.ordo-block {
		margin-top: 18px;
	}
	.ordo-cta {
		width: 100%;
		display: flex;
		align-items: center;
		gap: 16px;
		text-align: left;
		background: var(--surface);
		border: 1.5px dashed var(--border-strong);
		border-radius: 12px;
		padding: 18px 20px;
		cursor: pointer;
		font-family: inherit;
		transition: all 0.12s;
	}
	.ordo-cta:hover:not(:disabled) {
		border-color: var(--primary);
		background: var(--primary-50);
	}
	.ordo-cta:disabled {
		opacity: 0.55;
		cursor: not-allowed;
	}
	.ordo-cta-icon {
		width: 46px;
		height: 46px;
		border-radius: 11px;
		flex-shrink: 0;
		background: var(--primary-light);
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.ordo-cta-text {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 2px;
	}
	.ordo-cta-title {
		font-size: 15px;
		font-weight: 700;
	}
	.ordo-cta-sub {
		font-size: 13px;
		color: var(--text-muted);
	}
	.ordo-cta-open {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		color: var(--primary);
		font-weight: 600;
		font-size: 13.5px;
		flex-shrink: 0;
	}

	.ordo-ready {
		padding: 16px 20px;
		display: flex;
		align-items: center;
		gap: 16px;
		border-left: 4px solid var(--success);
	}
	.ordo-ready-icon {
		width: 46px;
		height: 46px;
		border-radius: 11px;
		flex-shrink: 0;
		background: var(--success-light);
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.ordo-ready-text {
		flex: 1;
	}
	.ordo-ready-title {
		font-size: 15px;
		font-weight: 700;
	}
	.ordo-ready-sub {
		font-size: 13px;
		color: var(--text-muted);
		margin-top: 2px;
	}

	/* ── Action bar ── */
	.action-bar {
		flex-shrink: 0;
		border-top: 1px solid var(--border);
		background: var(--surface);
		box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.04);
		padding: 14px 20px;
		border-radius: 0 0 10px 10px;
	}
	.action-inner {
		max-width: 880px;
		margin: 0 auto;
		display: flex;
		align-items: center;
		gap: 24px;
		flex-wrap: wrap;
	}
	.fee-title {
		display: flex;
		align-items: center;
		gap: 6px;
		font-size: 12.5px;
		font-weight: 700;
		color: var(--text-muted);
		text-transform: uppercase;
		letter-spacing: 0.4px;
		margin-bottom: 6px;
	}
	.fee-line {
		display: flex;
		align-items: center;
		gap: 8px;
	}
	.fee-chips {
		gap: 6px;
	}
	.fee-chip {
		background: white;
		border: 1px solid var(--border-strong);
		color: var(--text-muted);
		font-family: inherit;
		font-size: 12.5px;
		font-weight: 600;
		padding: 6px 12px;
		border-radius: 20px;
		cursor: pointer;
		transition: all 0.12s;
	}
	.fee-chip:hover {
		border-color: var(--primary);
		color: var(--primary);
	}
	.fee-chip.active {
		background: var(--primary);
		border-color: var(--primary);
		color: white;
	}
	.fee-amount {
		display: flex;
		align-items: center;
		gap: 5px;
		margin-left: 4px;
	}
	.fee-input {
		width: 84px;
		padding: 7px 10px;
		border: 1.5px solid var(--border-strong);
		border-radius: 8px;
		font-family: inherit;
		font-size: 15px;
		font-weight: 700;
		text-align: right;
		background: white;
		color: var(--text);
		outline: none;
	}
	.fee-input:focus {
		border-color: var(--primary);
		box-shadow: 0 0 0 3px rgba(15, 118, 110, 0.1);
	}
	.fee-currency {
		font-size: 13px;
		font-weight: 700;
		color: var(--text-muted);
	}
	.action-spacer {
		flex: 1;
	}
	.action-btns {
		display: flex;
		align-items: center;
		gap: 10px;
	}
	.btn-ordo-secondary {
		display: inline-flex;
		align-items: center;
		gap: 7px;
		background: var(--surface);
		border: 1px solid var(--primary);
		color: var(--primary);
		border-radius: 9px;
		padding: 11px 18px;
		font-family: inherit;
		font-size: 14px;
		font-weight: 600;
		cursor: pointer;
	}
	.btn-ordo-secondary:hover:not(:disabled) {
		background: var(--primary-50);
	}
	.btn-ordo-secondary:disabled {
		opacity: 0.55;
		cursor: not-allowed;
	}
	.btn-save {
		display: flex;
		align-items: center;
		gap: 8px;
		padding: 11px 22px;
		background: var(--primary);
		color: white;
		border: none;
		border-radius: 9px;
		font-family: inherit;
		font-size: 15px;
		font-weight: 600;
		cursor: pointer;
	}
	.btn-save:hover:not(:disabled) {
		background: var(--primary-dark);
	}
	.btn-save:disabled {
		opacity: 0.6;
		cursor: default;
	}
	.kbd {
		font-size: 11px;
		font-weight: 600;
		opacity: 0.75;
		background: rgba(255, 255, 255, 0.18);
		padding: 1px 6px;
		border-radius: 5px;
	}

	.btn-ghost {
		display: inline-flex;
		align-items: center;
		gap: 4px;
		background: var(--primary-50);
		border: 1px solid var(--primary-light);
		border-radius: 6px;
		padding: 4px 9px;
		font-family: inherit;
		font-size: 11.5px;
		font-weight: 600;
		color: var(--primary);
		cursor: pointer;
	}
	.btn-outline {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		background: var(--surface);
		border: 1px solid var(--border);
		border-radius: 7px;
		padding: 7px 12px;
		font-family: inherit;
		font-size: 12.5px;
		font-weight: 500;
		color: var(--text-muted);
		text-decoration: none;
		white-space: nowrap;
		cursor: pointer;
	}
	.btn-outline:hover {
		border-color: var(--primary);
		color: var(--primary);
	}
	.btn-primary-sm {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		background: var(--primary);
		border: none;
		border-radius: 7px;
		padding: 8px 13px;
		font-family: inherit;
		font-size: 12.5px;
		font-weight: 600;
		color: white;
		cursor: pointer;
		white-space: nowrap;
	}
	.btn-primary-sm:hover {
		background: var(--primary-dark);
	}

	.field-error {
		display: flex;
		align-items: center;
		gap: 5px;
		font-size: 11.5px;
		color: var(--danger);
		font-weight: 500;
		margin-top: 7px;
	}

	/* ── Responsive ── */
	@media (max-width: 1100px) {
		.vitals-row {
			grid-template-columns: repeat(3, minmax(0, 1fr));
		}
	}
	@media (max-width: 920px) {
		.cockpit {
			flex-direction: column;
			height: auto;
			overflow: visible;
		}
		.rail-left {
			width: 100%;
		}
		.center {
			min-height: 0;
		}
		.center-scroll {
			overflow: visible;
		}
	}
</style>
