using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AutoScaleTexture : MonoBehaviour
{
    public Vector2 textureScale = new Vector2(1, 1); // tiling size in world units

    void Update()
    {
        Renderer rend = GetComponent<Renderer>();
        Vector3 scale = transform.lossyScale; // world scale of the object

        // Match texture tiling to object size
        rend.material.mainTextureScale = new Vector2(
      scale.x / textureScale.x,
      scale.z / textureScale.y
    );
    }
}