<script lang="ts">
	import { goto } from '$app/navigation';
	import type { PageData } from './$types';
	import type { PatientSummary, PatientDetail, ConsultationSummary, ConsultationDetail, PatientInvoice } from '$lib/types/api';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import Badge from '$lib/components/Badge.svelte';

	let { data }: { data: PageData } = $props();

	let selectedPatient = $state<PatientSummary | null>(null);
	let activeTab = $state<'infos' | 'consultations' | 'ordonnances' | 'facturation'>('infos');

	// Full patient record (loaded on select)
	let detail        = $state<PatientDetail | null>(null);
	let detailLoad    = $state(false);
	let detailErr     = $state('');

	// Load the full record whenever a patient is selected
	$effect(() => {
		const patientId = selectedPatient?.id;
		if (!patientId) { detail = null; return; }

		detail     = null;
		detailErr  = '';
		detailLoad = true;

		fetch(`/api/patients/${patientId}`)
			.then(async (res) => {
				if (!res.ok) throw new Error(`Erreur ${res.status}`);
				detail = await res.json();
			})
			.catch((e: Error) => { detailErr = e.message || 'Erreur lors du chargement.'; })
			.finally(() => { detailLoad = false; });
	});

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
		if ((activeTab !== 'consultations' && activeTab !== 'ordonnances') || !patientId) return;

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

	// --- Ordonnances: consultations that carry a prescription ---
	const prescribedConsultations = $derived(consultations.filter(c => c.prescriptionCount > 0));

	// --- Facturation ---
	let invoices         = $state<PatientInvoice[]>([]);
	let invoicesLoading  = $state(false);
	let invoicesError    = $state('');

	const PAYMENT_LABELS: Record<string, string> = {
		Cash:         'Espèces',
		BankTransfer: 'Virement',
		Check:        'Chèque',
		Other:        'Autre',
	};

	function loadInvoices(patientId: string) {
		invoices        = [];
		invoicesError   = '';
		invoicesLoading = true;
		fetch(`/api/patients/${patientId}/invoices`)
			.then(async (res) => {
				if (!res.ok) throw new Error(`Erreur ${res.status}`);
				invoices = await res.json();
			})
			.catch((e: Error) => { invoicesError = e.message || 'Erreur lors du chargement.'; })
			.finally(() => { invoicesLoading = false; });
	}

	$effect(() => {
		const patientId = selectedPatient?.id;
		if (activeTab !== 'facturation' || !patientId) return;
		loadInvoices(patientId);
	});

	// Payment modal
	let payInvoice    = $state<PatientInvoice | null>(null);
	let payMethod     = $state<'Cash' | 'BankTransfer' | 'Check' | 'Other'>('Cash');
	let paySubmitting = $state(false);
	let payError      = $state('');

	function openPayModal(inv: PatientInvoice) {
		payInvoice    = inv;
		payMethod     = 'Cash';
		payError      = '';
		paySubmitting = false;
	}

	async function confirmPayment() {
		if (!payInvoice) return;
		paySubmitting = true;
		payError      = '';
		try {
			const res = await fetch(`/api/invoices/${payInvoice.id}/pay`, {
				method: 'PATCH',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ paymentMethod: payMethod }),
			});
			if (!res.ok) {
				const body = await res.json().catch(() => ({}));
				throw new Error(body?.error || `Erreur ${res.status}`);
			}
			payInvoice = null;
			if (selectedPatient) loadInvoices(selectedPatient.id);
		} catch (e) {
			payError = e instanceof Error ? e.message : 'Erreur lors de l\'encaissement.';
		} finally {
			paySubmitting = false;
		}
	}
</script>

