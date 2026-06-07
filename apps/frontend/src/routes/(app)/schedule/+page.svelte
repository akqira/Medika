<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import type { PageData } from './$types';
	import type { AppointmentSlot } from '$lib/types/api';
	import Badge from '$lib/components/Badge.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import BookingModal from './BookingModal.svelte';

	let { data }: { data: PageData } = $props();

	let selectedAppt = $state<AppointmentSlot | null>(null);
	let appointments = $state<AppointmentSlot[]>(data.appointments);
	let showBookingModal = $state(false);

	// Keep local appointments in sync with server data
	$effect(() => {
		appointments = data.appointments;
		selectedAppt = null;
	});

	// Status action state
	let actionLoading  = $state<string | null>(null); // 'confirm' | 'no-show' | 'cancel'
	let cancelReason   = $state('');
	let showCancelForm = $state(false);

	$effect(() => {
		if (!selectedAppt) {
			showCancelForm = false;
			cancelReason   = '';
		}
	});

	const STATUS_LABEL: Record<string, string> = {
		Confirmed: 'confirmé', InProgress: 'en cours', Pending: 'en attente',
		Completed: 'terminé', Cancelled: 'annulé', NoShow: 'absent'
	};

	const STATUS_COLOR: Record<string, string> = {
		Confirmed:  'var(--success)',
		InProgress: 'var(--primary)',
		Pending:    'var(--warning)',
		Completed:  'var(--primary)',
		Cancelled:  'var(--danger)',
		NoShow:     'var(--text-muted)',
	};

	const STATUS_BG: Record<string, string> = {
		Confirmed:  'var(--success-light)',
		InProgress: 'var(--primary-50)',
		Pending:    'var(--warning-light)',
		Completed:  'var(--primary-50)',
		Cancelled:  'var(--danger-light)',
		NoShow:     'var(--border)',
	};

	const AVATAR: Record<string, { bg: string; color: string }> = {
		Confirmed:  { bg: '#DBEAFE', color: '#1D4ED8' },
		InProgress: { bg: 'var(--primary-light)', color: 'var(--primary)' },
		Pending:    { bg: 'var(--warning-light)', color: 'var(--warning)' },
		Completed:  { bg: '#F3F4F6', color: '#6B7280' },
		Cancelled:  { bg: 'var(--danger-light)', color: 'var(--danger)' },
		NoShow:     { bg: '#F3F4F6', color: 'var(--text-muted)' },
	};

	const DAY_SHORT = ['Dim', 'Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam'];

	const weekDates = $derived.by(() => {
		const d = new Date(data.date + 'T00:00:00');
		const dow = d.getDay();
		const monday = new Date(d);
		monday.setDate(d.getDate() - ((dow + 6) % 7));
		return Array.from({ length: 7 }, (_, i) => {
			const day = new Date(monday);
			day.setDate(monday.getDate() + i);
			const iso = day.toISOString().split('T')[0];
			return {
				iso,
				dayShort: DAY_SHORT[day.getDay()],
				num: day.getDate(),
				isSelected: iso === data.date,
				count: iso === data.date ? (appointments.length as number | null) : null,
			};
		});
	});

	const weekLabel = $derived.by(() => {
		const first = new Date(weekDates[0].iso + 'T00:00:00');
		const last  = new Date(weekDates[6].iso + 'T00:00:00');
		const d1 = first.getDate();
		const d2 = last.getDate();
		const m2 = last.toLocaleDateString('fr-FR', { month: 'long' });
		const y  = last.getFullYear();
		if (first.getMonth() === last.getMonth()) return `Semaine du ${d1} au ${d2} ${m2} ${y}`;
		const m1 = first.toLocaleDateString('fr-FR', { month: 'long' });
		return `Semaine du ${d1} ${m1} au ${d2} ${m2} ${y}`;
	});

	const prevWeekDate = $derived.by(() => {
		const d = new Date(weekDates[0].iso + 'T00:00:00');
		d.setDate(d.getDate() - 7);
		return d.toISOString().split('T')[0];
	});

	const nextWeekDate = $derived.by(() => {
		const d = new Date(weekDates[0].iso + 'T00:00:00');
		d.setDate(d.getDate() + 7);
		return d.toISOString().split('T')[0];
	});

	const dateLabel = $derived(
		new Date(data.date + 'T00:00:00').toLocaleDateString('fr-FR', {
			day: '2-digit', month: '2-digit', year: 'numeric'
		})
	);

	// Timeline: 08:00–18:00, 52px per 30-min slot
	const SLOT_H = 52;
	const START_MIN = 8 * 60;
	const END_MIN   = 18 * 60;
	const TOTAL_SLOTS = (END_MIN - START_MIN) / 30;

	const timeLines = Array.from({ length: TOTAL_SLOTS + 1 }, (_, i) => {
		const total = START_MIN + i * 30;
		const h = Math.floor(total / 60);
		const m = total % 60;
		return { top: i * SLOT_H, isHour: m === 0, label: m === 0 ? `${String(h).padStart(2, '0')}:00` : '' };
	});

	function apptPos(appt: AppointmentSlot) {
		const [h, m] = appt.time.split(':').map(Number);
		const startMin = Math.max(START_MIN, Math.min(END_MIN - 30, h * 60 + m));
		const top    = ((startMin - START_MIN) / 30) * SLOT_H;
		const height = Math.max((appt.durationMinutes / 30) * SLOT_H - 4, 40);
		return { top, height };
	}

	function initials(name: string) {
		return name.split(' ').map(n => n[0] ?? '').join('').slice(0, 2).toUpperCase();
	}

	// Status action helpers
	function updateLocalStatus(id: string, status: AppointmentSlot['status']) {
		appointments = appointments.map(a => a.id === id ? { ...a, status } : a);
		if (selectedAppt?.id === id) {
			selectedAppt = { ...selectedAppt, status };
		}
	}

	async function confirmAppt(id: string) {
		actionLoading = 'confirm';
		try {
			const res = await fetch(`/api/appointments/${id}/confirm`, { method: 'PATCH' });
			if (res.ok) updateLocalStatus(id, 'Confirmed');
		} finally {
			actionLoading = null;
		}
	}

	async function noShowAppt(id: string) {
		actionLoading = 'no-show';
		try {
			const res = await fetch(`/api/appointments/${id}/no-show`, { method: 'PATCH' });
			if (res.ok) updateLocalStatus(id, 'NoShow');
		} finally {
			actionLoading = null;
		}
	}

	async function cancelAppt(id: string) {
		actionLoading = 'cancel';
		try {
			const res = await fetch(`/api/appointments/${id}/cancel`, {
				method: 'PATCH',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ reason: cancelReason }),
			});
			if (res.ok) {
				updateLocalStatus(id, 'Cancelled');
				showCancelForm = false;
				cancelReason = '';
			}
		} finally {
			actionLoading = null;
		}
	}

	function handleBooked() {
		showBookingModal = false;
		invalidateAll();
	}
