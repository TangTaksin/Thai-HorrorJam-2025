using UnityEngine;
using System.Collections;

public class InteractCameraFocus : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("ตำแหน่งเป้าหมายที่กล้องจะเคลื่อนไปหา (สร้างเป็น Empty GameObject)")]
    public Transform cameraFocusTarget;

    [Header("Player Components")]
    [Tooltip("ใส่ Script ที่ใช้ควบคุมการเคลื่อนที่ของตัวละคร (เช่น FirstPersonController, PlayerMovement)")]
    public FPPlayerController playerControllerScript;

    [Tooltip("ใส่ Script ที่ใช้ควบคุมการหันกล้อง (ถ้ามีแยก เช่น MouseLook)")]
    public FPCameraController playerCameraScript; // อาจเป็น null ได้ถ้า script เดียวกัน

    [Header("Settings")]
    [Tooltip("ความเร็วในการเคลื่อนที่/หมุนของกล้อง")]
    public float transitionDuration = 1.5f;
    [Tooltip("ปุ่มที่ใช้ในการโต้ตอบ")]
    public KeyCode interactKey = KeyCode.E;

    private Camera mainCamera;
    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    private bool isFocused = false;
    private bool playerInRange = false;
    private Coroutine moveCoroutine;

    void Start()
    {
        mainCamera = Camera.main; // หา Main Camera อัตโนมัติ

        // ตรวจสอบว่ามี Collider ที่เป็น Trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"Object '{name}' ไม่มี Collider!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider บน '{name}' ไม่ได้ตั้งค่าเป็น 'Is Trigger'. กรุณาตั้งค่าเพื่อให้ทำงานถูกต้อง");
        }
        
        if (cameraFocusTarget == null)
        {
            Debug.LogError("ยังไม่ได้กำหนด 'cameraFocusTarget'!");
        }
        
        if (playerControllerScript == null)
        {
            Debug.LogError("ยังไม่ได้กำหนด 'playerControllerScript'!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าเป็นผู้เล่นหรือไม่ (แนะนำให้ใช้ Tag "Player" บนตัวละครผู้เล่น)
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // TODO: แสดง UI บอกให้กด E ได้ที่นี่
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // TODO: ซ่อน UI บอกให้กด E
        }
    }

    void Update()
    {
        // เมื่อผู้เล่นอยู่ในระยะและกดปุ่ม
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (!isFocused)
            {
                StartFocus();
            }
            else
            {
                StopFocus(); // กด E อีกครั้งเพื่อออก
            }
        }

        // กด Esc เพื่อออก
        if (isFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            StopFocus();
        }
    }

    public void StartFocus()
    {
        isFocused = true;

        // 1. ปิดการควบคุมผู้เล่น
        playerControllerScript.enabled = false;
        if (playerCameraScript != null)
        {
            playerCameraScript.enabled = false;
        }

        // 2. เก็บตำแหน่งและมุมกล้องเดิม
        originalCamPosition = mainCamera.transform.position;
        originalCamRotation = mainCamera.transform.rotation;

        // 3. เริ่ม Coroutine เพื่อเคลื่อนกล้อง
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCamera(cameraFocusTarget.position, cameraFocusTarget.rotation, true));
    }

    void StopFocus()
    {
        isFocused = false;

        // 1. เริ่ม Coroutine เพื่อเคลื่อนกล้องกลับ
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCamera(originalCamPosition, originalCamRotation, false));
    }

    IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot, bool isFocusing)
    {
        float time = 0;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (time < transitionDuration)
        {
            // คำนวณ % การเคลื่อนที่
            float t = time / transitionDuration;
            // ใช้ SmoothStep เพื่อให้การเคลื่อนที่นุ่มนวลขึ้น (เริ่มช้า เร่งตรงกลาง หยุดช้า)
            t = t * t * (3f - 2f * t);

            // เคลื่อนที่ (Lerp) และหมุน (Slerp)
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            time += Time.deltaTime;
            yield return null; // รอเฟรมถัดไป
        }

        // Snap ไปยังตำแหน่งสุดท้ายเป๊ะๆ
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;

        // ถ้าเป็นการเคลื่อนที่ "กลับ" (isFocusing == false)
        if (!isFocusing)
        {
            // 2. คืนการควบคุมให้ผู้เล่น
            playerControllerScript.enabled = true;
            if (playerCameraScript != null)
            {
                playerCameraScript.enabled = true;
            }
        }
        
        moveCoroutine = null;
    }
}