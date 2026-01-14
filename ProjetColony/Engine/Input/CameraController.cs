// ============================================================================
// CAMERACONTROLLER.CS — Gestion de la rotation de la caméra
// ============================================================================
// Ce fichier est dans Engine/Input, donc il fait partie de ENGINE.
// Il dépend de Godot (Camera3D, Node3D, Vector2, etc.)
//
// C'EST QUOI CETTE CLASSE ?
// Elle gère la rotation de la caméra quand le joueur bouge la souris.
// C'est le comportement standard des jeux FPS (First Person Shooter).
//
// POURQUOI UNE CLASSE SÉPARÉE ?
// Avant, ce code était dans Player.cs avec les champs _rotationX, _rotationY,
// _mouseSensitivity. En l'extrayant :
//   - Player.cs devient plus court
//   - CameraController a UNE seule responsabilité
//   - On pourrait réutiliser ce code pour d'autres caméras
//
// PROBLÈME RÉSOLU : GIMBAL LOCK
// Si on applique les rotations directement avec RotateX/RotateY,
// les axes se mélangent et la caméra "roule" bizarrement.
// Solution : on stocke les angles séparément (_rotationX, _rotationY)
// et on reconstruit la rotation complète à chaque mouvement.
//
// SÉPARATION CORPS / TÊTE :
// - Le CORPS (Player) tourne horizontalement (regarder gauche/droite)
// - La CAMÉRA tourne verticalement (regarder haut/bas)
// C'est comme une vraie personne : on tourne sur soi pour regarder
// à gauche, mais on hoche la tête pour regarder en haut.
// ============================================================================

using Godot;

namespace ProjetColony.Engine.Input;

public class CameraController
{
    // ========================================================================
    // RÉFÉRENCES
    // ========================================================================
    
    // La caméra à faire tourner (rotation verticale seulement)
    private Camera3D _camera;
    
    // Le corps du joueur (rotation horizontale seulement)
    // C'est le CharacterBody3D qui contient la caméra comme enfant
    private Node3D _playerBody;
    
    // ========================================================================
    // ÉTAT DE LA ROTATION
    // ========================================================================
    // On stocke les angles séparément pour éviter le Gimbal Lock.
    // Les valeurs sont en RADIANS (pas en degrés).
    //   - 0 radians = 0°
    //   - π/2 radians ≈ 1.57 = 90°
    //   - π radians ≈ 3.14 = 180°
    
    // _rotationX = pitch (regarder haut/bas)
    // Limité entre -90° et +90° pour ne pas faire de looping
    private float _rotationX;
    
    // _rotationY = yaw (regarder gauche/droite)
    // Pas de limite, on peut tourner indéfiniment
    private float _rotationY;
    
    // ========================================================================
    // CONFIGURATION
    // ========================================================================
    
    // Sensibilité de la souris (plus petit = plus lent)
    // 0.002 est une valeur standard pour les FPS
    private float _mouseSensitivity = 0.002f;

    // ========================================================================
    // CONSTRUCTEUR
    // ========================================================================
    // Reçoit la caméra et le corps du joueur en paramètres.
    // On les stocke pour les utiliser dans HandleMouseMotion.
    public CameraController(Camera3D camera, Node3D playerBody)
    {
        _camera = camera;
        _playerBody = playerBody;
    }

    // ========================================================================
    // HANDLEMOUSEMOTION — Appelée quand la souris bouge
    // ========================================================================
    // PARAMÈTRE :
    //   relativeMotion = combien la souris a bougé depuis la dernière frame
    //     .X = mouvement horizontal (gauche/droite)
    //     .Y = mouvement vertical (haut/bas)
    //
    // POURQUOI LE SIGNE NÉGATIF ?
    // Bouger la souris vers la droite donne un X positif.
    // Mais on veut tourner dans le sens inverse (convention FPS).
    // Pareil pour Y : souris vers le haut = regarder vers le haut.
    public void HandleMouseMotion(Vector2 relativeMotion)
    {
        // Mouvement horizontal souris → rotation Y (tourner sur soi)
        _rotationY += -relativeMotion.X * _mouseSensitivity;
            
        // Mouvement vertical souris → rotation X (hocher la tête)
        _rotationX += -relativeMotion.Y * _mouseSensitivity;
            
        // Limite la rotation verticale entre -90° et +90°
        // Sans ça, on pourrait faire un looping complet (bizarre)
        // Mathf.DegToRad convertit des degrés en radians
        _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            
        // Applique les rotations :
        // - Le corps tourne horizontalement (Y)
        // - La caméra tourne verticalement (X)
        _playerBody.Rotation = new Vector3(0, _rotationY, 0);
        _camera.Rotation = new Vector3(_rotationX, 0, 0);
    }
}