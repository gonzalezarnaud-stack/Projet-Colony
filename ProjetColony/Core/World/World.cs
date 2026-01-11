// ============================================================================
// WORLD.CS — Le médiateur central pour accéder au monde
// ============================================================================
// Ce fichier est dans Core/World, donc il fait partie de CORE (simulation pure).
// AUCUNE dépendance à Godot.
//
// POURQUOI CETTE CLASSE ?
// Avant, le Player faisait lui-même :
//   - Calculer chunkX = blockX / 16
//   - Calculer localX = blockX % 16
//   - Récupérer le chunk via Main.ChunkManager
//   - Modifier le bloc directement
//
// Problèmes :
//   1. Le Player connaissait les détails internes (chunks, taille 16, etc.)
//   2. Bug avec les coordonnées négatives (-1 / 16 = 0 en C#, pas -1)
//   3. Couplage fort : Player dépendait de Main
//
// SOLUTION : World devient le SEUL point d'accès aux blocs.
// Le Player dit juste : world.GetBlock(10, 5, -3)
// Et World s'occupe de tout le reste.
//
// C'EST QUOI UN MÉDIATEUR ?
// Un objet qui se place entre deux autres pour qu'ils ne se parlent pas
// directement. Ici, World est entre Player et ChunkManager.
// Avantage : on peut changer ChunkManager sans toucher à Player.
// ============================================================================

using System;
using System.Collections.Generic;

namespace ProjetColony.Core.World;

public class World
{
    // ------------------------------------------------------------------------
    // LE GESTIONNAIRE DE CHUNKS
    // ------------------------------------------------------------------------
    // World POSSÈDE le ChunkManager. C'est lui le propriétaire.
    // "private" = personne d'autre ne peut y accéder directement.
    private ChunkManager _chunkManager;
    
    // ------------------------------------------------------------------------
    // CONSTRUCTEUR
    // ------------------------------------------------------------------------
    // Appelé quand on fait "new World()".
    // Crée un ChunkManager vide, prêt à recevoir des chunks.
    public World()
    {
        _chunkManager = new ChunkManager();
    }
    
    // ------------------------------------------------------------------------
    // ADDCHUNK — Ajouter un chunk au monde
    // ------------------------------------------------------------------------
    // Pour l'instant, Main crée les chunks et les ajoute ici.
    // Plus tard, World pourrait générer ses propres chunks automatiquement.
    public void AddChunk(Chunk chunk)
    {
        _chunkManager.AddChunk(chunk);
    }

    // ========================================================================
    // CONVERSION DES COORDONNÉES — LE CŒUR DE WORLD
    // ========================================================================
    // Ces méthodes résolvent le bug des coordonnées négatives.
    //
    // LE PROBLÈME :
    // En mathématiques : -1 ÷ 16 = -0.0625, arrondi vers le bas = -1
    // En C# :            -1 / 16 = 0 (troncature vers zéro, pas vers le bas !)
    //
    // Exemple concret :
    //   Position monde X = -1 (un bloc à gauche de l'origine)
    //   On veut : chunk -1, position locale 15
    //   C# donne : chunk 0, position locale -1 (FAUX !)
    // ------------------------------------------------------------------------

    // ------------------------------------------------------------------------
    // WORLDTOCHUNK — Coordonnée monde → coordonnée chunk
    // ------------------------------------------------------------------------
    // Exemple :
    //   WorldToChunk(17) = 1   (bloc 17 est dans le chunk 1)
    //   WorldToChunk(-1) = -1  (bloc -1 est dans le chunk -1)
    //
    // On utilise Math.Floor pour arrondir vers le bas (pas vers zéro).
    // Le cast en (double) force une division décimale avant l'arrondi.
    private int WorldToChunk(int worldCoord)
    {
        return (int)Math.Floor((double)worldCoord / Chunk.Size);
    }

    // ------------------------------------------------------------------------
    // WORLDTOLOCAL — Coordonnée monde → position locale (0 à 15)
    // ------------------------------------------------------------------------
    // Exemple :
    //   WorldToLocal(17) = 1   (bloc 17 est en position 1 dans son chunk)
    //   WorldToLocal(-1) = 15  (bloc -1 est en position 15 dans le chunk -1)
    //
    // LE PROBLÈME DU MODULO NÉGATIF :
    //   En C# : -1 % 16 = -1 (on veut 15 !)
    //
    // SOLUTION : Si le résultat est négatif, on ajoute Chunk.Size
    //   -1 + 16 = 15 ✓
    private int WorldToLocal(int worldCoord)
    {
        var local = worldCoord % Chunk.Size;
        if (local < 0)
        {
            local = local + Chunk.Size;
        }
        return local;
    }

