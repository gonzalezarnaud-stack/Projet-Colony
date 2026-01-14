// ============================================================================
// MATERIALDEFINITION.CS — Définition d'un matériau
// ============================================================================
// Ce fichier est dans Core/Data/Definitions, donc il fait partie de CORE.
// AUCUNE dépendance à Godot.
//
// POURQUOI CETTE CLASSE ?
// Avant, les couleurs étaient codées en dur dans BlockRenderer.cs.
// Maintenant, chaque matériau est décrit par un objet MaterialDefinition
// chargé depuis un fichier JSON.
//
// SIMULATION DWARF FORTRESS :
// Cette classe prévoit les propriétés pour une simulation physique réaliste :
// - États de la matière (solide, liquide, gaz)
// - Transitions de phase (fonte, ébullition, gel)
// - Propriétés thermiques (conductivité, chaleur spécifique)
// - Combustion (inflammabilité, point d'ignition)
//
// Ces propriétés ne sont pas utilisées maintenant mais sont prêtes
// pour quand on implémentera la simulation.
// ============================================================================

namespace ProjetColony.Core.Data.Definitions;

public class MaterialDefinition
{
    // ------------------------------------------------------------------------
    // IDENTITÉ
    // ------------------------------------------------------------------------
    public ushort Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }

    // ------------------------------------------------------------------------
    // APPARENCE
    // ------------------------------------------------------------------------
    // Couleur RGBA (0 à 1). ColorA = transparence.
    // Texture : chemin vers le fichier texture (futur)
    public float ColorR { get; set; }
    public float ColorG { get; set; }
    public float ColorB { get; set; }
    public float ColorA { get; set; }
    public string Texture { get; set; }

    // ------------------------------------------------------------------------
    // ÉTAT DE LA MATIÈRE
    // ------------------------------------------------------------------------
    // Un matériau est dans UN SEUL état à la fois.
    // L'air est un gaz, l'eau est un liquide, la pierre est solide.
    public bool IsSolid { get; set; }
    public bool IsLiquid { get; set; }
    public bool IsGas { get; set; }
    public bool IsFlammable { get; set; }

    // ------------------------------------------------------------------------
    // PROPRIÉTÉS PHYSIQUES
    // ------------------------------------------------------------------------
    // Density : kg/m³ (eau = 1000, pierre ≈ 2700, air ≈ 1.2)
    //           Utilisé pour : flottaison, pression, écoulement
    // Hardness : résistance au minage (0 = instantané, 3 = pierre)
    // MiningLevel : niveau d'outil requis (0 = main, 1 = pioche pierre...)
    public float Density { get; set; }
    public int Hardness { get; set; }
    public int MiningLevel { get; set; }

    // ------------------------------------------------------------------------
    // PROPRIÉTÉS THERMIQUES
    // ------------------------------------------------------------------------
    // ThermalConductivity : W/(m·K) — vitesse de propagation de la chaleur
    //                       Métal = élevé, bois = faible
    // SpecificHeat : kJ/(kg·K) — énergie pour chauffer 1kg de 1°C
    //                Eau = 4.18 (très élevé), pierre ≈ 0.84
    // MeltingPoint : °C où le solide devient liquide (null si non applicable)
    // BoilingPoint : °C où le liquide devient gaz
    // IgnitionPoint : °C où le matériau prend feu (null si non inflammable)
    public float ThermalConductivity { get; set; }
    public float SpecificHeat { get; set; }
    public float? MeltingPoint { get; set; }
    public float? BoilingPoint { get; set; }
    public float? IgnitionPoint { get; set; }

    // ------------------------------------------------------------------------
    // TRANSITIONS DE PHASE
    // ------------------------------------------------------------------------
    // Nom du matériau résultant quand on change d'état.
    // Exemple : "Water" → StateWhenFrozen = "Ice"
    //           "Ice" → StateWhenMelted = "Water"
    //           "Stone" → StateWhenMelted = "Magma"
    // null = pas de transition (le matériau disparaît ou reste)
    public string StateWhenMelted { get; set; }
    public string StateWhenBoiled { get; set; }
    public string StateWhenCooled { get; set; }
    public string StateWhenFrozen { get; set; }
    public string BurnsInto { get; set; }

    // ------------------------------------------------------------------------
    // ÉMISSION (pour plus tard)
    // ------------------------------------------------------------------------
    // Certains matériaux émettent de la lumière ou de la chaleur.
    // Exemple : magma, torches, feu
    public bool EmitsLight { get; set; }
    public int LightLevel { get; set; }
    public bool EmitsHeat { get; set; }
    public float HeatLevel { get; set; }
}
