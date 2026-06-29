# Medika — Product Roadmap

**Vision :** L'outil le plus simple pour gérer un cabinet médical en Algérie — un médecin gère sa journée, ses patients, ses ordonnances et son argent, sans formation.

> 📋 **État en temps réel → [GitHub Project « Medika — Roadmap »](https://github.com/users/akqira/projects/3)**
> Le détail de chaque chantier vit dans les **[issues](https://github.com/akqira/Medika/issues)** ; l'avancement par phase dans les **[milestones](https://github.com/akqira/Medika/milestones)**.
> Ce fichier ne garde que la **vision**, l'**état macro** et les **décisions d'architecture**. Pour le « qui fait quoi / où on en est », c'est GitHub.

**Légende :** ✅ Fait · 🟡 En cours · ⬜ Planifié · ⏸️ Bloqué (décision produit)

---

## État par phase

| Phase | Statut | Détail |
|---|---|---|
| 0 · Pré-lancement (login, dashboard, légal, e-mail) | ✅ — recherche navbar câblée (#54) | [milestone](https://github.com/akqira/Medika/milestone/7) |
| 1 · Refonte Fiche Patient | ✅ — validation design Kader en attente | [milestone](https://github.com/akqira/Medika/milestone/8) |
| 2 · Agenda | 🟡 En cours (drag-to-reschedule, vues jour/mois, timeline 07:00) | [milestone](https://github.com/akqira/Medika/milestone/9) |
| 3 · Consultation & Ordonnance | ✅ Terminée | [milestone](https://github.com/akqira/Medika/milestone/10) |
| 4 · Finances | ✅ — export comptable PDF/Excel ⏸️ reporté | [milestone](https://github.com/akqira/Medika/milestone/11) |
| Backlog — Plus tard | ⬜ Versements partiels, self-booking, multi-cabinet, rôles | [milestone](https://github.com/akqira/Medika/milestone/12) |

**Phases 0 à 4 livrées et vérifiées** (tests unitaires .NET + e2e Playwright + vitest front au vert en CI). **Dernière mise en production `dev → main` — [v0.2.0](https://github.com/akqira/Medika/releases/tag/v0.2.0)** (#149 ; refonte consultation/ordonnance, dossier patient éditable, toasts). Première promotion : [v0.1.0](https://github.com/akqira/Medika/releases/tag/v0.1.0) (#60). Reste non bloquant : validation visuelle dossier patient. Décisions ouvertes : `docs/Kaki/Feedback-Analyse.md`.

---

## Décisions d'architecture (à garder)

1. **Chaque document porte `cabinetId` + `doctorId`** dès maintenant, même en mono-médecin — agréger plus tard = un `$group`, pas une migration.
2. **Ordonnance générée côté serveur** — en-tête configurable par cabinet dès le départ.
3. **Modèle finance pensé pour l'agrégation** — paiements et dépenses dans leurs propres collections, pas en embarqué.
4. **Liste patients paginée côté serveur** dès la refonte — ne jamais charger tout le jeu de données (contrainte bas débit Algérie).
5. **QA automatisée en continu** — l'agent `/qa-sweep` (skill + sous-agent `qa-explorer`) et un cron GitHub Actions nocturne testent le frontend comme un vrai utilisateur (négatif / non-mutant) et ouvrent des issues `status:needs-triage`. Catalogue des contrôles : `docs/qa/check-catalog.md`.

---

## Hors périmètre (pas maintenant)

- Import relevés bancaires
- Intégration CNAS / assurance
- App mobile
- Insights IA

---

## Tenir ce fichier à jour

À chaque merge : mettre à jour l'**issue GitHub** correspondante (statut + label `status:`, colonne du board) — pas ce fichier. On ne touche ici que si la **vision**, une **phase** ou une **décision d'architecture** change. Le détail d'implémentation (bugs corrigés, ADR, couverture de test) reste dans les issues / l'historique git.