</script>

<BookingModal
	bind:open={showBookingModal}
	defaultDate={data.date}
	onbooked={handleBooked}
/>

<div style="display:flex;flex-direction:column;height:calc(100vh - 58px);overflow:hidden;background:var(--bg)">

	<!-- ── Header ── -->
	<div style="padding:20px 28px 0;background:var(--bg)">

		<!-- Title row -->
		<div style="display:flex;align-items:center;gap:10px;margin-bottom:18px">
			<h1 style="font-size:20px;font-weight:700;letter-spacing:-0.3px">Agenda</h1>

			<a href="/schedule?date={prevWeekDate}"
				style="display:flex;align-items:center;justify-content:center;width:28px;height:28px;border-radius:6px;background:var(--surface);border:1px solid var(--border);color:var(--text-muted);text-decoration:none;flex-shrink:0">
				<Icon name="chevronLeft" size={13} />
			</a>
			<a href="/schedule?date={nextWeekDate}"
				style="display:flex;align-items:center;justify-content:center;width:28px;height:28px;border-radius:6px;background:var(--surface);border:1px solid var(--border);color:var(--text-muted);text-decoration:none;flex-shrink:0">
				<Icon name="chevronRight" size={13} />
			</a>

			<span style="font-size:13.5px;color:var(--text-muted);font-weight:400">{weekLabel}</span>

			<div style="flex:1"></div>

			<button
				type="button"
				onclick={() => showBookingModal = true}
				style="display:inline-flex;align-items:center;gap:7px;padding:9px 16px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;flex-shrink:0;cursor:pointer"
			>
				<Icon name="plus" size={14} color="white" />
				Nouveau rendez-vous
			</button>
		</div>

		<!-- Week strip -->
		<div style="display:flex;gap:3px">
			{#each weekDates as wd}
				<a href="/schedule?date={wd.iso}" style="
					flex:1;display:flex;flex-direction:column;align-items:center;
					padding:10px 6px 12px;border-radius:10px 10px 0 0;text-decoration:none;
					background:{wd.isSelected ? 'var(--primary)' : 'transparent'};
				">
					<span style="font-size:10px;font-weight:600;text-transform:uppercase;letter-spacing:0.6px;
						color:{wd.isSelected ? 'rgba(255,255,255,0.72)' : 'var(--text-muted)'}">
						{wd.dayShort}
					</span>
					<span style="font-size:22px;font-weight:700;line-height:1.1;margin-top:3px;
						color:{wd.isSelected ? 'white' : 'var(--text)'}">
						{wd.num}
					</span>
					{#if wd.count !== null}
						<span style="font-size:10px;margin-top:4px;
							color:{wd.isSelected ? 'rgba(255,255,255,0.65)' : 'var(--text-muted)'}">
							{wd.count} rdv
						</span>
					{:else}
						<span style="font-size:10px;margin-top:4px;color:transparent">0</span>
					{/if}
				</a>
			{/each}
		</div>
	</div>

	<!-- ── Content ── -->
	<div style="display:flex;flex:1;overflow:hidden;border-top:1px solid var(--border)">

		<!-- Timeline -->
		<div style="flex:1;overflow-y:auto;background:var(--surface)">
			<div style="position:relative;padding-left:64px;height:{TOTAL_SLOTS * SLOT_H + 32}px">

				{#each timeLines as tl}
					{#if tl.label}
						<div style="position:absolute;left:0;top:{tl.top - 9}px;width:56px;text-align:right;
							padding-right:12px;font-size:11px;font-weight:500;color:var(--text-muted);user-select:none">
							{tl.label}
						</div>
					{/if}
					<div style="position:absolute;left:64px;right:0;top:{tl.top}px;
						border-top:{tl.isHour ? '1px solid var(--border)' : '1px dashed rgba(0,0,0,0.05)'}">
					</div>
				{/each}

				{#each appointments as appt}
					{@const pos = apptPos(appt)}
					{@const av  = AVATAR[appt.status]  ?? AVATAR.Completed}
					{@const col = STATUS_COLOR[appt.status] ?? '#D1D5DB'}
					<button
						onclick={() => selectedAppt = selectedAppt?.id === appt.id ? null : appt}
						style="
							position:absolute;left:68px;right:14px;top:{pos.top + 2}px;height:{pos.height}px;
							background:white;border:1px solid var(--border);border-left:3px solid {col};
							border-radius:7px;padding:0 12px;text-align:left;cursor:pointer;
							display:flex;align-items:center;gap:10px;font-family:inherit;
							box-shadow:{selectedAppt?.id === appt.id ? `0 0 0 2px ${col}55` : '0 1px 3px rgba(0,0,0,0.05)'};
						"
					>
						<div style="width:30px;height:30px;border-radius:50%;flex-shrink:0;
							background:{av.bg};color:{av.color};
							display:flex;align-items:center;justify-content:center;
							font-size:10.5px;font-weight:700">
							{initials(appt.patientName)}
						</div>
						<div style="flex:1;min-width:0">
							<div style="font-size:13.5px;font-weight:600;color:var(--text);
								overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
								{appt.patientName}
							</div>
							{#if pos.height > 42}
								<div style="font-size:12px;color:var(--text-muted);margin-top:1px;
									overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
									{appt.reason || appt.type}
								</div>
							{/if}
						</div>
						<span style="font-size:12px;color:var(--text-muted);flex-shrink:0;white-space:nowrap">
							{appt.durationMinutes} min
						</span>
					</button>
				{/each}

				{#if appointments.length === 0}
					<div style="position:absolute;top:80px;left:64px;right:0;
						display:flex;flex-direction:column;align-items:center;gap:10px;color:var(--text-muted)">
						<Icon name="calendar" size={36} color="var(--border-strong)" />
						<p style="font-size:13.5px">Aucun rendez-vous planifié ce jour</p>
					</div>
				{/if}
			</div>
		</div>

		<!-- Right panel -->
		<div style="width:320px;border-left:1px solid var(--border);background:var(--surface);
			display:flex;flex-direction:column;overflow:hidden;flex-shrink:0">

			{#if selectedAppt}
				{@const av = AVATAR[selectedAppt.status] ?? AVATAR.Completed}
				<!-- Detail view -->
				<div style="padding:16px 20px;border-bottom:1px solid var(--border);
					display:flex;align-items:center;justify-content:space-between">
					<h3 style="font-size:14px;font-weight:600">Détail du rendez-vous</h3>
					<button onclick={() => selectedAppt = null}
						style="background:none;border:none;cursor:pointer;color:var(--text-muted);
							display:flex;align-items:center;padding:4px;border-radius:5px">
						<Icon name="x" size={15} />
					</button>
				</div>

				<div style="flex:1;overflow-y:auto;padding:20px">

					<div style="display:flex;flex-direction:column;align-items:center;text-align:center;margin-bottom:20px">
						<div style="width:56px;height:56px;border-radius:50%;
							background:{av.bg};color:{av.color};
							display:flex;align-items:center;justify-content:center;
							font-size:19px;font-weight:700">
							{initials(selectedAppt.patientName)}
						</div>
						<div style="margin-top:10px;font-size:15px;font-weight:700">{selectedAppt.patientName}</div>
						<div style="margin-top:7px">
							<Badge status={selectedAppt.status}>{STATUS_LABEL[selectedAppt.status]}</Badge>
						</div>
					</div>

					<div style="display:flex;flex-direction:column;gap:6px;margin-bottom:16px">
						{#each [
							{ icon: 'clock',       label: 'Heure',  val: `${selectedAppt.time} · ${selectedAppt.durationMinutes} min` },
							{ icon: 'stethoscope', label: 'Type',   val: selectedAppt.type },
							...(selectedAppt.patientPhone     ? [{ icon: 'phone',    label: 'Tél.',   val: selectedAppt.patientPhone }]     : []),
							...(selectedAppt.patientBloodGroup ? [{ icon: 'activity', label: 'Groupe', val: selectedAppt.patientBloodGroup }] : []),
						] as row}
							<div style="display:flex;align-items:center;gap:8px;font-size:13px;
								padding:8px 10px;background:var(--bg);border-radius:7px">
								<Icon name={row.icon} size={13} color="var(--text-muted)" />
								<span style="color:var(--text-muted);width:44px;flex-shrink:0">{row.label}</span>
								<span style="font-weight:600;margin-left:auto;text-align:right;color:var(--text)">{row.val}</span>
							</div>
						{/each}
					</div>

					{#if selectedAppt.reason}
						<div style="background:var(--bg);border-radius:8px;padding:12px;margin-bottom:10px">
							<div style="font-size:10.5px;font-weight:600;color:var(--text-muted);
								text-transform:uppercase;letter-spacing:0.5px;margin-bottom:5px">Motif</div>
							<div style="font-size:13.5px;color:var(--text)">{selectedAppt.reason}</div>
						</div>
					{/if}

					{#if selectedAppt.patientAllergies.length > 0}
						<div style="background:var(--danger-light);border-radius:8px;padding:12px;margin-bottom:14px">
							<div style="font-size:10.5px;font-weight:600;color:var(--danger);
								text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px">Allergies</div>
							<div style="display:flex;flex-wrap:wrap;gap:4px">
								{#each selectedAppt.patientAllergies as a}
									<span style="background:white;color:var(--danger);font-size:11.5px;
										padding:2px 8px;border-radius:20px">{a}</span>
								{/each}
							</div>
						</div>
					{/if}

					<!-- Status action buttons -->
					<div style="display:flex;flex-direction:column;gap:8px;margin-bottom:14px">
						{#if selectedAppt.status === 'Pending'}
							<button
								type="button"
								disabled={actionLoading !== null}
								onclick={() => confirmAppt(selectedAppt!.id)}
								style="display:flex;align-items:center;justify-content:center;gap:7px;padding:9px;background:var(--success-light);color:var(--success);border:1px solid var(--success);border-radius:7px;font-family:inherit;font-size:13px;font-weight:600;cursor:pointer;opacity:{actionLoading === 'confirm' ? 0.6 : 1}"
							>
								<Icon name="check" size={14} color="var(--success)" />
								{actionLoading === 'confirm' ? 'Confirmation…' : 'Confirmer'}
							</button>
						{/if}

						{#if selectedAppt.status === 'Pending' || selectedAppt.status === 'Confirmed'}
							<button
								type="button"
								disabled={actionLoading !== null}
								onclick={() => noShowAppt(selectedAppt!.id)}
								style="display:flex;align-items:center;justify-content:center;gap:7px;padding:9px;background:var(--bg);color:var(--text-muted);border:1px solid var(--border);border-radius:7px;font-family:inherit;font-size:13px;font-weight:500;cursor:pointer;opacity:{actionLoading === 'no-show' ? 0.6 : 1}"
							>
								<Icon name="x" size={14} color="var(--text-muted)" />
								{actionLoading === 'no-show' ? 'Enregistrement…' : 'Patient absent'}
							</button>
						{/if}

						{#if selectedAppt.status !== 'Completed' && selectedAppt.status !== 'Cancelled'}
							{#if showCancelForm}
								<div style="background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;padding:12px">
									<div style="font-size:12px;font-weight:600;color:var(--danger);margin-bottom:8px">Motif d'annulation</div>
									<input
										type="text"
										bind:value={cancelReason}
										placeholder="Motif (optionnel)…"
										class="mk-input"
										style="margin-bottom:8px;font-size:13px"
									/>
									<div style="display:flex;gap:8px">
										<button
											type="button"
											onclick={() => showCancelForm = false}
											style="flex:1;padding:7px;background:white;color:var(--text-muted);border:1px solid var(--border);border-radius:6px;font-family:inherit;font-size:12.5px;cursor:pointer"
										>
											Annuler
										</button>
										<button
											type="button"
											disabled={actionLoading !== null}
											onclick={() => cancelAppt(selectedAppt!.id)}
											style="flex:2;padding:7px;background:var(--danger);color:white;border:none;border-radius:6px;font-family:inherit;font-size:12.5px;font-weight:600;cursor:pointer;opacity:{actionLoading === 'cancel' ? 0.6 : 1}"
										>
											{actionLoading === 'cancel' ? 'Annulation…' : 'Confirmer l\'annulation'}
										</button>
									</div>
								</div>
							{:else}
								<button
									type="button"
									onclick={() => showCancelForm = true}
									style="display:flex;align-items:center;justify-content:center;gap:7px;padding:9px;background:var(--danger-light);color:var(--danger);border:1px solid #FECACA;border-radius:7px;font-family:inherit;font-size:13px;font-weight:500;cursor:pointer"
								>
									<Icon name="x" size={14} color="var(--danger)" />
									Annuler le rendez-vous
								</button>
							{/if}
						{/if}
					</div>

					<a href="/consultation?appointmentId={selectedAppt.id}"
						style="display:flex;align-items:center;justify-content:center;gap:7px;
							padding:11px;background:var(--primary);color:white;border-radius:8px;
							text-decoration:none;font-weight:600;font-size:14px">
						<Icon name="stethoscope" size={15} color="white" />
						Démarrer la consultation
					</a>
				</div>

			{:else}
				<!-- Appointment list -->
				<div style="padding:16px 20px;border-bottom:1px solid var(--border)">
					<h3 style="font-size:13.5px;font-weight:700;color:var(--text)">RDV du {dateLabel}</h3>
					<p style="font-size:12px;color:var(--text-muted);margin-top:2px">
						{appointments.length} rendez-vous programmés
					</p>
				</div>

				<div style="flex:1;overflow-y:auto">
					{#if appointments.length === 0}
						<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;
							height:180px;color:var(--text-muted);gap:10px">
							<Icon name="calendar" size={34} color="var(--border-strong)" />
							<p style="font-size:13px;text-align:center;line-height:1.5">
								Aucun rendez-vous<br>planifié ce jour
							</p>
						</div>
					{:else}
						{#each appointments as appt}
							{@const av = AVATAR[appt.status] ?? AVATAR.Completed}
							<button
								onclick={() => selectedAppt = appt}
								style="display:flex;align-items:flex-start;gap:12px;width:100%;
									padding:12px 20px;text-align:left;background:none;border:none;
									border-bottom:1px solid var(--border);cursor:pointer;font-family:inherit;"
							>
								<div style="width:34px;height:34px;border-radius:50%;flex-shrink:0;
									background:{av.bg};color:{av.color};margin-top:1px;
									display:flex;align-items:center;justify-content:center;
									font-size:11.5px;font-weight:700">
									{initials(appt.patientName)}
								</div>
								<div style="flex:1;min-width:0">
									<div style="font-size:13.5px;font-weight:600;color:var(--text)">
										{appt.patientName}
									</div>
									<div style="font-size:12px;color:var(--text-muted);margin-top:2px;
										overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
										{appt.time} · {appt.reason || appt.type}
									</div>
									<div style="margin-top:6px">
										<Badge status={appt.status}>{STATUS_LABEL[appt.status]}</Badge>
									</div>
								</div>
							</button>
						{/each}
					{/if}
				</div>

				<div style="padding:14px 20px;border-top:1px solid var(--border)">
					<button
						type="button"
						onclick={() => showBookingModal = true}
						style="display:flex;align-items:center;justify-content:center;gap:7px;width:100%;
							padding:10px;background:var(--primary);color:white;border:none;border-radius:8px;
							font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer"
					>
						<Icon name="plus" size={14} color="white" />
						Nouveau rendez-vous
					</button>
				</div>
			{/if}
		</div>
	</div>
</div>