    // ========================================================================
    // ACCÈS AUX BLOCS — L'INTERFACE PUBLIQUE
    // ========================================================================
    // C'est ce que le reste du code (Player, IA, systèmes) utilise.
    // On donne des coordonnées MONDE, World fait toutes les conversions.
    // ------------------------------------------------------------------------

    // ------------------------------------------------------------------------
    // GETBLOCKS — Lire les blocs à une position monde
    // ------------------------------------------------------------------------
    // Paramètres : coordonnées mondiales (peuvent être négatives)
    // Retour : liste des blocs à cette position (liste vide si chunk inexistant)
    //
    // ÉTAPES :
    // 1. Trouver dans quel chunk se trouve cette position
    // 2. Récupérer le chunk (peut être null)
    // 3. Si null → retourner une liste vide (air)
    // 4. Calculer la position locale dans le chunk
    // 5. Demander au chunk les blocs à cette position
    public List<Block> GetBlocks(int worldX, int worldY, int worldZ)
    {
        // Étape 1 : Convertir coordonnées monde → coordonnées chunk
        var chunkX = WorldToChunk(worldX);
        var chunkY = WorldToChunk(worldY);
        var chunkZ = WorldToChunk(worldZ);
        
        // Étape 2 : Récupérer le chunk
        var chunk = _chunkManager.GetChunk(chunkX, chunkY, chunkZ);

        // Étape 3 : Si le chunk n'existe pas, c'est de l'air
        if (chunk == null)
        {
            return new List<Block>();
        }

        // Étape 4 : Convertir coordonnées monde → position locale
        var localX = WorldToLocal(worldX);
        var localY = WorldToLocal(worldY);
        var localZ = WorldToLocal(worldZ);

        // Étape 5 : Demander au chunk le bloc
        return chunk.GetBlocks(localX, localY, localZ);
    }

    // ------------------------------------------------------------------------
    // ADDBLOCK — Ajouter un bloc à une position monde
    // ------------------------------------------------------------------------
    // Paramètres : coordonnées mondiales + le bloc à ajouter
    // Retour : true si réussi, false si le chunk n'existe pas
    //
    // Le bloc est AJOUTÉ à la liste existante (plusieurs blocs possibles
    // par voxel grâce à la sous-grille 4×4×4).
    //
    // ÉVOLUTION FUTURE :
    //   - Émettre un événement "BlockChanged" pour que le rendu se mette à jour
    //   - Valider si le placement est autorisé (règles de jeu)
    //   - Créer le chunk automatiquement s'il n'existe pas
    public bool AddBlock(int worldX, int worldY, int worldZ, Block block)
    {
        // Trouver le chunk
        var chunkX = WorldToChunk(worldX);
        var chunkY = WorldToChunk(worldY);
        var chunkZ = WorldToChunk(worldZ);
        var chunk = _chunkManager.GetChunk(chunkX, chunkY, chunkZ);

        // Si le chunk n'existe pas, échec
        if (chunk == null)
        {
            return false;
        }

        // Calculer la position locale
        var localX = WorldToLocal(worldX);
        var localY = WorldToLocal(worldY);
        var localZ = WorldToLocal(worldZ);

        // Placer le bloc
        chunk.AddBlock(localX, localY, localZ, block);
        return true;
    }

    // ------------------------------------------------------------------------
    // CLEARBLOCKS — Supprimer tous les blocs à une position monde
    // ------------------------------------------------------------------------
    // Paramètres : coordonnées mondiales
    // Retour : true si réussi, false si le chunk n'existe pas
    //
    // Vide entièrement la liste de blocs à cette position.
    // Utilisé quand le joueur casse un bloc en mode normal.
    public bool ClearBlocks(int worldX, int worldY, int worldZ)
    {
        var chunkX = WorldToChunk(worldX);
        var chunkY = WorldToChunk(worldY);
        var chunkZ = WorldToChunk(worldZ);
        var chunk = _chunkManager.GetChunk(chunkX, chunkY, chunkZ);

        if (chunk == null)
        {
            return false;
        }

        var localX = WorldToLocal(worldX);
        var localY = WorldToLocal(worldY);
        var localZ = WorldToLocal(worldZ);

        // Retirer les blocs
        chunk.ClearBlocks(localX, localY, localZ);
        return true;
    }
}