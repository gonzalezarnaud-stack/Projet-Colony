// ============================================================================
// BUILDINGPREVIEW.CS — Calcul et affichage de la preview de placement
// ============================================================================
// Ce fichier est dans Engine/Building, donc il fait partie de ENGINE.
// Il dépend de Godot (Camera3D, Vector3, Raycast, etc.)
//
// C'EST QUOI CETTE CLASSE ?
// Elle calcule OÙ le bloc va être posé et affiche le highlight (preview).
// À chaque frame, elle :
//   1. Détermine quelle position le joueur vise
//   2. Calcule la sous-position (mode fin)
//   3. Met à jour le highlight semi-transparent
//   4. Stocke les résultats pour que Player puisse les lire au clic
//
// DEUX CAS EN MODE FIN :
//   - Bloc PLEIN visé : calculer sub depuis hitPoint (où on regarde sur la face)
//   - Bloc STACKABLE visé (poteau) : se coller au bloc existant
//
// LOGIQUE POUR BLOCS STACKABLES :
// Pour chaque axe, on vérifie la TAILLE du bloc visé :
//   - PETIT (< 1) sur cet axe → décaler le sub, rester dans le même voxel
//   - GRAND (≥ 1) sur cet axe → aller au voxel adjacent
//
// POURQUOI UNE CLASSE SÉPARÉE ?
// Avant, ce code était dans Player._Process() — des centaines de lignes !
// En l'extrayant :
//   - Player.cs devient plus court et lisible
//   - BuildingPreview a UNE seule responsabilité (principe SRP)
//   - On peut tester/modifier la preview sans toucher au mouvement
//
// ARCHITECTURE :
// Player appelle BuildingPreview.Update() à chaque frame.
// Quand le joueur clique, Player lit les propriétés publiques
// (CanPlace, NextBlockX, NextSubX, etc.) pour savoir où poser.
// ============================================================================

using Godot;                            // On importe Godot pour Camera3D, Vector3, CharacterBody3D, etc.
using ProjetColony.Core.Building;       // On importe Core.Building pour BuildingState et PlacementCalculator
using ProjetColony.Core.Data;           // On importe Core.Data pour Shapes (constantes des formes)
using ProjetColony.Core.Data.Registries; // On importe pour ShapeRegistry (vérifier CanStackInVoxel)
using ProjetColony.Engine.Rendering;    // On importe Engine.Rendering pour BlockHighLight

namespace ProjetColony.Engine.Building;

public class BuildingPreview
{
    // ========================================================================
    // RÉFÉRENCES — Objets dont BuildingPreview a besoin
    // ========================================================================
    // Ces objets sont INJECTÉS via le constructeur (pas créés ici).
    // C'est le principe d'INJECTION DE DÉPENDANCES :
    //   - Plus flexible (on peut changer les objets)
    //   - Plus testable (on peut injecter des faux objets pour les tests)

    // La caméra — pour savoir où le joueur regarde
    private Camera3D _camera;

    // Le joueur — pour exclure son collider du raycast
    // CharacterBody3D (pas Node3D) car on a besoin de GetRid()    
    private CharacterBody3D _player;

    // L'état de construction — mode fin ? forme choisie ?
    private BuildingState _buildingState;

    // Le calculateur — méthodes pour convertir fractions en sub, etc.    
    private PlacementCalculator _placementCalculator;

    // Le highlight — le cube semi-transparent qu'on affiche
    private BlockHighLight _blockHighLight;
    
    // ========================================================================
    // RÉSULTATS DU CALCUL — Position où le bloc sera posé
    // ========================================================================
    // Ces variables sont remplies par Update() à chaque frame.
    // Player les lit quand le joueur clique pour poser un bloc.
    
    // Position du VOXEL (coordonnées entières dans le monde)
    private int _nextBlockX;
    private int _nextBlockY;
    private int _nextBlockZ;

    // Sous-position dans la grille 3×3×3 (0 = centré, 1-3 = position fine)
    private byte _nextSubX;
    private byte _nextSubY;
    private byte _nextSubZ;

    // True si on peut poser un bloc ici, false sinon
    private bool _canPlace;

    // DEBUG : pour n'afficher qu'une fois par changement de bloc visé ou normale
    private int _lastLogBlockX = int.MinValue;
    private int _lastLogBlockY = int.MinValue;
    private int _lastLogBlockZ = int.MinValue;
    private Vector3 _lastLogNormal = Vector3.Zero;
    
