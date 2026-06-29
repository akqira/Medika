<script lang="ts">
	import { enhance } from '$app/forms';
	import { frValidation } from '$lib/actions/frValidation';
	import type { PageData, ActionData } from './$types';
	import type { Charge } from '$lib/types/api';
	import StatCard from '$lib/components/StatCard.svelte';
	import Icon from '$lib/components/Icon.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	const MONTHS = ['Janvier','Février','Mars','Avril','Mai','Juin','Juillet','Août','Septembre','Octobre','Novembre','Décembre'];

	const fmt = new Intl.NumberFormat('fr-DZ', { maximumFractionDigits: 0 });

	const prev = $derived(
		data.month === 1
			? { year: data.year - 1, month: 12 }
			: { year: data.year, month: data.month - 1 }
	);
	const next = $derived(
		data.month === 12
			? { year: data.year + 1, month: 1 }
			: { year: data.year, month: data.month + 1 }
	);

	const maxTrend = $derived(
		Math.max(...data.summary.monthlyTrend.map(t => t.amount), 1)
	);

	// Charge form state
	let showChargeForm = $state(false);
	let submitting     = $state(false);
	const today = new Date().toISOString().split('T')[0];

	// value = backend ChargeCategory enum member (sent to the API); label = French UI text.
	// (Previously the French labels were sent raw and Enum.Parse rejected most of them.)
	const CHARGE_CATEGORIES = [
		{ value: 'Rent',        label: 'Loyer',        icon: 'mapPin' },
		{ value: 'Internet',    label: 'Internet',     icon: 'activity' },
		{ value: 'Phone',       label: 'Téléphone',    icon: 'phone' },
		{ value: 'Insurance',   label: 'Assurance',    icon: 'shieldCheck' },
		{ value: 'Equipment',   label: 'Matériel',     icon: 'clipboard' },
		{ value: 'Maintenance', label: 'Maintenance',  icon: 'settings' },
		{ value: 'Accounting',  label: 'Comptabilité', icon: 'fileText' },
		{ value: 'Other',       label: 'Autre',        icon: 'dollar' },
	];
	const CATEGORY_LABEL: Record<string, string> = Object.fromEntries(CHARGE_CATEGORIES.map((c) => [c.value, c.label]));
	const CATEGORY_ICON: Record<string, string> = Object.fromEntries(CHARGE_CATEGORIES.map((c) => [c.value, c.icon]));

	function formatChargeDate(iso: string) {
		return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
	}

	// Close form on success
	$effect(() => {
		if (form?.success) showChargeForm = false;
	});
</script>

