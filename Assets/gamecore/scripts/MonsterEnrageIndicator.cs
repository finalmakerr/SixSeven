using UnityEngine;

namespace GameCore
{
    public class MonsterEnrageIndicator : MonoBehaviour
    {
        [SerializeField] private Color enragedTint = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float pulseStrength = 0.12f;

        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private Vector3 baseScale = Vector3.one;
        private bool isEnraged;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        }

        private void Update()
        {
            if (!isEnraged)
            {
                return;
            }

            var pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(baseColor, enragedTint, 0.35f + 0.65f * pulse);
            }

            transform.localScale = baseScale * (1f + pulse * pulseStrength);
        }

        public void SetEnraged(bool enraged)
        {
            isEnraged = enraged;
            if (!isEnraged)
            {
                ResetVisuals();
            }
        }

        private void ResetVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            transform.localScale = baseScale;
        }

        private void OnDisable()
        {
            isEnraged = false;
            ResetVisuals();
        }
    }
}