    // ========================================================================
    // CONFIGURATION
    // ========================================================================
    
    // Distance maximale pour interagir avec les blocs (en mètres)
    private float _interactionRange = 5.0f;

    // ========================================================================
    // PROPRIÉTÉS PUBLIQUES — Pour que Player puisse lire les résultats
    // ========================================================================
    // Ces propriétés sont en LECTURE SEULE (pas de set, seulement get).
    // Player peut lire : _buildingPreview.CanPlace
    // Mais ne peut pas écrire : _buildingPreview.CanPlace = true; // ERREUR
    //
    // SYNTAXE "=>" :
    // C'est une "expression-bodied property" — une écriture raccourcie.
    //   public bool CanPlace => _canPlace;
    // Est équivalent à :
    //   public bool CanPlace { get { return _canPlace; } }
    // C'est juste plus court pour les propriétés simples.
    public bool CanPlace => _canPlace;
    public int NextBlockX => _nextBlockX;
    public int NextBlockY => _nextBlockY;
    public int NextBlockZ => _nextBlockZ;
    public byte NextSubX => _nextSubX;
    public byte NextSubY => _nextSubY;
    public byte NextSubZ => _nextSubZ;

    // ========================================================================
    // CONSTRUCTEUR — Reçoit toutes les dépendances
    // ========================================================================
    // Un constructeur est une méthode spéciale qui :
    //   - A le MÊME NOM que la classe
    //   - N'a PAS de type de retour (pas void, pas int, rien)
    //   - S'exécute automatiquement quand on fait "new BuildingPreview(...)"
    //
    // INJECTION DE DÉPENDANCES :
    // Au lieu de créer les objets à l'intérieur (couplage fort),
    // on les reçoit en paramètres (couplage faible).
    // L'appelant (Player) décide quels objets passer.
    //
    // CONVENTION : Un paramètre par ligne quand il y en a beaucoup.
    // C'est plus lisible que tout sur une seule ligne.
    public BuildingPreview(
        Camera3D camera, 
        CharacterBody3D player, 
        BuildingState buildingState,
        PlacementCalculator placementCalculator,
        BlockHighLight blockHighLight)
    {
        // On stocke les paramètres dans les champs privés
        // ATTENTION à l'ordre : champ = paramètre (pas l'inverse !)
        _camera = camera;
        _player = player;
        _buildingState = buildingState;
        _placementCalculator = placementCalculator;
        _blockHighLight = blockHighLight;
    }

    // ========================================================================
    // RAYCAST — Lance un rayon invisible depuis la caméra
    // ========================================================================
    // Un RAYCAST c'est comme un laser invisible qui détecte ce qu'il touche.
    // On l'utilise pour savoir quel bloc le joueur regarde.
    //
    // RETOURNE un Dictionary (dictionnaire) contenant :
    //   - "position" : le point exact où le rayon touche (Vector3)
    //   - "normal" : la direction de la surface touchée (Vector3)
    //   - "collider" : l'objet (Node) touché
    // Retourne un Dictionary VIDE si le rayon ne touche rien.
    //
    // POURQUOI EXCLURE LE JOUEUR ?
    // Sans ça, le rayon toucherait d'abord le collider du joueur
    // (la capsule qui l'entoure) au lieu des blocs du monde.
    private Godot.Collections.Dictionary Raycast()
    {
        // Récupère l'espace physique pour faire des requêtes
        // DirectSpaceState permet de faire des raycasts manuellement
        var spaceState = _player.GetWorld3D().DirectSpaceState;
        
        // Point de départ = position de la caméra (les yeux du joueur)
        var from = _camera.GlobalPosition;
        
        // Direction = là où regarde la caméra
        // -Z car en 3D, l'axe Z pointe vers l'ARRIÈRE par convention
        var direction = -_camera.GlobalTransform.Basis.Z;
        
        // Point d'arrivée = départ + direction × portée
        var to = from + direction * _interactionRange;
        
        // Crée les paramètres du raycast (from, to)
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        
        // Exclure le joueur du raycast
        // GetRid() retourne l'identifiant unique du collider du joueur
        query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
        
        // Lance le rayon et retourne le résultat
        return spaceState.IntersectRay(query);
    }

