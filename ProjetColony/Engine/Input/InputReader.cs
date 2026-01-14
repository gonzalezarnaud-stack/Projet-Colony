// ============================================================================
// INPUTREADER.CS — Lecture des entrées du joueur
// ============================================================================
// Ce fichier est dans Engine/Input, donc il fait partie de ENGINE.
// Il dépend de Godot (pour Input) ET de Core (pour GameAction, GameAxis).
//
// C'EST QUOI CE FICHIER ?
// C'est l'INTERFACE entre le jeu et les entrées du joueur.
// Le reste du code ne parle jamais directement à Godot pour les touches.
// Il passe toujours par InputReader.
//
// POURQUOI ?
// Le code de gameplay dit : "Est-ce que le joueur veut sauter ?"
// Il ne dit PAS : "Est-ce que la touche Espace est pressée ?"
//
// Avantages :
//   1. Le code est plus lisible (IsActionPressed(Jump) vs IsKeyPressed(Space))
//   2. On peut changer les touches sans toucher au code de gameplay
//   3. On pourra ajouter le support manette facilement
//
// COMMENT ÇA MARCHE ?
//   1. Le gameplay demande : InputReader.IsActionPressed(GameAction.Jump)
//   2. InputReader cherche dans InputMapping : Jump → Key.Space
//   3. InputReader demande à Godot : Input.IsKeyPressed(Key.Space)
//   4. Godot répond : true ou false
//   5. InputReader transmet la réponse au gameplay
//
// C'est une CHAÎNE DE TRADUCTION :
//   Gameplay (concepts) → InputReader → InputMapping → Godot (touches)
// ============================================================================

using Godot;
using ProjetColony.Core.Input;

namespace ProjetColony.Engine.Input;

public static class InputReader
{

    // ------------------------------------------------------------------------
    // ISACTIONPRESSED — Vérifie si une action est active
    // ------------------------------------------------------------------------
    // PARAMÈTRE :
    //   action = l'action à vérifier (Jump, Crouch, etc.)
    //
    // RETOURNE :
    //   true si la touche correspondante est pressée, false sinon
    //
    // EXEMPLE :
    //   if (InputReader.IsActionPressed(GameAction.Jump))
    //   {
    //       // Le joueur veut sauter !
    //   }
    //
    // ÉTAPES INTERNES :
    //   1. Cherche la touche dans le dictionnaire Actions
    //   2. Demande à Godot si cette touche est pressée
    //   3. Retourne le résultat
    public static bool IsActionPressed(GameAction action)
    {
        var key = InputMapping.Actions[action];
        return Godot.Input.IsKeyPressed(key);
    }

    // ------------------------------------------------------------------------
    // GETAXIS — Lit la valeur d'un axe de mouvement
    // ------------------------------------------------------------------------
    // PARAMÈTRE :
    //   gameAxis = l'axe à lire (MoveX, MoveZ, etc.)
    //
    // RETOURNE :
    //   -1 si touche négative pressée (gauche, arrière)
    //   +1 si touche positive pressée (droite, avant)
    //    0 si aucune touche ou les deux (elles s'annulent)
    //
    // EXEMPLE :
    //   float moveX = InputReader.GetAxis(GameAxis.MoveX);
    //   // moveX = -1 (Q pressé), +1 (D pressé), ou 0 (rien/les deux)
    //
    // POURQUOI DEUX IF SÉPARÉS (PAS IF/ELSE) ?
    //   Si le joueur appuie sur Q ET D en même temps :
    //     - Premier if : value -= 1 → value = -1
    //     - Deuxième if : value += 1 → value = 0
    //   Les deux touches s'annulent ! C'est le comportement voulu.
    //
    //   Avec if/else, on ne vérifierait que la première touche.
    public static float GetAxis(GameAxis gameAxis)
    {
        var mapping = InputMapping.Axes[gameAxis];
        float value = 0;

        if(Godot.Input.IsKeyPressed(mapping.Positive))
        {
            value += 1;
        }
        if(Godot.Input.IsKeyPressed(mapping.Negative))
        {
            value -= 1;
        }

        return value;
    }
}