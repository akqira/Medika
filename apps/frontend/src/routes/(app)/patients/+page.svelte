<script lang="ts">
	import { goto } from '$app/navigation';
	import type { PageData } from './$types';
	import type { PatientSummary, ConsultationSummary, ConsultationDetail } from '$lib/types/api';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import Badge from '$lib/components/Badge.svelte';

	let { data }: { data: PageData } = $props();

	let selectedPatient = $state<PatientSummary | null>(null);
	let activeTab = $state<'infos' | 'consultations' | 'ordonnances' | 'facturation'>('infos');

	// Consultation state
	let consultations     = $state<ConsultationSummary[]>([]);
	let consultLoading    = $state(false);
	let consultError      = $state('');
	let expandedId        = $state<string | null>(null);
	let expandedDetail    = $state<ConsultationDetail | null>(null);
	let detailLoading     = $state(false);

	let searchTerm = $state('');
	$effect(() => { searchTerm = data.term ?? ''; });

	let searchTimer: ReturnType<typeof setTimeout>;
	function onSearch() {
		clearTimeout(searchTimer);
		searchTimer = setTimeout(() => {
			goto(`/patients?term=${encodeURIComponent(searchTerm)}&page=1`, { replaceState: true });
		}, 350);
	}

	// Fetch consultations when tab changes or patient changes
	$effect(() => {
		const patientId = selectedPatient?.id;
		if (activeTab !== 'consultations' || !patientId) return;

		consultations  = [];
		consultError   = '';
		expandedId     = null;
		expandedDetail = null;
		consultLoading = true;

		fetch(`/api/patients/${patientId}/consultations?page=1&pageSize=10`)
			.then(async (res) => {
				if (!res.ok) throw new Error(`Erreur ${res.status}`);
				const json = await res.json();
				consultations = json.items ?? [];
			})
			.catch((e: Error) => {
				consultError = e.message || 'Erreur lors du chargement.';
			})
			.finally(() => {
				consultLoading = false;
			});
	});

	async function toggleConsultation(id: string) {
		if (expandedId === id) {
			expandedId = null;
			expandedDetail = null;
			return;
		}
		expandedId = id;
		expandedDetail = null;
		detailLoading = true;
		try {
			// Re-use the same proxy and append the id
			const res = await fetch(`/api/patients/${selectedPatient!.id}/consultations/${id}`);
			if (res.ok) {
				expandedDetail = await res.json();
			}
		} catch {
			// Detail failed — still show summary
		} finally {
			detailLoading = false;
		}
	}

	function formatDate(iso: string) {
		return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
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

	const fmt = new Intl.NumberFormat('fr-DZ', { maximumFractionDigits: 0 });
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

				{:else if activeTab === 'consultations'}
					{#if consultLoading}
						<div style="display:flex;align-items:center;justify-content:center;padding:60px 0;color:var(--text-muted);gap:10px">
							<Icon name="activity" size={20} color="var(--border-strong)" />
							<span style="font-size:13.5px">Chargement des consultations…</span>
						</div>
					{:else if consultError}
						<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;max-width:480px">
							<Icon name="alertCircle" size={15} color="var(--danger)" />
							<span style="font-size:13.5px;color:var(--danger)">{consultError}</span>
						</div>
					{:else if consultations.length === 0}
						<div style="display:flex;flex-direction:column;align-items:center;padding:60px 20px;text-align:center;color:var(--text-muted)">
							<Icon name="fileText" size={40} color="var(--border-strong)" />
							<p style="margin-top:14px;font-size:14.5px;font-weight:500">Aucune consultation enregistrée pour ce patient.</p>
						</div>
					{:else}
						<div style="max-width:720px;display:flex;flex-direction:column;gap:10px">
							{#each consultations as c}
								<div class="card" style="overflow:hidden">
									<button
										type="button"
										onclick={() => toggleConsultation(c.id)}
										style="display:flex;align-items:center;gap:14px;width:100%;padding:14px 18px;background:none;border:none;cursor:pointer;font-family:inherit;text-align:left"
									>
										<!-- Date badge -->
										<div style="flex-shrink:0;text-align:center;min-width:48px">
											<div style="font-size:18px;font-weight:700;color:var(--primary);line-height:1">{new Date(c.date).getDate()}</div>
											<div style="font-size:10.5px;color:var(--text-muted);text-transform:uppercase;margin-top:1px">
												{new Date(c.date).toLocaleDateString('fr-FR', { month: 'short' })}
											</div>
										</div>
										<div style="width:1px;height:36px;background:var(--border);flex-shrink:0"></div>
										<div style="flex:1;min-width:0">
											<div style="font-size:13.5px;font-weight:600;color:var(--text);margin-bottom:3px">
												{c.reason || 'Consultation'}
											</div>
											{#if c.diagnosis}
												<div style="font-size:12px;color:var(--text-muted);overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{c.diagnosis}</div>
											{/if}
										</div>
										<div style="display:flex;align-items:center;gap:10px;flex-shrink:0">
											{#if c.tariff > 0}
												<span style="font-size:12.5px;font-weight:600;color:var(--primary)">{fmt.format(c.tariff)} DA</span>
											{/if}
											{#if c.finalized}
												<Badge variant="success">Finalisée</Badge>
											{:else}
												<Badge variant="warning">Brouillon</Badge>
											{/if}
											<Icon
												name={expandedId === c.id ? 'chevronDown' : 'chevronRight'}
												size={14}
												color="var(--text-muted)"
											/>
										</div>
									</button>

									{#if expandedId === c.id}
										<div style="border-top:1px solid var(--border);padding:16px 18px;background:var(--bg)">
											{#if detailLoading}
												<div style="text-align:center;color:var(--text-muted);font-size:13px;padding:20px 0">Chargement…</div>
											{:else if expandedDetail}
												<!-- Vitals -->
												{#if expandedDetail.vitalSigns && Object.values(expandedDetail.vitalSigns).some(v => v)}
													{@const vs = expandedDetail.vitalSigns}
													<div style="margin-bottom:14px">
														<div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px">Constantes</div>
														<div style="display:flex;flex-wrap:wrap;gap:8px">
															{#each [
																{ label: 'Tension', val: vs.bloodPressure },
																{ label: 'Pouls',   val: vs.pulseRate },
																{ label: 'Temp.',   val: vs.temperature },
																{ label: 'Poids',   val: vs.weight },
																{ label: 'Taille',  val: vs.height },
																{ label: 'SatO₂',   val: vs.spO2 },
															].filter(x => x.val) as v}
																<div style="background:var(--surface);border:1px solid var(--border);border-radius:6px;padding:6px 10px">
																	<div style="font-size:10px;color:var(--text-muted)">{v.label}</div>
																	<div style="font-size:13px;font-weight:600">{v.val}</div>
																</div>
															{/each}
														</div>
													</div>
												{/if}

												<!-- Prescription -->
												{#if expandedDetail.prescription?.length > 0}
													<div>
														<div style="font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px">Ordonnance</div>
														<div style="display:flex;flex-direction:column;gap:6px">
															{#each expandedDetail.prescription as line}
																<div style="background:var(--surface);border:1px solid var(--border);border-radius:7px;padding:10px 12px;display:flex;align-items:center;gap:10px">
																	<Icon name="fileText" size={14} color="var(--primary)" />
																	<div style="flex:1">
																		<div style="font-size:13px;font-weight:600">{line.medication}</div>
																		<div style="font-size:12px;color:var(--text-muted);margin-top:2px">
																			{[line.dosage, line.frequency, line.duration].filter(Boolean).join(' · ')}
																			{#if line.quantity > 0} · {line.quantity} boîte{line.quantity > 1 ? 's' : ''}{/if}
																		</div>
																	</div>
																</div>
															{/each}
														</div>
													</div>
												{/if}
											{:else}
												<!-- Show summary data if detail fetch failed -->
												<p style="font-size:13px;color:var(--text-muted)">Détail non disponible.</p>
											{/if}
										</div>
									{/if}
								</div>
							{/each}
						</div>
					{/if}

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
