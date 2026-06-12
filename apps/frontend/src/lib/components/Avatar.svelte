<script lang="ts">
	let { nom = '', prenom = '', sexe = 'M', size = 36 }: {
		nom?: string;
		prenom?: string;
		sexe?: 'M' | 'F';
		size?: number;
	} = $props();

	const initials = $derived(
		((prenom[0] ?? '').toUpperCase() + (nom[0] ?? '').toUpperCase()) || '?'
	);
	const s = $derived(
		sexe === 'F'
			? { bg: '#FDF2F8', text: '#9D174D', border: '#FCE7F3' }
			: { bg: 'var(--primary-50)', text: 'var(--primary)', border: 'var(--primary-light)' }
	);
	const fs = $derived(Math.round(size * 0.35));
</script>

<div style="
	width:{size}px;height:{size}px;border-radius:50%;flex-shrink:0;
	background:{s.bg};color:{s.text};border:2px solid {s.border};
	display:flex;align-items:center;justify-content:center;
	font-size:{fs}px;font-weight:700;user-select:none;
">{initials}</div>
