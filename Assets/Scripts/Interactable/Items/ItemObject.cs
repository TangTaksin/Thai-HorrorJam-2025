using UnityEngine;
using UnityEngine.Events;

public class ItemObject : MonoBehaviour, IInteractable
{
    public GameObject objectMesh;
    public string itemDisplayName;
    [Multiline] public string itemDesciption;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    public UnityEvent OnInspectorOpen;
    public UnityEvent OnInspectorClose;

    private void Start()
    {
        objectMesh.TryGetComponent<MeshFilter>(out meshFilter);
        objectMesh.TryGetComponent<MeshRenderer>(out meshRenderer);
    }

    public (MeshFilter, MeshRenderer) GetMesh()
    {
        return (meshFilter, meshRenderer);
    }

    public virtual void Interact(GameObject interacter)
    {
        print(interacter.name + " attempted to interact with " + gameObject.name);

        InspectionManager.instance?.CallInspector(this);
        InspectionManager.InspectorLoaded += OnInspectorOpened;
        InspectionManager.InspectorClosed += OnInspectorClosed;
    }

    void OnInspectorOpened(ItemObject item)
    {
        OnInspectorOpen?.Invoke();
        InspectionManager.InspectorLoaded -= OnInspectorOpened;
    }

    void OnInspectorClosed()
    {
        OnInspectorClose?.Invoke();
        InspectionManager.InspectorClosed -= OnInspectorClosed;
    }
}
