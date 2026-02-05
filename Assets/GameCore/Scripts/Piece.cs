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
        public int BombTier { get; private set; } // CODEX BOMB TIERS: store tier for bomb specials.

        private SpriteRenderer spriteRenderer;
        private Sprite baseSprite;
        private Sprite bombSprite;

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
            baseSprite = sprite;
            ApplySpecialVisual();
        }

        // CODEX BOSS PR2
        public void SetSpecialType(SpecialType type)
        {
            SpecialType = type;
            if (type != SpecialType.Bomb)
            {
                BombTier = 0;
                bombSprite = null;
            }
            ApplySpecialVisual();
        }

        // CODEX BOMB TIERS: assign bomb tier + sprite when creating a bomb special.
        public void SetBombTier(int tier, Sprite sprite)
        {
            SpecialType = SpecialType.Bomb;
            BombTier = tier;
            bombSprite = sprite;
            ApplySpecialVisual();
        }

        private void ApplySpecialVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (SpecialType == SpecialType.Bomb && bombSprite != null)
            {
                spriteRenderer.sprite = bombSprite;
            }
            else if (baseSprite != null)
            {
                spriteRenderer.sprite = baseSprite;
            }

            spriteRenderer.color = Color.white;
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
