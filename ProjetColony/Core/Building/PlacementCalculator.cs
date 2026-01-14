// ============================================================================
// PLACEMENTCALCULATOR.CS — Calculs de placement de blocs
// ============================================================================
// Ce fichier est dans Core/Building, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// C'EST QUOI CETTE CLASSE ?
// Elle contient toute la logique pour calculer OÙ poser un bloc :
//   - Dans quel voxel ?
//   - À quelle sous-position (sub) ?
//   - Avec quel décalage visuel (offset) ?
//
// POURQUOI UNE CLASSE SÉPARÉE ?
// Avant, ces calculs étaient éparpillés dans Player.cs (dans _Process,
// dans _Input, dupliqués entre le highlight et la pose...).
// Maintenant, tout est centralisé ici. UN seul endroit à débugger.
//
// C'EST QUOI "const" ?
// Une constante est une valeur qui ne change JAMAIS pendant le jeu.
// Elle est définie une fois et reste fixe. Le compilateur peut
// optimiser le code car il sait que cette valeur est immuable.
// ============================================================================

using System.ComponentModel;

namespace ProjetColony.Core.Building;

public class PlacementCalculator
{
    // ------------------------------------------------------------------------
    // CONSTANTES
    // ------------------------------------------------------------------------
    //
    // TAILLE DU VOXEL
    // VoxelSize = taille d'un voxel en mètres (1 mètre de côté)
    //
    // SOUS-GRILLE
    // SubGridSize = nombre de divisions par axe (3 = grille 3×3×3)
    // SubGridSpacing = écart entre chaque position (~0.33 mètre)
    //   - Formule : offset = (sub - 2) × 0.33
    //     → sub=1 : offset = -0.33 (décalé vers le négatif)
    //     → sub=2 : offset = 0 (centré)
    //     → sub=3 : offset = +0.33 (décalé vers le positif)
    //
    // POSITIONS DANS LA SOUS-GRILLE
    // SubNone = 0 → pas de sous-position (bloc plein, centré)
    // SubFirst = 1 → premier côté (gauche/bas/arrière)
    // SubCenter = 2 → centre
    // SubLast = 3 → dernier côté (droite/haut/avant)
    //
    // SEUIL DE DÉTECTION
    // NormalThreshold = 0.5 → seuil pour détecter la direction d'une normale
    //   - Si normale > 0.5 → pointe vers le positif
    //   - Si normale < -0.5 → pointe vers le négatif
    public const float VoxelSize = 1.0f;
    public const int SubGridSize = 3;
    public const float SubGridSpacing = 0.33f;
    public const int SubNone = 0;
    public const int SubFirst = 1;
    public const int SubCenter = 2;
    public const int SubLast = 3;

    // ------------------------------------------------------------------------
    // DEMI-VOXEL
    // ------------------------------------------------------------------------
    // La moitié de la taille d'un voxel (0.5 mètre).
    // Utilisé pour calculer la position des faces d'un voxel.
    // Le centre d'un voxel est à (X, Y, Z), ses faces sont à ±0.5 de là.
    public const float HalfVoxel = 0.5f;

    // ------------------------------------------------------------------------
    // SEUIL DE DÉTECTION DE NORMALE
    // ------------------------------------------------------------------------
    // Seuil pour déterminer si une normale pointe dans une direction.
    // Si normale.X > 0.5 → la face pointe vers +X (droite)
    // Si normale.X < -0.5 → la face pointe vers -X (gauche)
    // Sinon → la face ne pointe pas sur cet axe
    //
    // Pourquoi 0.5 et pas 0.9 ou 1.0 ?
    // Les normales sont souvent (1, 0, 0) exactement, mais parfois
    // légèrement imprécises à cause des erreurs de calcul flottant.
    // 0.5 laisse une marge de sécurité.
    public const float NormalThreshold = 0.5f;

