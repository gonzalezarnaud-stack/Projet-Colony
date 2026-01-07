// ============================================================================
// PLAYER.CS — Contrôleur du joueur avec physique
// ============================================================================
// Ce fichier est dans Engine/Input, donc il fait partie de ENGINE.
// Il dépend de Godot (CharacterBody3D, Input, etc.)
//
// CE QU'IL FAIT :
// - Gère le mouvement du joueur (ZQSD)
// - Applique la gravité quand le joueur est en l'air
// - Permet de sauter avec Espace
// - Gère la rotation de la caméra avec la souris
// - Toggle capture souris avec Echap
// - Casse des blocs (clic gauche)
// - Pose des blocs (clic droit)
// - Sélection du type de bloc (touches 1, 2, 3)
// - Affiche la surbrillance du bloc visé
//
// DIFFÉRENCE AVEC FREECAMERA :
// - FreeCamera vole librement, traverse tout (mode debug/noclip)
// - Player marche au sol, subit la gravité, ne traverse pas les murs
//
// CHARACTERBODY3D :
// C'est un type de node Godot conçu pour les personnages.
// Il fournit :
// - Velocity : la vitesse actuelle
// - IsOnFloor() : détecte si on touche le sol
// - MoveAndSlide() : déplace en gérant les collisions
// ============================================================================

using System;
using Godot;
using ProjetColony.Core.World;
using ProjetColony.Core.Data;
using ProjetColony.Engine.Rendering;
using ProjetColony.Engine.UI;
using ProjetColony.Scenes;

namespace ProjetColony.Engine.Input;

public partial class Player : CharacterBody3D
{
    // ========================================================================
    // CONFIGURATION DU MOUVEMENT
    // ========================================================================
    
    // Vitesse de déplacement horizontal (unités par seconde)
    private float _moveSpeed = 5.0f;
    
    // Force du saut — vélocité verticale initiale quand on saute
    private float _jumpForce = 8.0f;
    
    // Gravité — accélération vers le bas (unités par seconde²)
    // Plus c'est grand, plus on tombe vite
    private float _gravity = 20.0f;

    // ========================================================================
    // CONFIGURATION DE LA SOURIS
    // ========================================================================
    
    // Sensibilité de la souris (plus petit = plus lent)
    private float _mouseSensitivity = 0.002f;
    
    // Angles de rotation actuels (en radians)
    // _rotationX = pitch (regarder haut/bas) — appliqué à la caméra
    // _rotationY = yaw (regarder gauche/droite) — appliqué au corps
    private float _rotationX = 0f;
    private float _rotationY = 0f;
    
    // ========================================================================
    // CONFIGURATION DES TOUCHES
    // ========================================================================
    
    private Key _keyForward = Key.Z;
    private Key _keyBackward = Key.S;
    private Key _keyLeft = Key.Q;
    private Key _keyRight = Key.D;
    private Key _keyJump = Key.Space;
    private Key _keyCrouch = Key.Shift;

    // ========================================================================
    // INVENTAIRE — Type de bloc sélectionné
    // ========================================================================
    // Le joueur peut choisir quel type de bloc poser avec les touches 1, 2, 3.
    // Cette variable stocke le MaterialId du bloc actuellement sélectionné.
    // Par défaut : pierre (Materials.Stone).
    //
    // ÉVOLUTION FUTURE :
    // - Un vrai système d'inventaire avec quantités limitées
    // - Interface graphique montrant le bloc sélectionné
    // - Plus de types de blocs
    private ushort _selectedMaterialId = Materials.Stone;
    
    // La forme du bloc sélectionné (touches 4, 5, 6, 7).
    // Par défaut : bloc plein (Shapes.Full).
    //
    // ÉVOLUTION FUTURE :
    // - Afficher la forme dans l'UI (UiInHand ou nouveau composant)
    // - Molette souris pour parcourir les formes
    // - Sous-grille 4×4×4 pour placement fin (MVP-B)
    private ushort _selectedShapeId = Shapes.Full;

    // La rotation du bloc sélectionné (touche R pour tourner).
    // Valeurs : 0, 1, 2, 3 = 0°, 90°, 180°, 270°
    // S'applique aux pentes et autres blocs directionnels.
    private ushort _selectedRotation = 0;

