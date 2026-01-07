# Prompt de reprise — ProjetColony

## État actuel du projet

MVP-A est **terminé**. MVP-B est **en cours** — formes de blocs implémentées, reste la sous-grille 4×4×4.

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
- `Engine/Input/Player.cs` — mouvement, physique, casser/poser blocs, sélection matériau/forme/rotation
- `Engine/Input/FreeCamera.cs` — caméra noclip pour debug
- `Engine/Rendering/BlockRenderer.cs` — création visuels de blocs avec formes et rotation
- `Engine/Rendering/BlockHighlight.cs` — cube semi-transparent sur bloc visé
- `Engine/UI/UiInHand.cs` — affiche le matériau sélectionné

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
- Rotation des blocs (touche R)
- Highlight du bloc visé
- UI affichant le matériau sélectionné

## Architecture
```
Player → World (médiateur) → ChunkManager → Chunk → Block
Player → BlockRenderer (création visuels avec forme + rotation)
Player → BlockHighlight (surbrillance)
Main → UiInHand (affichage UI)
```

## Prochaines étapes MVP-B
1. **UI forme/rotation** — Afficher la forme et rotation sélectionnées
2. **Sous-grille 4×4×4** — Placement fin des blocs
3. **Mode normal vs mode fin** — Bascule avec une touche

## Améliorations techniques notées
- Step climbing (monter demi-blocs et pentes sans sauter)
- Normales pour les meshes custom (éclairage correct des pentes)
- Collisions adaptées aux formes (pentes, demi-blocs)
- Races avec tailles différentes (post MVP-C)

## Notes pédagogiques
- Arnaud apprend le C# et Godot, il écrit le code lui-même avec guidage
- Commentaires en français dans le code
- Expliquer le "pourquoi" des choix techniques
- Concepts maîtrisés : DRY, structs vs classes, tableaux, meshes 3D (vertices/indices), modulo