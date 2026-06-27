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

	// Fréquents — one click adds a pre-filled medication line.
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
			else if (ev.key.toLowerCase() === 'n') { ev.preventDefault(); addMed(); }
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
		return async ({ update }) => {
			submitting = false;
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
		<!-- ════════ LEFT RAIL — patient context ════════ -->
		<aside class="rail rail-left card">
			{#if selectedPatient}
				<div class="patient-head">
					<Avatar nom={selectedPatient.lastName} prenom={selectedPatient.firstName} sexe={selectedPatient.gender} size={56} />
					<h2 class="patient-name">{selectedPatient.firstName} {selectedPatient.lastName}</h2>
					<p class="patient-sub">
						{selectedPatient.age} ans · {selectedPatient.gender === 'F' ? 'Femme' : 'Homme'}
					</p>
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
				<!-- No patient yet -->
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

		<!-- ════════ CENTER — clinical ════════ -->
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

			<!-- Constantes vitales -->
			<div class="vitals-head">
				<Icon name="activity" size={14} color="var(--primary)" />
				<span class="vitals-title">Constantes vitales</span>
				<span class="vitals-hint">— saisie directe, statut calculé automatiquement</span>
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
						<span class="vital-unit">{v.unit}</span>
					</div>
				{/each}
			</div>

			<!-- Tabs -->
			<div class="tabs" role="tablist">
				<button type="button" class="mk-tab" class:active={activeTab === 'motif'} role="tab" aria-selected={activeTab === 'motif'} onclick={() => (activeTab = 'motif')}>Motif &amp; examen</button>
				<button type="button" class="mk-tab" class:active={activeTab === 'diag'} role="tab" aria-selected={activeTab === 'diag'} onclick={() => (activeTab = 'diag')}>Diagnostic</button>
				<button type="button" class="mk-tab" class:active={activeTab === 'notes'} role="tab" aria-selected={activeTab === 'notes'} onclick={() => (activeTab = 'notes')}>Notes</button>
			</div>

			<!-- Tab panels: hidden (not removed) so every field still submits -->
			<div class="panel" style:display={activeTab === 'motif' ? '' : 'none'}>
				<div class="motif-chips">
					{#each MOTIFS as m}
						<button type="button" class="chip-add" onclick={() => applyMotif(m)}>+ {m}</button>
					{/each}
				</div>
				<label class="field-label" for="reason">Motif de consultation</label>
				<input id="reason" name="reason" bind:value={chiefComplaint} class="mk-input" placeholder="Motif…" autocomplete="off" />
				<label class="field-label" for="clinicalExam" style="margin-top:12px">Examen clinique</label>
				<textarea id="clinicalExam" name="clinicalExam" bind:value={clinicalNotes} class="mk-input exam-area" placeholder="Observations de l’examen…"></textarea>
			</div>

			<div class="panel" style:display={activeTab === 'diag' ? '' : 'none'}>
				<label class="field-label" for="diagnosis">Diagnostic</label>
				<textarea id="diagnosis" name="diagnosis" bind:value={diagnosis} class="mk-input exam-area" placeholder="Diagnostic principal…"></textarea>
			</div>

			<div class="panel" style:display={activeTab === 'notes' ? '' : 'none'}>
				<label class="field-label" for="notes">Notes complémentaires</label>
				<textarea id="notes" name="notes" bind:value={notes} class="mk-input exam-area" placeholder="Observations supplémentaires…"></textarea>
			</div>
		</section>

		<!-- ════════ RIGHT RAIL — ordonnance + honoraires ════════ -->
		<aside class="rail rail-right card">
			<div class="ord-head">
				<div>
					<h2 class="ord-title">Ordonnance</h2>
					<p class="ord-count">{medications.length} médicament{medications.length !== 1 ? 's' : ''}</p>
				</div>
				<button type="button" class="btn-print" disabled title="Disponible après l’enregistrement, depuis le dossier patient">
					<Icon name="printer" size={13} color="var(--text-muted)" /> Imprimer
				</button>
			</div>

			<div class="rail-scroll">
				<button type="button" class="btn-add-med" onclick={() => addMed()}>
					<Icon name="plus" size={14} color="var(--primary)" /> Ajouter un médicament
				</button>

				<div class="freq-block">
					<div class="freq-title">Fréquents <span>— clic pour ajouter</span></div>
					<div class="chips">
						{#each FREQUENT_MEDS as fm}
							<button type="button" class="chip-add" onclick={() => addMed(fm)}>+ {fm}</button>
						{/each}
					</div>
				</div>

				{#if medications.length === 0}
					<div class="ord-empty">
						<Icon name="fileText" size={30} color="var(--border-strong)" />
						<p>Aucun médicament</p>
						<span>Cliquez sur « Ajouter » ou un médicament fréquent</span>
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

			<!-- Honoraires -->
			<div class="fee-block">
				<div class="fee-title"><Icon name="dollar" size={13} color="var(--text-muted)" /> Honoraires de consultation</div>
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
					<span class="fee-amount-label">Montant</span>
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
					<p class="field-error"><Icon name="alertCircle" size={12} color="var(--danger)" /> {errors.fee}</p>
				{/if}
			</div>

			<button type="button" class="btn-save" disabled={submitting} onclick={handleSave}>
				<Icon name="check" size={16} color="white" />
				{submitting ? 'Enregistrement…' : 'Enregistrer la consultation'}
				<span class="kbd">⌘↵</span>
			</button>
		</aside>
	</div>
</form>

<style>
	.cockpit {
		height: calc(100vh - 58px);
		display: grid;
		grid-template-columns: 248px minmax(0, 1fr) 312px;
		gap: 14px;
		padding: 14px;
		background: var(--bg);
		overflow: hidden;
	}

	/* ── Rails ── */
	.rail {
		display: flex;
		flex-direction: column;
		min-height: 0;
		overflow: hidden;
	}
	.rail-left { padding: 16px 14px; }
	.rail-right { padding: 0; }
	.rail-scroll { overflow-y: auto; min-height: 0; flex: 1; }

	/* ── Patient head ── */
	.patient-head { display: flex; flex-direction: column; align-items: center; text-align: center; padding-bottom: 12px; }
	.patient-name { font-size: 15.5px; font-weight: 700; margin-top: 9px; line-height: 1.25; }
	.patient-sub { font-size: 12.5px; color: var(--text-muted); margin-top: 2px; }
	.patient-meta { display: flex; align-items: center; justify-content: center; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
	.badge-blood { background: var(--danger-light); color: var(--danger); font-size: 11.5px; font-weight: 700; padding: 2px 9px; border-radius: 20px; }
	.patient-phone { font-size: 11.5px; color: var(--text-muted); display: inline-flex; align-items: center; gap: 4px; }
	.patient-switch { font-size: 12.5px; padding: 6px 8px; margin-bottom: 12px; }
	.patient-empty { padding: 2px; }

	/* ── Context blocks ── */
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

	/* ── Center ── */
	.center { display: flex; flex-direction: column; min-height: 0; overflow-y: auto; padding: 2px; }
	.center-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; margin-bottom: 14px; }
	.center-title { font-size: 19px; font-weight: 700; line-height: 1.2; }
	.center-sub { font-size: 12.5px; color: var(--text-muted); margin-top: 3px; }

	.banner-error { display: flex; align-items: center; gap: 8px; padding: 10px 14px; background: var(--danger-light); border: 1px solid #FECACA; border-radius: 8px; margin-bottom: 14px; }
	.banner-error span { font-size: 13px; color: var(--danger); font-weight: 500; }

	.vitals-head { display: flex; align-items: center; gap: 6px; margin-bottom: 10px; }
	.vitals-title { font-size: 13.5px; font-weight: 600; }
	.vitals-hint { font-size: 11.5px; color: var(--text-light); }
	.vitals-row { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 10px; margin-bottom: 18px; }
	.vital-card { background: var(--surface); border: 1px solid var(--border); border-top: 3px solid var(--border-strong); border-radius: 9px; padding: 9px 10px 10px; display: flex; flex-direction: column; transition: border-top-color 0.2s; }
	.vital-label { font-size: 11px; font-weight: 600; color: var(--text-muted); margin-bottom: 4px; }
	.vital-input { border: none; outline: none; background: transparent; font-family: inherit; font-size: 18px; font-weight: 600; color: var(--text); width: 100%; padding: 0; }
	.vital-input::placeholder { color: var(--text-light); font-weight: 500; }
	.vital-unit { font-size: 10.5px; color: var(--text-light); margin-top: 2px; }

	.tabs { display: flex; gap: 2px; border-bottom: 1px solid var(--border); margin-bottom: 14px; }
	.tabs .mk-tab { padding: 8px 14px; font-size: 13.5px; }
	.panel { display: flex; flex-direction: column; }
	.motif-chips { display: flex; flex-wrap: wrap; gap: 7px; margin-bottom: 14px; }
	.field-label { font-size: 12.5px; color: var(--text-muted); margin-bottom: 5px; }
	.exam-area { min-height: 120px; resize: vertical; }

	.chip-add { background: var(--surface); border: 1px solid var(--border); color: var(--text); font-family: inherit; font-size: 12px; font-weight: 500; padding: 5px 11px; border-radius: 20px; cursor: pointer; transition: all 0.12s; white-space: nowrap; }
	.chip-add:hover { border-color: var(--primary); color: var(--primary); background: var(--primary-50); }

	/* ── Right rail ── */
	.ord-head { display: flex; align-items: center; justify-content: space-between; padding: 14px 16px; border-bottom: 1px solid var(--border); }
	.ord-title { font-size: 14.5px; font-weight: 700; }
	.ord-count { font-size: 11.5px; color: var(--text-muted); margin-top: 1px; }
	.btn-print { display: inline-flex; align-items: center; gap: 5px; background: var(--surface); border: 1px solid var(--border); border-radius: 7px; padding: 6px 11px; font-family: inherit; font-size: 12px; font-weight: 500; color: var(--text-muted); cursor: not-allowed; opacity: 0.7; }
	.rail-right .rail-scroll { padding: 14px 16px; }

	.btn-add-med { display: flex; align-items: center; justify-content: center; gap: 7px; width: 100%; padding: 9px; background: var(--primary-50); border: 1px dashed var(--primary-light); border-radius: 8px; font-family: inherit; font-size: 13px; font-weight: 600; color: var(--primary); cursor: pointer; }
	.btn-add-med:hover { background: var(--primary-light); }

	.freq-block { margin: 14px 0; }
	.freq-title { font-size: 10.5px; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
	.freq-title span { font-weight: 500; text-transform: none; letter-spacing: 0; color: var(--text-light); }

	.ord-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; text-align: center; padding: 30px 12px; color: var(--text-muted); }
	.ord-empty p { font-size: 13px; font-weight: 500; }
	.ord-empty span { font-size: 11.5px; color: var(--text-light); }

	.med-card { border: 1px solid var(--border); border-left: 3px solid var(--primary); border-radius: 9px; padding: 11px; margin-bottom: 10px; display: flex; flex-direction: column; gap: 7px; }
	.med-card-head { display: flex; align-items: center; justify-content: space-between; }
	.med-card-no { font-size: 10.5px; font-weight: 700; color: var(--primary); text-transform: uppercase; letter-spacing: 0.5px; }
	.med-remove { background: none; border: none; cursor: pointer; display: flex; padding: 2px; }
	.med-field { font-size: 13px; padding: 7px 9px; }
	.med-row { display: grid; grid-template-columns: 1fr 64px; gap: 7px; }
	.med-qty { text-align: center; }
	.poso-hint { font-size: 10.5px; color: var(--primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: -2px; }

	/* ── Honoraires ── */
	.fee-block { padding: 14px 16px; border-top: 1px solid var(--border); }
	.fee-title { display: flex; align-items: center; gap: 5px; font-size: 11px; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.4px; margin-bottom: 10px; }
	.fee-chips { margin-bottom: 10px; }
	.fee-chip { background: var(--surface); border: 1px solid var(--border); color: var(--text); font-family: inherit; font-size: 12.5px; font-weight: 600; padding: 6px 13px; border-radius: 20px; cursor: pointer; transition: all 0.12s; }
	.fee-chip:hover { border-color: var(--primary); }
	.fee-chip.active { background: var(--primary); border-color: var(--primary); color: white; }
	.fee-amount { display: flex; align-items: center; gap: 9px; }
	.fee-amount-label { font-size: 12.5px; color: var(--text-muted); flex: 1; }
	.fee-input { width: 96px; text-align: right; font-weight: 700; font-size: 15px; }
	.fee-currency { font-size: 12.5px; color: var(--text-muted); font-weight: 600; }

	.btn-save { display: flex; align-items: center; justify-content: center; gap: 8px; margin: 0 16px 16px; padding: 12px; background: var(--success); color: white; border: none; border-radius: 9px; font-family: inherit; font-size: 14px; font-weight: 600; cursor: pointer; }
	.btn-save:hover:not(:disabled) { background: #047857; }
	.btn-save:disabled { opacity: 0.6; cursor: default; }
	.kbd { font-size: 11px; font-weight: 600; opacity: 0.75; background: rgba(255,255,255,0.18); padding: 1px 6px; border-radius: 5px; }

	.btn-ghost { display: inline-flex; align-items: center; gap: 4px; background: var(--primary-50); border: 1px solid var(--primary-light); border-radius: 6px; padding: 4px 9px; font-family: inherit; font-size: 11.5px; font-weight: 600; color: var(--primary); cursor: pointer; }
	.btn-outline { display: inline-flex; align-items: center; gap: 6px; background: var(--surface); border: 1px solid var(--border); border-radius: 7px; padding: 7px 12px; font-family: inherit; font-size: 12.5px; font-weight: 500; color: var(--text-muted); text-decoration: none; white-space: nowrap; }
	.btn-outline:hover { border-color: var(--primary); color: var(--primary); }

	.field-error { display: flex; align-items: center; gap: 5px; font-size: 11.5px; color: var(--danger); font-weight: 500; margin-top: 7px; }

	/* Narrower laptops: keep three columns but tighten; stack only on very small widths. */
	@media (max-width: 1180px) {
		.cockpit { grid-template-columns: 224px minmax(0, 1fr) 288px; gap: 10px; padding: 10px; }
		.vitals-row { grid-template-columns: repeat(3, minmax(0, 1fr)); }
	}
	@media (max-width: 920px) {
		.cockpit { grid-template-columns: 1fr; height: auto; overflow: visible; }
		.rail, .center { overflow: visible; }
	}
</style>
