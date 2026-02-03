using UnityEngine;
using UnityEngine.UI;

public class TileView : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private GameObject selection;
    [SerializeField] private Button button;

    private GameManager manager;

    public int X { get; private set; }
    public int Y { get; private set; }
    public int Type { get; set; }

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Init(GameManager owner, int x, int y)
    {
        manager = owner;
        X = x;
        Y = y;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => manager.OnTileClicked(this));
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
}
