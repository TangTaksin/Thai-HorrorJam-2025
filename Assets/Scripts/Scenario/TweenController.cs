using UnityEngine;
using DG.Tweening; // << สำคัญมาก! ต้องมี DOTween ก่อน

public class TweenController : MonoBehaviour
{
    [Header("เป้าหมายการเคลื่อนไหว")]
    [Tooltip("ติ๊กถูกถ้าต้องการให้เคลื่อนที่ (Move)")]
    public bool doMove = false;
    public Vector3 targetPosition;
    [Tooltip("ติ๊กถูก ถ้าจะให้เคลื่อนที่ในพิกัด Local (เทียบกับ Parent)")]
    public bool useLocalMove = true;

    [Header("เป้าหมายการหมุน")]
    [Tooltip("ติ๊กถูกถ้าต้องการให้หมุน (Rotate)")]
    public bool doRotate = false;
    public Vector3 targetRotation; // หมุนไปกี่องศา (Euler)
    [Tooltip("ติ๊กถูก ถ้าจะให้หมุนในพิกัด Local (เทียบกับ Parent)")]
    public bool useLocalRotate = true;

    [Header("เป้าหมายการขยาย/ย่อ")]
    [Tooltip("ติ๊กถูกถ้าต้องการให้ขยาย/ย่อ (Scale)")]
    public bool doScale = false;
    public Vector3 targetScale = Vector3.one;

    [Header("การตั้งค่า Tween")]
    [Tooltip("ความเร็วในการเคลื่อนไหว (วินาที)")]
    public float duration = 1.0f;
    [Tooltip("หน่วงเวลาก่อนเริ่มทำงาน (วินาที)")]
    public float delay = 0f;
    [Tooltip("รูปแบบการเคลื่อนไหว (เช่น Linear, InOutQuad)")]
    public Ease easeType = Ease.InOutQuad;

    // --- ตัวแปรภายในสำหรับ 'Reverse' ---
    private Vector3 originalPosition;
    private Quaternion originalRotation; // ใช้ Quaternion เพื่อความแม่นยำ
    private Vector3 originalScale;

    private void Awake()
    {
        // 1. เก็บค่าเริ่มต้นไว้สำหรับ 'Reverse'
        if (useLocalMove)
            originalPosition = transform.localPosition;
        else
            originalPosition = transform.position;

        if (useLocalRotate)
            originalRotation = transform.localRotation;
        else
            originalRotation = transform.rotation;
            
        originalScale = transform.localScale;
    }

    /// <summary>
    /// (PUBLIC) Function สำหรับสั่งให้ "เคลื่อนไหวไปข้างหน้า"
    /// (นี่คือ Function ที่เราจะเรียกจาก ZoneTrigger)
    /// </summary>
    public void PlayTween()
    {
        // หยุด Tween เก่าที่อาจจะค้างอยู่ (ถ้ามี)
        transform.DOKill(); 

        if (doMove)
        {
            if (useLocalMove)
                transform.DOLocalMove(targetPosition, duration).SetDelay(delay).SetEase(easeType);
            else
                transform.DOMove(targetPosition, duration).SetDelay(delay).SetEase(easeType);
        }

        if (doRotate)
        {
            if (useLocalRotate)
                transform.DOLocalRotate(targetRotation, duration, RotateMode.FastBeyond360).SetDelay(delay).SetEase(easeType);
            else
                transform.DORotate(targetRotation, duration, RotateMode.FastBeyond360).SetDelay(delay).SetEase(easeType);
        }

        if (doScale)
        {
            transform.DOScale(targetScale, duration).SetDelay(delay).SetEase(easeType);
        }
    }

    /// <summary>
    /// (PUBLIC) Function สำหรับสั่งให้ "เคลื่อนไหวกลับที่เดิม"
    /// (นี่คือ Function ที่เราจะเรียกจาก ZoneTrigger ตอน 'OnExit')
    /// </summary>
    public void ReverseTween()
    {
        // หยุด Tween เก่าที่อาจจะค้างอยู่
        transform.DOKill();

        if (doMove)
        {
            if (useLocalMove)
                transform.DOLocalMove(originalPosition, duration).SetDelay(delay).SetEase(easeType);
            else
                transform.DOMove(originalPosition, duration).SetDelay(delay).SetEase(easeType);
        }

        if (doRotate)
        {
            if (useLocalRotate)
                transform.DOLocalRotateQuaternion(originalRotation, duration).SetDelay(delay).SetEase(easeType);
            else
                transform.DORotateQuaternion(originalRotation, duration).SetDelay(delay).SetEase(easeType);
        }
        
        if (doScale)
        {
            transform.DOScale(originalScale, duration).SetDelay(delay).SetEase(easeType);
        }
    }
}