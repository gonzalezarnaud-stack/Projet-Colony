# Prompt de reprise — ProjetColony

## État actuel du projet

MVP-A est **terminé**. MVP-B est **en cours** — sous-grille 4×4×4 fonctionnelle avec mode fin.

## Fichiers du projet

### Core/ (simulation pure, pas de Godot)
- `Core/World/Block.cs` — struct avec MaterialId, ShapeId, RotationId, SubX, SubY, SubZ
- `Core/World/Chunk.cs` — tableau 16×16×16 de List<Block> (plusieurs blocs par voxel)
- `Core/World/ChunkManager.cs` — dictionnaire de chunks
- `Core/World/World.cs` — médiateur central, gère conversions coordonnées
- `Core/World/TerrainGenerator.cs` — génération Simplex noise avec couches
- `Core/Data/Materials.cs` — constantes Air=0, Stone=1, Dirt=2, Grass=3
- `Core/Data/Shapes.cs` — constantes Full=0, Demi=1, FullSlope=2, DemiSlope=3, Post=4

### Engine/ (dépend de Godot)
- `Engine/Input/Player.cs` — mouvement, physique, casser/poser blocs, mode fin, step climbing
- `Engine/Input/FreeCamera.cs` — caméra noclip pour debug
- `Engine/Rendering/BlockRenderer.cs` — création visuels avec formes, rotation, colliders, métadonnées
- `Engine/Rendering/BlockHighlight.cs` — preview semi-transparent du bloc à poser
- `Engine/UI/UiInHand.cs` — affiche matériau + forme + rotation

### Scenes/
- `Scenes/Main.cs` — point d'entrée, crée World, génère terrain, crée UI

## Ce qui fonctionne

### MVP-A (terminé)
- Génération terrain procédural (Simplex noise)
- Couches : herbe (surface) → terre (2 blocs) → pierre (profondeur)
- Marcher, sauter, gravité
- Casser blocs (clic gauche)
- Poser blocs (clic droit)
- Choisir matériau : 1=Pierre, 2=Terre, 3=Herbe
- Highlight du bloc visé

### MVP-B (en cours)
- Choisir forme : 4=Full, 5=Demi, 6=Post, 7=FullSlope, 8=DemiSlope
- Rotation des blocs (touche R) : 0°, 90°, 180°, 270°
- **Step climbing** : monte automatiquement les demi-blocs
- **Pentes** : marche fluidement sur FullSlope et DemiSlope (FloorMaxAngle = 50°)
- **Colliders adaptés** : Demi, FullSlope, DemiSlope, Post
- **Sous-grille 4×4×4** : placement fin des blocs
- **Mode fin (touche F)** : bascule entre placement normal et précis
- **Collage automatique** : les blocs se collent au bloc visé
- **Métadonnées** : SubX/Y/Z et ShapeId stockés sur StaticBody3D
- **Preview précis** : highlight montre la forme et la position exacte

## Architecture

```
Player → World (médiateur) → ChunkManager → Chunk → List<Block>
Player → BlockRenderer (création visuels avec métadonnées)
Player → BlockHighlight (preview avec forme/rotation)
Player → TryStepUp() (step climbing pour demi-blocs)
Main → UiInHand (affichage UI)
```

## Sous-grille 4×4×4

### Structure de données
- `Block.SubX`, `Block.SubY`, `Block.SubZ` : byte (0-4)
- Sub = 0 → centré (mode normal)
- Sub = 1,2,3,4 → position dans la sous-grille (mode fin)

### Formule d'offset
```csharp
offset = (Sub - 2.5f) * 0.25f
// Sub=1 → -0.375, Sub=2 → -0.125, Sub=3 → +0.125, Sub=4 → +0.375
```

### Métadonnées sur StaticBody3D
```csharp
staticBody.SetMeta("SubX", (int)block.SubX);
staticBody.SetMeta("SubY", (int)block.SubY);
staticBody.SetMeta("SubZ", (int)block.SubZ);
staticBody.SetMeta("ShapeId", (int)block.ShapeId);
```

### Logique de collage (mode fin)
1. Récupérer la sous-position du bloc visé via GetMeta()
2. Si bloc centré (Sub=0) → calculer depuis hitPosition
3. Sinon → partir de la sous-position du bloc visé
4. Décaler selon la normale (+1 ou -1)
5. Si dépassement (>4 ou <1) → passer au voxel adjacent

## Step Climbing (Player.cs)

Constantes :
- `_stepCheckLow = 0.4f` — hauteur du raycast bas
- `_stepCheckHigh = 0.9f` — hauteur du raycast haut
- `_stepImpulse = 5.0f` — force du mini-saut

## Colliders (BlockRenderer.cs)

- `Full` : BoxShape3D 1×1×1
- `Demi` : BoxShape3D 1×0.5×1, position Y=-0.25
- `Post` : BoxShape3D 0.25×1×0.25
- `FullSlope` : ConvexPolygonShape3D (6 vertices)
- `DemiSlope` : ConvexPolygonShape3D (6 vertices, Y max = 0)

## Touches

| Touche | Action |
|--------|--------|
| Z/Q/S/D | Déplacement |
| Espace | Sauter |
| Souris | Regarder |
| Clic gauche | Casser bloc |
| Clic droit | Poser bloc |
| 1/2/3 | Matériau (Pierre/Terre/Herbe) |
| 4/5/6/7/8 | Forme (Full/Demi/Post/FullSlope/DemiSlope) |
| R | Rotation (0°/90°/180°/270°) |
| F | Toggle mode fin |
| Echap | Libérer/capturer souris |

## Prochaines étapes MVP-B
1. **UI mode fin** — Afficher "Mode Fin" ou "Mode Normal" à l'écran
2. **Gestion SubY** — Placement vertical dans la sous-grille
3. **RemoveBlock** — Supprimer un bloc spécifique (pas tous les blocs du voxel)

## Prochaines étapes MVP-C
1. **Vue gestion** — Caméra du dessus + couches
2. **Transition** — FPS ↔ gestion
3. **UN colon** — Pathfinding, besoins (faim/soif/sommeil), job basique
4. **Ressources** — Plantes, eau, stockage
5. **Craft basique** — Établi + quelques recettes

## Bugs résolus cette session
- Poteaux qui sautent au voxel suivant → StaticBody au centre, mesh/collision décalés
- Highlight qui disparaît → else manquant et appel UpdateHighLight manquant
- Poteaux pas collés → métadonnées + logique de collage depuis bloc visé

## Notes pédagogiques
- Arnaud apprend le C# et Godot, il écrit le code lui-même avec guidage
- Commentaires en français dans le code
- Concepts maîtrisés : List<T>, métadonnées (SetMeta/GetMeta), sous-grille, ConvexPolygonShape3D