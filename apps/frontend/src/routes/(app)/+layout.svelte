<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import type { Snippet } from 'svelte';
	import type { LayoutData } from './$types';
	import Icon from '$lib/components/Icon.svelte';
	import { PERMISSIONS, can } from '$lib/permissions';

	let { data, children }: { data: LayoutData; children: Snippet } = $props();

	const perms = $derived(data.user.permissions ?? []);
	const canSeePatients = $derived(can(perms, PERMISSIONS.patients.view));
	const canSeeConsultation = $derived(can(perms, PERMISSIONS.consultations.manage));
	const canSeeTeam = $derived(can(perms, PERMISSIONS.users.view));

	// Issue #54 / #59-followup — global navbar search: live patient autocomplete.
	// Typing (debounced, min 2 chars) queries /api/patients/search and shows a
	// dropdown of matches directly under the input; selecting a row opens the
	// patient file. Patients-only scope; truly-global search tracked separately.
	import type { PagedResult, PatientSummary } from '$lib/types/api';

	let navSearch = $state('');
	let navResults = $state<PatientSummary[]>([]);
	let navOpen = $state(false);
	let navLoading = $state(false);
	let navHighlight = $state(-1);

	let navTimer: ReturnType<typeof setTimeout> | undefined;
	let navAbort: AbortController | undefined;

	function runNavSearch(term: string) {
		navAbort?.abort();
		navAbort = new AbortController();
		navLoading = true;
		fetch(`/api/patients/search?term=${encodeURIComponent(term)}&pageSize=8`, {
			signal: navAbort.signal
		})
			.then((r) => (r.ok ? r.json() : Promise.reject(r)))
			.then((data: PagedResult<PatientSummary>) => {
				navResults = data.items ?? [];
				navHighlight = navResults.length ? 0 : -1;
				navOpen = true;
				navLoading = false;
			})
			.catch((e) => {
				if (e?.name === 'AbortError') return; // superseded by a newer keystroke
				console.error('[navbar-search]', e);
				navResults = [];
				navHighlight = -1;
				navOpen = true;
				navLoading = false;
			});
	}

	function onNavInput() {
		clearTimeout(navTimer);
		const term = navSearch.trim();
		if (term.length < 2) {
			navAbort?.abort();
			navResults = [];
			navHighlight = -1;
			navOpen = term.length > 0; // show the "min 2 chars" hint while typing 1 char
			navLoading = false;
			return;
		}
		navTimer = setTimeout(() => runNavSearch(term), 250);
	}

	function closeNavSearch() {
		navOpen = false;
		navHighlight = -1;
	}

	function selectPatient(id: string) {
		closeNavSearch();
		navSearch = '';
		navResults = [];
		goto(`/patients/${id}`);
	}

	function onNavSearch(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			closeNavSearch();
			return;
		}
		if (!navOpen || navResults.length === 0) {
			if (e.key === 'Enter') e.preventDefault();
			return;
		}
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			navHighlight = (navHighlight + 1) % navResults.length;
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			navHighlight = (navHighlight - 1 + navResults.length) % navResults.length;
		} else if (e.key === 'Enter') {
			e.preventDefault();
			const pick = navResults[navHighlight] ?? navResults[0];
			if (pick) selectPatient(pick.id);
		}
	}

	// Split a label into matched / unmatched segments for highlighting the term.
	function segments(text: string, term: string): { text: string; hit: boolean }[] {
		const q = term.trim();
		if (!q) return [{ text, hit: false }];
		const out: { text: string; hit: boolean }[] = [];
		const lower = text.toLowerCase();
		const needle = q.toLowerCase();
		let i = 0;
		let idx = lower.indexOf(needle, i);
		while (idx !== -1) {
			if (idx > i) out.push({ text: text.slice(i, idx), hit: false });
			out.push({ text: text.slice(idx, idx + needle.length), hit: true });
			i = idx + needle.length;
			idx = lower.indexOf(needle, i);
		}
		if (i < text.length) out.push({ text: text.slice(i), hit: false });
		return out;
	}

	// Each link declares the permission required to see it (null = always visible).
	// A Doctor (admin) holds every permission, so the full nav shows for them; a Secretary
	// sees only the sections their customised permission set grants.
	const NAV = $derived(
		[
			{ href: '/dashboard',    label: 'Tableau de bord', icon: 'dashboard',  perm: null },
			{ href: '/schedule',     label: 'Agenda',          icon: 'calendar',   perm: PERMISSIONS.scheduling.view },
			{ href: '/patients',     label: 'Patients',        icon: 'users',      perm: PERMISSIONS.patients.view },
			{ href: '/consultation', label: 'Consultations',   icon: 'clipboard',  perm: PERMISSIONS.consultations.manage },
			{ href: '/finance',      label: 'Finances',        icon: 'barchart',   perm: PERMISSIONS.finance.viewSummary },
			{ href: '/team',         label: 'Équipe',          icon: 'shieldCheck', perm: PERMISSIONS.users.view },
		].filter((item) => item.perm === null || can(perms, item.perm))
	);

	const roleLabel = $derived(
		data.user.role === 'Doctor'
			? 'Médecin généraliste'
			: data.user.role === 'Secretary'
				? 'Secrétaire'
				: data.user.role
	);

	const initials = $derived(
		data.user.fullName.split(' ').map((n: string) => n[0] ?? '').join('').toUpperCase().slice(0, 2)
	);

	let showUserMenu = $state(false);
