using UnityEngine;

public class EndingController : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.ChangeState(GameState.MainMenu);
        // ทันทีที่ฉากนี้เริ่ม
        // บอก GameManager ให้เปลี่ยนเป็น MainMenu
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }
}
