<script lang="ts">
	import type { PageData } from './$types';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import Badge from '$lib/components/Badge.svelte';

	let { data }: { data: PageData } = $props();

	const patient = $derived(data.patient);
	const consultations = $derived(data.consultations);
	const invoices = $derived(data.invoices);

	const fmt = new Intl.NumberFormat('fr-DZ', { maximumFractionDigits: 0 });

	function formatDate(iso: string | undefined) {
		if (!iso) return '—';
		return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
	}
	function formatLastVisit(iso: string | undefined) {
		if (!iso) return 'Aucune visite';
		return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' });
	}

	const PAYMENT_LABELS: Record<string, string> = {
		Cash: 'Espèces',
		BankTransfer: 'Virement',
		Check: 'Chèque',
		Other: 'Autre'
	};

	// Contact / civil fields — only those with a value are shown (AC-3).
	const identityFields = $derived(
		[
			{ label: 'Date de naissance', val: formatDate(patient.dateOfBirth) },
			{ label: 'Sexe', val: patient.gender === 'F' ? 'Femme' : 'Homme' },
			{ label: 'Téléphone', val: patient.phone },
			{ label: 'Email', val: patient.email },
			{ label: 'Adresse', val: patient.address },
			{ label: 'Wilaya', val: patient.wilaya },
			{ label: "Contact d'urgence", val: patient.emergencyContactName },
			{ label: "Tél. d'urgence", val: patient.emergencyContactPhone },
			{ label: 'Assurance', val: patient.insuranceProvider }
		].filter((f) => f.val)
	);
</script>

<svelte:head>
	<title>{patient.firstName} {patient.lastName} · Dossier patient</title>
</svelte:head>

<div style="max-width:880px;margin:0 auto;padding:20px 24px">

	<a href="/patients" style="display:inline-flex;align-items:center;gap:6px;font-size:13px;color:var(--text-muted);text-decoration:none;margin-bottom:16px">
		← Retour aux patients
	</a>

	<!-- Header -->
	<div class="card" style="padding:20px 24px;margin-bottom:20px">
		<div style="display:flex;align-items:center;gap:16px">
			<Avatar nom={patient.lastName} prenom={patient.firstName} sexe={patient.gender} size={56} />
			<div style="flex:1;min-width:0">
				<h1 style="font-size:19px;font-weight:700;margin:0">{patient.firstName} {patient.lastName}</h1>
				<div style="display:flex;align-items:center;gap:10px;margin-top:6px;flex-wrap:wrap">
					<span style="font-size:13px;color:var(--text-muted)">{patient.age} ans</span>
					<span style="color:var(--border-strong)">·</span>
					<span style="font-size:13px;color:var(--text-muted)">{patient.gender === 'F' ? 'Femme' : 'Homme'}</span>
					<span style="color:var(--border-strong)">·</span>
					<span style="font-size:13px;color:var(--text-muted)">Dernière visite : {formatLastVisit(patient.lastVisitAt)}</span>
					{#if patient.bloodGroup}
						<span style="background:var(--danger-light);color:var(--danger);font-size:12px;font-weight:600;padding:2px 8px;border-radius:20px">{patient.bloodGroup}</span>
					{/if}
				</div>
			</div>
			<a href="tel:{patient.phone}" style="display:flex;align-items:center;gap:6px;padding:8px 14px;background:var(--bg);border:1px solid var(--border);border-radius:7px;text-decoration:none;color:var(--text);font-size:13px">
				<Icon name="phone" size={14} color="var(--text-muted)" />
				{patient.phone}
			</a>
		</div>
	</div>

	<!-- Identity / contact -->
	<section style="margin-bottom:24px">
		<div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.6px;margin-bottom:10px">État civil &amp; contact</div>
		<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
			{#each identityFields as field}
				<div class="card" style="padding:14px 16px">
					<div style="font-size:11.5px;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin-bottom:5px">{field.label}</div>
					<div style="font-size:14px;font-weight:500">{field.val}</div>
				</div>
			{/each}
		</div>
	</section>

	<!-- Consultations (role-shaped — diagnosis present only for doctors, ADR-002) -->
	<section style="margin-bottom:24px">
		<div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.6px;margin-bottom:10px">Consultations</div>
		{#if consultations.failed}
			<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px">
				<Icon name="alertCircle" size={15} color="var(--danger)" />
				<span style="font-size:13.5px;color:var(--danger)">Impossible de charger les consultations.</span>
			</div>
		{:else if consultations.data.length === 0}
			<div class="card" style="display:flex;flex-direction:column;align-items:center;padding:36px 20px;text-align:center;color:var(--text-muted)">
				<Icon name="fileText" size={32} color="var(--border-strong)" />
				<p style="margin-top:12px;font-size:14px;font-weight:500">Aucune consultation</p>
			</div>
		{:else}
			<div style="display:flex;flex-direction:column;gap:10px">
				{#each consultations.data as c}
					<div class="card" style="display:flex;align-items:center;gap:14px;padding:14px 18px">
						<div style="flex-shrink:0;text-align:center;min-width:48px">
							<div style="font-size:18px;font-weight:700;color:var(--primary);line-height:1">{new Date(c.date).getDate()}</div>
							<div style="font-size:10.5px;color:var(--text-muted);text-transform:uppercase;margin-top:1px">{new Date(c.date).toLocaleDateString('fr-FR', { month: 'short' })}</div>
						</div>
						<div style="width:1px;height:36px;background:var(--border);flex-shrink:0"></div>
						<div style="flex:1;min-width:0">
							{#if c.diagnosis}
								<div style="font-size:13.5px;font-weight:600;color:var(--text);margin-bottom:3px">{c.reason || 'Consultation'}</div>
								<div style="font-size:12px;color:var(--text-muted);overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{c.diagnosis}</div>
							{:else}
								<div style="font-size:13.5px;font-weight:600;color:var(--text)">Consultation effectuée</div>
							{/if}
						</div>
						{#if c.isFinalized}
							<Badge variant="success">Finalisée</Badge>
						{:else}
							<Badge variant="warning">Brouillon</Badge>
						{/if}
					</div>
				{/each}
			</div>
		{/if}
	</section>

	<!-- Invoices -->
	<section>
		<div style="font-size:11px;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.6px;margin-bottom:10px">Facturation</div>
		{#if invoices.failed}
			<div style="display:flex;align-items:center;gap:8px;padding:14px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px">
				<Icon name="alertCircle" size={15} color="var(--danger)" />
				<span style="font-size:13.5px;color:var(--danger)">Impossible de charger les factures.</span>
			</div>
		{:else if invoices.data.length === 0}
			<div class="card" style="display:flex;flex-direction:column;align-items:center;padding:36px 20px;text-align:center;color:var(--text-muted)">
				<Icon name="dollar" size={32} color="var(--border-strong)" />
				<p style="margin-top:12px;font-size:14px;font-weight:500">Aucune facture</p>
			</div>
		{:else}
			<table class="mk-table">
				<thead>
					<tr>
						<th>N°</th>
						<th>Date</th>
						<th>Statut</th>
						<th style="text-align:right">Montant</th>
					</tr>
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
						</tr>
					{/each}
				</tbody>
			</table>
		{/if}
	</section>
</div>
