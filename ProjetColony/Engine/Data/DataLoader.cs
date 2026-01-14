// ============================================================================
// DATALOADER.CS — Chargement des données au démarrage
// ============================================================================
// Ce fichier est dans Engine car il dépend de Godot (FileAccess).
//
// POURQUOI CE FICHIER ?
// Les registries (ShapeRegistry, MaterialRegistry) sont dans Core et ne
// connaissent pas Godot. Ils attendent du texte JSON en paramètre.
//
// DataLoader fait le pont :
// 1. Utilise Godot.FileAccess pour lire les fichiers
// 2. Passe le contenu aux registries
//
// APPEL :
// DataLoader.LoadAll() est appelé une fois au démarrage dans Main.cs,
// AVANT de créer le monde ou d'afficher quoi que ce soit.
//
// MODS (futur) :
// On pourra ajouter une méthode LoadMods() qui parcourt le dossier Mods/
// et charge les JSON de chaque mod.
// ============================================================================

using Godot;
using ProjetColony.Core.Data.Registries;

namespace ProjetColony.Engine.Data;

public static class DataLoader
{
    // Chemins vers les fichiers de données
    private const string ShapesPath = "res://Resources/Data/shapes.json";
    private const string MaterialsPath = "res://Resources/Data/materials.json";

    // ------------------------------------------------------------------------
    // LOADALL — Charge toutes les données de base
    // ------------------------------------------------------------------------
    // À appeler une fois au démarrage du jeu.
    // Ordre important : les matériaux peuvent référencer d'autres matériaux
    // (transitions de phase), donc on charge tout avant d'utiliser.
    public static void LoadAll()
    {
        GD.Print("Chargement des données...");
        
        LoadShapes();
        LoadMaterials();
        
        GD.Print("Données chargées : " + ShapeRegistry.Count + " formes, " + MaterialRegistry.Count + " matériaux");
    }

    // ------------------------------------------------------------------------
    // LOADSHAPES — Charge les formes depuis shapes.json
    // ------------------------------------------------------------------------
    private static void LoadShapes()
    {
        if (!FileAccess.FileExists(ShapesPath))
        {
            GD.PrintErr("Fichier non trouvé : " + ShapesPath);
            return;
        }

        var file = FileAccess.Open(ShapesPath, FileAccess.ModeFlags.Read);
        var jsonContent = file.GetAsText();
        file.Close();

        ShapeRegistry.LoadFromJson(jsonContent);
        GD.Print("  - Formes chargées : " + ShapeRegistry.Count);
    }

    // ------------------------------------------------------------------------
    // LOADMATERIALS — Charge les matériaux depuis materials.json
    // ------------------------------------------------------------------------
    private static void LoadMaterials()
    {
        if (!FileAccess.FileExists(MaterialsPath))
        {
            GD.PrintErr("Fichier non trouvé : " + MaterialsPath);
            return;
        }

        var file = FileAccess.Open(MaterialsPath, FileAccess.ModeFlags.Read);
        var jsonContent = file.GetAsText();
        file.Close();

        MaterialRegistry.LoadFromJson(jsonContent);
        GD.Print("  - Matériaux chargés : " + MaterialRegistry.Count);
    }

    // ------------------------------------------------------------------------
    // LOADMODS — Charge les données des mods (futur)
    // ------------------------------------------------------------------------
    // Parcourt le dossier Mods/ et charge les JSON de chaque mod.
    // Les mods peuvent ajouter des formes/matériaux ou remplacer les existants.
    /*
    public static void LoadMods()
    {
        var modsDir = "res://Mods/";
        // Pour chaque sous-dossier dans Mods/
        //   Charger Data/shapes.json si existe
        //   Charger Data/materials.json si existe
    }
    */
}