<div style="display:flex;height:calc(100vh - 58px);overflow:hidden">

	<!-- Left sidebar -->
	<div style="width:280px;border-right:1px solid var(--border);background:var(--surface);display:flex;flex-direction:column;flex-shrink:0;overflow:hidden">

		<!-- Header + search -->
		<div style="padding:16px;border-bottom:1px solid var(--border)">
			<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px">
				<div style="display:flex;align-items:center;gap:8px">
					<span style="font-size:14.5px;font-weight:600">Patients</span>
					<span style="font-size:12px;color:var(--text-muted)">· {data.result.totalCount} au total</span>
				</div>
				<a href="/patients/new" style="display:inline-flex;align-items:center;gap:6px;padding:6px 12px;background:var(--primary);color:white;border-radius:7px;text-decoration:none;font-size:12.5px;font-weight:600">
					<Icon name="plus" size={13} color="white" />
					Nouveau
				</a>
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
					{#if detailLoad}
						<div style="display:flex;align-items:center;justify-content:center;padding:60px 0;color:var(--text-muted);gap:10px">
							<Icon name="activity" size={20} color="var(--border-strong)" />
							<span style="font-size:13.5px">Chargement de la fiche…</span>
						</div>
					{:else if detailErr}
						<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;max-width:480px">
							<Icon name="alertCircle" size={15} color="var(--danger)" />
							<span style="font-size:13.5px;color:var(--danger)">{detailErr}</span>
						</div>
					{:else if detail}
						<div style="max-width:760px;display:flex;flex-direction:column;gap:20px">

							<!-- État civil & contact -->
							<section>
								<div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.6px;margin-bottom:10px">État civil &amp; contact</div>
								<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
									{#each [
										{ label: 'Prénom', val: detail.firstName },
										{ label: 'Nom', val: detail.lastName },
										{ label: 'Date de naissance', val: detail.dateOfBirth ? formatDate(detail.dateOfBirth) : '—' },
										{ label: 'Âge', val: `${detail.age} ans` },
										{ label: 'Sexe', val: detail.gender === 'F' ? 'Femme' : 'Homme' },
										{ label: 'Téléphone', val: detail.phone },
										{ label: 'Email', val: detail.email || '—' },
										{ label: 'Adresse', val: detail.address || '—' },
										{ label: 'Wilaya', val: detail.wilaya || '—' }
									] as field}
										<div class="card" style="padding:14px 16px">
											<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">{field.label}</div>
											<div style="font-size:14px;font-weight:500">{field.val}</div>
										</div>
									{/each}
								</div>
							</section>

							<!-- Données médicales -->
							<section>
								<div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.6px;margin-bottom:10px">Données médicales</div>
								<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
									<div class="card" style="padding:14px 16px">
										<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">Groupe sanguin</div>
										<div style="font-size:14px;font-weight:500">{detail.bloodGroup ?? '—'}</div>
									</div>
									<div class="card" style="padding:14px 16px">
										<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">Dernière visite</div>
										<div style="font-size:14px;font-weight:500">{formatLastVisit(detail.lastVisitAt)}</div>
									</div>
									<div class="card" style="padding:14px 16px;grid-column:1/-1">
										<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:7px">Allergies</div>
										{#if detail.allergies.length > 0}
											<div style="display:flex;flex-wrap:wrap;gap:6px">
												{#each detail.allergies as a}
													<span style="background:var(--warning-light);color:var(--warning);font-size:12.5px;font-weight:500;padding:3px 10px;border-radius:20px">⚠ {a}</span>
												{/each}
											</div>
										{:else}
											<div style="font-size:14px;font-weight:500;color:var(--text-muted)">Aucune allergie connue</div>
										{/if}
									</div>
									<div class="card" style="padding:14px 16px;grid-column:1/-1">
										<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:7px">Antécédents médicaux</div>
										{#if detail.medicalHistory.length > 0}
											<ul style="margin:0;padding-left:18px;display:flex;flex-direction:column;gap:4px">
												{#each detail.medicalHistory as h}
													<li style="font-size:13.5px">{h}</li>
												{/each}
											</ul>
										{:else}
											<div style="font-size:14px;font-weight:500;color:var(--text-muted)">Aucun antécédent renseigné</div>
										{/if}
									</div>
									<div class="card" style="padding:14px 16px;grid-column:1/-1">
										<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">Traitement en cours</div>
										<div style="font-size:14px;font-weight:500">{detail.currentTreatment || 'Aucun'}</div>
									</div>
								</div>
							</section>

							<!-- Assurance & urgence -->
							<section>
								<div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.6px;margin-bottom:10px">Assurance &amp; contact d'urgence</div>
								<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
									{#each [
										{ label: 'N° sécurité sociale', val: detail.nss || '—' },
										{ label: 'Assurance', val: detail.insuranceProvider || '—' },
										{ label: 'Mutuelle', val: detail.mutualInsurance || '—' },
										{ label: 'Contact d\'urgence', val: detail.emergencyContactName || '—' },
										{ label: 'Tél. d\'urgence', val: detail.emergencyContactPhone || '—' }
									] as field}
										<div class="card" style="padding:14px 16px">
											<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">{field.label}</div>
											<div style="font-size:14px;font-weight:500">{field.val}</div>
										</div>
									{/each}
								</div>
							</section>

						</div>
					{/if}

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
										onclick={() => toggleConsultation(c.consultationId)}
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
											{#if c.isFinalized}
												<Badge variant="success">Finalisée</Badge>
											{:else}
												<Badge variant="warning">Brouillon</Badge>
											{/if}
											<Icon
												name={expandedId === c.consultationId ? 'chevronDown' : 'chevronRight'}
												size={14}
												color="var(--text-muted)"
											/>
										</div>
									</button>

									{#if expandedId === c.consultationId}
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

				{:else if activeTab === 'ordonnances'}
					{#if consultLoading}
						<div style="display:flex;align-items:center;justify-content:center;padding:60px 0;color:var(--text-muted);gap:10px">
							<Icon name="activity" size={20} color="var(--border-strong)" />
							<span style="font-size:13.5px">Chargement des ordonnances…</span>
						</div>
					{:else if consultError}
						<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;max-width:480px">
							<Icon name="alertCircle" size={15} color="var(--danger)" />
							<span style="font-size:13.5px;color:var(--danger)">{consultError}</span>
						</div>
					{:else if prescribedConsultations.length === 0}
						<div style="display:flex;flex-direction:column;align-items:center;padding:60px 20px;text-align:center;color:var(--text-muted)">
							<Icon name="fileText" size={40} color="var(--border-strong)" />
							<p style="margin-top:14px;font-size:14.5px;font-weight:500">Aucune ordonnance pour ce patient.</p>
							<p style="margin-top:6px;font-size:13px">Les ordonnances apparaissent après une consultation avec prescription.</p>
						</div>
					{:else}
						<div style="max-width:720px;display:flex;flex-direction:column;gap:10px">
							{#each prescribedConsultations as c}
								<div class="card" style="overflow:hidden">
									<button
										type="button"
										onclick={() => toggleConsultation(c.consultationId)}
										style="display:flex;align-items:center;gap:14px;width:100%;padding:14px 18px;background:none;border:none;cursor:pointer;font-family:inherit;text-align:left"
									>
										<div style="flex-shrink:0;text-align:center;min-width:48px">
											<div style="font-size:18px;font-weight:700;color:var(--primary);line-height:1">{new Date(c.date).getDate()}</div>
											<div style="font-size:10.5px;color:var(--text-muted);text-transform:uppercase;margin-top:1px">
												{new Date(c.date).toLocaleDateString('fr-FR', { month: 'short' })}
											</div>
										</div>
										<div style="width:1px;height:36px;background:var(--border);flex-shrink:0"></div>
										<div style="flex:1;min-width:0">
											<div style="font-size:13.5px;font-weight:600;color:var(--text);margin-bottom:3px">{c.reason || 'Consultation'}</div>
											<div style="font-size:12px;color:var(--text-muted)">{c.prescriptionCount} médicament{c.prescriptionCount > 1 ? 's' : ''}</div>
										</div>
										<Icon
											name={expandedId === c.consultationId ? 'chevronDown' : 'chevronRight'}
											size={14}
											color="var(--text-muted)"
										/>
									</button>

									{#if expandedId === c.consultationId}
										<div style="border-top:1px solid var(--border);padding:16px 18px;background:var(--bg)">
											{#if detailLoading}
												<div style="text-align:center;color:var(--text-muted);font-size:13px;padding:20px 0">Chargement…</div>
											{:else if expandedDetail && expandedDetail.prescription?.length > 0}
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
											{:else}
												<p style="font-size:13px;color:var(--text-muted)">Détail non disponible.</p>
											{/if}
										</div>
									{/if}
								</div>
							{/each}
						</div>
					{/if}

				{:else}
					{#if invoicesLoading}
						<div style="display:flex;align-items:center;justify-content:center;padding:60px 0;color:var(--text-muted);gap:10px">
							<Icon name="activity" size={20} color="var(--border-strong)" />
							<span style="font-size:13.5px">Chargement des factures…</span>
						</div>
					{:else if invoicesError}
						<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;max-width:480px">
							<Icon name="alertCircle" size={15} color="var(--danger)" />
							<span style="font-size:13.5px;color:var(--danger)">{invoicesError}</span>
						</div>
					{:else if invoices.length === 0}
						<div style="display:flex;flex-direction:column;align-items:center;padding:60px 20px;text-align:center;color:var(--text-muted)">
							<Icon name="fileText" size={40} color="var(--border-strong)" />
							<p style="margin-top:14px;font-size:14.5px;font-weight:500">Aucune facture pour ce patient.</p>
						</div>
					{:else}
						<div style="max-width:760px">
							<table class="mk-table">
								<thead>
									<tr>
										<th>N°</th>
										<th>Date</th>
										<th>Statut</th>
										<th style="text-align:right">Montant</th>
										<th style="text-align:right">Action</th>
									</tr>
								</thead>
								<tbody>
									{#each invoices as inv}
										<tr>
											<td style="font-size:13px;font-weight:600;white-space:nowrap">{inv.number}</td>
											<td style="font-size:13px;color:var(--text-muted);white-space:nowrap">{formatDate(inv.issuedAt)}</td>
											<td>
												{#if inv.status === 'Paid'}
													<Badge variant="success">Payée{inv.paymentMethod ? ` · ${PAYMENT_LABELS[inv.paymentMethod]}` : ''}</Badge>
												{:else if inv.status === 'Cancelled'}
													<Badge variant="danger">Annulée</Badge>
												{:else}
													<Badge variant="warning">En attente</Badge>
												{/if}
											</td>
											<td style="text-align:right;font-size:13.5px;font-weight:600;white-space:nowrap">{fmt.format(inv.amount)} DA</td>
											<td style="text-align:right;white-space:nowrap">
												{#if inv.status === 'Pending'}
													<button
														type="button"
														onclick={() => openPayModal(inv)}
														style="display:inline-flex;align-items:center;gap:5px;padding:5px 11px;background:var(--primary);color:white;border:none;border-radius:6px;font-family:inherit;font-size:12.5px;font-weight:600;cursor:pointer"
													>
														<Icon name="dollar" size={12} color="white" />
														Encaisser
													</button>
												{:else}
													<span style="font-size:12.5px;color:var(--text-light)">—</span>
												{/if}
											</td>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{/if}
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

{#if payInvoice}
	<div
		style="position:fixed;inset:0;background:rgba(15,23,42,0.45);z-index:150;display:flex;align-items:center;justify-content:center;padding:20px"
		onclick={() => { if (!paySubmitting) payInvoice = null; }}
		onkeydown={(e) => { if (e.key === 'Escape' && !paySubmitting) payInvoice = null; }}
		role="presentation"
	>
		<div
			class="card"
			style="width:100%;max-width:400px;padding:24px"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
			role="dialog"
			aria-modal="true"
			tabindex="-1"
		>
			<h2 style="font-size:16px;font-weight:700;margin-bottom:4px">Encaisser la facture</h2>
			<p style="font-size:13px;color:var(--text-muted);margin-bottom:18px">
				{payInvoice.number} · <strong style="color:var(--text)">{fmt.format(payInvoice.amount)} DA</strong>
			</p>

			{#if payError}
				<div style="display:flex;align-items:center;gap:8px;padding:10px 14px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;margin-bottom:14px">
					<Icon name="alertCircle" size={14} color="var(--danger)" />
					<span style="font-size:13px;color:var(--danger)">{payError}</span>
				</div>
			{/if}

			<label for="pay-method" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:6px">Mode de paiement</label>
			<select id="pay-method" bind:value={payMethod} class="mk-input" style="margin-bottom:20px">
				<option value="Cash">Espèces</option>
				<option value="BankTransfer">Virement</option>
				<option value="Check">Chèque</option>
				<option value="Other">Autre</option>
			</select>

			<div style="display:flex;gap:10px;justify-content:flex-end">
				<button
					type="button"
					disabled={paySubmitting}
					onclick={() => payInvoice = null}
					style="padding:9px 18px;background:var(--bg);color:var(--text);border:1px solid var(--border);border-radius:7px;font-family:inherit;font-size:13.5px;cursor:pointer"
				>
					Annuler
				</button>
				<button
					type="button"
					disabled={paySubmitting}
					onclick={confirmPayment}
					style="padding:9px 22px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer;opacity:{paySubmitting ? 0.6 : 1}"
				>
					{paySubmitting ? 'Encaissement…' : 'Confirmer'}
				</button>
			</div>
		</div>
	</div>
{/if}
