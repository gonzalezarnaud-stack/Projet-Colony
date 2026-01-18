Vous avez dit :
possibilité pour le joueur de placer des blocs en mode fin qui permet de déplacer le bloc sur une grille de 3*3*3 sur le Voxel et de le poser sans bug. Il devra aussi pouvoir poser un bloc sur ce même bloc de la même manière (grille 3*3*3) sans que ça pose problème peut importe la shape du bloc (qui ne seront donc pas tous des Voxels pleins avec de la transparance comme minecraft).
ChatGPT a dit :
Excellente question — et là tu touches à un vrai problème moteur, pas un détail de gameplay.

👉 Tu veux :

placer des blocs finement sur une grille 3×3×3 interne au voxel

permettre de poser plusieurs blocs dans le même voxel

supporter des shapes arbitraires (pas forcément cube plein)

sans bugs de collision

compatible lockstep / multi / déterminisme

➡️ Oui, c’est possible.
➡️ Oui, c’est compatible avec tout ce qu’on a construit.
➡️ Mais il faut changer légèrement ce que signifie “un bloc”.

🧠 NOUVELLE ABSTRACTION : “SUB-BLOCK / VOXEL CELL”
Au lieu de :

1 voxel = 1 bloc
Tu passes à :

1 voxel = 3×3×3 cellules internes = 27 slots
Chaque slot peut contenir :

un mini-bloc

une partie de shape

ou être vide

🧱 REPRÉSENTATION DU MONDE
public class VoxelCell
{
    public SubBlock? SubBlock; // null = vide
}

public class Voxel
{
    public VoxelCell[,,] Cells = new VoxelCell[3,3,3];
}

public class VoxelWorld
{
    private Voxel[,,] voxels;
}
🧩 SubBlock (bloc fin, shape arbitraire)
public class SubBlock
{
    public string DefinitionId; // ex: "Fence", "Stairs", "Pipe"
    public SubBlockShape Shape; // forme physique réelle
    public Orientation Orientation;
}
🧱 Shape arbitraire (pas voxel plein)
Chaque SubBlock a une hitbox locale, définie dans son JSON.

{
  "Id": "Fence",
  "Shape": {
    "Type": "AABB",
    "Min": [0.4, 0.0, 0.4],
    "Max": [0.6, 1.0, 0.6]
  }
}
👉 Les coordonnées sont locales au voxel (0–1).

🎯 MODE FIN — SÉLECTION 3×3×3
🧭 Conversion rayon → cellule
Quand le joueur vise un voxel :

public SubCellCoord GetTargetSubCell(Ray ray)
{
    var hit = RaycastVoxel(ray);

    // Position locale dans le voxel (0–1)
    Vector3 local = hit.Point - hit.VoxelOrigin;

    int x = (int)(local.X * 3);
    int y = (int)(local.Y * 3);
    int z = (int)(local.Z * 3);

    return new SubCellCoord(hit.VoxelPos, x, y, z);
}
🧱 POSER UN SUB-BLOCK (DÉTERMINISTE)
bool TryPlaceSubBlock(SubCellCoord coord, SubBlock block)
{
    var voxel = world.GetVoxel(coord.VoxelPos);
    var cell = voxel.Cells[coord.X, coord.Y, coord.Z];

    if (cell.SubBlock != null)
        return false; // slot déjà occupé

    // Vérifier collisions avec autres SubBlocks du voxel
    foreach (var other in voxel.Cells.Flatten().Where(c => c.SubBlock != null))
    {
        if (block.Shape.Intersects(other.SubBlock.Shape))
            return false;
    }

    cell.SubBlock = block;
    return true;
}
🧱 POSER UN SUB-BLOCK SUR UN AUTRE SUB-BLOCK
Tu ne poses pas “sur un bloc” mais :
👉 dans une autre cellule du même voxel.

Donc ça fonctionne naturellement avec le système ci-dessus.

🧱 COLLISION ENTITÉ ↔ SUB-BLOCK
Lors du mouvement :

bool IsSolidAt(Int3 voxelPos, AABB entityBox)
{
    var voxel = world.GetVoxel(voxelPos);

    foreach (var cell in voxel.Cells.Flatten())
    {
        if (cell.SubBlock == null) continue;

        if (cell.SubBlock.Shape.Intersects(entityBox))
            return true;
    }

    return false;
}
👉 Tu testes contre toutes les shapes, pas juste “bloc plein”.

