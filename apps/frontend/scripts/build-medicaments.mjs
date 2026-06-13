#!/usr/bin/env node
// ─────────────────────────────────────────────────────────────────────────────
// Génère `apps/frontend/static/medicaments.json` : la liste (légère) des
// médicaments pour l'autocomplétion de l'ordonnance.
//
// Chaque entrée combine NOM COMMERCIAL + DOSAGE + FORME, ex. :
//   « DOLIPRANE 500MG COMPRIME », « DOLIPRANE 1G COMPRIME », « AUGMENTIN 1G SACHET »
// Ainsi le médecin choisit le bon dosage directement dans la liste, sans
// ressaisir la posologie dans un second champ.
//
//   Usage :  node apps/frontend/scripts/build-medicaments.mjs [source-json]
//
// Source par défaut : export JSON de la Nomenclature Nationale des médicaments
// (Algérie, au 31/12/2019) hébergé sur GitHub. Pour une version plus récente,
// récupère le fichier Excel officiel du Ministère de l'Industrie Pharmaceutique
//   https://www.miph.gov.dz/fr/nomenclature-nationale-des-produits-pharmaceutiques/
// convertis-le en JSON (mêmes lignes), puis passe son chemin en argument.
//
// Le fichier généré (~150-250 Ko) est chargé en différé par la page consultation
// et mis en cache par le navigateur. Pense à le committer (Vercel en a besoin).
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

// Clés candidates (auto-détection selon la source).
const BRAND_KEYS = ['NOM_DE_MARQUE', 'NOM_COMMERCIAL', 'NOM_COMMERCIALE', 'NOM', 'NOM_FR', 'nom', 'name'];
const DOSAGE_KEYS = ['DOSAGE', 'dosage', 'DOSE'];
const FORM_KEYS = ['FORME', 'forme', 'FORM'];

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
		const table = data.find((x) => x && x.type === 'table' && Array.isArray(x.data));
		if (table) return table.data;
		if (data.length && typeof data[0] === 'object' && data[0] && !data[0].type) return data;
	}
	throw new Error('Format JSON non reconnu (ni export phpMyAdmin, ni tableau de lignes).');
}

function pick(sample, keys) {
	return keys.find((k) => k in sample);
}

function clean(v) {
	return (v ?? '').toString().trim().replace(/\s+/g, ' ');
}

const data = await loadJson(src);
const rows = extractRows(data);
const sample = rows.find((r) => r && typeof r === 'object') || {};
const brandKey = pick(sample, BRAND_KEYS);
if (!brandKey) throw new Error(`Aucune colonne de nom trouvée. Colonnes : ${Object.keys(sample).join(', ')}`);
const dosageKey = pick(sample, DOSAGE_KEYS);
const formKey = pick(sample, FORM_KEYS);
console.log(`Colonnes — nom: ${brandKey} | dosage: ${dosageKey ?? '(aucune)'} | forme: ${formKey ?? '(aucune)'} — ${rows.length} lignes`);

const seen = new Set();
const names = [];
for (const r of rows) {
	const brand = clean(r?.[brandKey]);
	if (!brand) continue;
	const label = [brand, dosageKey && clean(r[dosageKey]), formKey && clean(r[formKey])]
		.filter(Boolean)
		.join(' ');
	const norm = label.toUpperCase();
	if (seen.has(norm)) continue;
	seen.add(norm);
	names.push(label);
}
names.sort((a, b) => a.localeCompare(b, 'fr'));

await mkdir(outDir, { recursive: true });
await writeFile(outFile, JSON.stringify(names), 'utf8');
console.log(`✓ ${names.length} entrées (nom + dosage + forme) → ${outFile}`);
