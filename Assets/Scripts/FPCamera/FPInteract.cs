using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPInteract : MonoBehaviour
{
    Transform _cam_transform;
    GameObject _detected_interactable;
    private PlayerControls controls;

    [SerializeField] LayerMask layerFilters;
    [SerializeField] float detectionRayLenght = 2f;

    private void Awake()
    {
        _cam_transform = Camera.main.transform;
        controls = new PlayerControls();

        controls.Enable();

        controls.Player.Interact.performed += Interact;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        FindInteractable();
    }

    void FindInteractable()
    {
        Ray ray = new Ray(_cam_transform.position, _cam_transform.forward);

        Physics.Raycast(ray, out RaycastHit hitInfo, detectionRayLenght, layerFilters);

        print(hitInfo.collider);
        _detected_interactable = hitInfo.transform?.gameObject;
    }

    void Interact(InputAction.CallbackContext context)
    {
        if (!_detected_interactable)
            return;

        _detected_interactable.TryGetComponent<IInteractable>(out var interactable);

        if (interactable != null)
        {
            interactable.Interact(this.gameObject);
        }
    }
}
