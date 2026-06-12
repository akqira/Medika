<script lang="ts">
	import Icon from './Icon.svelte';

	let { label, value, sub, icon, color = 'var(--primary)', trend }: {
		label: string;
		value: string | number;
		sub?: string;
		icon: string;
		color?: string;
		trend?: number;
	} = $props();

	const valueStr = $derived(String(value));
	const fs = $derived(valueStr.length > 9 ? 18 : 24);
</script>

<div class="card" style="padding:18px 20px;display:flex;gap:14px;align-items:flex-start;">
	<div style="width:42px;height:42px;border-radius:10px;flex-shrink:0;background:{color}1A;display:flex;align-items:center;justify-content:center;color:{color}">
		<Icon name={icon} size={20} />
	</div>
	<div style="flex:1;min-width:0;">
		<div style="font-size:12.5px;color:var(--text-muted);margin-bottom:3px;">{label}</div>
		<div style="font-size:{fs}px;font-weight:700;line-height:1.15;letter-spacing:-0.5px;">{value}</div>
		{#if sub}<div style="font-size:12px;color:var(--text-muted);margin-top:3px;">{sub}</div>{/if}
	</div>
	{#if trend !== undefined}
		<div style="font-size:12px;font-weight:600;color:{trend >= 0 ? 'var(--success)' : 'var(--danger)'};display:flex;align-items:center;gap:2px;margin-top:2px;">
			<Icon name="arrowUp" size={11} style={trend < 0 ? 'transform:rotate(180deg)' : ''} />
			{Math.abs(trend)}%
		</div>
	{/if}
</div>
