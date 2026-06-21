# Medika — Product Roadmap

**Vision :** L'outil le plus simple pour gérer un cabinet médical en Algérie — un médecin gère sa journée, ses patients, ses ordonnances et son argent, sans formation.

> Mise à jour du **21 juin 2026** : Phase 0 terminée et vérifiée, passage à la Phase 1.
> Issue de la revue terrain de Kader (`docs/Kaki/MyRemarks.md`).
> Les décisions encore ouvertes sont listées dans `docs/Kaki/Feedback-Analyse.md`.

**Légende :** ✅ Fait · 🟡 En cours · ⬜ Planifié · ⏸️ Bloqué (décision produit)

---

## État réel du produit

L'app n'est plus au stade « rien à montrer ». Sont en place et **vérifiés** : authentification, création de médecin, dashboard, agenda, liste/fiche patient, consultation, facturation espèces, autocomplétion des médicaments (nomenclature algérienne), suite E2E Playwright **au vert**, et le pack légal. **La Phase 0 (pré-lancement) est terminée** — l'app est crédible pour les premiers médecins amis.

Le frein restant n'est pas le nombre de features mais la **finition** : design de la fiche patient (Phase 1), fluidité de l'agenda (Phase 2), ergonomie de saisie (Phase 3).

**Prochaine étape : Phase 1 — refonte de la fiche patient.**

---

## Phase 0 · Pré-lancement — Corrections & confiance ✅ TERMINÉE
> Objectif : qu'un médecin ami ouvre l'app sans tomber sur une incohérence ou un détail qui « fait pas sérieux ». **Bugs et quick-wins avant toute nouvelle feature.**
> _Tous les points ci-dessous ont été audités sur le code et vérifiés._

