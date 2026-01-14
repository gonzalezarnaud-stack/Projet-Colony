//============================================================================
// PLACEMENTRESULT.CS — Résultat d'un calcul de placement de bloc
// ============================================================================
// Ce fichier est dans Core/Building, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// C'EST QUOI UNE STRUCT ?
// Une struct (structure) est un conteneur de données simples.
// C'est comme une boîte avec plusieurs compartiments étiquetés.
// Ici, notre boîte contient : position, sous-position, offset, validité.
//
// POURQUOI STRUCT ET PAS CLASS ?
// - struct = données copiées quand on les passe (comme un post-it recopié)
// - class = référence passée (comme donner l'adresse d'une maison)
// 
// PlacementResult est petit (quelques nombres) et on ne le modifie pas
// après création. Struct est parfait pour ça.
//
// À QUOI ÇA SERT ?
// Quand on calcule "où poser ce bloc ?", on obtient plein d'infos :
// - Dans quel voxel ? (BlockX, BlockY, BlockZ)
// - À quelle sous-position ? (SubX, SubY, SubZ)
// - Avec quel décalage visuel ? (OffsetX, OffsetY, OffsetZ)
// - Est-ce qu'on peut poser ici ? (IsValid)
//
// Au lieu de retourner 10 valeurs séparées d'une fonction, on retourne
// UN SEUL objet PlacementResult qui contient tout. C'est plus propre.
//
// EXEMPLE D'UTILISATION :
//   var result = calculator.Calculate(...);
//   if (result.IsValid)
//   {
//       world.AddBlock(result.BlockX, result.BlockY, result.BlockZ, ...);
//   }
// ============================================================================
namespace ProjetColony.Core.Building;

public struct PlacementResult
{
    // ------------------------------------------------------------------------
    // POSITION DU VOXEL
    // ------------------------------------------------------------------------
    // Les coordonnées du voxel (cube de 1×1×1) où le bloc sera posé.
    // Ce sont des coordonnées MONDIALES (pas locales au chunk).
    //
    // Exemple : BlockX=10, BlockY=5, BlockZ=-3
    // → Le bloc sera dans le voxel à la position (10, 5, -3) du monde.
    //
    // "int" = nombre entier (positif ou négatif, sans virgule)
    // On utilise int car les voxels sont sur une grille régulière.
    public int BlockX;
    public int BlockY;
    public int BlockZ;

    // ------------------------------------------------------------------------
    // SOUS-POSITION DANS LA GRILLE 3×3×3
    // ------------------------------------------------------------------------
    // Chaque voxel peut être divisé en 3×3×3 = 27 sous-positions.
    // Cela permet de placer des petits blocs (poteaux) précisément.
    //
    // Valeurs possibles :
    //   0 = bloc centré (mode normal, pas de sous-grille)
    //   1 = côté gauche / bas / arrière
    //   2 = centre
    //   3 = côté droit / haut / avant
    //
    // Exemple : SubX=1, SubY=2, SubZ=3
    // → Le bloc est à gauche, centré en hauteur, vers l'avant.
    //
    // "byte" = petit nombre entier de 0 à 255
    // On utilise byte car on n'a besoin que de 0, 1, 2, ou 3.
    // Ça économise de la mémoire (1 octet au lieu de 4 pour int).
    public byte SubX;
    public byte SubY;
    public byte SubZ;

    // ------------------------------------------------------------------------
    // DÉCALAGE VISUEL (OFFSET)
    // ------------------------------------------------------------------------
    // Le décalage en mètres pour afficher le bloc à la bonne position.
    // C'est calculé à partir de la sous-position.
    //
    // Formule : offset = (sub - 2) × 0.33
    //   Sub=1 → offset = -0.33 (décalé vers la gauche/bas/arrière)
    //   Sub=2 → offset = 0     (centré)
    //   Sub=3 → offset = +0.33 (décalé vers la droite/haut/avant)
    //
    // Pourquoi stocker l'offset ET le sub ?
    // - Sub = pour les DONNÉES (sauvegarde, logique de jeu)
    // - Offset = pour le RENDU (positionnement visuel)
    //
    // "float" = nombre à virgule (ex: 0.33, -0.5, 1.0)
    // On utilise float car les positions visuelles sont précises
    public float OffsetX;
    public float OffsetY;
    public float OffsetZ;

    // ------------------------------------------------------------------------
    // VALIDITÉ DU PLACEMENT
    // ------------------------------------------------------------------------
    // True = on peut poser un bloc ici.
    // False = impossible (hors du monde, déjà occupé, etc.)
    //
    // TOUJOURS vérifier IsValid avant d'utiliser les autres champs !
    // Si IsValid est false, les autres valeurs peuvent être incorrectes.
    //
    // "bool" = booléen, soit true (vrai) soit false (faux)
    // C'est comme un interrupteur : allumé ou éteint.
    public bool IsValid;

    // ------------------------------------------------------------------------
    // CONSTANTE : INVALID — Un résultat "échec" prêt à l'emploi
    // ------------------------------------------------------------------------
    // Parfois, le calcul de placement échoue :
    //   - Le joueur vise le ciel (pas de surface)
    //   - La position est hors des limites du monde
    //   - Le bloc ne peut pas être posé ici
    //
    // Au lieu de créer un nouveau PlacementResult à chaque échec :
    //   var result = new PlacementResult();
    //   result.IsValid = false;
    //   return result;
    //
    // On retourne directement la constante :
    //   return PlacementResult.Invalid;
    //
    // C'est plus court, plus lisible, et plus efficace (une seule instance).
    //
    // POURQUOI "static readonly" ET PAS "const" ?
    // En C#, "const" ne marche qu'avec les types simples (int, string...).
    // Une struct comme PlacementResult ne peut pas être const.
    // "static readonly" fait le même travail :
    //   - static = appartient à la classe, pas à une instance
    //   - readonly = ne peut pas être modifié après création
    public static readonly PlacementResult Invalid = new PlacementResult { IsValid = false };

    // ------------------------------------------------------------------------
    // TOSTRING — Affiche le résultat sous forme de texte
    // ------------------------------------------------------------------------
    // Quand tu débogues, tu veux voir ce qu'il y a dans un objet.
    // Sans ToString(), si tu fais GD.Print(result), tu vois :
    //   "ProjetColony.Core.Building.PlacementResult"
    // Pas très utile !
    //
    // Avec ToString(), tu vois :
    //   "Block(5, 10, 3) Sub(2, 1, 2) Offset(0, -0.33, 0) Valid=True"
    // Toutes les infos d'un coup !
    //
    // POURQUOI "override" ?
    // Tous les objets en C# ont déjà une méthode ToString() héritée de Object.
    // "override" signifie : "je REMPLACE la méthode par défaut par la mienne".
    // Sans override, le compilateur refuserait car la méthode existe déjà.
    //
    // EXEMPLE D'UTILISATION :
    //   var result = calculator.CalculateFinePlacement(...);
    //   GD.Print(result);  // Appelle automatiquement ToString()
    //   GD.Print($"Le résultat est : {result}");  // Marche aussi !
    public override string ToString()
    {
        return $"Block({BlockX}, {BlockY}, {BlockZ}) " +
        $"Sub({SubX}, {SubY}, {SubZ}) " + 
        $"Offset({OffsetX}, {OffsetY}, {OffsetZ}) " + 
        $"Valid={IsValid}";
    }

}