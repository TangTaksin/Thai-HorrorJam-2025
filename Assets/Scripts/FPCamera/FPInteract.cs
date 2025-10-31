using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPInteract : MonoBehaviour
{
    public static FPInteract Instance { get; private set; }
    [Header("Core Settings")]
    [Tooltip("ระยะยิง Raycast สูงสุด (ไกลที่สุดเท่าที่เป็นไปได้)")]
    [SerializeField] private float maxRaycastDistance = 10f;
    [Tooltip("Layer ทั้งหมดที่ Raycast จะตรวจจับ")]
    [SerializeField] private LayerMask layerFilters;

    [Header("References")]
    [Tooltip("ลาก Crosshair Controller มาใส่ (เพื่อดึงค่า Settings)")]
    [SerializeField] private CrosshairController crosshairController;

    [SerializeField] private AudioEvent interactSound;

    public IInteractable DetectedInteractable
    {
        get;
        private set;
    }

    private RaycastHit currentHitInfo;
    private bool hasHitInfo;
    private SignPainter lastPainter;
    private TextureEraser lastEraser;

    private Transform _cam_transform;
    private PlayerControls controls;
    private IInteractable _previouslyDetectedInteractable;

    public static Action<IInteractable> OnDetectedInteractableChanged;

    private void Awake()
    {
        _cam_transform = Camera.main.transform;
        controls = new PlayerControls();

        controls.Player.Interact.performed += Interact;

        if (crosshairController == null)
            Debug.LogError("FPInteract: ยังไม่ได้ลาก CrosshairController ใส่ใน Inspector!");
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        FindInteractable();

        HandlePainting();
        // HandleEraser();
    }

    void FindInteractable()
    {
        // ใช้ตัวแปร 'local' ชั่วคราวเก็บค่าที่เจอในเฟรมนี้
        IInteractable newlyDetectedInteractable = null;
        hasHitInfo = false;

        if (crosshairController == null) return;

        Ray ray = new Ray(_cam_transform.position, _cam_transform.forward);

        if (Physics.Raycast(ray, out currentHitInfo, maxRaycastDistance, layerFilters))
        {
            hasHitInfo = true;
            int objectLayer = currentHitInfo.collider.gameObject.layer;
            CrosshairLayerSetting setting = crosshairController.GetSettingForLayer(objectLayer);

            if (setting != null && currentHitInfo.distance <= setting.maxDistance)
            {
                if (currentHitInfo.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    // เราเจอของ
                    newlyDetectedInteractable = interactable;
                }
            }
        }

        // --- นี่คือส่วนสำคัญที่แก้ไข ---

        // 1. อัปเดต Property หลัก (เพื่อให้ HandlePainting/Eraser ทำงานได้)
        DetectedInteractable = newlyDetectedInteractable;

        // 2. เช็คว่าของที่เจอ "ตอนนี้" (DetectedInteractable) 
        //    ต่างจากของที่เจอ "เฟรมที่แล้ว" (_previouslyDetectedInteractable) หรือไม่
        if (DetectedInteractable != _previouslyDetectedInteractable)
        {
            // 3. ถ้า "ต่าง" ค่อยยิง Event บอก UI
            OnDetectedInteractableChanged?.Invoke(DetectedInteractable);

            // 4. "จำ" ของที่เจอในเฟรมนี้ไว้ เพื่อใช้เทียบในเฟรมหน้า
            _previouslyDetectedInteractable = DetectedInteractable;
        }
    }

    void Interact(InputAction.CallbackContext context)
    {
        if (DetectedInteractable != null)
        {
            DetectedInteractable.Interact(this.gameObject);
            SOAudioManager.Instance?.PlaySFX(interactSound);
        }
    }

    void HandlePainting()
    {
        bool isPainting = controls.Player.Draw.IsPressed();
        SignPainter currentPainter = null;

        if (isPainting && DetectedInteractable != null && hasHitInfo)
        {
            if (DetectedInteractable is SignPainter painter)
            {
                painter.ExternalPaint(currentHitInfo.textureCoord);
                currentPainter = painter;
            }
        }

        if (lastPainter != null && lastPainter != currentPainter)
        {
            lastPainter.StopPainting();
        }

        lastPainter = currentPainter;
    }

    // void HandleEraser()
    // {
    //     // --- สันนิษฐานว่าคุณมี Input Action ใหม่ชื่อ "Erase" ---
    //     // (ถ้าคุณยังไม่ได้สร้าง ให้ไปที่ PlayerControls asset
    //     // แล้วสร้าง Action ใหม่ ตั้งชื่อว่า "Erase" 
    //     // แล้ว bind เข้ากับปุ่ม เช่น Right Mouse Button)

    //     bool isErasing = controls.Player.Draw.IsPressed();
    //     TextureEraser currentEraser = null;

    //     if (isErasing && DetectedInteractable != null && hasHitInfo)
    //     {
    //         // ตรวจสอบว่า Object ที่เราเล็งอยู่คือ "TextureEraser" หรือไม่
    //         if (DetectedInteractable is TextureEraser eraser)
    //         {
    //             // ถ้าใช่ ให้สั่งลบโดยใช้ UV ที่ได้จาก Raycast
    //             // (*** อย่าลืม! Object ที่ลบต้องใช้ MeshCollider ***)
    //             eraser.ExternalPaint(currentHitInfo.textureCoord);
    //             currentEraser = eraser;
    //         }
    //     }

    //     // ลอจิกสำคัญ: ถ้าเราหยุดกด "ลบ" หรือหันไปเล็ง Object อื่น
    //     // ให้สั่ง "ยางลบอันเก่า" หยุดทำงาน (เพื่อไม่ให้เส้นลากต่อกัน)
    //     if (lastEraser != null && lastEraser != currentEraser)
    //     {
    //         // เรียกใช้ StopPainting() บน TextureEraser
    //         lastEraser.StopPainting();
    //     }

    //     lastEraser = currentEraser;
    // }


    public GameObject GetDetectedObject()
    {
        if (DetectedInteractable == null)
            return null;
        MonoBehaviour interactableMono = DetectedInteractable as MonoBehaviour;
        return interactableMono != null ? interactableMono.gameObject : null;
    }

    private void OnDrawGizmos()
    {
        Transform camTransform = _cam_transform;
        if (camTransform == null)
        {
            camTransform = Camera.main?.transform;
        }

        if (camTransform == null)
        {
            return;
        }

        Ray ray = new Ray(camTransform.position, camTransform.forward);

        Gizmos.color = Color.grey;
        Gizmos.DrawRay(ray.origin, ray.direction * maxRaycastDistance);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxRaycastDistance, layerFilters))
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(hitInfo.point, 0.05f);

            if (crosshairController == null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(ray.origin, hitInfo.point);
                return;
            }

            int objectLayer = hitInfo.collider.gameObject.layer;
            CrosshairLayerSetting setting = crosshairController.GetSettingForLayer(objectLayer);

            if (setting != null)
            {
                if (hitInfo.distance <= setting.maxDistance)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(ray.origin, hitInfo.point);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(ray.origin, hitInfo.point);
                }
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(ray.origin, hitInfo.point);
            }
        }
    }
}