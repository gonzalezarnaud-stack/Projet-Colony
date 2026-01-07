// ============================================================================
// SHAPES.CS — Définition des formes de blocs
// ============================================================================
// Ce fichier est dans Core/Data, donc il fait partie de CORE (simulation pure).
// AUCUNE dépendance à Godot.
//
// POURQUOI CE FICHIER ?
// Avant, tous les blocs étaient des cubes pleins.
// Maintenant, un bloc peut avoir différentes formes : demi-bloc, pente, poteau...
//
// Comme pour Materials.cs, on utilise des constantes nommées :
//   new Block{ShapeId = Shapes.Demi}   // Clair !
//   new Block{ShapeId = 1}             // C'est quoi 1 ?
//
// "static class" SIGNIFIE :
// - On ne peut pas faire "new Shapes()"
// - On accède directement aux membres : Shapes.Full
// - C'est comme un conteneur de constantes
//
// ÉVOLUTION FUTURE :
// - Charger depuis un fichier JSON (pour le modding)
// - Ajouter des propriétés : peut-on marcher dessus ? bloque la lumière ?
// - Sous-grille 4×4×4 pour placement fin
// ============================================================================

namespace ProjetColony.Core.Data;

public static class Shapes
{
    // ------------------------------------------------------------------------
    // LES FORMES DE BASE
    // ------------------------------------------------------------------------
    // "const" = constante, la valeur ne changera jamais
    // "ushort" = même type que ShapeId dans Block.cs (0 à 65535)
    
    // Bloc plein 1×1×1 — la forme par défaut
    public const ushort Full = 0;
    
    // Demi-bloc — moitié de hauteur (1×0.5×1)
    public const ushort Demi = 1;
    
    // Pente pleine — rampe de 1 bloc de haut
    public const ushort FullSlope = 2;
    
    // Pente demi — rampe de 0.5 bloc de haut (pas encore implémenté)
    public const ushort DemiSlope = 3;
    
    // Poteau — pilier fin (0.25×1×0.25)
    public const ushort Post = 4;
}