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

namespace ProjetColony.Engine.Rendering;

public static class BlockRenderer
{
    // ------------------------------------------------------------------------
    // CREATEBLOCK — Crée le visuel complet d'un bloc
    // ------------------------------------------------------------------------
    // Paramètres :
    //   - materialId : le type de bloc (Materials.Stone, Materials.Dirt, etc.)
    //   - shapeId : la forme du bloc (Shapes.Full, Shapes.Demi, etc.)
    //   - position : où placer le bloc dans le monde (coordonnées mondiales)
    //
    // Retourne : un StaticBody3D prêt à être ajouté à la scène
    //
    // Le StaticBody3D contient :
    //   - Un MeshInstance3D (ce qu'on voit — le cube coloré)
    //   - Un CollisionShape3D (ce qui bloque le joueur)
    //
    // POURQUOI STATICBODY3D ?
    // Godot a plusieurs types de corps physiques :
    //   - StaticBody3D : immobile, ne bouge jamais (parfait pour le terrain)
    //   - RigidBody3D : affecté par la physique (gravité, collisions)
    //   - CharacterBody3D : contrôlé par le code (le joueur)
    // Les blocs du terrain ne bougent pas, donc StaticBody3D.
    public static StaticBody3D CreateBlock(ushort materialId, ushort shapeId, ushort rotationId, Vector3 position)
    {
        // Crée le conteneur physique
        var staticBody = new StaticBody3D();
        staticBody.Position = position;
        staticBody.Rotation = new Vector3(0, Mathf.DegToRad(rotationId * 90), 0);
                        
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
        BoxMesh shape;
        if (shapeId == Shapes.Full)
        {
            // Bloc plein : cube 1×1×1 (taille par défaut de BoxMesh)
            shape = new BoxMesh();
            meshInstance.Mesh = shape;  
        }
        else if (shapeId == Shapes.Demi)
        {
            // Demi-bloc : cube 1×0.5×1
            // Décalé de -0.25 en Y pour que le BAS du bloc soit à Y=0
            // (sinon il serait centré et flotterait)
            shape = new BoxMesh();
            meshInstance.Position = new Vector3(0.0f, -0.25f, 0.0f);
            shape.Size = new Vector3(1.0f, 0.5f, 1.0f);
            meshInstance.Mesh = shape;
        }
        else if (shapeId == Shapes.Post)
        {
            // Poteau : cube fin 0.25×1×0.25
            // Position (0,0,0) = centré dans le bloc (modifiable plus tard
            // pour la sous-grille 4×4×4)
            shape = new BoxMesh();
            meshInstance.Position = new Vector3(0.0f, 0.0f, 0.0f);
            shape.Size = new Vector3(0.25f, 1.0f, 0.25f);
            meshInstance.Mesh = shape;
        }
        else if (shapeId == Shapes.FullSlope)
        {
            // Pente pleine : mesh triangulaire créé à la main
            // Pas de BoxMesh ici — on utilise un ArrayMesh custom
            meshInstance.Mesh = CreateSlopeMesh();
        }
        else if (shapeId == Shapes.DemiSlope)
        {
            meshInstance.Mesh = CreateDemiSlopeMesh();
        }
        else
        {
            // Forme inconnue : bloc plein par défaut
            shape = new BoxMesh();
            meshInstance.Mesh = shape;
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
        //
        // TODO : Adapter la collision à la forme du bloc (pente, demi-bloc)
        
        var collisionShape = new CollisionShape3D();
        
        if (shapeId == Shapes.Demi)
        {
            collisionShape = new CollisionShape3D();  // Le conteneur

            var boxShape = new BoxShape3D();               // Le cube (séparé)
            boxShape.Size = new Vector3(1.0f, 0.5f, 1.0f); // On change SA taille

            collisionShape.Shape = boxShape;               // On met le cube dans le conteneur
            collisionShape.Position = new Vector3(0, -0.25f, 0);  // On décale le conteneur
        }
        else
        {
            collisionShape = new CollisionShape3D();
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

        return staticBody;
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
    private static Mesh CreateSlopeMesh()
    {
        // --------------------------------------------------------------------
        // LES VERTICES — Les 6 coins de la pente
        // --------------------------------------------------------------------
        // Coordonnées de -0.5 à +0.5 car le bloc est centré sur (0,0,0).
        // Pas de sommets "haut avant" — c'est là que la pente descend.
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),  // 0 : bas arrière gauche
            new Vector3( 0.5f, -0.5f, -0.5f),  // 1 : bas arrière droit
            new Vector3( 0.5f, -0.5f,  0.5f),  // 2 : bas avant droit
            new Vector3(-0.5f, -0.5f,  0.5f),  // 3 : bas avant gauche
            new Vector3(-0.5f,  0.5f, -0.5f),  // 4 : haut arrière gauche
            new Vector3( 0.5f,  0.5f, -0.5f),  // 5 : haut arrière droit
        };

        // --------------------------------------------------------------------
        // LES INDICES — Quels vertices forment chaque triangle
        // --------------------------------------------------------------------
        // L'ordre est ANTI-HORAIRE vu de l'extérieur (convention Godot).
        // Cet ordre détermine quel côté de la face est "visible".
        //
        // 5 faces au total = 8 triangles :
        //   - Bas : 2 triangles (rectangle)
        //   - Arrière : 2 triangles (rectangle)
        //   - Gauche : 1 triangle
        //   - Droite : 1 triangle
        //   - Pente : 2 triangles (rectangle incliné)
        int[] indices = new int[]
        {
            // Bas (2 triangles)
            0, 2, 1,
            0, 3, 2,
            // Arrière (2 triangles)
            0, 5, 1,
            0, 4, 5,
            // Gauche (1 triangle)
            0, 4, 3,
            // Droite (1 triangle)
            1, 5, 2,
            // Pente (2 triangles)
            2, 5, 4,
            2, 4, 3,
        };

        // --------------------------------------------------------------------
        // ASSEMBLAGE DU MESH
        // --------------------------------------------------------------------
        // ArrayMesh permet de créer un mesh à partir de tableaux de données.
        // C'est du "boilerplate" Godot — la structure est toujours la même.
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        return mesh;
    }

    private static Mesh CreateDemiSlopeMesh()
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),  // 0 : bas arrière gauche
            new Vector3( 0.5f, -0.5f, -0.5f),  // 1 : bas arrière droit
            new Vector3( 0.5f, -0.5f,  0.5f),  // 2 : bas avant droit
            new Vector3(-0.5f, -0.5f,  0.5f),  // 3 : bas avant gauche
            new Vector3(-0.5f,  0.0f, -0.5f),  // 4 : haut arrière gauche
            new Vector3( 0.5f,  0.0f, -0.5f),  // 5 : haut arrière droit
        };
        
        int[] indices = new int[]
        {
            // Bas (2 triangles)
            0, 2, 1,
            0, 3, 2,
            // Arrière (2 triangles)
            0, 5, 1,
            0, 4, 5,
            // Gauche (1 triangle)
            0, 4, 3,
            // Droite (1 triangle)
            1, 5, 2,
            // Pente (2 triangles)
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
}