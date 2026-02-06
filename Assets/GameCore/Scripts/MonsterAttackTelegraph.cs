using UnityEngine;

namespace GameCore
{
    public class MonsterAttackTelegraph : MonoBehaviour
    {
        [SerializeField] private Color iconColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color twoTurnsColor = new Color(1f, 0.6f, 0.2f, 0.9f);
        [SerializeField] private Color oneTurnColor = new Color(1f, 0.2f, 0.2f, 0.95f);
        [SerializeField] private float slowPulseSpeed = 2.2f;
        [SerializeField] private float fastPulseSpeed = 5.5f;
        [SerializeField] private float pulseScale = 0.08f;
        [SerializeField] private float urgentPulseScale = 0.14f;
        [SerializeField] private Vector3 countdownOffset = new Vector3(0f, -0.15f, 0f);
        [SerializeField] private float outlineRadius = 0.42f;
        [SerializeField] private int outlineSegments = 32;
        [SerializeField] private float thinOutlineWidth = 0.035f;
        [SerializeField] private float thickOutlineWidth = 0.075f;

        private TextMesh iconText;
        private TextMesh countdownText;
        private LineRenderer outlineRenderer;
        private int turnsRemaining;
        private Vector3 baseScale;
        private float currentPulseSpeed;

        private void Awake()
        {
            baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            EnsureTextMeshes();
            ApplyColors();
            EnsureOutlineRenderer();
            UpdateTelegraphState();
        }

        private void Update()
        {
            if (turnsRemaining <= 0)
            {
                return;
            }

            var pulseAmount = turnsRemaining <= 1 ? urgentPulseScale : pulseScale;
            var pulse = 1f + Mathf.Sin(Time.time * currentPulseSpeed) * pulseAmount;
            transform.localScale = baseScale * pulse;
        }

        public void SetTurnsRemaining(int turns)
        {
            turnsRemaining = Mathf.Max(0, turns);
            EnsureTextMeshes();
            EnsureOutlineRenderer();
            UpdateTelegraphState();
        }

        public void SetWorldPosition(Vector3 position)
        {
            transform.position = position;
        }

        private void EnsureTextMeshes()
        {
            if (iconText != null && countdownText != null)
            {
                return;
            }

            var textMeshes = GetComponentsInChildren<TextMesh>(true);
            if (textMeshes.Length > 0)
            {
                iconText = textMeshes[0];
            }

            if (textMeshes.Length > 1)
            {
                countdownText = textMeshes[1];
            }

            if (iconText == null)
            {
                iconText = CreateTextMesh("AttackIcon", Vector3.zero, 64, "!");
            }
            else if (string.IsNullOrEmpty(iconText.text))
            {
                iconText.text = "!";
            }

            if (countdownText == null)
            {
                countdownText = CreateTextMesh("AttackCountdown", countdownOffset, 48, string.Empty);
            }

            countdownText.transform.localPosition = countdownOffset;
            countdownText.text = string.Empty;
            if (countdownText.gameObject.activeSelf)
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        private TextMesh CreateTextMesh(string name, Vector3 localPosition, int fontSize, string text)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = localPosition;
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = 0.1f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            var renderer = textObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
            }
            return textMesh;
        }

        private void ApplyColors()
        {
            if (iconText != null)
            {
                iconText.color = iconColor;
            }
        }

        private void EnsureOutlineRenderer()
        {
            if (outlineRenderer != null)
            {
                return;
            }

            var outlineObject = new GameObject("AttackOutline");
            outlineObject.transform.SetParent(transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineRenderer = outlineObject.AddComponent<LineRenderer>();
            outlineRenderer.loop = true;
            outlineRenderer.useWorldSpace = false;
            outlineRenderer.positionCount = outlineSegments + 1;
            outlineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            outlineRenderer.sortingOrder = 10;
            UpdateOutlinePositions();
        }

        private void UpdateOutlinePositions()
        {
            if (outlineRenderer == null)
            {
                return;
            }

            for (var i = 0; i <= outlineSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / outlineSegments;
                var position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * outlineRadius;
                outlineRenderer.SetPosition(i, position);
            }
        }

        private void UpdateTelegraphState()
        {
            if (turnsRemaining <= 0)
            {
                if (outlineRenderer != null)
                {
                    outlineRenderer.enabled = false;
                }
                return;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = true;
            }

            var isUrgent = turnsRemaining <= 1;
            currentPulseSpeed = isUrgent ? fastPulseSpeed : slowPulseSpeed;
            var tint = isUrgent ? oneTurnColor : twoTurnsColor;
            var width = isUrgent ? thickOutlineWidth : thinOutlineWidth;

            if (iconText != null)
            {
                iconText.color = tint;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.startColor = tint;
                outlineRenderer.endColor = tint;
                outlineRenderer.startWidth = width;
                outlineRenderer.endWidth = width;
            }
        }
    }
}
