<script lang="ts">
	import type { PageData } from './$types';
	import Icon from '$lib/components/Icon.svelte';
	import Avatar from '$lib/components/Avatar.svelte';

	let { data }: { data: PageData } = $props();

	let selectedPatientId = $state('');
	const selectedPatient = $derived(
		data.patients.find(p => p.id === selectedPatientId) ?? null
	);

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
	let fee            = $state('');

	// Prescription
	type Med = { id: number; name: string; dosage: string; freq: string; duration: string };
	let medications = $state<Med[]>([]);
	let nextId = $state(1);

	function addMed() {
		medications = [...medications, { id: nextId, name: '', dosage: '', freq: '', duration: '' }];
		nextId++;
	}
	function removeMed(id: number) {
		medications = medications.filter(m => m.id !== id);
	}
	function updateMed(id: number, field: keyof Omit<Med,'id'>, val: string) {
		medications = medications.map(m => m.id === id ? { ...m, [field]: val } : m);
	}
</script>

<div style="display:flex;height:calc(100vh - 58px);overflow:hidden">

	<!-- Left: form -->
	<div style="flex:1;overflow-y:auto;padding:24px;background:var(--bg)">
		<h1 style="font-size:18px;font-weight:700;margin-bottom:20px">Nouvelle consultation</h1>

		<!-- Patient selector -->
		<div class="card" style="padding:18px 20px;margin-bottom:16px">
			<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:10px">Patient</div>
			{#if selectedPatient}
				<div style="display:flex;align-items:center;gap:12px">
					<Avatar nom={selectedPatient.lastName} prenom={selectedPatient.firstName} sexe={selectedPatient.gender} size={40} />
					<div style="flex:1">
						<div style="font-size:14.5px;font-weight:600">{selectedPatient.firstName} {selectedPatient.lastName}</div>
						<div style="font-size:12.5px;color:var(--text-muted);margin-top:2px">{selectedPatient.age} ans · {selectedPatient.gender === 'F' ? 'Femme' : 'Homme'}</div>
					</div>
					<button onclick={() => selectedPatientId = ''} style="background:none;border:none;cursor:pointer;color:var(--text-muted)">
						<Icon name="x" size={16} />
					</button>
				</div>
			{:else}
				<select
					bind:value={selectedPatientId}
					class="mk-input"
					style="font-size:14px"
				>
					<option value="">— Sélectionner un patient —</option>
					{#each data.patients as p}
						<option value={p.id}>{p.firstName} {p.lastName} ({p.age} ans)</option>
					{/each}
				</select>
				{#if data.patients.length === 0}
					<p style="font-size:12.5px;color:var(--text-muted);margin-top:8px">Aucun patient disponible. Vérifiez la connexion au serveur.</p>
				{/if}
			{/if}
		</div>

		<!-- Vitals -->
		<div class="card" style="padding:18px 20px;margin-bottom:16px">
			<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Constantes vitales</div>
			<div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px">
				{#each [
					{ label: 'Tension (mmHg)', bind: 'bp',     val: bp,     placeholder: 'ex: 120/80' },
					{ label: 'Pouls (bpm)',    bind: 'pulse',  val: pulse,  placeholder: 'ex: 72'     },
					{ label: 'Temp. (°C)',     bind: 'temp',   val: temp,   placeholder: 'ex: 37.2'   },
					{ label: 'Poids (kg)',     bind: 'weight', val: weight, placeholder: 'ex: 70'     },
					{ label: 'Taille (cm)',    bind: 'height', val: height, placeholder: 'ex: 175'    },
					{ label: 'SatO₂ (%)',      bind: 'satO2',  val: satO2,  placeholder: 'ex: 98'     },
				] as field}
					<div>
						<label style="font-size:12px;color:var(--text-muted);display:block;margin-bottom:4px">{field.label}</label>
						{#if field.bind === 'bp'}
							<input bind:value={bp}     class="mk-input" placeholder={field.placeholder} />
						{:else if field.bind === 'pulse'}
							<input bind:value={pulse}  class="mk-input" placeholder={field.placeholder} />
						{:else if field.bind === 'temp'}
							<input bind:value={temp}   class="mk-input" placeholder={field.placeholder} />
						{:else if field.bind === 'weight'}
							<input bind:value={weight} class="mk-input" placeholder={field.placeholder} />
						{:else if field.bind === 'height'}
							<input bind:value={height} class="mk-input" placeholder={field.placeholder} />
						{:else}
							<input bind:value={satO2}  class="mk-input" placeholder={field.placeholder} />
						{/if}
					</div>
				{/each}
			</div>
		</div>

		<!-- Clinical notes -->
		<div class="card" style="padding:18px 20px;margin-bottom:16px">
			<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Anamnèse</div>
			<div style="margin-bottom:12px">
				<label style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Motif de consultation</label>
				<input bind:value={chiefComplaint} class="mk-input" placeholder="Décrivez le motif principal…" />
			</div>
			<div>
				<label style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Examen clinique</label>
				<textarea bind:value={clinicalNotes} class="mk-input" rows="4" placeholder="Notes d'examen clinique…"></textarea>
			</div>
		</div>

		<!-- Diagnosis + fee -->
		<div class="card" style="padding:18px 20px;margin-bottom:16px">
			<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:12px">Diagnostic et honoraires</div>
			<div style="margin-bottom:12px">
				<label style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Diagnostic</label>
				<textarea bind:value={diagnosis} class="mk-input" rows="2" placeholder="Diagnostic principal…"></textarea>
			</div>
			<div style="display:flex;align-items:center;gap:10px">
				<div style="flex:1">
					<label style="font-size:13px;color:var(--text-muted);display:block;margin-bottom:5px">Honoraires (DA)</label>
					<input bind:value={fee} class="mk-input" type="number" min="0" placeholder="0" />
				</div>
			</div>
		</div>

		<button
			style="width:100%;padding:12px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:14.5px;font-weight:600;cursor:pointer"
			onclick={() => alert('Enregistrement à implémenter côté serveur')}
		>
			Enregistrer la consultation
		</button>
	</div>

	<!-- Right: prescription editor -->
	<div style="width:360px;border-left:1px solid var(--border);background:var(--surface);display:flex;flex-direction:column;overflow:hidden;flex-shrink:0">
		<div style="padding:18px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
			<div>
				<h2 style="font-size:14.5px;font-weight:600">Ordonnance</h2>
				<p style="font-size:12px;color:var(--text-muted);margin-top:2px">{medications.length} médicament{medications.length !== 1 ? 's' : ''}</p>
			</div>
			<button
				onclick={addMed}
				style="display:flex;align-items:center;gap:6px;padding:7px 12px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13px;font-weight:500;cursor:pointer"
			>
				<Icon name="plus" size={14} color="white" />
				Ajouter
			</button>
		</div>

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
							<button onclick={() => removeMed(med.id)} style="background:none;border:none;cursor:pointer;color:var(--danger);display:flex;align-items:center">
								<Icon name="trash" size={14} color="var(--danger)" />
							</button>
						</div>
						<div style="display:flex;flex-direction:column;gap:8px">
							<input
								value={med.name}
								oninput={(e) => updateMed(med.id, 'name', (e.target as HTMLInputElement).value)}
								class="mk-input"
								placeholder="Nom du médicament"
							/>
							<div style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
								<input
									value={med.dosage}
									oninput={(e) => updateMed(med.id, 'dosage', (e.target as HTMLInputElement).value)}
									class="mk-input"
									placeholder="Posologie"
								/>
								<input
									value={med.freq}
									oninput={(e) => updateMed(med.id, 'freq', (e.target as HTMLInputElement).value)}
									class="mk-input"
									placeholder="Fréquence"
								/>
							</div>
							<input
								value={med.duration}
								oninput={(e) => updateMed(med.id, 'duration', (e.target as HTMLInputElement).value)}
								class="mk-input"
								placeholder="Durée (ex: 7 jours)"
							/>
						</div>
					</div>
				{/each}
			{/if}
		</div>

		{#if medications.length > 0}
			<div style="padding:14px 16px;border-top:1px solid var(--border)">
				<button
					style="width:100%;padding:10px;background:var(--bg);color:var(--primary);border:1px solid var(--primary);border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer;display:flex;align-items:center;justify-content:center;gap:8px"
					onclick={() => alert('Impression à implémenter')}
				>
					<Icon name="printer" size={15} color="var(--primary)" />
					Imprimer l'ordonnance
				</button>
			</div>
		{/if}
	</div>

</div>
