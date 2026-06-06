<script lang="ts">
	import type { PageData } from './$types';
	import StatCard from '$lib/components/StatCard.svelte';
	import Icon from '$lib/components/Icon.svelte';

	let { data }: { data: PageData } = $props();

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
		</div>
	</div>

	<!-- Stat cards -->
	<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:14px;margin-bottom:24px">
		<StatCard label="Revenus"     value="{fmt.format(data.summary.totalIncome)} DA"   icon="dollar"      color="var(--success)" sub="encaissés" />
		<StatCard label="Charges"     value="{fmt.format(data.summary.totalCharges)} DA"  icon="fileText"    color="var(--danger)"  sub="ce mois"  />
		<StatCard label="Bénéfice"    value="{fmt.format(data.summary.netIncome)} DA"     icon="barchart"    color={data.summary.netIncome >= 0 ? 'var(--primary)' : 'var(--danger)'} sub="net" />
		<StatCard label="Impayés"     value="{data.summary.pendingInvoices} facture{data.summary.pendingInvoices !== 1 ? 's' : ''}" icon="alertCircle" color="var(--warning)" sub="{fmt.format(data.summary.pendingAmount)} DA" />
	</div>

	<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">

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
				<!-- Y-axis hint -->
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
</div>
