// ─────────────────────────────────────────────────────────────────────────────
// Catalogue STRUCTURÉ de médicaments — pilote la recherche « prescrire en un clic »
// de la fenêtre Ordonnance (nouveau design MediKa).
//
// Chaque entrée porte sa classe thérapeutique (couleur), sa forme et une posologie
// par défaut → le médecin clique pour ajouter une ligne pré-remplie à l'ordonnance.
//
// Jeu curaté de démarrage. La saisie libre reste toujours possible dans chaque
// ligne d'ordonnance, et la liste complète (nom commercial, ~4800 présentations,
// static/medicaments.json) reste recherchable en texte libre — voir `medicaments.ts`.
// ─────────────────────────────────────────────────────────────────────────────

export type MedCategory =
	| 'Antalgique'
	| 'Anti-inflammatoire'
	| 'Antibiotique'
	| 'Gastro'
	| 'Antidiabétique'
	| 'Antihypertenseur'
	| 'Cardiologie'
	| 'Hypolipémiant'
	| 'Respiratoire'
	| 'Antihistaminique'
	| 'Corticoïde'
	| 'Supplément';

export type CatalogMed = {
	nom: string;
	dosage: string;
	forme: string;
	cat: MedCategory;
	/** Posologie par défaut (modifiable après ajout). */
	pos: string;
	/** Durée par défaut (modifiable après ajout). */
	duree: string;
};

export const CAT_COLOR: Record<MedCategory, string> = {
	Antalgique: '#2563EB',
	'Anti-inflammatoire': '#D97706',
	Antibiotique: '#DC2626',
	Gastro: '#7C3AED',
	Antidiabétique: '#0F766E',
	Antihypertenseur: '#0891B2',
	Cardiologie: '#BE185D',
	Hypolipémiant: '#65A30D',
	Respiratoire: '#0284C7',
	Antihistaminique: '#9333EA',
	Corticoïde: '#C2410C',
	Supplément: '#059669'
};

export const MEDICAMENT_CATALOG: CatalogMed[] = [
	{ nom: 'PARACÉTAMOL', dosage: '1g', forme: 'Comprimé', cat: 'Antalgique', pos: '1 comprimé 3 fois par jour si douleur', duree: '5 jours' },
	{ nom: 'PARACÉTAMOL', dosage: '500mg', forme: 'Comprimé', cat: 'Antalgique', pos: '1 à 2 comprimés 3 fois par jour', duree: '5 jours' },
	{ nom: 'IBUPROFÈNE', dosage: '400mg', forme: 'Comprimé', cat: 'Anti-inflammatoire', pos: '1 comprimé 3 fois par jour au repas', duree: '5 jours' },
	{ nom: 'DICLOFÉNAC', dosage: '50mg', forme: 'Comprimé', cat: 'Anti-inflammatoire', pos: '1 comprimé matin et soir au repas', duree: '5 jours' },
	{ nom: 'AMOXICILLINE', dosage: '1g', forme: 'Comprimé', cat: 'Antibiotique', pos: '1 comprimé matin et soir', duree: '7 jours' },
	{ nom: 'AMOXICILLINE', dosage: '500mg', forme: 'Gélule', cat: 'Antibiotique', pos: '1 gélule 3 fois par jour', duree: '7 jours' },
	{ nom: 'AMOX. + AC. CLAVULANIQUE', dosage: '1g', forme: 'Comprimé', cat: 'Antibiotique', pos: '1 comprimé matin et soir', duree: '7 jours' },
	{ nom: 'AZITHROMYCINE', dosage: '500mg', forme: 'Comprimé', cat: 'Antibiotique', pos: '1 comprimé par jour', duree: '3 jours' },
	{ nom: 'OMÉPRAZOLE', dosage: '20mg', forme: 'Gélule', cat: 'Gastro', pos: '1 gélule le matin à jeun', duree: '1 mois' },
	{ nom: 'MÉTOCLOPRAMIDE', dosage: '10mg', forme: 'Comprimé', cat: 'Gastro', pos: '1 comprimé avant les repas', duree: '5 jours' },
	{ nom: 'METFORMINE', dosage: '1000mg', forme: 'Comprimé', cat: 'Antidiabétique', pos: '1 comprimé matin et soir au repas', duree: '1 mois' },
	{ nom: 'METFORMINE', dosage: '850mg', forme: 'Comprimé', cat: 'Antidiabétique', pos: '1 comprimé matin et soir au repas', duree: '1 mois' },
	{ nom: 'INSULINE GLARGINE', dosage: '300UI/mL', forme: 'Stylo injectable', cat: 'Antidiabétique', pos: '20 UI le soir en sous-cutanée', duree: '3 mois' },
	{ nom: 'AMLODIPINE', dosage: '10mg', forme: 'Comprimé', cat: 'Antihypertenseur', pos: '1 comprimé par jour le matin', duree: '1 mois' },
	{ nom: 'AMLODIPINE', dosage: '5mg', forme: 'Comprimé', cat: 'Antihypertenseur', pos: '1 comprimé par jour le matin', duree: '1 mois' },
	{ nom: 'RAMIPRIL', dosage: '10mg', forme: 'Comprimé', cat: 'Antihypertenseur', pos: '1 comprimé par jour', duree: '1 mois' },
	{ nom: 'BISOPROLOL', dosage: '5mg', forme: 'Comprimé', cat: 'Cardiologie', pos: '1 comprimé par jour le matin', duree: '1 mois' },
	{ nom: 'ASPIRINE', dosage: '100mg', forme: 'Comprimé', cat: 'Cardiologie', pos: '1 comprimé par jour le soir', duree: '3 mois' },
	{ nom: 'ATORVASTATINE', dosage: '20mg', forme: 'Comprimé', cat: 'Hypolipémiant', pos: '1 comprimé le soir', duree: '3 mois' },
	{ nom: 'SALBUTAMOL', dosage: '100µg', forme: 'Spray', cat: 'Respiratoire', pos: '2 bouffées si besoin, max 4 fois/j', duree: '3 mois' },
	{ nom: 'FLUTICASONE', dosage: '125µg', forme: 'Spray', cat: 'Respiratoire', pos: '2 bouffées matin et soir', duree: '3 mois' },
	{ nom: 'CÉTIRIZINE', dosage: '10mg', forme: 'Comprimé', cat: 'Antihistaminique', pos: '1 comprimé par jour', duree: '7 jours' },
	{ nom: 'PRÉDNISOLONE', dosage: '20mg', forme: 'Comprimé', cat: 'Corticoïde', pos: '1 comprimé le matin', duree: '5 jours' },
	{ nom: 'VITAMINE D3', dosage: '100 000UI', forme: 'Ampoule buvable', cat: 'Supplément', pos: '1 ampoule par mois', duree: '3 mois' }
];
