// ============================================================================
// GAMEAXIS.CS — Liste des axes de mouvement du jeu
// ============================================================================
// Ce fichier est dans Core/Input, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// C'EST QUOI UN AXE ?
// Un axe représente un mouvement continu entre -1 et +1.
// Contrairement à une action (appuyé ou pas), un axe a une intensité.
//
// EXEMPLES :
//   - Joystick gauche/droite → MoveX = -1 (gauche) à +1 (droite)
//   - Touches Q/D → MoveX = -1 (Q) ou +1 (D) ou 0 (rien)
//   - Souris horizontale → LookX = mouvement relatif
//
// POURQUOI SÉPARER ACTIONS ET AXES ?
//   - Action = binaire (oui/non) → sauter, ouvrir inventaire
//   - Axe = continu (-1 à +1) → se déplacer, regarder
//
// Le mapping touche → axe est dans Engine/Input/InputMapping.cs
// ============================================================================

namespace ProjetColony.Core.Input;

public enum GameAxis
{
    // Déplacement du personnage
    // MoveX = gauche (-1) / droite (+1)
    // MoveZ = arrière (-1) / avant (+1)
    MoveX, MoveZ,

    // Rotation de la caméra (souris)
    // LookX = tourner gauche/droite
    // LookY = regarder haut/bas
    LookX, LookY
}