# Prompt de reprise — ProjetColony

## État actuel du projet

MVP-A est **terminé**. MVP-B est **en cours** — formes de blocs, rotation, colliders adaptés et step climbing implémentés. Reste la sous-grille 4×4×4.

## Fichiers du projet

### Core/ (simulation pure, pas de Godot)
- `Core/World/Block.cs` — struct avec MaterialId, ShapeId, RotationId (ushort)
- `Core/World/Chunk.cs` — tableau 16×16×16 de blocs
- `Core/World/ChunkManager.cs` — dictionnaire de chunks
- `Core/World/World.cs` — médiateur central, gère conversions coordonnées
- `Core/World/TerrainGenerator.cs` — génération Simplex noise avec couches
- `Core/Data/Materials.cs` — constantes Air=0, Stone=1, Dirt=2, Grass=3
- `Core/Data/Shapes.cs` — constantes Full=0, Demi=1, FullSlope=2, DemiSlope=3, Post=4

### Engine/ (dépend de Godot)
- `Engine/Input/Player.cs` — mouvement, physique, casser/poser blocs, sélection matériau/forme/rotation, step climbing
- `Engine/Input/FreeCamera.cs` — caméra noclip pour debug
- `Engine/Rendering/BlockRenderer.cs` — création visuels de blocs avec formes, rotation et colliders adaptés
- `Engine/Rendering/BlockHighlight.cs` — cube semi-transparent sur bloc visé
- `Engine/UI/UiInHand.cs` — affiche matériau + forme + rotation

### Scenes/
- `Scenes/Main.cs` — point d'entrée, crée World, génère terrain, crée UI

## Ce qui fonctionne
- Génération terrain procédural (Simplex noise)
- Couches : herbe (surface) → terre (2 blocs) → pierre (profondeur)
- Marcher, sauter, gravité
- Casser blocs (clic gauche)
- Poser blocs (clic droit)
- Choisir matériau : 1=Pierre, 2=Terre, 3=Herbe
- Choisir forme : 4=Full, 5=Demi, 6=Post, 7=FullSlope, 8=DemiSlope
- Rotation des blocs (touche R) : 0°, 90°, 180°, 270°
- Highlight du bloc visé
- UI affichant matériau + forme + rotation
- Raycast exclut le joueur (bug fix)
- **Step climbing** : monte automatiquement les demi-blocs
- **Pentes** : marche fluidement sur FullSlope et DemiSlope (FloorMaxAngle = 50°)
- **Colliders adaptés** : Demi, FullSlope, DemiSlope, Post

## Architecture
```
Player → World (médiateur) → ChunkManager → Chunk → Block
Player → BlockRenderer (création visuels avec forme + rotation + collider)
Player → BlockHighlight (surbrillance)
Player → TryStepUp() (step climbing pour demi-blocs)
Main → UiInHand (affichage UI)
```

## Step Climbing (Player.cs)

Constantes :
- `_stepCheckLow = 0.4f` — hauteur du raycast bas (détecte obstacle)
- `_stepCheckHigh = 0.9f` — hauteur du raycast haut (détecte bloc plein)
- `_stepImpulse = 5.0f` — force du mini-saut

Logique `TryStepUp()` :
1. Vérifie qu'on est au sol et qu'on bouge
2. Vérifie qu'on n'est pas sur une pente (`floorNormal.Y < 0.95`)
3. Raycast bas : détecte un obstacle devant
4. Raycast haut : vérifie si c'est un bloc plein
5. Si obstacle ET pas bloc plein → impulsion vers le haut

## Colliders (BlockRenderer.cs)

- `Full` : BoxShape3D 1×1×1
- `Demi` : BoxShape3D 1×0.5×1, position Y=-0.25
- `Post` : BoxShape3D 0.25×1×0.25
- `FullSlope` : ConvexPolygonShape3D (6 vertices)
- `DemiSlope` : ConvexPolygonShape3D (6 vertices, Y max = 0)

## Prochaines étapes MVP-B
1. **Commenter le nouveau code** — TryStepUp(), colliders, constantes
2. **Sous-grille 4×4×4** — Placement fin des blocs
3. **Mode normal vs mode fin** — Bascule avec une touche
4. **Preview de pose** — Highlight montre la forme réelle

## Prochaines étapes MVP-C
1. **Vue gestion** — Caméra du dessus + couches
2. **Transition** — FPS ↔ gestion
3. **UN colon** — Pathfinding, besoins (faim/soif/sommeil), job basique
4. **Ressources** — Plantes, eau, stockage
5. **Craft basique** — Établi + quelques recettes
6. **Eau simple** — Coule vers le bas

## Décisions de design

### Craft
- Joueur + colons craftent
- Établi obligatoire (sauf survie basique)
- Établis : portatif basique → portatifs spécialisés → fixes (niv 1-3)
- Qualité = atelier + matériaux + compétence colon
- Joueur : craft instantané, qualité limitée
- Colons : file de commandes (style DF), peuvent atteindre Masterwork
- Ateliers définis par contenu de pièce (simplifié pour POC)

### Qualité des objets
- 5 niveaux : Médiocre, Normal, Bon, Excellent, Masterwork
- Joueur max = Excellent (avec atelier fixe niv 3)
- Colon expert + atelier niv 3 = Masterwork

## Bugs résolus cette session
- Raycast touchait le joueur → ajout `query.Exclude`
- Sautillement sur sol plat → raycast bas à 0.4f au lieu de 0.1f
- Step climbing sur bloc plein → raycast haut à 0.9f
- Blocage en haut des pentes → `FloorMaxAngle = 50°`
- Colliders cubiques sur pentes → ConvexPolygonShape3D

## Notes pédagogiques
- Arnaud apprend le C# et Godot, il écrit le code lui-même avec guidage
- Commentaires en français dans le code
- Expliquer le "pourquoi" des choix techniques
- Concepts maîtrisés : DRY, structs vs classes, tableaux, meshes 3D (vertices/indices), modulo, exclusion raycast, opérateur ternaire, ConvexPolygonShape3D

## À commenter (début prochaine session)
- `Player.cs` : constantes step climbing, `FloorMaxAngle`, `TryStepUp()`
- `BlockRenderer.cs` : `CreateSlopeCollider()`, `CreateDemiSlopeCollider()`, cas colliders
