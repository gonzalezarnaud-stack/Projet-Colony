using Godot;

namespace ProjetColony.Engine.Input;

public class CameraController
{
    private Camera3D _camera;
    private Node3D _playerBody;
    private float _rotationX;
    private float _rotationY;
    private float _mouseSensitivity = 0.002f;

    public CameraController(Camera3D camera, Node3D playerBody)
    {
        _camera = camera;
        _playerBody = playerBody;
    }

    public void HandleMouseMotion(Vector2 relativeMotion)
    {
        // Mouvement horizontal souris → rotation Y (tourner sur soi)
        _rotationY += -relativeMotion.X * _mouseSensitivity;
            
        // Mouvement vertical souris → rotation X (hocher la tête)
        _rotationX += -relativeMotion.Y * _mouseSensitivity;
            
        // Limite pour ne pas faire de looping
        _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            
        // Applique : corps tourne horizontalement, caméra verticalement
        _playerBody.Rotation = new Vector3(0, _rotationY, 0);
        _camera.Rotation = new Vector3(_rotationX, 0, 0);
    }
}