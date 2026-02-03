using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    public void SetSprite(Sprite sprite)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }
}
