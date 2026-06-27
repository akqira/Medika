<script lang="ts">
	// Global toast viewport — ported from eGestion's Toast.svelte, restyled to Medika's
	// idiom (CSS variables from app.css + the shared Icon component, no Tailwind classes).
	// Mounted once in (app)/+layout.svelte; reads the module-level `toasts` $state store.
	// Issue #129.
	import { fly } from 'svelte/transition';
	import { toasts, dismissToast, type ToastType } from '$lib/stores/toast.svelte';
	import Icon from '$lib/components/Icon.svelte';

	const VARIANT: Record<ToastType, { icon: string; color: string; bg: string; border: string }> = {
		success: { icon: 'checkCircle', color: 'var(--success)', bg: 'var(--success-light)', border: '#A7F3D0' },
		error:   { icon: 'alertCircle', color: 'var(--danger)',  bg: 'var(--danger-light)',  border: '#FECACA' },
		info:    { icon: 'bell',        color: 'var(--info)',     bg: 'var(--info-light)',    border: '#BFDBFE' },
	};
</script>

<!-- z-index 1000: above the nav (200) and its dropdowns (300) so toasts are never covered. -->
<div
	style="position:fixed;top:18px;right:18px;z-index:1000;display:flex;flex-direction:column;gap:10px;pointer-events:none"
	aria-live="polite"
	aria-atomic="false"
>
	{#each toasts as t (t.id)}
		{@const v = VARIANT[t.type]}
		<div
			role="status"
			in:fly={{ y: -16, duration: 220 }}
			out:fly={{ y: -16, duration: 180 }}
			style="
				pointer-events:auto;display:flex;align-items:center;gap:11px;
				min-width:280px;max-width:380px;padding:12px 14px;
				background:var(--surface);border:1px solid {v.border};border-left:4px solid {v.color};
				border-radius:10px;box-shadow:0 10px 30px rgba(15,23,42,0.16);
			"
		>
			<span style="
				display:flex;align-items:center;justify-content:center;flex-shrink:0;
				width:26px;height:26px;border-radius:50%;background:{v.bg};
			">
				<Icon name={v.icon} size={15} color={v.color} />
			</span>
			<span style="flex:1;font-size:13.5px;font-weight:500;color:var(--text);line-height:1.35">{t.message}</span>
			<button
				type="button"
				onclick={() => dismissToast(t.id)}
				aria-label="Fermer la notification"
				style="flex-shrink:0;display:flex;align-items:center;background:none;border:none;cursor:pointer;padding:2px;opacity:0.55"
			>
				<Icon name="x" size={15} color="var(--text-muted)" />
			</button>
		</div>
	{/each}
</div>
