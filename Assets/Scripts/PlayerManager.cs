using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public FPPlayerController fPplayerController;
    public FPCameraController fPCameraController;
    public FPHeadBob fPHeadBob;
    public CameraSwayTilt cameraSwayTilt;
    public FPInteract fPInteracter;
    public FPCameraFlashlight fPCameraFlashlight;

    private void OnEnable()
    {
        GameManager.OnStateChanged += StateChecks;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= StateChecks;
    }

    void StateChecks(GameState state)
    {
        var isPlaying = (state == GameState.Playing);

        fPCameraController.HandlePause(isPlaying);
        fPInteracter.enabled = isPlaying;
        fPCameraFlashlight.enabled = isPlaying;
    }
}
