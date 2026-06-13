<script lang="ts">
	// Combobox d'autocomplétion sur-mesure, aligné sur le design system Medika.
	// - saisie libre toujours possible (le texte tapé est conservé)
	// - navigation clavier ↑ ↓ Entrée Échap
	// - surlignage de la portion correspondante
	// - panneau flottant (position fixed) qui ne se fait pas couper par les
	//   conteneurs à overflow, avec bascule vers le haut si peu de place
	interface Props {
		value: string;
		options: string[];
		placeholder?: string;
		onInput: (v: string) => void;
		style?: string;
		maxResults?: number;
	}
	let { value, options, placeholder = '', onInput, style = '', maxResults = 50 }: Props = $props();

	let open = $state(false);
	let highlighted = $state(-1);
	let inputEl = $state<HTMLInputElement | null>(null);
	let pos = $state({ left: 0, width: 0, top: 0, bottom: 0, up: false, maxH: 260 });

	const filtered = $derived.by(() => {
		const q = (value ?? '').trim().toLowerCase();
		const out: string[] = [];
		for (const o of options) {
			if (!q || o.toLowerCase().includes(q)) {
				out.push(o);
				if (out.length >= maxResults) break;
			}
		}
		return out;
	});

	function measure() {
		if (!inputEl) return;
		const r = inputEl.getBoundingClientRect();
		const below = window.innerHeight - r.bottom;
		const up = below < 240 && r.top > below;
		pos = {
			left: r.left,
			width: r.width,
			top: up ? 0 : r.bottom + 5,
			bottom: up ? window.innerHeight - r.top + 5 : 0,
			up,
			maxH: Math.max(140, Math.min(280, (up ? r.top : below) - 12))
		};
	}

	function openList() { measure(); open = true; highlighted = -1; }
	function closeList() { open = false; highlighted = -1; }

	function choose(v: string) {
		onInput(v);
		closeList();
		inputEl?.focus();
	}

	function onKeydown(e: KeyboardEvent) {
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			if (!open) { openList(); return; }
			highlighted = Math.min(highlighted + 1, filtered.length - 1);
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			highlighted = Math.max(highlighted - 1, 0);
		} else if (e.key === 'Enter') {
			if (open && highlighted >= 0 && highlighted < filtered.length) {
				e.preventDefault();
				choose(filtered[highlighted]);
			}
		} else if (e.key === 'Escape') {
			if (open) { e.preventDefault(); closeList(); }
		}
	}

	// Keep the panel glued to the input while scrolling/resizing
	$effect(() => {
		if (!open) return;
		const reflow = () => measure();
		window.addEventListener('scroll', reflow, true);
		window.addEventListener('resize', reflow);
		return () => {
			window.removeEventListener('scroll', reflow, true);
			window.removeEventListener('resize', reflow);
		};
	});

	// Keep highlighted row visible
	let panelEl = $state<HTMLDivElement | null>(null);
	$effect(() => {
		if (!open || highlighted < 0 || !panelEl) return;
		panelEl.querySelector<HTMLElement>(`[data-i="${highlighted}"]`)?.scrollIntoView({ block: 'nearest' });
	});

	function parts(label: string) {
		const q = (value ?? '').trim();
		if (!q) return [{ t: label, m: false }];
		const i = label.toLowerCase().indexOf(q.toLowerCase());
		if (i < 0) return [{ t: label, m: false }];
		return [
			{ t: label.slice(0, i), m: false },
			{ t: label.slice(i, i + q.length), m: true },
			{ t: label.slice(i + q.length), m: false }
		];
	}
</script>

<input
	bind:this={inputEl}
	{value}
	{placeholder}
	{style}
	class="mk-input"
	autocomplete="off"
	role="combobox"
	aria-expanded={open}
	aria-autocomplete="list"
	oninput={(e) => { onInput((e.target as HTMLInputElement).value); if (!open) openList(); highlighted = -1; }}
	onfocus={openList}
	onkeydown={onKeydown}
	onblur={() => setTimeout(closeList, 130)}
/>

{#if open && (filtered.length > 0 || (value ?? '').trim())}
	<div
		bind:this={panelEl}
		class="cbx-panel"
		role="listbox"
		style="left:{pos.left}px;width:{pos.width}px;max-height:{pos.maxH}px;{pos.up ? `bottom:${pos.bottom}px` : `top:${pos.top}px`}"
	>
		{#if filtered.length > 0}
			{#each filtered as opt, i}
				<button
					type="button"
					role="option"
					data-i={i}
					aria-selected={i === highlighted}
					class="cbx-item {i === highlighted ? 'active' : ''}"
					onmousedown={(e) => { e.preventDefault(); choose(opt); }}
					onmouseenter={() => highlighted = i}
				>
					{#each parts(opt) as p}{#if p.m}<strong>{p.t}</strong>{:else}{p.t}{/if}{/each}
				</button>
			{/each}
		{:else}
			<div class="cbx-empty">Aucune correspondance — « {(value ?? '').trim()} » sera utilisé tel quel</div>
		{/if}
	</div>
{/if}

<style>
	.cbx-panel {
		position: fixed;
		z-index: 400;
		overflow-y: auto;
		background: var(--surface);
		border: 1px solid var(--border);
		border-radius: 10px;
		box-shadow: 0 12px 32px rgba(15, 23, 42, 0.14);
		padding: 5px;
		animation: cbx-in 0.12s ease-out;
	}
	@keyframes cbx-in {
		from { opacity: 0; transform: translateY(-3px); }
		to   { opacity: 1; transform: translateY(0); }
	}
	.cbx-item {
		display: block;
		width: 100%;
		text-align: left;
		padding: 8px 11px;
		border: none;
		background: none;
		border-radius: 7px;
		font-family: inherit;
		font-size: 13.5px;
		color: var(--text);
		cursor: pointer;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		transition: background 0.1s;
	}
	.cbx-item.active { background: var(--primary-50); color: var(--primary); }
	.cbx-item strong { color: var(--primary); font-weight: 700; }
	.cbx-empty {
		padding: 10px 12px;
		font-size: 12.5px;
		color: var(--text-muted);
		line-height: 1.4;
	}
	/* Fine scrollbar pour rester discret */
	.cbx-panel::-webkit-scrollbar { width: 8px; }
	.cbx-panel::-webkit-scrollbar-thumb { background: var(--border-strong); border-radius: 8px; }
</style>
