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
//
// ÉVOLUTION FUTURE :
// - Changer de couleur selon l'action (rouge = casser, vert = poser)
// - Afficher la forme du bloc sélectionné (pente, demi-bloc, etc.)
// - Utiliser GlobalRotation pour montrer l'orientation avant de poser
// ============================================================================

using Godot;
using ProjetColony.Core.Data;

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
    // Appelée à chaque frame par le Player.
    //
    // Paramètre :
    // - position : coordonnées du bloc visé, ou null si on ne vise rien
    //
    // Vector3? (avec ?) est un type "nullable" — il peut contenir une valeur
    // OU être null. C'est parfait pour "il y a un bloc" vs "il n'y a rien".
    public void UpdateHighLight(ushort shapeId, ushort rotationId, Vector3? position)
    {
        if (position == null)
        {
            // Pas de bloc visé → cacher le highlight
            Visible = false;
        }
        else
        {
            Mesh = BlockRenderer.GetMeshForShape(shapeId);

            float offsetY = 0;
            if (shapeId == Shapes.Demi)
            {
                offsetY = -0.25f;
            }

            // Bloc visé → déplacer et afficher le highlight
            // GlobalPosition car on veut la position dans le monde,
            // pas relative au Player (qui est notre parent).
            GlobalPosition = position.Value + new Vector3(0.001f, offsetY + 0.001f, 0.001f);

            GlobalRotation = new Vector3(0, Mathf.DegToRad(rotationId * 90), 0);
            
            Visible = true;
        }
    }
}