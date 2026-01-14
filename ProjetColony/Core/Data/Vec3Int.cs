// ============================================================================
// VEC3INT.CS — Vecteur 3D avec des entiers
// ============================================================================
// Ce fichier est dans Core/Data, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// C'EST QUOI UN VECTEUR ?
// Un vecteur 3D regroupe trois valeurs : X, Y, Z.
// Il peut représenter :
//   - Une POSITION : "le bloc est à (5, 10, 3)"
//   - Une DIRECTION : "aller vers (1, 0, 0)" = vers la droite
//   - Une TAILLE : "le chunk fait (16, 16, 16) blocs"
//
// POURQUOI INT (ENTIER) ?
// Les positions de blocs sont toujours des nombres entiers.
// Un bloc est à (5, 10, 3), jamais à (5.5, 10.2, 3.7).
// Utiliser des entiers :
//   - Évite les erreurs d'arrondi
//   - Permet des comparaisons exactes (5 == 5, pas 5.0000001 ≈ 5)
//   - Prend moins de mémoire
//
// POURQUOI PAS GODOT.VECTOR3I ?
// Godot a son propre Vector3I, mais il fait partie de ENGINE.
// Core ne doit PAS dépendre de Godot.
// Comme ça, on peut tester Core sans lancer le moteur de jeu.
// Et si on change de moteur un jour, Core reste intact.
//
// UTILISATION :
//   var position = new Vec3Int(5, 10, 3);
//   int x = position.X;  // 5
// ============================================================================

namespace ProjetColony.Core.Data;

public struct Vec3Int
{
    // Les trois composantes du vecteur
    public int X;
    public int Y;
    public int Z;

    // ------------------------------------------------------------------------
    // CONSTRUCTEUR — Crée un vecteur avec les valeurs données
    // ------------------------------------------------------------------------
    // PARAMÈTRES :
    //   x, y, z = les trois composantes (minuscules = paramètres)
    //
    // Le constructeur assigne ces valeurs aux champs X, Y, Z (majuscules).
    //
    // EXEMPLE :
    //   var pos = new Vec3Int(5, 10, 3);
    //   // pos.X = 5, pos.Y = 10, pos.Z = 3
    public Vec3Int(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    // ------------------------------------------------------------------------
    // CONSTANTE : ZERO
    // ------------------------------------------------------------------------
    // Vecteur avec toutes les composantes à 0.
    // Représente souvent l'origine ou "pas de déplacement".
    //
    // "static readonly" = une seule instance partagée, jamais modifiée.
    // Pas "const" car les struct ne peuvent pas être const.
    //
    // EXEMPLE :
    //   var origine = Vec3Int.Zero;  // (0, 0, 0)
    public static readonly Vec3Int Zero = new Vec3Int(0, 0, 0);

    // ------------------------------------------------------------------------
    // TOSTRING — Convertit le vecteur en texte lisible
    // ------------------------------------------------------------------------
    // Surcharge la méthode ToString() héritée de Object.
    // Très utile pour le débogage : on voit directement les valeurs.
    //
    // EXEMPLE :
    //   var pos = new Vec3Int(5, 10, 3);
    //   Console.WriteLine(pos);  // Affiche : (5, 10, 3)
    //
    // Sans cette méthode, ça afficherait :
    //   ProjetColony.Core.Data.Vec3Int
    // (le nom du type, pas les valeurs)
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}