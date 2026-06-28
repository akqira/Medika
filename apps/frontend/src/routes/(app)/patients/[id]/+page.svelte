<script lang="ts">
	import { onMount } from 'svelte';
	import { invalidateAll, replaceState } from '$app/navigation';
	import { page as pageState } from '$app/state';
	import type { PageData } from './$types';
	import type { ConsultationDetail, PatientInvoice } from '$lib/types/api';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import Badge from '$lib/components/Badge.svelte';
	import { toast } from '$lib/stores/toast.svelte';

	let { data }: { data: PageData } = $props();

	const patient = $derived(data.patient);
	const consultations = $derived(data.consultations);
	const invoices = $derived(data.invoices);

	// The edit action redirects back here with `?toast=patient-updated` — surface the
	// confirmation once on mount, then strip the param so refresh/back doesn't re-fire it.
	onMount(() => {
		if (pageState.url.searchParams.get('toast') !== 'patient-updated') return;
		toast.success('Dossier patient mis à jour.');
		const url = new URL(pageState.url);
		url.searchParams.delete('toast');
		replaceState(url, {});
	});

	const fmt = new Intl.NumberFormat('fr-DZ', { maximumFractionDigits: 0 });

	function formatDate(iso: string | undefined) {
		if (!iso) return '—';
		return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
	}
	function formatLastVisit(iso: string | undefined) {
		if (!iso) return 'Aucune visite';
		return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' });
	}
	function monthShort(iso: string) {
		return new Date(iso).toLocaleDateString('fr-FR', { month: 'short' }).replace('.', '');
	}

	const PAYMENT_LABELS: Record<string, string> = {
		Cash: 'Espèces', BankTransfer: 'Virement', Check: 'Chèque', Other: 'Autre'
	};

	// Civil / contact fields — only those with a value are shown.
	const identityFields = $derived(
		[
			{ label: 'Date de naissance', val: formatDate(patient.dateOfBirth) },
			{ label: 'Sexe', val: patient.gender === 'F' ? 'Femme' : 'Homme' },
			{ label: 'Téléphone', val: patient.phone },
			{ label: 'Email', val: patient.email },
			{ label: 'Adresse', val: patient.address },
			{ label: 'Wilaya', val: patient.wilaya },
			{ label: 'NSS', val: patient.nss },
			{ label: 'Assurance', val: patient.insuranceProvider },
			{ label: 'Mutuelle', val: patient.mutualInsurance },
			{ label: "Contact d'urgence", val: patient.emergencyContactName },
			{ label: "Tél. d'urgence", val: patient.emergencyContactPhone }
		].filter((f) => f.val)
	);

	const hasBackground = $derived(
		patient.allergies.length > 0 || patient.medicalHistory.length > 0 || !!patient.currentTreatment
	);

	// ── Consultation detail: lazy-loaded on click, cached per id ──
	type DetailState = ConsultationDetail | 'loading' | 'error';
	let expandedId = $state<string | null>(null);
	let detailCache = $state<Record<string, DetailState>>({});

	async function toggleConsultation(id: string) {
		if (expandedId === id) { expandedId = null; return; }
		expandedId = id;
		if (detailCache[id] && detailCache[id] !== 'error') return;
		detailCache = { ...detailCache, [id]: 'loading' };
		try {
			const res = await fetch(`/api/patients/${patient.id}/consultations/${id}`);
			if (!res.ok) throw new Error('load failed');
			const detail = (await res.json()) as ConsultationDetail;
			detailCache = { ...detailCache, [id]: detail };
		} catch {
			detailCache = { ...detailCache, [id]: 'error' };
		}
	}

	// ── Encaissement (espèces uniquement) ──
	let payInvoice = $state<PatientInvoice | null>(null);
	let paySubmitting = $state(false);
	let payError = $state('');

	function openPay(inv: PatientInvoice) {
		payInvoice = inv;
		payError = '';
		paySubmitting = false;
	}

	async function confirmPayment() {
		if (!payInvoice) return;
		paySubmitting = true;
		payError = '';
		try {
			const res = await fetch(`/api/invoices/${payInvoice.id}/pay`, {
				method: 'PATCH',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ paymentMethod: 'Cash' })
			});
			if (!res.ok) {
				const body = await res.json().catch(() => ({}));
				throw new Error(body?.error || `Erreur ${res.status}`);
			}
			payInvoice = null;
			await invalidateAll(); // reload the dossier so the invoice flips to Payée
		} catch (e) {
			payError = e instanceof Error ? e.message : "Erreur lors de l'encaissement.";
		} finally {
			paySubmitting = false;
		}
	}

	function vitalList(v: ConsultationDetail['vitalSigns']) {
		if (!v) return [];
		return [
			{ label: 'Tension', val: v.bloodPressure },
			{ label: 'Pouls', val: v.pulseRate },
			{ label: 'Temp.', val: v.temperature },
			{ label: 'Poids', val: v.weight },
			{ label: 'Taille', val: v.height },
			{ label: 'SpO₂', val: v.spO2 }
		].filter((x) => x.val);
	}
