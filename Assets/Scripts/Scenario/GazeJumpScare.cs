using UnityEngine;
using NaughtyAttributes; // (Optional) ถ้าคุณมี NaughtyAttributes

[RequireComponent(typeof(Collider))] // บังคับว่าต้องมี Collider
public class GazeJumpScare : MonoBehaviour
{
    [Header("Target Manager")]
    [Tooltip("ลาก GameObject ที่มี 'JumpScareManager' (ตัวหลัก) มาใส่ที่นี่")]
    [Required] // (NaughtyAttributes)
    [SerializeField] private JumpScareManager jumpScareManager;

    [Header("Gaze Settings")]
    [Tooltip("ต้องจ้องมองนานกี่วินาที (เช่น 3)")]
    [SerializeField] private float gazeDuration = 3f;
    
    [Tooltip("ระยะทางสูงสุดที่การจ้องมองจะทำงาน")]
    [SerializeField] private float maxGazeDistance = 50f;
    
    [Tooltip("ความแม่นยำ (0.9 = ต้องจ้องเกือบกลางจอ, 0.5 = จ้องเฉียดๆ ก็นับ)")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float gazeAccuracy = 0.8f;

    [Tooltip("ขนาดของวัตถุ (สำหรับเช็คว่าอยู่ในหน้าจอหรือไม่)")]
    [SerializeField] private float objectSizeForCheck = 1.0f;

    // --- ตัวแปรภายใน ---
    private Camera mainCamera;
    private float gazeTimer = 0f;
    private bool hasTriggered = false;
    private Collider objCollider;
    private Plane[] cameraPlanes; // สำหรับเช็คว่าอยู่ในจอหรือไม่

    void Start()
    {
        mainCamera = Camera.main;
        objCollider = GetComponent<Collider>(); // เก็บ Collider ของตัวเอง

        if (jumpScareManager == null)
        {
            Debug.LogError("GazeJumpScare: ยังไม่ได้ตั้งค่า 'JumpScareManager'!");
            enabled = false;
        }
    }

    void Update()
    {
        // ถ้าทำงานไปแล้ว หรือหา Manager ไม่เจอ ก็ไม่ต้องทำอะไรต่อ
        if (hasTriggered || jumpScareManager == null || mainCamera == null)
        {
            return;
        }

        // --- หัวใจของ Script ---
        if (IsPlayerGazingAtThis())
        {
            // 1. ถ้าผู้เล่นกำลังจ้อง: เริ่มนับเวลา
            gazeTimer += Time.deltaTime;
            
            // (Optional) แสดง Debug
            // Debug.Log($"Gazing... {gazeTimer:F1} / {gazeDuration}");

            // 2. ถ้าเวลาถึงกำหนด: สั่ง Jumpscare!
            if (gazeTimer >= gazeDuration)
            {
                hasTriggered = true;
                gazeTimer = 0f;
                Debug.Log("Gaze Triggered! Firing JumpScare.");
                
                // ไปเรียก Function 'TriggerJumpScare' ของ Manager ตัวหลัก
                jumpScareManager.TriggerJumpScare();
            }
        }
        else
        {
            // 3. ถ้าผู้เล่นละสายตา (หรือมีอะไรบัง): รีเซ็ตเวลา
            gazeTimer = 0f;
        }
    }

    /// <summary>
    /// ตรวจสอบว่าผู้เล่น "จ้อง" วัตถุนี้โดยตรงหรือไม่
    /// </summary>
    private bool IsPlayerGazingAtThis()
    {
        // --- เช็คที่ 1: วัตถุนี้อยู่ในหน้าจอหรือไม่? (Frustum Culling) ---
        // (นี่เป็นการเช็คคร่าวๆ ที่เร็วมาก)
        Bounds objectBounds = new Bounds(transform.position, Vector3.one * objectSizeForCheck);
        cameraPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        
        if (!GeometryUtility.TestPlanesAABB(cameraPlanes, objectBounds))
        {
            return false; // อยู่นอกจอโดยสิ้นเชิง
        }

        // --- เช็คที่ 2: ผู้เล่นหันหน้าไปทางวัตถุนี้ "ตรงๆ" หรือไม่? (Dot Product) ---
        Vector3 directionToObject = (transform.position - mainCamera.transform.position).normalized;
        float dot = Vector3.Dot(mainCamera.transform.forward, directionToObject);
        
        // ถ้าค่า dot ต่ำกว่า 'gazeAccuracy' = ผู้เล่นมองเฉียดๆ (อยู่ขอบจอ)
        if (dot < gazeAccuracy) 
        {
            return false; 
        }

        // --- เช็คที่ 3: มีอะไรบังระหว่างกล้องกับวัตถุหรือไม่? (Raycast) ---
        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, directionToObject, out hit, maxGazeDistance))
        {
            // ถ้าสิ่งที่ Raycast ชน คือ 'Collider' ของตัวเราเอง
            if (hit.collider == objCollider)
            {
                // แปลว่ามองเห็น! (ไม่มีอะไรบัง)
                return true;
            }
        }

        // ถ้า Raycast ไม่ชนอะไรเลย (ไกลไป) หรือ ชนอย่างอื่น (เช่น ผนัง)
        return false;
    }

    /// <summary>
    /// วาด Gizmos ให้เห็นขอบเขต (Bounds) ที่ใช้เช็ค
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 1, 0.5f); // สีม่วง
        Gizmos.DrawWireCube(transform.position, Vector3.one * objectSizeForCheck);
    }
}