using UnityEditor;
using UnityEngine;

public class TextureScalerTool : EditorWindow
{
    private Vector2 customScale = Vector2.one;
    private float multiplier = 1f;

    [MenuItem("Tools/Texture Scaler")]
    public static void ShowWindow()
    {
        GetWindow<TextureScalerTool>("Texture Scaler");
    }

    void OnGUI()
    {
        GUILayout.Label("Auto-Scaling Textures", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto Fit to Object Scale"))
        {
            AutoScaleTextures();
        }

        GUILayout.Space(10);
        GUILayout.Label("Custom Controls", EditorStyles.boldLabel);

        customScale = EditorGUILayout.Vector2Field("Custom Scale", customScale);
        multiplier = EditorGUILayout.FloatField("Multiplier", multiplier);

        if (GUILayout.Button("Apply Custom Scale"))
        {
            ApplyCustomScale();
        }

        if (GUILayout.Button("Multiply Current Scale"))
        {
            MultiplyScale();
        }
    }

    private void AutoScaleTextures()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                Vector3 scale = obj.transform.lossyScale;
                rend.sharedMaterial.mainTextureScale = new Vector2(scale.x, scale.z);
            }
        }
    }

    private void ApplyCustomScale()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                rend.sharedMaterial.mainTextureScale = customScale;
            }
        }
    }

    private void MultiplyScale()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                Vector2 currentScale = rend.sharedMaterial.mainTextureScale;
                rend.sharedMaterial.mainTextureScale = currentScale * multiplier;
            }
        }
    }
}
