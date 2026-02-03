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
        // STAGE 1
        private Coroutine shakeRoutine;
        // STAGE 5
        private Coroutine punchRoutine;
        private Vector3 baseScale;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var collider = GetComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
            baseScale = transform.localScale;
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

        // STAGE 5
        public void PunchScale(float peakScale, float duration)
        {
            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
            }

            punchRoutine = StartCoroutine(PunchScaleRoutine(peakScale, duration));
        }

        // STAGE 1
        public void MicroShake(float duration, float magnitude)
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        // STAGE 5
        private IEnumerator PunchScaleRoutine(float peakScale, float duration)
        {
            if (duration <= 0f)
            {
                transform.localScale = baseScale;
                punchRoutine = null;
                yield break;
            }

            var targetScale = baseScale * peakScale;
            var halfDuration = duration * 0.5f;
            var elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(baseScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(targetScale, baseScale, t);
                yield return null;
            }

            transform.localScale = baseScale;
            punchRoutine = null;
        }

        // STAGE 1
        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            var basePosition = transform.position;
            if (duration <= 0f || magnitude <= 0f)
            {
                transform.position = basePosition;
                shakeRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var offset = (Vector3)Random.insideUnitCircle * magnitude;
                transform.position = basePosition + offset;
                yield return null;
            }

            transform.position = basePosition;
            shakeRoutine = null;
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
