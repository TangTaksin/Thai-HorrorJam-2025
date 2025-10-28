using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public FPPlayerController fPplayerController;
    public FPCameraController fPCameraController;
    public CameraSwayTilt cameraSwayTilt;
    public FPInteract fPInteracter;

    private void OnEnable()
    {
        GameManager.Instance.OnStateChanged += StateChecks;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnStateChanged -= StateChecks;
    }

    void StateChecks(GameState state)
    {
        


    }
}
