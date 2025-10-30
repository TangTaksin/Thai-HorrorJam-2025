using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public FPPlayerController fPplayerController;
    public FPCameraController fPCameraController;
    public FPHeadBob fPHeadBob;
    public CameraSwayTilt cameraSwayTilt;
    public FPInteract fPInteracter;
    public FPCameraFlashlight fPCameraFlashlight;
    public GameObject flashlightFPObject;

    private void OnEnable()
    {
        GameManager.OnStateChanged += StateChecks;
        GameManager.OnVariableUpdate += CheckGMVariables;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= StateChecks;
        GameManager.OnVariableUpdate -= CheckGMVariables;
    }

    void StateChecks(GameState state)
    {
        var isPlaying = (state == GameState.Playing);

        fPCameraController.HandlePause(isPlaying);
        fPInteracter.enabled = isPlaying;
        fPCameraFlashlight.enabled = isPlaying;
    }

    void CheckGMVariables()
    {
        var haveFlashlight = GameManager.Instance.haveflashlight;

        flashlightFPObject?.SetActive(haveFlashlight);
    }
}
