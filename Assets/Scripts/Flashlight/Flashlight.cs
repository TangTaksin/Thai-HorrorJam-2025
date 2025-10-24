using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 1f;

    [Header("Flicker Settings")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField] private float flickerChance = 0.02f;
    [SerializeField] private float flickerDuration = 0.1f;

    private bool isOn = true;
    private float baseIntensity;
    private float currentIntensity;
    private bool isFlickering;
    private float flickerTimer;

    private PlayerControls controls;

    private void Awake()
    {
        if (flashlight == null) flashlight = GetComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.enabled = isOn;

        baseIntensity = maxIntensity;
        currentIntensity = maxIntensity;

        // Setup Input System
        controls = new PlayerControls();
        controls.Player.ToggleFlashlight.performed += ctx => ToggleFlashlight();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        if (!isOn) return;

        HandleFlicker();
        flashlight.intensity = Mathf.Lerp(flashlight.intensity, currentIntensity, Time.deltaTime * 10f);
    }

    private void ToggleFlashlight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
    }

    private void HandleFlicker()
    {
        if (!enableFlicker) return;

        if (!isFlickering && Random.value < flickerChance)
        {
            isFlickering = true;
            flickerTimer = flickerDuration * Random.value;
            currentIntensity = Random.Range(minIntensity, baseIntensity);
        }

        if (isFlickering)
        {
            flickerTimer -= Time.deltaTime;
            if (flickerTimer <= 0f)
            {
                isFlickering = false;
                currentIntensity = baseIntensity;
            }
        }
    }
}
