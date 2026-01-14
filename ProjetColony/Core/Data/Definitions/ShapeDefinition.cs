// ============================================================================
// SHAPEDEFINITION.CS — Définition d'une forme de bloc
// ============================================================================
// Ce fichier est dans Core/Data/Definitions, donc il fait partie de CORE.
// AUCUNE dépendance à Godot.
//
// POURQUOI CETTE CLASSE ?
// Avant, les propriétés des formes étaient codées en dur dans Player.cs
// et BlockRenderer.cs. Impossible à modifier sans recompiler.
//
// Maintenant, chaque forme est décrite par un objet ShapeDefinition
// chargé depuis un fichier JSON. Les moddeurs peuvent ajouter des formes
// sans toucher au code.
//
// PROPRIÉTÉS :
// - Géométrie : taille, décalage, type de mesh
// - Collision : type de collision, angle de marche
// - Comportement : symétrie, step climbing, sous-grille
// - Simulation : bloque lumière, fluides, gaz
// ============================================================================

namespace ProjetColony.Core.Data.Definitions;

public class ShapeDefinition
{
    // ------------------------------------------------------------------------
    // IDENTITÉ
    // ------------------------------------------------------------------------
    // Id : identifiant unique, utilisé dans Block.ShapeId
    // Name : nom technique (utilisé dans le code)
    // DisplayName : nom affiché au joueur (peut être traduit)
    public ushort Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }

    // ------------------------------------------------------------------------
    // GÉOMÉTRIE
    // ------------------------------------------------------------------------
    // Dimensions du mesh en unités (1 = un bloc standard)
    // OffsetY : décalage vertical (ex: -0.25 pour demi-bloc posé en bas)
    // MeshType : "box", "slope", "demiSlope", ou "custom" (futur)
    public float SizeX { get; set; }
    public float SizeY { get; set; }
    public float SizeZ { get; set; }
    public float OffsetY { get; set; }
    public string MeshType { get; set; }

    // ------------------------------------------------------------------------
    // COLLISION
    // ------------------------------------------------------------------------
    // CollisionType : "box" ou "convex" (pour les formes non-cubiques)
    // WalkableAngle : angle max pour marcher dessus (0 = mur, 45 = pente)
    public string CollisionType { get; set; }
    public int WalkableAngle { get; set; }

    // ------------------------------------------------------------------------
    // COMPORTEMENT
    // ------------------------------------------------------------------------
    // IsSymmetric : peut se coller sol/plafond quand tourné horizontalement
    //               (ex: un poteau devient une poutre)
    // IsClimbable : le joueur peut monter automatiquement (step climbing)
    // CanStackInVoxel : plusieurs blocs de cette forme dans un voxel
    // SubGridSize : taille dans la sous-grille (1 = tout le voxel, 0.25 = poteau)
    public bool IsSymmetric { get; set; }
    public bool IsClimbable { get; set; }
    public bool CanStackInVoxel { get; set; }
    public float SubGridSize { get; set; }

    // ------------------------------------------------------------------------
    // SIMULATION (pour plus tard)
    // ------------------------------------------------------------------------
    // BlocksLight : bloque la propagation de la lumière
    // BlocksFluid : bloque les liquides (eau, magma)
    // BlocksGas : bloque les gaz (fumée, vapeur)
    public bool BlocksLight { get; set; }
    public bool BlocksFluid { get; set; }
    public bool BlocksGas { get; set; }
}
