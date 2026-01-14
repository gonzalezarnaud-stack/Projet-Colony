// ============================================================================
// GAMEACTION.CS — Liste des actions du jeu
// ============================================================================
// Ce fichier est dans Core/Input, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// C'EST QUOI UN ENUM ?
// Un enum (énumération) est une liste de valeurs possibles.
// Au lieu d'utiliser des nombres magiques (0, 1, 2...) ou des strings
// ("jump", "crouch"...), on utilise des noms clairs et vérifiés.
//
// AVANTAGES :
// 1. Le compilateur vérifie les fautes de frappe
//    - GameAction.Jmp → erreur (n'existe pas)
//    - "jmp" → pas d'erreur, mais bug silencieux
// 2. L'autocomplétion propose les valeurs possibles
// 3. Code plus lisible et maintenable
//
// POURQUOI DANS CORE ?
// Les actions sont des CONCEPTS du jeu, pas des touches clavier.
// "Sauter" existe indépendamment de la touche qui le déclenche.
// Le mapping touche → action est dans Engine/Input/InputMapping.cs
// ============================================================================

namespace ProjetColony.Core.Input;

public enum GameAction
{
    // Actions physiques du personnage
    Jump,
    Crouch,

    // Actions de construction
    ToggleFineMode,
    Rotate,

    // Actions système
    Escape
}