    // ------------------------------------------------------------------------
    // FIXSUBFORNORMAL — Fixe un sub selon la direction de la normale
    // ------------------------------------------------------------------------
    // Quand on clique sur un bloc PLEIN, on veut se coller contre lui.
    // Cette méthode ajuste le sub pour coller au bon côté.
    //
    // PARAMÈTRES :
    //   normal = composante de la normale (-1, 0, ou 1)
    //   currentSub = sub calculé depuis la fraction (où on a cliqué)
    //
    // RETOURNE :
    //   SubFirst (1) si normale positive → coller vers le négatif
    //   SubLast (3) si normale négative → coller vers le positif
    //   currentSub si normale nulle → garder la valeur calculée
    //
    // EXEMPLE :
    //   On clique sur la face droite (+X) d'un bloc.
    //   normal = 1.0, currentSub = 2
    //   → Retourne SubFirst (1) pour coller le nouveau bloc à gauche du voxel
    //
    // POURQUOI "private" ?
    //   Cette méthode est un "helper" (assistant) utilisé uniquement
    //   à l'intérieur de cette classe. Personne d'autre n'a besoin
    //   de l'appeler. "private" la cache du reste du code.
    public int FixSubForNormal(float normal, int currentSub)
    {
        if(normal > NormalThreshold)
        {
            return SubFirst;
        }
        else if(normal < -NormalThreshold)
        {
            return SubLast;
        }
        else
        {
            return currentSub;
        }
    }

    // ------------------------------------------------------------------------
    // CALCULATEAXISOFFSET — Calcule l'offset pour un axe selon la taille
    // ------------------------------------------------------------------------
    // Si le bloc est PETIT sur cet axe (< 1 mètre), on calcule l'offset
    // pour le positionner dans la sous-grille.
    // Si le bloc est GRAND (>= 1 mètre), il occupe tout l'axe : pas d'offset,
    // et on met sub à SubNone (pas de sous-position).
    //
    // PARAMÈTRES :
    //   size = dimension du bloc sur cet axe (après rotation)
    //   ref sub = sous-position (peut être modifiée)
    //
    // RETOURNE :
    //   L'offset en mètres (0 si bloc grand)
    //
    // C'EST QUOI "ref" ?
    //   "ref" permet de MODIFIER une variable passée en paramètre.
    //   Sans ref, la méthode reçoit une COPIE — les changements sont perdus.
    //   Avec ref, la méthode modifie l'ORIGINAL — sub = SubNone persiste.
    //
    // EXEMPLE :
    //   size = 0.33, sub = 2 → retourne 0 (offset central), sub reste 2
    //   size = 1.0, sub = 2 → retourne 0, sub devient SubNone (0)
    private float CalculateAxisOffset(float size, ref int sub)
    {
        if(size < VoxelSize)
        {
            return CalculateOffset(sub);
        }
        else
        {
            sub = SubNone;
            return 0;
        }
    }

    // ------------------------------------------------------------------------
    // CALCULATEOFFSET — Convertit un sub (1, 2, 3) en offset (-0.33, 0, +0.33)
    // ------------------------------------------------------------------------
    // PARAMÈTRE :
    //   sub = position dans la sous-grille (1, 2, ou 3)
    //
    // RETOURNE :
    //   Le décalage en mètres pour afficher le bloc à la bonne position.
    //
    // FORMULE : (sub - 2) × SubGridSpacing
    //   sub=1 → (1-2) × 0.33 = -0.33 (décalé vers gauche/bas/arrière)
    //   sub=2 → (2-2) × 0.33 = 0     (centré)
    //   sub=3 → (3-2) × 0.33 = +0.33 (décalé vers droite/haut/avant)
    //
    // POURQUOI "-2" ?
    // On veut que sub=2 (le centre) donne offset=0.
    // En soustrayant 2, on "recentre" les valeurs autour de zéro.
    public float CalculateOffset(int sub)
    {
        var offset = (sub -2) * SubGridSpacing;
        return offset;
    }

