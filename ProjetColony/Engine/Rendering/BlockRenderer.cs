// ============================================================================
// BLOCKRENDERER.CS — Création des visuels de blocs
// ============================================================================
// Ce fichier est dans Engine/Rendering, donc il fait partie de ENGINE.
// Il dépend de Godot (StaticBody3D, MeshInstance3D, etc.)
//
// POURQUOI CE FICHIER ?
// Avant, le code de création de bloc était dupliqué :
//   - Dans Main.cs (génération du terrain)
//   - Dans Player.cs (pose de blocs)
//
// Problèmes de la duplication :
//   - Si on change une couleur, il faut modifier deux endroits
//   - Risque d'incohérence (oublier un endroit)
//   - Code plus long et difficile à maintenir
//
// SOLUTION : Une méthode unique qui crée le visuel d'un bloc.
// Main.cs et Player.cs appellent tous les deux BlockRenderer.CreateBlock().
//
// "static class" SIGNIFIE :
// - On ne peut pas faire "new BlockRenderer()"
// - On appelle directement : BlockRenderer.CreateBlock(...)
// - C'est comme une boîte à outils de méthodes utilitaires
//
// ÉVOLUTION FUTURE :
// - Ajouter des textures au lieu de simples couleurs
// - Optimiser en réutilisant les matériaux au lieu d'en créer un par bloc
// - Ajouter les normales pour un éclairage correct des pentes
// ============================================================================

using Godot;
using ProjetColony.Core.Data;
using ProjetColony.Core.World;
using ProjetColony.Core.Data.Registries;

namespace ProjetColony.Engine.Rendering;