    // ========================================================================
    // RÉFÉRENCES
    // ========================================================================
    
    // La caméra est un node enfant du joueur.
    // On la stocke pour pouvoir modifier sa rotation verticale.
    private Camera3D _camera;

    // Portée d'interaction — distance max pour casser/poser des blocs
    private float _interactionRange = 5.0f;

    // ------------------------------------------------------------------------
    // SURBRILLANCE DU BLOC VISÉ
    // ------------------------------------------------------------------------
    // BlockHighlight est un cube semi-transparent qui montre quel bloc
    // on regarde. Il est créé comme enfant du Player mais utilise
    // GlobalPosition pour rester fixe dans le monde.
    private BlockHighLight _blockHighLight;

    // ========================================================================
    // _READY — Initialisation
    // ========================================================================
    public override void _Ready()
    {
        base._Ready();
        
        // Capture la souris au démarrage (invisible et centrée)
        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
        
        // Récupère la caméra enfant par son nom
        // Le nom doit correspondre exactement au nom du node dans la scène
        _camera = GetNode<Camera3D>("Camera3D");

        // Crée le highlight et l'ajoute comme enfant du Player
        // On l'ajoute au Player plutôt qu'à Main car le Player est prêt
        // avant Main (ordre d'initialisation de Godot).
        _blockHighLight = new BlockHighLight();
        AddChild(_blockHighLight);
    }

