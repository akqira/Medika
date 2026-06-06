<script lang="ts">
	import { goto } from '$app/navigation';
	import type { PageData } from './$types';
	import type { PatientSummary } from '$lib/types/api';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';

	let { data }: { data: PageData } = $props();

	let selectedPatient = $state<PatientSummary | null>(null);
	let activeTab = $state<'infos' | 'consultations' | 'ordonnances' | 'facturation'>('infos');

	let searchTerm = $state('');
	$effect(() => { searchTerm = data.term ?? ''; });

	let searchTimer: ReturnType<typeof setTimeout>;
	function onSearch() {
		clearTimeout(searchTimer);
		searchTimer = setTimeout(() => {
			goto(`/patients?term=${encodeURIComponent(searchTerm)}&page=1`, { replaceState: true });
		}, 350);
	}

	function formatLastVisit(iso: string | undefined) {
		if (!iso) return '—';
		return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' });
	}

	const TABS = [
		{ id: 'infos',          label: 'Informations' },
		{ id: 'consultations',  label: 'Consultations' },
		{ id: 'ordonnances',    label: 'Ordonnances' },
		{ id: 'facturation',    label: 'Facturation' },
	] as const;
</script>

<div style="display:flex;height:calc(100vh - 58px);overflow:hidden">

	<!-- Left sidebar -->
	<div style="width:280px;border-right:1px solid var(--border);background:var(--surface);display:flex;flex-direction:column;flex-shrink:0;overflow:hidden">

		<!-- Header + search -->
		<div style="padding:16px;border-bottom:1px solid var(--border)">
			<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px">
				<span style="font-size:14.5px;font-weight:600">Patients</span>
				<span style="font-size:12px;color:var(--text-muted)">{data.result.totalCount} au total</span>
			</div>
			<div style="display:flex;align-items:center;gap:8px;background:var(--bg);border:1px solid var(--border);border-radius:7px;padding:7px 11px">
				<Icon name="search" size={14} color="var(--text-muted)" />
				<input
					type="search"
					bind:value={searchTerm}
					oninput={onSearch}
					placeholder="Rechercher…"
					style="background:transparent;border:none;outline:none;font-family:inherit;font-size:13.5px;color:var(--text);width:100%"
				/>
			</div>
		</div>

		<!-- Patient list -->
		<div style="flex:1;overflow-y:auto">
			{#if data.result.items.length === 0}
				<div style="padding:32px 16px;text-align:center;color:var(--text-muted)">
					<Icon name="users" size={32} color="var(--border-strong)" />
					<p style="margin-top:10px;font-size:13px">Aucun patient trouvé</p>
				</div>
			{:else}
				{#each data.result.items as patient}
					<button
						onclick={() => { selectedPatient = patient; activeTab = 'infos'; }}
						style="
							display:flex;align-items:center;gap:10px;padding:12px 16px;width:100%;
							text-align:left;border:none;cursor:pointer;
							background:{selectedPatient?.id === patient.id ? 'var(--primary-50)' : 'transparent'};
							border-left:3px solid {selectedPatient?.id === patient.id ? 'var(--primary)' : 'transparent'};
							border-bottom:1px solid var(--border);
						"
					>
						<Avatar nom={patient.lastName} prenom={patient.firstName} sexe={patient.gender} size={34} />
						<div style="flex:1;min-width:0">
							<div style="font-size:13.5px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:{selectedPatient?.id === patient.id ? 'var(--primary)' : 'var(--text)'}">{patient.firstName} {patient.lastName}</div>
							<div style="font-size:12px;color:var(--text-muted);margin-top:1px">{patient.age} ans · {patient.gender === 'F' ? 'Femme' : 'Homme'}</div>
						</div>
						{#if patient.allergyCount > 0}
							<span style="font-size:13px;color:var(--warning);flex-shrink:0">⚠</span>
						{/if}
					</button>
				{/each}

				<!-- Pagination -->
				{#if data.result.totalPages > 1}
					<div style="display:flex;justify-content:space-between;padding:10px 16px;border-top:1px solid var(--border)">
						{#if data.result.hasPreviousPage}
							<a href="/patients?term={encodeURIComponent(data.term)}&page={data.page - 1}" style="font-size:12.5px;color:var(--primary);text-decoration:none">← Préc.</a>
						{:else}
							<span></span>
						{/if}
						<span style="font-size:12px;color:var(--text-muted)">{data.result.page}/{data.result.totalPages}</span>
						{#if data.result.hasNextPage}
							<a href="/patients?term={encodeURIComponent(data.term)}&page={data.page + 1}" style="font-size:12.5px;color:var(--primary);text-decoration:none">Suiv. →</a>
						{/if}
					</div>
				{/if}
			{/if}
		</div>
	</div>

	<!-- Right: patient detail -->
	<div style="flex:1;overflow-y:auto;background:var(--bg)">
		{#if selectedPatient}
			<!-- Patient header -->
			<div style="background:var(--surface);border-bottom:1px solid var(--border);padding:20px 24px">
				<div style="display:flex;align-items:center;gap:16px">
					<Avatar nom={selectedPatient.lastName} prenom={selectedPatient.firstName} sexe={selectedPatient.gender} size={56} />
					<div style="flex:1;min-width:0">
						<h2 style="font-size:18px;font-weight:700">{selectedPatient.firstName} {selectedPatient.lastName}</h2>
						<div style="display:flex;align-items:center;gap:10px;margin-top:6px;flex-wrap:wrap">
							<span style="font-size:13px;color:var(--text-muted)">{selectedPatient.age} ans</span>
							<span style="color:var(--border-strong)">·</span>
							<span style="font-size:13px;color:var(--text-muted)">{selectedPatient.gender === 'F' ? 'Femme' : 'Homme'}</span>
							{#if selectedPatient.bloodGroup}
								<span style="background:var(--danger-light);color:var(--danger);font-size:12px;font-weight:600;padding:2px 8px;border-radius:20px">{selectedPatient.bloodGroup}</span>
							{/if}
							{#if selectedPatient.allergyCount > 0}
								<span style="background:var(--warning-light);color:var(--warning);font-size:12px;font-weight:600;padding:2px 8px;border-radius:20px">⚠ {selectedPatient.allergyCount} allergie{selectedPatient.allergyCount > 1 ? 's' : ''}</span>
							{/if}
						</div>
					</div>
					<div style="display:flex;align-items:center;gap:8px">
						<a href="tel:{selectedPatient.phone}" style="display:flex;align-items:center;gap:6px;padding:8px 14px;background:var(--bg);border:1px solid var(--border);border-radius:7px;text-decoration:none;color:var(--text);font-size:13px">
							<Icon name="phone" size={14} color="var(--text-muted)" />
							{selectedPatient.phone}
						</a>
						<a href="/consultation" style="display:flex;align-items:center;gap:6px;padding:8px 14px;background:var(--primary);border-radius:7px;text-decoration:none;color:white;font-size:13px;font-weight:500">
							<Icon name="stethoscope" size={14} color="white" />
							Consultation
						</a>
					</div>
				</div>
			</div>

			<!-- Tabs -->
			<div style="background:var(--surface);border-bottom:1px solid var(--border);display:flex;padding:0 24px">
				{#each TABS as tab}
					<button
						onclick={() => activeTab = tab.id}
						class="mk-tab {activeTab === tab.id ? 'active' : ''}"
					>
						{tab.label}
					</button>
				{/each}
			</div>

			<!-- Tab content -->
			<div style="padding:24px">
				{#if activeTab === 'infos'}
					<div style="display:grid;grid-template-columns:1fr 1fr;gap:14px;max-width:640px">
						{#each [
							{ label: 'Prénom', val: selectedPatient.firstName },
							{ label: 'Nom', val: selectedPatient.lastName },
							{ label: 'Âge', val: `${selectedPatient.age} ans` },
							{ label: 'Sexe', val: selectedPatient.gender === 'F' ? 'Femme' : 'Homme' },
							{ label: 'Téléphone', val: selectedPatient.phone },
							{ label: 'Groupe sanguin', val: selectedPatient.bloodGroup ?? '—' },
							{ label: 'Allergies', val: selectedPatient.allergyCount > 0 ? `${selectedPatient.allergyCount} allergie(s)` : 'Aucune' },
							{ label: 'Dernière visite', val: formatLastVisit(selectedPatient.lastVisitAt) }
						] as field}
							<div class="card" style="padding:14px 16px">
								<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">{field.label}</div>
								<div style="font-size:14px;font-weight:500">{field.val}</div>
							</div>
						{/each}
					</div>
				{:else}
					<div style="display:flex;flex-direction:column;align-items:center;padding:60px 20px;text-align:center;color:var(--text-muted)">
						<Icon name="fileText" size={40} color="var(--border-strong)" />
						<p style="margin-top:14px;font-size:14.5px;font-weight:500">Aucune donnée disponible</p>
						<p style="margin-top:6px;font-size:13px">Cette section sera disponible prochainement.</p>
					</div>
				{/if}
			</div>
		{:else}
			<!-- Empty state -->
			<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;color:var(--text-muted);gap:14px">
				<div style="width:64px;height:64px;border-radius:16px;background:var(--surface);border:1px solid var(--border);display:flex;align-items:center;justify-content:center">
					<Icon name="user" size={28} color="var(--border-strong)" />
				</div>
				<div style="text-align:center">
					<p style="font-size:15px;font-weight:500">Sélectionnez un patient</p>
					<p style="font-size:13px;color:var(--text-light);margin-top:4px">Cliquez sur un patient dans la liste</p>
				</div>
			</div>
		{/if}
	</div>
</div>
