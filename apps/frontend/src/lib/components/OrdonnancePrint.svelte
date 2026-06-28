<script lang="ts">
	import Icon from '$lib/components/Icon.svelte';
	import type { Med, OrdonnancePatient } from '$lib/types/consultation';

	let {
		patient,
		medications,
		doctorName = '',
		today,
		onClose
	}: {
		patient: OrdonnancePatient;
		medications: Med[];
		doctorName?: string;
		today: string;
		onClose: () => void;
	} = $props();

	const validMeds = $derived(medications.filter((m) => m.medication.trim()));

	function doPrint() {
		window.print();
	}
</script>

<div
	class="overlay no-print"
	role="presentation"
	onclick={(e) => {
		if (e.target === e.currentTarget) onClose();
	}}
>
	<div class="modal" role="dialog" aria-modal="true" aria-label="Aperçu de l'ordonnance">
		<div class="bar no-print">
			<h3>Aperçu avant impression</h3>
			<div class="bar-actions">
				<button type="button" class="btn-print" onclick={doPrint}>
					<Icon name="printer" size={18} color="white" /> Imprimer
				</button>
				<button type="button" class="btn-close" onclick={onClose} aria-label="Fermer">
					<Icon name="x" size={20} color="var(--text-muted)" />
				</button>
			</div>
		</div>

		<div class="scroll">
			<!-- print-area : la feuille seule est visible à l'impression (voir :global ci-dessous) -->
			<div class="sheet print-area">
				<header class="sheet-head">
					<div>
						<div class="doc-name">{doctorName ? `Dr. ${doctorName}` : 'Le médecin'}</div>
						<div class="doc-sub">Médecin</div>
					</div>
					<div class="sheet-brand">
						<div class="logo">Medi<span>Ka</span></div>
						<div class="sheet-date">Le {today}</div>
					</div>
				</header>

				<div class="patient-box">
					<div class="patient-line">
						Patient&nbsp;: <strong>{patient.firstName} {patient.lastName}</strong>
					</div>
					<div class="patient-sub">
						Âge&nbsp;: {patient.age} ans
						{#if patient.bloodGroup}
							· Groupe sanguin&nbsp;: <strong class="blood">{patient.bloodGroup}</strong>
						{/if}
						{#if patient.allergies.length}
							<span class="allergy">⚠ Allergie&nbsp;: {patient.allergies.join(', ')}</span>
						{/if}
					</div>
				</div>

				<div class="sheet-title">ORDONNANCE MÉDICALE</div>

				{#if validMeds.length === 0}
					<p class="empty">Aucun médicament renseigné.</p>
				{:else}
					{#each validMeds as med, i (med.id)}
						<div class="rx-line">
							<span class="rx-no">{i + 1}.</span>
							<div>
								<div class="rx-name">{med.medication}</div>
								{#if med.dosage.trim()}
									<div class="rx-pos">{med.dosage}</div>
								{/if}
								{#if med.duration.trim()}
									<div class="rx-meta">Durée&nbsp;: {med.duration}</div>
								{/if}
							</div>
						</div>
					{/each}
				{/if}

				<div class="signature">
					<div class="sig-box">
						<div class="sig-space"></div>
						<div class="sig-label">Signature &amp; Cachet du médecin</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</div>

<style>
	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.55);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 700;
		padding: 20px;
	}
	.modal {
		background: white;
		width: 720px;
		max-width: 100%;
		max-height: 92vh;
		border-radius: 12px;
		overflow: hidden;
		display: flex;
		flex-direction: column;
		box-shadow: 0 24px 64px rgba(0, 0, 0, 0.3);
	}
	.bar {
		padding: 14px 20px;
		border-bottom: 1px solid var(--border);
		display: flex;
		justify-content: space-between;
		align-items: center;
		background: var(--bg);
		flex-shrink: 0;
	}
	.bar h3 {
		font-weight: 700;
		font-size: 15px;
	}
	.bar-actions {
		display: flex;
		gap: 8px;
		align-items: center;
	}
	.btn-print {
		display: flex;
		align-items: center;
		gap: 8px;
		background: var(--accent);
		color: white;
		border: none;
		border-radius: 9px;
		padding: 10px 20px;
		cursor: pointer;
		font-family: inherit;
		font-size: 14px;
		font-weight: 700;
		box-shadow: 0 2px 10px rgba(217, 119, 6, 0.35);
	}
	.btn-close {
		background: none;
		border: none;
		cursor: pointer;
		padding: 6px;
		display: flex;
	}
	.scroll {
		overflow: auto;
		flex: 1;
		padding: 28px 32px;
		background: #f0ede8;
	}
	.sheet {
		background: white;
		padding: 36px;
		max-width: 600px;
		margin: 0 auto;
		border-radius: 4px;
		box-shadow: 0 2px 16px rgba(0, 0, 0, 0.12);
	}
	.sheet-head {
		display: flex;
		justify-content: space-between;
		border-bottom: 2.5px solid var(--primary);
		padding-bottom: 16px;
		margin-bottom: 22px;
	}
	.doc-name {
		font-weight: 700;
		font-size: 17px;
		color: var(--primary);
	}
	.doc-sub {
		font-size: 13px;
		color: #555;
		margin-top: 3px;
	}
	.sheet-brand {
		text-align: right;
	}
	.logo {
		font-weight: 800;
		font-size: 22px;
		letter-spacing: -0.5px;
		color: var(--primary);
	}
	.logo span {
		color: #0d9488;
	}
	.sheet-date {
		font-size: 13px;
		color: #888;
		margin-top: 8px;
	}
	.patient-box {
		background: #f8f7f4;
		border-radius: 7px;
		padding: 11px 16px;
		margin-bottom: 22px;
		border: 1px solid var(--border);
	}
	.patient-line {
		font-weight: 600;
		font-size: 14px;
	}
	.patient-sub {
		font-size: 13px;
		color: #555;
		margin-top: 3px;
	}
	.patient-sub .blood {
		color: #dc2626;
	}
	.patient-sub .allergy {
		color: #dc2626;
		margin-left: 10px;
	}
	.sheet-title {
		text-align: center;
		font-size: 15px;
		font-weight: 700;
		margin-bottom: 20px;
		text-decoration: underline;
		text-underline-offset: 3px;
	}
	.empty {
		color: #999;
		font-style: italic;
		font-size: 13px;
	}
	.rx-line {
		display: flex;
		gap: 10px;
		margin-bottom: 18px;
	}
	.rx-no {
		font-weight: 700;
		font-size: 15px;
		color: var(--primary);
		flex-shrink: 0;
	}
	.rx-name {
		font-weight: 700;
		font-size: 15px;
	}
	.rx-pos {
		font-size: 13.5px;
		color: #333;
		margin-top: 3px;
	}
	.rx-meta {
		font-size: 12.5px;
		color: #777;
		margin-top: 2px;
	}
	.signature {
		margin-top: 44px;
		padding-top: 18px;
		border-top: 1px solid #ddd;
		display: flex;
		justify-content: flex-end;
	}
	.sig-box {
		text-align: center;
		width: 180px;
	}
	.sig-space {
		height: 52px;
	}
	.sig-label {
		border-top: 1px solid #555;
		padding-top: 6px;
		font-size: 12px;
		color: #555;
	}

	/* À l'impression : seule la feuille (.print-area) reste visible. */
	@media print {
		:global(body * ) {
			visibility: hidden !important;
		}
		.print-area,
		:global(.print-area *) {
			visibility: visible !important;
		}
		.print-area {
			position: absolute;
			left: 0;
			top: 0;
			margin: 0;
			max-width: 100%;
			box-shadow: none;
			border-radius: 0;
			padding: 0;
		}
		.overlay {
			position: static;
			background: none;
			padding: 0;
		}
		.scroll {
			padding: 0;
			background: none;
			overflow: visible;
		}
	}
</style>