    // ========================================================================
    // UPDATE — Calcule la position de placement à chaque frame
    // ========================================================================
    // Appelée par Player._Process() environ 60 fois par seconde.
    //
    // LOGIQUE :
    // 1. Raycast → trouve le bloc visé et le point exact d'impact
    // 2. Mode normal → voxel adjacent, Sub = (0, 0, 0)
    // 3. Mode fin :
    //    a) Si bloc STACKABLE (poteau) → se coller à lui selon la taille par axe
    //    b) Sinon (bloc plein) → calculer Sub depuis hitPoint
    public void Update()
    {
        var result = Raycast();

        if (result.Count > 0)
        {
            var collider = (Node3D)result["collider"];
            var hitNormal = (Vector3)result["normal"];
            var hitPoint = (Vector3)result["position"];

            // Position du bloc VISÉ (celui qu'on a touché)
            var hitBlockX = Mathf.RoundToInt(collider.GlobalPosition.X);
            var hitBlockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
            var hitBlockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);

            if (_buildingState.IsFineMode)
            {
                // ============================================================
                // MODE FIN — Deux cas selon le type de bloc visé
                // ============================================================
                
                // Récupérer le shapeId du bloc visé via les métadonnées
                int hitShapeId = collider.HasMeta("ShapeId") 
                    ? (int)collider.GetMeta("ShapeId") 
                    : Shapes.Full;
                
                // Vérifier si le bloc visé peut contenir plusieurs blocs
                var hitShapeDef = ShapeRegistry.Get((ushort)hitShapeId);
                bool hitCanStack = hitShapeDef != null && hitShapeDef.CanStackInVoxel;

                // ============================================================
                // VÉRIFIER SI LE BLOC À PLACER PEUT TENIR DANS LA SOUS-GRILLE
                // ============================================================
                // Si le bloc à placer est GRAND (≥ 1) sur un axe NON-NORMAL,
                // il va chevaucher le bloc visé. Dans ce cas, on doit aller
                // au voxel adjacent au lieu de rester dans le même voxel.
                //
                // Exemple : poteau vertical (0.33 × 1.0 × 0.33) visé sur face +X
                // pour placer un poteau horizontal (0.33 × 0.33 × 1.0 après rotation).
                // Le poteau horizontal est grand sur Z, donc il ne peut pas
                // coexister avec le poteau vertical dans le même voxel.
                
                var selectedShapeDef = ShapeRegistry.Get(_buildingState.SelectedShapeId);
                float placeSizeX = selectedShapeDef != null ? selectedShapeDef.SizeX : 1.0f;
                float placeSizeY = selectedShapeDef != null ? selectedShapeDef.SizeY : 1.0f;
                float placeSizeZ = selectedShapeDef != null ? selectedShapeDef.SizeZ : 1.0f;

                // Appliquer la rotation au bloc à placer
                // RotX d'abord (sur axes locaux)
                if (_buildingState.SelectedRotationX == 1 || _buildingState.SelectedRotationX == 3)
                {
                    float temp = placeSizeY;
                    placeSizeY = placeSizeZ;
                    placeSizeZ = temp;
                }
                // Puis RotY (sur axes monde)
                if (_buildingState.SelectedRotationY == 1 || _buildingState.SelectedRotationY == 3)
                {
                    float temp = placeSizeX;
                    placeSizeX = placeSizeZ;
                    placeSizeZ = temp;
                }

                // Vérifier si le bloc à placer est grand sur un axe non-normal
                // (utilisé plus tard pour ajuster le sub)
                bool placeIsLargeOnX = placeSizeX >= 1.0f;
                bool placeIsLargeOnY = placeSizeY >= 1.0f;
                bool placeIsLargeOnZ = placeSizeZ >= 1.0f;

                if (hitCanStack)
                {
                    // --------------------------------------------------------
                    // CAS A : BLOC STACKABLE — Partir du sub du bloc visé
                    // --------------------------------------------------------
                    // On récupère la sous-position du bloc visé et on décale
                    // selon la normale. ClampSub gère le changement de voxel
                    // si on dépasse la grille 3×3×3.
                    //
                    // Si le bloc à PLACER est grand sur un axe, il ne peut pas
                    // utiliser la sous-grille sur cet axe → on force le voxel
                    // adjacent sur cet axe.
                    
                    // Récupérer le sub du bloc visé
                    int hitSubX = collider.HasMeta("SubX") 
                        ? (int)collider.GetMeta("SubX") 
                        : PlacementCalculator.SubCenter;
                    int hitSubY = collider.HasMeta("SubY") 
                        ? (int)collider.GetMeta("SubY") 
                        : PlacementCalculator.SubCenter;
                    int hitSubZ = collider.HasMeta("SubZ") 
                        ? (int)collider.GetMeta("SubZ") 
                        : PlacementCalculator.SubCenter;

                    // CORRECTION : Si Sub = 0 (SubNone, bloc posé en mode normal),
                    // le bloc est visuellement centré → utiliser SubCenter (2)
                    if (hitSubX == PlacementCalculator.SubNone) 
                        hitSubX = PlacementCalculator.SubCenter;
                    if (hitSubY == PlacementCalculator.SubNone) 
                        hitSubY = PlacementCalculator.SubCenter;
                    if (hitSubZ == PlacementCalculator.SubNone) 
                        hitSubZ = PlacementCalculator.SubCenter;

                    // Dimensions du bloc visé (APRÈS rotation)
                    float sizeX = hitShapeDef.SizeX;
                    float sizeY = hitShapeDef.SizeY;
                    float sizeZ = hitShapeDef.SizeZ;

                    // Récupérer la rotation du bloc visé
                    int hitRotY = collider.HasMeta("RotationY") 
                        ? (int)collider.GetMeta("RotationY") 
                        : 0;
                    int hitRotX = collider.HasMeta("RotationX") 
                        ? (int)collider.GetMeta("RotationX") 
                        : 0;

                    // Appliquer la rotation aux dimensions du bloc visé
                    // RotX d'abord (sur axes locaux)
                    if (hitRotX == 1 || hitRotX == 3)
                    {
                        float temp = sizeY;
                        sizeY = sizeZ;
                        sizeZ = temp;
                    }
                    // Puis RotY (sur axes monde)
                    if (hitRotY == 1 || hitRotY == 3)
                    {
                        float temp = sizeX;
                        sizeX = sizeZ;
                        sizeZ = temp;
                    }

                    // DEBUG temporaire (seulement quand bloc visé ou normale change)
                    if (hitBlockX != _lastLogBlockX || hitBlockY != _lastLogBlockY || hitBlockZ != _lastLogBlockZ || hitNormal != _lastLogNormal)
                    {
                        GD.Print($"[DEBUG CAS A] hitNormal={hitNormal}, hitRot=(Y:{hitRotY}, X:{hitRotX})");
                        GD.Print($"[DEBUG CAS A] hitSize (après rot)=({sizeX}, {sizeY}, {sizeZ})");
                        GD.Print($"[DEBUG CAS A] placeSize (après rot)=({placeSizeX}, {placeSizeY}, {placeSizeZ})");
                        _lastLogBlockX = hitBlockX;
                        _lastLogBlockY = hitBlockY;
                        _lastLogBlockZ = hitBlockZ;
                        _lastLogNormal = hitNormal;
                    }

                    // Initialiser destination = bloc visé
                    _nextBlockX = hitBlockX;
                    _nextBlockY = hitBlockY;
                    _nextBlockZ = hitBlockZ;

                    int newSubX = hitSubX;
                    int newSubY = hitSubY;
                    int newSubZ = hitSubZ;

                    // ----------------------------------------------------
                    // AXE X : Si la normale pointe vers +X ou -X
                    // ----------------------------------------------------
                    if (Mathf.Abs(hitNormal.X) > 0.5f)
                    {
                        if (sizeX >= 1.0f)
                        {
                            // Grand sur X → aller au voxel adjacent
                            _nextBlockX += Mathf.RoundToInt(hitNormal.X);
                            // Se coller au bord du nouveau voxel
                            newSubX = _placementCalculator.FixSubForNormal(hitNormal.X, PlacementCalculator.SubCenter);
                        }
                        else
                        {
                            // Petit sur X → décaler le sub dans le même voxel
                            newSubX = hitSubX + Mathf.RoundToInt(hitNormal.X);
                        }
                    }

                    // ----------------------------------------------------
                    // AXE Y : Si la normale pointe vers +Y ou -Y
                    // ----------------------------------------------------
                    if (Mathf.Abs(hitNormal.Y) > 0.5f)
                    {
                        if (sizeY >= 1.0f)
                        {
                            // Grand sur Y → aller au voxel adjacent (dessus/dessous)
                            _nextBlockY += Mathf.RoundToInt(hitNormal.Y);
                            // Se coller au bord du nouveau voxel
                            newSubY = _placementCalculator.FixSubForNormal(hitNormal.Y, PlacementCalculator.SubCenter);
                        }
                        else
                        {
                            // Petit sur Y → décaler le sub dans le même voxel
                            newSubY = hitSubY + Mathf.RoundToInt(hitNormal.Y);
                        }
                    }

                    // ----------------------------------------------------
                    // AXE Z : Si la normale pointe vers +Z ou -Z
                    // ----------------------------------------------------
                    if (Mathf.Abs(hitNormal.Z) > 0.5f)
                    {
                        if (sizeZ >= 1.0f)
                        {
                            // Grand sur Z → aller au voxel adjacent
                            _nextBlockZ += Mathf.RoundToInt(hitNormal.Z);
                            // Se coller au bord du nouveau voxel
                            newSubZ = _placementCalculator.FixSubForNormal(hitNormal.Z, PlacementCalculator.SubCenter);
                        }
                        else
                        {
                            // Petit sur Z → décaler le sub dans le même voxel
                            newSubZ = hitSubZ + Mathf.RoundToInt(hitNormal.Z);
                        }
                    }

                    // ----------------------------------------------------
                    // AXES NON-NORMAUX : Calculer depuis hitPoint
                    // ----------------------------------------------------
                    // Pour les axes où la normale ne pointe PAS, on calcule
                    // le sub depuis hitPoint (où l'utilisateur regarde).
                    // Cela permet de placer librement sur la face (haut/milieu/bas).
                    //
                    // On ne garde PAS le sub du bloc visé car :
                    // 1. On veut un placement libre selon où on regarde
                    // 2. Le nouveau bloc doit se centrer, pas hériter du sub
                    //
                    // Note : on utilise _nextBlockX/Y/Z car il peut avoir changé
                    // si on est allé au voxel adjacent sur l'axe de la normale.
                    
                    // Axe X : si la normale NE pointe PAS vers X
                    if (Mathf.Abs(hitNormal.X) < 0.5f)
                    {
                        float fracX = hitPoint.X - (_nextBlockX - PlacementCalculator.HalfVoxel);
                        newSubX = _placementCalculator.CalculateSub(fracX);
                    }
                    
                    // Axe Y : si la normale NE pointe PAS vers Y
                    if (Mathf.Abs(hitNormal.Y) < 0.5f)
                    {
                        float fracY = hitPoint.Y - (_nextBlockY - PlacementCalculator.HalfVoxel);
                        newSubY = _placementCalculator.CalculateSub(fracY);
                    }
                    
                    // Axe Z : si la normale NE pointe PAS vers Z
                    if (Mathf.Abs(hitNormal.Z) < 0.5f)
                    {
                        float fracZ = hitPoint.Z - (_nextBlockZ - PlacementCalculator.HalfVoxel);
                        newSubZ = _placementCalculator.CalculateSub(fracZ);
                    }

                    // ----------------------------------------------------
                    // GÉRER LE DÉBORDEMENT D'ABORD
                    // ----------------------------------------------------
                    // Si sub = 0 ou sub = 4 après le décalage, on a débordé
                    // du voxel → passer au voxel adjacent.
                    //
                    // IMPORTANT : On fait ça AVANT de forcer SubNone, sinon
                    // un sub=0 (débordement) serait confondu avec SubNone=0.
                    int voxelOffsetX = 0, voxelOffsetY = 0, voxelOffsetZ = 0;
                    
                    // ClampSub gère : sub<1 → voxel précédent, sub>3 → voxel suivant
                    newSubX = _placementCalculator.ClampSub(newSubX, out voxelOffsetX);
                    newSubY = _placementCalculator.ClampSub(newSubY, out voxelOffsetY);
                    newSubZ = _placementCalculator.ClampSub(newSubZ, out voxelOffsetZ);

                    _nextBlockX += voxelOffsetX;
                    _nextBlockY += voxelOffsetY;
                    _nextBlockZ += voxelOffsetZ;

                    // ----------------------------------------------------
                    // VÉRIFIER LE BLOC À PLACER
                    // ----------------------------------------------------
                    // Si le bloc à placer est grand sur un axe, le sub
                    // n'a pas de sens sur cet axe → forcer SubNone.
                    // On fait ça APRÈS le ClampSub pour ne pas confondre
                    // un débordement (sub=0) avec "pas de sous-grille".
                    if (placeSizeX >= 1.0f) newSubX = PlacementCalculator.SubNone;
                    if (placeSizeY >= 1.0f) newSubY = PlacementCalculator.SubNone;
                    if (placeSizeZ >= 1.0f) newSubZ = PlacementCalculator.SubNone;

                    _nextSubX = (byte)newSubX;
                    _nextSubY = (byte)newSubY;
                    _nextSubZ = (byte)newSubZ;
                }
                else
                {
                    // --------------------------------------------------------
                    // CAS B : BLOC PLEIN — Calculer Sub depuis hitPoint
                    // --------------------------------------------------------
                    // Le voxel destination est adjacent au bloc visé.
                    // On calcule où on regarde sur la face pour déterminer le sub.
                    
                    _nextBlockX = hitBlockX + Mathf.RoundToInt(hitNormal.X);
                    _nextBlockY = hitBlockY + Mathf.RoundToInt(hitNormal.Y);
                    _nextBlockZ = hitBlockZ + Mathf.RoundToInt(hitNormal.Z);

                    var destCenter = new Vector3(_nextBlockX, _nextBlockY, _nextBlockZ);
                    
                    float fracX = hitPoint.X - (destCenter.X - PlacementCalculator.HalfVoxel);
                    float fracY = hitPoint.Y - (destCenter.Y - PlacementCalculator.HalfVoxel);
                    float fracZ = hitPoint.Z - (destCenter.Z - PlacementCalculator.HalfVoxel);

                    int subX = _placementCalculator.CalculateSub(fracX);
                    int subY = _placementCalculator.CalculateSub(fracY);
                    int subZ = _placementCalculator.CalculateSub(fracZ);

                    // Fixer le sub sur l'axe de la normale pour se coller au bloc
                    subX = _placementCalculator.FixSubForNormal(hitNormal.X, subX);
                    subY = _placementCalculator.FixSubForNormal(hitNormal.Y, subY);
                    subZ = _placementCalculator.FixSubForNormal(hitNormal.Z, subZ);

                    // Si le bloc à placer est grand sur un axe, pas de sous-grille
                    // sur cet axe (le bloc occupera tout le voxel).
                    if (placeSizeX >= 1.0f) subX = PlacementCalculator.SubNone;
                    if (placeSizeY >= 1.0f) subY = PlacementCalculator.SubNone;
                    if (placeSizeZ >= 1.0f) subZ = PlacementCalculator.SubNone;

                    _nextSubX = (byte)subX;
                    _nextSubY = (byte)subY;
                    _nextSubZ = (byte)subZ;
                }
            }
            else
            {
                // ============================================================
                // MODE NORMAL — Pas de sous-position
                // ============================================================
                _nextBlockX = hitBlockX + Mathf.RoundToInt(hitNormal.X);
                _nextBlockY = hitBlockY + Mathf.RoundToInt(hitNormal.Y);
                _nextBlockZ = hitBlockZ + Mathf.RoundToInt(hitNormal.Z);

                _nextSubX = (byte)PlacementCalculator.SubNone;
                _nextSubY = (byte)PlacementCalculator.SubNone;
                _nextSubZ = (byte)PlacementCalculator.SubNone;
            }

            _canPlace = true;

            _blockHighLight.UpdateHighLight(
                _buildingState.SelectedShapeId,
                _buildingState.SelectedRotationY,
                _buildingState.SelectedRotationX,
                new Vector3(_nextBlockX, _nextBlockY, _nextBlockZ),
                _nextSubX,
                _nextSubY,
                _nextSubZ
            );
        }
        else
        {
            _canPlace = false;
            _blockHighLight.UpdateHighLight(Shapes.Full, 0, 0, null, 0, 0, 0);
        }
    }

    // ------------------------------------------------------------------------
    // TOSTRING — Affiche l'état du calcul de preview pour le débogage
    // ------------------------------------------------------------------------
    public override string ToString()
    {
        return $"BuildingPreview(CanPlace:{_canPlace} " +
               $"Block:({_nextBlockX},{_nextBlockY},{_nextBlockZ}) " +
               $"Sub:({_nextSubX},{_nextSubY},{_nextSubZ}))";
    }
}
