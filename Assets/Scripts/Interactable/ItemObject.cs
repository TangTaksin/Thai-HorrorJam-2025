using UnityEngine;

public class ItemObject : MonoBehaviour, IInteractable
{
    public void Interact(GameObject interacter)
    {
        print(interacter.name + " attempted to interact with " + gameObject.name);
    }
}
