<script lang="ts">
	import { enhance } from '$app/forms';
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import type { PageData, ActionData } from './$types';
	import type { PatientSummary } from '$lib/types/api';
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
		patients.find(p => p.id === selectedPatientId) ?? null
	);

	function onPatientAdded(patient: PatientSummary) {
		patients = [patient, ...patients];
		selectedPatientId = patient.id;
		showQuickAdd = false;
	}

	// Vitals
	let bp     = $state('');
	let pulse  = $state('');
	let temp   = $state('');
	let weight = $state('');
	let height = $state('');
	let satO2  = $state('');

	// Clinical
	let chiefComplaint = $state('');
	let clinicalNotes  = $state('');
	let diagnosis      = $state('');
	let notes          = $state('');
	let fee            = $state('');

	// Act catalogue: picking an act pre-fills the honoraires (still overridable).
	let selectedActId = $state('');
	const selectedActName = $derived(data.acts.find((a) => a.id === selectedActId)?.name ?? '');
	function applyAct() {
		const act = data.acts.find((a) => a.id === selectedActId);
		if (act) fee = String(act.tariff);
	}

	// Liste médicaments : démarre sur le jeu intégré, puis remplace par la
	// nomenclature complète (static/medicaments.json) si elle a été générée.
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

	// Prescription
	type Med = { id: number; medication: string; dosage: string; frequency: string; duration: string; quantity: number };
	let medications = $state<Med[]>([]);
	let nextId = $state(1);

	function addMed() {
		medications = [...medications, { id: nextId, medication: '', dosage: '', frequency: '', duration: '', quantity: 1 }];
		nextId++;
	}
	function removeMed(id: number) {
		medications = medications.filter(m => m.id !== id);
	}
	function updateMed(id: number, field: keyof Omit<Med,'id'>, val: string | number) {
		medications = medications.map(m => m.id === id ? { ...m, [field]: val } : m);
	}

	// Serialized prescription for hidden input
	const prescriptionJson = $derived(
		JSON.stringify(medications.map(({ medication, dosage, frequency, duration, quantity }) =>
			({ medication, dosage, frequency, duration, quantity })
		))
	);

	// Loading & confirm state
	let submitting    = $state(false);
	let finalizeValue = $state('false');
	let formEl        = $state<HTMLFormElement | null>(null);

	function handleFinalize() {
		if (confirm('Finaliser la consultation ? Cette action ne peut pas être annulée.')) {
			finalizeValue = 'true';
			// Use setTimeout to let the hidden input value update before submit
			setTimeout(() => formEl?.requestSubmit(), 0);
		}
	}
</script>

