# Medika — Product Roadmap

**Vision :** L'outil le plus simple pour gérer un cabinet médical en Algérie — un médecin gère sa journée, ses patients, ses ordonnances et son argent, sans formation.

> Mise à jour du 12 juin 2026, après la revue terrain de Kader (`docs/Kaki/MyRemarks.md`).
> Les décisions encore ouvertes sont listées dans `docs/Kaki/Feedback-Analyse.md`.

---

## État réel du produit

Contrairement à la version précédente de cette roadmap, l'app n'est plus au stade « rien à montrer ». Sont déjà en place : authentification, création de médecin, dashboard, agenda, liste/fiche patient, consultation, facturation. **Le travail n'est donc plus de construire, mais de polir, corriger et crédibiliser pour livrer aux premiers médecins.**

Le frein principal n'est pas le nombre de features mais la **finition** : ergonomie de saisie, design de la fiche patient, fluidité de l'agenda, et confiance (un nouveau médecin ne doit pas sentir qu'il est le cobaye n°1).

---

## Phase 0 · Pré-lancement — Corrections & confiance
> Objectif : qu'un médecin ami ouvre l'app sans tomber sur une incohérence ou un détail qui « fait pas sérieux ». **Bugs et quick-wins avant toute nouvelle feature.**

### Login & confiance
- Retirer le compteur « nombre de médecins » (rassure mal quand on est le premier).
- Champ mot de passe : **une seule** icône œil (le doublon des deux côtés n'a pas de sens).
- Tester le flux **Mot de passe oublié** de bout en bout (voir pré-requis mail/data dans le doc feedback).
- **CGU** + **Politique de confidentialité** : brouillons prêts (`docs/legal/`). Pour le pilote : rester RGPD-clean (hébergement UE + sécurité déjà en place), médecin = responsable de traitement, transparence patient. Le volet ANPDP algérien est **séquencé pour plus tard** (voir `docs/legal/Rapport-conformite-loi-18-07.md`) — pas un blocage du pilote.

### Dashboard
- Tuile « Patients aujourd'hui » : retirer le **+8%** (chiffre faux/inventé).
- Retirer les tuiles **« reste à voir »** et **« en cours »** (peu pertinentes pour le terrain algérien, usage pas temps-réel).
- Retirer **« Total patients »** → déplacer en **icône + compteur à côté de « Patients » dans la navbar**.
- **Programme du jour** : garder, retirer le bouton « Agenda complet ».
- **Patients récents** : garder tel quel (validé).

### Facturation (vérification)
- S'assurer que la facturation fonctionne **à 100 %**.
- Mode de paiement Algérie = **Espèces