    // ------------------------------------------------------------------------
    // CALCULATESUB — Convertit une fraction (0.0-1.0) en sub (1, 2, 3)
    // ------------------------------------------------------------------------
    // PARAMÈTRE :
    //   fraction = position relative dans le voxel (0.0 à 1.0)
    //              0.0 = bord gauche/bas/arrière du voxel
    //              1.0 = bord droit/haut/avant du voxel
    //
    // RETOURNE :
    //   Le numéro de sous-position (1, 2, ou 3)
    //
    // FORMULE : (int)(fraction × SubGridSize) + 1
    //   fraction=0.1 → (int)(0.1 × 3) + 1 = 0 + 1 = 1
    //   fraction=0.5 → (int)(0.5 × 3) + 1 = 1 + 1 = 2
    //   fraction=0.9 → (int)(0.9 × 3) + 1 = 2 + 1 = 3
    //
    // POURQUOI "+1" ?
    // La multiplication donne 0, 1, ou 2. On ajoute 1 pour avoir 1, 2, ou 3.
    // C'est plus intuitif : 1 = premier, 2 = deuxième, 3 = troisième.
    //
    // UTILISATION :
    // Quand le joueur clique quelque part dans un voxel, on calcule où
    // exactement il a cliqué (fraction), puis on convertit en sub.
    public int CalculateSub(float fraction)
    {
        var sub = (int)(fraction * SubGridSize) + 1;
        return sub;
    }

    // ------------------------------------------------------------------------
    // CALCULATEDIMENSIONAFTERROTATION — Calcule les dimensions après rotation
    // ------------------------------------------------------------------------
    // Quand on tourne un bloc, ses dimensions changent d'axe.
    // Exemple : un poteau vertical (0.33 × 1 × 0.33) couché sur le côté
    // devient horizontal (0.33 × 0.33 × 1).
    //
    // PARAMÈTRES D'ENTRÉE :
    //   sizeX, sizeY, sizeZ = dimensions originales du bloc
    //   rotationY = rotation horizontale (0, 1, 2, 3 = 0°, 90°, 180°, 270°)
    //   rotationX = rotation verticale (pour coucher le bloc)
    //
    // PARAMÈTRES DE SORTIE (out) :
    //   newSizeX, newSizeY, newSizeZ = dimensions après rotation
    //
    // C'EST QUOI "out" ?
    // Normalement une méthode retourne UNE seule valeur.
    // Avec "out", on peut "retourner" plusieurs valeurs.
    // La méthode DOIT assigner une valeur à chaque paramètre out.
    //
    // POURQUOI CET ORDRE ? (rotationX puis rotationY)
    // Les rotations ne sont pas commutatives : l'ordre compte !
    // On applique d'abord rotationX (sur les axes locaux du bloc),
    // puis rotationY (sur les axes du monde).
    //
    // ÉCHANGE DE VALEURS :
    // Pour échanger A et B, on ne peut pas faire :
    //   A = B;  ← A prend la valeur de B
    //   B = A;  ← B prend la valeur de A... qui est maintenant B !
    // On perd la valeur originale de A.
    //
    // Solution : variable temporaire
    //   temp = A;  ← on sauvegarde A
    //   A = B;     ← A prend la valeur de B
    //   B = temp;  ← B prend l'ancienne valeur de A
    public void CalculateDimensionAfterRotation(
        float sizeX, float sizeY, float sizeZ,
        int rotationY, int rotationX,
        out float newSizeX, out float newSizeY, out float newSizeZ
    )
    {
        newSizeX = sizeX ;
        newSizeY = sizeY;
        newSizeZ = sizeZ;

        if(rotationX == 1 || rotationX == 3)
        {
            var temp = newSizeY;
            newSizeY = newSizeZ;
            newSizeZ = temp;
        }

        if(rotationY == 1 || rotationY == 3)
        {
            var temp = newSizeX;
            newSizeX = newSizeZ;
            newSizeZ = temp;
        }
    }

