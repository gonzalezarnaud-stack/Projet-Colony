using Godot;
using ProjetColony.Core.Building;
using ProjetColony.Core.Data;
using ProjetColony.Engine.Rendering;

namespace ProjetColony.Engine.Building;

public class BuildingPreview
{
    // Références
    private Camera3D _camera;
    private Node3D _player;
    private BuildingState _buildingState;
    private PlacementCalculator _placementCalculator;
    private BlockHighLight _blockHighLight;
    
    // Résultat du calcul (position où poser)
    private int _nextBlockX;
    private int _nextBlockY;
    private int _nextBlockZ;
    private byte _nextSubX;
    private byte _nextSubY;
    private byte _nextSubZ;
    private bool _canPlace;
    
    // Portée d'interaction
    private float _interactionRange = 5.0f;

    // Propriétés publiques pour que Player puisse lire les résultats
    // "=>" equivaut à {get {return ce qui suit "=>";}}
    public bool CanPlace => _canPlace;
    public int NextBlockX => _nextBlockX;
    public int NextBlockY => _nextBlockY;
    public int NextBlockZ => _nextBlockZ;
    public byte NextSubX => _nextSubX;
    public byte NextSubY => _nextSubY;
    public byte NextSubZ => _nextSubZ;

    public BuildingPreview(
        Camera3D camera, 
        Node3D player, 
        BuildingState buildingState,
        PlacementCalculator placementCalculator,
        BlockHighLight blockHighLight)
    {
        _camera = camera;
        _player = player;
        _buildingState = buildingState;
        _placementCalculator = placementCalculator;
        _blockHighLight = blockHighLight;
    }

    private Godot.Collections.Dictionary Raycast()
    {
    // Récupère l'espace physique pour faire des requêtes
    var spaceState = _player.GetWorld3D().DirectSpaceState;
    
    // Point de départ = position de la caméra
    var from = _camera.GlobalPosition;
    
    // Direction = là où regarde la caméra (axe -Z local)
    var direction = -_camera.GlobalTransform.Basis.Z;
    
    // Point d'arrivée = départ + direction × portée
    var to = from + direction * _interactionRange;
    
    // Crée les paramètres du raycast
    var query = PhysicsRayQueryParameters3D.Create(from, to);
    
    // Exclure le joueur du raycast
    query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
    
    // Lance le rayon et retourne le résultat
    return spaceState.IntersectRay(query);
    }

    public void Update()
    {
        if(_buildingState.IsFineMode && _buildingState.HasSelectedSurface)
        {
            var planeNormal = new Vector3(_buildingState.SurfaceNormalX, _buildingState.SurfaceNormalY, _buildingState.SurfaceNormalZ);
            var voxelCenter = new Vector3(_buildingState.SurfaceVoxelX, _buildingState.SurfaceVoxelY, _buildingState.SurfaceVoxelZ);
            var planeOrigin = voxelCenter - planeNormal * PlacementCalculator.HalfVoxel;
        
            var rayOrigin = _camera.GlobalPosition;
            var rayDirection = -_camera.GlobalTransform.Basis.Z;

            float denom = planeNormal.Dot(rayDirection);
            if(Mathf.Abs(denom) > 0.001f)
            {
                float t = (planeOrigin - rayOrigin).Dot(planeNormal) / denom;
                if(t > 0)
                {
                    var hitPoint = rayOrigin + rayDirection * t;
                    _nextSubX = (byte)hitPoint.X;
                    _nextSubY = (byte)hitPoint.Y;
                    _nextSubZ = (byte)hitPoint.Z;
                }
            }
        }
    }

}