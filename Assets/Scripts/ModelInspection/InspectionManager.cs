using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager instance;

    ItemObject stachedItem;

    public static Action<ItemObject> InspectorLoaded;
    public static Action InspectorClosed;

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
        GameManager.Instance.ChangeState(GameState.Inspect);
        SceneManager.sceneLoaded -= OnInspectorLoaded;
        InspectorLoaded?.Invoke(stachedItem);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void CloseInspector()
    {

        SceneManager.UnloadSceneAsync("InspectionScene");
        GameManager.Instance.ChangeState(GameState.Playing);
        InspectorClosed?.Invoke();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    } 
}
