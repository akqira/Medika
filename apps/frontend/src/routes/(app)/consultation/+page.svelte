<script lang="ts">
	import { enhance } from '$app/forms';
	import { page } from '$app/state';
	import type { PageData, ActionData } from './$types';
	import type { PatientSummary } from '$lib/types/api';
	import { MEDICAMENTS } from '$lib/data/medicaments';
	import Icon from '$lib/components/Icon.svelte';
	import Avatar from '$lib/components/Avatar.svelte';
	import QuickAddPatientModal from '$lib/components/QuickAddPatientModal.svelte';

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

	<div style="display:flex;height:calc(100vh - 58px);overflow:hidden">

		<!-- Left: form -->
		<div style="flex:1;overflow-y:auto;padding:24px;background:var(--bg)">
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
					<label for="tariff" style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Honoraires (DA)</label>
					<input id="tariff" name="tariff" bind:value={fee} class="mk-input" type="number" min="0" placeholder="0" style="max-width:200px" />
				</div>
			</div>

			<!-- Action buttons -->
			<div style="display:flex;gap:10px">
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

		<!-- Right: prescription editor -->
		<div style="width:360px;border-left:1px solid var(--border);background:var(--surface);display:flex;flex-direction:column;overflow:hidden;flex-shrink:0">
			<div style="padding:18px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
				<div>
					<h2 style="font-size:14.5px;font-weight:600">Ordonnance</h2>
					<p style="font-size:12px;color:var(--text-muted);margin-top:2px">{medications.length} médicament{medications.length !== 1 ? 's' : ''}</p>
				</div>
				<button
					type="button"
					onclick={addMed}
					style="display:flex;align-items:center;gap:6px;padding:7px 12px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13px;font-weight:500;cursor:pointer"
				>
					<Icon name="plus" size={14} color="white" />
					Ajouter
				</button>
			</div>

			<!-- Autocomplétion médicaments (saisie libre toujours possible) -->
			<datalist id="med-list">
				{#each MEDICAMENTS as m}<option value={m}></option>{/each}
			</datalist>

			<div style="flex:1;overflow-y:auto;padding:16px">
				{#if medications.length === 0}
					<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:200px;color:var(--text-muted);gap:12px;text-align:center">
						<Icon name="fileText" size={36} color="var(--border-strong)" />
						<div>
							<p style="font-size:14px;font-weight:500">Aucun médicament</p>
							<p style="font-size:12.5px;margin-top:4px">Cliquez sur « Ajouter » pour commencer</p>
						</div>
					</div>
				{:else}
					{#each medications as med}
						<div class="card" style="padding:14px;margin-bottom:10px">
							<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:10px">
								<span style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.4px">Médicament</span>
								<button type="button" onclick={() => removeMed(med.id)} style="background:none;border:none;cursor:pointer;color:var(--danger);display:flex;align-items:center">
									<Icon name="trash" size={14} color="var(--danger)" />
								</button>
							</div>
							<div style="display:flex;flex-direction:column;gap:8px">
								<!-- Médicament (autocomplété, saisie libre possible) + posologie sur la même ligne -->
								<div style="display:flex;gap:8px">
									<input
										value={med.medication}
										oninput={(e) => updateMed(med.id, 'medication', (e.target as HTMLInputElement).value)}
										list="med-list"
										class="mk-input"
										style="flex:1.7;min-width:0"
										placeholder="Médicament"
										autocomplete="off"
									/>
									<input
										value={med.dosage}
										oninput={(e) => updateMed(med.id, 'dosage', (e.target as HTMLInputElement).value)}
										class="mk-input"
										style="flex:1;min-width:0"
										placeholder="500 mg"
										title="Posologie / dosage"
									/>
								</div>
								<!-- Prise (champ libre, bref) + durée + quantité -->
								<div style="display:grid;grid-template-columns:1.5fr 1fr 0.7fr;gap:8px">
									<input
										value={med.frequency}
										oninput={(e) => updateMed(med.id, 'frequency', (e.target as HTMLInputElement).value)}
										class="mk-input"
										placeholder="Prise (matin et soir…)"
										title="Prise — texte libre"
									/>
									<input
										value={med.duration}
										oninput={(e) => updateMed(med.id, 'duration', (e.target as HTMLInputElement).value)}
										class="mk-input"
										placeholder="Durée (7j)"
									/>
									<input
										type="number"
										min="1"
										value={med.quantity}
										oninput={(e) => updateMed(med.id, 'quantity', Number((e.target as HTMLInputElement).value))}
										class="mk-input"
										placeholder="Qté"
										title="Quantité (boîtes)"
									/>
								</div>
							</div>
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

	</div>
</form>

<style>
	/* Constantes vitales : champs courts (5-6 caractères) — pas de gaspillage d'espace */
	.vital-in {
		padding: 7px 8px !important;
		font-size: 13px !important;
		text-align: center;
	}
</style>
