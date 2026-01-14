// ============================================================================
// VEC3FLOAT.CS — Vecteur 3D avec des flottants
// ============================================================================
// Ce fichier est dans Core/Data, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// POURQUOI FLOAT (FLOTTANT) ?
// Certaines valeurs ne sont pas des entiers :
//   - Les OFFSETS : décalage de 0.33 mètre dans la sous-grille
//   - Les NORMALES : direction (0.707, 0.707, 0) pour une diagonale
//   - Les POSITIONS PRÉCISES : le joueur est à (5.2, 10.0, 3.8)
//
// DIFFÉRENCE AVEC VEC3INT :
//   - Vec3Int = positions de blocs (toujours entiers)
//   - Vec3Float = offsets, normales, positions précises (décimaux)
//
// ATTENTION AUX COMPARAISONS :
// Avec des floats, évite de comparer avec == directement.
//   - 0.1 + 0.2 == 0.3 → souvent FALSE à cause des erreurs d'arrondi !
//   - Utilise plutôt : Math.Abs(a - b) < 0.0001
//
// POURQUOI PAS GODOT.VECTOR3 ?
// Même raison que Vec3Int : Core ne dépend pas de Godot.
// On convertira entre Vec3Float et Godot.Vector3 dans Engine.
//
// UTILISATION :
//   var offset = new Vec3Float(0.33f, 0, -0.33f);
//   float x = offset.X;  // 0.33
// ============================================================================

namespace ProjetColony.Core.Data;

public struct Vec3Float
{
    // Les trois composantes du vecteur
    public float X;
    public float Y;
    public float Z;

// ------------------------------------------------------------------------
    // CONSTRUCTEUR — Crée un vecteur avec les valeurs données
    // ------------------------------------------------------------------------
    // PARAMÈTRES :
    //   x, y, z = les trois composantes
    //
    // EXEMPLE :
    //   var offset = new Vec3Float(0.33f, 0, -0.33f);
    //   // offset.X = 0.33, offset.Y = 0, offset.Z = -0.33
    public Vec3Float(float x, float y, float z)
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
    //   var origine = Vec3Float.Zero;  // (0, 0, 0)
    public static readonly Vec3Float Zero = new Vec3Float(0, 0, 0);

    // ------------------------------------------------------------------------
    // TOSTRING — Convertit le vecteur en texte lisible
    // ------------------------------------------------------------------------
    // Surcharge la méthode ToString() héritée de Object.
    // Très utile pour le débogage : on voit directement les valeurs.
    //
    // EXEMPLE :
    //   var pos = new Vec3Float(5.1f, 10.2f, 3.3f);
    //   Console.WriteLine(pos);  // Affiche : (5.1, 10.2, 3.3)
    //
    // Sans cette méthode, ça afficherait :
    //   ProjetColony.Core.Data.Vec3Float
    // (le nom du type, pas les valeurs)
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}