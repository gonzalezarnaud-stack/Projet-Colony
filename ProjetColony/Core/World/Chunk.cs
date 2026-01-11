// ============================================================================
// CHUNK.CS — Un morceau du monde (16×16×16 blocs)
// ============================================================================
// Le monde est trop grand pour gérer chaque bloc individuellement.
// Un monde de 1000×1000×256 blocs = 256 millions de blocs !
// On regroupe donc les blocs en "chunks" (morceaux) de 16×16×16.
//
// POURQUOI UNE CLASS ET PAS UNE STRUCT ?
// - struct = pour les petites données simples (comme Block, 2 bytes)
// - class = pour les objets plus complexes (comme Chunk, 4096 blocs + méthodes)
// Un Chunk contient beaucoup de données et on n'en aura que quelques centaines
// en mémoire à la fois (ceux proches du joueur), pas des millions.
//
// POURQUOI 16 ?
// - 16 est une puissance de 2 (2^4), ce qui optimise certains calculs
// - 16×16×16 = 4096 blocs par chunk, un bon équilibre mémoire/performance
// - C'est le standard de l'industrie (Minecraft utilise aussi 16)
//
// TABLEAU 3D
// Un tableau 3D est comme un cube de cases numérotées.
// On accède à une case avec trois coordonnées : [x, y, z]
// Exemple : _blocks[5, 10, 3] = le bloc à la position (5, 10, 3) dans ce chunk
// ============================================================================
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProjetColony.Core.World;

public class Chunk
{
    // ------------------------------------------------------------------------
    // CONSTANTE : LA TAILLE DU CHUNK
    // ------------------------------------------------------------------------
    // "const" = constante, cette valeur ne changera JAMAIS pendant l'exécution
    // "int" = nombre entier
    // On utilise Size partout au lieu de "16" pour que si on veut changer
    // la taille un jour, on ne modifie qu'ici.
    public const int Size = 16;

    // ------------------------------------------------------------------------
    // LE TABLEAU DE LISTES DE BLOCS
    // ------------------------------------------------------------------------
    // "private" = accessible uniquement dans cette classe (pas de l'extérieur)
    // "List<Block>[,,]" = tableau 3D où chaque case contient une LISTE de blocs
    // "_blocks" = le nom (le underscore _ indique que c'est privé, convention)
    //
    // POURQUOI UNE LISTE PAR CASE ?
    // Avec la sous-grille 4×4×4, un même voxel peut contenir plusieurs blocs
    // à des sous-positions différentes (ex: 4 poteaux dans les coins).
    // Une liste vide = air (aucun bloc à cette position).
    private List<Block>[,,] _blocks;

    // ------------------------------------------------------------------------
    // POSITION DU CHUNK DANS LE MONDE
    // ------------------------------------------------------------------------
    // Ces coordonnées sont en "coordonnées de chunk", pas de bloc.
    // Exemple : le chunk (2, 0, 1) contient les blocs de x=32 à x=47
    // (car 2 × 16 = 32, et 32 + 15 = 47)
    //
    // "{ get; }" = propriété en lecture seule
    // On peut lire X de l'extérieur, mais pas le modifier après création.
    // C'est une protection : une fois créé, un chunk ne bouge pas.
    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    // ------------------------------------------------------------------------
    // CONSTRUCTEUR
    // ------------------------------------------------------------------------
    // Le constructeur s'exécute quand on crée un nouveau Chunk avec "new".
    // Exemple : var monChunk = new Chunk(2, 0, 1);
    //
    // Il reçoit les coordonnées (x, y, z) en paramètres (minuscules)
    // et les stocke dans les propriétés X, Y, Z (majuscules).
    // Il crée aussi le tableau vide de 16×16×16 blocs.
    //
    // Note : au départ, tous les blocs ont MaterialId = 0 (air) par défaut,
    // car les struct sont initialisées à zéro en C#.
    public Chunk(int x, int y, int z)
    {
        // Stocke les coordonnées du chunk
        X = x;
        Y = y;
        Z = z;

        // Crée le tableau 3D vide
        // "new" = réserve de la mémoire pour quelque chose de nouveau
        // Block[Size, Size, Size] = tableau de 16×16×16 cases
        _blocks = new List<Block>[Size, Size, Size];

        for (int bx = 0; bx < Size; bx++)
        {
            for (int by = 0; by < Size; by++)
            {
                for (int bz = 0; bz < Size; bz++)
                {
                    _blocks[bx, by, bz] = new List<Block>();
                }
            }
        }
    }

    // ------------------------------------------------------------------------
    // MÉTHODE : LIRE LES BLOCS
    // ------------------------------------------------------------------------
    // Renvoie la liste des blocs situés à la position (x, y, z) dans ce chunk.
    // x, y, z doivent être entre 0 et 15 (car Size = 16).
    //
    // La liste peut contenir :
    // - 0 bloc = air (vide)
    // - 1 bloc = cas classique (terrain, mur...)
    // - Plusieurs blocs = construction fine (poteaux, décorations...)
    public List<Block> GetBlocks(int x, int y, int z)
    {
        return _blocks[x, y, z];
    }

    // ------------------------------------------------------------------------
    // MÉTHODE : AJOUTER UN BLOC
    // ------------------------------------------------------------------------
    // Ajoute un bloc à la position (x, y, z) dans ce chunk.
    // x, y, z doivent être entre 0 et 15.
    //
    // Le bloc est AJOUTÉ à la liste existante (pas de remplacement).
    // Pour supprimer un bloc, il faudra une méthode RemoveBlock (à venir).
    //
    // "void" = cette méthode ne renvoie rien, elle fait juste une action
    // "Block block" = le bloc à ajouter (reçu en paramètre)
    public void AddBlock(int x, int y, int z, Block block)
    {
        _blocks[x, y, z].Add(block);
    }

    // ------------------------------------------------------------------------
    // MÉTHODE : SUPPRIMER TOUS LES BLOCS
    // ------------------------------------------------------------------------
    // Vide la liste de blocs à la position (x, y, z).
    // Après cette opération, la case est vide (air).
    //
    // ÉVOLUTION FUTURE :
    // Une méthode RemoveBlock pour supprimer UN bloc spécifique
    // (utile en mode fin pour casser un seul poteau parmi plusieurs).
    public void ClearBlocks(int x, int y, int z)
    {
        _blocks[x, y, z].Clear();
    }

    // ------------------------------------------------------------------------
    // REMOVEBLOCK — Supprimer un bloc spécifique par sa sous-position
    // ------------------------------------------------------------------------
    // En mode fin, un voxel peut contenir plusieurs blocs (ex: 4 poteaux).
    // Cette méthode supprime UN SEUL bloc identifié par SubX/Y/Z.
    //
    // Paramètres : position locale (0-15) + sous-position du bloc à supprimer
    // Retourne : true si un bloc a été supprimé, false sinon
    public bool RemoveBlock(int x, int y, int z, byte subX, byte subY, byte subZ)
    {
        var blocks = _blocks[x, y, z];
        
        // Cherche le bloc avec la bonne sous-position
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].SubX == subX && blocks[i].SubY == subY && blocks[i].SubZ == subZ)
            {
                blocks.RemoveAt(i);
                return true;
            }
        }
        
        return false;
    }
}