</script>

<svelte:head>
	<title>{patient.firstName} {patient.lastName} · Dossier patient</title>
</svelte:head>

<div style="max-width:1100px;margin:0 auto;padding:18px 24px">

	<!-- Top bar -->
	<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px;gap:12px">
		<a href="/patients" style="display:inline-flex;align-items:center;gap:6px;font-size:13px;color:var(--text-muted);text-decoration:none">
			← Retour aux patients
		</a>
		<div style="display:inline-flex;align-items:center;gap:10px">
			<a href="/patients/{patient.id}/edit"
				style="display:inline-flex;align-items:center;gap:7px;padding:9px 16px;background:var(--surface);color:var(--text);border:1px solid var(--border);border-radius:8px;text-decoration:none;font-size:13.5px;font-weight:600">
				<Icon name="edit" size={15} color="var(--text-muted)" />
				Modifier
			</a>
			<a href="/consultation?patientId={patient.id}"
				style="display:inline-flex;align-items:center;gap:7px;padding:9px 16px;background:var(--primary);color:white;border-radius:8px;text-decoration:none;font-size:13.5px;font-weight:600">
				<Icon name="stethoscope" size={15} color="white" />
				Ajouter une consultation
			</a>
		</div>
	</div>

	<!-- Dossier: 2 columns -->
	<div style="display:grid;grid-template-columns:312px 1fr;gap:20px;align-items:start">

		<!-- ── Left: identity ── -->
		<aside style="position:sticky;top:18px;display:flex;flex-direction:column;gap:14px">

			<div class="card" style="padding:22px 20px;text-align:center">
				<div style="display:flex;justify-content:center;margin-bottom:14px">
					<Avatar nom={patient.lastName} prenom={patient.firstName} sexe={patient.gender} size={76} />
				</div>
				<h1 style="font-size:18px;font-weight:700;margin:0;line-height:1.25">{patient.firstName} {patient.lastName}</h1>
				<div style="font-size:13px;color:var(--text-muted);margin-top:5px">
					{patient.age} ans · {patient.gender === 'F' ? 'Femme' : 'Homme'}
				</div>

				<div style="display:flex;justify-content:center;gap:8px;margin-top:12px;flex-wrap:wrap">
					{#if patient.bloodGroup}
						<span style="background:var(--danger-light);color:var(--danger);font-size:12px;font-weight:700;padding:3px 10px;border-radius:20px">{patient.bloodGroup}</span>
					{/if}
					{#if patient.allergies.length > 0}
						<span style="background:var(--warning-light);color:var(--warning);font-size:12px;font-weight:600;padding:3px 10px;border-radius:20px;display:inline-flex;align-items:center;gap:4px">
							<Icon name="alertCircle" size={12} color="var(--warning)" /> {patient.allergies.length} allergie{patient.allergies.length > 1 ? 's' : ''}
						</span>
					{/if}
				</div>

				<!-- Phone (visible, non-clickable) -->
				<div style="display:flex;align-items:center;justify-content:center;gap:7px;margin-top:16px;padding:9px;background:var(--bg);border:1px solid var(--border);border-radius:8px;font-size:13.5px;font-weight:500;user-select:text">
					<Icon name="phone" size={14} color="var(--text-muted)" />
					{patient.phone}
				</div>
			</div>

			<!-- Civil details as a clean list (not product tiles) -->
			<div class="card" style="padding:6px 18px">
				{#each identityFields as field, i}
					<div style="display:flex;justify-content:space-between;align-items:baseline;gap:12px;padding:11px 0;{i < identityFields.length - 1 ? 'border-bottom:1px solid var(--border)' : ''}">
						<span style="font-size:12.5px;color:var(--text-muted);flex-shrink:0">{field.label}</span>
						<span style="font-size:13px;font-weight:500;text-align:right;word-break:break-word">{field.val}</span>
					</div>
				{/each}
			</div>

			<div style="text-align:center;font-size:11.5px;color:var(--text-light)">
				Dossier créé le {formatDate(patient.createdAt)} · Dernière visite {formatLastVisit(patient.lastVisitAt)}
			</div>
		</aside>

		<!-- ── Right: clinical ── -->
		<main style="display:flex;flex-direction:column;gap:22px;min-width:0">

			<!-- Background: allergies / history / treatment -->
			{#if hasBackground}
				<section>
					<div class="sec-title">Antécédents &amp; allergies</div>
					<div class="card" style="padding:18px 20px;display:flex;flex-direction:column;gap:16px">
						{#if patient.allergies.length > 0}
							<div>
								<div style="font-size:12px;font-weight:600;color:var(--danger);margin-bottom:7px;display:flex;align-items:center;gap:6px">
									<Icon name="alertCircle" size={13} color="var(--danger)" /> Allergies
								</div>
								<div style="display:flex;flex-wrap:wrap;gap:6px">
									{#each patient.allergies as a}
										<span style="background:var(--danger-light);color:var(--danger);font-size:12.5px;font-weight:500;padding:4px 11px;border-radius:20px">{a}</span>
									{/each}
								</div>
							</div>
						{/if}
						{#if patient.medicalHistory.length > 0}
							<div>
								<div style="font-size:12px;font-weight:600;color:var(--text-muted);margin-bottom:7px">Antécédents médicaux</div>
								<div style="display:flex;flex-wrap:wrap;gap:6px">
									{#each patient.medicalHistory as h}
										<span style="background:var(--bg);border:1px solid var(--border);color:var(--text);font-size:12.5px;padding:4px 11px;border-radius:20px">{h}</span>
									{/each}
								</div>
							</div>
						{/if}
						{#if patient.currentTreatment}
							<div>
								<div style="font-size:12px;font-weight:600;color:var(--text-muted);margin-bottom:5px">Traitement en cours</div>
								<div style="font-size:13.5px;line-height:1.5">{patient.currentTreatment}</div>
							</div>
						{/if}
					</div>
				</section>
			{/if}

			<!-- Consultations -->
			<section>
				<div class="sec-title">Consultations {#if !consultations.failed}<span style="color:var(--text-light);font-weight:500">· {consultations.data.length}</span>{/if}</div>

				{#if consultations.failed}
					<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px">
						<Icon name="alertCircle" size={15} color="var(--danger)" />
						<span style="font-size:13.5px;color:var(--danger)">Impossible de charger les consultations.</span>
					</div>
				{:else if consultations.data.length === 0}
					<div class="card" style="display:flex;flex-direction:column;align-items:center;padding:36px 20px;text-align:center;color:var(--text-muted)">
						<Icon name="fileText" size={30} color="var(--border-strong)" />
						<p style="margin-top:10px;font-size:14px;font-weight:500">Aucune consultation</p>
						<a href="/consultation?patientId={patient.id}" style="margin-top:12px;display:inline-flex;align-items:center;gap:6px;padding:8px 15px;background:var(--primary);color:white;border-radius:7px;text-decoration:none;font-size:13px;font-weight:600">
							<Icon name="stethoscope" size={13} color="white" /> Démarrer la première consultation
						</a>
					</div>
				{:else}
					<div style="display:flex;flex-direction:column;gap:8px">
						{#each consultations.data as c}
							{@const open = expandedId === c.consultationId}
							{@const detail = detailCache[c.consultationId]}
							<div class="card" style="overflow:hidden;{open ? 'border-color:var(--primary-light)' : ''}">
								<!-- Row (clickable) -->
								<button type="button" onclick={() => toggleConsultation(c.consultationId)}
									style="display:flex;align-items:center;gap:14px;width:100%;padding:13px 18px;background:none;border:none;cursor:pointer;font-family:inherit;text-align:left">
									<div style="flex-shrink:0;text-align:center;min-width:42px">
										<div style="font-size:18px;font-weight:700;color:var(--primary);line-height:1">{new Date(c.date).getDate()}</div>
										<div style="font-size:10.5px;color:var(--text-muted);text-transform:uppercase;margin-top:1px">{monthShort(c.date)}</div>
									</div>
									<div style="width:1px;height:34px;background:var(--border);flex-shrink:0"></div>
									<div style="flex:1;min-width:0">
										<div style="font-size:13.5px;font-weight:600;color:var(--text);overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{c.reason || 'Consultation'}</div>
										{#if c.diagnosis}
											<div style="font-size:12px;color:var(--text-muted);margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{c.diagnosis}</div>
										{/if}
									</div>
									{#if (c.prescriptionCount ?? 0) > 0}
										<span title="Ordonnance" style="flex-shrink:0;color:var(--text-muted);display:inline-flex"><Icon name="fileText" size={15} color="var(--text-muted)" /></span>
									{/if}
									{#if c.isFinalized}
										<Badge variant="success">Finalisée</Badge>
									{:else}
										<Badge variant="warning">Brouillon</Badge>
									{/if}
									<Icon name="chevronDown" size={15} color="var(--text-muted)" style={open ? 'transform:rotate(180deg);transition:transform 0.15s' : 'transition:transform 0.15s'} />
								</button>

								<!-- Detail panel -->
								{#if open}
									<div style="border-top:1px solid var(--border);padding:16px 18px;background:var(--bg)">
										{#if detail === 'loading' || detail === undefined}
											<div style="font-size:13px;color:var(--text-muted);padding:8px 0">Chargement du détail…</div>
										{:else if detail === 'error'}
											<div style="font-size:13px;color:var(--danger)">Impossible de charger le détail de la consultation.</div>
										{:else}
											<!-- Constantes vitales -->
											{@const vitals = vitalList(detail.vitalSigns)}
											{#if vitals.length > 0}
												<div class="det-label">Constantes vitales</div>
												<div style="display:flex;flex-wrap:wrap;gap:8px;margin-bottom:16px">
													{#each vitals as v}
														<div style="background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:7px 12px;min-width:74px">
															<div style="font-size:10.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.4px">{v.label}</div>
															<div style="font-size:14px;font-weight:600;margin-top:1px">{v.val}</div>
														</div>
													{/each}
												</div>
											{/if}

											{#if detail.reason}
												<div class="det-label">Anamnèse / motif</div>
												<p class="det-text">{detail.reason}</p>
											{/if}
											{#if detail.clinicalExam}
												<div class="det-label">Examen clinique</div>
												<p class="det-text">{detail.clinicalExam}</p>
											{/if}
											{#if detail.notes}
												<div class="det-label">Notes complémentaires</div>
												<p class="det-text">{detail.notes}</p>
											{/if}

											<!-- Diagnostic + honoraires -->
											<div style="display:flex;flex-wrap:wrap;gap:24px;margin-top:6px;padding-top:14px;border-top:1px solid var(--border)">
												<div style="flex:1;min-width:180px">
													<div class="det-label" style="margin-top:0">Diagnostic</div>
													<p class="det-text" style="margin-bottom:0">{detail.diagnosis || '—'}</p>
												</div>
												<div>
													<div class="det-label" style="margin-top:0">Honoraires</div>
													<div style="font-size:16px;font-weight:700;color:var(--primary)">{fmt.format(detail.tariff)} DA</div>
												</div>
											</div>

											<!-- Ordonnance -->
											{#if detail.prescription.length > 0}
												<div style="margin-top:16px;padding-top:14px;border-top:1px solid var(--border)">
													<div style="display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:8px">
														<div class="det-label" style="margin-top:0">Ordonnance</div>
														<a href="/api/patients/{patient.id}/consultations/{c.consultationId}/ordonnance" target="_blank" rel="noopener"
															style="display:inline-flex;align-items:center;gap:6px;padding:6px 12px;background:var(--surface);border:1px solid var(--border);border-radius:7px;text-decoration:none;color:var(--text);font-size:12.5px;font-weight:600">
															<Icon name="printer" size={13} color="var(--primary)" /> Imprimer l'ordonnance
														</a>
													</div>
													<div style="display:flex;flex-direction:column;gap:6px">
														{#each detail.prescription as m}
															<div style="display:flex;align-items:baseline;gap:8px;background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:9px 12px">
																<Icon name="activity" size={13} color="var(--primary)" />
																<span style="font-size:13.5px;font-weight:600">{m.medication}{#if m.dosage}<span style="font-weight:400;color:var(--text-muted)"> · {m.dosage}</span>{/if}</span>
																<span style="flex:1"></span>
																<span style="font-size:12.5px;color:var(--text-muted);text-align:right">{[m.frequency, m.duration].filter(Boolean).join(' · ')}</span>
															</div>
														{/each}
													</div>
												</div>
											{/if}
										{/if}
									</div>
								{/if}
							</div>
						{/each}
					</div>
				{/if}
			</section>

			<!-- Facturation -->
			<section>
				<div class="sec-title">Facturation</div>
				{#if invoices.failed}
					<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px">
						<Icon name="alertCircle" size={15} color="var(--danger)" />
						<span style="font-size:13.5px;color:var(--danger)">Impossible de charger les factures.</span>
					</div>
				{:else if invoices.data.length === 0}
					<div class="card" style="display:flex;flex-direction:column;align-items:center;padding:32px 20px;text-align:center;color:var(--text-muted)">
						<Icon name="dollar" size={28} color="var(--border-strong)" />
						<p style="margin-top:10px;font-size:13.5px;font-weight:500">Aucune facture</p>
					</div>
				{:else}
					<div class="card" style="padding:0;overflow:hidden">
						<table class="mk-table">
							<thead>
								<tr><th>N°</th><th>Date</th><th>Statut</th><th style="text-align:right">Montant</th><th></th></tr>
							</thead>
							<tbody>
								{#each invoices.data as inv}
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
												<button type="button" onclick={() => openPay(inv)}
													style="display:inline-flex;align-items:center;gap:6px;padding:6px 12px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:12.5px;font-weight:600;cursor:pointer">
													<Icon name="wallet" size={13} color="white" /> Encaisser
												</button>
											{/if}
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</section>

		</main>
	</div>
</div>

<!-- Encaissement modal (espèces uniquement) -->
{#if payInvoice}
	<div
		role="presentation"
		onclick={() => { if (!paySubmitting) payInvoice = null; }}
		onkeydown={(e) => { if (e.key === 'Escape' && !paySubmitting) payInvoice = null; }}
		style="position:fixed;inset:0;background:rgba(15,23,42,0.45);z-index:150;display:flex;align-items:center;justify-content:center;padding:20px"
	>
		<div
			class="card"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
			style="width:100%;max-width:400px;padding:24px"
		>
			<h2 style="font-size:16px;font-weight:700;margin:0 0 4px">Encaisser la facture</h2>
			<p style="font-size:13px;color:var(--text-muted);margin:0 0 18px">
				{payInvoice.number} · <strong style="color:var(--text)">{fmt.format(payInvoice.amount)} DA</strong>
			</p>

			{#if payError}
				<div style="display:flex;align-items:center;gap:8px;padding:10px 14px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;margin-bottom:14px">
					<Icon name="alertCircle" size={14} color="var(--danger)" />
					<span style="font-size:13px;color:var(--danger)">{payError}</span>
				</div>
			{/if}

			<div style="font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:6px">Mode de paiement</div>
			<div style="display:flex;align-items:center;gap:8px;padding:10px 14px;background:var(--bg);border:1px solid var(--border);border-radius:8px;margin-bottom:20px">
				<Icon name="wallet" size={15} color="var(--primary)" />
				<span style="font-size:13.5px;font-weight:600;color:var(--text)">Espèces</span>
			</div>

			<div style="display:flex;gap:10px;justify-content:flex-end">
				<button type="button" disabled={paySubmitting} onclick={() => payInvoice = null}
					style="padding:9px 18px;background:var(--bg);color:var(--text);border:1px solid var(--border);border-radius:7px;font-family:inherit;font-size:13.5px;cursor:pointer">
					Annuler
				</button>
				<button type="button" disabled={paySubmitting} onclick={confirmPayment}
					style="padding:9px 22px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer;opacity:{paySubmitting ? 0.6 : 1}">
					{paySubmitting ? 'Encaissement…' : 'Confirmer'}
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.sec-title {
		font-size: 11px;
		font-weight: 700;
		color: var(--text-muted);
		text-transform: uppercase;
		letter-spacing: 0.6px;
		margin-bottom: 10px;
	}
	.det-label {
		font-size: 11px;
		font-weight: 700;
		color: var(--text-muted);
		text-transform: uppercase;
		letter-spacing: 0.5px;
		margin: 12px 0 5px;
	}
	.det-text {
		font-size: 13.5px;
		line-height: 1.55;
		color: var(--text);
		margin: 0 0 4px;
		white-space: pre-wrap;
	}
</style>