    // ========================================================================
    // _INPUT — Gestion des événements d'entrée
    // ========================================================================
    // Appelée à chaque événement (touche, souris, etc.)
    public override void _Input(InputEvent @event)
    {
        // ====================================================================
        // ROTATION AVEC LA SOURIS
        // ====================================================================
        // Seulement si la souris est capturée (en mode jeu)
        if (@event is InputEventMouseMotion mouseMotion && Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured)
        {
            // Mise à jour des angles de rotation
            // Relative.X (mouvement horizontal souris) → rotation Y (tourner sur soi)
            // Relative.Y (mouvement vertical souris) → rotation X (hocher la tête)
            _rotationY += -mouseMotion.Relative.X * _mouseSensitivity;
            _rotationX += -mouseMotion.Relative.Y * _mouseSensitivity;
            
            // Limite la rotation verticale pour ne pas faire de looping
            _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            
            // Applique la rotation horizontale au CORPS du joueur
            // Ainsi, le joueur se déplace dans la direction où il regarde
            Rotation = new Vector3(0, _rotationY, 0);
            
            // Applique la rotation verticale à la CAMÉRA seulement
            // Le corps reste droit, seule la vue monte/descend
            _camera.Rotation = new Vector3(_rotationX, 0, 0);
        }

        // ====================================================================
        // CLICS SOURIS — CASSER ET POSER DES BLOCS
        // ====================================================================
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured)
        {
            // ----------------------------------------------------------------
            // CLIC GAUCHE — CASSER UN BLOC
            // ----------------------------------------------------------------
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                // Lance un rayon depuis la caméra
                var result = Raycast();
                
                // Si le rayon touche quelque chose
                if (result.Count > 0)
                {
                    // Récupère le collider (StaticBody3D) touché
                    var collider = (Node3D)result["collider"];
                    
                    // La position du bloc = position du collider
                    // On utilise RoundToInt car la position devrait être entière
                    // mais peut avoir de petites erreurs flottantes
                    var blockX = Mathf.RoundToInt(collider.GlobalPosition.X);
                    var blockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
                    var blockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);

                    // Met le bloc à air dans les données (World gère les conversions)
                    Main.World.SetBlock(blockX, blockY, blockZ, new Block{MaterialId = Materials.Air});

                    // Supprime le visuel du bloc
                    collider.QueueFree();
                }
            }
            // ----------------------------------------------------------------
            // CLIC DROIT — POSER UN BLOC
            // ----------------------------------------------------------------
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                var result = Raycast();
                
                if (result.Count > 0)
                {
                    // La normale indique la direction de la surface touchée
                    // Ex: (0, 1, 0) = on a touché le dessus du bloc
                    var hitNormal = (Vector3)result["normal"];
                    var collider = (Node3D)result["collider"];

                    // Position du bloc touché (celui sur lequel on clique)
                    var hitBlockX = Mathf.RoundToInt(collider.GlobalPosition.X);
                    var hitBlockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
                    var hitBlockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);
                    
                    // Position du nouveau bloc = bloc touché + normale
                    // Si normale = (0, 1, 0), le nouveau bloc est AU-DESSUS
                    // Si normale = (1, 0, 0), le nouveau bloc est À DROITE
                    var blockX = hitBlockX + Mathf.RoundToInt(hitNormal.X);
                    var blockY = hitBlockY + Mathf.RoundToInt(hitNormal.Y);
                    var blockZ = hitBlockZ + Mathf.RoundToInt(hitNormal.Z);
                    
                    // Vérifie que la position est vide (air) avant de poser
                    if (Main.World.GetBlock(blockX, blockY, blockZ).MaterialId == Materials.Air)
                    {
                        // Met le bloc sélectionné dans les données
                        var block = new Block{MaterialId = _selectedMaterialId, ShapeId = _selectedShapeId, RotationId = _selectedRotation};
                        Main.World.SetBlock(blockX, blockY, blockZ, block);
                        
                        // Crée le visuel via BlockRenderer (évite la duplication de code)
                        var staticBody = BlockRenderer.CreateBlock(_selectedMaterialId, _selectedShapeId, _selectedRotation, new Vector3(blockX, blockY, blockZ));

                        // Ajoute à la scène principale
                        Main.Instance.AddChild(staticBody);
                    }
                }
            }
        }

        // ====================================================================
        // TOUCHES CLAVIER
        // ====================================================================
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // ----------------------------------------------------------------
            // ECHAP — Toggle capture souris
            // ----------------------------------------------------------------
            if (keyEvent.Keycode == Key.Escape)
            {
                // Si capturée → libérer (pour cliquer ailleurs, quitter, etc.)
                if (Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured)
                {
                    Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Visible;
                }
                // Si visible → recapturer (pour rejouer)
                else
                {
                    Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
                }             
            }
            // ----------------------------------------------------------------
            // TOUCHE ROTATION BLOC — Rotation du bloc en main
            // ----------------------------------------------------------------
            else if (keyEvent.Keycode == Key.R)
            {
                _selectedRotation += 1;
                _selectedRotation = (ushort)(_selectedRotation % 4);
                Main.UiInHand.SetRotation(_selectedRotation);      
            }

            // ----------------------------------------------------------------
            // TOUCHES 1, 2, 3 — Sélection du type de bloc
            // ----------------------------------------------------------------
            // Change le bloc qui sera posé au prochain clic droit.
            // Pas d'interface visuelle pour l'instant, mais ça fonctionne !
            else if (keyEvent.Keycode == Key.Key1)
            {
                _selectedMaterialId = Materials.Stone;
                Main.UiInHand.SetMaterial("Stone");
            }
            else if (keyEvent.Keycode == Key.Key2)
            {
                _selectedMaterialId = Materials.Dirt;
                Main.UiInHand.SetMaterial("Dirt");
            }
            else if (keyEvent.Keycode == Key.Key3)
            {
                _selectedMaterialId = Materials.Grass;
                Main.UiInHand.SetMaterial("Grass");
            }
            // ----------------------------------------------------------------
            // TOUCHES 4, 5, 6, 7 — Sélection de la forme
            // ----------------------------------------------------------------
            // Change la forme du bloc qui sera posé au prochain clic droit.
            // Fonctionne indépendamment du matériau (pierre en demi-bloc, etc.)
            else if (keyEvent.Keycode == Key.Key4)
            {
                _selectedShapeId = Shapes.Full;
                Main.UiInHand.SetShape("Full");
            }
            else if (keyEvent.Keycode == Key.Key5)
            {
                _selectedShapeId = Shapes.Demi;
                Main.UiInHand.SetShape("Demi");
            }
            else if (keyEvent.Keycode == Key.Key6)
            {
                _selectedShapeId = Shapes.Post;
                Main.UiInHand.SetShape("Post");
            }
            else if (keyEvent.Keycode == Key.Key7)
            {
                _selectedShapeId = Shapes.FullSlope;
                Main.UiInHand.SetShape("FullSlope");
            }
            else if (keyEvent.Keycode == Key.Key8)
            {
                _selectedShapeId = Shapes.DemiSlope;
                Main.UiInHand.SetShape("DemiSlope");
            }
        }
    }

    // ========================================================================
    // _PROCESS — Mise à jour à chaque frame (pour le visuel)
    // ========================================================================
    // Appelée à chaque frame (~60 fois par seconde).
    // Utilisée ici pour mettre à jour le highlight du bloc visé.
    //
    // DIFFÉRENCE AVEC _PHYSICSPROCESS :
    // - _Process : pour les trucs visuels, synchro avec l'affichage
    // - _PhysicsProcess : pour la physique, synchro avec le moteur physique
    public override void _Process(double delta)
    {
        base._Process(delta);

        // Lance un rayon pour voir quel bloc on vise
        var result = Raycast();

        if (result.Count > 0)
        {
            // On vise un bloc → récupère sa position et met à jour le highlight
            var collider = (Node3D)result["collider"];
            var blockX = Mathf.RoundToInt(collider.GlobalPosition.X);
            var blockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
            var blockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);
            
            _blockHighLight.UpdateHighLight(new Vector3(blockX, blockY, blockZ));
        }
        else
        {
            // On ne vise rien → cache le highlight
            _blockHighLight.UpdateHighLight(null);
        }
    }

    // ========================================================================
    // _PHYSICSPROCESS — Mise à jour physique
    // ========================================================================
    // Appelée à intervalle fixe (60 fois par seconde par défaut)
    // Synchronisée avec le moteur physique — idéal pour les mouvements
    // qui impliquent des collisions.
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // Lecture des entrées clavier
        var inputZ = GetInputAxis(_keyBackward, _keyForward);
        var inputX = GetInputAxis(_keyLeft, _keyRight);

        // Calcul de la direction de mouvement
        // On utilise Transform.Basis pour se déplacer dans la direction
        // où regarde le joueur (pas la caméra, sinon on volerait)
        var direction = Vector3.Zero;
        direction += -Transform.Basis.Z * inputZ;  // Avant/arrière
        direction += Transform.Basis.X * inputX;   // Gauche/droite
        
        // Normalisation pour que la diagonale ne soit pas plus rapide
        direction = direction.Normalized();

        // Récupère la vélocité actuelle (pour conserver la composante Y)
        var velocity = Velocity;
        
        // Applique le mouvement horizontal
        velocity.X = direction.X * _moveSpeed;
        velocity.Z = direction.Z * _moveSpeed;

        // Gravité — appliquée seulement si on n'est pas au sol
        if (!IsOnFloor())
        {
            // On soustrait car Y négatif = vers le bas
            velocity.Y -= _gravity * (float)delta;
        }
        // Saut — seulement si on est au sol
        else if (Godot.Input.IsKeyPressed(_keyJump))
        {
            velocity.Y = _jumpForce;
        }

        // Applique la vélocité calculée
        Velocity = velocity;
        
        // Déplace le joueur en gérant les collisions automatiquement
        // MoveAndSlide() utilise Velocity et ajuste le mouvement
        // pour glisser le long des surfaces au lieu de s'arrêter net
        MoveAndSlide();
    }

    // ========================================================================
    // GETINPUTAXIS — Helper pour lire deux touches opposées
    // ========================================================================
    // Retourne -1 si negative pressée, +1 si positive pressée, 0 sinon
    // Permet d'écrire du code propre sans forêt de if
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

    // ========================================================================
    // RAYCAST — Lance un rayon depuis la caméra
    // ========================================================================
    // Retourne un Dictionary contenant :
    // - "position" : point exact où le rayon touche
    // - "normal" : direction de la surface touchée
    // - "collider" : l'objet (Node) touché
    // Retourne un Dictionary vide si rien n'est touché
    private Godot.Collections.Dictionary Raycast()
    {
        // Récupère l'espace physique pour faire des requêtes
        var spaceState = GetWorld3D().DirectSpaceState;
        
        // Point de départ = position de la caméra
        var from = _camera.GlobalPosition;
        
        // Direction = là où regarde la caméra (axe -Z local)
        var direction = -_camera.GlobalTransform.Basis.Z;
        
        // Point d'arrivée = départ + direction * portée
        var to = from + direction * _interactionRange;
        
        // Crée les paramètres du raycast
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        
        // Lance le rayon et récupère le résultat
        var result = spaceState.IntersectRay(query);

        return result;
    }
}