{#if showQuickAdd}
	<QuickAddPatientModal {onPatientAdded} onClose={() => showQuickAdd = false} />
{/if}

<form
	bind:this={formEl}
	method="POST"
	use:enhance={() => {
		submitting = true;
		return async ({ update }) => {
			submitting = false;
			finalizeValue = 'false';
			await update();
		};
	}}
>
	<!-- Hidden fields -->
	<input type="hidden" name="finalize" value={finalizeValue} />
	<input type="hidden" name="appointmentId" value={appointmentId} />
	<input type="hidden" name="prescription" value={prescriptionJson} />
	<input type="hidden" name="actName" value={selectedActName} />

	<div style="height:calc(100vh - 58px);overflow-y:auto;background:var(--bg)">

		<!-- Colonne unique — ordonnance en pleine largeur plus bas -->
		<div style="max-width:980px;margin:0 auto;padding:24px 24px 40px">
			<h1 style="font-size:18px;font-weight:700;margin-bottom:20px">Nouvelle consultation</h1>

			<!-- Error / success banners -->
			{#if form?.error}
				<div style="display:flex;align-items:center;gap:8px;padding:12px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;margin-bottom:16px">
					<Icon name="alertCircle" size={15} color="var(--danger)" />
					<span style="font-size:13.5px;color:var(--danger);font-weight:500">{form.error}</span>
				</div>
			{/if}
			{#if form?.success}
				<div style="display:flex;align-items:center;gap:8px;padding:12px 16px;background:var(--success-light);border:1px solid #A7F3D0;border-radius:8px;margin-bottom:16px">
					<Icon name="checkCircle" size={15} color="var(--success)" />
					<span style="font-size:13.5px;color:var(--success);font-weight:500">{form.success}</span>
				</div>
			{/if}

			<!-- Patient selector -->
			<input type="hidden" name="patientId" value={selectedPatientId} />
			{#if selectedPatient}
				<!-- Compact bar once a patient is chosen — doesn't hog vertical space -->
				<div class="card" style="padding:8px 12px;margin-bottom:16px;display:flex;align-items:center;gap:10px">
					<Avatar nom={selectedPatient.lastName} prenom={selectedPatient.firstName} sexe={selectedPatient.gender} size={30} />
					<div style="flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
						<span style="font-size:14px;font-weight:600">{selectedPatient.firstName} {selectedPatient.lastName}</span>
						<span style="font-size:12.5px;color:var(--text-muted)"> · {selectedPatient.age} ans · {selectedPatient.gender === 'F' ? 'Femme' : 'Homme'}</span>
					</div>
					<button type="button" onclick={() => selectedPatientId = ''}
						style="display:inline-flex;align-items:center;gap:5px;background:none;border:none;cursor:pointer;color:var(--text-muted);font-family:inherit;font-size:12.5px;font-weight:500">
						Changer <Icon name="x" size={14} />
					</button>
				</div>
			{:else}
				<div class="card" style="padding:14px 16px;margin-bottom:16px">
					<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px">
						<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px">Patient</div>
						<button
							type="button"
							onclick={() => showQuickAdd = true}
							style="display:flex;align-items:center;gap:5px;padding:5px 11px;background:var(--primary-50);border:1px solid var(--primary-light);border-radius:6px;font-family:inherit;font-size:12px;font-weight:600;color:var(--primary);cursor:pointer"
						>
							<Icon name="plus" size={12} color="var(--primary)" />
							Nouveau patient
						</button>
					</div>
					<select bind:value={selectedPatientId} class="mk-input" style="font-size:14px">
						<option value="">— Sélectionner un patient —</option>
						{#each patients as p}
							<option value={p.id}>{p.firstName} {p.lastName} ({p.age} ans)</option>
						{/each}
					</select>
					{#if patients.length === 0}
						<p style="font-size:12.5px;color:var(--text-muted);margin-top:8px">Aucun patient. Utilisez « Nouveau patient » pour en créer un.</p>
					{/if}
				</div>
			{/if}

			<!-- Vitals -->
			<div class="card" style="padding:18px 20px;margin-bottom:16px">
				<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Constantes vitales</div>
				<div style="display:flex;flex-wrap:wrap;gap:12px 18px;align-items:flex-end">
					<div style="width:92px">
						<label for="vital-bp" style="font-size:11.5px;color:var(--text-muted);display:block;margin-bottom:4px">Tension</label>
						<input id="vital-bp" name="bloodPressure" bind:value={bp} class="mk-input vital-in" placeholder="120/80" />
					</div>
					<div style="width:66px">
						<label for="vital-pulse" style="font-size:11.5px;color:var(--text-muted);display:block;margin-bottom:4px">Pouls</label>
						<input id="vital-pulse" name="pulseRate" bind:value={pulse} class="mk-input vital-in" placeholder="72" />
					</div>
					<div style="width:66px">
						<label for="vital-temp" style="font-size:11.5px;color:var(--text-muted);display:block;margin-bottom:4px">Temp.</label>
						<input id="vital-temp" name="temperature" bind:value={temp} class="mk-input vital-in" placeholder="37.2" />
					</div>
					<div style="width:66px">
						<label for="vital-weight" style="font-size:11.5px;color:var(--text-muted);display:block;margin-bottom:4px">Poids</label>
						<input id="vital-weight" name="weight" bind:value={weight} class="mk-input vital-in" placeholder="70" />
					</div>
					<div style="width:66px">
						<label for="vital-height" style="font-size:11.5px;color:var(--text-muted);display:block;margin-bottom:4px">Taille</label>
						<input id="vital-height" name="height" bind:value={height} class="mk-input vital-in" type="number" min="0" placeholder="175" />
					</div>
					<div style="width:66px">
						<label for="vital-spo2" style="font-size:11.5px;color:var(--text-muted);display:block;margin-bottom:4px">SatO₂</label>
						<input id="vital-spo2" name="spO2" bind:value={satO2} class="mk-input vital-in" placeholder="98" />
					</div>
				</div>
			</div>

			<!-- Clinical notes -->
			<div class="card" style="padding:18px 20px;margin-bottom:16px">
				<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Anamnèse</div>
				<div style="margin-bottom:12px">
					<label for="reason" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Motif de consultation</label>
					<input id="reason" name="reason" bind:value={chiefComplaint} class="mk-input" placeholder="Décrivez le motif principal…" />
				</div>
				<div style="margin-bottom:12px">
					<label for="clinicalExam" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Examen clinique</label>
					<textarea id="clinicalExam" name="clinicalExam" bind:value={clinicalNotes} class="mk-input" rows="4" placeholder="Notes d'examen clinique…"></textarea>
				</div>
				<div>
					<label for="notes" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Notes complémentaires</label>
					<textarea id="notes" name="notes" bind:value={notes} class="mk-input" rows="2" placeholder="Observations supplémentaires…"></textarea>
				</div>
			</div>

			<!-- Diagnosis + fee -->
			<div class="card" style="padding:18px 20px;margin-bottom:16px">
				<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Diagnostic et honoraires</div>
				<div style="margin-bottom:12px">
					<label for="diagnosis" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Diagnostic</label>
					<textarea id="diagnosis" name="diagnosis" bind:value={diagnosis} class="mk-input" rows="2" placeholder="Diagnostic principal…"></textarea>
				</div>
				<div>
					<label for="act" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Acte</label>
					<select id="act" bind:value={selectedActId} onchange={applyAct} class="mk-input" style="max-width:320px;margin-bottom:12px">
						<option value="">— Acte libre —</option>
						{#each data.acts as a}
							<option value={a.id}>{a.name} — {a.tariff} DA</option>
						{/each}
					</select>
					{#if data.acts.length === 0}
						<p style="font-size:11.5px;color:var(--text-muted);margin:-6px 0 12px">
							Aucun acte. <a href="/actes" style="color:var(--primary)">Gérer le catalogue</a>.
						</p>
					{/if}
					<label for="tariff" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Honoraires (DA)</label>
					<input id="tariff" name="tariff" bind:value={fee} class="mk-input" type="number" min="0" placeholder="0" style="max-width:200px" />
				</div>
			</div>

			<!-- Ordonnance — section pleine largeur -->
			<div class="card" style="padding:0;margin-bottom:16px">
				<div style="padding:16px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
					<div>
						<h2 style="font-size:14.5px;font-weight:600">Ordonnance</h2>
						<p style="font-size:12px;color:var(--text-muted);margin-top:2px">{medications.length} médicament{medications.length !== 1 ? 's' : ''}</p>
					</div>
					<button
						type="button"
						onclick={addMed}
						style="display:flex;align-items:center;gap:6px;padding:8px 14px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13px;font-weight:600;cursor:pointer"
					>
						<Icon name="plus" size={14} color="white" />
						Ajouter un médicament
					</button>
				</div>

			<div style="padding:16px 20px">
				{#if medications.length === 0}
					<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:200px;color:var(--text-muted);gap:12px;text-align:center">
						<Icon name="fileText" size={36} color="var(--border-strong)" />
						<div>
							<p style="font-size:14px;font-weight:500">Aucun médicament</p>
							<p style="font-size:12.5px;margin-top:4px">Cliquez sur « Ajouter » pour commencer</p>
						</div>
					</div>
				{:else}
					<!-- En-têtes de colonnes -->
					<div class="ord-grid ord-head">
						<span class="ord-h">Médicament</span>
						<span class="ord-h">Posologie</span>
						<span class="ord-h">Durée</span>
						<span class="ord-h" style="text-align:center">Qté</span>
						<span></span>
					</div>
					{#each medications as med}
						{@const posoHint = expandPosology(med.dosage)}
						<div class="ord-grid">
							<Combobox
								value={med.medication}
								options={medList}
								placeholder="Médicament"
								onInput={(v) => updateMed(med.id, 'medication', v)}
								style="width:100%;min-width:0"
							/>
							<div style="min-width:0">
								<input
									value={med.dosage}
									oninput={(e) => updateMed(med.id, 'dosage', (e.target as HTMLInputElement).value)}
									onblur={() => { const x = expandPosology(med.dosage); if (x) updateMed(med.id, 'dosage', x); }}
									class="mk-input" style="width:100%;min-width:0" placeholder="1-0-1-0 ou texte libre"
									title="Posologie — tapez 1-0-1-0 (matin-midi-soir-coucher) ou du texte libre" />
								{#if posoHint && posoHint !== med.dosage}
									<div style="font-size:10.5px;color:var(--primary);margin-top:3px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis" title={posoHint}>= {posoHint}</div>
								{/if}
							</div>
							<input
								value={med.duration}
								oninput={(e) => updateMed(med.id, 'duration', (e.target as HTMLInputElement).value)}
								class="mk-input" style="min-width:0" placeholder="7j" title="Durée" />
							<input
								type="number" min="1"
								value={med.quantity}
								oninput={(e) => updateMed(med.id, 'quantity', Number((e.target as HTMLInputElement).value))}
								class="mk-input" style="min-width:0;text-align:center" placeholder="Qté" title="Quantité (boîtes)" />
							<button type="button" onclick={() => removeMed(med.id)} aria-label="Retirer le médicament"
								style="display:flex;align-items:center;justify-content:center;background:none;border:none;cursor:pointer;color:var(--danger);height:38px">
								<Icon name="trash" size={15} color="var(--danger)" />
							</button>
						</div>
					{/each}
				{/if}
			</div>

			{#if medications.length > 0}
				<div style="padding:12px 16px;border-top:1px solid var(--border);display:flex;align-items:center;gap:8px;color:var(--text-muted)">
					<Icon name="printer" size={14} color="var(--text-muted)" />
					<span style="font-size:12px;line-height:1.4">L'ordonnance s'imprime depuis le dossier patient une fois la consultation enregistrée.</span>
				</div>
			{/if}
		</div>

			<!-- Actions -->
			<div style="display:flex;gap:10px;margin-bottom:8px">
				<button
					type="submit"
					disabled={submitting}
					style="flex:1;padding:12px;background:var(--bg);color:var(--primary);border:1.5px solid var(--primary);border-radius:8px;font-family:inherit;font-size:14px;font-weight:600;cursor:pointer;opacity:{submitting ? 0.6 : 1}"
				>
					{submitting ? 'Enregistrement…' : 'Enregistrer brouillon'}
				</button>
				<button
					type="button"
					disabled={submitting}
					onclick={handleFinalize}
					style="flex:2;padding:12px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:14.5px;font-weight:600;cursor:pointer;opacity:{submitting ? 0.6 : 1}"
				>
					{submitting ? 'Finalisation…' : 'Finaliser la consultation'}
				</button>
			</div>

		</div>
	</div>
</form>

<style>
	/* Constantes vitales : champs courts (5-6 caractères) — pas de gaspillage d'espace */
	.vital-in {
		padding: 7px 8px !important;
		font-size: 13px !important;
		text-align: center;
	}
	/* Ordonnance — lignes pleine largeur, colonnes alignées */
	.ord-grid {
		display: grid;
		grid-template-columns: minmax(0, 2.6fr) minmax(0, 2fr) minmax(0, 1fr) 72px 32px;
		gap: 8px;
		align-items: center;
		margin-bottom: 9px;
	}
	.ord-head { margin-bottom: 4px; padding: 0 2px; }
	.ord-h {
		font-size: 11px;
		font-weight: 600;
		color: var(--text-muted);
		text-transform: uppercase;
		letter-spacing: 0.4px;
	}
	@media (max-width: 720px) {
		.ord-grid { grid-template-columns: 1fr 1fr; }
		.ord-head { display: none; }
	}
</style>
