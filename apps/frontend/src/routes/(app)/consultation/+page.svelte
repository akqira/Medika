<script lang="ts">
	import { enhance } from '$app/forms';
	import { onMount, tick } from 'svelte';
	import { page } from '$app/state';
	import type { PageData, ActionData } from './$types';
	import type { PatientSummary, PatientDetail, ConsultationSummary } from '$lib/types/api';
	import { MEDICAMENTS } from '$lib/data/medicaments';
	import Icon from '$lib/components/Icon.svelte';
	import Avatar from '$lib/components/Avatar.svelte';
	import Combobox from '$lib/components/Combobox.svelte';
	import QuickAddPatientModal from '$lib/components/QuickAddPatientModal.svelte';
	import { expandPosology } from '$lib/posology';
	import { toast } from '$lib/stores/toast.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	const appointmentId = $derived(page.url.searchParams.get('appointmentId') ?? '');

	let patients = $state<PatientSummary[]>(data.patients);
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

	// ── Patient clinical context ──
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
		return () => { cancelled = true; };
	});

	// ── Header ──
	const today = new Intl.DateTimeFormat('fr-FR', {
		day: 'numeric', month: 'long', year: 'numeric'
	}).format(new Date());

	function fmtDate(iso: string): string {
		try {
			return new Intl.DateTimeFormat('fr-FR', {
				day: '2-digit', month: '2-digit', year: 'numeric'
			}).format(new Date(iso));
		} catch { return iso; }
	}

	// ── Vitals ──
	let bp = $state('');
	let pulse = $state('');
	let temp = $state('');
	let weight = $state('');
	let height = $state('');
	let satO2 = $state('');

	type Status = 'empty' | 'ok' | 'warn' | 'danger';
	const STATUS_COLOR: Record<Status, string> = {
		empty: 'var(--border-strong)',
		ok: 'var(--success)',
		warn: 'var(--warning)',
		danger: 'var(--danger)'
	};
	const STATUS_LABEL: Record<Status, string> = {
		empty: '', ok: 'Normal', warn: 'Élevé', danger: 'Critique'
	};

	function num(v: string): number | null {
		const n = parseFloat(String(v).replace(',', '.'));
		return Number.isFinite(n) ? n : null;
	}
	function bpStatus(v: string): Status {
		if (!v.trim()) return 'empty';
		const m = v.match(/^\s*(\d{2,3})\s*\/\s*(\d{2,3})\s*$/);
		if (!m) return 'warn';
		const s = +m[1]; const d = +m[2];
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
		key: string; label: string; name: string; unit: string; placeholder: string;
		get: () => string; set: (v: string) => void; status: () => Status; type?: string;
	};
	const vitals = $derived<Vital[]>([
		{ key: 'bp', label: 'Tension', name: 'bloodPressure', unit: 'mmHg', placeholder: '120/80', get: () => bp, set: (v) => (bp = v), status: () => bpStatus(bp) },
		{ key: 'pulse', label: 'Pouls', name: 'pulseRate', unit: 'bpm', placeholder: '72', get: () => pulse, set: (v) => (pulse = v), status: () => pulseStatus },
		{ key: 'temp', label: 'Temp.', name: 'temperature', unit: '°C', placeholder: '37.0', get: () => temp, set: (v) => (temp = v), status: () => tempStatus },
		{ key: 'weight', label: 'Poids', name: 'weight', unit: 'kg', placeholder: '70', get: () => weight, set: (v) => (weight = v), status: () => (weight.trim() ? 'ok' : 'empty') },
		{ key: 'height', label: 'Taille', name: 'height', unit: 'cm', placeholder: '175', type: 'number', get: () => height, set: (v) => (height = v), status: () => (height.trim() ? 'ok' : 'empty') },
		{ key: 'spo2', label: 'SpO₂', name: 'spO2', unit: '%', placeholder: '98', get: () => satO2, set: (v) => (satO2 = v), status: () => spo2Status }
	]);

	// ── Clinical tabs ──
	let chiefComplaint = $state('');
	let clinicalNotes = $state('');
	let diagnosis = $state('');
	let notes = $state('');
	let fee = $state('');
	const feeStr = $derived(String(fee ?? ''));

	type Tab = 'motif' | 'diag' | 'notes';
	let activeTab = $state<Tab>('motif');

	const MOTIFS = [
		'Bilan diabète trimestriel', 'Contrôle TA',
		'Renouvellement ordonnance', 'Syndrome grippal', 'Certificat médical'
	];
	function applyMotif(label: string) {
		chiefComplaint = chiefComplaint.trim()
			? `${chiefComplaint.trim()}, ${label.toLowerCase()}`
			: label;
	}

	// ── Honoraires ──
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

	const saveBtnLabel = $derived(
		feeStr.trim() && num(fee) !== null && num(fee)! > 0
			? `Enregistrer & encaisser ${Number(fee).toLocaleString('fr-FR')} DA`
			: 'Enregistrer la consultation'
	);

	// ── Prescription / medications ──
	let medList = $state<string[]>(MEDICAMENTS);
	onMount(async () => {
		try {
			const res = await fetch('/medicaments.json');
			if (!res.ok) return;
			const full = await res.json();
			if (Array.isArray(full) && full.length) medList = full as string[];
		} catch { /* keep starter list */ }
	});

	const FREQUENT_MEDS = ['INSULINE GLARGINE', 'ASPIRINE 100mg', 'METFORMINE 1000mg', 'ATORVASTATINE 20mg', 'PARACÉTAMOL 1g'];

	type Med = { id: number; medication: string; dosage: string; frequency: string; duration: string; quantity: number };
	let medications = $state<Med[]>([]);
	let nextId = $state(1);

	function addMed(medication = '') {
		medications = [...medications, { id: nextId, medication, dosage: '', frequency: '', duration: '', quantity: 1 }];
		nextId++;
	}
	function removeMed(id: number) {
		medications = medications.filter((m) => m.id !== id);
	}
	function updateMed(id: number, field: keyof Omit<Med, 'id'>, val: string | number) {
		medications = medications.map((m) => (m.id === id ? { ...m, [field]: val } : m));
	}

	const prescriptionJson = $derived(
		JSON.stringify(
			medications.map(({ medication, dosage, frequency, duration, quantity }) => ({
				medication, dosage, frequency, duration, quantity
			}))
		)
	);

	// ── Ordonnance overlay ──
	let showOrdonnance = $state(false);
	let searchMed = $state('');

	const filteredMeds = $derived(
		searchMed.trim().length >= 2
			? medList.filter((m) => m.toLowerCase().includes(searchMed.toLowerCase())).slice(0, 60)
			: []
	);

	function parseMedName(full: string): { nom: string; dosage: string; forme: string } {
		const parts = full.trim().split(/\s+/);
		const dosageIdx = parts.findIndex((p) => /\d/.test(p));
		if (dosageIdx <= 0) return { nom: full, dosage: '', forme: '' };
		return {
			nom: parts.slice(0, dosageIdx).join(' '),
			dosage: parts[dosageIdx],
			forme: parts.slice(dosageIdx + 1).join(' ')
		};
	}

	// Six distinct brand-palette colors cycling by first character.
	const CAT_COLORS = ['#0F766E', '#2563EB', '#7C3AED', '#059669', '#D97706', '#DB2777'];
	function medColor(nom: string): string {
		return CAT_COLORS[nom.charCodeAt(0) % CAT_COLORS.length];
	}

	function addMedFromCatalogue(fullName: string) {
		addMed(fullName);
		searchMed = '';
	}

	function openOrdonnance() { showOrdonnance = true; }
	function closeOrdonnance() { showOrdonnance = false; }

	// ── Print preview ──
	let showPrintPreview = $state(false);

	// ── Submit + inline validation ──
	let submitting = $state(false);
	let formEl = $state<HTMLFormElement | null>(null);
	let patientSelectEl = $state<HTMLSelectElement | null>(null);
	let feeInputEl = $state<HTMLInputElement | null>(null);
	let errors = $state<{ patient?: string; fee?: string }>({});

	function validate(): boolean {
		const e: { patient?: string; fee?: string } = {};
		if (!selectedPatientId) e.patient = "Sélectionnez un patient avant d'enregistrer.";
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
		if ((ev.ctrlKey || ev.metaKey) && ev.key === 'Enter') {
			ev.preventDefault();
			handleSave();
			return;
		}
		if (ev.altKey && !ev.ctrlKey && !ev.metaKey) {
			if (ev.key === '1') { ev.preventDefault(); activeTab = 'motif'; }
			else if (ev.key === '2') { ev.preventDefault(); activeTab = 'diag'; }
			else if (ev.key === '3') { ev.preventDefault(); activeTab = 'notes'; }
			else if (ev.key.toLowerCase() === 'n') {
				ev.preventDefault();
				showOrdonnance = true;
				addMed();
			}
			else if (ev.key.toLowerCase() === 'p') { ev.preventDefault(); patientSelectEl?.focus(); }
		}
		// Escape closes overlays.
		if (ev.key === 'Escape') {
			if (showPrintPreview) { showPrintPreview = false; return; }
			if (showOrdonnance) { showOrdonnance = false; return; }
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
			if (result.type === 'failure') {
				const msg = (result.data?.error as string | undefined) ?? "Échec de l’enregistrement de la consultation.";
				toast.error(msg);
			} else if (result.type === 'error') {
				toast.error("Échec de l’enregistrement de la consultation.");
			}
			await update();
		};
	}}