### Login & confiance
- ✅ Retirer le compteur « nombre de médecins » (stats remplacées par Wilayas / Algérienne / Disponibilité).
- ✅ Champ mot de passe : **une seule** icône œil à droite (l'icône de gauche est un cadenas non cliquable).
- ⬜ Tester le flux **Mot de passe oublié** de bout en bout (voir pré-requis mail/data dans le doc feedback) — _décision e-mail vs SMS encore ouverte._
- ✅ **CGU** + **Politique de confidentialité** + conformité loi 18-07 : en place (`docs/legal/`). Médecin = responsable de traitement, transparence patient. Le volet ANPDP algérien est séquencé pour plus tard — pas un blocage du pilote.

### Dashboard
- ✅ Tuile « Patients aujourd'hui » : **+8%** retiré (plus aucun delta inventé).
- ✅ Tuiles **« reste à voir »** / **« en cours »** remplacées par : RDV aujourd'hui · Recette du mois · Bénéfice du mois · Impayés.
- ✅ **« Total patients »** déplacé en **compteur à côté de « Patients » dans la navbar** (alimenté par un appel `pageSize=1`).
- ✅ **Programme du jour** : bouton « Agenda complet » retiré.
- ✅ **Patients récents** : conservé tel quel.

### Facturation (vérification)
- ✅ Facturation fonctionnelle de bout en bout.
- ✅ Mode de paiement = **Espèces uniquement** (`paymentMethod: 'Cash'`, aucun sélecteur carte/chèque).
- ✅ Bouton **TEL** : numéro visible mais **non cliquable** (plus de lien `tel:`).

### Saisie médecin
- ✅ Autocomplétion **ordonnance** sur la nomenclature nationale des médicaments + ajout libre.
- ✅ Suite **E2E Playwright** (failing-path first) — tous les specs au vert.

### Reliquat (non bloquant, reporté)
- ⬜ La barre de recherche **globale de la navbar** est décorative → la câbler sur le même comportement que la recherche de la page Patients dans une passe ultérieure.

---

## Phase 1 · Refonte Fiche Patient 🟡 PROCHAINE
> La remarque la plus structurante. Aujourd'hui la fiche ressemble à une « fiche produit » (tuiles), pas à un dossier médical.
> Réf. design : `docs/Kaki/images/design-medical.pdf` + `medical-record.webp`.

- ⬜ Repenser la fiche en **vrai dossier patient** : colonne identité/infos à gauche, contenu clinique structuré à droite (antécédents, dernières consultations, constantes, ordonnances).
- ✅ **Recherche patient (page Patients)** : navigation clavier (↑ ↓ + Entrée + Échap) dans les résultats — _déjà livré._
- ⬜ **Performance liste patients** : pas de chargement total au démarrage → **pagination / infinite scroll** + recherche serveur limitée (adapté connexion bas débit, voir doc feedback pour la stratégie).
- Bloc **Consultations** dans la fiche :
  - ⬜ Bouton **« Ajouter une consultation »** depuis le dossier → ouvre une consultation avec le patient déjà sélectionné.
  - ⬜ Clic sur une consultation existante → afficher ce que le médecin a saisi : **constantes vitales, anamnèse, notes complémentaires, diagnostic & honoraires**.
  - ⬜ **Ordonnance imprimable** depuis le détail de consultation (ouvrir / lancer l'impression).
- ⬜ Trancher l'utilité de la **« fiche complète patient »** (voir doc feedback — proposition : la fusionner dans le nouveau dossier).

**Signal de succès :** un médecin ouvre un patient, voit son historique d'un coup d'œil, démarre une consultation en un clic et imprime l'ordonnance — sans quitter la fiche.

---

## Phase 2 · Feature Agenda (epic dédié) ⬜
> Identifié par Kader comme « beaucoup de travail à venir ». Mérite son propre chantier.
> Réf. design : `docs/Kaki/images/calendar.webp`.

- Corriger l'affichage actuel : trop zoomé, navigation peu fluide/intuitive.
- Bouton **« Aujourd'hui »** pour revenir à la date du jour.
- Supprimer le **doublon** du bouton « Nouveau RDV » (un seul, en haut).
- Vue **semaine** affichant la **journée entière** (~12 h visibles d'un coup, pas une fenêtre de 6 h).
- À détailler en spec dédiée (vues jour/semaine/mois, création/déplacement de RDV, statuts).

---

## Phase 3 · Consultation & Ordonnance — ergonomie de saisie ⬜
> Principe directeur : le médecin écrit **vite**, peu de clics, peu d'allers-retours clavier/souris.

- **Dropdown patient** : compacter (ne pas garder une grande zone une fois le patient sélectionné).
- **Constantes vitales** : champs courts (5-6 caractères) → resserrer la mise en page, arrêter de gaspiller l'espace.
- Anamnèse / Examen : OK pour l'instant.
- **Ordonnance** :
  - **Liste de médicaments** auto-complétée à partir de la **Nomenclature Nationale des Produits Pharmaceutiques** (source officielle ANPP / Ministère de l'Industrie Pharmaceutique) + possibilité pour le médecin d'ajouter un médicament libre. Détails sourcing dans le doc feedback.
  - **Posologie** saisie sur la même ligne que le nom du médicament (gain de place).
  - Remplacer le champ ambigu **« Fréquence »** par un format bref à valider (proposition dans le doc feedback).

---

## Phase 4 · Finances (à cadrer) ⏸️ BLOQUÉE
> Rien n'a encore été proposé côté produit. Périmètre à définir avec Kader (questions dans le doc feedback).

Pistes (à confirmer) : total du jour / du mois, recettes par période, suivi de dépenses par catégorie, P&L mensuel simple.

---

## Plus tard (backlog priorisé)
- **Gestion des versements** : encaissement partiel d'une facture (acompte + reliquat). Nécessite un modèle de paiements multiples par facture.
- Idées de tuiles dashboard de remplacement (voir doc feedback).
- Prise de RDV par le patient (URL publique) — design d'abord.
- Cabinet multi-médecins, rôles (médecin / secrétaire), vue consolidée.

---

## Décisions d'architecture (à garder)
1. **Chaque document porte `cabinetId` + `doctorId`** dès maintenant, même en mono-médecin — agréger plus tard = un `$group`, pas une migration.
2. **Ordonnance générée côté serveur** — en-tête configurable par cabinet dès le départ.
3. **Modèle finance pensé pour l'agrégation** — paiements et dépenses dans leurs propres collections, pas en embarqué.
4. **Liste patients paginée côté serveur** dès la refonte — ne jamais charger tout le jeu de données (contrainte bas débit Algérie).

---

## Hors périmètre (pas maintenant)
- Import relevés bancaires
- Intégration CNAS / assurance
- App mobile
- Insights IA