public static class BlockRenderer
{
    // ------------------------------------------------------------------------
    // CREATEBLOCK — Crée le visuel complet d'un bloc
    // ------------------------------------------------------------------------
    // Paramètres :
    //   - block : le bloc à créer (contient MaterialId, ShapeId, RotationId, SubX/Y/Z)
    //   - position : position du VOXEL dans le monde (coordonnées entières)
    //
    // Retourne : un StaticBody3D prêt à être ajouté à la scène
    //
    // Le StaticBody3D contient :
    //   - Un MeshInstance3D (ce qu'on voit — le cube coloré)
    //   - Un CollisionShape3D (ce qui bloque le joueur)
    //
    // ARCHITECTURE IMPORTANTE :
    // Le StaticBody3D est positionné au CENTRE du voxel.
    // Le MeshInstance3D et CollisionShape3D sont décalés à l'intérieur
    // selon SubX/Y/Z. Cela permet au raycast de toujours retrouver
    // le bon voxel via collider.GlobalPosition.
    //
    // POURQUOI STATICBODY3D ?
    // Godot a plusieurs types de corps physiques :
    //   - StaticBody3D : immobile, ne bouge jamais (parfait pour le terrain)
    //   - RigidBody3D : affecté par la physique (gravité, collisions)
    //   - CharacterBody3D : contrôlé par le code (le joueur)
    // Les blocs du terrain ne bougent pas, donc StaticBody3D.
    public static StaticBody3D CreateBlock(Block block, Vector3 position)
    {
        // Crée le conteneur physique
        var staticBody = new StaticBody3D();
        
        // Extrait les propriétés du bloc
        var materialId = block.MaterialId;
        var shapeId = block.ShapeId;
        var rotationY = block.RotationId;
        var rotationX = block.RotationX;

        staticBody.Rotation = new Vector3(
            Mathf.DegToRad(rotationX * 90),
            Mathf.DegToRad(rotationY * 90),
            0
        );

        // --------------------------------------------------------------------
        // CALCUL DE L'OFFSET (sous-grille 4×4×4)
        // --------------------------------------------------------------------
        // On n'applique l'offset que sur les axes où le bloc est PETIT après rotation.
        
        float offsetX = 0;
        float offsetY = 0;
        float offsetZ = 0;

        var shapeDef = ShapeRegistry.Get(block.ShapeId);
        if (shapeDef != null && shapeDef.CanStackInVoxel)
        {
            float sizeX = shapeDef.SizeX;
            float sizeY = shapeDef.SizeY;
            float sizeZ = shapeDef.SizeZ;

            // Calculer les dimensions effectives après rotation
            // L'ordre compte : on applique RotX sur les axes locaux après RotY
            int rotY = block.RotationId;      // ou block.RotationId dans BlockRenderer
            int rotX = block.RotationX;     // ou block.RotationX dans BlockRenderer

            // RotX d'abord (sur axes locaux)
            if (rotX == 1 || rotX == 3)
            {
                float temp = sizeY;
                sizeY = sizeZ;
                sizeZ = temp;
            }

            // Puis RotY (sur axes monde)
            if (rotY == 1 || rotY == 3)
            {
                float temp = sizeX;
                sizeX = sizeZ;
                sizeZ = temp;
            }

            GD.Print("RotY:" + block.RotationId + " RotX:" + block.RotationX + " → Size:" + sizeX + "," + sizeY + "," + sizeZ);
            GD.Print("Sub:" + block.SubX + "," + block.SubY + "," + block.SubZ);

            // Offset seulement si petit sur cet axe ET sub != 0
            if (block.SubX != 0 && sizeX < 1)
            {
                offsetX = (block.SubX - 2f) * 0.33f;
            }
            if (block.SubY != 0 && sizeY < 1)
            {
                offsetY = (block.SubY - 2f) * 0.33f;
            }
            if (block.SubZ != 0 && sizeZ < 1)
            {
                offsetZ = (block.SubZ - 2f) * 0.33f;
            }
            GD.Print("RENDERER - offset:" + offsetX + "," + offsetY + "," + offsetZ);
        }

        // Le StaticBody reste au centre du voxel
        // L'offset sera appliqué aux enfants (mesh et collision)
        staticBody.Position = position + new Vector3(offsetX, offsetY, offsetZ);

        // --------------------------------------------------------------------
        // LE MESH — Ce qu'on voit à l'écran
        // --------------------------------------------------------------------
        // MeshInstance3D affiche une forme 3D.
        // La forme dépend du shapeId : cube, demi-bloc, pente, poteau...
        var meshInstance = new MeshInstance3D();
        
        // --------------------------------------------------------------------
        // CHOIX DE LA FORME
        // --------------------------------------------------------------------
        // Chaque forme a son propre mesh et potentiellement un décalage
        // de position pour être bien alignée avec la grille.
        BoxMesh boxShape;
        
        if (shapeId == Shapes.Full)
        {
            boxShape = new BoxMesh();
            meshInstance.Mesh = boxShape;
        }
        else if (shapeId == Shapes.Tiers)
        {
            boxShape = new BoxMesh();
            boxShape.Size = new Vector3(1.0f, 0.33f, 1.0f);
            meshInstance.Position = new Vector3(0.0f, -0.335f, 0.0f);
            meshInstance.Mesh = boxShape;
        }
        else if (shapeId == Shapes.DeuxTiers)
        {
            boxShape = new BoxMesh();
            boxShape.Size = new Vector3(1.0f, 0.66f, 1.0f);
            meshInstance.Position = new Vector3(0.0f, -0.17f, 0.0f);
            meshInstance.Mesh = boxShape;
        }
        else if (shapeId == Shapes.Post)
        {
            boxShape = new BoxMesh();
            boxShape.Size = new Vector3(0.33f, 1.0f, 0.33f);
            meshInstance.Mesh = boxShape;
        }
        else if (shapeId == Shapes.FullSlope)
        {
            meshInstance.Mesh = CreateSlopeMesh(1.0f);
        }
        else if (shapeId == Shapes.TiersSlope)
        {
            meshInstance.Mesh = CreateSlopeMesh(0.33f);
        }
        else if (shapeId == Shapes.DeuxTiersSlope)
        {
            meshInstance.Mesh = CreateSlopeMesh(0.66f);
        }
        else
        {
            boxShape = new BoxMesh();
            meshInstance.Mesh = boxShape;
        }

        // --------------------------------------------------------------------
        // LA COULEUR — Dépend du type de matériau
        // --------------------------------------------------------------------
        // StandardMaterial3D permet de définir l'apparence du mesh.
        // AlbedoColor = la couleur de base (albedo = réflectance en latin).
        // MaterialOverride = applique ce matériau au mesh.
        //
        // CullMode.Disabled = affiche les deux côtés de chaque face.
        // Nécessaire pour les pentes car on n'a pas défini les normales.
        // Sans ça, certaines faces disparaissent selon l'angle de vue.
        //
        // Les couleurs sont en RGB (Rouge, Vert, Bleu), valeurs de 0 à 1.
        if (materialId == Materials.Stone)
        {
            var material = new StandardMaterial3D();
            material.AlbedoColor = new Color(0.5f, 0.5f, 0.5f); // Gris
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            meshInstance.MaterialOverride = material;
        }
        else if (materialId == Materials.Dirt)
        {
            var material = new StandardMaterial3D();
            material.AlbedoColor = new Color(0.6f, 0.3f, 0.1f); // Marron
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            meshInstance.MaterialOverride = material;
        }
        else // Grass et tout le reste
        {
            var material = new StandardMaterial3D();
            material.AlbedoColor = new Color(0.2f, 0.8f, 0.2f); // Vert
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            meshInstance.MaterialOverride = material;
        }

        // --------------------------------------------------------------------
        // LA COLLISION — Ce qui bloque le joueur
        // --------------------------------------------------------------------
        // CollisionShape3D définit la forme utilisée pour les collisions.
        // BoxShape3D = un cube de 1×1×1 (même taille que le mesh).
        // Sans collision, le joueur traverserait les blocs !
        
        var collisionShape = new CollisionShape3D();

        if (shapeId == Shapes.Tiers)
        {
            var boxCol = new BoxShape3D();
            boxCol.Size = new Vector3(1.0f, 0.33f, 1.0f);
            collisionShape.Shape = boxCol;
            collisionShape.Position = new Vector3(0, -0.335f, 0);
        }
        else if (shapeId == Shapes.DeuxTiers)
        {
            var boxCol = new BoxShape3D();
            boxCol.Size = new Vector3(1.0f, 0.66f, 1.0f);
            collisionShape.Shape = boxCol;
            collisionShape.Position = new Vector3(0, -0.17f, 0);
        }
        else if (shapeId == Shapes.FullSlope)
        {
            collisionShape.Shape = CreateSlopeCollider(1.0f);
        }
        else if (shapeId == Shapes.TiersSlope)
        {
            collisionShape.Shape = CreateSlopeCollider(0.33f);
        }
        else if (shapeId == Shapes.DeuxTiersSlope)
        {
            collisionShape.Shape = CreateSlopeCollider(0.66f);
        }
        else if (shapeId == Shapes.Post)
        {
            var boxCol = new BoxShape3D();
            boxCol.Size = new Vector3(0.33f, 1.0f, 0.33f);
            collisionShape.Shape = boxCol;
        }
        else
        {
            collisionShape.Shape = new BoxShape3D();
        }
        
        // --------------------------------------------------------------------
        // ASSEMBLAGE — Hiérarchie de nodes
        // --------------------------------------------------------------------
        // En Godot, les objets sont organisés en arbre (parent → enfants).
        // Ici : StaticBody3D
        //         ├── MeshInstance3D (visuel)
        //         └── CollisionShape3D (collision)
        // Quand on déplace le StaticBody, ses enfants bougent avec.
        staticBody.AddChild(meshInstance);
        staticBody.AddChild(collisionShape);

        // --------------------------------------------------------------------
        // MÉTADONNÉES — Stockage des infos du bloc pour le raycast
        // --------------------------------------------------------------------
        // On sauvegarde SubX/Y/Z et ShapeId dans le StaticBody.
        // Cela permet au raycast de récupérer ces infos quand on vise un bloc.
        //
        // Utilité en mode fin :
        // Quand on vise un poteau à SubX=2, on veut poser le suivant à SubX=3.
        // Sans ces métadonnées, on ne saurait pas où est le bloc visé.
        //
        // SetMeta(clé, valeur) stocke une donnée arbitraire dans un Node.
        // GetMeta(clé) la récupère plus tard.
        staticBody.SetMeta("SubX", (int)block.SubX);
        staticBody.SetMeta("SubY", (int)block.SubY);
        staticBody.SetMeta("SubZ", (int)block.SubZ);
        staticBody.SetMeta("ShapeId", (int)block.ShapeId);

        return staticBody;
    }

