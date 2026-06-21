# Feedback & analyse — MyRemarks.md

> Réponse de Claude à la revue terrain de Kader. Objectif : répondre à tes questions, te proposer des solutions, et lister ce dont **j'ai besoin de toi** pour avancer. La roadmap correspondante est dans `ROADMAP.md`.

---

## 1. Lecture d'ensemble

Tes remarques se rangent en 4 familles :

1. **Quick-wins / bugs** (1-2 h chacun) : compteur médecins, +8%, icône œil, bouton TEL, doublon « Nouveau RDV », bouton « Agenda complet », tuiles dashboard inutiles. → à faire en premier, ça crédibilise tout de suite.
2. **Refonte fiche patient** : le plus gros chantier UX, c'est ce qui « fait pas sérieux » aujourd'hui.
3. **Agenda** : mérite un epic à part (tu l'as dit toi-même).
4. **Décisions produit ouvertes** : finances, CGU/confidentialité, contenu des tuiles dashboard, liste médicaments. → c'est là que j'ai besoin de tes réponses.

Bonne nouvelle : la majorité des points sont de la finition, pas de la reconstruction.

---

## 2. Ce que je te propose directement (pas besoin de valider, sauf désaccord)

**Icône œil mot de passe** → une seule icône, à droite du champ. Standard.

**Liste patients / performance (connexion bas débit Algérie)** → pagination serveur, **20-25 patients par page**, infinite scroll. Recherche = appel serveur déclenché après 300 ms de pause de frappe (debounce), avec `cabinetId` en premier filtre comme partout. On ne charge jamais tout. C'est le bon réflexe vu le contexte réseau.

**Navigation clavier recherche sidebar** → ↑ ↓ pour parcourir, Entrée pour ouvrir, Échap pour fermer. Rapide à faire.

**« Fréquence » dans l'ordonnance** → je propose de remplacer par un champ unique très court type **« 1-0-1 »** (matin-midi-soir, convention que les médecins connaissent) + un champ **durée** (ex. « 7j »). Exemple de ligne d'ordonnance compacte :
`Amoxicilline 500mg — 1-0-1 — 7j`
Un seul focus clavier, pas de dropdown. À valider avec un vrai médecin.

**Fiche complète patient** → mon avis : elle ne se justifie pas en double du dossier. Je propose de **la supprimer et de tout fusionner dans le nouveau dossier patient** (refonte Phase 1). Tu me dis si tu vois un usage que je rate.

**Bouton TEL** → on garde le numéro en texte simple, copiable, sans `tel:` ni action.

---

## 3. Réponses à tes questions explicites

### Mot de passe oublié — que préparer ?
Oui, il faut préparer 2 choses :
- **Un envoi d'e-mail** : un compte SMTP / service transactionnel (ex. un service d'envoi type Resend/SendGrid/Mailgun, ou le SMTP de ton hébergeur). Il faut une adresse expéditeur (ex. `noreply@medika.dz`) et la clé API en variable d'env (jamais committée, cf. CLAUDE.md).
- **Côté data** : un champ pour stocker le **token de réinitialisation** (valeur hashée + date d'expiration ~30 min) sur l'utilisateur, et l'e-mail de l'utilisateur doit être fiable/vérifié.
- **Question pour toi** : les médecins ont-ils tous une adresse e-mail fiable ? En Algérie, beaucoup utilisent surtout le téléphone. → faut-il prévoir une **réinitialisation par SMS** plutôt (ça change l'implémentation et le coût) ?

### Liste de médicaments — d'où la ramener ?
Il existe une source officielle : la **Nomenclature Nationale des Produits Pharmaceutiques** (Ministère de l'Industrie Pharmaceutique / ANPP), ~4800 médicaments avec dosage, labo, n° d'enregistrement. C'est LA bonne base.
- Le ministère publie des versions régulières (fichier Excel). On l'importe une fois en base, on rafraîchit 1-2x/an.
- Des dépôts GitHub ont déjà converti d'anciennes versions en SQL/JSON (utile comme point de départ, mais à recaler sur la version officielle récente).
- **Reco** : on intègre la nomenclature comme table de référence (nom commercial + DCI + dosage), autocomplete dessus, + bouton « ajouter médicament libre » pour les exceptions.
- **Question pour toi** : tu préfères afficher le **nom commercial** (Doliprane) ou la **DCI / molécule** (Paracétamol) en premier dans l'autocomplete ? Les deux ? (les médecins prescrivent souvent en commercial).

### Tuiles dashboard de remplacement (« reste à voir » / « en cours »)
Vu que l'usage n'est pas temps-réel, je propose des tuiles **utiles le matin et le soir**, pas du live :
- **Recette du jour** (espèces encaissées aujourd'hui).
- **RDV du jour** (nombre + prochains horaires).
- **Factures impayées / reliquats** (quand on aura les versements).
- **Recette du mois** vs mois précédent.
Dis-moi lesquelles te parlent, on en garde 2-3 max.

### Finances — qu'est-ce qu'on y met ?
Je ne te propose rien tant que je ne sais pas comment tu raisonnes ton cabinet. Questions ci-dessous (section 4).

---

## 4. Ce dont j'ai besoin de toi pour avancer

### A. Finances (bloquant pour cadrer la Phase 4)
1. Aujourd'hui, qu'est-ce que tu veux **suivre** : juste les recettes (consultations payées) ? ou aussi les **dépenses** (loyer, matériel, charges) ?
2. Tu veux un **P&L mensuel** (gagné − dépensé = net) ou juste « combien j'ai encaissé » ?
3. Y a-t-il des **honoraires variables** par acte/médecin, ou un tarif de consultation fixe ?
4. Besoin d'**exporter** (PDF/Excel) pour le comptable ?

### B. CGU (Conditions Générales d'Utilisation)
Pour les générer, dis-moi :
1. Entité juridique : nom de la société/personne qui édite Medika, statut, pays (Algérie ?).
2. Modèle : gratuit / payant / abonnement ? (impacte les clauses de paiement et résiliation).
3. Qui est responsable des données saisies par le médecin (lui, en tant que praticien) ?
4. Loi applicable et tribunal compétent (Algérie j'imagine ?).
5. Limitation de responsabilité : Medika = outil de gestion, **pas** un dispositif médical / pas d'aide à la décision clinique — à acter ?

### C. Politique de confidentialité (données de santé = sensible)
1. Où sont **hébergées** les données ? (MongoDB Atlas — quelle région ? Europe ? Hors Algérie a des implications légales pour des données de santé algériennes.)
2. Quelles données patient sont stockées exactement (nom, date de naissance, téléphone, données cliniques) ?
3. Durée de **conservation** des données ? Que se passe-t-il si un médecin supprime son compte ?
4. Le patient peut-il demander l'accès / la suppression de ses données ? Via qui (le médecin) ?
5. Sous-traitants : hébergeur (Atlas), stockage fichiers (Cloudflare R2), service e-mail/SMS — à lister.
6. **Important** : connais-tu la réglementation algérienne sur la protection des données de santé (loi 18-07 sur la protection des données personnelles) ? Je peux faire une recherche dédiée si tu veux qu'on s'y conforme proprement.

### D. Décisions UX à confirmer
1. Format ordonnance « 1-0-1 + durée » : OK pour tester avec un médecin ?
2. Suppression de la « fiche complète patient » : d'accord ?
3. Réinitialisation mot de passe par **e-mail** ou **SMS** ?

---

## 5. Ordre que je te recommande

1. **Phase 0** (bugs + quick-wins + facturation 100 % + légal) — crédibilise et débloque le test réel.
2. **Phase 1** (refonte fiche patient) en parallèle du cadrage légal/finances.
3. **Phase 2** (Agenda) — spec dédiée.
4. **Phase 3** (ergonomie consultation/ordonnance).
5. **Phase 4** (Finances) une fois tes réponses section 4.A reçues.

Dis-moi sur quoi tu veux qu'on attaque en premier, et réponds aux questions section 4 quand tu peux — je peux commencer les quick-wins sans attendre.

---

**Sources médicaments :**
- [Nomenclature Nationale des Produits Pharmaceutiques — Ministère de l'Industrie Pharmaceutique](https://www.miph.gov.dz/fr/nomenclature-nationale-des-produits-pharmaceutiques/)
- [ANPP — Direction de l'enregistrement des produits pharmaceutiques](https://anpp.dz/en/a-propos-direction-denregistrement/)
- [Dépôt GitHub — Nomenclature des médicaments en Algérie (SQL/JSON)](https://github.com/mahmoudBens/Nomenclature-des-medicaments-en-algerie)
