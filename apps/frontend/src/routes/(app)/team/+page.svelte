<script lang="ts">
	import { enhance } from '$app/forms';
	import { invalidateAll } from '$app/navigation';
	import Icon from '$lib/components/Icon.svelte';
	import { DEFAULT_SECRETARY } from '$lib/permissions';
	import type { PageData, ActionData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let showAdd = $state(false);
	let editingUserId = $state<string | null>(null);

	const editingUser = $derived(data.users.find((u) => u.id === editingUserId) ?? null);

	function roleLabel(role: string) {
		return role === 'Doctor' ? 'Administrateur' : role === 'Secretary' ? 'Secrétaire' : role;
	}
	function initials(first: string, last: string) {
		return `${first[0] ?? ''}${last[0] ?? ''}`.toUpperCase();
	}
	function fmtDate(iso: string | null) {
		if (!iso) return 'Jamais connecté(e)';
		return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
	}

	// After a successful mutation, close panels and refresh the roster.
	function onResult() {
		return async ({ result, update }: { result: { type: string }; update: () => Promise<void> }) => {
			await update();
			if (result.type === 'success') {
				showAdd = false;
				editingUserId = null;
				await invalidateAll();
			}
		};
	}
</script>

<div class="mk-content-inner" style="max-width:980px;margin:0 auto;padding:24px 0">
	<!-- Header -->
	<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:6px">
		<div>
			<h1 style="font-size:22px;font-weight:700;color:var(--text);margin:0">Équipe</h1>
			<p style="color:var(--text-muted);font-size:13.5px;margin:4px 0 0">
				Gérez les membres de votre cabinet et leurs permissions.
			</p>
		</div>
		{#if data.canAdd}
			<button
				onclick={() => { showAdd = !showAdd; editingUserId = null; }}
				class="mk-btn-primary"
				style="display:inline-flex;align-items:center;gap:7px;padding:9px 15px;border-radius:8px;background:var(--primary);color:white;border:none;cursor:pointer;font-family:inherit;font-size:13.5px;font-weight:600"
			>
				<Icon name="plus" size={15} color="white" />
				Ajouter un membre
			</button>
		{/if}
	</div>

	{#if form?.success}
		<div role="status" style="margin:14px 0;padding:11px 14px;border-radius:8px;background:var(--success-light);color:var(--success);font-size:13.5px;border:1px solid var(--success)">
			{form.success}
		</div>
	{/if}
	{#if form?.error}
		<div role="alert" style="margin:14px 0;padding:11px 14px;border-radius:8px;background:var(--danger-light);color:var(--danger);font-size:13.5px;border:1px solid var(--danger)">
			{form.error}
		</div>
	{/if}

	<!-- Add member form -->
	{#if showAdd}
		<div class="card" style="margin:16px 0;padding:20px">
			<h2 style="font-size:15px;font-weight:700;color:var(--text);margin:0 0 14px">Nouveau secrétaire</h2>
			<form method="POST" action="?/createStaff" use:enhance={onResult}>
				<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:14px">
					<label style="display:flex;flex-direction:column;gap:5px;font-size:12.5px;color:var(--text-muted)">
						Prénom
						<input class="mk-input" name="firstName" required maxlength="100" />
					</label>
					<label style="display:flex;flex-direction:column;gap:5px;font-size:12.5px;color:var(--text-muted)">
						Nom
						<input class="mk-input" name="lastName" required maxlength="100" />
					</label>
					<label style="display:flex;flex-direction:column;gap:5px;font-size:12.5px;color:var(--text-muted)">
						Email
						<input class="mk-input" name="email" type="email" required />
					</label>
					<label style="display:flex;flex-direction:column;gap:5px;font-size:12.5px;color:var(--text-muted)">
						Mot de passe provisoire
						<input class="mk-input" name="password" type="password" required minlength="8" />
					</label>
				</div>

				<div style="font-size:12.5px;font-weight:600;color:var(--text);margin:8px 0 10px">Permissions</div>
				<div style="display:grid;grid-template-columns:1fr 1fr;gap:18px">
					{#each data.categories as cat (cat.key)}
						<fieldset style="border:1px solid var(--border);border-radius:8px;padding:12px 14px;margin:0">
							<legend style="display:flex;align-items:center;gap:6px;font-size:12.5px;font-weight:600;color:var(--text);padding:0 6px">
								<Icon name={cat.icon} size={14} color="var(--primary)" />
								{cat.label}
							</legend>
							{#each cat.permissions as p (p.key)}
								<label style="display:flex;align-items:flex-start;gap:8px;padding:5px 0;font-size:13px;color:var(--text);cursor:pointer">
									<input type="checkbox" name="permissions" value={p.key} checked={DEFAULT_SECRETARY.includes(p.key)} style="margin-top:2px" />
									<span>
										{p.label}
										<span style="display:block;font-size:11.5px;color:var(--text-muted)">{p.description}</span>
									</span>
								</label>
							{/each}
						</fieldset>
					{/each}
				</div>

				<div style="display:flex;gap:10px;justify-content:flex-end;margin-top:16px">
					<button type="button" onclick={() => (showAdd = false)} style="padding:9px 15px;border-radius:8px;background:var(--surface);border:1px solid var(--border);cursor:pointer;font-family:inherit;font-size:13.5px;color:var(--text)">Annuler</button>
					<button type="submit" style="padding:9px 18px;border-radius:8px;background:var(--primary);color:white;border:none;cursor:pointer;font-family:inherit;font-size:13.5px;font-weight:600">Créer le compte</button>
				</div>
			</form>
		</div>
	{/if}

	<!-- Roster -->
	<div class="card" style="margin-top:16px;padding:0;overflow:hidden">
		<table class="mk-table" style="width:100%;border-collapse:collapse">
			<thead>
				<tr style="background:var(--bg);text-align:left">
					<th style="padding:12px 16px;font-size:12px;color:var(--text-muted);font-weight:600">Membre</th>
					<th style="padding:12px 16px;font-size:12px;color:var(--text-muted);font-weight:600">Rôle</th>
					<th style="padding:12px 16px;font-size:12px;color:var(--text-muted);font-weight:600">Statut</th>
					<th style="padding:12px 16px;font-size:12px;color:var(--text-muted);font-weight:600">Dernière connexion</th>
					<th style="padding:12px 16px"></th>
				</tr>
			</thead>
			<tbody>
				{#each data.users as u (u.id)}
					<tr style="border-top:1px solid var(--border)">
						<td style="padding:12px 16px">
							<div style="display:flex;align-items:center;gap:10px">
								<div style="width:32px;height:32px;border-radius:50%;background:var(--primary-50);color:var(--primary-dark);display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;flex-shrink:0">
									{initials(u.firstName, u.lastName)}
								</div>
								<div>
									<div style="font-size:13.5px;font-weight:600;color:var(--text)">{u.firstName} {u.lastName}</div>
									<div style="font-size:12px;color:var(--text-muted)">{u.email}</div>
								</div>
							</div>
						</td>
						<td style="padding:12px 16px;font-size:13px;color:var(--text)">{roleLabel(u.role)}</td>
						<td style="padding:12px 16px">
							{#if u.isActive}
								<span style="font-size:12px;font-weight:600;color:var(--success);background:var(--success-light);padding:3px 9px;border-radius:20px">Actif</span>
							{:else}
								<span style="font-size:12px;font-weight:600;color:var(--text-muted);background:var(--bg);padding:3px 9px;border-radius:20px;border:1px solid var(--border)">Désactivé</span>
							{/if}
						</td>
						<td style="padding:12px 16px;font-size:12.5px;color:var(--text-muted)">{fmtDate(u.lastLoginAt)}</td>
						<td style="padding:12px 16px;text-align:right;white-space:nowrap">
							{#if u.role !== 'Doctor' && data.canManage}
								<button
									onclick={() => { editingUserId = editingUserId === u.id ? null : u.id; showAdd = false; }}
									style="padding:6px 11px;border-radius:7px;background:var(--surface);border:1px solid var(--border);cursor:pointer;font-family:inherit;font-size:12.5px;color:var(--text);margin-right:6px"
								>Permissions</button>
								<form method="POST" action="?/setActive" use:enhance={onResult} style="display:inline">
									<input type="hidden" name="userId" value={u.id} />
									<input type="hidden" name="isActive" value={(!u.isActive).toString()} />
									<button type="submit" style="padding:6px 11px;border-radius:7px;background:transparent;border:1px solid var(--border);cursor:pointer;font-family:inherit;font-size:12.5px;color:{u.isActive ? 'var(--danger)' : 'var(--success)'}">
										{u.isActive ? 'Désactiver' : 'Réactiver'}
									</button>
								</form>
							{:else if u.role === 'Doctor'}
								<span style="font-size:12px;color:var(--text-light)">Accès complet</span>
							{/if}
						</td>
					</tr>

					<!-- Inline permission editor -->
					{#if editingUserId === u.id && editingUser}
						<tr style="background:var(--bg)">
							<td colspan="5" style="padding:18px 16px">
								<form method="POST" action="?/savePermissions" use:enhance={onResult}>
									<input type="hidden" name="userId" value={u.id} />
									<div style="font-size:13px;font-weight:700;color:var(--text);margin-bottom:12px">
										Permissions de {u.firstName} {u.lastName}
									</div>
									<div style="display:grid;grid-template-columns:1fr 1fr;gap:18px">
										{#each data.categories as cat (cat.key)}
											<fieldset style="border:1px solid var(--border);border-radius:8px;padding:12px 14px;margin:0;background:var(--surface)">
												<legend style="display:flex;align-items:center;gap:6px;font-size:12.5px;font-weight:600;color:var(--text);padding:0 6px">
													<Icon name={cat.icon} size={14} color="var(--primary)" />
													{cat.label}
												</legend>
												{#each cat.permissions as p (p.key)}
													<label style="display:flex;align-items:flex-start;gap:8px;padding:5px 0;font-size:13px;color:var(--text);cursor:pointer">
														<input type="checkbox" name="permissions" value={p.key} checked={editingUser.permissions.includes(p.key)} style="margin-top:2px" />
														<span>
															{p.label}
															<span style="display:block;font-size:11.5px;color:var(--text-muted)">{p.description}</span>
														</span>
													</label>
												{/each}
											</fieldset>
										{/each}
									</div>
									<div style="display:flex;gap:10px;justify-content:flex-end;margin-top:14px">
										<button type="button" onclick={() => (editingUserId = null)} style="padding:8px 14px;border-radius:8px;background:var(--surface);border:1px solid var(--border);cursor:pointer;font-family:inherit;font-size:13px;color:var(--text)">Annuler</button>
										<button type="submit" style="padding:8px 16px;border-radius:8px;background:var(--primary);color:white;border:none;cursor:pointer;font-family:inherit;font-size:13px;font-weight:600">Enregistrer</button>
									</div>
								</form>
							</td>
						</tr>
					{/if}
				{/each}

				{#if data.users.length === 0}
					<tr><td colspan="5" style="padding:32px 16px;text-align:center;color:var(--text-muted);font-size:13.5px">Aucun membre pour l'instant.</td></tr>
				{/if}
			</tbody>
		</table>
	</div>
</div>
