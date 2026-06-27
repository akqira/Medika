<script lang="ts">
	import { onMount } from 'svelte';
	import { goto, replaceState } from '$app/navigation';
	import { page as pageState } from '$app/state';
	import type { PageData } from './$types';
	import type { PatientSummary, PagedResult } from '$lib/types/api';
	import Avatar from '$lib/components/Avatar.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import { toast } from '$lib/stores/toast.svelte';

	let { data }: { data: PageData } = $props();

	// Issue #129 — success notifications. The patient-new and consultation-save server
	// actions redirect here with `?toast=…`; surface the matching toast once on mount,
	// then strip the param so a refresh/back doesn't re-fire it. onMount (not $effect)
	// guarantees a single fire — a reactive effect would re-run and loop on the param.
	const TOAST_MESSAGES: Record<string, string> = {
		'patient-created': 'Patient créé avec succès.',
		'consultation-saved': 'Consultation enregistrée avec succès.',
	};
	onMount(() => {
		const kind = pageState.url.searchParams.get('toast');
		const message = kind && TOAST_MESSAGES[kind];
		if (!message) return;
		toast.success(message);
		const url = new URL(pageState.url);
		url.searchParams.delete('toast');
		url.searchParams.delete('created');
		replaceState(url, {});
	});

	const PAGE_SIZE = 20;

	// Client-accumulated list (SSR gives page 1; we append on scroll).
	let patients      = $state<PatientSummary[]>(data.result.items);
	let term          = $state(data.term ?? '');
	let page          = $state(data.result.page);
	let hasNext       = $state(data.result.hasNextPage);
	// cabinetTotal: real total for this cabinet — never updated by search
	let cabinetTotal  = $state(data.cabinetTotal);
	// filteredTotal: count matching the current search term
	let filteredTotal = $state(data.result.totalCount);
	let loadingMore = $state(false);
	let searching   = $state(false);
	let loadError   = $state('');

	// Keyboard navigation
	let activeIndex = $state(-1);
	let listEl = $state<HTMLDivElement | null>(null);

	let searchTimer: ReturnType<typeof setTimeout>;

	async function fetchPage(t: string, p: number): Promise<PagedResult<PatientSummary> | null> {
		try {
			const res = await fetch(`/api/patients/search?term=${encodeURIComponent(t)}&page=${p}&pageSize=${PAGE_SIZE}`);
			if (!res.ok) throw new Error(`Erreur ${res.status}`);
			return (await res.json()) as PagedResult<PatientSummary>;
		} catch (e) {
			loadError = e instanceof Error ? e.message : 'Erreur de chargement.';
			return null;
		}
	}

	function onSearch() {
		clearTimeout(searchTimer);
		searchTimer = setTimeout(runSearch, 300);
	}

	async function runSearch() {
		searching = true;
		loadError = '';
		activeIndex = -1;
		const r = await fetchPage(term, 1);
		searching = false;
		if (!r) return;
		patients = r.items;
		page = 1;
		hasNext = r.hasNextPage;
		filteredTotal = r.totalCount;
		if (listEl) listEl.scrollTop = 0;
	}

	async function loadMore() {
		if (loadingMore || !hasNext) return;
		loadingMore = true;
		const r = await fetchPage(term, page + 1);
		loadingMore = false;
		if (!r) return;
		patients = [...patients, ...r.items];
		page = page + 1;
		hasNext = r.hasNextPage;
	}

	function onListScroll() {
		if (!listEl || loadingMore || !hasNext) return;
		const { scrollTop, clientHeight, scrollHeight } = listEl;
		if (scrollTop + clientHeight >= scrollHeight - 240) loadMore();
	}

	function open(p: PatientSummary) {
		goto(`/patients/${p.id}`);
	}

	function onSearchKeydown(e: KeyboardEvent) {
		if (patients.length === 0) return;
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			activeIndex = Math.min(activeIndex + 1, patients.length - 1);
			// Prefetch more when navigating near the end
			if (activeIndex >= patients.length - 3) loadMore();
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			activeIndex = Math.max(activeIndex - 1, 0);
		} else if (e.key === 'Enter') {
			e.preventDefault();
			if (activeIndex >= 0 && activeIndex < patients.length) open(patients[activeIndex]);
		} else if (e.key === 'Escape') {
			activeIndex = -1;
		}
	}

	// Keep the highlighted row visible
	$effect(() => {
		if (activeIndex < 0 || !listEl) return;
		const row = listEl.querySelector<HTMLElement>(`[data-idx="${activeIndex}"]`);
		row?.scrollIntoView({ block: 'nearest' });
	});
</script>

<svelte:head><title>Patients · Medika</title></svelte:head>

