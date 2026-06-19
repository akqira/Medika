# Politique de confidentialité — Medika

> **Brouillon de travail.** Base rédigée à partir des choix de l'éditeur. **À relire et valider par un avocat algérien** spécialisé en protection des données de santé avant mise en ligne. Passages entre crochets `[…]` à compléter.

**Dernière mise à jour :** [date]
**Éditeur / sous-traitant :** Oritek — `https://medika.app`

---

## 1. Préambule

La présente Politique explique comment **Medika** (éditée par **Oritek**) traite les données personnelles dans le cadre de son service de gestion de cabinet médical, conformément à la **loi algérienne n° 18-07 du 10 juin 2018** relative à la protection des personnes physiques dans le traitement des données à caractère personnel.

Les données de santé étant des **données sensibles**, elles font l'objet de protections renforcées décrites ci-dessous.

## 2. Rôles : qui est responsable de quoi

| Acteur | Rôle | Responsabilité |
|---|---|---|
| **Le médecin** (Utilisateur) | **Responsable de traitement** | Détermine pourquoi et comment les données de ses patients sont traitées. Répond de leur licéité, de l'information des patients et du recueil du consentement. |
| **Oritek / Medika** | **Sous-traitant** | Traite les données **uniquement** pour le compte du médecin et selon ses instructions, aux seules fins de fournir le Service. |

Oritek ne réutilise jamais les données patient à ses propres fins (pas de revente, pas de publicité, pas d'exploitation commerciale).

## 3. Données traitées

### 3.1 Données des Utilisateurs (médecins / personnel)
- Identité : nom, prénom, civilité, spécialité.
- Coordonnées : e-mail, téléphone.
- Données de cabinet : nom du cabinet, adresse, en-tête d'ordonnance.
- Données de connexion : identifiants, journaux techniques, jeton de réinitialisation de mot de passe (haché, à durée limitée).
- Données d'abonnement et de facturation.

### 3.2 Données des Patients (saisies par l'Utilisateur)
- **Identité** : nom, prénom, date de naissance, sexe.
- **Coordonnées** : téléphone, e-mail, adresse.
- **Données de santé** : motif de consultation, constantes vitales, anamnèse, examen, diagnostic, notes complémentaires, ordonnances et médicaments prescrits, groupe sanguin, allergies, antécédents.
- **Données administratives** : rendez-vous, factures et paiements.

## 4. Finalités du traitement

Les données sont traitées pour :

- fournir les fonctionnalités du Service (agenda, dossiers, consultations, ordonnances, finances) ;
- permettre l'authentification et la sécurité des comptes ;
- générer les documents (ordonnances PDF) ;
- assurer le support, la maintenance et l'amélioration technique du Service ;
- gérer la facturation de l'abonnement.

## 5. Base légale

Le traitement repose sur :
- l'**exécution du contrat** d'abonnement (pour les données des Utilisateurs) ;
- la **relation de soin** et les obligations du médecin envers ses patients, sous la responsabilité du médecin responsable de traitement (pour les Données patient) ;
- le cas échéant, le **consentement** recueilli par le médecin auprès de ses patients.

## 6. Hébergement et localisation des données

| Donnée | Sous-traitant ultérieur | Localisation |
|---|---|---|
| Base de données (dossiers, RDV, finances) | **MongoDB Atlas** | **Europe** |
| Fichiers et ordonnances PDF | **Cloudflare R2** (ou Amazon S3) | [région à confirmer] |
| Envoi d'e-mails et de SMS (ex. réinitialisation de mot de passe) | **Brevo** | [UE] |
| Hébergement de l'application web (frontend) | **Vercel** | [région à confirmer] |
| Hébergement de l'API (backend) | **Azure App Service** | [région à confirmer] |

> ⚠️ **Transfert hors d'Algérie.** Les données étant hébergées **en dehors du territoire algérien** (Europe notamment), ce transfert est encadré par la loi 18-07 (voir le rapport de conformité dédié). Une **information claire des patients** et, le cas échéant, les **autorisations requises auprès de l'ANPDP**, doivent être mises en place. Point à traiter avec un juriste.

## 7. Sous-traitants

Oritek fait appel aux sous-traitants listés à l'article 6, dans un objectif de **transparence totale**. Chacun n'a accès aux données que dans la mesure nécessaire à sa prestation et est tenu à des obligations de sécurité et de confidentialité. La liste est tenue à jour et communiquée sur demande.

## 8. Durée de conservation

- **Données patient** : conservées tant que le compte du médecin est actif. Le médecin, en tant que responsable de traitement, fixe les durées conformes à ses obligations professionnelles et légales [durées de conservation des dossiers médicaux en Algérie à préciser].
- **En cas de suppression du compte / résiliation** : **l'ensemble des données du médecin sont supprimées** des systèmes d'Oritek, après un éventuel délai de récupération [ex. 30 jours] permettant l'export.
- **Données de facturation de l'abonnement** : conservées selon les obligations comptables et fiscales applicables.
- **Jetons de réinitialisation de mot de passe** : supprimés après expiration (durée courte, ex. 30 minutes).

## 9. Sécurité

Oritek met en œuvre des mesures techniques et organisationnelles, notamment :
- chiffrement des échanges (HTTPS/TLS) ;
- mots de passe stockés sous forme **hachée** ;
- authentification par jeton signé, accès cloisonné **par cabinet** (un cabinet ne peut accéder aux données d'un autre) ;
- protection de l'API par clé d'accès et contrôle anti-rejeu ;
- journalisation des accès avec **masquage des données sensibles** dans les journaux.

## 10. Droits des personnes

### 10.1 Droits des patients
Conformément à la loi 18-07, le patient dispose de droits d'**accès, de rectification et de suppression** de ses données, ainsi que d'opposition dans les conditions prévues par la loi.

> **Comment les exercer :** le patient adresse sa demande **à son médecin ou à l'assistant(e) du médecin**, qui est le responsable de traitement et dispose des outils nécessaires dans Medika pour y répondre. Oritek assiste le médecin sur le plan technique si nécessaire.

### 10.2 Droits des Utilisateurs
Les Utilisateurs peuvent accéder à leurs données de compte, les rectifier, et demander la suppression de leur compte.

## 11. Cookies et traceurs

[Décrire les cookies utilisés : strictement nécessaires (session/authentification), éventuels cookies de mesure d'audience. À compléter selon l'implémentation réelle.]

## 12. Violation de données

En cas de violation de données susceptible d'engendrer un risque pour les personnes concernées, Oritek en informe sans délai le médecin responsable de traitement concerné et coopère aux notifications requises auprès de l'**ANPDP** dans les conditions prévues par la loi.

## 13. Modification de la Politique

La présente Politique peut être mise à jour. Les modifications substantielles sont portées à la connaissance des Utilisateurs par un moyen approprié.

## 14. Contact

Pour toute question ou pour exercer un droit relatif au Service : [adresse e-mail, ex. `privacy@medika.app`].
Pour les patients : s'adresser à leur médecin (responsable de traitement).

---

### Points à compléter / valider avec un juriste
- Régions exactes d'hébergement R2/S3, Vercel, Azure (art. 6).
- Encadrement du **transfert de données de santé hors d'Algérie** et formalités ANPDP (art. 6 + rapport 18-07).
- Durées légales de conservation des dossiers médicaux en Algérie (art. 8).
- Politique cookies réelle (art. 11).
- Désignation éventuelle d'un point de contact / référent données chez Oritek.
