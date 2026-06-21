# Medika — Product Roadmap

**Vision :** L'outil le plus simple pour gérer un cabinet médical en Algérie — un médecin gère sa journée, ses patients, ses ordonnances et son argent, sans formation.

> Mise à jour du **21 juin 2026** : **Phases 0 à 4 terminées et vérifiées** (unit .NET + e2e au vert en CI, déployées sur `dev`).
> Issue de la revue terrain de Kader (`docs/Kaki/MyRemarks.md`).
> Les décisions encore ouvertes sont listées dans `docs/Kaki/Feedback-Analyse.md`.

**Légende :** ✅ Fait · 🟡 En cours · ⬜ Planifié · ⏸️ Bloqué (décision produit)

---

## État réel du produit

L'app n'est plus au stade « rien à montrer ». Sont en place et **vérifiés** : authentification, création de médecin, dashboard, agenda, liste/fiche patient, consultation, facturation espèces, autocomplétion des médicaments (nomenclature algérienne), suite E2E Playwright **au vert**, et le pack légal. **La Phase 0 (pré-lancement) est terminée** — l'app est crédible pour les premiers médecins amis.

**Phases 1 à 4 : terminées et vérifiées** — dossier patient, agenda, ergonomie consultation/ordonnance, et finances (recettes + dépenses, P&L mensuel, catalogue d'actes + sélecteur dans la consultation, répartition des recettes par acte). Tout est vérifié en exécution, couvert par la CI (e2e Playwright + tests unitaires .NET sur le push `dev`) et déployé sur `dev`. Gaps ciblés corrigés au passage : unicité NSS, posologie (texte libre + raccourci 1-0-1-0), mot de passe oublié, rate-limit auth par IP, bug catégorie de dépense (FR/enum), endpoints de suppression patient/dépense/acte.

**Reste ouvert (non bloquant) :**
- ▢ Validation visuelle du dossier patient par Kader (réf. `design-medical.pdf`).
- ⏸️ Export comptable PDF/Excel (Phase 4 — reporté, décision Kader).
- ▢ Outillage : brancher les tests unitaires **vitest** (front) en CI — ils ne tournent qu'en local pour l'instant.
- ▢ Mise en production (`dev` → `main`) quand Kader valide.

---

## Phase 0 · Pré-lancement — Corrections & confiance ✅ TERMINÉE
> Objectif : qu'un médecin ami ouvre l'app sans tomber sur une incohérence ou un détail qui « fait pas sérieux ». **Bugs et quick-wins avant toute nouvelle feature.**
> _Tous les points ci-dessous ont été audités sur le code et vérifiés._

### Login & confiance
- ✅ Retirer le compteur « nombre de médecins » (stats remplacées par Wilayas / Algérienne / Disponibilité).
- ✅ Champ mot de passe : **une seule** icône œil à droite (l'icône de gauche est un cadenas non cliquable).
- ✅ **Mot de passe oublié** — flux e-mail de bout en bout : page `/forgot-password` → token à usage unique (haché, expire en 30 min) → page `/reset-password`. Sans énumération de comptes (réponse générique), token consommé après usage. **Canal e-mail** retenu (SMS reporté). Livraison via `IPasswordResetSender` : pour l'instant le lien est **journalisé côté serveur** (`LoggingPasswordResetSender`) — il restera à brancher un vrai fournisseur e-mail (Resend/SendGrid/SMTP) le moment venu, sans toucher au reste. Couvert par `e2e/password-reset.spec.ts`.
- ✅ **Rate-limit auth par IP client** : `login` / `forgot-password` / `reset-password` sont throttlés 5/60s, désormais **keyés sur l'IP réelle de l'utilisateur** que le BFF transmet en en-tête `X-Client-IP` (et non `X-Forwarded-For`, réécrit par l'hôte avec l'IP du BFF). Skippé en Development. Ancien `AddRateLimiter("login")` ASP.NET mort retiré.
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

## Phase 1 · Refonte Fiche Patient ✅ TERMINÉE (validation design Kader en attente)
> La remarque la plus structurante. La fiche a été refondue en dossier 2 colonnes (identité à gauche, clinique à droite).
> Réf. design : `docs/Kaki/images/design-medical.pdf` + `medical-record.webp`.
> _Audité sur le code : tout est implémenté de bout en bout (route front + handler back). Reste : **vérification fonctionnelle en exécution** et **validation visuelle par Kader** contre la réf. design._

- ✅ Fiche en **vrai dossier patient** : colonne identité à gauche (liste propre, plus de tuiles « fiche produit »), clinique à droite (antécédents/allergies, consultations, facturation).
- ✅ **Recherche patient (page Patients)** : navigation clavier (↑ ↓ + Entrée + Échap).
- ✅ **Performance liste patients** : pagination serveur (pageSize 20, skip/limit Mongo) + infinite scroll + recherche serveur debouncée 300 ms — jamais de chargement total. Adapté bas débit.
- Bloc **Consultations** dans la fiche :
  - ✅ Bouton **« Ajouter une consultation »** → ouvre `/consultation?patientId=` avec le patient pré-sélectionné et verrouillé.
  - ✅ Clic sur une consultation → panneau détaillé : **constantes vitales, anamnèse, examen, notes, diagnostic & honoraires**.
  - ✅ **Ordonnance imprimable** (PDF QuestPDF côté serveur, en-tête médecin) depuis le détail.
- ✅ Pas de **« fiche complète patient »** redondante (n'existe pas / déjà fusionnée).

**Signal de succès :** un médecin ouvre un patient, voit son historique d'un coup d'œil, démarre une consultation en un clic et imprime l'ordonnance — sans quitter la fiche.

**Vérifié le 21/06/2026** (app en exécution, login → dossier → détail consultation → ordonnance) : flux complet fonctionnel. Bug trouvé et corrigé au passage : création d'un 2ᵉ patient **sans NSS** échouait (index unique `nss` rejetait les `null` en double) → index unique partiel par cabinet. **Couvert par un test e2e de régression** (`apps/frontend/e2e/patient-new-nss.spec.ts` : deux patients sans NSS créés à la suite — vert). Les accents (« Frères », « épouse ») sont bien gérés en création ; seul l'ancien enregistrement seed « Benali » contient des caractères corrompus (donnée obsolète, cosmétique).

**Outils ajoutés :** endpoint **DELETE `/api/patients/{id}`** (scopé cabinet, refus 400 si consultations/factures liées) + route proxy SvelteKit ; le test e2e NSS s'auto-nettoie désormais via cet endpoint (re-jouable, sans accumulation).

**Reste à faire :** ▢ validation visuelle Kader (la réf. `design-medical.pdf` est un template riche avec courbes/calendrier — à arbitrer : on reste sur le dossier sobre actuel, ou on enrichit ?) · ▢ nettoyer les anciens enregistrements résiduels (« Benali » corrompu, patients de test créés avant l'auto-nettoyage) — possible maintenant via l'endpoint DELETE.

---

## Phase 2 · Feature Agenda (epic dédié) ⬜
> Identifié par Kader comme « beaucoup de travail à venir ». Mérite son propre chantier.
> Réf. design : `docs/Kaki/images/calendar.webp`.

- ✅ Bouton **« Aujourd'hui »** pour revenir à la date du jour.
- ✅ Doublon du bouton « Nouveau RDV » supprimé (un seul, en haut).
- ✅ Vue **semaine** affichant la **journée entière d'un coup** sans scroll (timeline 08:00–20:00, ajustée à la hauteur écran).
- ✅ Création de RDV via modale (recherche patient, date, heure, durée, type, motif).
- 🟡 Affaire à arbitrer : démarrer la timeline à **07:00** plutôt que 08:00 (mineur).
- ⬜ Bascule de vues **jour / semaine / mois** (non demandé explicitement — à confirmer si utile).
- ⬜ **Déplacer un RDV** (drag-to-reschedule / édition) — non implémenté.

---

## Phase 3 · Consultation & Ordonnance — ergonomie de saisie ✅ TERMINÉE
> Principe directeur : le médecin écrit **vite**, peu de clics, peu d'allers-retours clavier/souris.

- ✅ **Dropdown patient** : se compacte en barre fine (avatar + nom + « Changer ») une fois le patient sélectionné.
- ✅ **Constantes vitales** : champs courts en ligne (flex-wrap, largeurs 66-92px) — espace resserré.
- ✅ Anamnèse / Examen : OK.
- **Ordonnance** :
  - ✅ **Liste de médicaments** auto-complétée sur la nomenclature (`/medicaments.json`) + ajout libre (« … sera utilisé tel quel »).
  - ✅ **Posologie** : champ **texte libre visible** sur la ligne d'ordonnance (placeholder « 1 cp matin et soir »), affiché à côté du nom du médicament dans le dossier/PDF. Le champ `dosage` n'est plus caché. Couvert par un test e2e (`consultation.spec.ts`).
  - ✅ Champ ambigu **« Prise » / « Fréquence » retiré** (« laisse tomber, c'est ambigus » — feedback Kader).
  - ✅ **Raccourci posologie « 1-0-1-0 »** : en plus du texte libre, taper un motif `matin-midi-soir-coucher` (3 ou 4 segments) affiche un aperçu en direct (« = 1 matin, 2 soir, 1 coucher ») et se normalise au blur. Le texte libre reste prioritaire (un motif non reconnu n'est pas modifié). Logique pure dans `src/lib/posology.ts` (test unitaire vitest) + e2e dans `consultation.spec.ts`.
- ✅ **Brouillon de consultation retiré** : le bouton « Enregistrer brouillon » et l'état intermédiaire sont supprimés — une consultation se **finalise** directement (un seul bouton, `finalize: true`). Simplifie le flux et évite les consultations à moitié saisies. e2e `consultation.spec.ts` mis à jour (le garde « patient requis » passe désormais par la finalisation).
- ✅ **« Zone de danger » (suppression de compte) retirée** de la page Profil — fonctionnalité non implémentée côté backend, le bouton ne faisait rien.

---

## Phase 4 · Finances ✅ TERMINÉE (export comptable reporté)
> Cadrage tranché avec Kader : **recettes + dépenses**, **P&L mensuel (net)**, honoraires via **catalogue d'actes + tarifs**, export comptable **plus tard**.

**Déjà en place (audit) :** dépenses (`Charge` : catégorie, montant, date) avec création/liste + page Finances ; factures (recettes, total payé par période) ; **P&L mensuel déjà calculé** (`GetFinancialSummaryHandler` : `NetIncome = recettes − dépenses`, tendance 6 mois, impayés) affiché sur le dashboard et la page Finances.

**À faire :**
- ✅ **Bug dépenses corrigé** : la page Finances envoyait des catégories en français (« Loyer »…) alors que l'enum backend attend l'anglais (`Rent`…) → `Enum.Parse` échouait, l'ajout plantait. La page envoie désormais la valeur d'enum (libellés FR affichés). Endpoint **DELETE `/api/charges/{id}`** ajouté (scopé cabinet) + proxy ; e2e happy-path (création → affichage → suppression) qui aurait détecté le bug.
- ✅ **Catalogue d'actes + tarifs** :
  - ✅ Entité `Act` par cabinet (nom + tarif) + repo/index + endpoints **POST/GET/DELETE `/api/acts`** (scopés cabinet) ; écran de gestion `(app)/actes` (ajout/liste/suppression), accessible depuis Finances. Tests : **unit .NET** (`Medika.Tests/Finance` — domaine + handlers Add/Delete, 9 tests) + **e2e** (`actes.spec.ts`).
  - ✅ **Sélecteur d'acte dans la consultation** : un menu « Acte » (depuis le catalogue) pré-remplit les honoraires à la sélection, montant restant modifiable (« Acte libre » par défaut). e2e dans `consultation.spec.ts`.
- ✅ **Répartition des recettes par acte** : la facture porte l'acte facturé (`Invoice.ActName`, depuis le sélecteur de consultation) ; `breakdownByType` agrège désormais les factures **payées** par acte (libellé, montant, %), tri décroissant, « Autres » pour les actes libres. Tests : **unit .NET** (`Invoice` + `GetFinancialSummaryHandler`) ; logique d'agrégation couverte en unitaire (un e2e exigerait une finalisation de consultation irréversible).
- ⏸️ Export PDF/Excel comptable — reporté (décision Kader).

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
