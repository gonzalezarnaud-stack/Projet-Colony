// ============================================================================
// CHUNKMANAGER.CS — Gestionnaire de chunks
// ============================================================================
// Ce fichier est dans Core/World, donc il fait partie de CORE (simulation pure).
// AUCUNE dépendance à Godot.
//
// C'EST QUOI UN CHUNKMANAGER ?
// Le monde est composé de plusieurs chunks (morceaux de 16×16×16 blocs).
// Le ChunkManager stocke tous ces chunks et permet de les retrouver
// par leur position.
//
// POURQUOI UN DICTIONARY ?
// Un Dictionary (dictionnaire) associe une CLÉ à une VALEUR.
// Ici : clé = position (x, y, z) du chunk, valeur = le chunk lui-même.
// Avantage : on peut retrouver un chunk instantanément par sa position,
// sans parcourir toute une liste.
//
// UTILISATION FUTURE :
// - Charger/décharger les chunks selon la position du joueur
// - Sauvegarder/charger les chunks sur le disque
// - Générer les chunks à la demande
// ============================================================================

using System.Collections.Generic;

namespace ProjetColony.Core.World;

public class ChunkManager
{
    // ------------------------------------------------------------------------
    // LE DICTIONNAIRE DE CHUNKS
    // ------------------------------------------------------------------------
    // Dictionary<clé, valeur>
    // - Clé : un tuple (int, int, int) représentant la position du chunk
    // - Valeur : le Chunk lui-même
    //
    // Un tuple c'est un regroupement de valeurs : (x, y, z)
    // C'est plus simple qu'une classe juste pour stocker 3 nombres.
    private Dictionary<(int, int, int), Chunk> _chunks;

    // ------------------------------------------------------------------------
    // CONSTRUCTEUR
    // ------------------------------------------------------------------------
    // Appelé quand on fait "new ChunkManager()"
    // Initialise le dictionnaire vide, prêt à recevoir des chunks.
    public ChunkManager()
    {
        _chunks = new Dictionary<(int, int, int), Chunk>();
    }

    // ------------------------------------------------------------------------
    // ADDCHUNK — Ajouter un chunk au manager
    // ------------------------------------------------------------------------
    // Le chunk connaît déjà sa position (chunk.X, chunk.Y, chunk.Z)
    // On l'utilise comme clé dans le dictionnaire.
    //
    // Si un chunk existe déjà à cette position, il sera remplacé.
    public void AddChunk(Chunk chunk)
    {
        _chunks[(chunk.X, chunk.Y, chunk.Z)] = chunk;
    }

    // ------------------------------------------------------------------------
    // GETCHUNK — Récupérer un chunk par sa position
    // ------------------------------------------------------------------------
    // Retourne le chunk à la position (x, y, z), ou null si aucun chunk
    // n'existe à cette position.
    //
    // "Chunk?" — le ? signifie que la méthode peut retourner null
    //
    // TryGetValue est plus efficace que ContainsKey + accès :
    // - Il cherche UNE seule fois dans le dictionnaire
    // - "out var chunk" récupère la valeur si elle existe
    public Chunk? GetChunk(int x, int y, int z)
    {
        if (_chunks.TryGetValue((x, y, z), out var chunk))
        {
            return chunk;
        }
        return null;
    }
}