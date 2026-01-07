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
    // LE TABLEAU DE BLOCS
    // ------------------------------------------------------------------------
    // "private" = accessible uniquement dans cette classe (pas de l'extérieur)
    // "Block[,,]" = tableau à 3 dimensions contenant des Block
    // "_blocks" = le nom (le underscore _ indique que c'est privé, convention)
    // Ce tableau contient 16×16×16 = 4096 blocs
    private Block[,,] _blocks;

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
        _blocks = new Block[Size, Size, Size];
    }

    // ------------------------------------------------------------------------
    // MÉTHODE : LIRE UN BLOC
    // ------------------------------------------------------------------------
    // Renvoie le bloc situé à la position (x, y, z) dans ce chunk.
    // x, y, z doivent être entre 0 et 15 (car Size = 16).
    //
    // "public" = accessible de l'extérieur
    // "Block" = le type de ce que la méthode renvoie
    // "return" = renvoie une valeur à celui qui a appelé la méthode
    public Block GetBlock(int x, int y, int z)
    {
        return _blocks[x, y, z];
    }

    // ------------------------------------------------------------------------
    // MÉTHODE : ÉCRIRE UN BLOC
    // ------------------------------------------------------------------------
    // Place un bloc à la position (x, y, z) dans ce chunk.
    // x, y, z doivent être entre 0 et 15.
    //
    // "void" = cette méthode ne renvoie rien, elle fait juste une action
    // "Block block" = le bloc à placer (reçu en paramètre)
    public void SetBlock(int x, int y, int z, Block block)
    {
        _blocks[x, y, z] = block;
    }
}