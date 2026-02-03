using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private Image image;
    [SerializeField] private GameObject selection;
    [SerializeField] private Button button;
    [SerializeField] private float dragThreshold = 28f;

    private GameManager manager;
    private RectTransform rectTransform;
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
            rectTransform.localScale = Vector3.one * scale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        dragTriggered = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragTriggered || manager == null)
            return;

        Vector2 delta = eventData.position - pointerDownPosition;
        if (delta.magnitude < dragThreshold)
            return;

        dragTriggered = true;
        Vector2 direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? (delta.x > 0 ? Vector2.right : Vector2.left)
            : (delta.y > 0 ? Vector2.up : Vector2.down);

        manager.OnTileDragged(this, direction);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (dragTriggered || manager == null)
            return;

        manager.OnTileClicked(this);
    }
}
