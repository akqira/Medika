<script lang="ts">
	import type { PageData } from './$types';
	import StatCard from '$lib/components/StatCard.svelte';
	import Badge from '$lib/components/Badge.svelte';
	import Icon from '$lib/components/Icon.svelte';

	let { data }: { data: PageData } = $props();

	const STATUS_LABEL: Record<string, string> = {
		Confirmed: 'confirmé', InProgress: 'en cours', Pending: 'en attente',
		Completed: 'terminé', Cancelled: 'annulé', NoShow: 'absent'
	};

	// Pastel background + text color per status for the initials circle
	const APPT_STYLE: Record<string, { bg: string; color: string }> = {
		Confirmed:  { bg: '#DBEAFE', color: '#1D4ED8' },
		InProgress: { bg: 'var(--primary-light)', color: 'var(--primary)' },
		Pending:    { bg: 'var(--warning-light)', color: 'var(--warning)' },
		Completed:  { bg: '#F3F4F6', color: '#6B7280' },
		Cancelled:  { bg: 'var(--danger-light)', color: 'var(--danger)' },
		NoShow:     { bg: 'var(--danger-light)', color: 'var(--danger)' },
	};

	const remaining  = $derived(data.appointments.filter(a => a.status === 'Pending' || a.status === 'Confirmed').length);
	const inProgress = $derived(data.appointments.filter(a => a.status === 'InProgress').length);
	const alertAppts = $derived(data.appointments.filter(a => a.patientAllergies.length > 0));

	const todayLabel = $derived(
		new Date(data.today + 'T00:00:00').toLocaleDateString('fr-FR', {
			weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
		})
	);

	function initials(name: string) {
		return name.split(' ').map(n => n[0] ?? '').join('').slice(0, 2).toUpperCase();
	}

	function lastVisit(iso: string | undefined) {
		if (!iso) return null;
		return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
	}
</script>

