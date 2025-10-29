using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RandomSign : MonoBehaviour
{
    [Header("ตั้งค่าสัญลักษณ์")]
    public Material[] symbolMaterials;

    [Header("ตั้งค่าการเปลี่ยนแปลง")]
    public float timeToChange = 3.0f;
    [Range(0f, 1f)] public float changeProbability = 0.75f;

    [Header("การตรวจจับสายตาผู้เล่น")]
    public Camera playerCamera;
    public LayerMask obstructionLayers = Physics.DefaultRaycastLayers;

    [Header("การเพิ่มประสิทธิภาพ")]
    [Tooltip("ความถี่ในการตรวจสอบการมองเห็น (ครั้งต่อวินาที)")]
    public float visibilityCheckRate = 10f;
    
    // --- เพิ่มเข้ามาใหม่ ---
    [Tooltip("ระยะทางสูงสุดที่สคริปต์จะเริ่มตรวจสอบ (เมตร)")]
    public float maxDistance = 50f;
    // ----------------------

    [Header("ตัวเลือกการเปลี่ยน Material")]
    [Tooltip("เลือก false เพื่อเปลี่ยน Material ได้หลายครั้ง")]
    public bool changeOnce = false;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Renderer objectRenderer;
    private int currentMaterialIndex;
    private Material[] materialInstances;

    private bool hasBeenSeen = false;
    private bool isVisibleNow = false;
    private bool hasChangedWhileInvisible = false;
    private float invisibleTimer = 0f;
    private float visibilityCheckTimer = 0f;

    // --- เพิ่มเข้ามาใหม่ ---
    private float maxDistanceSqr; // เก็บค่าระยะทางยกกำลังสองเพื่อประสิทธิภาพ
    // ----------------------

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("TrailBlazeChanger: ไม่มี Renderer!", this);
            enabled = false;
            return;
        }

        if (symbolMaterials == null || symbolMaterials.Length < 2)
        {
            Debug.LogError("TrailBlazeChanger: ต้องมี Material อย่างน้อย 2 ชิ้น!", this);
            enabled = false;
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        // --- เพิ่มเข้ามาใหม่ ---
        // คำนวณค่า maxDistance ยกกำลังสองไว้ล่วงหน้า
        maxDistanceSqr = maxDistance * maxDistance;
        // ----------------------

        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        // สร้าง Material instances เพื่อไม่ให้กระทบ Material ต้นฉบับ
        materialInstances = new Material[symbolMaterials.Length];
        for (int i = 0; i < symbolMaterials.Length; i++)
        {
            materialInstances[i] = new Material(symbolMaterials[i]);
            materialInstances[i].name = symbolMaterials[i].name + "_Instance";
        }

        // หา material เริ่มต้น
        Material initial = objectRenderer.sharedMaterial;
        currentMaterialIndex = -1;
        for (int i = 0; i < symbolMaterials.Length; i++)
        {
            // เปรียบเทียบ material ต้นฉบับ ไม่ใช่ instance
            if (symbolMaterials[i] == initial)
            {
                currentMaterialIndex = i;
                break;
            }
        }

        if (currentMaterialIndex == -1)
            currentMaterialIndex = 0;

        objectRenderer.material = materialInstances[currentMaterialIndex];
    }

    void Update()
    {
        if (playerCamera == null) return;

        // --- เพิ่มเข้ามาใหม่ ---
        // ตรวจสอบระยะทางก่อนเพื่อเพิ่มประสิทธิภาพ
        // ใช้ .sqrMagnitude เพื่อหลีกเลี่ยงการคำนวณ Square Root ที่ช้า
        float sqrDist = (playerCamera.transform.position - objectRenderer.bounds.center).sqrMagnitude;
        if (sqrDist > maxDistanceSqr)
        {
            // ผู้เล่นอยู่ไกลเกินไป ไม่ต้องทำอะไร
            // รีเซ็ตสถานะเพื่อให้เริ่มทำงานใหม่เมื่อเข้าใกล้
            isVisibleNow = false;
            invisibleTimer = 0f;
            // (hasBeenSeen สามารถคงไว้ หรือรีเซ็ตก็ได้ ขึ้นอยู่กับดีไซน์)
            // hasBeenSeen = false; 
            return; // ออกจาก Update ทันที
        }
        // ----------------------


        // จำกัดความถี่ในการตรวจสอบการมองเห็น
        visibilityCheckTimer += Time.deltaTime;
        float checkInterval = 1f / visibilityCheckRate;

        if (visibilityCheckTimer >= checkInterval)
        {
            isVisibleNow = IsActuallyVisible();
            visibilityCheckTimer = 0f;
        }

        if (isVisibleNow)
        {
            hasBeenSeen = true;
            invisibleTimer = 0f;

            if (changeOnce && hasChangedWhileInvisible)
                return;

            hasChangedWhileInvisible = false;
        }
        else if (hasBeenSeen && !hasChangedWhileInvisible)
        {
            invisibleTimer += Time.deltaTime;

            if (invisibleTimer >= timeToChange)
            {
                TryChangeSymbol();
                hasChangedWhileInvisible = true;
            }
        }
    }

    private bool IsActuallyVisible()
    {
        Vector3 camPos = playerCamera.transform.position;
        Vector3 targetPos = objectRenderer.bounds.center;
        float dist = Vector3.Distance(camPos, targetPos);

        // ตรวจสอบว่าอยู่ใน viewport หรือไม่
        Vector3 viewPos = playerCamera.WorldToViewportPoint(targetPos);

        bool inView =
            viewPos.z > playerCamera.nearClipPlane &&
            viewPos.z < playerCamera.farClipPlane &&
            viewPos.x > 0f && viewPos.x < 1f &&
            viewPos.y > 0f && viewPos.y < 1f;

        if (!inView)
        {
            if (showDebugLogs) Debug.Log($"{gameObject.name}: ไม่อยู่ใน viewport");
            return false;
        }

        // ตรวจสอบการบดบัง
        Vector3 dir = (targetPos - camPos).normalized;
        if (Physics.Raycast(camPos, dir, out RaycastHit hit, dist, obstructionLayers))
        {
            if (hit.collider.gameObject != gameObject)
            {
                if (showDebugLogs) Debug.Log($"{gameObject.name}: ถูกบดบังโดย {hit.collider.gameObject.name}");
                return false;
            }
        }

        if (showDebugLogs) Debug.Log($"{gameObject.name}: มองเห็นได้");
        return true;
    }

    private void TryChangeSymbol()
    {
        if (Random.value <= changeProbability)
        {
            int newIndex;
            do
            {
                newIndex = Random.Range(0, materialInstances.Length);
            } while (newIndex == currentMaterialIndex && materialInstances.Length > 1);

            currentMaterialIndex = newIndex;
            objectRenderer.material = materialInstances[currentMaterialIndex];

            if (showDebugLogs)
                Debug.Log($"{gameObject.name}: เปลี่ยนเป็น material #{currentMaterialIndex}");
        }
    }

    void OnDestroy()
    {
        // ทำความสะอาด material instances
        if (materialInstances != null)
        {
            foreach (Material mat in materialInstances)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // --- เพิ่มเข้ามาใหม่ ---
        // วาด Gizmo ของระยะทำงาน
        if (objectRenderer != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(objectRenderer.bounds.center, maxDistance);
        }
        // ----------------------

        if (playerCamera != null && objectRenderer != null)
        {
            Gizmos.color = isVisibleNow ? Color.green : Color.red;
            Gizmos.DrawLine(playerCamera.transform.position, objectRenderer.bounds.center);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(objectRenderer.bounds.center, 0.2f);
        }
    }
#endif
}