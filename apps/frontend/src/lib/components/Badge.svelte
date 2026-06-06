<script lang="ts">
	import type { Snippet } from 'svelte';

	const VARIANTS: Record<string, { bg: string; color: string }> = {
		default: { bg: '#F3F4F6',              color: '#374151' },
		success: { bg: 'var(--success-light)', color: 'var(--success)' },
		warning: { bg: 'var(--warning-light)', color: 'var(--warning)' },
		danger:  { bg: 'var(--danger-light)',  color: 'var(--danger)' },
		primary: { bg: 'var(--primary-light)', color: 'var(--primary)' },
		info:    { bg: '#DBEAFE',              color: '#1D4ED8' },
		purple:  { bg: '#EDE9FE',              color: '#7C3AED' },
	};

	const STATUS_VARIANTS: Record<string, string> = {
		Confirmed: 'success', Completed: 'default', InProgress: 'primary',
		Pending: 'warning', Cancelled: 'danger', NoShow: 'danger',
	};

	let { children, variant = 'default', status }: {
		children?: Snippet;
		variant?: string;
		status?: string;
	} = $props();

	const resolvedVariant = $derived(status ? (STATUS_VARIANTS[status] ?? 'default') : variant);
	const s = $derived(VARIANTS[resolvedVariant] ?? VARIANTS.default);
</script>

<span style="
	display:inline-flex;align-items:center;padding:2px 9px;
	border-radius:20px;font-size:12px;font-weight:500;white-space:nowrap;
	background:{s.bg};color:{s.color};
">
	{@render children?.()}
</span>
