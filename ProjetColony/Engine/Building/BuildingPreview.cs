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
// DEUX MODES DE FONCTIONNEMENT :
//   - Mode normal : raycast classique, on vise un bloc existant
//   - Mode fin avec surface : intersection rayon/plan mathématique
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

    // L'état de construction — mode fin ? surface sélectionnée ? forme choisie ?
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
    // DEUX CAS DIFFÉRENTS :
    //
    // CAS 1 : Mode fin AVEC surface sélectionnée
    //   → On ne fait PAS de raycast sur les blocs
    //   → On calcule l'intersection entre le regard et un PLAN mathématique
    //   → Permet de placer librement sur la surface choisie
    //
    // CAS 2 : Mode normal (ou mode fin SANS surface)
    //   → Raycast classique pour trouver le bloc visé
    //   → On pose dans le voxel ADJACENT (selon la normale)
    //
    // À LA FIN de chaque cas, on :
    //   1. Remplit _nextBlockX/Y/Z et _nextSubX/Y/Z
    //   2. Met _canPlace à true ou false
    //   3. Met à jour le highlight (position + visibilité)
    public void Update()
    {
        // ====================================================================
        // CAS 1 : MODE FIN AVEC SURFACE SÉLECTIONNÉE
        // ====================================================================
        // Le joueur a déjà cliqué sur une face pour la sélectionner.
        // Maintenant, on calcule où sur cette face il regarde.
        //
        // POURQUOI PAS UN RAYCAST NORMAL ?
        // On veut pouvoir placer des blocs même dans le VIDE (au-dessus
        // de la surface). Un raycast ne toucherait rien. Donc on utilise
        // une intersection rayon/plan mathématique.
        if(_buildingState.IsFineMode && _buildingState.HasSelectedSurface)
        {
            // ----------------------------------------------------------------
            // ÉTAPE 1 : Définir le plan de la surface
            // ----------------------------------------------------------------
            // Un plan est défini par :
            //   - Un point d'origine (planeOrigin)
            //   - Une direction perpendiculaire (planeNormal)
            //
            // La normale est stockée dans BuildingState quand on a cliqué
            // sur la surface (ex: 0,1,0 = face du dessus).
            var planeNormal = new Vector3(_buildingState.SurfaceNormalX, _buildingState.SurfaceNormalY, _buildingState.SurfaceNormalZ);
            
            // Le centre du voxel où on va poser
            var voxelCenter = new Vector3(_buildingState.SurfaceVoxelX, _buildingState.SurfaceVoxelY, _buildingState.SurfaceVoxelZ);
            
            // Le plan est sur la FACE du voxel, pas au centre
            // On recule de 0.5 dans la direction de la normale
            // ASTUCE VECTORIELLE : une seule ligne au lieu de 6 if !
            var planeOrigin = voxelCenter - planeNormal * PlacementCalculator.HalfVoxel;
        
            // ----------------------------------------------------------------
            // ÉTAPE 2 : Définir le rayon du regard
            // ----------------------------------------------------------------
            var rayOrigin = _camera.GlobalPosition;
            var rayDirection = -_camera.GlobalTransform.Basis.Z;

            // ----------------------------------------------------------------
            // ÉTAPE 3 : Calculer l'intersection rayon/plan
            // ----------------------------------------------------------------
            // FORMULE MATHÉMATIQUE :
            //   t = (planeOrigin - rayOrigin) · normal / (rayDirection · normal)
            //   hitPoint = rayOrigin + rayDirection × t
            //
            // "·" est le PRODUIT SCALAIRE (Dot product) :
            // Il mesure l'alignement entre deux vecteurs.
            //   - Si les vecteurs sont parallèles → grand nombre
            //   - Si perpendiculaires → 0
            //
            // "denom" = dénominateur de la formule
            // Si proche de 0, le rayon est parallèle au plan (pas d'intersection)
            float denom = planeNormal.Dot(rayDirection);
            if(Mathf.Abs(denom) > 0.001f)
            {
                // "t" = distance le long du rayon jusqu'au point d'intersection
                float t = (planeOrigin - rayOrigin).Dot(planeNormal) / denom;
                
                // t > 0 signifie que le point est DEVANT nous (pas derrière)
                if(t > 0)
                {
                    // Le point exact où le regard touche le plan
                    var hitPoint = rayOrigin + rayDirection * t;

                    // --------------------------------------------------------
                    // ÉTAPE 4 : Convertir en sous-position (1, 2, ou 3)
                    // --------------------------------------------------------
                    // fracX/Y/Z = position relative dans le voxel (0 à 1)
                    // On soustrait le coin bas-arrière-gauche du voxel
                    float fracX = hitPoint.X - (voxelCenter.X - PlacementCalculator.HalfVoxel);
                    float fracY = hitPoint.Y - (voxelCenter.Y - PlacementCalculator.HalfVoxel);
                    float fracZ = hitPoint.Z - (voxelCenter.Z - PlacementCalculator.HalfVoxel);

                    // Convertit fraction (0-1) en sub (1, 2, 3)
                    int subX = _placementCalculator.CalculateSub(fracX);
                    int subY = _placementCalculator.CalculateSub(fracY);
                    int subZ = _placementCalculator.CalculateSub(fracZ);

                    // --------------------------------------------------------
                    // ÉTAPE 5 : Ajuster selon HasFixedSub ou la normale
                    // --------------------------------------------------------
                    if (_buildingState.HasFixedSub)
                    {
                        // On a cliqué sur un bloc stackable (poteau)
                        // → Utiliser le sub pré-calculé pour se coller à lui
                        subX = _buildingState.FixedSubX;
                        subY = _buildingState.FixedSubY;
                        subZ = _buildingState.FixedSubZ;
                    }
                    else
                    {
                        // On a cliqué sur un bloc plein
                        // → Se coller contre la surface (fixer l'axe de la normale)
                        subX = _placementCalculator.FixSubForNormal(_buildingState.SurfaceNormalX, subX);
                        subY = _placementCalculator.FixSubForNormal(_buildingState.SurfaceNormalY, subY);
                        subZ = _placementCalculator.FixSubForNormal(_buildingState.SurfaceNormalZ, subZ);
                    }

                    // --------------------------------------------------------
                    // ÉTAPE 6 : Stocker les résultats
                    // --------------------------------------------------------
                    _nextBlockX = _buildingState.SurfaceVoxelX;
                    _nextBlockY = _buildingState.SurfaceVoxelY;
                    _nextBlockZ = _buildingState.SurfaceVoxelZ;

                    _nextSubX = (byte)subX;
                    _nextSubY = (byte)subY;
                    _nextSubZ = (byte)subZ;

                    _canPlace = true;

                    // Met à jour le highlight avec la forme/rotation sélectionnées
                    _blockHighLight.UpdateHighLight(
                        _buildingState.SelectedShapeId,
                        _buildingState.SelectedRotationY,
                        _buildingState.SelectedRotationX,
                        new Vector3(_nextBlockX, _nextBlockY, _nextBlockZ)
                    );
                }
                else
                {
                    // t <= 0 : le plan est DERRIÈRE nous
                    _canPlace = false;
                    _blockHighLight.UpdateHighLight(Shapes.Full, 0, 0, null);
                }
            }
            else
            {
                // Rayon parallèle au plan (pas d'intersection possible)
                _canPlace = false;
                _blockHighLight.UpdateHighLight(Shapes.Full, 0, 0, null);
            }
        }

        // ====================================================================
        // CAS 2 : MODE NORMAL (ou mode fin sans surface sélectionnée)
        // ====================================================================
        // Raycast classique : on cherche quel bloc le joueur regarde,
        // puis on calcule la position du voxel ADJACENT.
        else
        {
            // Lance le raycast
            var result = Raycast();

            if(result.Count > 0)
            {
                // On a touché quelque chose !
                
                // Le collider touché (le StaticBody3D du bloc)
                var collider = (Node3D)result["collider"];

                // La normale de la face touchée (direction perpendiculaire)
                // Ex: (0, 1, 0) = face du dessus, (1, 0, 0) = face droite
                var hitNormal = (Vector3)result["normal"];

                // Position du bloc VISÉ (celui qu'on a touché)
                // RoundToInt car la position peut avoir des décimales
                var hitBlockX = Mathf.RoundToInt(collider.GlobalPosition.X);
                var hitBlockY = Mathf.RoundToInt(collider.GlobalPosition.Y);
                var hitBlockZ = Mathf.RoundToInt(collider.GlobalPosition.Z);

                // Position du voxel ADJACENT (où on va poser)
                // On ajoute la normale pour aller dans le voxel d'à côté
                // Ex: bloc visé (5, 10, 3), normale (0, 1, 0) → poser en (5, 11, 3)
                _nextBlockX = hitBlockX + Mathf.RoundToInt(hitNormal.X);
                _nextBlockY = hitBlockY + Mathf.RoundToInt(hitNormal.Y);
                _nextBlockZ = hitBlockZ + Mathf.RoundToInt(hitNormal.Z);

                // Pas de sous-position en mode normal (bloc centré)
                // SubNone = 0, signifie "pas de sous-grille"
                _nextSubX = (byte)PlacementCalculator.SubNone;
                _nextSubY = (byte)PlacementCalculator.SubNone;
                _nextSubZ = (byte)PlacementCalculator.SubNone;

                _canPlace = true;

                // Affiche le highlight à la position calculée
                _blockHighLight.UpdateHighLight(
                    _buildingState.SelectedShapeId,
                    _buildingState.SelectedRotationY,
                    _buildingState.SelectedRotationX,
                    new Vector3(_nextBlockX, _nextBlockY, _nextBlockZ)
                );
            }
            else
            {
                // Le raycast n'a rien touché (on regarde le ciel, trop loin, etc.)
                _canPlace = false;

                // Cache le highlight en passant null comme position
                _blockHighLight.UpdateHighLight(Shapes.Full, 0, 0, null);
            }
        }
    }
}