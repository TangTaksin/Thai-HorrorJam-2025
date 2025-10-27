using UnityEngine;

public class ItemObject : MonoBehaviour, IInteractable
{
    public GameObject objectMesh;
    public string itemDisplayName;
    [Multiline] public string itemDesciption;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    private void Start()
    {
        objectMesh.TryGetComponent<MeshFilter>(out meshFilter);
        objectMesh.TryGetComponent<MeshRenderer>(out meshRenderer);
    }

    public (MeshFilter, MeshRenderer) GetMesh()
    {
        return (meshFilter, meshRenderer);
    }

    public void Interact(GameObject interacter)
    {
        print(interacter.name + " attempted to interact with " + gameObject.name);

        InspectionManager.instance?.CallInspector(this);
        
    }
}
