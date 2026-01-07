// ============================================================================
// MATERIALS.CS — Définition des types de matériaux
// ============================================================================
// Ce fichier est dans Core/Data, donc il fait partie de CORE (simulation pure).
// AUCUNE dépendance à Godot.
//
// POURQUOI CE FICHIER ?
// Avant, on écrivait directement :
//   new Block{MaterialId = 1}   // C'est quoi 1 ? Pierre ? Terre ?
//
// Maintenant, on écrit :
//   new Block{MaterialId = Materials.Stone}   // Clair !
//
// AVANTAGES :
// - Code plus lisible
// - Si on change l'ID de la pierre, on le fait ici, pas partout
// - Le compilateur vérifie les fautes de frappe
//   (Materials.Ston → erreur, "1" → pas d'erreur même si c'est faux)
//
// "static class" SIGNIFIE :
// - On ne peut pas faire "new Materials()"
// - On accède directement aux membres : Materials.Stone
// - C'est comme un conteneur de constantes
//
// ÉVOLUTION FUTURE :
// - Charger depuis un fichier JSON (pour le modding)
// - Ajouter des propriétés : dureté, inflammabilité, couleur...
// - Créer une vraie classe Material avec des méthodes
// ============================================================================

namespace ProjetColony.Core.Data;

public static class Materials
{
    // ------------------------------------------------------------------------
    // LES MATÉRIAUX DE BASE
    // ------------------------------------------------------------------------
    // "const" = constante, la valeur ne changera jamais
    // "ushort" = même type que MaterialId dans Block.cs (0 à 65535)
    
    // Air = vide, pas de bloc. Toujours 0.
    public const ushort Air = 0;
    
    // Pierre = roche solide, base du terrain
    public const ushort Stone = 1;
    
    // Terre = sol meuble, sous l'herbe
    public const ushort Dirt = 2;
    
    // Herbe = terre avec végétation sur le dessus (face exposée au soleil)
    public const ushort Grass = 3;
}