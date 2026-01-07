# Projet Colony — Document de Vision

## Le jeu en une phrase (version simple)

Un jeu où tu incarnes un personnage dans un monde généré aléatoirement. Tu récoltes, tu construis, tu attires des colons, et tu fais grandir ta colonie jusqu'à devenir une capitale.

## Le jeu en une phrase (version gamer)

Un Dwarf Fortress en voxel où tu joues un personnage.

## Les trois piliers

### Pilier 1 : Double vie

En tant que futur dirigeant de colonie et commence seul à explorer, récolter, fabriquer et construire pour attirer des colons. Puis tu pourra continuer à le faire en donnant des instructions aux colons pour faire prospérer la colonie et subvenir à leurs besoins.

En tant que personnage :
Tu explore, récolte, plante, fabrique, te bat, construit, commerce, mange (dort? et ca skip du temps?), bois, discute avec les colons (ce qu'ils veulent, ce qui les inquiète, des infos sur le monde).

En tant que gestionnaire :
Tu assigne les colons a des taches, des jobs, vois leurs besoins, leurs relations, regarder d'en haut ce de petit monde et prendre soin d'eux.

Transition :
Avec une touche. Comme un souverain il te faudra être à l'abris pour gérer la colonie car le temps continue de tourner.

Question ouverte :
La gestion de la faim, soif, sommeil devrait être mise en pause en mode gestionnaire?

### Pilier 2 : Monde vivant

Le monde n'est pas juste du terrain généré aléatoirement. Il a une histoire, des civilisations, des événements qui se sont passés avant que tu arrives. Il a vécu sans toi et pourrait vivre sans toi.

Ce qui existe quand tu arrives :
Suivant le temps passé avant ton arrivée: Villes, ruines, des routes, routes commerciales, des factions (souvent une seule ville/forteresse mais a voir), des alliances, des guerres, des objets magiques, artefacts, des monstres ou personnages importants, des modifications d'environnements (forets, marais, desert, rivières etc)

Le passé du monde :
La génération commence à la "création" du monde. et plus le temps passe et plus il s'est passé d'évènements. Tu ne pourra pas commencer à l'an 0 parce qu'il y aurait quasi rien donc les premiers débuts serait environs 500 ans après, que des villages, routes etc. aient eut le temps de se faire.

Le monde sans toi :
Le monde continue de vivre en parallèle. Des villes grandisses, d'autres finissent en ruines, de nouvelles alliances ou guerres débutent, de nouvelles routes et routes commerciales se font, les marchants et aventuriers voyagent a travers le monde, de nouveau objets magiques sont crées, de nouvelles personnalités influentes émergent ou disparaissent.

Univers :
(Univers Fantasy à la Dwarf Fortress. Technologie de l'âge de pierre jusqu'à la renaissance en gros mais avec des anachronismes à la Warhammer Fantasy)

Défi technique :
Simuler un monde entier vivant est une tâche ardue. Nous commencerons simple et augmenterons les possibilités au fur et à mesure.

### Pilier 3 : Construction expressive

Pouvoir créer comme on le souhaite sans que ce soit un logiciel 3D. Un pouvoir de création tout en restant simple.

Le problème Minecraft :
Dans Minecraft lorsque tu pose un bloc "escalier" c'est en fait un bloc complet mais avec de la transparence, pour une barrière, le première qu'on pose pourrait ressembler a un pied de table mais pareil c'est un bloc plein avec de la transparence. Des exemples comme ça il y en a énormément et pour faire de la création c'est très pénible et ça oblige à utiliser des subterfuges compliqué pour avoir quelque chose de joli. Ca manque un peu de finesse et de possibilités.

Les types de blocs :
Des demi blocs, des quarts (pourquoi pas), des "poteaux" ou "pied de chaise". La possibilité de les placer horizontalement ou verticalement de manière fine (sous-grille de 4*4*4?), des pentes (Bloc ou demi-bloc).

Le système de placement :
Un voxel pourrait potentiellement être divisé en 4*4*4. On pourrait avoir le mode normal de placement en Voxel par Voxel ou passer dans un mode plus fin nous permettant de placer les objets dans cette grille de 4*4*4. Ensuite chaque bloc, en mode finesse pourrait être tourné horizontalement et/ou verticalement de 90°.

L'objectif :
Créer librement sans que ce soit trop compliqué

Défi technique :
La sous-grille 4×4×4 sera complexe à implémenter.

## Références

### Ce que je prends de Dwarf Fortress
Génération du monde, gestion des colons, jobs, humeurs, besoins, types de pièces pour les colons, envies, relations, préférences, aptitudes, vue gestion, gestion des fluides, gestion de la gravité (ex: arbres qui tombent quand on les coupes), fabrications de mécanismes simple mais très profond.

### Ce que je prends de Minecraft
Voxels, vue FPS, minage, exploration, craft, construction, lumière, aminaux utiles, plantations.

### Ce que je prends d'ailleurs
Dark Souls pour le combat. Parchemins de sorts comme dans Baldur's gate. Dans un futur lointain de l'automatisation comme dans un satisfactory.

## Scope

### MVP-A : Le monde voxel de base

Objectif : Prouver que je peux me balader dans un monde voxel et interagir avec.

Le monde :
- Petite zone avec du relief
- Blocs de pierre
- Blocs de terre avec herbe sur les faces du dessus exposées au soleil
- Eau (sans gestion des fluides, juste un bloc visuel)

Le joueur :
- Marche, saute, s'accroupit
- Mode vol/noclip pour les tests

Interaction :
- Casser des blocs (sans outil, à la main)
- Placer des blocs (sans outil, à la main)

C'est terminé quand :

Requis :
- La zone se génère procéduralement avec des reliefs
- Les blocs de pierre sont généralement sous les blocs de terre
- Les blocs de terre avec la face du dessus exposée au soleil ont cette face herbeuse
- Des blocs d'eau sont présents dans le monde
- Le personnage se déplace, saute et s'accroupit
- Mode noclip : traverse les blocs, monte avec saut, descend avec accroupi
- Les blocs disparaissent quand on les casse
- Les blocs apparaissent quand on les place
- Un point au centre de l'écran sert de viseur

Bonus (peut être reporté) :
- L'herbe repousse après un certain temps sur les blocs de terre exposés
- L'eau uniquement dans des cuvettes ou lits de rivière logiques (pas en plein milieu d'un terrain plat)