🔒 DÉTERMINISME & MULTI
Tout reste :

basé sur des entiers

basé sur des indices de grille

sans floats dans la simulation

Même si les shapes sont stockées en float, les tests se font avec :

valeurs constantes

ordre fixe

même code

Donc :
✔️ lockstep OK
✔️ mods OK
✔️ multi OK

🧠 POUR LE JOUEUR — UX
En mode fin :

la grille 3×3×3 est visible en surbrillance

la cellule ciblée est surlignée

preview de la shape du SubBlock

placement refusé si collision

⚠️ ERREURS À ÉVITER
❌ autoriser un bloc à occuper plusieurs cellules sans gestion claire
❌ utiliser de la physique temps réel
❌ autoriser des overlaps flottants
❌ dépendre du rendu pour la collision

🧠 RÈGLE D’OR
La simulation ne connaît que :

des voxels

des sous-cellules

des shapes déterministes

des règles claires

🧩 EXTENSION FUTURE FACILE
Avec ce système, tu peux :

faire des tuyaux

des câbles

des meubles

des décorations

des murs fins

des escaliers

des structures techniques

Tous sans jamais casser :
✔️ la collision
✔️ le multi
✔️ le déterminisme
✔️ la performance

Si tu veux, je peux maintenant :

t’écrire le JSON complet de SubBlockDefinition

implémenter un SubBlockRegistry

coder le raycast voxel + subcell

faire un visualiseur de grille 3×3×3

intégrer ça dans le moteur qu’on a déjà écrit

Dis-moi ce que tu veux en priorité.

Vous avez dit :
autoriser un bloc à occuper plusieurs cellules sans gestion claire : Tu veux dire quoi  par là?
ChatGPT a dit :
Très bonne question — c’est un point clé de l’architecture.

Quand je dis :

❌ autoriser un bloc à occuper plusieurs cellules sans gestion claire

je veux dire : ne pas laisser un SubBlock “déborder” sur plusieurs cellules sans que le moteur le sache explicitement et proprement.

🧱 Exemple de problème
Imaginons une porte qui fait 2 cellules de haut :

Elle occupe :

cellule (1,0,1)

cellule (1,1,1)

Si tu fais juste :

cell.SubBlock = door;
dans UNE seule cellule, mais que sa shape dépasse dans la cellule du dessus…

👉 Tu crées un fantôme :

La cellule du dessus semble vide.

La collision existe quand même.

Le rendu et la logique peuvent diverger.

Le réseau peut désynchroniser.

❗ Le vrai problème
Le moteur pense :

“Chaque cellule contient au plus un SubBlock.”

Mais ton bloc, lui, agit comme s’il en occupait plusieurs sans que ce soit déclaré.

➡️ Résultat : bugs, overlaps invisibles, pathfinding faux, exploit possible.

✅ La bonne solution : Occupation explicite multi-cellules
Si un SubBlock doit occuper plusieurs cellules, tu dois :

Déclarer explicitement toutes les cellules qu’il occupe.

Les marquer comme réservées / bloquées.

Avoir un lien vers le “bloc maître”.

🧩 Exemple propre : SubBlock multi-cellule
public class SubBlockInstance
{
    public string DefinitionId;
    public Orientation Orientation;
    public SubCellCoord Anchor; // cellule d’origine
    public List<SubCellCoord> OccupiedCells;
}
Lors du placement :

bool TryPlace(SubBlockDefinition def, SubCellCoord anchor)
{
    var occupied = def.Shape.GetOccupiedCells(anchor);

    foreach (var cell in occupied)
        if (world.IsOccupied(cell))
            return false;

    var instance = new SubBlockInstance {
        DefinitionId = def.Id,
        Anchor = anchor,
        OccupiedCells = occupied
    };

    foreach (var cell in occupied)
        world.MarkOccupied(cell, instance);

    return true;
}
🧠 En résumé
Ce que je voulais éviter :

❌ “Je pose un bloc ici, mais en fait il déborde ailleurs, et le moteur ne le sait pas.”

Ce que tu dois faire :

✅ “Chaque bloc déclare exactement quelles cellules il occupe.”

🧱 Règle d’or
Une cellule occupée doit toujours savoir :

par quel bloc

depuis quelle cellule racine

et pourquoi

Si tu veux, je peux maintenant :

