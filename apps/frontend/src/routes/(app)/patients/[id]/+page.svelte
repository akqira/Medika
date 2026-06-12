<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import type { PageData } from './$types';
	import type { ConsultationDetail, PatientInvoice } from '$lib/types/api';
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
		<a href="/consultation?patientId={patient.id}"
			style="display:inline-flex;align-items:center;gap:7px;padding:9px 16px;background:var(--primary);color:white;border-radius:8px;text-decoration:none;font-size:13.5px;font-weight:600">
			<Icon name="stethoscope" size={15} color="white" />
			Ajouter une consultation
		</a>
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
									<Icon name="alertCircle" size={13} color="var(-