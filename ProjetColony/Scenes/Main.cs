// ============================================================================
// MAIN.CS — Point d'entrée du jeu, affiche plusieurs chunks de test
// ============================================================================
// Ce fichier est dans le dossier Scenes, donc il fait partie de ENGINE.
// Il peut utiliser Godot (using Godot) ET notre code Core (using ProjetColony.Core.World).
//
// CE QU'IL FAIT :
// 1. Crée un World pour gérer plusieurs chunks
// 2. Crée une grille de 3×1×3 chunks (9 chunks au total)
// 3. Utilise TerrainGenerator pour créer un terrain avec du relief
// 4. Génère des couches : herbe en surface, terre en dessous, pierre en profondeur
// 5. Affiche tous les blocs solides avec des couleurs différentes
// ============================================================================

using Godot;
using ProjetColony.Core.World;
using ProjetColony.Core.Data;
using ProjetColony.Engine.Rendering;
using ProjetColony.Engine.UI;

namespace ProjetColony.Scenes;

public partial class Main : Node3D
{
    // ------------------------------------------------------------------------
    // RÉFÉRENCES STATIQUES
    // ------------------------------------------------------------------------
    // "static" permet d'accéder à ces variables depuis n'importe où :
    // Main.Instance, Main.World, Main.UiInHand
    //
    // C'est pratique pour un prototype, mais on améliorera plus tard
    // avec un système plus propre (injection de dépendances, singletons...).
    
    // Référence à cette instance de Main (pour AddChild depuis Player)
    public static Main Instance;
    
    // Le monde : médiateur central qui gère tous les chunks
    public static World World;
    
    // L'UI affichant le bloc en main
    public static UiInHand UiInHand;    

    // ========================================================================
    // _READY — Appelée une fois quand la scène est prête
    // ========================================================================
    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        // ====================================================================
        // ÉTAPE 1 : CRÉER LE MONDE
        // ====================================================================
        // World est le médiateur central qui gère tous les chunks.
        // Il s'occupe des conversions de coordonnées et cache les détails
        // d'implémentation au reste du code.
        World = new World();

        // ====================================================================
        // ÉTAPE 2 : CRÉER PLUSIEURS CHUNKS
        // ====================================================================
        // On crée une grille de 3×1×3 chunks :
        // - 3 chunks en X (positions 0, 1, 2)
        // - 1 chunk en Y (position 0) — pour l'instant on reste au sol
        // - 3 chunks en Z (positions 0, 1, 2)
        // Total : 9 chunks = 9 × 4096 = 36 864 blocs possibles
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 1; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    // Crée un chunk à la position (x, y, z)
                    // Note : c'est la position du CHUNK, pas du bloc
                    var chunk = new Chunk(x, y, z);
                    World.AddChunk(chunk);

                    // ========================================================
                    // ÉTAPE 3 : REMPLIR LE CHUNK AVEC DES BLOCS
                    // ========================================================
                    // On parcourt toutes les positions à l'intérieur du chunk.
                    // bx, by, bz = coordonnées LOCALES au chunk (0 à 15)
                    for (int bx = 0; bx < Chunk.Size; bx++)
                    {
                        for (int by = 0; by < Chunk.Size; by++)
                        {
                            for (int bz = 0; bz < Chunk.Size; bz++)
                            {
                                // Calcule les coordonnées MONDIALES du bloc
                                // Position mondiale = position du chunk × taille + position locale
                                var worldX = x * Chunk.Size + bx;
                                var worldZ = z * Chunk.Size + bz;

                                // Utilise le générateur de terrain pour avoir la hauteur
                                // La hauteur dépend de la position X et Z (pas Y)
                                var terrainHeight = TerrainGenerator.GetHeight(worldX, worldZ);

                                // ------------------------------------------------
                                // GÉNÉRATION DES COUCHES
                                // ------------------------------------------------
                                // On ne place des blocs que sous la hauteur du terrain.
                                // Au-dessus, c'est de l'air (on ne fait rien).
                                if (by < terrainHeight)
                                {
                                    Block block;

                                    // Surface : herbe (le bloc le plus haut)
                                    if (by == terrainHeight - 1)
                                    {
                                        block = new Block{MaterialId = Materials.Grass};    
                                    }
                                    // Sous-sol : terre (2 blocs sous la surface)
                                    else if (by >= terrainHeight - 3)
                                    {
                                        block = new Block{MaterialId = Materials.Dirt};
                                    }
                                    // Profondeur : pierre (tout le reste)
                                    else
                                    {
                                        block = new Block{MaterialId = Materials.Stone};
                                    }
                                    
                                    chunk.AddBlock(bx, by, bz, block);
                                }

                                // ============================================
                                // ÉTAPE 4 : AFFICHER LE BLOC À L'ÉCRAN
                                // ============================================
                                // Calcule worldY pour l'affichage
                                var worldY = y * Chunk.Size + by;

                                // Récupère le bloc (avec ses coordonnées LOCALES)
                                var blocToRender = chunk.GetBlocks(bx, by, bz);

                                // Si le bloc n'est pas de l'air, on l'affiche
                                if (blocToRender.Count > 0)
                                {
                                    // ----------------------------------------
                                    // CRÉATION DU VISUEL
                                    // ----------------------------------------
                                    foreach (var block in blocToRender)
                                    {
                                        var staticBody = BlockRenderer.CreateBlock(block, new Vector3(worldX, worldY, worldZ));
                                        AddChild(staticBody);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        // ====================================================================
        // ÉTAPE 5 : CRÉER L'INTERFACE UTILISATEUR
        // ====================================================================
        // UiInHand affiche le nom du bloc sélectionné en haut à gauche.
        // C'est un Label (texte 2D) qui flotte par-dessus le monde 3D.
        //
        // On le crée ici dans Main car c'est le point d'entrée du jeu.
        // Player.cs y accède via Main.UiInHand pour mettre à jour le texte.
        UiInHand = new UiInHand();
        AddChild(UiInHand);
    }
}