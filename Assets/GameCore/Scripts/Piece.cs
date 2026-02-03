using System.Collections;
using UnityEngine;

namespace GameCore
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Piece : MonoBehaviour
    {
        [SerializeField] private float size = 0.9f;

        public enum SpecialType
        {
            None,
            Bomb,
            StrongBomb,
            MegaBomb,
            UltimateBomb
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int ColorIndex { get; private set; }
        public SpecialType Special { get; private set; }

        private SpriteRenderer spriteRenderer;
        private Coroutine moveRoutine;

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
            Special = SpecialType.None;
            SetColor(colorIndex, sprite);
            name = $"Piece_{x}_{y}";
        }

        public void SetColor(int colorIndex, Sprite sprite)
        {
            ColorIndex = colorIndex;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        public void SetSpecialType(SpecialType specialType)
        {
            Special = specialType;
        }

        public void SetPosition(int x, int y, Vector3 worldPosition)
        {
            X = x;
            Y = y;
            transform.position = worldPosition;
            name = $"Piece_{x}_{y}";
        }

        public void UpdateGridPosition(int x, int y)
        {
            X = x;
            Y = y;
            name = $"Piece_{x}_{y}";
        }

        public void MoveTo(Vector3 targetPosition, float duration)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveRoutine(targetPosition, duration));
        }

        private IEnumerator MoveRoutine(Vector3 targetPosition, float duration)
        {
            var start = transform.position;
            if (duration <= 0f)
            {
                transform.position = targetPosition;
                moveRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            moveRoutine = null;
        }
    }
}
