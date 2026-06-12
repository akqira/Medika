# login page
- Nombre de medecins a retirer, car pas beaucoup de clients pour l'instant, un nouveau médecin va se dire, oula je suis le premier ou parmi les premiers, c pas rassurant
- J'ai besoin de conditions générales:  aide moi a en générer, pose moi ce qu'il faut comme questions
- J'ai besoin de politque de confidentialité:  aide moi a en générer, pose moi ce qu'il faut comme questions
- Mot de passe oublié, je dois tester ça, dis moi si je dois préparer qlq chose (mail, data...)
- champs mot de passe  : icon oeil des deux cotés : pas logique

# Dashboard
- Sur la tuile "Patients aujourd'hui" il y a un 8% qui n'est pas vrai du coup, faut retirer
- les médecins algériens n'utilisent pas trop l'outil en temps réel, pas tous en tout cas, les deux tuiles restant à voir et en cours ne sont pas utiles pour moi, je n'ai pas d'idée sur quoi mettre, aide moi si t'en as toi
- on peut virer aussi total patients, je préfère mettre une icone a coté de "Patients" du navbar avec le nb total
- Programme du jour : Ok, on vire juste Agenda complet
- Patients récents : parfait

# Agenda
- L'affichage n'est pas top, trop zoomé, la navigation pas trop fluide non plus ni intuitive. il manque une action pour revenir à "Aujourd'hui"
- Nouveau rdv dupliquée, un bouton en haut et un autre en bas
- Affiche la semaine mais garde une vue sur toute la journée, pas une intervalle horaire de 6h, plutot 12h visible sur le meme écran
- Créer une Feature pour l'agenda car beaucoup de travail a venir (roadmap)

# patients
- Quand on focus l'input recherche sur la sidebard (pas la nvabar) : il faut permettre de naviguer dans les résultats en utilisant le clavier, fléche haute, fleche basse, boutton entrée, c plus rapide
- Pour des raisons de performances, il faut s'assurer que la liste des patients ne soit pas chargée en totalité au début, donc plutot du infinite scoll, ou infinte load; si on lance une recherche même chose, on limite le nombre de données affichées d'un coup, je te laisse me conseiller sachant que le client est algérien, connexion pas trop haut débit par endroits, donc faut s'adapter ici :)
- Informations patient : TROP pas joli, des tuiles comme ça, ça ne ressemble pas à un dossier patient, plutot à une fiche produit. J'ai rajouté un PDF C:\Users\orite\source\repos\OriteK\Medika\docs\Kaki\images\design-medical.pdf je te demande de t'en inspirer ainsi que les photos qui sont dans le meme répértoire que le PDF
- Consultations (dans la fiche patient) : Il faut un bouton : Ajouter consultation depuis le dossier patient, si on clique, une nouvelle consultation est affichée avec le patient déjà sélectionné
- Quand on clique sur une consultation existante, il faut afficher les infos que le médecin a saisi :
Constantes vitales
Anamnèse
Notes complémentaires
Diagnostic et honoraires
- une ordonnance doit pouvoir être imprimée depuis la fiche patient -> Consultation -> details de la consultation (on vois l'ordonnance mais on peut pas lancer impression ou l'ouvrir)
- je ne vois pas l'utilité de la fiche complète patient, a toi de me convaincre
- Facturation ok, il faut juste s'assurer que ça fonctionne a 100%, pour le mode paiement en algérie c que Espéce ;)
- POUR PLUS TARD : à l'encaissement, fais en sorte qu'on puisse encaisser une partie de la somme, donc il nous faut une gestion des versements, 
- le bouton TEL qui appelle le patient a virer, garde le numero visible mais sans aucune action (dans le nouveau design que tu vas refaire)

# Consultations
- Le dropdown patient prend top de place pour rien, meme si le patient est selected, ça prend tjrs de la place. 
- Constantes vitales : aussi, on perds beaucoup d'espace pour des champs qui ne prennent que 5 ou 6 caractères max, adjuste ça
- Anamnèse, Examen, le reste ça va pour l'instant
- Ordonnance à ajouter : 
    - Nom du médicament : on dois avoir une liste, et le médecin pour rajouter, d'où on peut ramener cette liste d'ailleurs ? les médocs algériens ???
    - Posologie sera inscrit sur le nom du médicament pour gagner place,
    - Fréquence c'est quoi ? Si ça veut dire jours, semaines, heures laisse tomber, c ambigus, soyons plus brefs:  les médecins aiment écrire vite : pas trop de clics souris et encore moins basculer entre clavier souris sans arrêt, donc soyons brefs; je te laisse proposer

# Finances
Tu n'as rien proposé, pose des questions pour qu'on vois ce qu'on y mets
