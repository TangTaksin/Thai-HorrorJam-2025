using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 
using NaughtyAttributes;           

/// <summary>
/// A persistent Singleton for managing scene loading, reloading, and quitting.
/// Now includes Additive loading and Unloading.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    // The static instance of the Singleton
    public static GameSceneManager Instance { get; private set; }

    
    // --- Inspector Test Actions ---
    
    [BoxGroup("Inspector Scene Loader")]
    [Tooltip("Select a scene from the dropdown to test-load it.")]
    [SerializeField, Scene] // [Scene] attribute shows a dropdown of scenes in Build Settings
    private string _sceneToLoadForTesting;

    // --- Standard Loading ---

    [Button("Test Load (Async - Single)", EButtonEnableMode.Editor)]
    private void TestLoadSceneAsync()
    {
        if (string.IsNullOrEmpty(_sceneToLoadForTesting)) return;
        LoadSceneAsync(_sceneToLoadForTesting);
    }
    
    
    [Button("Test Load (Sync - Single)", EButtonEnableMode.Editor)]
    private void TestLoadSceneSync()
    {
        if (string.IsNullOrEmpty(_sceneToLoadForTesting)) return;
        LoadSceneByName(_sceneToLoadForTesting);
    }

    // --- Additive / Unload Testing ---

    [Button("Test Load (Additive)", EButtonEnableMode.Editor)]
    private void TestLoadSceneAdditive()
    {
        if (string.IsNullOrEmpty(_sceneToLoadForTesting)) return;
        // Call the new Additive method
        LoadSceneAdditiveAsync(_sceneToLoadForTesting);
    }
    
 
    [Button("Test Unload Scene", EButtonEnableMode.Editor)]
    private void TestUnloadScene()
    {
        if (string.IsNullOrEmpty(_sceneToLoadForTesting)) return;
        // Call the new Unload method
        UnloadSceneAsync(_sceneToLoadForTesting);
    }


    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Public API ---

    /// <summary>
    /// Loads a scene by its string name. (Synchronous / Blocking)
    /// This REPLACES the current scene.
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Loads a scene asynchronously in the background (without a loading screen).
    /// This REPLACES the current scene.
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, LoadSceneMode.Single));
    }

    /// <summary>
    /// [NEW] Loads a scene additively (on top of the current scene).
    /// </summary>
    public void LoadSceneAdditiveAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, LoadSceneMode.Additive));
    }

    /// <summary>
    /// [NEW] Unloads a scene that was loaded additively.
    /// </summary>
    public void UnloadSceneAsync(string sceneName)
    {
        StartCoroutine(UnloadSceneCoroutine(sceneName));
    }



    /// <summary>
    /// Reloads the currently active scene.
    /// </summary>
    [Button("Reload Current Scene", EButtonEnableMode.Playmode)]
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    [Button("Quit Game", EButtonEnableMode.Playmode)]
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }


    // --- Coroutines ---

    /// <summary>
    /// Coroutine for loading scenes (both Single and Additive).
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName, LoadSceneMode mode)
    {
        // Start the asynchronous operation
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

        while (!operation.isDone)
        {
            yield return null;
        }

        Debug.Log($"Scene loaded ({mode}): {sceneName}");
    }

    /// <summary>
    /// [NEW] Coroutine to unload a scene.
    /// </summary>
    private IEnumerator UnloadSceneCoroutine(string sceneName)
    {
        Debug.Log($"Attempting to unload scene: {sceneName}");

        // Start the unload operation
        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

        // operation will be null if the scene isn't loaded
        if (operation == null)
        {
            Debug.LogWarning($"Scene '{sceneName}' could not be unloaded (maybe it was never loaded additively?)");
            yield break;
        }
        
        // Wait until the operation is finished
        while (!operation.isDone)
        {
            yield return null;
        }

        Debug.Log($"Successfully unloaded scene: {sceneName}");
    }
}