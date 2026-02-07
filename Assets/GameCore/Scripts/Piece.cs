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
        public int ItemTurnsRemaining { get; private set; } // CODEX STAGE 7B: track item lifetime.
        public bool IsPlayer => isPlayerPiece;

        private SpriteRenderer spriteRenderer;
        private Sprite baseSprite;
        private Sprite bombSprite;
        private bool isPlayerPiece;
        private bool hasInitializedSpecialType;
        // CODEX CHEST PR1
        private SpriteRenderer treasureOverlayRenderer;
        private TextMesh treasureDebugText;
        // CODEX STAGE 7B
        private TextMesh itemTurnsText;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var collider = GetComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
        }

        public void Initialize(int x, int y, int colorIndex, Sprite sprite)
        {
            if (isPlayerPiece)
            {
                return;
            }

            X = x;
            Y = y;

            isPlayerPiece = false;
            SetColor(colorIndex, sprite);
            if (!hasInitializedSpecialType)
            {
                SetSpecialType(SpecialType.None);
            }
            name = $"Piece_{x}_{y}";
        }

        public void InitializeAsPlayer(int x, int y)
        {
            X = x;
            Y = y;

            isPlayerPiece = true;
            SpecialType = SpecialType.Player;
            ColorIndex = -1;
            BombTier = 0;
            ItemTurnsRemaining = 0;
            baseSprite = null;
            bombSprite = null;
            hasInitializedSpecialType = true;
            ClearTreasureChestVisual();
            ClearItemVisual();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
            }
            ApplySpecialVisual();
            name = $"Piece_{x}_{y}";
        }

        public void SetColor(int colorIndex, Sprite sprite)
        {
            if (isPlayerPiece)
            {
                return;
            }

            ColorIndex = colorIndex;
            baseSprite = sprite;
            ApplySpecialVisual();
        }

        // CODEX BOSS PR2
        public void SetSpecialType(SpecialType type)
        {
            if (isPlayerPiece)
            {
                return;
            }

            if (type == SpecialType.None && hasInitializedSpecialType)
            {
                return;
            }

            SpecialType = type;
            hasInitializedSpecialType = true;
            if (type != SpecialType.Bomb)
            {
                BombTier = 0;
                bombSprite = null;
            }
            if (type != SpecialType.TreasureChest)
            {
                ClearTreasureChestVisual();
            }
            if (type != SpecialType.Item && type != SpecialType.Bugada)
            {
                ItemTurnsRemaining = 0;
                ClearItemVisual();
            }
            ApplySpecialVisual();
        }

        // CODEX BOMB TIERS: assign bomb tier + sprite when creating a bomb special.
        public void SetBombTier(int tier, Sprite sprite)
        {
            if (isPlayerPiece)
            {
                return;
            }

            SpecialType = SpecialType.Bomb;
            hasInitializedSpecialType = true;
            BombTier = tier;
            bombSprite = sprite;
            ItemTurnsRemaining = 0;
            ClearItemVisual();
            ApplySpecialVisual();
        }

        // CODEX STAGE 7B: configure a board-spawned item with remaining turns.
        public void ConfigureAsItem(int remainingTurns)
        {
            if (isPlayerPiece)
            {
                return;
            }

            SpecialType = SpecialType.Item;
            hasInitializedSpecialType = true;
            BombTier = 0;
            bombSprite = null;
            ClearTreasureChestVisual();
            ItemTurnsRemaining = Mathf.Max(0, remainingTurns);
            UpdateItemTurnsVisual();
            ApplySpecialVisual();
        }

        // CODEX STAGE 7D: configure a Bugada special item.
        public void ConfigureAsBugada()
        {
            if (isPlayerPiece)
            {
                return;
            }

            SpecialType = SpecialType.Bugada;
            hasInitializedSpecialType = true;
            BombTier = 0;
            bombSprite = null;
            ClearTreasureChestVisual();
            ItemTurnsRemaining = 0;
            UpdateBugadaVisual();
            ApplySpecialVisual();
        }

        // CODEX STAGE 7B: update item turns remaining and indicator.
        public void UpdateItemTurns(int remainingTurns)
        {
            if (isPlayerPiece || SpecialType != SpecialType.Item)
            {
                return;
            }

            ItemTurnsRemaining = Mathf.Max(0, remainingTurns);
            UpdateItemTurnsVisual();
        }

        // CODEX STAGE 7B: fade item visuals on expiry.
        public void SetItemFade(float alpha)
        {
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }

            if (itemTurnsText != null)
            {
                var color = itemTurnsText.color;
                color.a = alpha;
                itemTurnsText.color = color;
            }
        }

        private void ApplySpecialVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (isPlayerPiece)
            {
                spriteRenderer.sprite = null;
                spriteRenderer.color = Color.white;
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

        // CODEX CHEST PR1
        public void SetTreasureChestVisual(Sprite overlaySprite, bool debugMarker)
        {
            if (isPlayerPiece)
            {
                return;
            }

            if (overlaySprite != null)
            {
                var overlay = EnsureTreasureOverlayRenderer();
                overlay.sprite = overlaySprite;
                overlay.enabled = true;
                if (treasureDebugText != null)
                {
                    treasureDebugText.gameObject.SetActive(false);
                }
                return;
            }

            if (debugMarker)
            {
                var text = EnsureTreasureDebugText();
                text.text = "CHEST";
                text.gameObject.SetActive(true);
                if (treasureOverlayRenderer != null)
                {
                    treasureOverlayRenderer.enabled = false;
                }
                return;
            }

            ClearTreasureChestVisual();
        }

        // CODEX CHEST PR1
        private void ClearTreasureChestVisual()
        {
            if (treasureOverlayRenderer != null)
            {
                treasureOverlayRenderer.enabled = false;
            }

            if (treasureDebugText != null)
            {
                treasureDebugText.gameObject.SetActive(false);
            }
        }

        // CODEX STAGE 7B
        private void UpdateItemTurnsVisual()
        {
            if (SpecialType != SpecialType.Item)
            {
                ClearItemVisual();
                return;
            }

            var text = EnsureItemTurnsText();
            text.text = ItemTurnsRemaining.ToString();
            text.color = Color.white;
            text.gameObject.SetActive(true);
        }

        // CODEX STAGE 7B
        private void ClearItemVisual()
        {
            if (itemTurnsText != null)
            {
                itemTurnsText.gameObject.SetActive(false);
            }
        }

        private void UpdateBugadaVisual()
        {
            if (SpecialType != SpecialType.Bugada)
            {
                return;
            }

            var text = EnsureItemTurnsText();
            text.text = "BUG";
            text.color = new Color(1f, 0.85f, 0.2f);
            text.gameObject.SetActive(true);
        }

        // CODEX STAGE 7B
        private TextMesh EnsureItemTurnsText()
        {
            if (itemTurnsText != null)
            {
                return itemTurnsText;
            }

            var textObject = new GameObject("ItemTurnsText");
            textObject.transform.SetParent(transform, false);
            itemTurnsText = textObject.AddComponent<TextMesh>();
            itemTurnsText.anchor = TextAnchor.MiddleCenter;
            itemTurnsText.alignment = TextAlignment.Center;
            itemTurnsText.characterSize = 0.18f;
            itemTurnsText.fontSize = 60;
            itemTurnsText.color = Color.white;
            return itemTurnsText;
        }

        // CODEX CHEST PR1
        private SpriteRenderer EnsureTreasureOverlayRenderer()
        {
            if (treasureOverlayRenderer != null)
            {
                return treasureOverlayRenderer;
            }

            var overlayObject = new GameObject("TreasureOverlay");
            overlayObject.transform.SetParent(transform, false);
            treasureOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            treasureOverlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            treasureOverlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            return treasureOverlayRenderer;
        }

        // CODEX CHEST PR1
        private TextMesh EnsureTreasureDebugText()
        {
            if (treasureDebugText != null)
            {
                return treasureDebugText;
            }

            var textObject = new GameObject("TreasureDebugText");
            textObject.transform.SetParent(transform, false);
            treasureDebugText = textObject.AddComponent<TextMesh>();
            treasureDebugText.anchor = TextAnchor.MiddleCenter;
            treasureDebugText.alignment = TextAlignment.Center;
            treasureDebugText.characterSize = 0.15f;
            treasureDebugText.fontSize = 50;
            treasureDebugText.color = Color.yellow;
            return treasureDebugText;
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
