<script lang="ts">
	import Icon from '$lib/components/Icon.svelte';
	import { MEDICAMENT_CATALOG, CAT_COLOR, type CatalogMed } from '$lib/data/medicament-catalog';
	import { expandPosology } from '$lib/posology';
	import type { Med, OrdonnancePatient } from '$lib/types/consultation';

	let {
		patient,
		medications,
		medList = [],
		addMed,
		updateMed,
		removeMed,
		onPrint,
		onClose
	}: {
		patient: OrdonnancePatient;
		medications: Med[];
		/** Liste libre complète (noms commerciaux) — recherche secondaire en texte. */
		medList?: string[];
		addMed: (init?: Partial<Med>) => void;
		updateMed: (id: number, field: keyof Omit<Med, 'id'>, val: string | number) => void;
		removeMed: (id: number) => void;
		onPrint: () => void;
		onClose: () => void;
	} = $props();

	let q = $state('');
	let searchEl = $state<HTMLInputElement | null>(null);

	$effect(() => {
		searchEl?.focus();
	});

	const validCount = $derived(medications.filter((m) => m.medication.trim()).length);

	// Résultats du catalogue structuré (rangées riches, posologie pré-remplie).
	const catalogResults = $derived.by(() => {
		const s = q.trim().toLowerCase();
		if (!s) return MEDICAMENT_CATALOG;
		return MEDICAMENT_CATALOG.filter((d) =>
			`${d.nom} ${d.dosage} ${d.cat} ${d.forme}`.toLowerCase().includes(s)
		);
	});

	// Résultats en texte libre (liste complète), uniquement quand on cherche, en
	// excluant ce que le catalogue couvre déjà, plafonnés pour rester lisibles.
	const freeResults = $derived.by(() => {
		const s = q.trim().toLowerCase();
		if (!s) return [] as string[];
		const covered = new Set(MEDICAMENT_CATALOG.map((d) => d.nom.toLowerCase()));
		return medList
			.filter((m) => m.toLowerCase().includes(s) && !covered.has(m.split(' ')[0].toLowerCase()))
			.slice(0, 40);
	});

	const resultCount = $derived(catalogResults.length + freeResults.length);

	function catLabel(d: CatalogMed): string {
		return `${d.nom} ${d.dosage}`;
	}
	const isCatAdded = (d: CatalogMed) => medications.some((m) => m.medication === catLabel(d));
	const isFreeAdded = (name: string) => medications.some((m) => m.medication === name);

	function pickCatalog(d: CatalogMed) {
		if (isCatAdded(d)) return;
		addMed({ medication: catLabel(d), dosage: d.pos, duration: d.duree, quantity: 1 });
	}
	function pickFree(name: string) {
		if (isFreeAdded(name)) return;
		addMed({ medication: name, quantity: 1 });
	}

	function onKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			e.preventDefault();
			e.stopPropagation();
			onClose();
		}
	}
</script>

<svelte:window onkeydown={onKeydown} />

