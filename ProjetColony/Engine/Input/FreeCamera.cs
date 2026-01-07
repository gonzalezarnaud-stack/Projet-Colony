// ============================================================================
// FREECAMERA.CS — Caméra libre pour explorer le monde
// ============================================================================
// Ce fichier est dans Engine/Input, donc il fait partie de ENGINE.
// Il dépend de Godot (Camera3D, Input, etc.)
//
// CE QU'IL FAIT :
// - Capture la souris au démarrage
// - Permet de regarder autour avec la souris
// - Permet de se déplacer avec le clavier (ZQSD + Espace + Shift)
//
// PROBLÈME RÉSOLU : GIMBAL LOCK
// Si on enchaîne RotateX et RotateY directement, les axes se mélangent
// et la caméra "roule" sur le côté. C'est un problème mathématique classique.
// Solution : on stocke les angles séparément (_rotationX, _rotationY)
// et on reconstruit la rotation à chaque mouvement de souris.
// Tous les jeux FPS utilisent cette technique.
// ============================================================================

using Godot;

namespace ProjetColony.Engine.Input;

// "partial" est requis par Godot pour les scripts C#
// "Camera3D" est la classe parente — notre FreeCamera EST une Camera3D
public partial class FreeCamera : Camera3D
{
    // ========================================================================
    // CONFIGURATION — Variables modifiables pour ajuster le comportement
    // ========================================================================
    
    // Vitesse de déplacement (unités par seconde)
    private float _moveSpeed = 10.0f;
    
    // Sensibilité de la souris (plus petit = plus lent)
    private float _mouseSensitivity = 0.002f;
    
    // ========================================================================
    // CONFIGURATION DES TOUCHES
    // ========================================================================
    // Stockées dans des variables pour pouvoir les modifier plus tard
    // (menu options, support AZERTY/QWERTY, etc.)
    
    private Key _keyForward = Key.Z;
    private Key _keyBackward = Key.S;
    private Key _keyLeft = Key.Q;
    private Key _keyRight = Key.D;
    private Key _keyUp = Key.Space;
    private Key _keyDown = Key.Shift;
    
    // ========================================================================
    // ÉTAT DE LA ROTATION
    // ========================================================================
    // On stocke les angles séparément pour éviter le Gimbal Lock
    // _rotationX = pitch (regarder haut/bas)
    // _rotationY = yaw (regarder gauche/droite)
    // Les valeurs sont en RADIANS (pas en degrés)
    
    private float _rotationX = 0f;
    private float _rotationY = 0f;

    // ========================================================================
    // _READY — Appelée une fois quand la caméra est prête
    // ========================================================================
    public override void _Ready()
    {
        base._Ready();
        
        // Capture la souris : elle devient invisible et bloquée au centre
        // C'est le comportement standard des jeux FPS
        // Note : "Godot.Input" car notre namespace contient aussi "Input"
        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
    }

    // ========================================================================
    // _INPUT — Appelée à chaque événement d'entrée (touche, souris, etc.)
    // ========================================================================
    // @event contient les informations sur l'événement
    // Le @ devant "event" est nécessaire car "event" est un mot réservé en C#
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        
        // Vérifie si l'événement est un mouvement de souris
        // "is" teste le type ET crée une variable "mouseMotion" si c'est vrai
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // mouseMotion.Relative = combien la souris a bougé depuis la dernière frame
            // .X = mouvement horizontal, .Y = mouvement vertical
            
            // Ajoute le mouvement horizontal à l'angle Y (regarder gauche/droite)
            // Le signe - inverse le mouvement pour qu'il soit naturel
            _rotationY += -mouseMotion.Relative.X * _mouseSensitivity;
            
            // Ajoute le mouvement vertical à l'angle X (regarder haut/bas)
            _rotationX += -mouseMotion.Relative.Y * _mouseSensitivity;
            
            // Limite l'angle vertical entre -90° et +90°
            // Sans ça, on pourrait faire un looping complet (bizarre)
            // Mathf.DegToRad convertit les degrés en radians
            _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            
            // Applique la rotation à la caméra
            // Vector3(_rotationX, _rotationY, 0) = pas de roulis (rotation sur Z)
            Rotation = new Vector3(_rotationX, _rotationY, 0);
        }
    }

    // ========================================================================
    // _PROCESS — Appelée à chaque frame (60 fois par seconde environ)
    // ========================================================================
    // delta = temps écoulé depuis la dernière frame (en secondes)
    // On l'utilise pour que le mouvement soit fluide quelle que soit la vitesse du PC
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Vecteur qui va contenir la direction du mouvement
        // Commence à zéro (pas de mouvement)
        var direction = Vector3.Zero;
        
        // Lit les entrées clavier avec notre méthode helper
        // Chaque valeur est -1, 0, ou 1
        var inputZ = GetInputAxis(_keyBackward, _keyForward);   // Avant/Arrière
        var inputX = GetInputAxis(_keyLeft, _keyRight);         // Gauche/Droite
        var inputY = GetInputAxis(_keyDown, _keyUp);            // Bas/Haut

        // Construit le vecteur direction en combinant les entrées
        // avec les axes locaux de la caméra
        
        // Transform.Basis.Z pointe vers l'ARRIÈRE de la caméra
        // Donc on met un - pour avancer quand inputZ est positif
        direction += -Transform.Basis.Z * inputZ;
        
        // Transform.Basis.X pointe vers la DROITE de la caméra
        direction += Transform.Basis.X * inputX;
        
        // Vector3.Up = (0, 1, 0), toujours vers le haut du monde
        // On utilise l'axe monde (pas l'axe caméra) pour monter/descendre
        direction += Vector3.Up * inputY;

        // Déplace la caméra
        // Position += ... ajoute à la position actuelle
        // * _moveSpeed = vitesse
        // * (float)delta = rend le mouvement indépendant du framerate
        // (float) convertit delta de double en float car Vector3 utilise des float
        Position += direction * _moveSpeed * (float)delta;
    }

    // ========================================================================
    // GETINPUTAXIS — Méthode helper pour lire deux touches opposées
    // ========================================================================
    // Retourne -1 si negative est pressée, +1 si positive est pressée, 0 sinon
    // Si les deux sont pressées, elles s'annulent (retourne 0)
    //
    // POURQUOI CETTE MÉTHODE ?
    // Évite de répéter des if/else pour chaque paire de touches
    // Code plus propre et plus facile à maintenir
    private float GetInputAxis(Key negative, Key positive)
    {
        float value = 0f;
        
        if (Godot.Input.IsKeyPressed(negative))
        {
            value -= 1;
        }
        if (Godot.Input.IsKeyPressed(positive))
        {
            value += 1;
        }
        
        return value;
    }
}