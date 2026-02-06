using UnityEngine;

namespace GameCore
{
    public class MonsterAttackTelegraph : MonoBehaviour
    {
        [SerializeField] private Color iconColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color countdownColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float pulseSpeed = 5f;
        [SerializeField] private float pulseScale = 0.08f;
        [SerializeField] private float urgentPulseScale = 0.14f;
        [SerializeField] private Vector3 countdownOffset = new Vector3(0f, -0.15f, 0f);

        private TextMesh iconText;
        private TextMesh countdownText;
        private int turnsRemaining;
        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            EnsureTextMeshes();
            ApplyColors();
            UpdateCountdownText();
        }

        private void Update()
        {
            if (turnsRemaining <= 0)
            {
                return;
            }

            var pulseAmount = turnsRemaining <= 1 ? urgentPulseScale : pulseScale;
            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * pulse;
        }

        public void SetTurnsRemaining(int turns)
        {
            turnsRemaining = Mathf.Max(0, turns);
            EnsureTextMeshes();
            UpdateCountdownText();
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

            if (countdownText != null)
            {
                countdownText.color = countdownColor;
            }
        }

        private void UpdateCountdownText()
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.text = turnsRemaining > 0 ? turnsRemaining.ToString() : string.Empty;
        }
    }
}
