// ============================================================================
// BUILDINGSTATE.CS — État du mode construction
// ============================================================================
// Ce fichier est dans Core/Building, donc il fait partie de CORE.
// AUCUNE dépendance à Godot — c'est de la simulation pure.
//
// C'EST QUOI UNE CLASS ?
// Une classe est un modèle pour créer des objets. Contrairement à une struct,
// une classe est passée par RÉFÉRENCE : quand tu la donnes à une fonction,
// c'est la même instance qui est modifiée partout.
//
// POURQUOI CLASS ET PAS STRUCT ?
// BuildingState est un objet qu'on modifie souvent pendant le jeu :
// - Le joueur change de mode (normal ↔ fin)
// - Le joueur sélectionne un matériau, une forme
// - Le joueur clique sur une surface
//
// On veut qu'il y ait UNE SEULE instance de BuildingState, partagée par
// tous les systèmes qui en ont besoin. Class est parfait pour ça.
//
// C'EST QUOI UN ÉTAT ?
// L'état, c'est "la situation actuelle". À tout moment, BuildingState
// répond aux questions :
// - Est-on en mode fin ? (IsFineMode)
// - Quel bloc tient le joueur ? (SelectedMaterialId, SelectedShapeId, etc.)
// - Y a-t-il une surface sélectionnée ? (HasSelectedSurface)
//
// AVANT : Ces infos étaient éparpillées dans Player.cs (15+ variables).
// MAINTENANT : Tout est regroupé ici, dans une seule classe.
// ============================================================================
namespace ProjetColony.Core.Building;

public class BuildingState
{

    // ------------------------------------------------------------------------
    // MODE DE CONSTRUCTION
    // ------------------------------------------------------------------------
    // Ces deux booléens définissent dans quel "mode" on est.
    //
    // IsFineMode :
    //   false = mode normal (un bloc = un voxel entier)
    //   true = mode fin (sous-grille 3×3×3, placement précis)
    //
    // HasSelectedSurface :
    //   En mode fin, on doit d'abord cliquer sur une face pour la sélectionner.
    //   false = pas encore de surface sélectionnée
    //   true = surface sélectionnée, on peut placer des blocs dessus
    public bool IsFineMode;
    public bool HasSelectedSurface;

    // ------------------------------------------------------------------------
    // BLOC EN MAIN
    // ------------------------------------------------------------------------
    // Ce que le joueur va poser quand il clique.
    //
    // SelectedMaterialId : le matériau (pierre=1, terre=2, herbe=3, etc.)
    // SelectedShapeId : la forme (plein=0, tiers=1, poteau=4, etc.)
    // SelectedRotationY : rotation horizontale (0=0°, 1=90°, 2=180°, 3=270°)
    // SelectedRotationX : rotation verticale (pour coucher un bloc)
    //
    // "ushort" = nombre entier positif de 0 à 65535
    // On utilise ushort car les IDs sont toujours positifs et petits.
    public ushort SelectedMaterialId;
    public ushort SelectedShapeId;
    public ushort SelectedRotationY;
    public ushort SelectedRotationX;

    // ------------------------------------------------------------------------
    // SURFACE DE TRAVAIL (mode fin uniquement)
    // ------------------------------------------------------------------------
    // Quand on clique sur une face en mode fin, on "verrouille" cette surface.
    // Le highlight se déplace alors en 2D sur cette surface.
    //
    // SurfaceVoxelX/Y/Z : le voxel OÙ ON VA POSER (pas celui qu'on a cliqué)
    // SurfaceNormalX/Y/Z : la direction de la face (ex: 0,1,0 = face du dessus)
    //
    // Exemple : tu cliques sur le dessus d'un bloc à (5, 10, 3)
    //   → SurfaceVoxel = (5, 11, 3)  ← le voxel AU-DESSUS
    //   → SurfaceNormal = (0, 1, 0) ← direction "vers le haut"
    public int SurfaceVoxelX;
    public int SurfaceVoxelY;
    public int SurfaceVoxelZ;

    public float SurfaceNormalX;
    public float SurfaceNormalY;
    public float SurfaceNormalZ;

    // ------------------------------------------------------------------------
    // SUB FIXÉ (clic sur un poteau)
    // ------------------------------------------------------------------------
    // Quand on clique sur un poteau (bloc stackable), on veut se COLLER à lui,
    // pas aller au bord du voxel.
    //
    // Exemple : poteau en sub (2, 2, 2), on clique sur sa face droite (+X)
    //   → Le nouveau bloc doit aller en sub (3, 2, 2), collé au poteau
    //   → On stocke ça dans FixedSubX/Y/Z
    //
    // HasFixedSub :
    //   false = on a cliqué sur un bloc plein, pas de sub fixé
    //   true = on a cliqué sur un poteau, utiliser FixedSubX/Y/Z
    public int FixedSubX;
    public int FixedSubY;
    public int FixedSubZ;
    public bool HasFixedSub;

    // ------------------------------------------------------------------------
    // CONSTRUCTEUR — Appelé quand on fait "new BuildingState()"
    // ------------------------------------------------------------------------
    // Un constructeur est une méthode spéciale qui :
    //   - Porte le MÊME NOM que la classe
    //   - N'a PAS de type de retour (pas de void, pas de int, rien)
    //   - S'exécute automatiquement quand on crée l'objet avec "new"
    //
    // POURQUOI EN A-T-ON BESOIN ?
    // Sans constructeur, toutes les valeurs sont à 0 par défaut.
    // Or, SelectedMaterialId = 0 signifie "Air" (pas de bloc).
    // On veut que le joueur commence avec de la pierre (ID = 1).
    //
    // EXEMPLE :
    //   var state = new BuildingState();
    //   // À ce moment, le constructeur s'exécute
    //   // state.SelectedMaterialId vaut maintenant 1, pas 0
    public BuildingState()
    {
        SelectedMaterialId = 1;
        SelectedShapeId = 0;
    }

    // ------------------------------------------------------------------------
    // RESET — Remet l'état de construction à zéro
    // ------------------------------------------------------------------------
    // Appelé quand on veut tout réinitialiser :
    //   - Le joueur quitte le mode construction
    //   - On charge une nouvelle partie
    //   - On veut annuler la sélection en cours
    //
    // VALEURS PAR DÉFAUT :
    //   - Mode normal (pas fin)
    //   - Pas de surface sélectionnée
    //   - Pierre en main (MaterialId = 1)
    //   - Bloc plein (ShapeId = 0)
    //   - Pas de rotation
    //
    // NOTE FUTURE :
    // Quand on aura un inventaire avec hotbar, SelectedMaterialId et
    // SelectedShapeId viendront de l'item sélectionné, pas d'ici.
    // On refactorisera à ce moment-là.
    public void Reset()
    {
        IsFineMode = false;
        HasSelectedSurface = false;
        SelectedMaterialId = 1;
        SelectedShapeId = 0;
        SelectedRotationY = 0;
        SelectedRotationX = 0;
        HasFixedSub = false;
    }
}