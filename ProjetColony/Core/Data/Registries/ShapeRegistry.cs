// ============================================================================
// SHAPEREGISTRY.CS — Registre central des formes
// ============================================================================
// Ce fichier est dans Core/Data/Registries, donc il fait partie de CORE.
//
// POURQUOI UN REGISTRY ?
// Un registry est un dictionnaire centralisé qui stocke toutes les définitions.
// Au lieu de chercher les propriétés dans le code, on demande au registry :
//   ShapeRegistry.Get(shapeId).IsSymmetric
//
// CHARGEMENT :
// Au démarrage du jeu, on appelle LoadFromJson() qui lit le fichier
// shapes.json et remplit le dictionnaire.
//
// MODDABILITÉ :
// Les mods peuvent appeler Register() pour ajouter leurs propres formes,
// ou LoadFromJson() avec leur propre fichier.
// ============================================================================

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ProjetColony.Core.Data.Definitions;

namespace ProjetColony.Core.Data.Registries;

public static class ShapeRegistry
{
    // ------------------------------------------------------------------------
    // LE DICTIONNAIRE DES FORMES
    // ------------------------------------------------------------------------
    // Clé = Id de la forme, Valeur = définition complète
    private static Dictionary<ushort, ShapeDefinition> _shapes = new Dictionary<ushort, ShapeDefinition>();

    // Dictionnaire secondaire pour chercher par nom
    private static Dictionary<string, ShapeDefinition> _shapesByName = new Dictionary<string, ShapeDefinition>();

    // ------------------------------------------------------------------------
    // REGISTER — Ajouter une forme au registre
    // ------------------------------------------------------------------------
    // Utilisé par LoadFromJson et par les mods pour ajouter des formes.
    public static void Register(ShapeDefinition shape)
    {
        _shapes[shape.Id] = shape;
        _shapesByName[shape.Name] = shape;
    }

    // ------------------------------------------------------------------------
    // GET — Récupérer une forme par son Id
    // ------------------------------------------------------------------------
    // Retourne null si la forme n'existe pas.
    public static ShapeDefinition Get(ushort id)
    {
        if (_shapes.TryGetValue(id, out var shape))
        {
            return shape;
        }
        return null;
    }

    // ------------------------------------------------------------------------
    // GETBYNAME — Récupérer une forme par son nom
    // ------------------------------------------------------------------------
    // Utile pour les transitions (ex: "Full" au lieu de 0)
    public static ShapeDefinition GetByName(string name)
    {
        if (name == null) return null;
        
        if (_shapesByName.TryGetValue(name, out var shape))
        {
            return shape;
        }
        return null;
    }

    // ------------------------------------------------------------------------
    // GETALL — Récupérer toutes les formes
    // ------------------------------------------------------------------------
    // Utile pour l'UI (liste des formes disponibles)
    public static IEnumerable<ShapeDefinition> GetAll()
    {
        return _shapes.Values;
    }

    // ------------------------------------------------------------------------
    // LOADFROMJSON — Charger les formes depuis un fichier JSON
    // ------------------------------------------------------------------------
    // Paramètre : chemin vers le fichier (ex: "res://Resources/Data/shapes.json")
    //
    // Options de désérialisation :
    // - PropertyNameCaseInsensitive : "sizeX" et "SizeX" sont équivalents
    public static void LoadFromJson(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var shapes = JsonSerializer.Deserialize<List<ShapeDefinition>>(jsonContent, options);

        if (shapes != null)
        {
            foreach (var shape in shapes)
            {
                Register(shape);
            }
        }
    }

    // ------------------------------------------------------------------------
    // CLEAR — Vider le registre
    // ------------------------------------------------------------------------
    // Utile pour les tests ou pour recharger les données
    public static void Clear()
    {
        _shapes.Clear();
        _shapesByName.Clear();
    }

    // ------------------------------------------------------------------------
    // COUNT — Nombre de formes enregistrées
    // ------------------------------------------------------------------------
    public static int Count => _shapes.Count;
}