<div style="padding:24px 28px">

	<!-- Header -->
	<div style="margin-bottom:22px">
		<h1 style="font-size:20px;font-weight:700;letter-spacing:-0.3px">Tableau de bord</h1>
		<p style="font-size:13.5px;color:var(--text-muted);margin-top:4px;text-transform:capitalize">
			{todayLabel} — Bienvenue, <strong style="color:var(--text);font-weight:600">{data.user.fullName}</strong>
		</p>
	</div>

	<!-- Stat cards -->
	<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:14px;margin-bottom:22px">
		<StatCard label="Patients aujourd'hui" value={data.appointments.length} icon="calendar"    sub="Programme du jour"         trend={8}  />
		<StatCard label="Restant à voir"        value={remaining}               icon="clock"       color="var(--warning)"  sub="À venir ce jour"           />
		<StatCard label="En cours"              value={inProgress}              icon="activity"    color="var(--primary)"  sub="Consultations actives"     />
		<StatCard label="Patients total"        value={data.totalPatients}      icon="users"       color="#7C3AED"         sub="{data.totalPatients} actifs au total" />
	</div>

	<!-- 2-column -->
	<div style="display:grid;grid-template-columns:1fr 316px;gap:16px;align-items:start">

		<!-- Programme du jour -->
		<div class="card" style="overflow:hidden">
			<div style="padding:16px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
				<div>
					<h2 style="font-size:15px;font-weight:600">Programme du jour</h2>
					<p style="font-size:12.5px;color:var(--text-muted);margin-top:2px">
						{data.appointments.length} rendez-vous · <span style="text-transform:capitalize">{todayLabel.split(' ').slice(0,3).join(' ')}</span>
					</p>
				</div>
				<a href="/schedule" style="display:inline-flex;align-items:center;gap:6px;padding:7px 13px;background:var(--bg);border:1px solid var(--border);border-radius:7px;text-decoration:none;color:var(--text);font-size:13px;font-weight:500">
					<Icon name="calendar" size={13} color="var(--text-muted)" />
					Agenda complet
				</a>
			</div>

			{#if data.appointments.length === 0}
				<div style="padding:52px 20px;text-align:center;color:var(--text-muted)">
					<Icon name="calendar" size={36} color="var(--border-strong)" />
					<p style="margin-top:12px;font-size:14px">Aucun rendez-vous aujourd'hui</p>
					<p style="font-size:13px;color:var(--text-light);margin-top:4px">Le backend n'est pas encore connecté</p>
				</div>
			{:else}
				{#each data.appointments as appt}
					{@const s = APPT_STYLE[appt.status] ?? APPT_STYLE.Completed}
					<div style="display:flex;align-items:center;gap:14px;padding:13px 20px;border-bottom:1px solid var(--border)">

						<!-- Time -->
						<div style="width:42px;flex-shrink:0;text-align:center">
							<div style="font-size:14px;font-weight:700;line-height:1">{appt.time}</div>
							<div style="font-size:11px;color:var(--text-muted);margin-top:2px">{appt.durationMinutes}min</div>
						</div>

						<!-- Colored initials -->
						<div style="width:34px;height:34px;border-radius:50%;flex-shrink:0;background:{s.bg};display:flex;align-items:center;justify-content:center;font-size:11.5px;font-weight:700;color:{s.color}">
							{initials(appt.patientName)}
						</div>

						<!-- Info -->
						<div style="flex:1;min-width:0">
							<div style="font-size:14px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{appt.patientName}</div>
							<div style="font-size:12.5px;color:var(--text-muted);margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{appt.reason || appt.type}</div>
						</div>

						<!-- Allergy warning -->
						{#if appt.patientAllergies.length > 0}
							<span style="font-size:13px;color:var(--warning);flex-shrink:0" title="Allergie : {appt.patientAllergies.join(', ')}">⚠</span>
						{/if}

						<!-- Status or CTA -->
						{#if appt.status === 'InProgress'}
							<a href="/consultation" style="display:inline-flex;align-items:center;gap:6px;padding:7px 14px;background:var(--primary);color:white;border-radius:7px;text-decoration:none;font-size:13px;font-weight:600;white-space:nowrap;flex-shrink:0">
								<Icon name="stethoscope" size={13} color="white" />
								Consulter
							</a>
						{:else}
							<Badge status={appt.status}>{STATUS_LABEL[appt.status]}</Badge>
						{/if}

					</div>
				{/each}
			{/if}
		</div>

		<!-- Right column -->
		<div style="display:flex;flex-direction:column;gap:14px">

			<!-- Patients récents -->
			<div class="card" style="overflow:hidden">
				<div style="padding:14px 16px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
					<h2 style="font-size:14.5px;font-weight:600">Patients récents</h2>
					<a href="/patients" style="font-size:12.5px;color:var(--primary);text-decoration:none;font-weight:500">Voir tout →</a>
				</div>
				{#if data.recentPatients.length === 0}
					<div style="padding:24px;text-align:center;color:var(--text-muted);font-size:13px">Aucun patient</div>
				{:else}
					{#each data.recentPatients as p}
						<a href="/patients" style="display:flex;align-items:center;gap:10px;padding:11px 16px;border-bottom:1px solid var(--border);text-decoration:none;color:var(--text)">
							<!-- Initials circle (gender-based) -->
							<div style="
								width:32px;height:32px;border-radius:50%;flex-shrink:0;
								background:{p.gender === 'F' ? '#FDF2F8' : 'var(--primary-50)'};
								border:1.5px solid {p.gender === 'F' ? '#FCE7F3' : 'var(--primary-light)'};
								display:flex;align-items:center;justify-content:center;
								font-size:11px;font-weight:700;
								color:{p.gender === 'F' ? '#9D174D' : 'var(--primary)'};
							">
								{(p.firstName[0] ?? '').toUpperCase()}{(p.lastName[0] ?? '').toUpperCase()}
							</div>
							<div style="flex:1;min-width:0">
								<div style="font-size:13px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{p.firstName} {p.lastName}</div>
								{#if p.lastVisitAt}
									<div style="font-size:11.5px;color:var(--text-muted);margin-top:1px">Dernière visite : {lastVisit(p.lastVisitAt)}</div>
								{:else}
									<div style="font-size:11.5px;color:var(--text-muted);margin-top:1px">{p.age} ans · {p.gender === 'F' ? 'Femme' : 'Homme'}</div>
								{/if}
							</div>
							<Icon name="chevronRight" size={14} color="var(--border-strong)" />
						</a>
					{/each}
				{/if}
			</div>

			<!-- Rappels & alertes (only when data exists) -->
			{#if alertAppts.length > 0}
				<div class="card" style="overflow:hidden">
					<div style="padding:14px 16px;border-bottom:1px solid var(--border)">
						<h2 style="font-size:14.5px;font-weight:600">Rappels &amp; alertes</h2>
					</div>
					{#each alertAppts as appt}
						<div style="display:flex;align-items:flex-start;gap:10px;padding:12px 16px;border-bottom:1px solid var(--border)">
							<div style="width:22px;height:22px;border-radius:50%;background:var(--warning-light);display:flex;align-items:center;justify-content:center;flex-shrink:0;margin-top:1px">
								<Icon name="alertCircle" size={12} color="var(--warning)" />
							</div>
							<div style="font-size:13px">
								<span style="font-weight:600">Allergie</span> — {appt.patientName}
								<div style="font-size:12px;color:var(--text-muted);margin-top:2px">{appt.patientAllergies.join(', ')}</div>
							</div>
						</div>
					{/each}
				</div>
			{/if}

		</div>
	</div>
</div>
