using UnityEngine;

namespace GameCore
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Piece : MonoBehaviour
    {
        [SerializeField] private float size = 0.9f;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int ColorIndex { get; private set; }
        public SpecialType SpecialType { get; private set; }

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var collider = GetComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
        }

        public void Initialize(int x, int y, int colorIndex, Sprite sprite)
        {
            X = x;
            Y = y;
            SetColor(colorIndex, sprite);
            SetSpecialType(SpecialType.None);
            name = $"Piece_{x}_{y}";
        }

        public void SetColor(int colorIndex, Sprite sprite)
        {
            ColorIndex = colorIndex;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                ApplySpecialVisual();
            }
        }

        // CODEX BOSS PR2
        public void SetSpecialType(SpecialType type)
        {
            SpecialType = type;
            ApplySpecialVisual();
        }

        private void ApplySpecialVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = SpecialType == SpecialType.Bomb
                ? new Color(0.25f, 0.25f, 0.25f)
                : Color.white;
        }

        public void SetPosition(int x, int y, Vector3 worldPosition)
        {
            X = x;
            Y = y;
            transform.position = worldPosition;
            name = $"Piece_{x}_{y}";
        }
    }
}
