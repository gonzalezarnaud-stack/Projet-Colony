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
// - Sélection de la forme (touches 4, 5, 6, 7, 8, 9, 0)
// - Rotation du bloc (touches R et T)
// - Mode fin avec sous-grille 3×3×3 (touche F)
//
// CHARACTERBODY3D :
// C'est un type de node Godot conçu pour les personnages.
// Il fournit :
// - Velocity : la vitesse actuelle
// - IsOnFloor() : détecte si on touche le sol
// - MoveAndSlide() : déplace en gérant les collisions
// ============================================================================

using Godot;
using ProjetColony.Core.World;
using ProjetColony.Core.Data;
using ProjetColony.Core.Data.Registries;
using ProjetColony.Core.Input;
using ProjetColony.Core.Building;
using ProjetColony.Engine.Rendering;
using ProjetColony.Engine.Building;
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
    private float _gravity = 20.0f;

    // ========================================================================
    // CONFIGURATION DU STEP CLIMBING
    // ========================================================================
    // Le "step climbing" permet de monter automatiquement sur les petits
    // obstacles (tiers de blocs) sans avoir à sauter manuellement.
    //
    // On lance deux raycasts horizontaux devant le joueur :
    //   1. Raycast BAS → détecte s'il y a un obstacle
    //   2. Raycast HAUT → vérifie si c'est un bloc plein ou petit
    //
    // Si obstacle en bas MAIS rien en haut → petit bloc → mini-saut !
    
    private float _stepCheckLow = 0.4f;   // Hauteur du raycast bas
    private float _stepCheckHigh = 0.9f;  // Hauteur du raycast haut
    private float _stepImpulse = 5.0f;    // Force du mini-saut

    // ========================================================================
    // CONFIGURATION DE LA SOURIS
    // ========================================================================

    private CameraController _cameraController;

    // ========================================================================
    // RÉFÉRENCES
    // ========================================================================
    
    // La caméra est un node enfant du joueur
    private Camera3D _camera;
    
    // Portée d'interaction — distance max pour casser/poser des blocs
    private float _interactionRange = 5.0f;

    // Highlight — le cube semi-transparent qui montre où on va poser
    private BlockHighLight _blockHighLight;

    private BuildingState _buildingState;
    private BuildingController _buildingController;
    private PlacementCalculator _placementCalculator;
    private BuildingPreview _buildingPreview;

    // ========================================================================
    // _READY — Initialisation (appelée une fois au démarrage)
    // ========================================================================
    public override void _Ready()
    {
        base._Ready();
        
        // Capture la souris (invisible et centrée)
        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
        
        // Récupère la caméra enfant
        _camera = GetNode<Camera3D>("Camera3D");
        _cameraController = new CameraController(_camera, this);

        // Crée le highlight et l'ajoute comme enfant du Player
        _blockHighLight = new BlockHighLight();
        AddChild(_blockHighLight);

        // Angle max pour marcher sur une pente (50° permet les FullSlope à 45°)
        FloorMaxAngle = Mathf.DegToRad(50);

        _buildingState = new BuildingState();
        _placementCalculator = new PlacementCalculator();
        _buildingController = new BuildingController(Main.World, _placementCalculator, _buildingState);
    
        _buildingPreview = new BuildingPreview(
        _camera,
        this,
        _buildingState,
        _placementCalculator,
        _blockHighLight
        );
    }

    // ========================================================================
    // _INPUT — Gestion des événements (touche, souris, etc.)
    // ========================================================================
    public override void _Input(InputEvent @event)
    {
        // ====================================================================
        // ROTATION AVEC LA SOURIS
        // ====================================================================
        if (@event is InputEventMouseMotion mouseMotion && Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured)
        {
            _cameraController.HandleMouseMotion(mouseMotion.Relative);
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
                var result = Raycast();
    
                if (result.Count > 0)
                {
                    var collider = (Node3D)result["collider"];
        
                    // Récupère la position du voxel
                    var blockX = Mathf.RoundToInt(collider.GlobalPosition.X);
                    var blockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
                    var blockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);

                    byte subX = 0;
                    byte subY = 0;
                    byte subZ = 0;

                    if (_buildingState.IsFineMode)
                    {
                        // MODE FIN — Supprimer seulement le bloc visé
                        // On récupère sa sous-position via les métadonnées
                        subX = collider.HasMeta("SubX") ? (byte)(int)collider.GetMeta("SubX") : (byte)0;
                        subY = collider.HasMeta("SubY") ? (byte)(int)collider.GetMeta("SubY") : (byte)0;
                        subZ = collider.HasMeta("SubZ") ? (byte)(int)collider.GetMeta("SubZ") : (byte)0;
                    }
                    
                    bool success = _buildingController.RemoveBlock(
                        blockX, blockY, blockZ,
                        subX, subY, subZ);
                    if(success)
                    {
                        collider.QueueFree();
                    }
                }
            }
            // ----------------------------------------------------------------
            // CLIC DROIT — POSER UN BLOC
            // ----------------------------------------------------------------
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                // ============================================================
                // CAS 1 : Mode fin, pas de surface sélectionnée
                // → On sélectionne la surface de travail
                // ============================================================
                if (_buildingState.IsFineMode && !_buildingState.HasSelectedSurface)
                {
                    var result = Raycast();
                    if (result.Count > 0)
                    {
                        var hitNormal = (Vector3)result["normal"];
                        var collider = (Node3D)result["collider"];

                        // Position du bloc visé
                        var hitBlockX = Mathf.RoundToInt(collider.GlobalPosition.X);
                        var hitBlockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
                        var hitBlockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);

                        // Vérifier si le bloc visé peut contenir plusieurs blocs
                        int hitShapeId = collider.HasMeta("ShapeId") ? (int)collider.GetMeta("ShapeId") : Shapes.Full;
                        var hitShapeDef = ShapeRegistry.Get((ushort)hitShapeId);
                        bool hitCanStack = hitShapeDef != null && hitShapeDef.CanStackInVoxel;

                        if (hitCanStack)
                        {
                            // --------------------------------------------------
                            // BLOC STACKABLE (poteau) — Rester dans le même voxel
                            // et se coller au bloc existant
                            // --------------------------------------------------
                            
                            // Récupérer la sous-position du bloc visé
                            int hitSubX = collider.HasMeta("SubX") ? (int)collider.GetMeta("SubX") : PlacementCalculator.SubCenter;
                            int hitSubY = collider.HasMeta("SubY") ? (int)collider.GetMeta("SubY") : PlacementCalculator.SubCenter;
                            int hitSubZ = collider.HasMeta("SubZ") ? (int)collider.GetMeta("SubZ") : PlacementCalculator.SubCenter;

                            // Décaler d'un cran selon la normale
                            // Ex: normale +X → on se décale de 1 en X
                            int newSubX = hitSubX + Mathf.RoundToInt(hitNormal.X);
                            int newSubY = hitSubY + Mathf.RoundToInt(hitNormal.Y);
                            int newSubZ = hitSubZ + Mathf.RoundToInt(hitNormal.Z);

                            // Voxel de destination (pour l'instant le même)
                            _buildingState.SurfaceVoxelX = hitBlockX;
                            _buildingState.SurfaceVoxelY = hitBlockY;
                            _buildingState.SurfaceVoxelZ = hitBlockZ;

                            // Si on dépasse la grille 3×3, changer de voxel
                            // Ex: sub = 4 → sub = 1 du voxel suivant
                            int voxelOffsetX;
                            int voxelOffsetY;
                            int voxelOffsetZ;

                            newSubX = _placementCalculator.ClampSub(newSubX, out voxelOffsetX);
                            newSubY = _placementCalculator.ClampSub(newSubY, out voxelOffsetY);
                            newSubZ = _placementCalculator.ClampSub(newSubZ, out voxelOffsetZ);

                            _buildingState.SurfaceVoxelX = hitBlockX + voxelOffsetX;
                            _buildingState.SurfaceVoxelY = hitBlockY + voxelOffsetY;
                            _buildingState.SurfaceVoxelZ = hitBlockZ + voxelOffsetZ;
                            _buildingState.HasFixedSub = true;
                        }
                        else
                        {
                            // --------------------------------------------------
                            // BLOC PLEIN — Aller dans le voxel adjacent
                            // --------------------------------------------------
                            _buildingState.SurfaceVoxelX = hitBlockX + Mathf.RoundToInt(hitNormal.X);
                            _buildingState.SurfaceVoxelY = hitBlockY + Mathf.RoundToInt(hitNormal.Y);
                            _buildingState.SurfaceVoxelZ = hitBlockZ + Mathf.RoundToInt(hitNormal.Z);
                            _buildingState.HasFixedSub = false;
                        }

                        _buildingState.SurfaceNormalX = hitNormal.X;
                        _buildingState.SurfaceNormalY = hitNormal.Y;
                        _buildingState.SurfaceNormalZ = hitNormal.Z;
                        _buildingState.HasSelectedSurface = true;
                    }
                }
                // ============================================================
                // CAS 2 : On peut poser (mode normal ou mode fin avec surface)
                // → On pose le bloc
                // ============================================================
                else if (_buildingPreview.CanPlace)
                {
                    // Vérifie qu'il y a de la place (ou mode fin = plusieurs blocs possibles)
                    if (Main.World.GetBlocks(_buildingPreview.NextBlockX, _buildingPreview.NextBlockY, _buildingPreview.NextBlockZ).Count == 0 || _buildingState.IsFineMode)
                    {
                        // Crée le bloc avec ses propriétés
                        var block = new Block{
                            MaterialId = _buildingState.SelectedMaterialId,
                            ShapeId = _buildingState.SelectedShapeId,
                            RotationId = _buildingState.SelectedRotationY,
                            RotationX = _buildingState.SelectedRotationX,
                            SubX = _buildingPreview.NextSubX,
                            SubY = _buildingPreview.NextSubY,
                            SubZ = _buildingPreview.NextSubZ
                        };

                        // Ajoute aux données et crée le visuel
                        bool success= _buildingController.PlaceBlock(
                            _buildingPreview.NextBlockX, 
                            _buildingPreview.NextBlockY,
                            _buildingPreview.NextBlockZ,
                            block
                        );
                        if(success)
                        {
                            var staticBody = BlockRenderer.CreateBlock(
                                block,
                                new Vector3(
                                    _buildingPreview.NextBlockX,
                                    _buildingPreview.NextBlockY,
                                    _buildingPreview.NextBlockZ)
                                );
                            Main.Instance.AddChild(staticBody);
                        }  
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
            // ECHAP — Libérer/capturer la souris
            // ----------------------------------------------------------------
            if (keyEvent.Keycode == Key.Escape)
            {
                if (Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured)
                {
                    Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Visible;
                }
                else
                {
                    Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
                }             
            }
            // ----------------------------------------------------------------
            // R — Rotation Y du bloc (0°, 90°, 180°, 270°)
            // ----------------------------------------------------------------
            else if (keyEvent.Keycode == Key.R)
            {
                _buildingState.SelectedRotationY = (ushort)((_buildingState.SelectedRotationY + 1) % 4);
                Main.UiInHand.SetRotation(_buildingState.SelectedRotationY);      
            }
            // ----------------------------------------------------------------
            // T — Rotation X du bloc (coucher le bloc)
            // ----------------------------------------------------------------
            else if (keyEvent.Keycode == Key.T)
            {
                _buildingState.SelectedRotationX = (ushort)((_buildingState.SelectedRotationX + 1) % 4);
                Main.UiInHand.SetRotationX(_buildingState.SelectedRotationX);
            }
            // ----------------------------------------------------------------
            // F — Mode fin / Désélectionner surface
            // ----------------------------------------------------------------
            else if (keyEvent.Keycode == Key.F)
            {
                if (_buildingState.IsFineMode && _buildingState.HasSelectedSurface)
                {
                    // Surface sélectionnée → la désélectionner
                    _buildingState.HasSelectedSurface = false;
                    _buildingState.HasFixedSub = false;
                }
                else
                {
                    // Sinon → toggle mode fin
                    _buildingState.IsFineMode = !_buildingState.IsFineMode;
                    _buildingState.HasSelectedSurface = false;
                    _buildingState.HasFixedSub = false;
                    Main.UiInHand.SetFineMode(_buildingState.IsFineMode);
                }
            }
            // ----------------------------------------------------------------
            // 1, 2, 3 — Sélection du matériau
            // ----------------------------------------------------------------
            else if (keyEvent.Keycode == Key.Key1)
            {
                _buildingState.SelectedMaterialId = Materials.Stone;
                Main.UiInHand.SetMaterial("Stone");
            }
            else if (keyEvent.Keycode == Key.Key2)
            {
                _buildingState.SelectedMaterialId = Materials.Dirt;
                Main.UiInHand.SetMaterial("Dirt");
            }
            else if (keyEvent.Keycode == Key.Key3)
            {
                _buildingState.SelectedMaterialId = Materials.Grass;
                Main.UiInHand.SetMaterial("Grass");
            }
            // ----------------------------------------------------------------
            // 4-0 — Sélection de la forme
            // ----------------------------------------------------------------
            else if (keyEvent.Keycode == Key.Key4)
            {
                _buildingState.SelectedShapeId = Shapes.Full;
                Main.UiInHand.SetShape("Full");
            }
            else if (keyEvent.Keycode == Key.Key5)
            {
                _buildingState.SelectedShapeId = Shapes.Tiers;
                Main.UiInHand.SetShape("Tiers");
            }
            else if (keyEvent.Keycode == Key.Key6)
            {
                _buildingState.SelectedShapeId = Shapes.DeuxTiers;
                Main.UiInHand.SetShape("DeuxTiers");
            }
            else if (keyEvent.Keycode == Key.Key7)
            {
                _buildingState.SelectedShapeId = Shapes.Post;
                Main.UiInHand.SetShape("Post");
            }
            else if (keyEvent.Keycode == Key.Key8)
            {
                _buildingState.SelectedShapeId = Shapes.FullSlope;
                Main.UiInHand.SetShape("FullSlope");
            }
            else if (keyEvent.Keycode == Key.Key9)
            {
                _buildingState.SelectedShapeId = Shapes.TiersSlope;
                Main.UiInHand.SetShape("TiersSlope");
            }
            else if (keyEvent.Keycode == Key.Key0)
            {
                _buildingState.SelectedShapeId = Shapes.DeuxTiersSlope;
                Main.UiInHand.SetShape("DeuxTiersSlope");
            }
        }
    }
    // ========================================================================
    // _PROCESS — Appelée à chaque frame (~60 fois par seconde)
    // ========================================================================
    // Met à jour la position du highlight selon ce qu'on vise.
    public override void _Process(double delta)
    {
        base._Process(delta);
        _buildingPreview.Update();
    }
    // ========================================================================
    // _PHYSICSPROCESS — Mise à jour physique (60 fois par seconde)
    // ========================================================================
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // Lecture des entrées clavier
        var inputZ = InputReader.GetAxis(GameAxis.MoveZ);
        var inputX = InputReader.GetAxis(GameAxis.MoveX);

        // Direction de mouvement (relative au joueur)
        var direction = Vector3.Zero;
        direction += -Transform.Basis.Z * inputZ;
        direction += Transform.Basis.X * inputX;
        direction = direction.Normalized();

        // Récupère la vélocité actuelle
        var velocity = Velocity;
        
        // Mouvement horizontal
        velocity.X = direction.X * _moveSpeed;
        velocity.Z = direction.Z * _moveSpeed;

        // Gravité (seulement en l'air)
        if (!IsOnFloor())
        {
            velocity.Y -= _gravity * (float)delta;
        }
        // Saut (seulement au sol)
        else if (InputReader.IsActionPressed(GameAction.Jump))
        {
            velocity.Y = _jumpForce;
        }

        Velocity = velocity;
        MoveAndSlide();
        TryStepUp();
    }

    // ========================================================================
    // TRYSTEPUP — Monte automatiquement les petits obstacles
    // ========================================================================
    private void TryStepUp()
    {
        if (!IsOnFloor()) return;

        var inputZ = InputReader.GetAxis(GameAxis.MoveZ);    
        var inputX = InputReader.GetAxis(GameAxis.MoveX);

        if (inputX == 0 && inputZ == 0) return;

        var floorNormal = GetFloorNormal();
        if (floorNormal.Y < 0.95f) return;

        var direction = -Transform.Basis.Z * inputZ + Transform.Basis.X * inputX;
        direction = direction.Normalized();

        var spaceState = GetWorld3D().DirectSpaceState;
        var from = GlobalPosition + new Vector3(0, _stepCheckLow, 0);
        var to = from + direction * 0.5f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            from = GlobalPosition + new Vector3(0, _stepCheckHigh, 0);
            to = from + direction * 0.5f;
            query = PhysicsRayQueryParameters3D.Create(from, to);
            query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
            result = spaceState.IntersectRay(query);
    
            if (result.Count == 0)
            {
                Velocity = new Vector3(Velocity.X, _stepImpulse, Velocity.Z);
            }
        }
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
        var spaceState = GetWorld3D().DirectSpaceState;
        var from = _camera.GlobalPosition;
        var direction = -_camera.GlobalTransform.Basis.Z;
        var to = from + direction * _interactionRange;
        
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        
        return spaceState.IntersectRay(query);
    }
}