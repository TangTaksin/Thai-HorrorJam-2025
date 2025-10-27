using NUnit.Framework.Internal;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.StandaloneInputModule;

public class ModelInspector : MonoBehaviour
{
    [SerializeField] GameObject inspectObject;
    [SerializeField] Transform inspectCam;

    ItemObject itemToDisplay;

    MeshFilter obj_meshFilter;
    MeshRenderer obj_meshRenderer;

    Vector2 mouse_delta_v2;
    Vector2 mouse_scroll_v2;
    bool mouse_left_down, mouse_mid_down, mouse_right_down;

    float _pitch, _yaw;

    [Space]
    [SerializeField] float pitchLimitmin = -90;
    [SerializeField] float pitchLimitmax = 90;

    float distance = 4f;
    [Space]
    [SerializeField] float distanceMin= .1f;
    [SerializeField] float distanceMax = 5f;

    [Header("Info Panel")]
    [SerializeField] GameObject infoPanel;
    [SerializeField] TextMeshProUGUI itemNameTxt, itemDescTxt;

    private void OnEnable()
    {
        InspectionManager.InspectorLoaded += SetItem;
    }

    private void OnDisable()
    {
        InspectionManager.InspectorLoaded -= SetItem;
    }

    private void Start()
    {
        Init();
    }

    void Init()
    {
        inspectObject.TryGetComponent<MeshFilter>(out obj_meshFilter);
        inspectObject.TryGetComponent<MeshRenderer>(out obj_meshRenderer);

        if (itemToDisplay)
        ChangeModel(itemToDisplay);

        distance = (inspectCam.position - inspectObject.transform.position).magnitude;
    }

    private void Update()
    {
        Inputs();

        RotateModel();
        ZoomModel();

        CamUpdate();
    }


    public void SetItem(ItemObject display_object)
    {
        itemToDisplay = display_object;
        itemNameTxt.text = display_object.itemDisplayName;
        itemDescTxt.text = display_object.itemDesciption;
    }

    public void ChangeModel(ItemObject display_object)
    {
        var mesh = display_object.GetMesh();

        inspectObject.gameObject.SetActive(false);

        obj_meshFilter.mesh = mesh.Item1.mesh;
        obj_meshRenderer.SetMaterials(mesh.Item2.materials.ToList<Material>());

        inspectObject.gameObject.SetActive(true);
    }


    void Inputs()
    {
        mouse_delta_v2 = Input.mousePositionDelta;
        mouse_scroll_v2 = Input.mouseScrollDelta;
        mouse_left_down = Input.GetMouseButton(0);
        mouse_right_down = Input.GetMouseButton(1);
        mouse_mid_down = Input.GetMouseButton(2);
    }

    void PanModel()
    {

    }

    void RotateModel()
    {
        if (!mouse_left_down)
            return;

        _yaw += mouse_delta_v2.x;
        _pitch -= mouse_delta_v2.y;

        _pitch = Mathf.Clamp(_pitch, pitchLimitmin, pitchLimitmax);
    }

    void ZoomModel()
    {
        distance += mouse_scroll_v2.y;
        distance = Mathf.Clamp(distance, distanceMin, distanceMax);

    }

    void CamUpdate()
    {
        var qua_rot = Quaternion.Euler(_pitch, _yaw, 0);

        var posOffset = qua_rot * new Vector3(0, 0, -distance);
        inspectCam.position = inspectObject.transform.position + posOffset;
        inspectCam.rotation = qua_rot;
    }


    public void ToggleInfo()
    {
        infoPanel.SetActive(!infoPanel.activeSelf);
    }

    public void CloseInspector()
    {
        InspectionManager.instance.CloseInspector();
    }
}
