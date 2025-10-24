using System.Collections.Generic;
using UnityEngine;

public class FPSCounters : MonoBehaviour
{
    // Singleton instance
    public static FPSCounters Instance { get; private set; }

    [Header("Display Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private TextAnchor alignment = TextAnchor.UpperLeft;
    [SerializeField, Range(10, 50)] private int fontSize = 24;
    [SerializeField] private Vector2 padding = new Vector2(10, 10);
    [SerializeField] private Color goodFPSColor = Color.green;
    [SerializeField] private Color warningFPSColor = Color.yellow;
    [SerializeField] private Color badFPSColor = Color.red;

    [Header("Calculation Settings")]
    [SerializeField, Range(0.1f, 1f)] private float smoothingFactor = 0.1f;
    [SerializeField, Range(10, 60)] private int averageFrameCount = 30;

    private readonly List<float> deltaTimes = new List<float>();
    private float smoothedDeltaTime = 0f;
    private GUIStyle style;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keep FPS counter across scenes

        style = new GUIStyle
        {
            alignment = alignment,
            normal = { textColor = goodFPSColor }
        };
    }

    private void Update()
    {
        if (!showFPS) return;

        smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * smoothingFactor;

        deltaTimes.Add(smoothedDeltaTime);
        if (deltaTimes.Count > averageFrameCount)
            deltaTimes.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (!showFPS || deltaTimes.Count == 0) return;

        style.fontSize = fontSize;

        float avgDelta = GetAverageDeltaTime();
        float fps = 1f / avgDelta;
        float ms = avgDelta * 1000f;
        float minFps = 1f / Mathf.Max(deltaTimes.ToArray());
        float maxFps = 1f / Mathf.Min(deltaTimes.ToArray());
        float memoryMB = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);

        style.normal.textColor = GetColorForFPS(fps);

        Rect rect = GetLabelRect(Screen.width, Screen.height);
        string text = $"FPS: {fps:0.} (Avg: {1f / avgDelta:0.})\n" +
                      $"Min FPS: {minFps:0.} | Max FPS: {maxFps:0.}\n" +
                      $"Frame Time: {ms:0.0} ms\n" +
                      $"Memory: {memoryMB:0.0} MB";
        GUI.Label(rect, text, style);
    }

    private float GetAverageDeltaTime()
    {
        float sum = 0f;
        foreach (float dt in deltaTimes) sum += dt;
        return sum / deltaTimes.Count;
    }

    private Color GetColorForFPS(float fps) => fps switch
    {
        > 60f => goodFPSColor,
        > 30f => warningFPSColor,
        _     => badFPSColor
    };

    private Rect GetLabelRect(int screenWidth, int screenHeight)
    {
        float height = fontSize * 4f; // Increase for multiple lines
        float width = 250f;
        return alignment switch
        {
            TextAnchor.UpperLeft  => new Rect(padding.x, padding.y, width, height),
            TextAnchor.UpperRight => new Rect(screenWidth - padding.x - width, padding.y, width, height),
            TextAnchor.LowerLeft  => new Rect(padding.x, screenHeight - padding.y - height, width, height),
            TextAnchor.LowerRight => new Rect(screenWidth - padding.x - width, screenHeight - padding.y - height, width, height),
            _                     => new Rect(padding.x, padding.y, width, height),
        };
    }

    // Public method to toggle FPS display
    public void ToggleFPSDisplay(bool show) => showFPS = show;
}