    // ------------------------------------------------------------------------
    // CLAMPSUB — Garde le sub dans les bornes (1-3), gère le changement de voxel
    // ------------------------------------------------------------------------
    // En mode fin, quand on se déplace sur la grille, on peut "sortir" du voxel.
    // Exemple : on est en sub=3 et on veut aller à droite → sub=4
    // Mais sub=4 n'existe pas ! On doit passer au voxel suivant, position 1.
    //
    // PARAMÈTRE D'ENTRÉE :
    //   sub = la sous-position à vérifier (peut être 0, 4, ou autre)
    //
    // PARAMÈTRE DE SORTIE (out) :
    //   voxelOffset = de combien décaler le voxel
    //     -1 = aller au voxel précédent
    //      0 = rester dans le même voxel
    //     +1 = aller au voxel suivant
    //
    // RETOURNE :
    //   Le sub corrigé (toujours entre 1 et 3)
    //
    // EXEMPLES :
    //   sub=2 → voxelOffset=0, retourne 2 (pas de changement)
    //   sub=4 → voxelOffset=1, retourne 1 (voxel suivant, première position)
    //   sub=0 → voxelOffset=-1, retourne 3 (voxel précédent, dernière position)
    //
    // UTILISATION :
    //   Quand on clique sur la face droite d'un poteau en sub=3,
    //   on calcule sub = 3 + 1 = 4. ClampSub corrige ça.
    public int ClampSub(int sub, out int voxelOffset)
    {
        int nextVoxel = 1;
        int firstPositionNextVoxel = 1;
        int previousVoxel = -1;
        int lastPositionPreviousVoxel = 3;

        if(sub > 3)
        {
            voxelOffset = nextVoxel;
            return firstPositionNextVoxel;
        }
        else if (sub < 1)
        {
            voxelOffset = previousVoxel;
            return lastPositionPreviousVoxel;
        }
        else
        {
            voxelOffset = 0;
            return sub;
        }
    }

    // ------------------------------------------------------------------------
    // CALCULATENORMALPLACEMENT — Calcul de placement en mode normal
    // ------------------------------------------------------------------------
    // En mode normal, c'est simple : on pose dans le voxel ADJACENT à la
    // face cliquée. Pas de sous-grille, le bloc occupe tout le voxel.
    //
    // PARAMÈTRES D'ENTRÉE :
    //   hitBlockX/Y/Z = position du bloc sur lequel on a cliqué
    //   normalX/Y/Z = direction de la face cliquée
    //     (1,0,0) = face droite   → on pose à droite
    //     (-1,0,0) = face gauche  → on pose à gauche
    //     (0,1,0) = face du dessus → on pose au-dessus
    //     etc.
    //
    // RETOURNE :
    //   Un PlacementResult avec la position calculée.
    //
    // POURQUOI MATH.ROUND ?
    //   La normale est un float (ex: 0.9999 ou 1.0001 à cause des erreurs
    //   de calcul en virgule flottante). Round l'arrondit proprement à 1.
    //
    // EXEMPLE :
    //   On clique sur le dessus du bloc (5, 10, 3)
    //   normale = (0, 1, 0)
    //   → BlockX = 5 + 0 = 5
    //   → BlockY = 10 + 1 = 11
    //   → BlockZ = 3 + 0 = 3
    //   → Le bloc sera posé en (5, 11, 3), juste au-dessus.
    public PlacementResult CalculateNormalPlacement(
        int hitBlockX, int hitBlockY, int hitBlockZ,
        float normalX, float normalY, float normalZ
    )
    {
        var result = new PlacementResult();
        result.BlockX = hitBlockX + (int)System.Math.Round(normalX);
        result.BlockY = hitBlockY + (int)System.Math.Round(normalY);
        result.BlockZ = hitBlockZ + (int)System.Math.Round(normalZ);
        result.SubX = 0;
        result.SubY = 0;
        result.SubZ = 0;
        result.OffsetX = 0;
        result.OffsetY = 0;
        result.OffsetZ = 0;

        result.IsValid = true;

        return result;
    }

