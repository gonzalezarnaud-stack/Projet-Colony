# Projet Colony — Document Technique

## Concepts de base

### La Game Loop

C'est ce que l'ordinateur fait en boucle tant que le jeu tourne :

1. INPUT — Lire ce que fait le joueur
2. UPDATE — Calculer ce qui change dans le jeu
3. RENDER — Dessiner le résultat à l'écran

Cette boucle tourne généralement 60 fois par seconde (60 FPS).

### Simulation vs Rendu

La simulation c'est tout les calculs que fait l'ordinateur en temps réel. Le rendu c'est uniquement ce qui est visible à l'écran.

### Pourquoi séparer le code en fichiers

Pour éviter d'avoir à débugguer un fichier avec 40k lignes de codes. Pour bien séparer les fonctions de chaque code. Eviter des copier-coller en utilisant du code provenant d'autres fichiers.

## Architecture

### Le principe : Core et Engine

On sépare le code en deux grandes parties :

**Core (simulation)**
Core contient tout les calculs qui devront etre effectués afin que la simulation fonctionne. C'est elle qui décrit ce qui se passe, tout ce qui existe.

**Engine (rendu)**
Engine s'occupe de tout ce qui doit être affiché à l'écran.

### Pourquoi cette séparation ?

La séparation permet de pouvoir changer le moteur du jeu si besoin et permet aussi de tester la simulation sans avoir besoin de lancer le jeu.

### Exemple

Quand un colon a faim :
- Core : Le colon a 15/100 en faim. Colon cherche a manger (pathfinding).
- Engine : La barre de faim affiche 15/100, une émoticon montre qu'il est pas content, il se déplace à l'écran pour charcher à manger.

## Structure des dossiers
ProjetColony/
│
├── Documents/
│   ├── VISION.md
│   └── TECHNIQUE.md
│
├── Core/
│   ├── World/
│   ├── Entities/
│   ├── Systems/
│   └── Data/
│
├── Engine/
│   ├── Rendering/
│   ├── Input/
│   ├── UI/
│   └── Audio/
│
├── Scenes/
│
└── Resources/

### Core/ — La simulation

**World/** — Génération du monde en Voxel

**Entities/** — Les PNJ/Monstres/Animaux, objets, fleches/carreaux/boulets de canon/sorts, Plantes/Arbres

**Systems/** — Toute la logique: systeme de faim/soif/someil/relations/envies, pathfinding, systeme de job

**Data/** — Les définitions: Types de blocs, Formes de blocs, Matérieux, Minéraux, Plantes/Arbres

### Engine/ — Le rendu

**Rendering/** — Tout ce qui est affichage des blocs et Entities

**Input/** — Code pour gérer le clavier/souris ou la manette. Gère les controleurs.

**UI/** — Affichage des différents menus, des barres de vie/faim/soif/sommeil. C'est l'interface utilisateur.

**Audio/** — Gère les sons et les musiques.

Plantes/Arbres — Entities ou Data ?
Les deux, en fait :
Data: La définition : "Un chêne a tel sprite, pousse en 10 jours, donne du bois de chêne
Entities: L'instance : "Ce chêne précis, à la position (45, 12, 8), planté il y a 3 jours"
Data = le modèle, la recette.
Entities = l'objet réel dans le monde.

## Ordre d'implémentation — MVP-A

On construit de bas en haut : d'abord les fondations, ensuite ce qui repose dessus.

### Étape 1 : Structure Block
Ce qu'on code : La définition d'un bloc (type, forme, rotation)
Ce qu'on voit : Rien encore
Pourquoi d'abord : C'est la brique élémentaire de tout le monde

### Étape 2 : Structure Chunk
Ce qu'on code : Un conteneur de 16×16×16 blocs
Ce qu'on voit : Rien encore
Pourquoi d'abord : On ne peut pas gérer des millions de blocs un par un

### Étape 3 : Afficher un chunk
Ce qu'on code : Transformer les données du chunk en image 3D
Ce qu'on voit : Des cubes à l'écran
Pourquoi maintenant : Première preuve visuelle que ça marche

### Étape 4 : Caméra libre
Ce qu'on code : Mouvement d'une caméra avec clavier/souris ou manette
Ce qu'on voit : Le monde 3D bouge en fonction des touches/boutons appuyés
Pourquoi maintenant : Vérifier que les contoles répondent correctements

### Étape 5 : Plusieurs chunks
Ce qu'on code : Un gestionnaire de chunks (ChunkManager) qui gère plusieurs chunks
Ce qu'on voit : Un monde plus grand que 16×16×16 blocs
Pourquoi maintenant : Un seul chunk c'est trop petit pour tester quoi que ce soit

### Étape 6 : Génération terrain
Ce qu'on code : La génération procédurale du terrain
Ce qu'on voit : Des collines, des creux, pas juste du plat
Pourquoi maintenant : On a besoin d'un vrai terrain pour tester la marche

### Étape 7 : Joueur qui marche
Ce qu'on code : La gravité, les collisions avec le sol, le saut
Ce qu'on voit : Un personnage qui marche sur le terrain, pas à travers
Pourquoi maintenant : On a enfin un terrain sur lequel marcher

### Étape 8 : Casser/Poser blocs
Ce qu'on code : Le joueur peut effacer des blocs ou en faire apparaitre
Ce qu'on voit : Des blocs qui disparaissent ou apparaissent
Pourquoi maintenant : Parce que c'est la base du jeu