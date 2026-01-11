// ============================================================================
// BLOCK.CS — La brique élémentaire du monde
// ============================================================================
// Un Block représente une case dans la grille 3D du monde.
// C'est la plus petite unité : pierre, terre, air, eau...
//
// POURQUOI UNE STRUCT ET PAS UNE CLASS ?
// - struct = données stockées "par valeur" (copie les données)
// - class = données stockées "par référence" (copie un pointeur)
// On utilise struct parce que :
//   1. Un Block est simple (juste quelques nombres)
//   2. Il y en aura des millions en mémoire
//   3. Les structs sont plus rapides pour les petites données
//
// POURQUOI PAS DE "using Godot" ?
// Ce fichier est dans Core. Core = simulation pure.
// Aucune dépendance au moteur de jeu. Si on change de moteur, ce code reste.
// ============================================================================

namespace ProjetColony.Core.World;

public struct Block
{
    // ------------------------------------------------------------------------
    // LE MATÉRIAU
    // ------------------------------------------------------------------------
    // L'identifiant du matériau de ce bloc.
    // Type: ushort = nombre entier de 0 à 65535 (assez pour des milliers de matériaux)
    // Valeur 0 = air (vide, pas de bloc)
    // Valeur 1+ = matériaux solides (pierre, terre, bois, etc.)
    // Voir Materials.cs pour les constantes nommées.
    public ushort MaterialId;
    
    // ------------------------------------------------------------------------
    // LA FORME
    // ------------------------------------------------------------------------
    // L'identifiant de la forme de ce bloc.
    // Valeur 0 = bloc plein (Full) — la forme par défaut
    // Valeur 1+ = autres formes (demi-bloc, pente, poteau, etc.)
    // Voir Shapes.cs pour les constantes nommées.
    //
    // Note : par défaut, ShapeId = 0 (Full) car les struct sont initialisées à zéro.
    public ushort ShapeId;

    // ------------------------------------------------------------------------
    // LA ROTATION
    // ------------------------------------------------------------------------
    // L'orientation du bloc (0 à 3 = 0°, 90°, 180°, 270°).
    // Utile pour les pentes, escaliers, et autres blocs directionnels.
    //   0 = face vers +Z (avant, par défaut)
    //   1 = face vers +X (droite)
    //   2 = face vers -Z (arrière)
    //   3 = face vers -X (gauche)
    //
    // Note : par défaut, RotationId = 0 car les struct sont initialisées à zéro.
    public ushort RotationId;

    // ------------------------------------------------------------------------
    // LA SOUS-POSITION (Sous-grille 4×4×4)
    // ------------------------------------------------------------------------
    // Position du bloc à l'intérieur du voxel.
    // Chaque voxel est divisé en 4×4×4 = 64 sous-positions possibles.
    // Valeurs : 0, 1, 2, ou 3 pour chaque axe.
    //
    // Exemple : un poteau en SubX=0, SubY=0, SubZ=0 sera dans le coin
    // bas-arrière-gauche du voxel. En SubX=3, SubY=0, SubZ=3, il sera
    // dans le coin bas-avant-droit.
    //
    // Type byte = 0 à 255, stocké sur 1 octet (économie de mémoire).
    // Par défaut, tout est à 0 (coin bas-arrière-gauche).
    public byte SubX;
    public byte SubY;
    public byte SubZ;
}