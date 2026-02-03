using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private Image image;
    [SerializeField] private GameObject selection;
    [SerializeField] private Button button;
    [SerializeField] private float dragThreshold = 28f;
    [SerializeField] private Vector2 pressedScale = new Vector2(1.02f, 0.96f);

    private GameManager manager;
    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Vector2 pointerDownPosition;
    private bool dragTriggered;

    public int X { get; private set; }
    public int Y { get; private set; }
    public int Type { get; set; }

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
        if (button == null)
            button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            baseScale = rectTransform.localScale;
    }

    public void Init(GameManager owner, int x, int y)
    {
        manager = owner;
        X = x;
        Y = y;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void SetSelected(bool isSelected)
    {
        if (selection != null)
            selection.SetActive(isSelected);
    }

    public void SetScale(float scale)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseScale = Vector3.one * scale;
            rectTransform.localScale = baseScale;
        }
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
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        rectTransform.localScale = isPressed
            ? new Vector3(baseScale.x * pressedScale.x, baseScale.y * pressedScale.y, baseScale.z)
            : baseScale;
    }
}
