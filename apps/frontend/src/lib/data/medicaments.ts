// ─────────────────────────────────────────────────────────────────────────────
// Liste de DÉMARRAGE de médicaments (noms commerciaux courants en Algérie).
//
// ⚠️ Ceci n'est qu'un jeu de départ pour l'autocomplétion de l'ordonnance.
// À remplacer par la **Nomenclature Nationale des Produits Pharmaceutiques**
// complète (≈ 4800 entrées) publiée par l'ANPP / Ministère de l'Industrie
// Pharmaceutique, importée depuis le fichier Excel officiel.
//   Source : https://www.miph.gov.dz/fr/nomenclature-nationale-des-produits-pharmaceutiques/
//   Point de départ JSON : https://github.com/mahmoudBens/Nomenclature-des-medicaments-en-algerie
//
// Le médecin peut toujours saisir un nom libre : l'autocomplétion ne contraint pas.
// Affichage par NOM COMMERCIAL (choix produit). La force/dosage se saisit dans le
// champ « Posologie » sur la même ligne.
// ─────────────────────────────────────────────────────────────────────────────

export const MEDICAMENTS: string[] = [
	// Antalgiques / antipyrétiques
	'Doliprane', 'Efferalgan', 'Dafalgan', 'Perfalgan', 'Paracétamol',
	'Aspégic', 'Aspirine', 'Ibuprofène', 'Brufen', 'Nurofen',
	// AINS
	'Voltarène', 'Diclofénac', 'Profenid', 'Kétoprofène', 'Feldène', 'Mobic', 'Célébrex',
	// Antibiotiques
	'Augmentin', 'Amoxicilline', 'Clamoxyl', 'Amoxil', 'Zinnat', 'Céfuroxime',
	'Rocéphine', 'Ciprofloxacine', 'Ciflox', 'Oflocet', 'Ofloxacine',
	'Bactrim', 'Flagyl', 'Métronidazole', 'Zithromax', 'Azithromycine',
	'Klacid', 'Clarithromycine', 'Doxycycline', 'Péni G', 'Erythromycine',
	// ORL / respiratoire
	'Ventoline', 'Salbutamol', 'Symbicort', 'Seretide', 'Sérétide',
	'Solupred', 'Célestène', 'Prednisolone', 'Aerius', 'Zyrtec', 'Clarityne',
	'Toplexil', 'Bronchokod', 'Rhinathiol', 'Maxilase',
	// Gastro
	'Inexium', 'Esoméprazole', 'Mopral', 'Oméprazole', 'Lanzor',
	'Gaviscon', 'Maalox', 'Smecta', 'Spasfon', 'Débridat', 'Motilium',
	'Imodium', 'Forlax', 'Duphalac',
	// Cardio / métabolisme
	'Amlor', 'Amlodipine', 'Tahor', 'Atorvastatine', 'Crestor',
	'Kardégic', 'Plavix', 'Lasilix', 'Furosémide', 'Aldactone',
	'Coversyl', 'Triatec', 'Ramipril', 'Loxen', 'Bisoprolol',
	'Glucophage', 'Metformine', 'Diamicron', 'Amarel', 'Lantus', 'Janumet',
	'Levothyrox', 'Lévothyroxine',
	// Neuro / psy / antalgie
	'Atarax', 'Lexomil', 'Stilnox', 'Lyrica', 'Tegretol', 'Dépakine',
	'Laroxyl', 'Seroplex', 'Xanax',
	// Divers
	'Cortancyl', 'Daflon', 'Spedifen', 'Calcium D3', 'Tardyféron',
	'Speciafoldine', 'Vitamine D', 'Magné B6',
];
