// ─────────────────────────────────────────────────────────────────────────────
// Liste de DÉMARRAGE de médicaments (nom commercial + dosage), Algérie.
//
// Chaque entrée porte le dosage → le médecin choisit directement la bonne
// présentation dans l'autocomplétion, sans ressaisir la posologie.
//
// ⚠️ Jeu de départ uniquement. À remplacer par la Nomenclature Nationale
// complète (≈ 4800+ présentations) via :
//   node apps/frontend/scripts/build-medicaments.mjs
// qui génère static/medicaments.json (chargé en différé, prioritaire sur cette liste).
//
// La saisie libre reste toujours possible (un nom hors liste est conservé tel quel).
// ─────────────────────────────────────────────────────────────────────────────

export const MEDICAMENTS: string[] = [
	// Antalgiques / antipyrétiques
	'Doliprane 500mg comprimé', 'Doliprane 1g comprimé', 'Doliprane 1000mg comprimé',
	'Doliprane 2.4% suspension buvable', 'Doliprane 150mg suppositoire',
	'Efferalgan 500mg comprimé', 'Efferalgan 1g comprimé', 'Efferalgan 150mg suppositoire',
	'Dafalgan 500mg comprimé', 'Dafalgan 1g comprimé',
	'Paracétamol 500mg comprimé', 'Paracétamol 1g comprimé',
	'Aspégic 100mg sachet', 'Aspégic 500mg sachet', 'Aspégic 1000mg sachet',
	'Aspirine 500mg comprimé',
	// AINS
	'Ibuprofène 200mg comprimé', 'Ibuprofène 400mg comprimé',
	'Brufen 400mg comprimé', 'Nurofen 200mg comprimé', 'Nurofen 400mg comprimé',
	'Voltarène 50mg comprimé', 'Voltarène 75mg comprimé', 'Voltarène 100mg suppositoire',
	'Diclofénac 50mg comprimé', 'Profenid 100mg comprimé', 'Kétoprofène 100mg comprimé',
	'Feldène 20mg comprimé', 'Mobic 7.5mg comprimé', 'Mobic 15mg comprimé',
	// Antibiotiques
	'Augmentin 500mg comprimé', 'Augmentin 1g comprimé', 'Augmentin 1g sachet',
	'Augmentin 100mg/12.5mg suspension buvable',
	'Amoxicilline 250mg gélule', 'Amoxicilline 500mg gélule', 'Amoxicilline 1g comprimé',
	'Clamoxyl 500mg gélule', 'Clamoxyl 1g comprimé', 'Amoxil 500mg gélule',
	'Zinnat 250mg comprimé', 'Zinnat 500mg comprimé', 'Céfuroxime 500mg comprimé',
	'Rocéphine 1g injectable', 'Ciprofloxacine 500mg comprimé', 'Ciflox 500mg comprimé',
	'Oflocet 200mg comprimé', 'Ofloxacine 200mg comprimé',
	'Bactrim forte comprimé', 'Flagyl 500mg comprimé', 'Métronidazole 500mg comprimé',
	'Zithromax 250mg comprimé', 'Azithromycine 250mg comprimé', 'Azithromycine 500mg comprimé',
	'Klacid 500mg comprimé', 'Clarithromycine 500mg comprimé', 'Doxycycline 100mg comprimé',
	// ORL / respiratoire
	'Ventoline 100µg spray', 'Salbutamol 100µg spray',
	'Symbicort 160/4.5µg inhalateur', 'Seretide 250µg inhalateur',
	'Solupred 20mg comprimé', 'Célestène 0.5mg comprimé', 'Prednisolone 20mg comprimé',
	'Aerius 5mg comprimé', 'Zyrtec 10mg comprimé', 'Clarityne 10mg comprimé',
	'Toplexil sirop', 'Bronchokod 5% sirop', 'Rhinathiol sirop', 'Maxilase comprimé',
	// Gastro
	'Inexium 20mg comprimé', 'Inexium 40mg comprimé', 'Esoméprazole 20mg comprimé',
	'Mopral 20mg gélule', 'Oméprazole 20mg gélule',
	'Gaviscon suspension buvable', 'Maalox suspension buvable', 'Smecta 3g sachet',
	'Spasfon 80mg comprimé', 'Débridat 100mg comprimé', 'Motilium 10mg comprimé',
	'Imodium 2mg gélule', 'Forlax 10g sachet', 'Duphalac sirop',
	// Cardio / métabolisme
	'Amlor 5mg gélule', 'Amlor 10mg gélule', 'Amlodipine 5mg comprimé',
	'Tahor 10mg comprimé', 'Tahor 20mg comprimé', 'Atorvastatine 20mg comprimé',
	'Crestor 10mg comprimé', 'Crestor 20mg comprimé',
	'Kardégic 75mg sachet', 'Kardégic 160mg sachet', 'Plavix 75mg comprimé',
	'Lasilix 40mg comprimé', 'Furosémide 40mg comprimé', 'Aldactone 25mg comprimé',
	'Coversyl 5mg comprimé', 'Triatec 5mg comprimé', 'Ramipril 5mg comprimé',
	'Loxen 20mg comprimé', 'Bisoprolol 5mg comprimé',
	'Glucophage 500mg comprimé', 'Glucophage 850mg comprimé', 'Glucophage 1000mg comprimé',
	'Metformine 850mg comprimé', 'Diamicron 30mg comprimé', 'Diamicron 60mg comprimé',
	'Amarel 2mg comprimé', 'Amarel 4mg comprimé', 'Lantus 100UI/ml stylo',
	'Levothyrox 50µg comprimé', 'Levothyrox 75µg comprimé', 'Levothyrox 100µg comprimé',
	// Neuro / psy
	'Atarax 25mg comprimé', 'Lexomil 6mg comprimé', 'Stilnox 10mg comprimé',
	'Lyrica 75mg gélule', 'Lyrica 150mg gélule', 'Tegretol 200mg comprimé',
	'Dépakine 500mg comprimé', 'Laroxyl 25mg comprimé', 'Seroplex 10mg comprimé',
	// Divers
	'Cortancyl 5mg comprimé', 'Cortancyl 20mg comprimé', 'Daflon 500mg comprimé',
	'Spedifen 400mg comprimé', 'Calcium D3 comprimé', 'Tardyféron 80mg comprimé',
	'Speciafoldine 5mg comprimé', 'Vitamine D 100000UI ampoule', 'Magné B6 comprimé',
];
