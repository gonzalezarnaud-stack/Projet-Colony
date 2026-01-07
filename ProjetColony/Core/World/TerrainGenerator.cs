// ============================================================================
// TERRAINGENERATOR.CS — Génération procédurale du terrain
// ============================================================================
// Ce fichier est dans Core/World, donc il fait partie de CORE (simulation pure).
// AUCUNE dépendance à Godot.
//
// C'EST QUOI LA GÉNÉRATION PROCÉDURALE ?
// Au lieu de dessiner le terrain à la main, on utilise des maths pour le créer.
// Avantages :
// - Terrain infini sans tout stocker en mémoire
// - Chaque position donne toujours la même hauteur (déterministe)
// - On peut créer des mondes variés en changeant quelques paramètres
//
// BRUIT DE SIMPLEX (Simplex Noise)
// Inventé par Ken Perlin en 2001 (amélioration de son "Perlin Noise" de 1983).
// C'est un algorithme qui génère des valeurs "aléatoires mais lisses".
// 
// Contrairement aux sinus (qui se répètent tous les ~63 blocs),
// le bruit Simplex ne se répète jamais visiblement → terrain naturel.
//
// Utilisé partout dans le jeu vidéo : terrain, nuages, textures, fumée...
//
// ÉVOLUTION FUTURE :
// - Combiner plusieurs couches de bruit (octaves) pour plus de détails
// - Ajouter des biomes (forêt, désert, montagne)
// - Générer des grottes, rivières, minerais
// ============================================================================

using System;
using SimplexNoise;

namespace ProjetColony.Core.World;

public class TerrainGenerator
{
    // ------------------------------------------------------------------------
    // PARAMÈTRES DE GÉNÉRATION
    // ------------------------------------------------------------------------
    // Ces constantes contrôlent l'apparence du terrain.
    // En les modifiant, on peut créer des mondes très différents.
    
    // Hauteur minimum du terrain (en blocs)
    // On a toujours au moins 1 bloc de sol, jamais de trou jusqu'au vide.
    public const int MinHeight = 1;
    
    // Amplitude du terrain (différence entre point bas et point haut)
    // Avec MinHeight=1 et HeightRange=9, le terrain varie de 1 à 10 blocs.
    public const int HeightRange = 9;
    
    // Échelle du bruit (plus c'est petit, plus les collines sont larges)
    // 0.01f = collines très larges et douces
    // 0.1f  = collines plus petites et nombreuses
    // Essaie de changer cette valeur pour voir l'effet !
    public const float LargeHills = 0.01f;

    // ------------------------------------------------------------------------
    // GETHEIGHT — Calcule la hauteur du terrain à une position
    // ------------------------------------------------------------------------
    // Paramètres :
    // - worldX, worldZ : coordonnées MONDIALES (pas locales au chunk)
    //
    // Retourne : la hauteur en blocs (nombre entier)
    //
    // "static" signifie qu'on peut appeler cette méthode sans créer d'objet :
    // TerrainGenerator.GetHeight(10, 20) au lieu de
    // var gen = new TerrainGenerator(); gen.GetHeight(10, 20);
    public static int GetHeight(int worldX, int worldZ)
    {
        // Noise.CalcPixel2D retourne une valeur entre 0 et 255
        // On divise par 255f pour avoir une valeur entre 0 et 1
        // Puis on multiplie par HeightRange et on ajoute MinHeight
        // Résultat : une hauteur entre MinHeight et (MinHeight + HeightRange)
        var noise = (int)(Noise.CalcPixel2D(worldX, worldZ, LargeHills) / 255f * HeightRange + MinHeight);
        return noise;
    }
}