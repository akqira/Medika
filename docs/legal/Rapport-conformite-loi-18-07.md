# Conformité données — chemin pragmatique pour Medika

> Note d'orientation pour Oritek (fondateur basé en **Belgique / UE**), en phase **pilote**. **Ce n'est pas un avis juridique** ; c'est une mise en perspective pour avancer sereinement. Un conseil local sera utile au moment de scaler, pas avant.

**Date :** 12 juin 2026
**Idée directrice :** tu n'as pas à résoudre la conformité algérienne formelle aujourd'hui. Reste propre côté **RGPD**, garde le **médecin comme responsable**, sois **transparent** — et séquence le reste.

---

## 1. La bonne nouvelle d'abord

Trois faits jouent en ta faveur :

1. **Tu es un opérateur UE (Belgique).** Ton cadre principal est le **RGPD**, le standard le plus exigeant au monde. Tes données sont déjà hébergées dans l'UE. La loi algérienne 18-07 est calquée sur le RGPD : être solide côté RGPD, c'est cocher l'essentiel des deux.

2. **Le médecin est le responsable de traitement, pas toi.** Oritek est **sous-traitant** : tu fournis un outil, le médecin décide quoi faire des données de ses patients. La déclaration/autorisation pour données de santé pèse **d'abord sur le médecin**. Ton job : outil sécurisé + transparence.

3. **Tu es en pilote, pas en déploiement national.** Quelques médecins amis = validation produit. Ce n'est pas le moment des formalités lourdes ; c'est le moment de prouver que le produit sert.

## 2. Ce que tu fais déjà bien (à simplement documenter)

Ton architecture couvre déjà une grande partie des exigences « sécurité » :

- cloisonnement strict **par cabinet** (`cabinetId`) ;
- mots de passe **hachés**, authentification par jeton signé ;
- API protégée par clé d'accès + horodatage anti-rejeu ;
- journalisation avec **masquage des données sensibles** ;
- chiffrement des échanges (HTTPS/TLS) ;
- hébergement **dans l'UE**.

Tu n'as pas à construire ça : tu l'as. Il suffit de l'écrire noir sur blanc le jour où on te le demande.

## 3. Le plan, séquencé

### Maintenant (phase pilote) — léger, à ta main
- **Rester RGPD-clean** : hébergement UE (fait), sécurité (faite), politique de confidentialité claire (brouillon prêt).
- **Garder le médecin comme responsable** dans les CGU et la politique (déjà le cas).
- **Transparence patient** : fournir à chaque médecin pilote une courte **notice d'information patient** type (« vos données sont gérées par votre médecin via l'outil Medika, hébergé dans l'UE, etc. »). Le médecin la transmet/affiche.
- **Annexe sous-traitance (DPA)** légère entre Oritek et le médecin (responsable ↔ sous-traitant). Modèle RGPD standard, réutilisable.
- Ne **pas** se lancer dans l'ANPDP maintenant. C'est volontaire et raisonnable à ce stade.

### À la traction (quand de vrais cabinets payants arrivent)
- **Consulter un avocat / partenaire local algérien** une seule fois, pour cadrer : qui déclare quoi à l'ANPDP, et comment encadrer proprement le fait que l'hébergement est en UE. Un intermédiaire sur place rend « compliqué là-bas » → gérable.
- **Registre des traitements** côté Oritek + **désignation d'un référent données**.
- **Procédure de violation de données** formalisée.

### Au scale (déploiement large / public)
- Finaliser le volet ANPDP avec le conseil local.
- Réévaluer l'hébergement seulement si nécessaire (ex. exigence formelle de résidence des données) — pas avant.

## 4. Sur l'hébergement en Algérie

Tu as dit ne pas pouvoir (ni vouloir) héberger en Algérie. **Ce n'est pas un mur.** Le transfert de données hors d'Algérie se gère par l'**information des patients** et, le cas échéant, une **autorisation** — pas obligatoirement par une relocalisation des serveurs. Beaucoup d'éditeurs servent des marchés depuis l'UE. On garde donc l'UE, et on traite le sujet par la transparence et le conseil local au bon moment.

## 5. Le cadre, en deux lignes (pour info)

La **loi 18-07** (équivalent algérien du RGPD) encadre les données personnelles, avec un régime renforcé pour les **données de santé**, sous le contrôle de l'**ANPDP**. Les sanctions (jusqu'à 6 M DZD) visent les manquements graves et de mauvaise foi — pas un pilote transparent et sécurisé. Tu n'es pas la cible de ce dispositif à ton stade.

## 6. Honnêteté

Je ne suis pas avocat : ceci t'aide à décider, ça ne te couvre pas juridiquement. Le risque résiduel d'un pilote **hébergé UE + médecin-responsable + transparent** est **faible**, mais non nul. La règle simple : avancer maintenant, formaliser avec un conseil local **quand l'enjeu le justifie**. Tu n'es pas bloqué — tu es en avance sur la plupart des projets à ce stade.

---

### Sources
- [Loi 18-07 — Conformité, amendes et démarches ANPDP (webminds.dz)](https://webminds.dz/blog/loi-1807.html)
- [Loi n° 18-07 : protection des données personnelles (GAAN)](https://members.gaan.dz/articles-divers/loi-n18-07--protection-des-donnees-personnelles-page-15279)
- [Guide conformité PDP 18-07 (HALKORB)](https://halkorb.com/blog/protection-donnees-pdp-18-07-algerie.html)
- [Protection des données personnelles en Algérie — généralités (Amad Advisory)](https://amadadvisory.com/35-protection-des-donnes-personnelles-en-algrie---gnralits-sur-la-loi-18-07/)
