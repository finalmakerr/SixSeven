using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    // CODEX BONUS PR5
    public class MemoryBonusGame : BonusMiniGameBase
    {
        private const int Columns = 4;
        private const int Rows = 3;
        private const float TotalTimeSeconds = 10f;
        private const float CountdownStepSeconds = 0.8f;
        private const string BonusFontKey = "MemoryBonusFont";

        private readonly List<MemoryCard> cards = new List<MemoryCard>();
        private readonly List<int> cardValues = new List<int>();

        private Transform uiParent;
        private System.Random randomSeed;
        private GameObject panel;
        private Text headerText;
        private Text instructionText;
        private Text timerText;
        private Text calloutText;
        private Text resultText;
        private GridLayoutGroup grid;
        private AudioSource audioSource;
        private AudioClip tickClip;
        private Font bonusFont;
        private bool inputLocked = true;
        private bool gameComplete;
        private Coroutine timerRoutine;
        private MemoryCard firstSelection;
        private MemoryCard secondSelection;

        public override string GameName => "Memory";

        public override void Begin(Transform uiParent, System.Random randomSeed)
        {
            this.uiParent = uiParent;
            this.randomSeed = randomSeed ?? new System.Random();
            LoadAssetsFromSceneAssetLoader();
            BuildUI();
            SetupCards();
            StartCoroutine(CountdownRoutine());
        }

        public override void StopGame()
        {
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }

            if (panel != null)
            {
                Destroy(panel);
            }

            Destroy(gameObject);
        }

        private void BuildUI()
        {
            panel = new GameObject("MemoryBonusPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(uiParent, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            headerText = CreateText("MemoryHeader", 38, TextAnchor.UpperCenter, new Vector2(0f, -30f), new Vector2(700f, 60f));
            headerText.text = "MEMORY BONUS";

            instructionText = CreateText("MemoryInstructions", 22, TextAnchor.UpperCenter, new Vector2(0f, -90f), new Vector2(820f, 80f));
            instructionText.text = "Match all pairs. One mistake = FAIL.";

            timerText = CreateText("MemoryTimer", 24, TextAnchor.UpperCenter, new Vector2(0f, -150f), new Vector2(300f, 50f));
            timerText.text = "Time: 10.0";

            calloutText = CreateText("MemoryCallout", 64, TextAnchor.MiddleCenter, new Vector2(0f, 170f), new Vector2(400f, 120f));
            calloutText.text = string.Empty;

            resultText = CreateText("MemoryResult", 52, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(500f, 120f));
            resultText.text = string.Empty;

            var gridObject = new GameObject("MemoryGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(panel.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, -40f);
            gridRect.sizeDelta = new Vector2(620f, 420f);

            grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.cellSize = new Vector2(140f, 110f);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            audioSource = panel.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            tickClip = BuildTickClip();
        }

        private Text CreateText(string name, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            if (bonusFont != null)
            {
                text.font = bonusFont;
            }
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }


        private void LoadAssetsFromSceneAssetLoader()
        {
            var sceneAssetLoader = FindObjectOfType<SceneAssetLoader>();
            if (sceneAssetLoader == null)
            {
                return;
            }

            var catalog = sceneAssetLoader.GetLoadedAsset<MemoryBonusAssetCatalog>();
            if (catalog == null)
            {
                return;
            }

            bonusFont = catalog.GetFont(BonusFontKey);
        }

        private void SetupCards()
        {
            cards.Clear();
            cardValues.Clear();

            for (var pair = 0; pair < Rows * Columns / 2; pair++)
            {
                cardValues.Add(pair);
                cardValues.Add(pair);
            }

            Shuffle(cardValues);

            for (var i = 0; i < cardValues.Count; i++)
            {
                var cardObject = new GameObject($"Card{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                cardObject.transform.SetParent(grid.transform, false);
                var image = cardObject.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.9f);

                var button = cardObject.GetComponent<Button>();
                var label = CreateCardLabel(cardObject.transform, "?");

                var card = new MemoryCard
                {
                    Button = button,
                    Label = label,
                    Value = cardValues[i],
                    Matched = false
                };

                button.onClick.AddListener(() => HandleCardClicked(card));
                cards.Add(card);
            }
        }

        private Text CreateCardLabel(Transform parent, string label)
        {
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<Text>();
            if (bonusFont != null)
            {
                text.font = bonusFont;
            }
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.text = label;
            return text;
        }

        private void HandleCardClicked(MemoryCard card)
        {
            if (inputLocked || gameComplete || card.Matched || card == firstSelection || card == secondSelection)
            {
                return;
            }

            RevealCard(card);

            if (firstSelection == null)
            {
                firstSelection = card;
                return;
            }

            secondSelection = card;
            inputLocked = true;

            if (firstSelection.Value != secondSelection.Value)
            {
                TriggerFailure();
                return;
            }

            firstSelection.Matched = true;
            secondSelection.Matched = true;
            firstSelection = null;
            secondSelection = null;
            inputLocked = false;

            if (cards.TrueForAll(cardData => cardData.Matched))
            {
                TriggerSuccess();
            }
        }

        private void RevealCard(MemoryCard card)
        {
            card.Label.text = GetCardLabel(card.Value);
            if (card.Button != null)
            {
                var image = card.Button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.7f, 1f, 0.7f, 1f);
                }
            }
        }

        private void HideCard(MemoryCard card)
        {
            card.Label.text = "?";
            if (card.Button != null)
            {
                var image = card.Button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(1f, 1f, 1f, 0.9f);
                }
            }
        }

        private IEnumerator CountdownRoutine()
        {
            inputLocked = true;
            calloutText.text = "Get ready!";
            yield return new WaitForSeconds(CountdownStepSeconds);

            var countdown = new[] { "3", "2", "1", "GOO!" };
            foreach (var item in countdown)
            {
                calloutText.text = item;
                yield return new WaitForSeconds(CountdownStepSeconds);
            }

            calloutText.text = string.Empty;
            inputLocked = false;
            timerRoutine = StartCoroutine(TimerRoutine());
        }

        private IEnumerator TimerRoutine()
        {
            var elapsed = 0f;
            var nextTickTime = 6f;
            var tickInterval = 1f;

            while (elapsed < TotalTimeSeconds && !gameComplete)
            {
                elapsed += Time.deltaTime;
                var remaining = Mathf.Max(0f, TotalTimeSeconds - elapsed);
                if (timerText != null)
                {
                    timerText.text = $"Time: {remaining:0.0}";
                }

                if (elapsed >= nextTickTime)
                {
                    PlayTick();
                    if (Mathf.Approximately(nextTickTime, 6f))
                    {
                        StartCoroutine(CalloutRoutine("6"));
                    }
                    else if (Mathf.Approximately(nextTickTime, 7f))
                    {
                        StartCoroutine(CalloutRoutine("7"));
                        tickInterval = 0.5f;
                    }

                    nextTickTime += tickInterval;
                }

                yield return null;
            }

            if (!gameComplete)
            {
                TriggerFailure();
            }
        }

        private IEnumerator CalloutRoutine(string message)
        {
            calloutText.text = message;
            yield return new WaitForSeconds(0.45f);
            if (!gameComplete)
            {
                calloutText.text = string.Empty;
            }
        }

        private void TriggerSuccess()
        {
            if (gameComplete)
            {
                return;
            }

            gameComplete = true;
            inputLocked = true;
            resultText.text = "😊";
            Complete(true);
        }

        private void TriggerFailure()
        {
            if (gameComplete)
            {
                return;
            }

            gameComplete = true;
            inputLocked = true;
            resultText.text = "😢";

            if (firstSelection != null)
            {
                HideCard(firstSelection);
            }

            if (secondSelection != null)
            {
                HideCard(secondSelection);
            }

            Complete(false);
        }

        private void PlayTick()
        {
            if (audioSource != null && tickClip != null)
            {
                audioSource.PlayOneShot(tickClip);
            }
        }

        private AudioClip BuildTickClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.08f;
            const float frequency = 880f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("MemoryTick", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.2f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private void Shuffle(List<int> values)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = randomSeed.Next(i + 1);
                var temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

        private string GetCardLabel(int value)
        {
            return ((char)('A' + value)).ToString();
        }

        private class MemoryCard
        {
            public Button Button;
            public Text Label;
            public int Value;
            public bool Matched;
        }
    }
}
