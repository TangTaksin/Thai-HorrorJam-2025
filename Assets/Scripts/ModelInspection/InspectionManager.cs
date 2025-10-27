using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager instance;

    ItemObject stachedItem;

    public static Action<ItemObject> InspectorLoaded;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void CallInspector(ItemObject obj)
    {   
        SceneManager.sceneLoaded += OnInspectorLoaded;
        SceneManager.LoadScene("InspectionScene", LoadSceneMode.Additive);

        stachedItem = obj;
    }

    void OnInspectorLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        SceneManager.sceneLoaded -= OnInspectorLoaded;
        InspectorLoaded?.Invoke(stachedItem);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void CloseInspector()
    {
        SceneManager.UnloadSceneAsync("InspectionScene");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    } 
}
