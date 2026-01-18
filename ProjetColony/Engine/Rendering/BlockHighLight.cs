// ============================================================================
// BLOCKHIGHLIGHT.CS — Surbrillance du bloc visé
// ============================================================================
// Ce fichier est dans Engine/Rendering, donc il fait partie de ENGINE.
// Il dépend de Godot (MeshInstance3D, StandardMaterial3D, etc.)
//
// CE QU'IL FAIT :
// Affiche un cube semi-transparent autour du bloc que le joueur regarde.
// Permet de voir exactement quel bloc sera cassé ou où le nouveau bloc sera posé.
//
// SOUS-GRILLE 3×3×3 :
// En mode fin, le highlight doit s'afficher à la sous-position calculée,
// pas au centre du voxel. On utilise la même logique que BlockRenderer
// pour calculer l'offset.
//
// POURQUOI HÉRITER DE MESHINSTANCE3D ?
// MeshInstance3D est un node Godot qui affiche un mesh (forme 3D).
// En héritant, BlockHighlight EST un mesh — on peut directement lui assigner
// une forme, un matériau, et le rendre visible/invisible.
//
// POSITION GLOBALE VS LOCALE :
// - Position = position relative au parent (ici le Player)
// - GlobalPosition = position dans le monde
// Comme le highlight doit rester fixe sur un bloc même si le joueur bouge,
// on utilise GlobalPosition.
// ============================================================================

using Godot;
using ProjetColony.Core.Data;
using ProjetColony.Core.Data.Registries;

namespace ProjetColony.Engine.Rendering;

public partial class BlockHighLight : MeshInstance3D
{
    // ========================================================================
    // _READY — Initialisation
    // ========================================================================
    // Crée le mesh et le matériau transparent une seule fois au démarrage.
    public override void _Ready()
    {
        base._Ready();
        
        // --------------------------------------------------------------------
        // LE MESH — Un cube légèrement plus grand que les blocs
        // --------------------------------------------------------------------
        // 1.01 au lieu de 1.0 pour que le highlight dépasse légèrement
        // et soit visible autour du bloc (sinon ils se superposent exactement
        // et on ne voit rien à cause du Z-fighting).
        var boxMesh = new BoxMesh();
        boxMesh.Size = new Vector3(1.01f, 1.01f, 1.01f);
        Mesh = boxMesh;
        
        // --------------------------------------------------------------------
        // LE MATÉRIAU — Semi-transparent
        // --------------------------------------------------------------------
        // Transparency.Alpha active le canal alpha (transparence).
        // AlbedoColor(1, 1, 1, 0.3f) = blanc à 30% d'opacité.
        // Le 4ème paramètre (0.3f) est l'alpha : 0 = invisible, 1 = opaque.
        var material = new StandardMaterial3D();
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        material.AlbedoColor = new Color(1, 1, 1, 0.3f);
        MaterialOverride = material;
        
        // Caché par défaut — sera affiché quand on vise un bloc
        Visible = false;
    }

    // ========================================================================
    // UPDATEHIGHLIGHT — Met à jour la position et la visibilité
    // ========================================================================
    // Appelée à chaque frame par BuildingPreview.
    //
    // Paramètres :
    // - shapeId : forme sélectionnée (pour le mesh et le calcul d'offset)
    // - rotationY : rotation horizontale (0-3)
    // - rotationX : rotation verticale (0-3)
    // - position : coordonnées du voxel, ou null si on ne vise rien
    // - subX, subY, subZ : sous-position dans la grille 3×3×3
    //
    // La sous-position sert à décaler le highlight pour qu'il s'affiche
    // exactement où le bloc sera posé, pas au centre du voxel.
    public void UpdateHighLight(
        ushort shapeId, 
        ushort rotationY, 
        ushort rotationX, 
        Vector3? position,
        byte subX = 0,
        byte subY = 0,
        byte subZ = 0)
    {
        if (position == null)
        {
            Visible = false;
        }
        else
        {
            // Récupère le mesh pour cette forme
            Mesh = BlockRenderer.GetMeshForShape(shapeId);

            // ----------------------------------------------------------------
            // CALCUL DE L'OFFSET (sous-grille 3×3×3)
            // ----------------------------------------------------------------
            // Même logique que BlockRenderer.CreateBlock() :
            // L'offset ne s'applique que sur les axes où le bloc est PETIT
            // après rotation, ET si sub != 0.
            
            float offsetX = 0;
            float offsetY = 0;
            float offsetZ = 0;

            var shapeDef = ShapeRegistry.Get(shapeId);
            
            // Offset pour les formes qui ont un décalage Y fixe (Tiers, DeuxTiers)
            if (shapeId == Shapes.Tiers)
            {
                offsetY = -0.335f;
            }
            else if (shapeId == Shapes.DeuxTiers)
            {
                offsetY = -0.17f;
            }

            // Offset pour la sous-grille (mode fin)
            if (shapeDef != null && shapeDef.CanStackInVoxel)
            {
                float sizeX = shapeDef.SizeX;
                float sizeY = shapeDef.SizeY;
                float sizeZ = shapeDef.SizeZ;

                // Calculer les dimensions effectives après rotation
                // L'ordre compte : on applique RotX sur les axes locaux après RotY
                
                // RotX d'abord (sur axes locaux)
                if (rotationX == 1 || rotationX == 3)
                {
                    float temp = sizeY;
                    sizeY = sizeZ;
                    sizeZ = temp;
                }

                // Puis RotY (sur axes monde)
                if (rotationY == 1 || rotationY == 3)
                {
                    float temp = sizeX;
                    sizeX = sizeZ;
                    sizeZ = temp;
                }

                // Offset seulement si petit sur cet axe ET sub != 0
                if (subX != 0 && sizeX < 1)
                {
                    offsetX = (subX - 2f) * 0.33f;
                }
                if (subY != 0 && sizeY < 1)
                {
                    offsetY = (subY - 2f) * 0.33f;
                }
                if (subZ != 0 && sizeZ < 1)
                {
                    offsetZ = (subZ - 2f) * 0.33f;
                }
            }

            // Applique la position avec l'offset
            // Le petit décalage 0.001 évite le Z-fighting avec les blocs existants
            GlobalPosition = position.Value + new Vector3(
                offsetX + 0.001f, 
                offsetY + 0.001f, 
                offsetZ + 0.001f
            );

            // Applique la rotation
            GlobalRotation = new Vector3(
                Mathf.DegToRad(rotationX * 90),
                Mathf.DegToRad(rotationY * 90),
                0
            );
            
            Visible = true;
        }
    }
}
