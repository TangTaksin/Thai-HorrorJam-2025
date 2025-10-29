using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // เวลาที่ใช้ในการเคลื่อนที่ (วินาที)
    public float transitionDuration = 1.5f;

    // Coroutine ที่กำลังทำงานอยู่ (สำหรับหยุดอันเก่าก่อนเริ่มอันใหม่)
    private Coroutine activeCoroutine;

    // นี่คือฟังก์ชันที่เราจะเรียกจากปุ่ม UI
    // มันต้องเป็น public void
    public void SwitchCameraView(Transform targetView)
    {
        // ถ้ากำลังย้ายกล้องอยู่ ให้หยุดอันเก่าก่อน
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        // เริ่ม Coroutine ใหม่เพื่อย้ายกล้องไปยังเป้าหมาย
        activeCoroutine = StartCoroutine(MoveToTarget(targetView));
    }

    private IEnumerator MoveToTarget(Transform target)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (elapsedTime < transitionDuration)
        {
            // คำนวณ % ความคืบหน้า (0.0 ถึง 1.0)
            float t = elapsedTime / transitionDuration;

            // ใช้ SmoothStep เพื่อให้มีการเร่งและผ่อน (Ease-In & Ease-Out)
            t = t * t * (3f - 2f * t);

            // Lerp ตำแหน่งและมุมหมุน
            transform.position = Vector3.Lerp(startPosition, target.position, t);
            transform.rotation = Quaternion.Lerp(startRotation, target.rotation, t);

            // เพิ่มเวลา
            elapsedTime += Time.deltaTime;

            yield return null; // รอเฟรมถัดไป
        }

        // เมื่อจบ Loop ให้กำหนดค่าเป๊ะๆ ไปเลยกันคลาดเคลื่อน
        transform.position = target.position;
        transform.rotation = target.rotation;

        activeCoroutine = null; // เคลียร์ Coroutine ที่จบไปแล้ว
        gameObject.SetActive(false); // ปิดกล้องหลังเปลี่ยนมุมมองเสร็จ
    }

}
