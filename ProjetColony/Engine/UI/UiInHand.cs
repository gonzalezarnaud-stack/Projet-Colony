// ============================================================================
// UIINHAND.CS — Affichage du bloc sélectionné
// ============================================================================
// Ce fichier est dans Engine/UI, donc il fait partie de ENGINE.
// Il dépend de Godot (Label, Vector2, etc.)
//
// CE QU'IL FAIT :
// Affiche en haut à gauche de l'écran ce que le joueur a "en main" :
// le matériau, la forme et la rotation du bloc qui sera posé.
// Format : "Pierre | Full | 0°"
//
// POURQUOI HÉRITER DE LABEL ?
// Label est un Control Godot qui affiche du texte.
// En héritant, UiInHand EST un Label — on peut directement modifier
// sa propriété Text pour changer ce qui s'affiche.
//
// ARCHITECTURE MODULAIRE :
// Ce fichier ne gère QUE l'affichage du bloc en main.
// Plus tard, on aura d'autres fichiers UI :
//   - UiHealthBar.cs → barre de vie
//   - UiHungerBar.cs → barre de faim (MVP-C)
//   - HUD.cs → conteneur qui rassemble tous les éléments UI
//
// ÉVOLUTION FUTURE :
// - Afficher une icône du bloc au lieu de juste du texte
// - Intégrer dans une barre d'inventaire avec la molette souris
// ============================================================================

using Godot;

namespace ProjetColony.Engine.UI;

public partial class UiInHand : Label
{
    // ------------------------------------------------------------------------
    // ÉTAT ACTUEL DE LA SÉLECTION
    // ------------------------------------------------------------------------
    // Ces variables stockent ce qui est affiché. Mises à jour par Player.cs
    // quand le joueur change de matériau, forme ou rotation.
    private string _materialName = "Pierre";
    private string _shapeName = "Full";
    private int _rotation = 0;
    private int _rotationX = 0;

    // Mode de placement actuel (Normal ou Fin)
    // Mis à jour par Player.cs quand on appuie sur F.
    private bool _fineMode = false;

    // ------------------------------------------------------------------------
    // UPDATEDISPLAY — Reconstruit le texte affiché
    // ------------------------------------------------------------------------
    // Appelée après chaque changement pour mettre à jour le Label.
    // Combine les trois valeurs avec des séparateurs.
    // La rotation est convertie en degrés (0, 1, 2, 3 → 0°, 90°, 180°, 270°).
    private void UpdateDisplay()
    {
        string modeName = _fineMode ? "Fin" : "Normal";
        Text = _materialName + " | " + _shapeName + " | Y:" + (_rotation * 90) + "° X:" + (_rotationX * 90) + "° | " + modeName;
    }

    // ========================================================================
    // _READY — Initialisation
    // ========================================================================
    // Appelée une fois quand le Label est ajouté à la scène.
    // Configure la position et affiche les valeurs par défaut.
    public override void _Ready()
    {
        base._Ready();
        Position = new Vector2(20, 20);
        UpdateDisplay();        
    }

    // ========================================================================
    // SETTERS — Méthodes pour mettre à jour l'affichage
    // ========================================================================
    // Appelées par Player.cs quand le joueur change de sélection.
    // Chaque méthode stocke la nouvelle valeur puis rafraîchit l'affichage.

    // Touches 1, 2, 3 — change le matériau
    public void SetMaterial(string materialName)
    {
        _materialName = materialName;
        UpdateDisplay();
    }

    // Touches 4, 5, 6, 7, 8 — change la forme
    public void SetShape(string shapeName)
    {
        _shapeName = shapeName;
        UpdateDisplay();
    }

    // Touche R — change la rotation
    public void SetRotation(int rotation)
    {
        _rotation = rotation;
        UpdateDisplay();
    }

    public void SetRotationX(int rotationX)
    {
        _rotationX = rotationX;
        UpdateDisplay();
    }

    // ------------------------------------------------------------------------
    // SETFINEMODE — Change l'affichage du mode de placement
    // ------------------------------------------------------------------------
    // Appelée par Player.cs quand le joueur appuie sur F.
    // Mode Normal = placement par voxel entier
    // Mode Fin = placement dans la sous-grille 4×4×4
    public void SetFineMode(bool fineMode)
    {
        _fineMode = fineMode;
        UpdateDisplay();
    }
}