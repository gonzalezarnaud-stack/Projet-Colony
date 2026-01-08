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
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using System.Linq;

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
    // CONFIGURATION DU STEP CLIMBING
    // ========================================================================
    // Le "step climbing" permet de monter automatiquement sur les petits
    // obstacles (demi-blocs) sans avoir à sauter manuellement.
    //
    // POURQUOI C'EST NÉCESSAIRE ?
    // Quand le joueur est bloqué contre un mur, sa vélocité horizontale = 0.
    // Impossible de détecter qu'il "veut avancer" en regardant la vélocité.
    // On vérifie donc directement si les touches de déplacement sont pressées.
    //
    // COMMENT ÇA MARCHE ?
    // On lance deux raycasts horizontaux devant le joueur :
    //   1. Raycast BAS (à _stepCheckLow) → détecte s'il y a un obstacle
    //   2. Raycast HAUT (à _stepCheckHigh) → vérifie si c'est un bloc plein
    //
    // Si obstacle en bas MAIS rien en haut → c'est un demi-bloc → mini-saut !
    // Si obstacle en bas ET en haut → c'est un mur complet → on ne fait rien.

    // Hauteur du raycast bas (en mètres depuis les pieds du joueur)
    // 0.4m = à mi-hauteur d'un demi-bloc (qui fait 0.5m)
    // Trop bas (0.1m) → déclenchait sur le sol plat (bug de sautillement)
    private float _stepCheckLow = 0.4f;

    // Hauteur du raycast haut (en mètres depuis les pieds du joueur)
    // 0.9m = juste sous la hauteur d'un bloc plein (1m)
    // Si ce raycast touche quelque chose, c'est un mur → pas de step climbing
    private float _stepCheckHigh = 0.9f;

    // Force de l'impulsion verticale pour monter le demi-bloc
    // C'est une vélocité Y, pas une hauteur. 5.0 suffit pour 0.5m.
    private float _stepImpulse = 5.0f;

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

        // --------------------------------------------------------------------
        // ANGLE MAXIMUM POUR MARCHER SUR UNE PENTE
        // --------------------------------------------------------------------
        // FloorMaxAngle définit l'inclinaison maximale que Godot considère
        // comme "sol" (où IsOnFloor() retourne true).
        //
        // Par défaut : 45° — trop faible pour nos pentes !
        // Une FullSlope fait 45° exactement, donc le joueur glissait parfois.
        // 
        // À 50°, on a une marge de sécurité et le joueur marche sans problème
        // sur les FullSlope (45°) et DemiSlope (~26°).
        //
        // Note : DegToRad convertit les degrés en radians (Godot utilise les radians).
        FloorMaxAngle = Mathf.DegToRad(50);
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
        TryStepUp();
    }

    // ========================================================================
    // TRYSTEPUP — Monte automatiquement les petits obstacles
    // ========================================================================
    // Appelée à chaque frame physique après MoveAndSlide().
    // Détecte si le joueur est bloqué par un demi-bloc et le fait monter.
    //
    // ALGORITHME :
    // 1. Vérifier qu'on est au sol et qu'on essaie de bouger
    // 2. Vérifier qu'on n'est pas sur une pente (sinon ça interfère)
    // 3. Raycast bas : y a-t-il un obstacle devant ?
    // 4. Si oui, raycast haut : est-ce un bloc plein ou un demi-bloc ?
    // 5. Si demi-bloc (rien en haut) → impulsion verticale
    //
    // POURQUOI VÉRIFIER LES TOUCHES ET PAS LA VÉLOCITÉ ?
    // Quand on est contre un mur, Velocity.X et Velocity.Z sont à 0
    // (MoveAndSlide les annule). On ne saurait pas que le joueur pousse.
    // En vérifiant les touches, on sait qu'il VEUT avancer.
    private void TryStepUp()
    {
        // ----------------------------------------------------------------
        // CONDITION 1 : Être au sol
        // ----------------------------------------------------------------
        // Pas de step climbing en l'air (on ne vole pas !)
        if (!IsOnFloor()) return;
    
        // ----------------------------------------------------------------
        // CONDITION 2 : Le joueur essaie de se déplacer
        // ----------------------------------------------------------------
        // On lit directement les touches au lieu de regarder Velocity.
        // L'opérateur ternaire "condition ? siVrai : siFaux" remplace un if/else.
        // Résultat : -1, 0, ou 1 pour chaque axe.
        var inputZ = Godot.Input.IsKeyPressed(_keyForward) ? 1 : (Godot.Input.IsKeyPressed(_keyBackward) ? -1 : 0);
        var inputX = Godot.Input.IsKeyPressed(_keyRight) ? 1 : (Godot.Input.IsKeyPressed(_keyLeft) ? -1 : 0);
    
        // Si aucune touche de déplacement → rien à faire
        if (inputX == 0 && inputZ == 0) return;

        // ----------------------------------------------------------------
        // CONDITION 3 : Ne pas être sur une pente
        // ----------------------------------------------------------------
        // GetFloorNormal() retourne la direction perpendiculaire au sol.
        // Sol plat → normale = (0, 1, 0) → normale.Y = 1.0
        // Pente 45° → normale.Y ≈ 0.707
        //
        // Si normale.Y < 0.95, on est sur une pente → pas de step climbing
        // (sinon on sauterait bizarrement en montant une rampe)
        var floorNormal = GetFloorNormal();
        if (floorNormal.Y < 0.95f) return;

        // ----------------------------------------------------------------
        // CALCUL DE LA DIRECTION DE DÉPLACEMENT
        // ----------------------------------------------------------------
        // Combine les entrées avec les axes locaux du joueur.
        // -Transform.Basis.Z = devant, Transform.Basis.X = droite
        // Normalized() pour que la diagonale ne soit pas plus longue.
        var direction = -Transform.Basis.Z * inputZ + Transform.Basis.X * inputX;
        direction = direction.Normalized();

        // ----------------------------------------------------------------
        // RAYCAST BAS : Y a-t-il un obstacle ?
        // ----------------------------------------------------------------
        // On lance un rayon horizontal depuis les pieds + _stepCheckLow
        // vers l'avant sur 0.5m (demi-bloc de distance).
        var spaceState = GetWorld3D().DirectSpaceState;
        var from = GlobalPosition + new Vector3(0, _stepCheckLow, 0);
        var to = from + direction * 0.5f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
    
        // IMPORTANT : Exclure notre propre collider du raycast
        // Sinon le rayon touche le joueur lui-même !
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        var result = spaceState.IntersectRay(query);

        // Si le raycast bas touche quelque chose → obstacle détecté
        if (result.Count > 0)
        {
            // ------------------------------------------------------------
            // RAYCAST HAUT : Est-ce un bloc plein ou un demi-bloc ?
            // ------------------------------------------------------------
            // Même rayon mais plus haut (_stepCheckHigh).
            // Si ce rayon touche aussi → bloc plein → on ne monte pas.
            // Si ce rayon ne touche rien → demi-bloc → on peut monter !
            from = GlobalPosition + new Vector3(0, _stepCheckHigh, 0);
            to = from + direction * 0.5f;
            query = PhysicsRayQueryParameters3D.Create(from, to);
            query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
            result = spaceState.IntersectRay(query);
    
            // Rien en haut → c'est un demi-bloc → MINI-SAUT !
            if (result.Count == 0)
            {
                // On garde la vélocité horizontale et on ajoute une impulsion Y
                Velocity = new Vector3(Velocity.X, _stepImpulse, Velocity.Z);
            }
        }
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