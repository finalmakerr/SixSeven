using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public sealed class ShopUIController : MonoBehaviour
    {
        [Serializable]
        private struct SlotView
        {
            public Text ItemLabel;
            public Text PriceLabel;
            public Text OverlayLabel;
            public Button BuyButton;
        }

        [SerializeField] private GameObject root;
        [SerializeField] private SlotView[] baseSlots = new SlotView[3];
        [SerializeField] private SlotView replacementSlot;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text dialogueText;

        private ShopRuntimeState state;
        private string persistentDialogue = string.Empty;
        private int temporaryDialogueToken;

        public event Action<int> PurchaseRequested;
        public event Action ContinueRequested;

        private void Awake()
        {
            for (var i = 0; i < baseSlots.Length; i++)
            {
                var index = i;
                if (baseSlots[i].BuyButton != null)
                {
                    baseSlots[i].BuyButton.onClick.RemoveAllListeners();
                    baseSlots[i].BuyButton.onClick.AddListener(() => PurchaseRequested?.Invoke(index));
                }
            }

            if (replacementSlot.BuyButton != null)
            {
                replacementSlot.BuyButton.onClick.RemoveAllListeners();
                replacementSlot.BuyButton.onClick.AddListener(() => PurchaseRequested?.Invoke(-1));
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => ContinueRequested?.Invoke());
            }
        }

        public void Show(ShopRuntimeState runtimeState, string dialogue)
        {
            state = runtimeState;
            SetVisible(true);
            persistentDialogue = dialogue ?? string.Empty;
            if (dialogueText != null)
            {
                dialogueText.text = persistentDialogue;
            }

            Refresh();
        }

        public void Hide()
        {
            temporaryDialogueToken++;
            SetVisible(false);
        }

        public void ShowTemporaryDialogue(string text, float duration = 2.5f)
        {
            if (dialogueText == null)
            {
                return;
            }

            var token = ++temporaryDialogueToken;
            dialogueText.text = text ?? string.Empty;
            StartCoroutine(RestoreDialogueAfterDelay(token, Mathf.Max(0f, duration)));
        }

        public void Refresh()
        {
            if (state == null)
            {
                return;
            }

            for (var i = 0; i < baseSlots.Length && i < state.BaseSlotItems.Count; i++)
            {
                var purchased = i < state.PurchasedFlags.Count && state.PurchasedFlags[i];
                var offer = state.BaseSlotItems[i];
                BindSlot(baseSlots[i], offer, state.PriceSnapshot[i], purchased, state.PurchaseCount >= 4);
                if (baseSlots[i].OverlayLabel != null)
                {
                    baseSlots[i].OverlayLabel.text = purchased ? "V" : ResolveQuestionMark(i);
                }
            }

            var showReplacement = state.ReplacementTriggered && !state.ReplacementConsumed;
            if (replacementSlot.BuyButton != null)
            {
                replacementSlot.BuyButton.gameObject.SetActive(showReplacement);
            }

            if (showReplacement)
            {
                BindSlot(replacementSlot, state.ReplacementItem, state.PriceSnapshot[3], false, state.PurchaseCount >= 4);
                if (replacementSlot.OverlayLabel != null)
                {
                    replacementSlot.OverlayLabel.text = string.Empty;
                }
            }
        }

        private string ResolveQuestionMark(int index)
        {
            if (state.PurchaseCount == 2 && index == 0)
            {
                return "?";
            }

            if (state.PurchaseCount == 3)
            {
                if (index == 0 || index == 1)
                {
                    return "?";
                }
            }

            return "-";
        }

        private static void BindSlot(SlotView view, ShopOffer offer, int price, bool purchased, bool locked)
        {
            if (view.ItemLabel != null)
            {
                view.ItemLabel.text = offer.OfferId;
            }

            if (view.PriceLabel != null)
            {
                view.PriceLabel.text = $"{price}g";
            }

            if (view.BuyButton != null)
            {
                view.BuyButton.interactable = !purchased && !locked;
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        private System.Collections.IEnumerator RestoreDialogueAfterDelay(int token, float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            if (token != temporaryDialogueToken || dialogueText == null)
            {
                yield break;
            }

            dialogueText.text = persistentDialogue;
        }
    }
}