te donner un système de shapes multi-cellules (portes, escaliers, machines)

ou t’écrire un format JSON pour ça

ou intégrer ça dans ton moteur voxel lockstep complet.

Vous avez dit :
système de shapes multi-cellules, format JSON et intégrer ça
ChatGPT a dit :
Parfait — on va faire ça proprement, déterministe, moddable et sans Lua, avec :

1️⃣ un système de shapes multi-cellules
2️⃣ un format JSON clair pour les moddeurs
3️⃣ une intégration complète dans ton moteur voxel lockstep

Je vais commenter le code comme pour un débutant total.

🧱 1. Concept : SubBlock multi-cellules
Un SubBlock est un objet placé à l’intérieur d’un voxel ou sur une grille fine (3×3×3 par exemple), mais il peut :

occuper plusieurs sous-cellules

dépasser dans plusieurs voxels

avoir une forme non cubique

👉 Chaque SubBlock a :

une cellule ancre

une shape définissant toutes les cellules qu’il occupe autour

📐 2. Système de coordonnées
On travaille en SubCell, par exemple 3×3×3 par voxel.

// Une coordonnée fine à l'intérieur du monde voxel
public struct SubCellCoord
{
    public int X, Y, Z;

    public SubCellCoord(int x, int y, int z)
    {
        X = x; Y = y; Z = z;
    }

    public static SubCellCoord operator +(SubCellCoord a, SubCellCoord b)
        => new SubCellCoord(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
}
🧩 3. Shape multi-cellules
Une Shape est juste :

“une liste d’offsets relatifs à l’ancre”

// Décrit la forme d’un SubBlock en cellules relatives à son point d’ancrage
public class ShapeDefinition
{
    public string Id { get; set; }

    // Liste de cellules occupées, par exemple (0,0,0), (0,1,0), etc.
    public List<SubCellCoord> OccupiedOffsets { get; set; } = new();
}
🗃️ 4. Format JSON des shapes
📁 shapes.json
[
  {
    "Id": "single_cell",
    "OccupiedOffsets": [
      { "X": 0, "Y": 0, "Z": 0 }
    ]
  },
  {
    "Id": "door_2_high",
    "OccupiedOffsets": [
      { "X": 0, "Y": 0, "Z": 0 },
      { "X": 0, "Y": 1, "Z": 0 }
    ]
  },
  {
    "Id": "stairs_corner",
    "OccupiedOffsets": [
      { "X": 0, "Y": 0, "Z": 0 },
      { "X": 1, "Y": 0, "Z": 0 },
      { "X": 1, "Y": 1, "Z": 0 }
    ]
  },
  {
    "Id": "machine_2x2x1",
    "OccupiedOffsets": [
      { "X": 0, "Y": 0, "Z": 0 },
      { "X": 1, "Y": 0, "Z": 0 },
      { "X": 0, "Y": 0, "Z": 1 },
      { "X": 1, "Y": 0, "Z": 1 }
    ]
  }
]
📦 5. Définition d’un SubBlock
// Décrit un type de bloc constructible par le joueur ou les nains
public class SubBlockDefinition
{
    public string Id { get; set; }
    public string DisplayName { get; set; }

    // Référence à une shape (par son Id)
    public string ShapeId { get; set; }