<div style="max-width:760px;margin:0 auto;padding:22px 24px;height:calc(100vh - 58px);display:flex;flex-direction:column">

	<!-- Header -->
	<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
		<div style="display:flex;align-items:baseline;gap:10px">
			<h1 style="font-size:20px;font-weight:700;letter-spacing:-0.3px">Patients</h1>
			<span style="font-size:13px;color:var(--text-muted)">{cabinetTotal} au total</span>
		</div>
		<a href="/patients/new" style="display:inline-flex;align-items:center;gap:6px;padding:9px 15px;background:var(--primary);color:white;border-radius:8px;text-decoration:none;font-size:13.5px;font-weight:600">
			<Icon name="plus" size={14} color="white" /> Nouveau patient
		</a>
	</div>

	<!-- Search -->
	<div style="display:flex;align-items:center;gap:9px;background:var(--surface);border:1px solid var(--border);border-radius:9px;padding:10px 14px;margin-bottom:6px">
		<Icon name="search" size={16} color="var(--text-muted)" />
		<!-- svelte-ignore a11y_autofocus -->
		<input
			type="search"
			bind:value={term}
			oninput={onSearch}
			onkeydown={onSearchKeydown}
			autofocus
			placeholder="Rechercher un patient… (↑ ↓ pour naviguer, Entrée pour ouvrir)"
			style="background:transparent;border:none;outline:none;font-family:inherit;font-size:14px;color:var(--text);width:100%"
		/>
		{#if searching}
			<Icon name="activity" size={15} color="var(--border-strong)" />
		{/if}
	</div>
	<div style="font-size:11.5px;color:var(--text-light);margin-bottom:10px;padding-left:2px">
		{#if term}
			{filteredTotal} résultat{filteredTotal !== 1 ? 's' : ''}
		{:else}
			Astuce : tapez pour filtrer, puis ↑ ↓ et Entrée — sans la souris.
		{/if}
	</div>

	{#if loadError}
		<div style="display:flex;align-items:center;gap:8px;padding:11px 14px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;margin-bottom:10px">
			<Icon name="alertCircle" size={14} color="var(--danger)" />
			<span style="font-size:13px;color:var(--danger)">{loadError}</span>
		</div>
	{/if}

	<!-- List -->
	<div bind:this={listEl} onscroll={onListScroll} class="card" style="flex:1;overflow-y:auto;padding:0">
		{#if patients.length === 0}
			<div style="padding:48px 16px;text-align:center;color:var(--text-muted)">
				<Icon name="users" size={34} color="var(--border-strong)" />
				<p style="margin-top:12px;font-size:14px">{searching ? 'Recherche…' : 'Aucun patient trouvé'}</p>
			</div>
		{:else}
			{#each patients as patient, i}
				<button
					type="button"
					data-idx={i}
					onclick={() => open(patient)}
					onmouseenter={() => activeIndex = i}
					style="
						display:flex;align-items:center;gap:12px;padding:13px 18px;width:100%;
						text-align:left;border:none;cursor:pointer;
						background:{activeIndex === i ? 'var(--primary-50)' : 'transparent'};
						border-left:3px solid {activeIndex === i ? 'var(--primary)' : 'transparent'};
						border-bottom:1px solid var(--border);
					"
				>
					<Avatar nom={patient.lastName} prenom={patient.firstName} sexe={patient.gender} size={38} />
					<div style="flex:1;min-width:0">
						<div style="font-size:14px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:{activeIndex === i ? 'var(--primary)' : 'var(--text)'}">
							{patient.firstName} {patient.lastName}
						</div>
						<div style="font-size:12.5px;color:var(--text-muted);margin-top:1px">
							{patient.age} ans · {patient.gender === 'F' ? 'Femme' : 'Homme'}
							{#if patient.phone} · {patient.phone}{/if}
						</div>
					</div>
					{#if patient.bloodGroup}
						<span style="background:var(--danger-light);color:var(--danger);font-size:11.5px;font-weight:700;padding:2px 8px;border-radius:20px;flex-shrink:0">{patient.bloodGroup}</span>
					{/if}
					{#if patient.allergyCount > 0}
						<span title="{patient.allergyCount} allergie(s)" style="font-size:13px;color:var(--warning);flex-shrink:0">⚠</span>
					{/if}
					<Icon name="chevronRight" size={15} color="var(--border-strong)" />
				</button>
			{/each}

			{#if loadingMore}
				<div style="display:flex;align-items:center;justify-content:center;gap:8px;padding:14px;color:var(--text-muted);font-size:13px">
					<Icon name="activity" size={15} color="var(--border-strong)" /> Chargement…
				</div>
			{:else if !hasNext}
				<div style="text-align:center;padding:14px;color:var(--text-light);font-size:12px">
					{patients.length} patient{patients.length > 1 ? 's' : ''} affiché{patients.length > 1 ? 's' : ''}
				</div>
			{/if}
		{/if}
	</div>
</div>
