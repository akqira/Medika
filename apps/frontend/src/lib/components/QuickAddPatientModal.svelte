<script lang="ts">
	import Icon from '$lib/components/Icon.svelte';
	import type { PatientSummary } from '$lib/types/api';

	let { onPatientAdded, onClose }: {
		onPatientAdded: (patient: PatientSummary) => void;
		onClose: () => void;
	} = $props();

	// Form state
	let firstName = $state('');
	let lastName  = $state('');
	let phone     = $state('');
	let dob       = $state('');
	let gender    = $state<'M' | 'F'>('M');
	let errors    = $state<Record<string, string>>({});
	let loading   = $state(false);
	let serverError = $state('');

	// Date bounds
	const today   = new Date();
	const maxDate = today.toISOString().slice(0, 10);
	const minDate = new Date(today.getFullYear() - 100, today.getMonth(), today.getDate()).toISOString().slice(0, 10);

	const NAME_RE  = /^[\p{L}\s'\-]{2,}$/u;
	const NAME_MAX = 100;
	const PHONE_RE = /^0[5-7]\d{8}$/;

	function validate(): boolean {
		const e: Record<string, string> = {};
		const fn = firstName.trim();
		const ln = lastName.trim();
		const ph = phone.trim().replace(/\s/g, '');

		if (!fn)                       e.firstName = 'Champ requis';
		else if (!NAME_RE.test(fn))    e.firstName = 'Lettres uniquement, 2 caractères minimum';
		else if (fn.length > NAME_MAX) e.firstName = '100 caractères maximum';

		if (!ln)                       e.lastName = 'Champ requis';
		else if (!NAME_RE.test(ln))    e.lastName = 'Lettres uniquement, 2 caractères minimum';
		else if (ln.length > NAME_MAX) e.lastName = '100 caractères maximum';

		if (!ph)                      e.phone = 'Champ requis';
		else if (!PHONE_RE.test(ph))  e.phone = 'Format invalide — ex: 0555 12 34 56';

		if (!dob)              e.dob = 'Date de naissance requise';
		else if (dob > maxDate) e.dob = 'La date ne peut pas être dans le futur';
		else if (dob < minDate) e.dob = 'Âge maximum 100 ans';

		errors = e;
		return Object.keys(e).length === 0;
	}

	async function submit() {
		if (!validate()) return;
		loading = true;
		serverError = '';
		try {
			const res = await fetch('/api/patients', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					firstName: firstName.trim(),
					lastName:  lastName.trim(),
					phone:     phone.trim().replace(/\s/g, ''),
					dateOfBirth: dob,
					gender,
				}),
			});
			const data = await res.json();
			if (!res.ok) {
				serverError = data.error ?? `Erreur ${res.status}`;
				return;
			}
			onPatientAdded(data as PatientSummary);
		} catch {
			serverError = 'Impossible de joindre le serveur.';
		} finally {
			loading = false;
		}
	}

	function onKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') onClose();
	}
</script>

<svelte:window onkeydown={onKeydown} />

<!-- Backdrop -->
<div
	role="presentation"
	onclick={onClose}
	style="position:fixed;inset:0;z-index:400;background:rgba(15,23,42,0.45);backdrop-filter:blur(4px)"
></div>

<!-- Modal -->
<div
	role="dialog"
	aria-modal="true"
	aria-label="Ajout rapide d'un patient"
	style="
		position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);z-index:500;
		background:var(--surface);border-radius:14px;width:min(480px,calc(100vw - 32px));
		box-shadow:0 20px 60px rgba(0,0,0,0.22);overflow:hidden;
	"
