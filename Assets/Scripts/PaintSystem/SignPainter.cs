using UnityEngine;

/// <summary>
/// Handles painting on a texture for an object.
/// This component must be on an object with a Renderer and a Material that has a main texture.
/// It implements IInteractable so it can be detected by the FPInteract system.
/// </summary>
public class SignPainter : MonoBehaviour, IInteractable
{
    [Header("Brush Settings")]
    public int brushSize = 10;
    public Color paintColor = Color.black;

    private Texture2D drawableTexture;
    private bool textureNeedsUpdate = false;
    private Vector2 lastDrawUV;
    private bool isDrawingLastFrame = false;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("SignPainter: No Renderer found on this object.");
            this.enabled = false;
            return;
        }

        Texture originalTexture = rend.material.mainTexture;
        if (originalTexture == null)
        {
            Debug.LogError("SignPainter: Material does not have a Main Texture (Base Map).");
            this.enabled = false;
            return;
        }

        // Create a readable copy of the original texture
        drawableTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
        
        RenderTexture rt = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
        Graphics.Blit(originalTexture, rt);
        
        RenderTexture.active = rt;
        drawableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        drawableTexture.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // Assign the new drawable texture to the material
        rend.material.mainTexture = drawableTexture;
    }

    // Apply texture changes at the end of the frame
    void LateUpdate()
    {
        if (textureNeedsUpdate)
        {
            drawableTexture.Apply();
            textureNeedsUpdate = false;
        }
    }

    #region Public Painting Methods

    /// <summary>
    /// Called by an external script (like FPInteract) to paint at a specific UV coordinate.
    /// This is triggered when the interaction key (e.g., Left Mouse) is held down.
    /// </summary>
    public void ExternalPaint(Vector2 uvCoordinate)
    {
        PaintStroke(uvCoordinate);
    }

    /// <summary>
    /// Called by an external script (like FPInteract) when painting stops.
    /// </summary>
    public void StopPainting()
    {
        isDrawingLastFrame = false;
    }

    /// <summary>
    /// Required by the IInteractable interface.
    /// Allows the crosshair to change state when looking at this object.
    /// Actual painting logic is handled by FPInteract calling ExternalPaint().
    /// </summary>
    public void Interact(GameObject interactor)
    {
        // No action needed here.
        // Painting is managed by FPInteract's continuous interaction.
    }

    /// <summary>
    /// Returns the active drawable texture.
    /// </summary>
    public Texture2D GetDrawableTexture()
    {
        return drawableTexture;
    }

    #endregion

    #region Internal Painting Logic

    /// <summary>
    /// Manages the painting stroke, interpolating if the mouse moved.
    /// </summary>
    private void PaintStroke(Vector2 currentUV)
    {
        if (!isDrawingLastFrame)
        {
            // First frame of drawing, just draw a single point
            DrawOnTexture(currentUV);
        }
        else
        {
            // Continue drawing, interpolate from the last point
            InterpolateDraw(lastDrawUV, currentUV);
        }

        lastDrawUV = currentUV;
        isDrawingLastFrame = true;
    }

    /// <summary>
    /// Draws a line between two UV points by drawing multiple circles.
    /// This prevents gaps when moving the mouse quickly.
    /// </summary>
    private void InterpolateDraw(Vector2 startUV, Vector2 endUV)
    {
        Vector2 startPixel = new Vector2(startUV.x * drawableTexture.width, startUV.y * drawableTexture.height);
        Vector2 endPixel = new Vector2(endUV.x * drawableTexture.width, endUV.y * drawableTexture.height);
        
        float distance = Vector2.Distance(startPixel, endPixel);
        float brushRadius = brushSize / 2.0f;
        
        // Determine step size based on brush size to avoid gaps
        float stepSize = Mathf.Max(1.0f, brushRadius * 0.25f);
        int steps = (int)Mathf.Ceil(distance / stepSize);
        steps = Mathf.Max(1, steps); // Ensure at least one step

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 interpolatedUV = Vector2.Lerp(startUV, endUV, t);
            DrawOnTexture(interpolatedUV);
        }
    }

    /// <summary>
    /// Draws a circular brush stroke at the given UV coordinate.
    /// </summary>
    public void DrawOnTexture(Vector2 uvCoordinate)
    {
        if (drawableTexture == null) return;

        int pixelX = (int)(uvCoordinate.x * drawableTexture.width);
        int pixelY = (int)(uvCoordinate.y * drawableTexture.height);
        
        float brushRadius = brushSize / 2.0f;
        int intRadius = (int)Mathf.Ceil(brushRadius);

        // Loop through a square bounding box around the brush center
        for (int y = -intRadius; y <= intRadius; y++)
        {
            for (int x = -intRadius; x <= intRadius; x++)
            {
                float distance = new Vector2(x, y).magnitude;
                
                // Only draw pixels within the circular radius
                if (distance > brushRadius)
                {
                    continue;
                }

                int targetX = pixelX + x;
                int targetY = pixelY + y;

                // Check if the pixel is within the texture bounds
                if (targetX >= 0 && targetX < drawableTexture.width &&
                    targetY >= 0 && targetY < drawableTexture.height)
                {
                    // Calculate falloff for a soft brush edge
                    float falloff = distance / brushRadius;
                    float strength = Mathf.SmoothStep(1.0f, 0.0f, falloff);

                    Color originalColor = drawableTexture.GetPixel(targetX, targetY);
                    
                    // Blend the new color with the original color
                    float blendAlpha = strength * paintColor.a;
                    Color blendedColor = Color.Lerp(originalColor, paintColor, blendAlpha);

                    drawableTexture.SetPixel(targetX, targetY, blendedColor);
                }
            }
        }

        textureNeedsUpdate = true;
    }

    #endregion
}