    // ========================================================================
    // GETMESHFORSHAPE — Retourne le mesh correspondant à une forme
    // ========================================================================
    // Utilisé par BlockHighlight pour afficher la preview du bloc.
    // Méthode publique car appelée depuis un autre fichier.
    //
    // Paramètre : shapeId — l'identifiant de la forme
    // Retourne : le Mesh correspondant
    public static Mesh GetMeshForShape(ushort shapeId)
    {
        if (shapeId == Shapes.Full)
        {
            return new BoxMesh();
        }
        else if (shapeId == Shapes.Tiers)
        {
            var mesh = new BoxMesh();
            mesh.Size = new Vector3(1.0f, 0.33f, 1.0f);
            return mesh;
        }
        else if (shapeId == Shapes.DeuxTiers)
        {
            var mesh = new BoxMesh();
            mesh.Size = new Vector3(1.0f, 0.66f, 1.0f);
            return mesh;
        }
        else if (shapeId == Shapes.Post)
        {
            var mesh = new BoxMesh();
            mesh.Size = new Vector3(0.33f, 1.0f, 0.33f);
            return mesh;
        }
        else if (shapeId == Shapes.FullSlope)
        {
            return CreateSlopeMesh(1.0f);
        }
        else if (shapeId == Shapes.TiersSlope)
        {
            return CreateSlopeMesh(0.33f);
        }
        else if (shapeId == Shapes.DeuxTiersSlope)
        {
            return CreateSlopeMesh(0.66f);
        }
        else
        {
            return new BoxMesh();
        }
    }