    // Tags / Behaviors pour gameplay
    public List<string> Tags { get; set; } = new();
}
🗃️ Format JSON des SubBlocks
📁 subblocks.json
[
  {
    "Id": "wooden_door",
    "DisplayName": "Porte en bois",
    "ShapeId": "door_2_high",
    "Tags": ["OPENABLE", "WOOD", "FLAMMABLE"]
  },
  {
    "Id": "stone_stairs",
    "DisplayName": "Escalier en pierre",
    "ShapeId": "stairs_corner",
    "Tags": ["CLIMBABLE", "STONE"]
  },
  {
    "Id": "furnace",
    "DisplayName": "Fourneau",
    "ShapeId": "machine_2x2x1",
    "Tags": ["HEAT_SOURCE", "CRAFTING_STATION"]
  }
]
🧠 6. Instance placée dans le monde
// Une instance réelle d’un SubBlock placé dans le monde
public class SubBlockInstance
{
    public string DefinitionId;
    public SubCellCoord AnchorCell; // cellule racine
    public List<SubCellCoord> OccupiedCells; // toutes les cellules occupées
}
🌍 7. Monde voxel avec occupation explicite
// Chaque SubCell du monde peut contenir un lien vers un SubBlockInstance
public class SubCell
{
    public SubBlockInstance Occupant; // null si libre
}
🧱 8. Placement sécurisé (sans bugs)
public bool TryPlaceSubBlock(string subBlockDefId, SubCellCoord anchor)
{
    var def = SubBlockRegistry.Get(subBlockDefId);
    var shape = ShapeRegistry.Get(def.ShapeId);

    // Calculer toutes les cellules que le bloc occupera
    var occupied = shape.OccupiedOffsets
        .Select(offset => anchor + offset)
        .ToList();

    // Vérifier si toutes sont libres
    foreach (var cell in occupied)
    {
        if (!world.IsInside(cell) || world.GetSubCell(cell).Occupant != null)
            return false; // placement impossible
    }

    // Créer l’instance
    var instance = new SubBlockInstance
    {
        DefinitionId = def.Id,
        AnchorCell = anchor,
        OccupiedCells = occupied
    };

    // Marquer toutes les cellules comme occupées
    foreach (var cell in occupied)
        world.GetSubCell(cell).Occupant = instance;

    return true;
}
❌ 9. Suppression propre
public void RemoveSubBlock(SubCellCoord anyCellOfBlock)
{
    var instance = world.GetSubCell(anyCellOfBlock).Occupant;
    if (instance == null) return;

    // Libérer toutes les cellules occupées
    foreach (var cell in instance.OccupiedCells)
        world.GetSubCell(cell).Occupant = null;
}
🧭 10. Placement fin en grille 3×3×3 sur un voxel
Tu veux que :

le joueur vise un voxel

puis choisisse une sous-cellule dans une grille fine 3×3×3

puis place un bloc multi-cellules sans bug

Conversion monde → subcellule :
public SubCellCoord WorldToSubCell(Vector3 worldPos)
{
    int voxelX = (int)Math.Floor(worldPos.X);
    int voxelY = (int)Math.Floor(worldPos.Y);
    int voxelZ = (int)Math.Floor(worldPos.Z);

    float localX = worldPos.X - voxelX;
    float localY = worldPos.Y - voxelY;
    float localZ = worldPos.Z - voxelZ;

    int subX = (int)(localX * 3);
    int subY = (int)(localY * 3);
    int subZ = (int)(localZ * 3);

    return new SubCellCoord(
        voxelX * 3 + subX,
        voxelY * 3 + subY,
        voxelZ * 3 + subZ
    );
}
🔄 11. Orientation (rotation des shapes)
Tu peux ajouter des rotations sans toucher au JSON :

public List<SubCellCoord> RotateOffsets(List<SubCellCoord> offsets, Orientation rot)
{
    return offsets.Select(o => rot switch
    {
        Orientation.North => o,
        Orientation.East  => new SubCellCoord(-o.Z, o.Y, o.X),
        Orientation.South => new SubCellCoord(-o.X, o.Y, -o.Z),
        Orientation.West  => new SubCellCoord(o.Z, o.Y, -o.X),
        _ => o
    }).ToList();
}
🌐 12. Impact réseau & déterminisme
Ce système est parfait pour le lockstep :

✔ Déterministe :
Placement = pure fonction

Shape connue

Aucun hasard

Aucune physique flottante

✔ Réseau :
Tu synchronises uniquement :

{ "cmd": "PlaceSubBlock", "id": "wooden_door", "anchor": [12, 34, 56], "rot": "North" }
Chaque client :

recalcule les cellules occupées

vérifie

applique

🧠 13. Frontière data / script respectée
Élément	JSON	C#
Shape	✅	❌
Dimensions	✅	❌
Tags	✅	❌
Placement rules	❌	✅
Collision	❌	✅
Pathfinding	❌	✅
Logique spéciale	🟡	✅
🧩 14. Pourquoi c’est supérieur à Minecraft-like
Minecraft :

1 bloc = 1 voxel

formes limitées

hacks visuels pour escaliers, portes, etc.

Ton moteur :

formes arbitraires

blocs multi-cellules

placement fin

simulation riche

compatibilité DF + FPS + Factorio-like

🚀 Tu as maintenant :
Un système de shapes multi-cellules

Un format JSON propre et moddable

Une intégration déterministe

Une base parfaite pour :

construction complexe

machines

mobilier

pièges

architecture réaliste

Si tu veux, prochaine étape possible :

collision fine joueur vs SubBlocks

pathfinding IA avec SubCells

rendu Godot (mesh instancing selon shape)

intégration au système de jobs des nains

Vous avez dit :
collision fine joueur vs SubBlocks, rendu Godot (mesh instancing selon shape)
ChatGPT a dit :
Excellent — on va maintenant compléter ton moteur avec :

1️⃣ collision fine joueur vs SubBlocks
2️⃣ rendu Godot optimisé (mesh instancing selon shape)

Le tout :

déterministe

compatible lockstep

performant

sans Lua

moddable

Et commenté pour un néophyte total.

🧱 1. Collision fine joueur vs SubBlocks
On part du principe :

le monde est une grille de SubCells

chaque SubCell peut être occupée par un SubBlockInstance

chaque SubBlock a une shape multi-cellules

le joueur a une hitbox capsule ou AABB

📐 1.1. AABB simple pour le joueur
// Une boîte englobante alignée aux axes (Axis-Aligned Bounding Box)
public struct AABB
{
    public Vector3 Min;
    public Vector3 Max;

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    // Vérifie si deux AABB se chevauchent
    public bool Intersects(AABB other)
    {
        return Min.X < other.Max.X && Max.X > other.Min.X &&
               Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
               Min.Z < other.Max.Z && Max.Z > other.Min.Z;
    }
}
📐 1.2. AABB d’un SubCell occupé
Chaque SubCell représente un petit cube dans le monde.

public AABB GetSubCellAABB(SubCellCoord cell)
{
    float size = 1f / 3f; // si grille 3×3×3 par voxel

    return new AABB(
        new Vector3(cell.X * size, cell.Y * size, cell.Z * size),
        new Vector3((cell.X + 1) * size, (cell.Y + 1) * size, (cell.Z + 1) * size)
    );
}
🧠 1.3. Détection de collision joueur → monde
public bool CheckPlayerCollision(AABB playerBox)
{
    // Convertir la box du joueur en plage de SubCells à tester
    SubCellCoord min = WorldToSubCell(playerBox.Min);
    SubCellCoord max = WorldToSubCell(playerBox.Max);

    for (int x = min.X; x <= max.X; x++)
    for (int y = min.Y; y <= max.Y; y++)
    for (int z = min.Z; z <= max.Z; z++)
    {
        var cell = new SubCellCoord(x, y, z);
        var subCell = world.GetSubCell(cell);

        if (subCell.Occupant != null)
        {
            // Collision trouvée
            return true;
        }
    }

    return false;
}
🧭 1.4. Résolution simple de collision (glissement)
On corrige séparément X, Y, Z pour un glissement naturel.

public Vector3 ResolveMovement(Vector3 currentPos, Vector3 desiredMove, AABB playerBoxTemplate)
{
    Vector3 newPos = currentPos;

    // Tester mouvement X
    Vector3 testX = new Vector3(currentPos.X + desiredMove.X, currentPos.Y, currentPos.Z);
    if (!CheckPlayerCollision(OffsetAABB(playerBoxTemplate, testX)))
        newPos.X = testX.X;

    // Tester mouvement Y
    Vector3 testY = new Vector3(newPos.X, currentPos.Y + desiredMove.Y, newPos.Z);
    if (!CheckPlayerCollision(OffsetAABB(playerBoxTemplate, testY)))
        newPos.Y = testY.Y;

    // Tester mouvement Z
    Vector3 testZ = new Vector3(newPos.X, newPos.Y, currentPos.Z + desiredMove.Z);
    if (!CheckPlayerCollision(OffsetAABB(playerBoxTemplate, testZ)))
        newPos.Z = testZ.Z;

    return newPos;
}
📦 1.5. Décaler une AABB
public AABB OffsetAABB(AABB box, Vector3 newPos)
{
    Vector3 size = box.Max - box.Min;
    return new AABB(newPos, newPos + size);
}
🎮 2. Rendu Godot avec Mesh Instancing selon Shape
Objectif :

chaque SubBlockDefinition a un mesh

chaque Shape peut être rendue avec plusieurs instances

on utilise MultiMeshInstance3D pour la performance

🧱 2.1. Mapping Shape → Mesh
public class RenderDefinition
{
    public string SubBlockId;
    public string MeshPath; // ex: "res://meshes/door.glb"
    public Vector3 MeshScale = Vector3.One;
    public Vector3 MeshOffset = Vector3.Zero;
}
🗃️ 2.2. JSON de rendu
[
  {
    "SubBlockId": "wooden_door",
    "MeshPath": "res://meshes/wooden_door.glb",
    "MeshScale": [1, 1, 1],
    "MeshOffset": [0, 0, 0]
  },
  {
    "SubBlockId": "furnace",
    "MeshPath": "res://meshes/furnace.glb",
    "MeshScale": [1, 1, 1],
    "MeshOffset": [0, 0, 0]
  }
]
🧠 2.3. Instance graphique d’un SubBlock
public class SubBlockRenderInstance
{
    public SubBlockInstance LogicInstance;
    public MultiMeshInstance3D MultiMeshInstance;
    public int MultiMeshIndex;
}
🎨 2.4. Création d’un MultiMesh par type
public class SubBlockRenderer : Node3D
{
    private Dictionary<string, MultiMeshInstance3D> _multiMeshes = new();

