<script lang="ts">
	import Icon from '$lib/components/Icon.svelte';
	import type { PatientSummary } from '$lib/types/api';

	let {
		open = $bindable(false),
		defaultDate = '',
		onbooked,
	}: {
		open?: boolean;
		defaultDate?: string;
		onbooked?: (appt: unknown) => void;
	} = $props();

	// Form fields
	let patientSearch   = $state('');
	let selectedPatient = $state<PatientSummary | null>(null);
	let date            = $state(defaultDate || new Date().toISOString().split('T')[0]);
	let time            = $state('08:00');
	let duration        = $state('30');
	let apptType        = $state('');
	let reason          = $state('');

	// Patient search state
	let searchResults   = $state<PatientSummary[]>([]);
	let searchLoading   = $state(false);
	let showDropdown    = $state(false);
	let searchTimer:    ReturnType<typeof setTimeout>;

	// Submit state
	let submitting = $state(false);
	let errorMsg   = $state('');

	const TYPES = [
		{ value: 'FirstVisit',    label: 'Première consultation' },
		{ value: 'FollowUp',      label: 'Suivi' },
		{ value: 'LabResults',    label: 'Résultats d\'analyses' },
		{ value: 'Prescription',  label: 'Renouvellement d\'ordonnance' },
		{ value: 'Certificate',   label: 'Certificat médical' },
		{ value: 'Urgent',        label: 'Urgence' },
		{ value: 'Other',         label: 'Autre' },
	];

	// Time slots: 08:00–18:00 in 15-minute steps
	const TIME_SLOTS = Array.from({ length: (10 * 60) / 15 + 1 }, (_, i) => {
		const total = 8 * 60 + i * 15;
		const h = Math.floor(total / 60);
		const m = total % 60;
		return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
	});

	function onPatientInput() {
		clearTimeout(searchTimer);
		selectedPatient = null;
		if (patientSearch.length < 2) {
			searchResults = [];
			showDropdown = false;
			return;
		}
		searchTimer = setTimeout(async () => {
			searchLoading = true;
			try {
				const res = await fetch(`/api/patients/search?term=${encodeURIComponent(patientSearch)}&page=1&pageSize=20`);
				if (res.ok) {
					const data = await res.json();
					searchResults = data.items ?? [];
					showDropdown = true;
				}
			} catch {
				searchResults = [];
			} finally {
				searchLoading = false;
			}
		}, 300);
	}

	function selectPatient(p: PatientSummary) {
		selectedPatient = p;
		patientSearch = `${p.firstName} ${p.lastName}`;
		showDropdown = false;
	}

	function closeModal() {
		open = false;
		resetForm();
	}

	function resetForm() {
		patientSearch = '';
		selectedPatient = null;
		date = defaultDate || new Date().toISOString().split('T')[0];
		time = '08:00';
		duration = '30';
		apptType = '';
		reason = '';
		errorMsg = '';
		searchResults = [];
		showDropdown = false;
	}

	$effect(() => {
		if (open) {
			date = defaultDate || new Date().toISOString().split('T')[0];
		}
	});

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') closeModal();
	}

	async function submit() {
		errorMsg = '';

		if (!selectedPatient) {
			errorMsg = 'Veuillez sélectionner un patient.';
			return;
		}
		if (!date) {
			errorMsg = 'Veuillez choisir une date.';
			return;
		}
		if (!apptType) {
			errorMsg = 'Veuillez choisir un type de rendez-vous.';
			return;
		}

		submitting = true;
		try {
			const res = await fetch('/api/appointments', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					patientId:       selectedPatient.id,
					date,
					time,
					durationMinutes: Number(duration),
					type:            apptType,
					reason,
				}),
			});

			if (!res.ok) {
				const body = await res.json().catch(() => ({}));
				errorMsg = body?.error ?? `Erreur ${res.status}`;
				return;
			}

			const appt = await res.json();
			onbooked?.(appt);
			closeModal();
		} catch {
			errorMsg = 'Erreur réseau, veuillez réessayer.';
		} finally {
			submitting = false;
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if open}
	<!-- Backdrop -->
	<div
		role="presentation"
		style="position:fixed;inset:0;background:rgba(0,0,0,0.45);z-index:400;display:flex;align-items:center;justify-content:center;padding:20px"
		onclick={(e) => { if (e.target === e.currentTarget) closeModal(); }}
	>
		<!-- Modal -->
		<div
			role="dialog"
			aria-modal="true"
			aria-label="Nouveau rendez-vous"
			style="background:var(--surface);border-radius:12px;width:100%;max-width:480px;max-height:90vh;display:flex;flex-direction:column;box-shadow:0 20px 60px rgba(0,0,0,0.25)"
		>
			<!-- Header -->
			<div style="padding:18px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;flex-shrink:0">
				<h2 style="font-size:16px;font-weight:700">Nouveau rendez-vous</h2>
				<button
					type="button"
					onclick={closeModal}
					aria-label="Fermer"
					style="background:none;border:none;cursor:pointer;color:var(--text-muted);display:flex;align-items:center;padding:4px;border-radius:6px"
				>
					<Icon name="x" size={18} />
				</button>
			</div>

			<!-- Body -->
			<div style="flex:1;overflow-y:auto;padding:20px;display:flex;flex-direction:column;gap:16px">

				{#if errorMsg}
					<div style="display:flex;align-items:center;gap:8px;padding:10px 14px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px">
						<Icon name="alertCircle" size={14} color="var(--danger)" />
						<span style="font-size:13px;color:var(--danger)">{errorMsg}</span>
					</div>
				{/if}

				<!-- Patient search -->
				<div>
					<label for="modal-patient" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Patient <span style="color:var(--danger)">*</span></label>
					<div style="position:relative">
						<input
							id="modal-patient"
							type="search"
							autocomplete="off"
							bind:value={patientSearch}
							oninput={onPatientInput}
							placeholder="Rechercher un patient…"
							class="mk-input"
							style="padding-right:32px"
						/>
						{#if searchLoading}
							<div style="position:absolute;right:10px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:11px">…</div>
						{/if}
						{#if showDropdown && searchResults.length > 0}
							<div style="position:absolute;top:calc(100% + 4px);left:0;right:0;background:var(--surface);border:1px solid var(--border);border-radius:8px;box-shadow:0 4px 16px rgba(0,0,0,0.1);z-index:10;max-height:200px;overflow-y:auto">
								{#each searchResults as p}
									<button
										type="button"
										onclick={() => selectPatient(p)}
										style="display:flex;align-items:center;gap:10px;width:100%;padding:10px 14px;background:none;border:none;border-bottom:1px solid var(--border);cursor:pointer;font-family:inherit;text-align:left"
									>
										<div style="font-size:13.5px;font-weight:500;color:var(--text)">{p.firstName} {p.lastName}</div>
										<span style="font-size:12px;color:var(--text-muted);margin-left:auto">{p.age} ans</span>
									</button>
								{/each}
							</div>
						{:else if showDropdown && searchResults.length === 0 && !searchLoading}
							<div style="position:absolute;top:calc(100% + 4px);left:0;right:0;background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:12px 14px;font-size:13px;color:var(--text-muted);z-index:10">
								Aucun patient trouvé
							</div>
						{/if}
					</div>
					{#if selectedPatient}
						<div style="margin-top:6px;font-size:12.5px;color:var(--success);display:flex;align-items:center;gap:4px">
							<Icon name="checkCircle" size={13} color="var(--success)" />
							{selectedPatient.firstName} {selectedPatient.lastName} sélectionné
						</div>
					{/if}
				</div>

				<!-- Date -->
				<div>
					<label for="modal-date" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Date <span style="color:var(--danger)">*</span></label>
					<input id="modal-date" type="date" bind:value={date} class="mk-input" />
				</div>

				<!-- Time + Duration -->
				<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
					<div>
						<label for="modal-time" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Heure <span style="color:var(--danger)">*</span></label>
						<select id="modal-time" bind:value={time} class="mk-input">
							{#each TIME_SLOTS as t}
								<option value={t}>{t}</option>
							{/each}
						</select>
					</div>
					<div>
						<label for="modal-duration" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Durée (min)</label>
						<select id="modal-duration" bind:value={duration} class="mk-input">
							{#each [15, 20, 30, 45, 60] as d}
								<option value={String(d)}>{d} min</option>
							{/each}
						</select>
					</div>
				</div>

				<!-- Type -->
				<div>
					<label for="modal-type" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Type de rendez-vous <span style="color:var(--danger)">*</span></label>
					<select id="modal-type" bind:value={apptType} class="mk-input">
						<option value="">— Choisir —</option>
						{#each TYPES as t}
							<option value={t.value}>{t.label}</option>
						{/each}
					</select>
				</div>

				<!-- Reason -->
				<div>
					<label for="modal-reason" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Motif</label>
					<input id="modal-reason" type="text" bind:value={reason} class="mk-input" placeholder="Motif optionnel…" />
				</div>
			</div>

			<!-- Footer -->
			<div style="padding:16px 20px;border-top:1px solid var(--border);display:flex;gap:10px;flex-shrink:0">
				<button
					type="button"
					onclick={closeModal}
					style="flex:1;padding:10px;background:var(--bg);color:var(--text);border:1px solid var(--border);border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:500;cursor:pointer"
				>
					Annuler
				</button>
				<button
					type="button"
					onclick={submit}
					disabled={submitting}
					style="flex:2;padding:10px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer;opacity:{submitting ? 0.6 : 1};display:flex;align-items:center;justify-content:center;gap:8px"
				>
					{#if submitting}
						<span style="font-size:12px">En cours…</span>
					{:else}
						<Icon name="check" size={14} color="white" />
						Confirmer le rendez-vous
					{/if}
				</button>
			</div>
		</div>
	</div>
{/if}
