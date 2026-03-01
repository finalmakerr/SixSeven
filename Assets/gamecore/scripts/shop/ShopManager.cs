using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public sealed class ShopManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ShopUIController uiController;
        [SerializeField] private ShopItemGenerator itemGenerator;
        [SerializeField] private ShopDialogueSystem dialogueSystem;
        [SerializeField] private List<ShopkeeperProfile> shopkeepers = new List<ShopkeeperProfile>();
        [SerializeField, Min(1)] private int maxPurchasesPerShop = 4;

        private ShopRuntimeState activeState;
        private ShopRuntimeState reviveSnapshot;
        private ShopkeeperProfile activeShopkeeper;
        private Action onShopClosed;

        public bool IsShopOpen { get; private set; }

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            if (uiController != null)
            {
                uiController.PurchaseRequested += HandlePurchaseRequested;
                uiController.ContinueRequested += CloseShop;
            }
        }

        private void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.PurchaseRequested -= HandlePurchaseRequested;
                uiController.ContinueRequested -= CloseShop;
            }
        }

        public bool TryOpenBeforeBossLevel(int nextLevelIndex, Action continueToBoss)
        {
            if (itemGenerator == null)
            {
                continueToBoss?.Invoke();
                return false;
            }

            var isBossLevel = gameManager != null && gameManager.IsBossLevel(nextLevelIndex);
            if (!isBossLevel)
            {
                continueToBoss?.Invoke();
                return false;
            }

            OpenShopInternal(continueToBoss, false);
            return true;
        }

        public bool TryReviveToShopFromBossDeath()
        {
            if (reviveSnapshot == null)
            {
                return false;
            }

            activeState = reviveSnapshot.DeepCopy();
            OpenShopInternal(null, true);
            return true;
        }

        private void OpenShopInternal(Action continueToBoss, bool fromRevive)
        {
            onShopClosed = continueToBoss;
            IsShopOpen = true;

            if (!fromRevive || activeState == null)
            {
                activeShopkeeper = PickShopkeeper();
                var guaranteeOneUp = gameManager != null && !gameManager.HasOneUpInInventory();
                activeState = itemGenerator.GenerateState(activeShopkeeper, gameManager.PlayerInventory, gameManager.GetGold(), guaranteeOneUp);
                reviveSnapshot = activeState.DeepCopy();
            }
            else
            {
                activeShopkeeper = FindProfile(activeState.ActiveShopkeeperId);
            }

            gameManager?.SetShopActive(true);
            var line = dialogueSystem != null ? dialogueSystem.SelectLine(activeShopkeeper) : string.Empty;
            if (uiController != null)
            {
                uiController.Show(activeState, line);
            }
        }

        private void CloseShop()
        {
            if (!IsShopOpen)
            {
                return;
            }

            IsShopOpen = false;
            gameManager?.SetShopActive(false);
            uiController?.Hide();
            var callback = onShopClosed;
            onShopClosed = null;
            callback?.Invoke();
        }

        private void HandlePurchaseRequested(int slotIndex)
        {
            if (activeState == null || gameManager == null || activeState.PurchaseCount >= maxPurchasesPerShop)
            {
                return;
            }

            var useReplacement = slotIndex < 0;
            ShopOffer offer;
            int price;

            if (useReplacement)
            {
                if (!activeState.ReplacementTriggered || activeState.ReplacementConsumed)
                {
                    return;
                }

                offer = activeState.ReplacementItem;
                price = activeState.PriceSnapshot[itemGenerator.BaseSlotCount];
            }
            else
            {
                if (slotIndex >= activeState.BaseSlotItems.Count || activeState.PurchasedFlags[slotIndex])
                {
                    return;
                }

                offer = activeState.BaseSlotItems[slotIndex];
                price = activeState.PriceSnapshot[slotIndex];
            }

            if (!gameManager.TrySpendGold(price))
            {
                return;
            }

            if (!gameManager.TryGrantShopOffer(offer))
            {
                gameManager.AddGold(price);
                return;
            }

            activeState.PurchaseCount += 1;
            var replacementWasTriggered = activeState.ReplacementTriggered;
            if (useReplacement)
            {
                activeState.ReplacementConsumed = true;
            }
            else
            {
                activeState.PurchasedFlags[slotIndex] = true;
            }

            if (activeState.PurchaseCount >= 3)
            {
                activeState.ReplacementTriggered = true;
            }

            var replacementJustUnlocked = !replacementWasTriggered && activeState.ReplacementTriggered;
            if (replacementJustUnlocked && !activeState.ReplacementUnlockDialoguePlayed)
            {
                activeState.ReplacementUnlockDialoguePlayed = true;
                var unlockLine = dialogueSystem != null
                    ? dialogueSystem.SelectReplacementUnlockLine(activeShopkeeper, activeState.ReplacementItem.Category)
                    : string.Empty;
                if (!string.IsNullOrWhiteSpace(unlockLine))
                {
                    uiController?.ShowTemporaryDialogue(unlockLine);
                }

                AudioManager.Instance?.PlayShopReplacementUnlock();
            }

            if (activeState.PurchaseCount >= maxPurchasesPerShop)
            {
                activeState.ReplacementConsumed = true;
            }

            reviveSnapshot = activeState.DeepCopy();
            uiController?.Refresh();
        }

        private ShopkeeperProfile PickShopkeeper()
        {
            if (shopkeepers == null || shopkeepers.Count == 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, shopkeepers.Count);
            return shopkeepers[index];
        }

        private ShopkeeperProfile FindProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return null;
            }

            for (var i = 0; i < shopkeepers.Count; i++)
            {
                var profile = shopkeepers[i];
                if (profile != null && profile.ShopkeeperId == profileId)
                {
                    return profile;
                }
            }

            return null;
        }
    }
}
