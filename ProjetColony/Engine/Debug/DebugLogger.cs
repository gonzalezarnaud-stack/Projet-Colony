// ============================================================================
// DEBUGLOGGER.CS — Outils de débogage centralisés
// ============================================================================
// Ce fichier est dans Engine/Debug, donc il fait partie de ENGINE.
//
// POURQUOI CETTE CLASSE ?
// Au lieu d'avoir des GD.Print() éparpillés partout qu'on oublie de supprimer,
// on centralise tout ici. On peut activer/désactiver le debug d'un seul endroit.
//
// UTILISATION :
//   DebugLogger.Log("Mon message");           // Toujours affiché
//   DebugLogger.LogBuilding("Placement...");  // Seulement si BuildingDebug = true
// ============================================================================

using Godot;

namespace ProjetColony.Engine.Debug;

public static class DebugLogger
{
    // ========================================================================
    // INTERRUPTEURS — Active/désactive le debug par catégorie
    // ========================================================================
    // Mets à true pour voir les messages de cette catégorie.
    // Mets à false pour les cacher (en production par exemple).
    
    public static bool BuildingDebug = true;   // Debug du placement de blocs
    public static bool PhysicsDebug = false;   // Debug du mouvement/collisions
    public static bool InputDebug = false;     // Debug des entrées clavier/souris
    
    // ========================================================================
    // LOG — Message toujours affiché
    // ========================================================================
    public static void Log(string message)
    {
        GD.Print($"[LOG] {message}");
    }
    
    // ========================================================================
    // LOGBUILDING — Messages de construction (si activé)
    // ========================================================================
    public static void LogBuilding(string message)
    {
        if (BuildingDebug)
        {
            GD.Print($"[BUILDING] {message}");
        }
    }
    
    // ========================================================================
    // LOGPHYSICS — Messages de physique (si activé)
    // ========================================================================
    public static void LogPhysics(string message)
    {
        if (PhysicsDebug)
        {
            GD.Print($"[PHYSICS] {message}");
        }
    }
    
    // ========================================================================
    // LOGINPUT — Messages d'entrées (si activé)
    // ========================================================================
    public static void LogInput(string message)
    {
        if (InputDebug)
        {
            GD.Print($"[INPUT] {message}");
        }
    }
}