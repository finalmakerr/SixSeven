using System.Collections;
using UnityEngine;

namespace GameCore
{
    public class MonsterAttackAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Header("Windup")]
        [SerializeField] private float windupDuration = 0.6f;
        [SerializeField] private float windupLeanBack = 0.12f;
        [SerializeField] private float windupShakeAmount = 0.04f;
        [SerializeField] private float windupScaleBoost = 0.08f;
        [SerializeField] private Color windupGlow = new Color(1f, 0.5f, 0.5f, 1f);
        [Header("Execute")]
        [SerializeField] private float executeDuration = 0.35f;
        [SerializeField] private float executeLungeDistance = 0.18f;
        [SerializeField] private float executeScaleBoost = 0.14f;
        [SerializeField] private Color executeGlow = new Color(1f, 0.3f, 0.3f, 1f);

        private static readonly int IsEnragedParam = Animator.StringToHash("IsEnraged");
        private static readonly int AttackWindupParam = Animator.StringToHash("AttackWindup");
        private static readonly int AttackExecuteParam = Animator.StringToHash("AttackExecute");

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private Color baseColor = Color.white;
        private Coroutine windupRoutine;
        private Coroutine executeRoutine;
        private bool isEnraged;

        public float AttackExecuteDuration => executeDuration;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            baseLocalPosition = transform.localPosition;
            baseLocalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }
        }

        public void SetEnraged(bool enraged)
        {
            isEnraged = enraged;
            if (animator != null)
            {
                animator.SetBool(IsEnragedParam, enraged);
            }

            if (!isEnraged)
            {
                ResetVisuals();
            }
        }

        public void TriggerWindup()
        {
            if (!isEnraged)
            {
                return;
            }

            if (executeRoutine != null)
            {
                StopCoroutine(executeRoutine);
                executeRoutine = null;
            }

            if (windupRoutine != null)
            {
                StopCoroutine(windupRoutine);
            }

            windupRoutine = StartCoroutine(WindupSequence());
        }

        public void TriggerAttackExecute()
        {
            if (executeRoutine != null)
            {
                StopCoroutine(executeRoutine);
            }

            if (windupRoutine != null)
            {
                StopCoroutine(windupRoutine);
                windupRoutine = null;
            }

            executeRoutine = StartCoroutine(ExecuteSequence());
        }

        private IEnumerator WindupSequence()
        {
            if (animator != null)
            {
                animator.ResetTrigger(AttackExecuteParam);
                animator.SetTrigger(AttackWindupParam);
            }

            var elapsed = 0f;
            while (elapsed < windupDuration)
            {
                if (!isEnraged)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / windupDuration);
                var shake = Mathf.Sin(Time.time * 40f) * windupShakeAmount * t;
                var lean = windupLeanBack * t;
                transform.localPosition = baseLocalPosition + new Vector3(shake, lean, 0f);
                transform.localScale = baseLocalScale * (1f + windupScaleBoost * t);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(baseColor, windupGlow, t);
                }
                yield return null;
            }
        }

        private IEnumerator ExecuteSequence()
        {
            if (animator != null)
            {
                animator.ResetTrigger(AttackWindupParam);
                animator.SetTrigger(AttackExecuteParam);
            }

            var elapsed = 0f;
            while (elapsed < executeDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / executeDuration);
                var lunge = Mathf.Sin(t * Mathf.PI);
                transform.localPosition = baseLocalPosition + new Vector3(0f, -executeLungeDistance * lunge, 0f);
                transform.localScale = baseLocalScale * (1f + executeScaleBoost * lunge);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(baseColor, executeGlow, lunge);
                }
                yield return null;
            }

            ResetVisuals();
        }

        private void ResetVisuals()
        {
            if (windupRoutine != null)
            {
                StopCoroutine(windupRoutine);
                windupRoutine = null;
            }

            if (executeRoutine != null)
            {
                StopCoroutine(executeRoutine);
                executeRoutine = null;
            }

            transform.localPosition = baseLocalPosition;
            transform.localScale = baseLocalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private void OnDisable()
        {
            isEnraged = false;
            ResetVisuals();
        }
    }
}
