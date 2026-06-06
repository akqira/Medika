<script lang="ts">
	import { page } from '$app/state';
	import type { Snippet } from 'svelte';
	import type { LayoutData } from './$types';
	import Icon from '$lib/components/Icon.svelte';

	let { data, children }: { data: LayoutData; children: Snippet } = $props();

	const NAV = [
		{ href: '/dashboard',    label: 'Tableau de bord', icon: 'dashboard' },
		{ href: '/schedule',     label: 'Agenda',          icon: 'calendar' },
		{ href: '/patients',     label: 'Patients',        icon: 'users' },
		{ href: '/consultation', label: 'Consultations',   icon: 'clipboard' },
		{ href: '/finance',      label: 'Finances',        icon: 'barchart' },
	];

	const initials = $derived(
		data.user.fullName.split(' ').map((n: string) => n[0] ?? '').join('').toUpperCase().slice(0, 2)
	);

	let showUserMenu = $state(false);
</script>

<!-- Invisible backdrop to close dropdown -->
{#if showUserMenu}
	<div
		onclick={() => showUserMenu = false}
		style="position:fixed;inset:0;z-index:250"
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
			</a>
		{/each}
	</div>

	<!-- Right -->
	<div style="display:flex;align-items:center;gap:10px;flex-shrink:0">
		<div style="display:flex;align-items:center;gap:8px;background:rgba(255,255,255,0.09);border-radius:7px;padding:6px 11px;width:200px;border:1px solid rgba(255,255,255,0.1)">
			<Icon name="search" size={14} color="rgba(255,255,255,0.5)" />
			<input placeholder="Rechercher un patient…" style="background:transparent;border:none;outline:none;color:white;font-family:inherit;font-size:13px;width:100%" />
		</div>

		<button style="position:relative;background:rgba(255,255,255,0.09);border:1px solid rgba(255,255,255,0.1);border-radius:7px;color:rgba(255,255,255,0.8);cursor:pointer;padding:7px 8px;display:flex;align-items:center">
			<Icon name="bell" size={17} />
			<span style="position:absolute;top:5px;right:5px;width:7px;height:7px;border-radius:50%;background:#F59E0B;border:1.5px solid var(--nav-bg)"></span>
		</button>

		<a href="/consultation" style="display:inline-flex;align-items:center;gap:6px;padding:6px 12px;border-radius:7px;background:var(--accent);color:white;text-decoration:none;font-size:13px;font-weight:500">
			<Icon name="plus" size={14} color="white" />
			Consultation
		</a>

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
					<div style="font-size:11px;opacity:0.55">{data.user.role === 'Doctor' ? 'Généraliste' : data.user.role}</div>
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
							{data.user.role === 'Doctor' ? 'Médecin généraliste' : data.user.role}
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