<div style="padding:24px;max-width:1200px;margin:0 auto">

	<!-- Header -->
	<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:24px">
		<div>
			<h1 style="font-size:20px;font-weight:700;letter-spacing:-0.3px">Finances</h1>
			<p style="font-size:13.5px;color:var(--text-muted);margin-top:3px">{MONTHS[data.month - 1]} {data.year}</p>
		</div>
		<div style="display:flex;align-items:center;gap:6px">
			<a href="/finance?year={prev.year}&month={prev.month}" style="display:flex;align-items:center;justify-content:center;width:34px;height:34px;border-radius:7px;background:var(--surface);border:1px solid var(--border);text-decoration:none;color:var(--text-muted)">
				<Icon name="chevronLeft" size={15} />
			</a>
			<span style="font-size:13.5px;font-weight:500;padding:0 8px">{MONTHS[data.month - 1]} {data.year}</span>
			<a href="/finance?year={next.year}&month={next.month}" style="display:flex;align-items:center;justify-content:center;width:34px;height:34px;border-radius:7px;background:var(--surface);border:1px solid var(--border);text-decoration:none;color:var(--text-muted)">
				<Icon name="chevronRight" size={15} />
			</a>
			<a
				href="/actes"
				style="display:flex;align-items:center;gap:7px;padding:9px 14px;background:var(--surface);color:var(--text);border:1px solid var(--border);border-radius:8px;font-size:13.5px;font-weight:600;text-decoration:none;margin-left:6px"
			>
				<Icon name="fileText" size={14} color="var(--text-muted)" />
				Catalogue d'actes
			</a>
			<button
				type="button"
				onclick={() => showChargeForm = !showChargeForm}
				style="display:flex;align-items:center;gap:7px;padding:9px 16px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer"
			>
				<Icon name="plus" size={14} color="white" />
				Ajouter une charge
			</button>
		</div>
	</div>

	<!-- Action feedback -->
	{#if form?.success}
		<div style="display:flex;align-items:center;gap:8px;padding:12px 16px;background:var(--success-light);border:1px solid #A7F3D0;border-radius:8px;margin-bottom:20px">
			<Icon name="checkCircle" size={15} color="var(--success)" />
			<span style="font-size:13.5px;color:var(--success);font-weight:500">{form.success}</span>
		</div>
	{/if}
	{#if form?.error}
		<div style="display:flex;align-items:center;gap:8px;padding:12px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;margin-bottom:20px">
			<Icon name="alertCircle" size={15} color="var(--danger)" />
			<span style="font-size:13.5px;color:var(--danger);font-weight:500">{form.error}</span>
		</div>
	{/if}

	<!-- Add charge form (inline) -->
	{#if showChargeForm}
		<div class="card" style="padding:20px;margin-bottom:24px;border-left:3px solid var(--primary)">
			<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
				<h2 style="font-size:14.5px;font-weight:700">Nouvelle charge</h2>
				<button
					type="button"
					onclick={() => showChargeForm = false}
					style="background:none;border:none;cursor:pointer;color:var(--text-muted);display:flex;align-items:center"
				>
					<Icon name="x" size={16} />
				</button>
			</div>

			<form
				method="POST"
				action="?/addCharge&year={data.year}&month={data.month}"
				use:frValidation
				use:enhance={() => {
					submitting = true;
					return async ({ update }) => {
						submitting = false;
						await update();
					};
				}}
			>
				<div style="display:grid;grid-template-columns:1fr 1fr;gap:14px;margin-bottom:16px">

					<div>
						<label for="charge-category" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Catégorie <span style="color:var(--danger)">*</span></label>
						<select id="charge-category" name="category" required class="mk-input">
							<option value="">— Choisir —</option>
							{#each CHARGE_CATEGORIES as cat}
								<option value={cat.value}>{cat.label}</option>
							{/each}
						</select>
					</div>

					<div>
						<label for="charge-amount" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Montant (DA) <span style="color:var(--danger)">*</span></label>
						<input id="charge-amount" name="amount" type="number" min="1" required class="mk-input" placeholder="ex: 5000" />
					</div>

					<div style="grid-column:1/-1">
						<label for="charge-description" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Description <span style="color:var(--danger)">*</span></label>
						<input id="charge-description" name="description" type="text" required class="mk-input" placeholder="ex: Loyer du cabinet – Juin 2026" />
					</div>

					<div>
						<label for="charge-date" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Date</label>
						<input id="charge-date" name="date" type="date" value={today} class="mk-input" />
					</div>

				</div>

				<div style="display:flex;gap:10px">
					<button
						type="button"
						onclick={() => showChargeForm = false}
						style="padding:9px 18px;background:var(--bg);color:var(--text);border:1px solid var(--border);border-radius:7px;font-family:inherit;font-size:13.5px;cursor:pointer"
					>
						Annuler
					</button>
					<button
						type="submit"
						disabled={submitting}
						style="padding:9px 22px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer;opacity:{submitting ? 0.6 : 1}"
					>
						{submitting ? 'Enregistrement…' : 'Ajouter la charge'}
					</button>
				</div>
			</form>
		</div>
	{/if}

	<!-- Stat cards -->
	<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:14px;margin-bottom:24px">
		<StatCard label="Revenus"     value="{fmt.format(data.summary.totalIncome)} DA"   icon="dollar"      color="var(--success)" sub="encaissés" />
		<StatCard label="Charges"     value="{fmt.format(data.summary.totalCharges)} DA"  icon="fileText"    color="var(--danger)"  sub="ce mois"  />
		<StatCard label="Bénéfice"    value="{fmt.format(data.summary.netIncome)} DA"     icon="barchart"    color={data.summary.netIncome >= 0 ? 'var(--primary)' : 'var(--danger)'} sub="net" />
		<StatCard label="Impayés"     value="{data.summary.pendingInvoices} facture{data.summary.pendingInvoices !== 1 ? 's' : ''}" icon="alertCircle" color="var(--warning)" sub="{fmt.format(data.summary.pendingAmount)} DA" />
	</div>

	<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px">

		<!-- Bar chart: monthly trend -->
		<div class="card" style="padding:20px">
			<h2 style="font-size:14.5px;font-weight:600;margin-bottom:20px">Tendance mensuelle</h2>
			{#if data.summary.monthlyTrend.length === 0}
				<div style="height:130px;display:flex;align-items:center;justify-content:center;color:var(--text-muted);font-size:13px">
					Pas de données
				</div>
			{:else}
				<div style="display:flex;align-items:flex-end;gap:6px;height:130px;padding-bottom:28px;position:relative">
					{#each data.summary.monthlyTrend as item}
						<div style="flex:1;display:flex;flex-direction:column;align-items:center;gap:0;height:100%;justify-content:flex-end;position:relative" title="{fmt.format(item.amount)} DA">
							<div style="
								width:100%;border-radius:4px 4px 0 0;
								background:var(--primary);opacity:{item.amount === maxTrend ? 1 : 0.6};
								height:{Math.max((item.amount / maxTrend) * 100, 2)}%;
								transition:opacity 0.2s;
							"></div>
							<div style="position:absolute;bottom:-22px;font-size:10px;color:var(--text-muted);text-align:center;width:100%;overflow:hidden;text-overflow:ellipsis">
								{item.month.slice(0, 3)}
							</div>
						</div>
					{/each}
				</div>
				<div style="display:flex;justify-content:space-between;margin-top:8px">
					<span style="font-size:11px;color:var(--text-muted)">0</span>
					<span style="font-size:11px;color:var(--text-muted)">{fmt.format(maxTrend)} DA</span>
				</div>
			{/if}
		</div>

		<!-- Breakdown by type -->
		<div class="card" style="padding:20px">
			<h2 style="font-size:14.5px;font-weight:600;margin-bottom:16px">Répartition par type</h2>
			{#if data.summary.breakdownByType.length === 0}
				<div style="padding:40px 0;text-align:center;color:var(--text-muted);font-size:13px">Pas de données</div>
			{:else}
				<div style="display:flex;flex-direction:column;gap:14px">
					{#each data.summary.breakdownByType as item}
						<div>
							<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:5px">
								<span style="font-size:13.5px;font-weight:500">{item.label}</span>
								<span style="font-size:13px;font-weight:600;color:var(--primary)">{fmt.format(item.amount)} DA</span>
							</div>
							<div style="height:6px;background:var(--bg);border-radius:3px;overflow:hidden">
								<div style="height:100%;background:var(--primary);border-radius:3px;width:{item.percentage}%"></div>
							</div>
							<div style="font-size:11.5px;color:var(--text-muted);margin-top:3px">{item.percentage.toFixed(1)} %</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>

	</div>

	<!-- Charges list -->
	<div class="card" style="padding:0;overflow:hidden">
		<div style="padding:16px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between">
			<h2 style="font-size:14.5px;font-weight:600">Charges du mois</h2>
			<span style="font-size:12.5px;color:var(--text-muted)">{data.charges.length} charge{data.charges.length !== 1 ? 's' : ''}</span>
		</div>

		{#if data.charges.length === 0}
			<div style="padding:40px 20px;text-align:center;color:var(--text-muted)">
				<Icon name="fileText" size={32} color="var(--border-strong)" />
				<p style="margin-top:10px;font-size:13.5px">Aucune charge enregistrée ce mois-ci.</p>
				<button
					type="button"
					onclick={() => showChargeForm = true}
					style="margin-top:12px;display:inline-flex;align-items:center;gap:6px;padding:8px 16px;background:var(--primary);color:white;border:none;border-radius:7px;font-family:inherit;font-size:13px;font-weight:500;cursor:pointer"
				>
					<Icon name="plus" size={13} color="white" />
					Ajouter une charge
				</button>
			</div>
		{:else}
			<table class="mk-table">
				<thead>
					<tr>
						<th>Date</th>
						<th>Catégorie</th>
						<th>Description</th>
						<th style="text-align:right">Montant</th>
					</tr>
				</thead>
				<tbody>
					{#each data.charges as charge}
						<tr data-charge-id={charge.id}>
							<td style="font-size:13px;color:var(--text-muted);white-space:nowrap">{formatChargeDate(charge.date)}</td>
							<td>
								<div style="display:flex;align-items:center;gap:7px">
									<div style="width:26px;height:26px;border-radius:6px;background:var(--danger-light);display:flex;align-items:center;justify-content:center;flex-shrink:0">
										<Icon name={CATEGORY_ICON[charge.category] ?? 'dollar'} size={13} color="var(--danger)" />
									</div>
									<span style="font-size:13px;font-weight:500">{CATEGORY_LABEL[charge.category] ?? charge.category}</span>
								</div>
							</td>
							<td style="font-size:13.5px;color:var(--text)">{charge.description}</td>
							<td style="text-align:right;font-size:13.5px;font-weight:600;color:var(--danger);white-space:nowrap">−{fmt.format(charge.amount)} DA</td>
						</tr>
					{/each}
					<tr>
						<td colspan="3" style="font-size:13px;font-weight:700;color:var(--text);padding-top:14px">Total charges</td>
						<td style="text-align:right;font-size:14px;font-weight:700;color:var(--danger);padding-top:14px;white-space:nowrap">
							−{fmt.format(data.charges.reduce((s, c) => s + c.amount, 0))} DA
						</td>
					</tr>
				</tbody>
			</table>
		{/if}
	</div>

</div>
