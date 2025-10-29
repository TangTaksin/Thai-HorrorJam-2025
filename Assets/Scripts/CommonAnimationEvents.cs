using UnityEngine;

public class CommonAnimationEvents : MonoBehaviour
{
    public void EnableSelf()
    {
        gameObject.SetActive(true);
    }

    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