>
	<!-- Hidden fields (always in DOM so they always submit) -->
	<input type="hidden" name="appointmentId" value={appointmentId} />
	<input type="hidden" name="patientId" value={selectedPatientId} />
	<input type="hidden" name="prescription" value={prescriptionJson} />
	<input type="hidden" name="actName" value={selectedActName} />

	<!-- ════════════════════════════════════════════
	     2-COLUMN COCKPIT: patient context + clinical
	     ════════════════════════════════════════════ -->
	<div class="cockpit">
		<!-- ── LEFT RAIL: patient context ── -->
		<aside class="rail rail-left card">
			{#if selectedPatient}
				<div class="patient-head">
					<Avatar nom={selectedPatient.lastName} prenom={selectedPatient.firstName} sexe={selectedPatient.gender} size={56} />
					<h2 class="patient-name">{selectedPatient.firstName} {selectedPatient.lastName}</h2>
					<p class="patient-sub">{selectedPatient.age} ans · {selectedPatient.gender === 'F' ? 'Femme' : 'Homme'}</p>
					<div class="patient-meta">
						{#if ctx.detail?.bloodGroup ?? selectedPatient.bloodGroup}
							<span class="badge-blood">{ctx.detail?.bloodGroup ?? selectedPatient.bloodGroup}</span>
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

						<section class="ctx-block">
							<div class="ctx-title">Dernières consultations</div>
							{#if ctx.recent.length}
								<ul class="recent-list">
									{#each ctx.recent as c}
										<li>
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
				<div class="patient-empty">
					<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:10px">
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

		<!-- ── CENTER: clinical documentation ── -->
		<section class="center">
			<header class="center-head">
				<div>
					<h1 class="center-title">Consultation du {today}</h1>
					<p class="center-sub">Suivi{data.doctorName ? ` · Dr. ${data.doctorName}` : ''}</p>
				</div>
				{#if selectedPatient}
					<a class="btn-outline" href={`/patients/${selectedPatientId}`}>
						<Icon name="fileText" size={14} color="var(--text-muted)" /> Dossier complet
					</a>
				{/if}
			</header>

			{#if form?.error}
				<div class="banner-error">
					<Icon name="alertCircle" size={15} color="var(--danger)" />
					<span>{form.error}</span>
				</div>
			{/if}

			<!-- Constantes vitales: color-coded cards with auto-computed status text -->
			<div class="vitals-head">
				<Icon name="activity" size={14} color="var(--primary)" />
				<span class="vitals-title">Constantes vitales</span>
				<span class="vitals-hint">— statut calculé automatiquement</span>
			</div>
			<div class="vitals-row">
				{#each vitals as v (v.key)}
					{@const st = v.status()}
					<div class="vital-card" style="border-top-color:{STATUS_COLOR[st]}">
						<label class="vital-label" for={`vital-${v.key}`}>{v.label}</label>
						<input
							id={`vital-${v.key}`}
							name={v.name}
							type={v.type ?? 'text'}
							value={v.get()}
							oninput={(e) => v.set((e.target as HTMLInputElement).value)}
							class="vital-input"
							placeholder={v.placeholder}
							autocomplete="off"
						/>
						<span class="vital-footer" style="color:{STATUS_COLOR[st]}">
							{v.unit}{STATUS_LABEL[st] ? ' · ' + STATUS_LABEL[st] : ''}
						</span>
					</div>
				{/each}
			</div>

			<!-- Clinical tabs card -->
			<div class="clinical-card card">
				<div class="tabs" role="tablist">
					<button type="button" class="mk-tab" class:active={activeTab === 'motif'} role="tab" aria-selected={activeTab === 'motif'} onclick={() => (activeTab = 'motif')}>Motif &amp; examen</button>
					<button type="button" class="mk-tab" class:active={activeTab === 'diag'} role="tab" aria-selected={activeTab === 'diag'} onclick={() => (activeTab = 'diag')}>Diagnostic</button>
					<button type="button" class="mk-tab" class:active={activeTab === 'notes'} role="tab" aria-selected={activeTab === 'notes'} onclick={() => (activeTab = 'notes')}>Notes</button>
				</div>

				<!-- Tab panels remain in the DOM so every field still submits -->
				<div class="panel" style:display={activeTab === 'motif' ? '' : 'none'}>
					<div class="motif-chips">
						{#each MOTIFS as m}
							<button type="button" class="chip-add" onclick={() => applyMotif(m)}>+ {m}</button>
						{/each}
					</div>
					<label class="field-label" for="reason">Motif de consultation</label>
					<input id="reason" name="reason" bind:value={chiefComplaint} class="mk-input" placeholder="Motif…" autocomplete="off" />
					<label class="field-label" for="clinicalExam" style="margin-top:12px">Examen clinique</label>
					<textarea id="clinicalExam" name="clinicalExam" bind:value={clinicalNotes} class="mk-input exam-area" placeholder="Observations de l'examen…"></textarea>
				</div>

				<div class="panel" style:display={activeTab === 'diag' ? '' : 'none'}>
					<label class="field-label" for="diagnosis">Diagnostic</label>
					<textarea id="diagnosis" name="diagnosis" bind:value={diagnosis} class="mk-input exam-area" placeholder="Diagnostic principal…"></textarea>
				</div>

				<div class="panel" style:display={activeTab === 'notes' ? '' : 'none'}>
					<label class="field-label" for="notes">Notes complémentaires</label>
					<textarea id="notes" name="notes" bind:value={notes} class="mk-input exam-area" placeholder="Observations supplémentaires…"></textarea>
				</div>
			</div>

			<!-- Ordonnance CTA (empty) / summary (filled) -->
			{#if medications.length === 0}
				<button type="button" class="ord-cta" onclick={openOrdonnance}>
					<Icon name="fileText" size={22} color="var(--primary)" />
					<span class="ord-cta-label">Créer une ordonnance</span>
					<span class="ord-cta-hint">Recherchez et prescrivez des médicaments</span>
				</button>
			{:else}
				<div class="ord-summary">
					<div class="ord-ready">
						<Icon name="check" size={16} color="var(--success)" />
						<span class="ord-ready-label">Ordonnance prête</span>
						<span class="ord-ready-count">{medications.length} médicament{medications.length !== 1 ? 's' : ''}</span>
					</div>
					<button type="button" class="btn-outline ord-edit-btn" onclick={openOrdonnance}>
						Modifier l'ordonnance
					</button>
				</div>
			{/if}
		</section>
	</div>

	<!-- ════════════════════════════════════════════
	     STICKY BOTTOM ACTION BAR: honoraires + save
	     ════════════════════════════════════════════ -->
	<div class="action-bar">
		<div class="honoraires">
			<span class="fee-title-bar"><Icon name="dollar" size={13} color="var(--text-muted)" /> Honoraires</span>
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
					class="mk-input fee-input"
					placeholder="0"
				/>
				<span class="fee-currency">DA</span>
			</div>
			{#if errors.fee}
				<p class="field-error-bar"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.fee}</p>
			{/if}
		</div>

		<button type="button" class="btn-save" disabled={submitting} onclick={handleSave}>
			<Icon name="check" size={16} color="white" />
			{submitting ? 'Enregistrement…' : saveBtnLabel}
			<span class="kbd">⌘↵</span>
		</button>
	</div>

	<!-- ════════════════════════════════════════════
	     ORDONNANCE OVERLAY: full-screen editor
	     ════════════════════════════════════════════ -->
	{#if showOrdonnance}
	<div class="ord-overlay" role="dialog" aria-modal="true" aria-label="Ordonnance">
		<!-- Dark brand header -->
		<div class="ord-header">
			<div class="ord-header-left">
				<Icon name="fileText" size={18} color="white" />
				<h2 class="ord-header-title">Ordonnance</h2>
				<span class="ord-header-count">
					{medications.length} médicament{medications.length !== 1 ? 's' : ''}
				</span>
			</div>
			<div class="ord-header-actions">
				<button
					type="button"
					class="btn-print-ord"
					onclick={() => (showPrintPreview = true)}
					disabled={medications.length === 0}
					title={medications.length === 0 ? 'Ajoutez des médicaments pour imprimer' : 'Aperçu et impression'}
				>
					<Icon name="printer" size={14} color={medications.length > 0 ? '#1C2B3A' : 'rgba(255,255,255,0.4)'} />
					Imprimer l'ordonnance
				</button>
				<button type="button" class="btn-close-ord" onclick={closeOrdonnance} aria-label="Fermer">
					Fermer
				</button>
			</div>
		</div>

		<!-- Two-panel body: catalogue (left) + prescription editor (right) -->
		<div class="ord-body">
			<!-- LEFT: medication search catalogue -->
			<div class="ord-catalogue">
				<div class="cat-search-wrap">
					<Icon name="search" size={16} color="var(--text-muted)" />
					<input
						class="cat-search"
						type="search"
						bind:value={searchMed}
						placeholder="Rechercher un médicament…"
						autocomplete="off"
					/>
				</div>

				<div class="cat-list">
					{#if filteredMeds.length > 0}
						{#each filteredMeds as medName (medName)}
							{@const { nom, dosage, forme } = parseMedName(medName)}
							{@const color = medColor(nom)}
							<button
								type="button"
								class="med-cat-row"
								data-testid="med-catalogue-row"
								onclick={() => addMedFromCatalogue(medName)}
								title="Prescrire {medName}"
							>
								<span class="med-cat-badge" style="background:{color}20;color:{color}">
									{nom[0]}
								</span>
								<div class="med-cat-info">
									<span class="med-cat-nom">{nom}</span>
									{#if dosage || forme}
										<span class="med-cat-meta">{[dosage, forme].filter(Boolean).join(' · ')}</span>
									{/if}
								</div>
								<Icon name="plus" size={14} color="var(--text-light)" />
							</button>
						{/each}
					{:else if searchMed.length >= 2}
						<p class="cat-empty">Aucun résultat pour « {searchMed} »</p>
					{:else if searchMed.length > 0}
						<p class="cat-hint">Continuez à taper…</p>
					{:else}
						<div class="cat-placeholder">
							<Icon name="search" size={32} color="var(--border-strong)" />
							<p>Recherchez dans les {medList.length.toLocaleString('fr-FR')} médicaments du catalogue national</p>
						</div>
					{/if}
				</div>
			</div>

			<!-- RIGHT: prescription editor -->
			<div class="ord-prescription">
				<div class="rx-toolbar">
					<button type="button" class="btn-add-med" onclick={() => addMed()}>
						<Icon name="plus" size={14} color="var(--primary)" /> Ajouter un médicament
					</button>
					<div class="freq-block">
						<span class="freq-title">Fréquents <span>— clic pour ajouter</span></span>
						<div class="chips">
							{#each FREQUENT_MEDS as fm}
								<button type="button" class="chip-add" onclick={() => addMed(fm)}>+ {fm}</button>
							{/each}
						</div>
					</div>
				</div>

				<div class="rx-list">
					{#if medications.length === 0}
						<div class="ord-empty">
							<Icon name="fileText" size={32} color="var(--border-strong)" />
							<p>Aucun médicament</p>
							<span>Recherchez dans le catalogue ou cliquez sur « Ajouter »</span>
						</div>
					{:else}
						{#each medications as med, i (med.id)}
							{@const posoHint = expandPosology(med.dosage)}
							<div class="med-card">
								<div class="med-card-head">
									<span class="med-card-no">Médicament {i + 1}</span>
									<button type="button" class="med-remove" onclick={() => removeMed(med.id)} aria-label="Retirer le médicament">
										<Icon name="trash" size={14} color="var(--danger)" />
									</button>
								</div>
								<Combobox
									value={med.medication}
									options={medList}
									placeholder="Nom du médicament"
									onInput={(v) => updateMed(med.id, 'medication', v)}
									style="width:100%;min-width:0"
								/>
								<input
									value={med.dosage}
									oninput={(e) => updateMed(med.id, 'dosage', (e.target as HTMLInputElement).value)}
									onblur={() => { const x = expandPosology(med.dosage); if (x) updateMed(med.id, 'dosage', x); }}
									class="mk-input med-field"
									placeholder="1-0-1-0 ou texte libre"
									title="Posologie — tapez 1-0-1-0 (matin-midi-soir-coucher) ou du texte libre"
								/>
								{#if posoHint && posoHint !== med.dosage}
									<div class="poso-hint" title={posoHint}>= {posoHint}</div>
								{/if}
								<div class="med-row">
									<input
										value={med.duration}
										oninput={(e) => updateMed(med.id, 'duration', (e.target as HTMLInputElement).value)}
										class="mk-input med-field"
										placeholder="Durée (ex. 1 mois)"
										title="Durée"
									/>
									<input
										type="number"
										min="1"
										value={med.quantity}
										oninput={(e) => updateMed(med.id, 'quantity', Number((e.target as HTMLInputElement).value))}
										class="mk-input med-field med-qty"
										placeholder="Qté"
										title="Quantité (boîtes)"
									/>
								</div>
							</div>
						{/each}
					{/if}
				</div>
			</div>
		</div>
	</div>
	{/if}

	<!-- ════════════════════════════════════════════
	     PRINT PREVIEW MODAL
	     ════════════════════════════════════════════ -->
	{#if showPrintPreview}
	<div class="print-modal" role="dialog" aria-modal="true" aria-label="Aperçu d'impression">
		<div class="print-modal-doc" id="print-ordonnance">
			<div class="pm-header">
				<div class="pm-title-block">
					<h2 class="pm-title">Ordonnance médicale</h2>
					<p class="pm-date">Le {today}</p>
				</div>
				{#if data.doctorName}
					<div class="pm-doctor">Dr. {data.doctorName}</div>
				{/if}
			</div>

			{#if selectedPatient}
				<div class="pm-patient">
					<span class="pm-patient-label">Patient :</span>
					<span class="pm-patient-name">{selectedPatient.firstName} {selectedPatient.lastName}</span>
					<span class="pm-patient-age">{selectedPatient.age} ans</span>
				</div>
			{/if}

			<div class="pm-meds">
				{#each medications as med, i}
					<div class="pm-med">
						<div class="pm-med-no">{i + 1}.</div>
						<div class="pm-med-body">
							<div class="pm-med-name">{med.medication}</div>
							{#if med.dosage}<div class="pm-med-line">Posologie : {med.dosage}</div>{/if}
							{#if med.duration}<div class="pm-med-line">Durée : {med.duration}</div>{/if}
							<div class="pm-med-line">Quantité : {med.quantity} boîte{med.quantity > 1 ? 's' : ''}</div>
						</div>
					</div>
				{/each}
			</div>

			<div class="pm-sig">
				<div class="pm-sig-line"></div>
				<div class="pm-sig-label">Cachet et signature du médecin</div>
			</div>
		</div>

		<div class="print-modal-actions">
			<button type="button" class="btn-do-print" onclick={() => { showPrintPreview = false; window.print(); }}>
				<Icon name="printer" size={15} color="white" /> Imprimer
			</button>
			<button type="button" class="btn-close-print" onclick={() => (showPrintPreview = false)}>
				Fermer l'aperçu
			</button>
		</div>
	</div>
	{/if}

	<!-- Print-only div: shown only during window.print() via @media print CSS -->
	<div class="print-only" aria-hidden="true">
		<div class="po-header">
			<h1 class="po-title">Ordonnance médicale</h1>
			{#if data.doctorName}<p class="po-doctor">Dr. {data.doctorName}</p>{/if}
			<p class="po-date">Le {today}</p>
		</div>
		{#if selectedPatient}
			<p class="po-patient">Patient : <strong>{selectedPatient.firstName} {selectedPatient.lastName}</strong>, {selectedPatient.age} ans</p>
		{/if}
		{#each medications as med, i}
			<div class="po-med">
				<p class="po-med-name">{i + 1}. {med.medication}</p>
				{#if med.dosage}<p class="po-med-detail">Posologie : {med.dosage}</p>{/if}
				{#if med.duration}<p class="po-med-detail">Durée : {med.duration}</p>{/if}
				<p class="po-med-detail">Quantité : {med.quantity} boîte{med.quantity > 1 ? 's' : ''}</p>
			</div>
		{/each}
		<div class="po-sig">
			<hr class="po-sig-line" />
			<p class="po-sig-label">Cachet et signature</p>
		</div>
	</div>
</form>

<style>
	/* ═══════════════════════════════════════
	   LAYOUT: 2-column cockpit
	   ═══════════════════════════════════════ */
	.cockpit {
		height: calc(100vh - 58px - 76px); /* nav + action bar */
		display: grid;
		grid-template-columns: 248px minmax(0, 1fr);
		gap: 14px;
		padding: 14px 14px 0;
		background: var(--bg);
		overflow: hidden;
	}

	/* ═══════════════════════════════════════
	   LEFT RAIL
	   ═══════════════════════════════════════ */
	.rail {
		display: flex;
		flex-direction: column;
		min-height: 0;
		overflow: hidden;
	}
	.rail-left { padding: 16px 14px; }
	.rail-scroll { overflow-y: auto; min-height: 0; flex: 1; }

	.patient-head { display: flex; flex-direction: column; align-items: center; text-align: center; padding-bottom: 12px; }
	.patient-name { font-size: 15.5px; font-weight: 700; margin-top: 9px; line-height: 1.25; }
	.patient-sub { font-size: 12.5px; color: var(--text-muted); margin-top: 2px; }
	.patient-meta { display: flex; align-items: center; justify-content: center; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
	.badge-blood { background: var(--danger-light); color: var(--danger); font-size: 11.5px; font-weight: 700; padding: 2px 9px; border-radius: 20px; }
	.patient-phone { font-size: 11.5px; color: var(--text-muted); display: inline-flex; align-items: center; gap: 4px; }
	.patient-switch { font-size: 12.5px; padding: 6px 8px; margin-bottom: 12px; }
	.patient-empty { padding: 2px; }

	.ctx-block { padding: 12px 0; border-top: 1px solid var(--border); }
	.ctx-block:first-child { border-top: none; padding-top: 4px; }
	.ctx-title { font-size: 11px; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; display: flex; align-items: center; gap: 5px; margin-bottom: 8px; }
	.ctx-title-danger { color: var(--danger); }
	.chips { display: flex; flex-wrap: wrap; gap: 6px; }
	.chip-allergy { background: var(--danger-light); color: var(--danger); font-size: 12px; font-weight: 600; padding: 3px 10px; border-radius: 20px; }
	.ctx-list { list-style: none; display: flex; flex-direction: column; gap: 6px; }
	.ctx-list li { font-size: 12.5px; color: var(--text); padding-left: 14px; position: relative; }
	.ctx-list li::before { content: ''; position: absolute; left: 2px; top: 7px; width: 5px; height: 5px; border-radius: 50%; background: var(--primary); }
	.recent-list { list-style: none; display: flex; flex-direction: column; gap: 9px; }
	.recent-list li { display: flex; flex-direction: column; gap: 1px; }
	.recent-date { font-size: 11.5px; font-weight: 600; color: var(--primary); }
	.recent-reason { font-size: 12px; color: var(--text-muted); line-height: 1.35; }
	.muted-note { font-size: 12px; color: var(--text-light); }

	/* ═══════════════════════════════════════
	   CENTER: clinical
	   ═══════════════════════════════════════ */
	.center { display: flex; flex-direction: column; min-height: 0; overflow-y: auto; gap: 14px; padding-bottom: 14px; }

	.center-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }
	.center-title { font-size: 19px; font-weight: 700; line-height: 1.2; }
	.center-sub { font-size: 12.5px; color: var(--text-muted); margin-top: 3px; }

	.banner-error { display: flex; align-items: center; gap: 8px; padding: 10px 14px; background: var(--danger-light); border: 1px solid #FECACA; border-radius: 8px; }
	.banner-error span { font-size: 13px; color: var(--danger); font-weight: 500; }

	/* Vitals */
	.vitals-head { display: flex; align-items: center; gap: 6px; }
	.vitals-title { font-size: 13.5px; font-weight: 600; }
	.vitals-hint { font-size: 11.5px; color: var(--text-light); }
	.vitals-row { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 10px; }
	.vital-card { background: var(--surface); border: 1px solid var(--border); border-top: 3px solid var(--border-strong); border-radius: 9px; padding: 9px 10px 10px; display: flex; flex-direction: column; transition: border-top-color 0.2s; }
	.vital-label { font-size: 11px; font-weight: 600; color: var(--text-muted); margin-bottom: 4px; }
	.vital-input { border: none; outline: none; background: transparent; font-family: inherit; font-size: 18px; font-weight: 600; color: var(--text); width: 100%; padding: 0; }
	.vital-input::placeholder { color: var(--text-light); font-weight: 500; }
	.vital-footer { font-size: 10.5px; margin-top: 3px; font-weight: 500; transition: color 0.2s; }

	/* Clinical tabs card */
	.clinical-card { padding: 0; overflow: hidden; }
	.tabs { display: flex; gap: 2px; border-bottom: 1px solid var(--border); padding: 0 14px; }
	.tabs .mk-tab { padding: 10px 14px; font-size: 13.5px; }
	.panel { display: flex; flex-direction: column; padding: 14px; }
	.motif-chips { display: flex; flex-wrap: wrap; gap: 7px; margin-bottom: 14px; }
	.field-label { font-size: 12.5px; color: var(--text-muted); margin-bottom: 5px; }
	.exam-area { min-height: 110px; resize: vertical; }

	.chip-add { background: var(--surface); border: 1px solid var(--border); color: var(--text); font-family: inherit; font-size: 12px; font-weight: 500; padding: 5px 11px; border-radius: 20px; cursor: pointer; transition: all 0.12s; white-space: nowrap; }
	.chip-add:hover { border-color: var(--primary); color: var(--primary); background: var(--primary-50); }

	/* Ordonnance CTA / summary */
	.ord-cta { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; width: 100%; padding: 28px 20px; background: var(--primary-50); border: 2px dashed var(--primary-light); border-radius: 12px; cursor: pointer; text-align: center; transition: all 0.15s; }
	.ord-cta:hover { background: var(--primary-light); border-color: var(--primary); }
	.ord-cta-label { font-size: 15px; font-weight: 700; color: var(--primary); }
	.ord-cta-hint { font-size: 12px; color: var(--text-muted); }

	.ord-summary { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 14px 16px; background: #ECFDF5; border: 1px solid #6EE7B7; border-radius: 10px; }
	.ord-ready { display: flex; align-items: center; gap: 8px; }
	.ord-ready-label { font-size: 14px; font-weight: 700; color: var(--success); }
	.ord-ready-count { font-size: 12.5px; color: var(--text-muted); background: white; border: 1px solid var(--border); border-radius: 20px; padding: 2px 10px; }
	.ord-edit-btn { font-size: 12.5px; padding: 7px 14px; }

	/* ═══════════════════════════════════════
	   STICKY BOTTOM ACTION BAR
	   ═══════════════════════════════════════ */
	.action-bar {
		position: fixed;
		bottom: 0;
		left: 0;
		right: 0;
		height: 76px;
		z-index: 150;
		background: var(--surface);
		border-top: 1px solid var(--border);
		display: flex;
		align-items: center;
		gap: 16px;
		padding: 0 20px;
		box-shadow: 0 -2px 8px rgba(0,0,0,0.06);
	}

	.honoraires { display: flex; align-items: center; gap: 10px; flex: 1; min-width: 0; }
	.fee-title-bar { display: flex; align-items: center; gap: 5px; font-size: 11px; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.4px; white-space: nowrap; }
	.fee-chips { flex-wrap: nowrap; overflow-x: auto; }
	.fee-chip { background: var(--surface); border: 1px solid var(--border); color: var(--text); font-family: inherit; font-size: 12.5px; font-weight: 600; padding: 6px 13px; border-radius: 20px; cursor: pointer; transition: all 0.12s; white-space: nowrap; }
	.fee-chip:hover { border-color: var(--primary); }
	.fee-chip.active { background: var(--primary); border-color: var(--primary); color: white; }
	.fee-amount { display: flex; align-items: center; gap: 6px; }
	.fee-input { width: 88px; text-align: right; font-weight: 700; font-size: 14px; padding: 6px 8px; }
	.fee-currency { font-size: 12px; color: var(--text-muted); font-weight: 600; white-space: nowrap; }
	.field-error-bar { display: flex; align-items: center; gap: 5px; font-size: 11.5px; color: var(--danger); font-weight: 500; white-space: nowrap; }

	.btn-save { display: flex; align-items: center; gap: 8px; padding: 13px 22px; background: var(--success); color: white; border: none; border-radius: 9px; font-family: inherit; font-size: 14px; font-weight: 600; cursor: pointer; white-space: nowrap; flex-shrink: 0; }
	.btn-save:hover:not(:disabled) { background: #047857; }
	.btn-save:disabled { opacity: 0.6; cursor: default; }
	.kbd { font-size: 11px; font-weight: 600; opacity: 0.75; background: rgba(255,255,255,0.18); padding: 1px 6px; border-radius: 5px; }

	/* ═══════════════════════════════════════
	   ORDONNANCE OVERLAY (full-screen)
	   ═══════════════════════════════════════ */
	.ord-overlay {
		position: fixed;
		top: 58px; /* below nav */
		left: 0;
		right: 0;
		bottom: 0;
		z-index: 180;
		background: var(--bg);
		display: flex;
		flex-direction: column;
	}

	.ord-header {
		height: 64px;
		flex-shrink: 0;
		background: var(--nav-bg);
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 20px;
		gap: 16px;
	}
	.ord-header-left { display: flex; align-items: center; gap: 10px; }
	.ord-header-title { font-size: 16px; font-weight: 700; color: white; }
	.ord-header-count { font-size: 12.5px; color: rgba(255,255,255,0.6); background: rgba(255,255,255,0.12); padding: 3px 10px; border-radius: 20px; }
	.ord-header-actions { display: flex; align-items: center; gap: 10px; }

	.btn-print-ord {
		display: inline-flex;
		align-items: center;
		gap: 7px;
		background: var(--accent);
		border: none;
		border-radius: 8px;
		padding: 9px 18px;
		font-family: inherit;
		font-size: 13px;
		font-weight: 700;
		color: #1C2B3A;
		cursor: pointer;
		transition: background 0.15s;
	}
	.btn-print-ord:hover:not(:disabled) { background: #B45309; color: white; }
	.btn-print-ord:disabled { opacity: 0.4; cursor: default; background: rgba(255,255,255,0.15); color: rgba(255,255,255,0.5); }

	.btn-close-ord {
		background: rgba(255,255,255,0.12);
		border: 1px solid rgba(255,255,255,0.2);
		border-radius: 7px;
		padding: 8px 16px;
		font-family: inherit;
		font-size: 13px;
		font-weight: 600;
		color: white;
		cursor: pointer;
		transition: background 0.15s;
	}
	.btn-close-ord:hover { background: rgba(255,255,255,0.2); }

	.ord-body {
		flex: 1;
		display: grid;
		grid-template-columns: 380px 1fr;
		min-height: 0;
		overflow: hidden;
	}

	/* Catalogue panel */
	.ord-catalogue {
		border-right: 1px solid var(--border);
		display: flex;
		flex-direction: column;
		overflow: hidden;
		background: var(--surface);
	}
	.cat-search-wrap {
		display: flex;
		align-items: center;
		gap: 8px;
		padding: 14px 16px;
		border-bottom: 1px solid var(--border);
		flex-shrink: 0;
	}
	.cat-search {
		flex: 1;
		border: none;
		outline: none;
		font-family: inherit;
		font-size: 14px;
		color: var(--text);
		background: transparent;
	}
	.cat-search::placeholder { color: var(--text-light); }
	.cat-list { flex: 1; overflow-y: auto; padding: 8px 0; }

	.med-cat-row {
		display: flex;
		align-items: center;
		gap: 10px;
		width: 100%;
		padding: 9px 16px;
		background: none;
		border: none;
		cursor: pointer;
		text-align: left;
		transition: background 0.1s;
	}
	.med-cat-row:hover { background: var(--primary-50); }
	.med-cat-badge {
		width: 30px;
		height: 30px;
		border-radius: 8px;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 13px;
		font-weight: 800;
		flex-shrink: 0;
	}
	.med-cat-info { flex: 1; min-width: 0; display: flex; flex-direction: column; }
	.med-cat-nom { font-size: 13px; font-weight: 600; color: var(--text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
	.med-cat-meta { font-size: 11.5px; color: var(--text-muted); }

	.cat-placeholder {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 10px;
		padding: 48px 24px;
		text-align: center;
		color: var(--text-muted);
		font-size: 13px;
	}
	.cat-empty { font-size: 12.5px; color: var(--text-muted); padding: 24px 16px; }
	.cat-hint { font-size: 12px; color: var(--text-light); padding: 12px 16px; }

	/* Prescription panel */
	.ord-prescription {
		display: flex;
		flex-direction: column;
		overflow: hidden;
		background: var(--bg);
	}
	.rx-toolbar {
		padding: 14px 20px;
		border-bottom: 1px solid var(--border);
		background: var(--surface);
		flex-shrink: 0;
		display: flex;
		align-items: flex-start;
		gap: 16px;
	}
	.rx-list { flex: 1; overflow-y: auto; padding: 16px 20px; display: flex; flex-direction: column; gap: 10px; }

	.btn-add-med { display: inline-flex; align-items: center; gap: 7px; padding: 8px 14px; background: var(--primary-50); border: 1px dashed var(--primary-light); border-radius: 8px; font-family: inherit; font-size: 13px; font-weight: 600; color: var(--primary); cursor: pointer; white-space: nowrap; flex-shrink: 0; }
	.btn-add-med:hover { background: var(--primary-light); }

	.freq-block { flex: 1; min-width: 0; }
	.freq-title { font-size: 10.5px; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; display: block; margin-bottom: 7px; }
	.freq-title span { font-weight: 500; text-transform: none; letter-spacing: 0; color: var(--text-light); }

	.ord-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; text-align: center; padding: 48px 20px; color: var(--text-muted); }
	.ord-empty p { font-size: 13px; font-weight: 500; }
	.ord-empty span { font-size: 11.5px; color: var(--text-light); }

	.med-card { border: 1px solid var(--border); border-left: 3px solid var(--primary); border-radius: 9px; padding: 11px; display: flex; flex-direction: column; gap: 7px; background: var(--surface); }
	.med-card-head { display: flex; align-items: center; justify-content: space-between; }
	.med-card-no { font-size: 10.5px; font-weight: 700; color: var(--primary); text-transform: uppercase; letter-spacing: 0.5px; }
	.med-remove { background: none; border: none; cursor: pointer; display: flex; padding: 2px; }
	.med-field { font-size: 13px; padding: 7px 9px; }
	.med-row { display: grid; grid-template-columns: 1fr 64px; gap: 7px; }
	.med-qty { text-align: center; }
	.poso-hint { font-size: 10.5px; color: var(--primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: -2px; }

	/* ═══════════════════════════════════════
	   PRINT PREVIEW MODAL
	   ═══════════════════════════════════════ */
	.print-modal {
		position: fixed;
		inset: 0;
		z-index: 300;
		background: rgba(15, 23, 42, 0.75);
		display: flex;
		align-items: flex-start;
		justify-content: center;
		overflow-y: auto;
		padding: 40px 20px;
	}
	.print-modal-doc {
		background: white;
		border-radius: 12px;
		padding: 40px;
		width: 100%;
		max-width: 600px;
		font-family: Georgia, serif;
		box-shadow: 0 20px 60px rgba(0,0,0,0.3);
	}
	.pm-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; border-bottom: 2px solid var(--nav-bg); padding-bottom: 16px; }
	.pm-title { font-size: 22px; font-weight: 700; color: var(--nav-bg); font-family: Georgia, serif; }
	.pm-date { font-size: 13px; color: var(--text-muted); margin-top: 4px; font-family: inherit; }
	.pm-doctor { font-size: 14px; font-weight: 700; color: var(--nav-bg); }
	.pm-patient { margin-bottom: 24px; padding: 10px 14px; background: #F8F8F8; border-radius: 6px; display: flex; align-items: center; gap: 8px; font-size: 13px; }
	.pm-patient-label { color: var(--text-muted); }
	.pm-patient-name { font-weight: 700; }
	.pm-patient-age { color: var(--text-muted); font-size: 12px; }
	.pm-meds { display: flex; flex-direction: column; gap: 16px; margin-bottom: 32px; }
	.pm-med { display: flex; gap: 12px; }
	.pm-med-no { font-size: 15px; font-weight: 700; color: var(--nav-bg); min-width: 20px; }
	.pm-med-body { display: flex; flex-direction: column; gap: 2px; }
	.pm-med-name { font-size: 15px; font-weight: 700; font-style: italic; }
	.pm-med-line { font-size: 13px; color: var(--text-muted); }
	.pm-sig { border-top: 1px solid #ddd; padding-top: 32px; text-align: right; }
	.pm-sig-line { height: 40px; border-bottom: 1px solid #aaa; margin-bottom: 8px; }
	.pm-sig-label { font-size: 12px; color: var(--text-muted); }

	.print-modal-actions {
		max-width: 600px;
		width: 100%;
		display: flex;
		gap: 12px;
		margin-top: 20px;
	}
	.btn-do-print { display: flex; align-items: center; gap: 8px; padding: 12px 24px; background: var(--accent); color: white; border: none; border-radius: 8px; font-family: inherit; font-size: 14px; font-weight: 700; cursor: pointer; }
	.btn-do-print:hover { background: #B45309; }
	.btn-close-print { padding: 12px 20px; background: var(--surface); border: 1px solid var(--border); border-radius: 8px; font-family: inherit; font-size: 14px; color: var(--text-muted); cursor: pointer; }
	.btn-close-print:hover { border-color: var(--text-muted); }

	/* ═══════════════════════════════════════
	   SHARED UTILITIES
	   ═══════════════════════════════════════ */
	.btn-ghost { display: inline-flex; align-items: center; gap: 4px; background: var(--primary-50); border: 1px solid var(--primary-light); border-radius: 6px; padding: 4px 9px; font-family: inherit; font-size: 11.5px; font-weight: 600; color: var(--primary); cursor: pointer; }
	.btn-outline { display: inline-flex; align-items: center; gap: 6px; background: var(--surface); border: 1px solid var(--border); border-radius: 7px; padding: 7px 12px; font-family: inherit; font-size: 12.5px; font-weight: 500; color: var(--text-muted); text-decoration: none; white-space: nowrap; cursor: pointer; }
	.btn-outline:hover { border-color: var(--primary); color: var(--primary); }
	.field-error { display: flex; align-items: center; gap: 5px; font-size: 11.5px; color: var(--danger); font-weight: 500; margin-top: 7px; }

	/* ═══════════════════════════════════════
	   PRINT-ONLY CONTENT (@media print)
	   ═══════════════════════════════════════ */
	.print-only { display: none; }

	@media print {
		:global(nav), :global(.mk-nav), .cockpit, .action-bar, .ord-overlay, .print-modal { display: none !important; }
		.print-only {
			display: block !important;
			font-family: Georgia, serif;
			padding: 32px;
		}
		.po-header { margin-bottom: 24px; border-bottom: 2px solid #1C2B3A; padding-bottom: 12px; }
		.po-title { font-size: 22px; font-weight: 700; color: #1C2B3A; margin-bottom: 4px; }
		.po-doctor { font-size: 14px; font-weight: 700; }
		.po-date { font-size: 13px; color: #666; }
		.po-patient { margin-bottom: 20px; font-size: 13px; }
		.po-med { margin-bottom: 16px; }
		.po-med-name { font-size: 15px; font-weight: 700; font-style: italic; margin-bottom: 3px; }
		.po-med-detail { font-size: 13px; color: #555; margin-left: 16px; }
		.po-sig { margin-top: 40px; text-align: right; }
		.po-sig-line { border: none; border-top: 1px solid #aaa; margin-bottom: 8px; }
		.po-sig-label { font-size: 12px; color: #666; }
	}

	/* ═══════════════════════════════════════
	   RESPONSIVE
	   ═══════════════════════════════════════ */
	@media (max-width: 1180px) {
		.cockpit { grid-template-columns: 224px minmax(0, 1fr); gap: 10px; padding: 10px 10px 0; }
		.vitals-row { grid-template-columns: repeat(3, minmax(0, 1fr)); }
		.ord-body { grid-template-columns: 300px 1fr; }
	}
	@media (max-width: 920px) {
		.cockpit { grid-template-columns: 1fr; height: auto; overflow: visible; }
		.rail { overflow: visible; }
		.action-bar { flex-wrap: wrap; height: auto; padding: 12px 16px; gap: 10px; }
		.honoraires { flex-wrap: wrap; }
		.btn-save { width: 100%; justify-content: center; }
		.ord-body { grid-template-columns: 1fr; }
		.ord-catalogue { border-right: none; border-bottom: 1px solid var(--border); max-height: 40vh; }
	}
</style>