    // ------------------------------------------------------------------------
    // CALCULATEFINEPLACEMENT — Calcul de placement en mode fin (sous-grille 3×3×3)
    // ------------------------------------------------------------------------
    // En mode fin, on peut placer des blocs à des positions précises dans
    // un voxel. Cette méthode fait tous les calculs nécessaires.
    //
    // PARAMÈTRES D'ENTRÉE :
    //   surfaceVoxelX/Y/Z = le voxel de destination (où on va poser)
    //   surfaceNormalX/Y/Z = direction de la face cliquée
    //   fractionX/Y/Z = où on a cliqué dans le voxel (0.0 à 1.0)
    //   hasFixedSub = true si on a cliqué sur un poteau
    //   fixedSubX/Y/Z = sub fixé (utilisé si hasFixedSub est true)
    //   shapeSizeX/Y/Z = dimensions du bloc à poser
    //   rotationY/X = rotation du bloc
    //
    // RETOURNE :
    //   Un PlacementResult avec position, sub, offset, et validité.
    //
    // ÉTAPES DU CALCUL :
    //   1. Initialiser la position avec le voxel de destination
    //   2. Calculer les sub depuis les fractions (où on a cliqué)
    //   3. Si hasFixedSub, utiliser les sub fixés (clic sur poteau)
    //      Sinon, ajuster les sub selon la normale (coller contre le bloc)
    //   4. Calculer les dimensions du bloc après rotation
    //   5. Calculer les offsets (seulement si le bloc est petit sur l'axe)
    //   6. Remplir et retourner le résultat
    //
    // POURQUOI AUTANT DE PARAMÈTRES ?
    //   Cette méthode est dans CORE — elle ne connaît pas Godot.
    //   Elle ne peut pas faire de raycast ou lire la position de la souris.
    //   C'est l'appelant (dans Engine) qui lui fournit toutes les infos.
    //   Avantage : on peut tester cette méthode sans lancer le jeu !
    public PlacementResult CalculateFinePlacement(
        int surfaceVoxelX, int surfaceVoxelY, int surfaceVoxelZ,
        float surfaceNormalX, float surfaceNormalY, float surfaceNormalZ,
        float fractionX, float fractionY, float fractionZ,
        bool hasFixedSub, int fixedSubX, int fixedSubY, int fixedSubZ,
        float shapeSizeX, float shapeSizeY, float shapeSizeZ,
        int rotationY, int rotationX
    )
    {
        var result = new PlacementResult();
        int blockX = surfaceVoxelX;
        int blockY = surfaceVoxelY;
        int blockZ = surfaceVoxelZ;
        int subX = CalculateSub(fractionX);
        int subY = CalculateSub(fractionY);
        int subZ = CalculateSub(fractionZ);

        if(hasFixedSub)
        {
            subX = fixedSubX;
            subY = fixedSubY;
            subZ = fixedSubZ;
        }
        else
        {
            subX = FixSubForNormal(surfaceNormalX, subX);
            subY = FixSubForNormal(surfaceNormalY, subY);
            subZ = FixSubForNormal(surfaceNormalZ, subZ);
        }

        float newSizeX;
        float newSizeY;
        float newSizeZ;

        CalculateDimensionAfterRotation(
            shapeSizeX, shapeSizeY, shapeSizeZ,
            rotationY, rotationX,
            out newSizeX, out newSizeY, out newSizeZ
        );

        float offsetX = CalculateAxisOffset(newSizeX, ref subX);
        float offsetY = CalculateAxisOffset(newSizeY, ref subY);
        float offsetZ = CalculateAxisOffset(newSizeZ, ref subZ);

        result.BlockX = blockX;
        result.BlockY = blockY;
        result.BlockZ = blockZ;
        result.SubX = (byte)subX;
        result.SubY = (byte)subY;
        result.SubZ = (byte)subZ;
        result.OffsetX = offsetX;
        result.OffsetY = offsetY;
        result.OffsetZ = offsetZ;

        result.IsValid = true;

        return result;
    }

}