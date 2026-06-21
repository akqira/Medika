<script lang="ts">
	import { page } from '$app/state';
	import Icon from '$lib/components/Icon.svelte';

	const isNotFound = $derived(page.status === 404);
	// Always a human, doctor-facing sentence — never the raw backend error body.
	const title = $derived(isNotFound ? 'Patient introuvable' : 'Une erreur est survenue');
	const detail = $derived(
		isNotFound
			? "Ce dossier n'existe pas ou n'est pas accessible."
			: "Le dossier n'a pas pu être chargé. Veuillez réessayer."
	);
</script>

<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:60vh;gap:16px;text-align:center;padding:24px">
	<div style="width:64px;height:64px;border-radius:16px;background:var(--surface);border:1px solid var(--border);display:flex;align-items:center;justify-content:center">
		<Icon name={isNotFound ? 'user' : 'alertCircle'} size={28} color="var(--border-strong)" />
	</div>
	<div>
		<p style="font-size:16px;font-weight:600">{title}</p>
		<p style="font-size:13px;color:var(--text-light);margin-top:4px">{detail}</p>
	</div>
	<a href="/patients" style="padding:9px 18px;background:var(--primary);color:white;border-radius:7px;text-decoration:none;font-size:13.5px;font-weight:600">
		Retour aux patients
	</a>
</div>
