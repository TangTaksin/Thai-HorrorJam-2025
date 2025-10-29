using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI; // Required for UI elements like Image
using NaughtyAttributes; 

/// <summary>
/// A persistent Singleton for managing scene loading, reloading, and quitting.
/// Now includes a fade-to-black transition for scene changes.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    // The static instance of the Singleton
    public static GameSceneManager Instance { get; private set; }

    
    // --- Inspector Test Actions ---
    
    [BoxGroup("Inspector Scene Loader")]
    [Tooltip("Select a scene from the dropdown to test-load it.")]
    [SerializeField, Scene] 
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

    // --- Additive / Unload Testing (These do not use the fade) ---

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

    // --- Fade Transition Settings ---

    [Header("Fade Transition")]
    [Tooltip("The black UI Image used for fading.")]
    [SerializeField] private Image _fadeImage;

    [Tooltip("How long the fade in/out transition takes.")]
    [SerializeField] private float _fadeDuration = 0.5f;

    // --- Private State ---
    private bool _isFading = false;


    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure the fade image is set up correctly on start
            if (_fadeImage != null)
            {
                // Set color to black and alpha to 0 (fully transparent)
                _fadeImage.color = new Color(0f, 0f, 0f, 0f);
                _fadeImage.raycastTarget = false; // Don't block clicks
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Public API ---

    /// <summary>
    /// Loads a scene by its string name (Synchronous / Blocking).
    /// This REPLACES the current scene and uses a fade transition.
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(LoadSceneByNameWithFade(sceneName));
    }

    /// <summary>
    /// Loads a scene asynchronously in the background.
    /// This REPLACES the current scene and uses a fade transition.
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncWithFade(sceneName, LoadSceneMode.Single));
    }

    /// <summary>
    /// [NEW] Loads a scene additively (on top of the current scene).
    /// This does NOT use a fade transition.
    /// </summary>
    public void LoadSceneAdditiveAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, LoadSceneMode.Additive));
    }

    /// <summary>
    /// [NEW] Unloads a scene that was loaded additively.
    /// This does NOT use a fade transition.
    /// </summary>
    public void UnloadSceneAsync(string sceneName)
    {
        StartCoroutine(UnloadSceneCoroutine(sceneName));
    }



    /// <summary>
    /// Reloads the currently active scene.
    /// Uses a fade transition.
    /// </summary>
    [Button("Reload Current Scene", EButtonEnableMode.Playmode)]
    public void ReloadCurrentScene()
    {
        StartCoroutine(ReloadCurrentSceneWithFade());
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

    // --- FADE COROUTINE ---
    
    /// <summary>
    /// Coroutine to fade the screen to a target alpha.
    /// </summary>
    /// <param name="targetAlpha">1 = fully black, 0 = fully transparent</param>
    private IEnumerator Fade(float targetAlpha)
    {
        if (_fadeImage == null)
        {
            Debug.LogWarning("FadeImage is not assigned. Skipping fade.");
            yield break;
        }

        _isFading = true;
        _fadeImage.raycastTarget = true; // Block clicks during fade

        Color currentColor = _fadeImage.color;
        float startAlpha = currentColor.a;
        float timer = 0f;

        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled time for fades
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / _fadeDuration);
            _fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
            yield return null;
        }

        // Ensure target alpha is set
        _fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);

        if (targetAlpha == 0f)
        {
            _fadeImage.raycastTarget = false; // Unblock clicks when transparent
        }
        
        _isFading = false;
    }

    // --- SCENE LOAD WRAPPERS (WITH FADE) ---

    /// <summary>
    /// Wrapper for Sync load with fade.
    /// </summary>
    private IEnumerator LoadSceneByNameWithFade(string sceneName)
    {
        if (_isFading) yield break;

        yield return StartCoroutine(Fade(1f)); // Fade Out

        SceneManager.LoadScene(sceneName);
        
        // This will run after the new scene is loaded
        yield return StartCoroutine(Fade(0f)); // Fade In
    }

    /// <summary>
    /// Wrapper for Async load with fade.
    /// </summary>
    private IEnumerator LoadSceneAsyncWithFade(string sceneName, LoadSceneMode mode)
    {
        if (_isFading) yield break;

        yield return StartCoroutine(Fade(1f)); // Fade Out

        // Now, do the async load using the original coroutine
        yield return StartCoroutine(LoadSceneAsyncCoroutine(sceneName, mode));
        
        // This will run after the async load is complete
        yield return StartCoroutine(Fade(0f)); // Fade In
    }

    /// <summary>
    /// Wrapper for Reload with fade.
    /// </summary>
    private IEnumerator ReloadCurrentSceneWithFade()
    {
        if (_isFading) yield break;
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        yield return StartCoroutine(Fade(1f)); // Fade Out

        SceneManager.LoadScene(currentSceneName);
        
        // This will run after the new scene is loaded
        yield return StartCoroutine(Fade(0f)); // Fade In
    }

    
    // --- ORIGINAL LOADING COROUTINES (Now used as utility) ---

    /// <summary>
    /// Coroutine for loading scenes (both Single and Additive).
    /// This is the actual loading part, now called by the fade wrappers.
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName, LoadSceneMode mode)
    {
        // Start the asynchronous operation
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

        while (!operation.isDone)
        {
            // You could update a loading bar here using operation.progress
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
