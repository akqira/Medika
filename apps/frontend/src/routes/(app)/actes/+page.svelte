<script lang="ts">
	import { enhance } from '$app/forms';
	import { frValidation } from '$lib/actions/frValidation';
	import type { PageData, ActionData } from './$types';
	import Icon from '$lib/components/Icon.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();
	let submitting = $state(false);
	const fmt = new Intl.NumberFormat('fr-DZ', { maximumFractionDigits: 0 });
</script>

<svelte:head><title>Catalogue d'actes · MediKa</title></svelte:head>

<div style="padding:24px;max-width:760px;margin:0 auto">
	<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:6px">
		<a href="/finance" style="display:inline-flex;align-items:center;gap:6px;font-size:13px;color:var(--text-muted);text-decoration:none">
			← Finances
		</a>
	</div>
	<h1 style="font-size:20px;font-weight:700;letter-spacing:-0.3px">Catalogue d'actes</h1>
	<p style="font-size:13.5px;color:var(--text-muted);margin:3px 0 20px">
		Les actes et tarifs prédéfinis du cabinet. Sélectionnables lors d'une consultation pour pré-remplir les honoraires.
	</p>

	{#if form?.success}
		<div style="display:flex;align-items:center;gap:8px;padding:12px 16px;background:var(--success-light);border:1px solid #A7F3D0;border-radius:8px;margin-bottom:18px">
			<Icon name="checkCircle" size={15} color="var(--success)" />
			<span style="font-size:13.5px;color:var(--success);font-weight:500">{form.success}</span>
		</div>
	{/if}
	{#if form?.error}
		<div style="display:flex;align-items:center;gap:8px;padding:12px 16px;background:var(--danger-light);border:1px solid #FECACA;border-radius:8px;margin-bottom:18px">
			<Icon name="alertCircle" size={15} color="var(--danger)" />
			<span style="font-size:13.5px;color:var(--danger);font-weight:500">{form.error}</span>
		</div>
	{/if}

	<!-- Add act -->
	<div class="card" style="padding:18px 20px;margin-bottom:20px">
		<form
			method="POST"
			action="?/add"
			use:frValidation
			use:enhance={() => { submitting = true; return async ({ update }) => { submitting = false; await update(); }; }}
			style="display:flex;gap:12px;align-items:flex-end;flex-wrap:wrap"
		>
			<div style="flex:1;min-width:200px">
				<label for="act-name" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Nom de l'acte</label>
				<input id="act-name" name="name" required class="mk-input" placeholder="ex: Consultation, Certificat médical…" />
			</div>
			<div style="width:140px">
				<label for="act-tariff" style="display:block;font-size:13px;font-weight:500;color:var(--text-muted);margin-bottom:5px">Tarif (DA)</label>
				<input id="act-tariff" name="tariff" type="number" min="0" value="0" class="mk-input" />
			</div>
			<button type="submit" disabled={submitting}
				style="display:flex;align-items:center;gap:6px;padding:10px 16px;background:var(--primary);color:white;border:none;border-radius:8px;font-family:inherit;font-size:13.5px;font-weight:600;cursor:pointer;opacity:{submitting ? 0.6 : 1}">
				<Icon name="plus" size={14} color="white" /> Ajouter
			</button>
		</form>
	</div>

	<!-- Catalogue -->
	<div class="card" style="padding:0;overflow:hidden">
		{#if data.acts.length === 0}
			<div style="display:flex;flex-direction:column;align-items:center;padding:40px 20px;text-align:center;color:var(--text-muted)">
				<Icon name="fileText" size={30} color="var(--border-strong)" />
				<p style="margin-top:10px;font-size:14px;font-weight:500">Aucun acte</p>
				<p style="font-size:12.5px;margin-top:2px">Ajoutez votre premier acte ci-dessus.</p>
			</div>
		{:else}
			<table class="mk-table">
				<thead>
					<tr><th>Acte</th><th style="text-align:right">Tarif</th><th></th></tr>
				</thead>
				<tbody>
					{#each data.acts as act}
						<tr data-act-id={act.id}>
							<td style="font-size:13.5px;font-weight:500">{act.name}</td>
							<td style="text-align:right;font-size:13.5px;font-weight:600;white-space:nowrap">{fmt.format(act.tariff)} DA</td>
							<td style="text-align:right;white-space:nowrap">
								<form method="POST" action="?/delete" use:enhance style="display:inline">
									<input type="hidden" name="id" value={act.id} />
									<button type="submit" aria-label="Supprimer l'acte"
										style="display:inline-flex;align-items:center;justify-content:center;background:none;border:none;cursor:pointer;color:var(--danger)">
										<Icon name="trash" size={15} color="var(--danger)" />
									</button>
								</form>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		{/if}
	</div>
</div>
