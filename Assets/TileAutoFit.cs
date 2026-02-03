using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TileAutoFit : MonoBehaviour
{
    public float targetSize = 1f; // world units

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        Vector2 spriteSize = sr.sprite.bounds.size;
        float maxSide = Mathf.Max(spriteSize.x, spriteSize.y);

        float scale = targetSize / maxSide;
        transform.localScale = Vector3.one * scale;
    }
}