</script>

<!-- Invisible backdrop to close dropdown -->
{#if showUserMenu}
	<div
		onclick={() => showUserMenu = false}
		style="position:fixed;inset:0;z-index:190"
		role="presentation"
	></div>
{/if}

<!-- Invisible backdrop to close the patient search dropdown -->
{#if navOpen}
	<div
		onclick={closeNavSearch}
		style="position:fixed;inset:0;z-index:190"
		role="presentation"
	></div>
{/if}

<nav class="mk-nav no-print">
	<!-- Logo -->
	<div style="display:flex;align-items:center;gap:9px;padding-right:18px;border-right:1px solid rgba(255,255,255,0.1);margin-right:8px;flex-shrink:0">
		<div style="width:30px;height:30px;border-radius:8px;background:var(--primary);display:flex;align-items:center;justify-content:center">
			<Icon name="activity" size={16} color="white" />
		</div>
		<span style="color:white;font-weight:700;font-size:16px;letter-spacing:-0.4px">
			Medi<span style="color:#5EE7D0">Ka</span>
		</span>
	</div>

	<!-- Links -->
	<div style="display:flex;gap:2px;flex:1">
		{#each NAV as item}
			{@const active = page.url.pathname.startsWith(item.href)}
			<a href={item.href} style="
				display:flex;align-items:center;gap:6px;padding:7px 13px;
				border-radius:6px;text-decoration:none;font-size:13.5px;font-family:inherit;
				background:{active ? 'rgba(255,255,255,0.13)' : 'transparent'};
				color:{active ? 'white' : 'rgba(255,255,255,0.6)'};
				font-weight:{active ? 600 : 400};
			">
				<Icon name={item.icon} size={15} color={active ? 'white' : 'rgba(255,255,255,0.6)'} />
				{item.label}
				{#if item.href === '/patients' && data.totalPatients > 0}
					<span style="
						margin-left:2px;padding:1px 7px;border-radius:10px;font-size:11px;font-weight:600;
						background:rgba(94,231,208,0.18);color:#5EE7D0;line-height:1.5;
					">{data.totalPatients}</span>
				{/if}
			</a>
		{/each}
	</div>

	<!-- Right -->
	<div style="display:flex;align-items:center;gap:10px;flex-shrink:0">
		{#if canSeePatients}
			<div style="position:relative">
				<div style="display:flex;align-items:center;gap:8px;background:rgba(255,255,255,0.09);border-radius:7px;padding:6px 11px;width:240px;border:1px solid rgba(255,255,255,0.1)">
					<Icon name="search" size={14} color="rgba(255,255,255,0.5)" />
					<input
						type="search"
						bind:value={navSearch}
						oninput={onNavInput}
						onkeydown={onNavSearch}
						onfocus={() => { if (navResults.length || navSearch.trim().length) navOpen = true; }}
						placeholder="Rechercher un patient…"
						aria-label="Recherche rapide de patients"
						autocomplete="off"
						style="background:transparent;border:none;outline:none;color:white;font-family:inherit;font-size:13px;width:100%"
					/>
				</div>

				<!-- Live results dropdown -->
				{#if navOpen}
					<div
						style="
							position:absolute;top:calc(100% + 8px);right:0;z-index:300;
							width:340px;max-height:60vh;overflow-y:auto;
							background:white;border:1px solid var(--border);border-radius:10px;
							box-shadow:0 8px 24px rgba(0,0,0,0.14);
						"
						role="listbox"
					>
						{#if navLoading && navResults.length === 0}
							<div style="padding:14px 16px;font-size:13px;color:var(--text-muted)">Recherche…</div>
						{:else if navSearch.trim().length < 2}
							<div style="padding:14px 16px;font-size:13px;color:var(--text-muted)">Tapez au moins 2 caractères…</div>
						{:else if navResults.length === 0}
							<div style="padding:14px 16px;font-size:13px;color:var(--text-muted)">Aucun patient trouvé</div>
						{:else}
							{#each navResults as p, i}
								<button
									type="button"
									role="option"
									aria-selected={i === navHighlight}
									onmouseenter={() => navHighlight = i}
									onclick={() => selectPatient(p.id)}
									style="
										display:flex;align-items:center;gap:11px;width:100%;text-align:left;
										padding:10px 14px;border:none;cursor:pointer;font-family:inherit;
										background:{i === navHighlight ? 'var(--primary-50)' : 'transparent'};
										border-bottom:1px solid var(--border);
									"
								>
									<div style="
										width:30px;height:30px;border-radius:50%;flex-shrink:0;
										background:var(--primary-light);color:var(--primary-dark);
										display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;
									">
										{(p.firstName[0] ?? '').toUpperCase()}{(p.lastName[0] ?? '').toUpperCase()}
									</div>
									<div style="min-width:0;flex:1">
										<div style="font-size:13.5px;color:var(--text);font-weight:500;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">
											{#each segments(`${p.firstName} ${p.lastName}`, navSearch) as seg}<span style={seg.hit ? 'background:var(--primary-light);color:var(--primary-dark);border-radius:3px' : ''}>{seg.text}</span>{/each}
										</div>
										<div style="font-size:11.5px;color:var(--text-muted);margin-top:1px">
											{p.age} ans · {p.gender === 'F' ? 'Femme' : 'Homme'}{p.phone ? ` · ${p.phone}` : ''}
										</div>
									</div>
								</button>
							{/each}
						{/if}
					</div>
				{/if}
			</div>
		{/if}

		{#if canSeeConsultation}
			<a href="/consultation" style="display:inline-flex;align-items:center;gap:6px;padding:6px 12px;border-radius:7px;background:var(--accent);color:white;text-decoration:none;font-size:13px;font-weight:500">
				<Icon name="plus" size={14} color="white" />
				Consultation
			</a>
		{/if}

		<!-- User dropdown -->
		<div style="position:relative">
			<button
				onclick={(e) => { e.stopPropagation(); showUserMenu = !showUserMenu; }}
				style="display:flex;align-items:center;gap:8px;background:rgba(255,255,255,0.09);border:1px solid rgba(255,255,255,0.1);border-radius:7px;padding:5px 10px;color:white;cursor:pointer;font-family:inherit"
			>
				<div style="width:26px;height:26px;border-radius:50%;background:var(--primary);display:flex;align-items:center;justify-content:center;font-size:10px;font-weight:700;flex-shrink:0">
					{initials}
				</div>
				<div style="text-align:left">
					<div style="font-size:12.5px;font-weight:600;line-height:1.2">{data.user.fullName}</div>
					<div style="font-size:11px;opacity:0.55">{data.user.role === 'Doctor' ? 'Généraliste' : roleLabel}</div>
				</div>
				<Icon name="chevronDown" size={13} color="rgba(255,255,255,0.5)" />
			</button>

			<!-- Dropdown menu -->
			{#if showUserMenu}
				<div
					style="
						position:absolute;top:calc(100% + 8px);right:0;z-index:300;
						background:white;border:1px solid var(--border);border-radius:10px;
						box-shadow:0 8px 24px rgba(0,0,0,0.12);min-width:188px;overflow:hidden;
					"
				>
					<!-- User info header -->
					<div style="padding:13px 16px;border-bottom:1px solid var(--border);background:var(--bg)">
						<div style="font-size:13px;font-weight:600;color:var(--text)">{data.user.fullName}</div>
						<div style="font-size:11.5px;color:var(--text-muted);margin-top:1px">
							{roleLabel}
						</div>
					</div>

					<!-- Menu items -->
					<div style="padding:5px">
						<a
							href="/profile"
							onclick={() => showUserMenu = false}
							style="display:flex;align-items:center;gap:10px;padding:9px 12px;border-radius:6px;text-decoration:none;color:var(--text);font-size:13.5px"
						>
							<Icon name="user" size={15} color="var(--text-muted)" />
							Mon profil
						</a>
						<a
							href="/profile"
							onclick={() => showUserMenu = false}
							style="display:flex;align-items:center;gap:10px;padding:9px 12px;border-radius:6px;text-decoration:none;color:var(--text);font-size:13.5px"
						>
							<Icon name="settings" size={15} color="var(--text-muted)" />
							Paramètres
						</a>
					</div>

					<div style="padding:5px;border-top:1px solid var(--border)">
						<a
							href="/logout"
							data-sveltekit-preload-data="off"
							data-sveltekit-reload
							onclick={() => showUserMenu = false}
							style="display:flex;align-items:center;gap:10px;padding:9px 12px;border-radius:6px;text-decoration:none;color:var(--danger);font-size:13.5px;font-weight:500"
						>
							<Icon name="logOut" size={15} color="var(--danger)" />
							Déconnexion
						</a>
					</div>
				</div>
			{/if}
		</div>
	</div>
</nav>

<main class="mk-content">
	{@render children()}
</main>