### MVP-B : La construction fine

Objectif : Prouver que le système de sous-grille fonctionne et que la construction est plus expressive.

Les formes disponibles :
- Bloc plein
- Demi-bloc
- Quart de bloc
- Pente bloc plein
- Pente demi-bloc
- Poteau

Les modes :
- Mode normal : placement par voxel entier, rotation automatique selon position du joueur et endroit cliqué
- Mode fin : placement dans la sous-grille 4×4×4, rotation manuelle avec des touches
- Bascule entre les deux modes avec une touche

C'est terminé quand :

Requis :
Formes disponibles: Bloc Plein, Demi-Bloc, Pente Bloc Plein, Pente Demi-Bloc, Poteau. Bloc en transparence pour visualisation de la pose. Le placement par voxel entier fonctionne et le placement est logique selon position du joueur. Passage en mode fin fonctionne: legere surbrillance de la grille 4*4*4 prêt du joueur, rotation manuelle et pose au bon endroit (snap pour aider?)

Bonus (peut être reporté) :
Quart de bloc.

### MVP-C : La double vie

Objectif : Prouver que jouer un personnage ET gérer des colons fonctionne.

Arrivée des colons :
- Les colons marchent depuis le bord de la carte
- Leur arrivée se déclenche a un certain niveau de ressource

Vue gestion :
- Caméra du dessus
- Peut pivoter et légèrement s'incliner
- Vue par couches comme dans Dwarf Fortress
- Zoom et dézoom

Infos des colons :
- Nom / Prénom
- Faim / Soif / Fatigue
- Job assigné
- Jobs préférés (plus performant dans ceux-là)

Jobs de base :
- Mineur
- Bûcheron
- Agriculteur
- Transporteur

C'est terminé quand :

Requis :
Les colons arrivent bien. On peux les accepter ou non (fenetre de dialogue). La vue gestion s'applique. La caméra est placée au bon endroit en prenant notre personage comme centre au départ. on peut naviguer librement de haut en bas et de droite a gauhe. On peut la faire pivoter librement et l'incliner légèrement. La vie des différentes couches comme dans Dwarf Fortress fonctionne. Le zoom/dezoom fonctionne. Les colons ont un nom et un prénom, une statistique de faim, soif et fatigue qui fluctue suivant leurs actions (manger, boire, dormir). Si ces statistiques sont trop basses ils cherchent a remonter ces besoins en mangeant, buvant, dormant. On peut leur assigner un job. Les jobs de bases sont mineur, agriculteur, transporteur.

Bonus (peut être reporté) :
Jobs préférés. Job de bucheron.

### V1 : Première vraie version

Ce qui s'ajoute après le MVP :
Combat type Dark Souls, magie à base de parchemins (liste fixe), potions/plats spéciaux/effets magiques (liste fixe), génération du monde avec histoire, plus de jobs, gestion des fluides, gestion de la gravité (un bloc tout seul en l'air tombe, pas comme dans Minecraft), relations entre colons, animaux multiples et matériaux en provenant, gestion des mécanismes (barrages activables, ponts levis, trappes, pieges), armes a distances, une multitude de biomes, une multitudes de matériaux et de blocs, des chariots/charettes/charriot a mines avec rails, bateaux, gestion température, races peuplant le monde, monstres, création d'objets magiques (liste fixe), propagation du feu.

### Le Rêve : Un jour peut-être

Les idées folles pour le futur lointain :
Automatisation style Satisfactory, magie avancée (création ce sorts customisés), potions/plats spéciaux/effets magiques (créations de potion/plats customisés), gestoin des gazs, création d'objets magiques avancée (customisés) et plus de tout.