<div class="ordo-window" role="dialog" aria-modal="true" aria-label="Nouvelle ordonnance">
	<!-- ── Header brand ── -->
	<header class="ow-head">
		<button type="button" class="ow-back" onclick={onClose} title="Retour à la consultation">
			<Icon name="chevronLeft" size={16} color="white" /> Retour
		</button>
		<div class="ow-id">
			<div class="ow-id-icon"><Icon name="fileText" size={18} color="white" /></div>
			<div>
				<div class="ow-id-title">Nouvelle ordonnance</div>
				<div class="ow-id-sub">{patient.firstName} {patient.lastName} · {patient.age} ans</div>
			</div>
		</div>

		<div class="ow-spacer"></div>

		<span class="ow-count">{validCount} médicament{validCount > 1 ? 's' : ''}</span>
		<button type="button" class="ow-print" disabled={validCount === 0} onclick={onPrint}>
			<Icon name="printer" size={18} color="white" /> Imprimer l'ordonnance
		</button>
		<button type="button" class="ow-close" onclick={onClose} aria-label="Fermer">
			<Icon name="x" size={22} color="rgba(255,255,255,0.7)" />
		</button>
	</header>

	<!-- ── Corps : deux colonnes ── -->
	<div class="ow-body">
		<!-- LEFT : recherche + catalogue -->
		<div class="ow-search-col">
			<div class="ow-search-bar">
				<label class="search-label" for="ordo-search">Rechercher un médicament</label>
				<div class="search-wrap">
					<span class="search-icon"><Icon name="search" size={18} color="var(--primary)" /></span>
					<input
						id="ordo-search"
						bind:this={searchEl}
						bind:value={q}
						class="search-input"
						placeholder="Nom, dosage ou classe… (ex. paracétamol, amox, tension)"
						autocomplete="off"
					/>
					{#if q}
						<button type="button" class="search-clear" onclick={() => (q = '')} aria-label="Effacer">
							<Icon name="x" size={18} color="var(--text-light)" />
						</button>
					{/if}
				</div>
				<div class="search-hint">
					{#if q}
						<strong>{resultCount}</strong> médicament{resultCount > 1 ? 's' : ''} trouvé{resultCount > 1
							? 's'
							: ''} — cliquez pour ajouter à l'ordonnance
					{:else}
						Liste des médicaments disponibles — cliquez sur « Ajouter » pour prescrire
					{/if}
				</div>
			</div>

			<div class="ow-results">
				{#if resultCount === 0}
					<div class="ow-empty-search">
						<Icon name="search" size={36} color="var(--border-strong)" />
						<div>Aucun médicament ne correspond à votre recherche.</div>
					</div>
				{:else}
					<div class="result-list">
						{#each catalogResults as d (d.nom + d.dosage)}
							{@const added = isCatAdded(d)}
							{@const c = CAT_COLOR[d.cat]}
							<button
								type="button"
								class="med-row"
								class:added
								onclick={() => pickCatalog(d)}
								disabled={added}
							>
								<span class="med-accent" style="background:{c}"></span>
								<div class="med-row-main">
									<div class="med-row-name">{d.nom} <span style="color:{c}">{d.dosage}</span></div>
									<div class="med-row-sub">
										{d.forme} · <span style="color:{c};font-weight:600">{d.cat}</span> · {d.pos}
									</div>
								</div>
								{#if added}
									<span class="med-added"><Icon name="check" size={16} color="var(--success)" /> Ajouté</span>
								{:else}
									<span class="med-add"><Icon name="plus" size={15} color="var(--primary)" /> Ajouter</span>
								{/if}
							</button>
						{/each}

						{#if freeResults.length}
							<div class="result-divider">Autres présentations (saisie libre)</div>
							{#each freeResults as name (name)}
								{@const added = isFreeAdded(name)}
								<button
									type="button"
									class="med-row med-row-free"
									class:added
									onclick={() => pickFree(name)}
									disabled={added}
								>
									<span class="med-accent" style="background:var(--text-light)"></span>
									<div class="med-row-main">
										<div class="med-row-name">{name}</div>
									</div>
									{#if added}
										<span class="med-added"><Icon name="check" size={16} color="var(--success)" /> Ajouté</span>
									{:else}
										<span class="med-add"><Icon name="plus" size={15} color="var(--primary)" /> Ajouter</span>
									{/if}
								</button>
							{/each}
						{/if}
					</div>
				{/if}
			</div>
		</div>

		<!-- RIGHT : ordonnance en construction -->
		<aside class="ow-panel">
			<div class="ow-panel-head">
				<Icon name="fileText" size={18} color="var(--primary)" />
				<div>
					<div class="ow-panel-title">Ordonnance</div>
					<div class="ow-panel-sub">
						{patient.firstName} {patient.lastName} · {validCount} médicament{validCount > 1 ? 's' : ''}
					</div>
				</div>
			</div>

			<div class="ow-panel-scroll">
				{#if medications.length === 0}
					<div class="panel-empty">
						<Icon name="search" size={30} color="var(--border-strong)" />
						<p>Recherchez un médicament à gauche, puis cliquez sur <strong>« Ajouter »</strong>.</p>
					</div>
				{:else}
					{#each medications as med, i (med.id)}
						<div class="ed-card">
							<span class="ed-accent"></span>
							<div class="ed-head">
								<span class="ed-no">Médicament {i + 1}</span>
								<button type="button" class="ed-remove" onclick={() => removeMed(med.id)} aria-label="Retirer">
									<Icon name="trash" size={15} color="var(--danger)" />
								</button>
							</div>
							<input
								class="ed-input ed-input-strong"
								value={med.medication}
								oninput={(e) => updateMed(med.id, 'medication', (e.target as HTMLInputElement).value)}
								placeholder="Nom + dosage"
							/>
							<input
								class="ed-input"
								value={med.dosage}
								oninput={(e) => updateMed(med.id, 'dosage', (e.target as HTMLInputElement).value)}
								onblur={() => {
									const x = expandPosology(med.dosage);
									if (x && x !== med.dosage) updateMed(med.id, 'dosage', x);
								}}
								placeholder="Posologie (ex. 1-0-1-0 ou 1 comprimé matin et soir)"
								title="Posologie — tapez 1-0-1-0 (matin-midi-soir-coucher) ou du texte libre"
							/>
							<div class="ed-row">
								<input
									class="ed-input"
									value={med.duration}
									oninput={(e) => updateMed(med.id, 'duration', (e.target as HTMLInputElement).value)}
									placeholder="Durée"
								/>
								<input
									class="ed-input ed-qty"
									type="number"
									min="1"
									value={med.quantity}
									oninput={(e) => updateMed(med.id, 'quantity', Number((e.target as HTMLInputElement).value) || 1)}
								/>
							</div>
						</div>
					{/each}
				{/if}
			</div>

			<div class="ow-panel-foot">
				<button type="button" class="panel-print" disabled={validCount === 0} onclick={onPrint}>
					<Icon name="printer" size={19} color="white" /> Imprimer l'ordonnance
				</button>
			</div>
		</aside>
	</div>
</div>

<style>
	.ordo-window {
		position: fixed;
		inset: 0;
		z-index: 500;
		background: var(--bg);
		display: flex;
		flex-direction: column;
	}

	/* ── Header ── */
	.ow-head {
		flex-shrink: 0;
		height: 64px;
		background: var(--nav-bg);
		color: white;
		display: flex;
		align-items: center;
		padding: 0 20px;
		gap: 16px;
	}
	.ow-back {
		display: flex;
		align-items: center;
		gap: 7px;
		background: rgba(255, 255, 255, 0.1);
		border: 1px solid rgba(255, 255, 255, 0.14);
		color: white;
		border-radius: 8px;
		padding: 8px 13px;
		cursor: pointer;
		font-family: inherit;
		font-size: 13.5px;
		font-weight: 500;
	}
	.ow-back:hover {
		background: rgba(255, 255, 255, 0.18);
	}
	.ow-id {
		display: flex;
		align-items: center;
		gap: 11px;
	}
	.ow-id-icon {
		width: 34px;
		height: 34px;
		border-radius: 9px;
		background: var(--primary);
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.ow-id-title {
		font-size: 15.5px;
		font-weight: 700;
		line-height: 1.1;
	}
	.ow-id-sub {
		font-size: 12.5px;
		opacity: 0.65;
		margin-top: 1px;
	}
	.ow-spacer {
		flex: 1;
	}
	.ow-count {
		font-size: 13px;
		opacity: 0.8;
	}
	.ow-print {
		display: flex;
		align-items: center;
		gap: 8px;
		background: var(--accent);
		color: white;
		border: none;
		border-radius: 9px;
		padding: 11px 20px;
		cursor: pointer;
		font-family: inherit;
		font-size: 14.5px;
		font-weight: 700;
		box-shadow: 0 2px 10px rgba(217, 119, 6, 0.4);
	}
	.ow-print:disabled {
		background: rgba(255, 255, 255, 0.15);
		cursor: not-allowed;
		opacity: 0.6;
		box-shadow: none;
	}
	.ow-close {
		background: none;
		border: none;
		cursor: pointer;
		padding: 6px;
		display: flex;
	}

	/* ── Body ── */
	.ow-body {
		flex: 1;
		display: flex;
		overflow: hidden;
	}
	.ow-search-col {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}
	.ow-search-bar {
		padding: 18px 22px 12px;
		flex-shrink: 0;
		background: var(--surface);
		border-bottom: 1px solid var(--border);
	}
	.search-label {
		display: block;
		margin-bottom: 6px;
		font-size: 13px;
		font-weight: 600;
		color: var(--text);
	}
	.search-wrap {
		position: relative;
	}
	.search-icon {
		position: absolute;
		left: 16px;
		top: 50%;
		transform: translateY(-50%);
		display: flex;
	}
	.search-input {
		width: 100%;
		padding: 13px 44px 13px 46px;
		border: 2px solid var(--border-strong);
		border-radius: 11px;
		font-family: inherit;
		font-size: 15px;
		color: var(--text);
		background: white;
		outline: none;
	}
	.search-input:focus {
		border-color: var(--primary);
		box-shadow: 0 0 0 4px rgba(15, 118, 110, 0.1);
	}
	.search-clear {
		position: absolute;
		right: 12px;
		top: 50%;
		transform: translateY(-50%);
		background: none;
		border: none;
		cursor: pointer;
		padding: 4px;
		display: flex;
	}
	.search-hint {
		font-size: 12.5px;
		color: var(--text-muted);
		margin-top: 7px;
	}
	.search-hint strong {
		color: var(--text);
	}

	.ow-results {
		flex: 1;
		overflow: auto;
		padding: 16px 22px;
	}
	.result-list {
		display: flex;
		flex-direction: column;
		gap: 8px;
	}
	.result-divider {
		font-size: 11px;
		font-weight: 700;
		text-transform: uppercase;
		letter-spacing: 0.5px;
		color: var(--text-light);
		margin: 14px 2px 2px;
	}
	.med-row {
		width: 100%;
		text-align: left;
		display: flex;
		align-items: center;
		gap: 14px;
		background: var(--surface);
		border: 1px solid var(--border);
		border-radius: 10px;
		padding: 12px 14px;
		cursor: pointer;
		font-family: inherit;
		transition: all 0.1s;
	}
	.med-row:hover:not(:disabled) {
		border-color: var(--primary);
		background: var(--primary-50);
	}
	.med-row.added {
		border-color: var(--success);
		cursor: default;
	}
	.med-accent {
		width: 8px;
		height: 38px;
		border-radius: 4px;
		flex-shrink: 0;
	}
	.med-row-free .med-accent {
		height: 24px;
	}
	.med-row-main {
		flex: 1;
		min-width: 0;
	}
	.med-row-name {
		font-size: 14.5px;
		font-weight: 700;
	}
	.med-row-sub {
		font-size: 12.5px;
		color: var(--text-muted);
		margin-top: 2px;
	}
	.med-added {
		display: flex;
		align-items: center;
		gap: 5px;
		color: var(--success);
		font-weight: 700;
		font-size: 13px;
		flex-shrink: 0;
	}
	.med-add {
		display: flex;
		align-items: center;
		gap: 5px;
		color: var(--primary);
		font-weight: 700;
		font-size: 13px;
		flex-shrink: 0;
		background: var(--primary-light);
		padding: 7px 13px;
		border-radius: 8px;
	}
	.ow-empty-search {
		text-align: center;
		color: var(--text-light);
		font-size: 14px;
		padding: 60px 20px;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 12px;
	}

	/* ── Panneau ordonnance ── */
	.ow-panel {
		width: 400px;
		flex-shrink: 0;
		background: var(--surface);
		border-left: 1px solid var(--border);
		display: flex;
		flex-direction: column;
	}
	.ow-panel-head {
		padding: 16px 18px;
		border-bottom: 1px solid var(--border);
		flex-shrink: 0;
		display: flex;
		align-items: center;
		gap: 10px;
	}
	.ow-panel-title {
		font-size: 14.5px;
		font-weight: 700;
	}
	.ow-panel-sub {
		font-size: 12px;
		color: var(--text-muted);
		margin-top: 1px;
	}
	.ow-panel-scroll {
		flex: 1;
		overflow: auto;
		padding: 16px;
		display: flex;
		flex-direction: column;
		gap: 11px;
	}
	.panel-empty {
		text-align: center;
		color: var(--text-light);
		font-size: 13.5px;
		padding: 40px 16px;
		border: 2px dashed var(--border);
		border-radius: 12px;
		line-height: 1.6;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 10px;
	}
	.panel-empty strong {
		color: var(--primary);
	}

	.ed-card {
		background: var(--surface);
		border: 1px solid var(--border);
		border-radius: 11px;
		padding: 13px 13px 13px 17px;
		position: relative;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
		display: flex;
		flex-direction: column;
		gap: 7px;
	}
	.ed-accent {
		position: absolute;
		top: 0;
		left: 0;
		bottom: 0;
		width: 4px;
		background: var(--success);
		border-radius: 11px 0 0 11px;
	}
	.ed-head {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.ed-no {
		font-size: 11px;
		font-weight: 700;
		color: var(--success);
		text-transform: uppercase;
		letter-spacing: 0.4px;
	}
	.ed-remove {
		background: none;
		border: none;
		cursor: pointer;
		padding: 2px;
		display: flex;
	}
	.ed-input {
		width: 100%;
		padding: 9px 11px;
		border: 1px solid var(--border-strong);
		border-radius: 7px;
		font-family: inherit;
		font-size: 13.5px;
		background: white;
		outline: none;
		color: var(--text);
	}
	.ed-input:focus {
		border-color: var(--primary);
		box-shadow: 0 0 0 3px rgba(15, 118, 110, 0.1);
	}
	.ed-input-strong {
		font-weight: 700;
	}
	.ed-row {
		display: grid;
		grid-template-columns: 1fr 72px;
		gap: 7px;
	}
	.ed-qty {
		text-align: center;
	}

	.ow-panel-foot {
		flex-shrink: 0;
		border-top: 1px solid var(--border);
		padding: 14px 16px;
	}
	.panel-print {
		width: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 9px;
		background: var(--accent);
		color: white;
		border: none;
		border-radius: 11px;
		padding: 14px;
		cursor: pointer;
		font-family: inherit;
		font-size: 15.5px;
		font-weight: 700;
		box-shadow: 0 4px 14px rgba(217, 119, 6, 0.3);
	}
	.panel-print:disabled {
		background: var(--border);
		cursor: not-allowed;
		box-shadow: none;
	}

	@media (max-width: 860px) {
		.ow-body {
			flex-direction: column;
			overflow: auto;
		}
		.ow-panel {
			width: 100%;
			border-left: none;
			border-top: 1px solid var(--border);
		}
		.ow-count {
			display: none;
		}
	}
</style>
