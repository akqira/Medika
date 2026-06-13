#!/usr/bin/env node
// ─────────────────────────────────────────────────────────────────────────────
// Génère `apps/frontend/static/medicaments.json` : la liste (légère) des noms
// commerciaux de médicaments, pour l'autocomplétion de l'ordonnance.
//
//   Usage :  node apps/frontend/scripts/build-medicaments.mjs [source-json]
//
// Source par défaut : export JSON de la Nomenclature Nationale des médicaments
// (Algérie, au 31/12/2019) hébergé sur GitHub.
//
// Pour une version plus RÉCENTE : récupère le fichier Excel officiel du
// Ministère de l'Industrie Pharmaceutique
//   https://www.miph.gov.dz/fr/nomenclature-nationale-des-produits-pharmaceutiques/
// convertis-le en JSON (même structure de lignes), puis :
//   node apps/frontend/scripts/build-medicaments.mjs ./ma-nomenclature.json
//
// Le fichier généré (~100-150 Ko) est chargé en différé par la page consultation
// et mis en cache par le navigateur — adapté aux connexions à faible débit.
// Pense à le committer après génération (Vercel en a besoin au build).
// ─────────────────────────────────────────────────────────────────────────────

import { writeFile, readFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const DEFAULT_SRC =
	'https://raw.githubusercontent.com/mahmoudBens/Nomenclature-des-medicaments-en-algerie/master/medicament.json';

const src = process.argv[2] || DEFAULT_SRC;
const here = dirname(fileURLToPath(import.meta.url));
const outDir = join(here, '..', 'static');
const outFile = join(outDir, 'medicaments.json');

// Clés candidates pour le nom commercial selon la source (auto-détection).
const NAME_KEYS = ['NOM_DE_MARQUE', 'NOM_COMMERCIAL', 'NOM_COMMERCIALE', 'NOM', 'NOM_FR', 'name', 'nom'];

async function loadJson(s) {
	if (/^https?:\/\//.test(s)) {
		console.log(`Téléchargement : ${s}`);
		const res = await fetch(s);
		if (!res.ok) throw new Error(`HTTP ${res.status} en récupérant ${s}`);
		return res.json();
	}
	console.log(`Lecture du fichier : ${s}`);
	return JSON.parse(await readFile(s, 'utf8'));
}

function extractRows(data) {
	if (Array.isArray(data)) {
		// Export type phpMyAdmin : [{type:'database'}, {type:'table', data:[...]}]
		const table = data.find((x) => x && x.type === 'table' && Array.isArray(x.data));
		if (table) return table.data;
		// Sinon : tableau de lignes directement
		if (data.length && typeof data[0] === 'object' && data[0] && !data[0].type) return data;
	}
	throw new Error('Format JSON non reconnu (ni export phpMyAdmin, ni tableau de lignes).');
}

function pickNameKey(rows) {
	const sample = rows.find((r) => r && typeof r === 'object') || {};
	const key = NAME_KEYS.find((k) => k in sample);
	if (!key) throw new Error(`Aucune colonne de nom trouvée. Colonnes : ${Object.keys(sample).join(', ')}`);
	return key;
}

const data = await loadJson(src);
const rows = extractRows(data);
const key = pickNameKey(rows);
console.log(`Champ "nom commercial" détecté : ${key} — ${rows.length} lignes brutes`);

const seen = new Set();
const names = [];
for (const r of rows) {
	const raw = (r?.[key] ?? '').toString().trim().replace(/\s+/g, ' ');
	if (!raw) continue;
	const norm = raw.toUpperCase();
	if (seen.has(norm)) continue;
	seen.add(norm);
	names.push(raw);
}
names.sort((a, b) => a.localeCompare(b, 'fr'));

await mkdir(outDir, { recursive: true });
await writeFile(outFile, JSON.stringify(names), 'utf8');
console.log(`✓ ${names.length} noms commerciaux uniques → ${outFile}`);
