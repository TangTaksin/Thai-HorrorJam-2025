using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ItemObject : MonoBehaviour, IInteractable
{
    public GameObject objectMesh;
    public string itemDisplayName;
    [Multiline] public string itemDesciption;

    public bool canInspect;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    public UnityEvent OnInspectorOpen;
    public UnityEvent OnInspectorClose;

    private PlayerInput playerInput;

    // --- NEW ---
    // 1. เพิ่มตัวแปรสถานะเพื่อป้องกันการกดรัว
    private bool isInspecting = false;
    // --- END NEW ---

    private void Start()
    {
        objectMesh.TryGetComponent<MeshFilter>(out meshFilter);
        objectMesh.TryGetComponent<MeshRenderer>(out meshRenderer);
    }

    public (MeshFilter, MeshRenderer) GetMesh()
    {
        return (meshFilter, meshRenderer);
    }

    public virtual void Interact(GameObject interacter)
    {
        // --- NEW ---
        // 2. ถ้ากำลังตรวจสอบอยู่ (isInspecting = true) ให้ออกจากฟังก์ชันทันที
        // นี่คือตัวป้องกันการกดรัว (Spam Guard)
        if (isInspecting) return;

        // 3. ถ้ายังไม่ได้ตรวจสอบ ก็ให้ "ล็อค" สถานะเลย
        isInspecting = true;
        // --- END NEW ---

        print(interacter.name + " attempted to interact with " + gameObject.name);

        if (interacter.TryGetComponent<PlayerInput>(out playerInput))
        {
            playerInput.DeactivateInput(); // ปิด Input
        }
        else
        {
            Debug.LogWarning("Interacter is missing PlayerInput component. Cannot disable controls.");
        }

        InspectionManager.instance?.CallInspector(this);
        InspectionManager.InspectorLoaded += OnInspectorOpened;
        InspectionManager.InspectorClosed += OnInspectorClosed;
    }

    void OnInspectorOpened(ItemObject item)
    {
        OnInspectorOpen?.Invoke();
        InspectionManager.InspectorLoaded -= OnInspectorOpened;
    }

    void OnInspectorClosed()
    {
        OnInspectorClose?.Invoke();
        InspectionManager.InspectorClosed -= OnInspectorClosed;

        if (playerInput != null)
        {
            playerInput.ActivateInput(); // เปิด Input
            playerInput = null; 
        }

        // --- NEW ---
        // 4. เมื่อปิด Inspector แล้ว ให้ "ปลดล็อค" สถานะ
        // เพื่อให้กลับมา interact ได้อีกครั้ง
        isInspecting = false;
        // --- END NEW ---
    }
}