using TMPro;
using UnityEngine;

public class PointerInfoDisplay : MonoBehaviour
{

    TextMeshProUGUI pointerTxt;

    private void Start()
    {
        pointerTxt = GetComponent<TextMeshProUGUI>();
        pointerTxt.text = string.Empty;
    }

    private void OnEnable()
    {
        FPInteract.OnDetectedInteractableChanged += UpdatePointerText;
    }

    private void OnDisable()
    {
        FPInteract.OnDetectedInteractableChanged -= UpdatePointerText;
    }

    void UpdatePointerText(IInteractable interactable)
    {
        var itemObj = interactable as ItemObject;
        var isntNull = itemObj != null;

        if (isntNull)
            // เพิ่ม "[ F ] " เข้าไปข้างหน้าชื่อไอเทม
            pointerTxt.text = $"[ F ] {itemObj.itemDisplayName}"; // <--- แก้บรรทัดนี้
        else
            pointerTxt.text = string.Empty;
    }
}
