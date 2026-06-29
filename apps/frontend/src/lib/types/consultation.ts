// Une ligne d'ordonnance, partagée entre la page consultation, la fenêtre
// Ordonnance et l'aperçu d'impression.
//
// Mapping vers le contrat backend (POST /api/consultations → prescription[]) :
//   medication → nom du médicament (+ dosage)
//   dosage     → posologie (texte libre ou code 1-0-1-0 développé)
//   frequency  → réservé (non édité dans l'UI actuelle)
//   duration   → durée du traitement
export type Med = {
	id: number;
	medication: string;
	dosage: string;
	frequency: string;
	duration: string;
};

/** Métadonnées patient nécessaires à l'en-tête de la fenêtre et à l'impression. */
export type OrdonnancePatient = {
	firstName: string;
	lastName: string;
	age: number;
	gender: string;
	bloodGroup?: string | null;
	allergies: string[];
};
