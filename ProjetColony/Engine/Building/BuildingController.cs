// ============================================================================
// BUILDINGCONTROLLER.CS — Gère la pose et la casse de blocs
// ============================================================================
// Ce fichier est dans Engine/Building, donc il fait partie de ENGINE.
// Il dépend de Godot ET de Core.
//
// C'EST QUOI CE FICHIER ?
// C'est le CHEF D'ORCHESTRE de la construction.
// Quand le joueur clique pour poser ou casser un bloc, c'est ici que ça passe.
//
// QU'EST-CE QU'IL FAIT ?
//   1. Utilise PlacementCalculator pour calculer OÙ poser
//   2. Utilise World pour MODIFIER les données (ajouter/supprimer blocs)
//   3. Utilise Godot pour CRÉER/SUPPRIMER les visuels
//
// POURQUOI UNE CLASSE SÉPARÉE ?
// Avant, tout ce code était dans Player.cs (des centaines de lignes).
// Maintenant :
//   - Player.cs détecte les clics et appelle BuildingController
//   - BuildingController fait le travail
//
// C'est la SÉPARATION DES RESPONSABILITÉS :
//   - Player = détecter les entrées du joueur
//   - BuildingController = construire/détruire
//   - PlacementCalculator = calculer les positions
//   - World = stocker les données
//
// POURQUOI PAS STATIC ?
// BuildingController a besoin de références vers World, Calculator, State.
// Une classe static ne peut pas stocker d'état.
// On crée UNE instance au démarrage du jeu et on la réutilise.
// ============================================================================

using Godot;
using ProjetColony.Core.World;
using ProjetColony.Core.Data;
using ProjetColony.Core.Building;

namespace ProjetColony.Engine.Building;

public class BuildingController
{
    // ------------------------------------------------------------------------
    // DÉPENDANCES — Les objets dont BuildingController a besoin
    // ------------------------------------------------------------------------
    // _world = pour modifier les données du monde (ajouter/supprimer blocs)
    // _calculator = pour calculer les positions de placement
    // _state = pour connaître l'état actuel (mode fin, bloc sélectionné...)
    //
    // Ces objets sont INJECTÉS via le constructeur.
    // BuildingController ne les crée pas lui-même — il les reçoit.
    // C'est le principe d'INJECTION DE DÉPENDANCES :
    //   - Plus facile à tester (on peut injecter des faux objets)
    //   - Plus flexible (on peut changer l'implémentation)
    private World _world;
    private PlacementCalculator _calculator;
    private BuildingState _state;

    // ------------------------------------------------------------------------
    // CONSTRUCTEUR — Reçoit les dépendances
    // ------------------------------------------------------------------------
    // On passe les objets dont BuildingController a besoin.
    // Il les stocke dans ses champs privés pour les utiliser plus tard.
    //
    // EXEMPLE D'UTILISATION (dans Main.cs ou Player.cs) :
    //   var world = new World();
    //   var calculator = new PlacementCalculator();
    //   var state = new BuildingState();
    //   var building = new BuildingController(world, calculator, state);
    public BuildingController(World world, PlacementCalculator calculator, BuildingState state)
    {
        _world = world;
        _calculator = calculator;
        _state = state;
    }

    // ------------------------------------------------------------------------
    // PLACEBLOCK — Ajoute un bloc dans le monde
    // ------------------------------------------------------------------------
    // Cette méthode gère UNIQUEMENT les données, pas le visuel.
    // L'appelant (Player) crée le visuel avec BlockRenderer si succès.
    //
    // PARAMÈTRES :
    //   blockX, blockY, blockZ = position du voxel où poser
    //   block = le bloc à ajouter (déjà configuré avec MaterialId, ShapeId, etc.)
    //
    // RETOURNE :
    //   true = bloc ajouté avec succès
    //   false = échec (chunk inexistant)
    //
    // POURQUOI LE BLOC EST PASSÉ EN PARAMÈTRE ?
    // L'appelant a besoin du Block pour créer le visuel avec BlockRenderer.
    // Si PlaceBlock créait le Block en interne, il faudrait le créer deux fois
    // (une fois ici, une fois pour le visuel). C'est de la duplication.
    //
    // En le passant en paramètre :
    //   1. L'appelant crée le Block
    //   2. L'appelant appelle PlaceBlock (données)
    //   3. L'appelant appelle BlockRenderer (visuel) avec le même Block
    //
    // EXEMPLE D'UTILISATION (dans Player.cs) :
    //   var block = new Block { MaterialId = 1, ShapeId = 0, ... };
    //   if (_buildingController.PlaceBlock(blockX, blockY, blockZ, block))
    //   {
    //       var visual = BlockRenderer.CreateBlock(block, position);
    //       Main.Instance.AddChild(visual);
    //   }
    public bool PlaceBlock(int blockX, int blockY, int blockZ, Block block)
    {
        return _world.AddBlock(blockX, blockY, blockZ, block);
    }

    // ------------------------------------------------------------------------
    // REMOVEBLOCK — Supprime un bloc du monde
    // ------------------------------------------------------------------------
    // Cette méthode gère UNIQUEMENT les données, pas le visuel.
    // L'appelant (Player) supprime le visuel avec QueueFree() si succès.
    //
    // PARAMÈTRES :
    //   blockX, blockY, blockZ = position du voxel
    //   subX, subY, subZ = sous-position dans le voxel (0 si mode normal)
    //
    // RETOURNE :
    //   true = bloc supprimé avec succès
    //   false = échec (bloc inexistant ou chunk inexistant)
    //
    // DEUX CAS :
    //   1. Mode normal (sub = 0, 0, 0) :
    //      Supprime TOUS les blocs du voxel avec ClearBlocks()
    //
    //   2. Mode fin (sub != 0) :
    //      Supprime UN SEUL bloc à la sous-position avec RemoveBlock()
    //      Les autres blocs du voxel restent en place
    //
    // EXEMPLE :
    //   Un voxel contient 4 poteaux aux coins.
    //   En mode normal : les 4 disparaissent.
    //   En mode fin : on supprime celui qu'on vise, les 3 autres restent.
    public bool RemoveBlock
    (
        int blockX, int blockY, int blockZ,
        byte subX, byte subY, byte subZ
    )
    {
        if(subX == 0 && subY == 0 && subZ == 0)
        {
            return _world.ClearBlocks(blockX, blockY, blockZ);
        }
        else
        {
            return _world.RemoveBlock
            (
                blockX, blockY, blockZ,
                subX, subY, subZ
            );
        }
    }
}