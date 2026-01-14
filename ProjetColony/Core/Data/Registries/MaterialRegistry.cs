// ============================================================================
// MATERIALREGISTRY.CS — Registre central des matériaux
// ============================================================================
// Ce fichier est dans Core/Data/Registries, donc il fait partie de CORE.
//
// Même principe que ShapeRegistry mais pour les matériaux.
// Charge les données depuis materials.json au démarrage.
// ============================================================================

using System.Collections.Generic;
using System.Text.Json;
using ProjetColony.Core.Data.Definitions;

namespace ProjetColony.Core.Data.Registries;

public static class MaterialRegistry
{
    // ------------------------------------------------------------------------
    // LES DICTIONNAIRES
    // ------------------------------------------------------------------------
    private static Dictionary<ushort, MaterialDefinition> _materials = new Dictionary<ushort, MaterialDefinition>();
    private static Dictionary<string, MaterialDefinition> _materialsByName = new Dictionary<string, MaterialDefinition>();

    // ------------------------------------------------------------------------
    // REGISTER — Ajouter un matériau au registre
    // ------------------------------------------------------------------------
    public static void Register(MaterialDefinition material)
    {
        _materials[material.Id] = material;
        _materialsByName[material.Name] = material;
    }

    // ------------------------------------------------------------------------
    // GET — Récupérer un matériau par son Id
    // ------------------------------------------------------------------------
    public static MaterialDefinition Get(ushort id)
    {
        if (_materials.TryGetValue(id, out var material))
        {
            return material;
        }
        return null;
    }

    // ------------------------------------------------------------------------
    // GETBYNAME — Récupérer un matériau par son nom
    // ------------------------------------------------------------------------
    // Très utile pour les transitions de phase !
    // Exemple : material.StateWhenMelted = "Magma"
    //           → MaterialRegistry.GetByName("Magma")
    public static MaterialDefinition GetByName(string name)
    {
        if (name == null) return null;
        
        if (_materialsByName.TryGetValue(name, out var material))
        {
            return material;
        }
        return null;
    }

    // ------------------------------------------------------------------------
    // GETALL — Récupérer tous les matériaux
    // ------------------------------------------------------------------------
    public static IEnumerable<MaterialDefinition> GetAll()
    {
        return _materials.Values;
    }

    // ------------------------------------------------------------------------
    // LOADFROMJSON — Charger les matériaux depuis un fichier JSON
    // ------------------------------------------------------------------------
    public static void LoadFromJson(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var materials = JsonSerializer.Deserialize<List<MaterialDefinition>>(jsonContent, options);

        if (materials != null)
        {
            foreach (var material in materials)
            {
                Register(material);
            }
        }
    }

    // ------------------------------------------------------------------------
    // CLEAR — Vider le registre
    // ------------------------------------------------------------------------
    public static void Clear()
    {
        _materials.Clear();
        _materialsByName.Clear();
    }

    // ------------------------------------------------------------------------
    // COUNT — Nombre de matériaux enregistrés
    // ------------------------------------------------------------------------
    public static int Count => _materials.Count;
}