>
	<!-- Header -->
	<div style="padding:20px 22px 16px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
		<div>
			<h2 style="font-size:16px;font-weight:700">Nouveau patient</h2>
			<p style="font-size:12.5px;color:var(--text-muted);margin-top:2px">Informations essentielles uniquement</p>
		</div>
		<button
			type="button"
			onclick={onClose}
			style="background:var(--bg);border:1px solid var(--border);border-radius:7px;padding:6px;cursor:pointer;display:flex;align-items:center"
		>
			<Icon name="x" size={16} color="var(--text-muted)" />
		</button>
	</div>

	<!-- Body -->
	<div style="padding:20px 22px;display:flex;flex-direction:column;gap:14px">

		{#if serverError}
			<div style="display:flex;align-items:center;gap:8px;padding:10px 13px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px">
				<Icon name="alertCircle" size={14} color="var(--danger)" />
				<span style="font-size:13px;color:var(--danger)">{serverError}</span>
			</div>
		{/if}

		<!-- Prénom / Nom -->
		<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
			<div>
				<label for="qap-firstName" class="ql">PRÉNOM *</label>
				<input id="qap-firstName" bind:value={firstName} maxlength={NAME_MAX} class="mk-input" class:input-err={errors.firstName} placeholder="Ahmed" />
				{#if errors.firstName}<p class="qe"><Icon name="alertCircle" size={11} color="var(--danger)" />{errors.firstName}</p>{/if}
			</div>
			<div>
				<label for="qap-lastName" class="ql">NOM *</label>
				<input id="qap-lastName" bind:value={lastName} maxlength={NAME_MAX} class="mk-input" class:input-err={errors.lastName} placeholder="Benali" />
				{#if errors.lastName}<p class="qe"><Icon name="alertCircle" size={11} color="var(--danger)" />{errors.lastName}</p>{/if}
			</div>
		</div>

		<!-- Date + Sexe -->
		<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
			<div>
				<label for="qap-dob" class="ql">DATE DE NAISSANCE *</label>
				<input id="qap-dob" type="date" bind:value={dob} min={minDate} max={maxDate} class="mk-input" class:input-err={errors.dob} />
				{#if errors.dob}<p class="qe"><Icon name="alertCircle" size={11} color="var(--danger)" />{errors.dob}</p>{/if}
			</div>
			<div>
				<span class="ql">SEXE</span>
				<div style="display:flex;gap:8px">
					<button type="button" class="seg {gender === 'M' ? 'seg-on' : ''}" onclick={() => gender = 'M'}>
						<span class="dot {gender === 'M' ? 'dot-on' : ''}"></span> Masculin
					</button>
					<button type="button" class="seg {gender === 'F' ? 'seg-on' : ''}" onclick={() => gender = 'F'}>
						<span class="dot {gender === 'F' ? 'dot-on' : ''}"></span> Féminin
					</button>
				</div>
			</div>
		</div>

		<!-- Téléphone -->
		<div>
			<label for="qap-phone" class="ql">TÉLÉPHONE *</label>
			<input id="qap-phone" bind:value={phone} class="mk-input" class:input-err={errors.phone} placeholder="0555 12 34 56" />
			{#if errors.phone}<p class="qe"><Icon name="alertCircle" size={11} color="var(--danger)" />{errors.phone}</p>{/if}
		</div>

	</div>

	<!-- Footer -->
	<div style="padding:14px 22px;border-top:1px solid var(--border);display:flex;justify-content:flex-end;gap:10px">
		<button
			type="button"
			onclick={onClose}
			style="padding:9px 18px;background:var(--bg);border:1px solid var(--border);border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:500;color:var(--text);cursor:pointer"
		>
			Annuler
		</button>
		<button
			type="button"
			onclick={submit}
			disabled={loading}
			style="display:flex;align-items:center;gap:7px;padding:9px 20px;background:{loading ? '#5BA8A2' : 'var(--primary)'};border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;color:white;cursor:{loading ? 'not-allowed' : 'pointer'}"
		>
			{#if loading}
				<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5" style="animation:spin 0.8s linear infinite">
					<path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/>
				</svg>
				Enregistrement…
			{:else}
				<Icon name="user" size={13} color="white" />
				Créer le patient
			{/if}
		</button>
	</div>
</div>

<style>
	@keyframes spin { to { transform: rotate(360deg); } }

	.ql {
		display: block;
		font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
		color: var(--text-muted); text-transform: uppercase; margin-bottom: 6px;
	}
	.qe {
		display: flex; align-items: center; gap: 4px;
		font-size: 11.5px; color: var(--danger); margin-top: 5px;
	}
	.seg {
		display: flex; align-items: center; gap: 7px; flex: 1; justify-content: center;
		padding: 8px 10px; border-radius: 7px; cursor: pointer; font-family: inherit;
		font-size: 13px; font-weight: 500; color: var(--text-muted);
		background: var(--bg); border: 1.5px solid var(--border);
	}
	.seg-on { color: var(--primary); border-color: var(--primary); background: var(--primary-50); font-weight: 600; }
	.dot {
		width: 14px; height: 14px; border-radius: 50%; flex-shrink: 0;
		border: 2px solid var(--border-strong); background: white; display: inline-block; position: relative;
	}
	.dot-on { border-color: var(--primary); }
	.dot-on::after { content: ''; position: absolute; inset: 3px; border-radius: 50%; background: var(--primary); }
	:global(.input-err) { border-color: var(--danger) !important; background: var(--danger-light); }
</style>
