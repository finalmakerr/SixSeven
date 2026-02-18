using System.Collections;
using UnityEngine;

namespace GameCore
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Piece : MonoBehaviour
    {
        [SerializeField] private float size = 0.9f;
        [SerializeField] private GameObject tileDebuffOverlay;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int ColorIndex { get; private set; }
        public SpecialType SpecialType { get; private set; }
        public int BombTier { get; private set; } // CODEX BOMB TIERS: store tier for bomb specials.
        public int ItemTurnsRemaining { get; private set; } // CODEX STAGE 7B: track item lifetime.
        public int TumorTier { get; private set; } // CODEX REPLAYABILITY: optional tumor challenge tier.
        public bool IsPlayer => isPlayerPiece;

        private SpriteRenderer spriteRenderer;
        private Sprite baseSprite;
        private Sprite bombSprite;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite hurtSprite;
        [SerializeField] private Sprite enrageSprite;
        [SerializeField] private Sprite outSprite;
        [SerializeField] private Sprite deadSprite;
        [SerializeField] private Sprite matchedSprite;
        private bool isPlayerPiece;
        private bool hasInitializedSpecialType;
        private bool isDead;
        private bool isMatched;
        private bool isEnraged;
        private bool isOutState;
        private bool isHurt;
        // CODEX CHEST PR1
        private SpriteRenderer treasureOverlayRenderer;
        private SpriteRenderer hazardOverlayRenderer;
        private TextMesh treasureDebugText;
        // CODEX STAGE 7B
        private TextMesh itemTurnsText;
        private TileDebuffType currentTileDebuff = TileDebuffType.None;
        private int tileDebuffDuration;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var collider = GetComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
            UpdateTileDebuffVisual();
        }

        public void ApplyTileDebuff(TileDebuffType type, int duration)
        {
            currentTileDebuff = type;
            tileDebuffDuration = Mathf.Max(0, duration);
            UpdateTileDebuffVisual();
        }

        public void ClearTileDebuff()
        {
            currentTileDebuff = TileDebuffType.None;
            tileDebuffDuration = 0;
            UpdateTileDebuffVisual();
        }

        public TileDebuffType GetTileDebuff()
        {
            return currentTileDebuff;
        }

        public void TickTileDebuff()
        {
            if (currentTileDebuff == TileDebuffType.None)
            {
                return;
            }

            tileDebuffDuration--;
            if (tileDebuffDuration <= 0)
            {
                ClearTileDebuff();
            }
        }

        public int GetGuaranteedDebuffDamage()
        {
            return currentTileDebuff == TileDebuffType.Entangled ? 1 : 0;
        }

        public void SetVisualState(MonsterVisualState state)
        {
            if (isPlayerPiece)
            {
                return;
            }

            switch (state)
            {
                case MonsterVisualState.Angry:
                    SetMonsterVisualStates(true, false, false);
                    break;
                case MonsterVisualState.Cry:
                    SetMonsterVisualStates(false, true, false);
                    break;
                case MonsterVisualState.Hurt:
                    SetMonsterVisualStates(false, false, false);
                    SetMonsterHurtVisual();
                    break;
                case MonsterVisualState.Idle:
                default:
                    SetMonsterVisualStates(false, false, false);
                    break;
            }
        }

        public void SetMonsterVisualStates(bool enraged, bool outState, bool hurt)
        {
            if (isPlayerPiece)
            {
                return;
            }

            isEnraged = enraged;
            isOutState = outState;
            isHurt = hurt;
            RefreshVisual();
        }

        public void SetMonsterDeadVisual(bool dead)
        {
            if (isPlayerPiece)
            {
                return;
            }

            isDead = dead;
            RefreshVisual();
        }

        public void SetMonsterMatchedVisual(bool matched)
        {
            if (isPlayerPiece)
            {
                return;
            }

            isMatched = matched;
            RefreshVisual();
        }

        public void SetMonsterEnragedVisual(bool enraged)
        {
            if (isPlayerPiece)
            {
                return;
            }

            isEnraged = enraged;
            RefreshVisual();
        }

        public void SetMonsterOutStateVisual(bool outState)
        {
            if (isPlayerPiece)
            {
                return;
            }

            isOutState = outState;
            RefreshVisual();
        }

        public void SetMonsterHurtVisual(float duration = 0.3f)
        {
            if (isPlayerPiece || isDead || isMatched)
                return;

            StopCoroutine(nameof(HurtRoutine));
            StartCoroutine(HurtRoutine(duration));
        }

        private IEnumerator HurtRoutine(float duration)
        {
            isHurt = true;
            RefreshVisual();

            yield return new WaitForSeconds(duration);

            isHurt = false;
            RefreshVisual();
        }

        private void UpdateTileDebuffVisual()
        {
            if (tileDebuffOverlay == null)
            {
                return;
            }

            tileDebuffOverlay.SetActive(currentTileDebuff != TileDebuffType.None);
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
            TumorTier = 0;
            ItemTurnsRemaining = 0;
            baseSprite = null;
            bombSprite = null;
            hasInitializedSpecialType = true;
            isDead = false;
            isMatched = false;
            isEnraged = false;
            isOutState = false;
            isHurt = false;
            ClearTreasureChestVisual();
            ClearItemVisual();
            SetSprite(null);
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
            if (type != SpecialType.Tumor)
            {
                TumorTier = 0;
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


        // CODEX REPLAYABILITY: configure a tumor tile with a tier marker.
        public void ConfigureAsTumor(int tier)
        {
            if (isPlayerPiece)
            {
                return;
            }

            SpecialType = SpecialType.Tumor;
            hasInitializedSpecialType = true;
            BombTier = 0;
            bombSprite = null;
            ItemTurnsRemaining = 0;
            ClearItemVisual();
            ClearTreasureChestVisual();
            TumorTier = Mathf.Max(1, tier);
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
                SetSprite(null);
                spriteRenderer.color = Color.white;
                ClearHazardOverlay();
                return;
            }

            if (SpecialType == SpecialType.Tumor)
            {
                var tint = TumorTier >= 3 ? new Color(0.55f, 0.1f, 0.7f) : TumorTier == 2 ? new Color(0.7f, 0.15f, 0.15f) : new Color(0.85f, 0.3f, 0.3f);
                spriteRenderer.color = tint;
                RefreshVisual();
                return;
            }

            spriteRenderer.color = Color.white;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null || isPlayerPiece)
                return;

            if (isDead)
            {
                SetSprite(deadSprite);
            }
            else if (isMatched)
            {
                SetSprite(matchedSprite);
            }
            else if (isHurt) // Hurt temporarily overrides Enrage/OutState
            {
                SetSprite(hurtSprite);
            }
            else if (isEnraged)
            {
                SetSprite(enrageSprite);
            }
            else if (isOutState)
            {
                SetSprite(outSprite);
            }
            else
            {
                SetSprite(ResolveIdleSprite());
            }
        }

        private Sprite ResolveIdleSprite()
        {
            if (SpecialType == SpecialType.Bomb && bombSprite != null)
            {
                return bombSprite;
            }

            if (idleSprite != null)
            {
                return idleSprite;
            }

            return baseSprite;
        }

        private void SetSprite(Sprite sprite)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (sprite == null)
            {
                sprite = ResolveIdleSprite();
            }

            if (spriteRenderer.sprite == sprite)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
            if (hazardOverlayRenderer != null)
            {
                hazardOverlayRenderer.sprite = sprite;
            }
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


        public void SetHazardOverlay(Color color)
        {
            if (isPlayerPiece)
            {
                return;
            }

            var overlay = EnsureHazardOverlayRenderer();
            color.a = 0.35f;
            overlay.color = color;
            overlay.enabled = true;
        }

        public void ClearHazardOverlay()
        {
            if (hazardOverlayRenderer != null)
            {
                hazardOverlayRenderer.enabled = false;
            }
        }

        private SpriteRenderer EnsureHazardOverlayRenderer()
        {
            if (hazardOverlayRenderer != null)
            {
                return hazardOverlayRenderer;
            }

            var overlayObject = new GameObject("HazardOverlay");
            overlayObject.transform.SetParent(transform, false);
            hazardOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            hazardOverlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            hazardOverlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
            hazardOverlayRenderer.sprite = spriteRenderer.sprite;
            var overlayColor = Color.white;
            overlayColor.a = 0.35f;
            hazardOverlayRenderer.color = overlayColor;
            return hazardOverlayRenderer;
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
