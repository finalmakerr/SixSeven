using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        if (sr == null)
            sr = FindIconRenderer();
    }

    public void SetSprite(Sprite sprite)
    {
        if (sr == null)
            sr = FindIconRenderer();
        if (sr == null)
            return;

        sr.sprite = sprite;
    }

    private SpriteRenderer FindIconRenderer()
    {
        Transform iconTransform = transform.Find("TileUI/Icon");
        if (iconTransform == null)
            iconTransform = transform.Find("Icon");

        if (iconTransform != null)
            return iconTransform.GetComponent<SpriteRenderer>();

        return GetComponent<SpriteRenderer>();
    }
}
