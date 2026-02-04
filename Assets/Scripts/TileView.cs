using UnityEngine;
using UnityEngine.EventSystems;
public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Color selectedTint = new Color(1f, 0.92f, 0.2f, 0.35f);
    [SerializeField] private float dragThreshold = 28f;
    [SerializeField] private Vector2 pressedScale = new Vector2(1.02f, 0.96f);

    private GameManager manager;
    private Transform cachedTransform;
    private Vector3 baseScale;
    private Color baseBackgroundColor;
    private Vector2 pointerDownPosition;
    private bool dragTriggered;

    public int X { get; private set; }
    public int Y { get; private set; }
    public int Type { get; set; }

    private void Awake()
    {
        if (iconRenderer == null)
            iconRenderer = FindRenderer("Icon");
        if (backgroundRenderer == null)
            backgroundRenderer = FindRenderer("Background");
        if (backgroundRenderer != null)
            baseBackgroundColor = backgroundRenderer.color;

        cachedTransform = transform;
        baseScale = cachedTransform.localScale;
    }

    public void Init(GameManager owner, int x, int y)
    {
        manager = owner;
        X = x;
        Y = y;

    }

    public void SetSprite(Sprite sprite)
    {
        if (iconRenderer == null)
            return;

        iconRenderer.sprite = sprite;
        iconRenderer.enabled = sprite != null;
    }

    public void SetSelected(bool isSelected)
    {
        if (backgroundRenderer == null)
            return;

        backgroundRenderer.color = isSelected ? selectedTint : baseBackgroundColor;
    }

    public void SetScale(float scale)
    {
        baseScale = Vector3.one * scale;
        cachedTransform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        dragTriggered = false;
        ApplyPressScale(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragTriggered || manager == null)
            return;

        Vector2 delta = eventData.position - pointerDownPosition;
        if (delta.magnitude < dragThreshold)
            return;

        dragTriggered = true;
        ApplyPressScale(false);
        Vector2 direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? (delta.x > 0 ? Vector2.right : Vector2.left)
            : (delta.y > 0 ? Vector2.up : Vector2.down);

        manager.OnTileDragged(this, direction);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ApplyPressScale(false);
        if (dragTriggered || manager == null)
            return;

        manager.OnTileClicked(this);
    }

    private void ApplyPressScale(bool isPressed)
    {
        cachedTransform.localScale = isPressed
            ? new Vector3(baseScale.x * pressedScale.x, baseScale.y * pressedScale.y, baseScale.z)
            : baseScale;
    }

    private SpriteRenderer FindRenderer(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return null;

        return child.GetComponent<SpriteRenderer>();
    }
}