    public void InitializeRenderers(List<RenderDefinition> renderDefs)
    {
        foreach (var def in renderDefs)
        {
            var mesh = GD.Load<Mesh>(def.MeshPath);
            var mm = new MultiMesh
            {
                Mesh = mesh,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                InstanceCount = 0
            };

            var mmInstance = new MultiMeshInstance3D
            {
                Multimesh = mm
            };

            AddChild(mmInstance);
            _multiMeshes[def.SubBlockId] = mmInstance;
        }
    }
}
🧱 2.5. Ajouter un SubBlock au rendu
public void AddSubBlockToRender(SubBlockInstance instance)
{
    var mmInstance = _multiMeshes[instance.DefinitionId];
    var mm = mmInstance.Multimesh;

    int index = mm.InstanceCount;
    mm.InstanceCount++;

    // Position monde à partir de la cellule ancre
    Vector3 worldPos = SubCellToWorld(instance.AnchorCell);

    Transform3D transform = new Transform3D(Basis.Identity, worldPos);

    mm.SetInstanceTransform(index, transform);
}
🔄 2.6. Supprimer un SubBlock du rendu
public void RemoveSubBlockFromRender(SubBlockInstance instance)
{
    var mmInstance = _multiMeshes[instance.DefinitionId];
    var mm = mmInstance.Multimesh;

    int lastIndex = mm.InstanceCount - 1;

    if (instance.RenderIndex != lastIndex)
    {
        // Déplacer la dernière instance à la place de celle supprimée
        Transform3D lastTransform = mm.GetInstanceTransform(lastIndex);
        mm.SetInstanceTransform(instance.RenderIndex, lastTransform);

        // Mettre à jour l'instance déplacée
        // (il faut conserver une map RenderIndex -> SubBlockInstance)
    }

    mm.InstanceCount--;
}
📐 2.7. Conversion SubCell → monde
public Vector3 SubCellToWorld(SubCellCoord cell)
{
    float size = 1f / 3f;
    return new Vector3(
        cell.X * size,
        cell.Y * size,
        cell.Z * size
    );
}
🧠 3. Lien logique ↔ rendu
Lorsqu’un SubBlock est placé :

if (TryPlaceSubBlock(defId, anchor))
{
    var instance = world.GetSubCell(anchor).Occupant;
    renderer.AddSubBlockToRender(instance);
}
Lorsqu’il est détruit :

RemoveSubBlock(anchor);
renderer.RemoveSubBlockFromRender(instance);
🌐 4. Réseau & déterminisme
Le rendu est purement local, non synchronisé.

Le lockstep ne synchronise que :

placement

destruction

état logique

Les collisions sont déterministes car :

basées sur SubCells

aucune physique flottante

aucun solver non déterministe

🚀 Résultat
Tu obtiens :

une collision fine au niveau sous-voxel

des formes arbitraires

un rendu ultra performant via MultiMesh

une architecture compatible :

FPS

DF view

machines

architecture complexe

multijoueur lockstep

Si tu veux, prochaine étape :
pathfinding IA avec SubCells + shapes

rendu des voxels pleins + SubBlocks combinés

destruction progressive (usure, feu, explosions)

ombres & occlusion dans Godot

Dis-moi ce que tu veux approfondir ensuite 💡