// ============================================================================
// INPUTMAPPING.CS — Lien entre les actions/axes et les touches Godot
// ============================================================================
// Ce fichier est dans Engine/Input, donc il fait partie de ENGINE.
// Il dépend de Godot (pour le type Key) ET de Core (pour GameAction, GameAxis).
//
// C'EST QUOI CE FICHIER ?
// Il fait le PONT entre deux mondes :
//   - Core : définit les CONCEPTS ("sauter", "avancer")
//   - Godot : connaît les TOUCHES (Key.Space, Key.Z)
//
// EXEMPLE :
//   Le code de mouvement dans Core dit : "je veux savoir si le joueur saute"
//   Il demande : GameAction.Jump
//   InputMapping traduit : "Jump = touche Espace"
//   Godot répond : "oui, Espace est pressée"
//
// POURQUOI CETTE SÉPARATION ?
// Imagine qu'on change de moteur de jeu (de Godot à Unity).
// Core ne change pas — il parle toujours de "Jump" et "MoveX".
// On réécrit seulement InputMapping pour utiliser les touches Unity.
//
// C'EST QUOI UN DICTIONARY ?
// Un Dictionary (dictionnaire) associe une CLÉ à une VALEUR.
// Comme un vrai dictionnaire : mot → définition.
// Ici : action → touche.
//
// Pour chercher : Actions[GameAction.Jump] retourne Key.Space
// C'est instantané, pas besoin de parcourir toute la liste.
//
// ÉVOLUTION FUTURE :
// On pourra charger ce mapping depuis un fichier JSON pour permettre
// au joueur de personnaliser ses touches dans les options.
// ============================================================================

using Godot;
using System.Collections.Generic;
using ProjetColony.Core.Input;

namespace ProjetColony.Engine.Input;

// ----------------------------------------------------------------------------
// AXISMAPPING — Associe deux touches à un axe
// ----------------------------------------------------------------------------
// C'EST QUOI UN AXE ?
// Un axe représente un mouvement avec deux directions opposées.
// Exemple : gauche/droite, avant/arrière.
//
// POURQUOI DEUX TOUCHES ?
// L'axe MoveX (gauche/droite) a besoin de :
//   - Une touche pour "gauche" (négatif, -1) → Q
//   - Une touche pour "droite" (positif, +1) → D
//
// Cette struct regroupe ces deux touches ensemble.
// Comme ça, quand on cherche MoveX, on récupère Q ET D d'un coup.
//
// AVEC UNE MANETTE :
// Un joystick donne directement une valeur de -1 à +1.
// Pas besoin de deux boutons. Mais au clavier, si !
// ----------------------------------------------------------------------------
public struct AxisMapping
{
    public Key Negative;
    public Key Positive;
    public AxisMapping(Key negative, Key positive)
    {
        Negative = negative;
        Positive = positive;
    }
}
public static class InputMapping
{
    // ------------------------------------------------------------------------
    // ACTIONS — Mapping action → touche
    // ------------------------------------------------------------------------
    // Chaque action correspond à UNE touche.
    // L'action est soit active (touche pressée), soit inactive (relâchée).
    //
    // COMMENT LIRE CE DICTIONNAIRE :
    //   { GameAction.Jump, Key.Space }
    //     ↑ clé (action)    ↑ valeur (touche)
    //
    // Pour savoir quelle touche déclenche Jump :
    //   Key touche = Actions[GameAction.Jump];  // retourne Key.Space
    public static Dictionary<GameAction, Key> Actions = new Dictionary<GameAction, Key>
    {
        {GameAction.Jump, Key.Space},
        {GameAction.Crouch, Key.Shift},
        {GameAction.ToggleFineMode, Key.F},
        {GameAction.Rotate, Key.R},
        {GameAction.Escape, Key.Escape}
    };

    // ------------------------------------------------------------------------
    // AXES — Mapping axe → deux touches
    // ------------------------------------------------------------------------
    // Chaque axe correspond à DEUX touches (négatif et positif).
    // L'axe retourne une valeur entre -1 et +1 :
    //   - Touche négative pressée → -1
    //   - Touche positive pressée → +1
    //   - Aucune ou les deux → 0
    //
    // COMMENT LIRE CE DICTIONNAIRE :
    //   { GameAxis.MoveX, new AxisMapping(Key.Q, Key.D) }
    //     ↑ axe             ↑ négatif (gauche)  ↑ positif (droite)
    public static Dictionary<GameAxis, AxisMapping> Axes = new Dictionary<GameAxis, AxisMapping>
    {
        {GameAxis.MoveX, new AxisMapping(Key.Q, Key.D)},
        {GameAxis.MoveZ, new AxisMapping(Key.S, Key.Z)}
    };
}