    // ========================================================================
    // CREATESLOPEMESH — Crée un mesh de pente à la main
    // ========================================================================
    // Godot n'a pas de "SlopeMesh" tout fait, donc on le construit nous-mêmes.
    //
    // UN MESH 3D C'EST QUOI ?
    // Une liste de triangles. Chaque triangle = 3 sommets (vertices).
    // On définit :
    //   - vertices : les points 3D (Vector3)
    //   - indices : quels vertices forment chaque triangle
    //
    // NOTRE PENTE (vue de côté) :
    //
    //     4───5        ← haut arrière (Y = +0.5)
    //    /│  /│
    //   / │ / │
    //  /  │/  │
    // 3───2   │        ← bas avant (Y = -0.5)
    //     │   │
    //     0───1        ← bas arrière
    //
    // La pente descend de l'arrière (Z = -0.5) vers l'avant (Z = +0.5).
    // ========================================================================
private static Mesh CreateSlopeMesh(float height)
    {
        float topY = -0.5f + height;
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),  // 0 : bas arrière gauche
            new Vector3( 0.5f, -0.5f, -0.5f),  // 1 : bas arrière droit
            new Vector3( 0.5f, -0.5f,  0.5f),  // 2 : bas avant droit
            new Vector3(-0.5f, -0.5f,  0.5f),  // 3 : bas avant gauche
            new Vector3(-0.5f,  topY, -0.5f),  // 4 : haut arrière gauche
            new Vector3( 0.5f,  topY, -0.5f),  // 5 : haut arrière droit
        };

        int[] indices = new int[]
        {
            0, 2, 1,
            0, 3, 2,
            0, 5, 1,
            0, 4, 5,
            0, 4, 3,
            1, 5, 2,
            2, 5, 4,
            2, 4, 3,
        };

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        return mesh;
    }

    private static ConvexPolygonShape3D CreateSlopeCollider(float height)
    {
        float topY = -0.5f + height;
        
        Vector3[] points = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f,  topY, -0.5f),
            new Vector3( 0.5f,  topY, -0.5f),
        };

        var shape = new ConvexPolygonShape3D();
        shape.Points = points;
        return shape;
    }
}