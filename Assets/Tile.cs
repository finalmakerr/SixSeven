using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color clickFlashColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private float clickAnimDuration = 0.14f;

    [Header("Player Aura")]
    [SerializeField] private Color auraColor = new Color(0.45f, 0.95f, 1f, 0.5f);
    [SerializeField] private float auraPulseSpeed = 2.1f;
    [SerializeField] private float auraMinAlpha = 0.22f;
    [SerializeField] private float auraMaxAlpha = 0.56f;
    [SerializeField] private float auraScaleMultiplier = 1.65f;

    private SpriteRenderer auraRenderer;
    private BoxCollider2D clickCollider;
    private Vector3 baseScale;
    private Color baseTileColor;
    private Coroutine clickRoutine;

    public Vector2Int GridPosition { get; private set; }

    private void Awake()
    {
        if (sr == null)
            sr = FindIconRenderer();

        if (sr != null)
            baseTileColor = sr.color;

        baseScale = transform.localScale;

        clickCollider = GetComponent<BoxCollider2D>();
        if (clickCollider == null)
            clickCollider = gameObject.AddComponent<BoxCollider2D>();

        EnsureAuraRenderer();
        SetPlayerHighlight(false);
    }

    private void Update()
    {
        if (auraRenderer == null || !auraRenderer.enabled)
            return;

        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * auraPulseSpeed * Mathf.PI * 2f);
        Color currentAura = auraColor;
        currentAura.a = Mathf.Lerp(auraMinAlpha, auraMaxAlpha, t);
        auraRenderer.color = currentAura;

        float scale = Mathf.Lerp(0.94f, 1.05f, t) * auraScaleMultiplier;
        auraRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void OnMouseUpAsButton()
    {
        if (!isActiveAndEnabled)
            return;

        if (clickRoutine != null)
            StopCoroutine(clickRoutine);

        clickRoutine = StartCoroutine(PlayClickAnimation());
    }

    public void SetGridPosition(int x, int y)
    {
        GridPosition = new Vector2Int(x, y);
    }

    public void SetSprite(Sprite sprite)
    {
        if (sr == null)
            sr = FindIconRenderer();
        if (sr == null)
            return;

        sr.sprite = sprite;
        FitColliderToSprite();
    }

    public void SetPlayerHighlight(bool isHighlighted)
    {
        if (auraRenderer == null)
            EnsureAuraRenderer();

        if (auraRenderer != null)
            auraRenderer.enabled = isHighlighted;
    }

    private IEnumerator PlayClickAnimation()
    {
        if (sr == null)
            yield break;

        float halfDuration = clickAnimDuration * 0.5f;
        Vector3 pressedScale = baseScale * 0.93f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(baseScale, pressedScale, t);
            sr.color = Color.Lerp(baseTileColor, clickFlashColor, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(pressedScale, baseScale, t);
            sr.color = Color.Lerp(clickFlashColor, baseTileColor, t);
            yield return null;
        }

        transform.localScale = baseScale;
        sr.color = baseTileColor;
        clickRoutine = null;
    }

    private void EnsureAuraRenderer()
    {
        if (auraRenderer != null)
            return;

        GameObject aura = new GameObject("Aura");
        aura.transform.SetParent(transform, false);
        aura.transform.localPosition = Vector3.zero;

        auraRenderer = aura.AddComponent<SpriteRenderer>();
        auraRenderer.sprite = SoftCircleSpriteCache.Get();
        auraRenderer.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
        auraRenderer.sortingOrder = sr != null ? sr.sortingOrder - 1 : -1;
        auraRenderer.color = auraColor;
        auraRenderer.enabled = false;
    }

    private void FitColliderToSprite()
    {
        if (clickCollider == null || sr == null || sr.sprite == null)
            return;

        clickCollider.size = sr.sprite.bounds.size;
        clickCollider.offset = Vector2.zero;
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

    private static class SoftCircleSpriteCache
    {
        private static Sprite sprite;

        public static Sprite Get()
        {
            if (sprite != null)
                return sprite;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            float softEdge = size * 0.26f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(radius - softEdge, radius, distance));
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return sprite;
        }
    }
}
