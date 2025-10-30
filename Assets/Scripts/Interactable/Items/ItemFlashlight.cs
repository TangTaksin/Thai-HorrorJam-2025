using UnityEngine;

public class ItemFlashlight : MonoBehaviour
{
    public void PickUp()
    {
        GameManager.Instance.haveflashlight = true;
        gameObject.SetActive(false);
    }
}
