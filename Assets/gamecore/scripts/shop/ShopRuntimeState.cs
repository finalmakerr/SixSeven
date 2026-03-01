using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public sealed class ShopRuntimeState
    {
        [SerializeField] private string activeShopkeeperId;
        [SerializeField] private List<ShopOffer> baseSlotItems = new List<ShopOffer>(3);
        [SerializeField] private ShopOffer replacementItem;
        [SerializeField] private List<bool> purchasedFlags = new List<bool>(3);
        [SerializeField] private bool replacementTriggered;
        [SerializeField] private bool replacementConsumed;
        [SerializeField] private bool replacementUnlockDialoguePlayed;
        [SerializeField] private List<int> priceSnapshot = new List<int>(4);

        public string ActiveShopkeeperId
        {
            get => activeShopkeeperId;
            set => activeShopkeeperId = value;
        }

        public List<ShopOffer> BaseSlotItems => baseSlotItems;
        public ShopOffer ReplacementItem
        {
            get => replacementItem;
            set => replacementItem = value;
        }

        public List<bool> PurchasedFlags => purchasedFlags;
        public bool ReplacementTriggered
        {
            get => replacementTriggered;
            set => replacementTriggered = value;
        }

        public bool ReplacementConsumed
        {
            get => replacementConsumed;
            set => replacementConsumed = value;
        }

        public bool ReplacementUnlockDialoguePlayed
        {
            get => replacementUnlockDialoguePlayed;
            set => replacementUnlockDialoguePlayed = value;
        }

        public List<int> PriceSnapshot => priceSnapshot;
        public int PurchaseCount { get; set; }

        public void EnsureShape(int baseSlots)
        {
            while (purchasedFlags.Count < baseSlots)
            {
                purchasedFlags.Add(false);
            }

            while (priceSnapshot.Count < baseSlots + 1)
            {
                priceSnapshot.Add(0);
            }
        }

        public ShopRuntimeState DeepCopy()
        {
            var clone = new ShopRuntimeState
            {
                activeShopkeeperId = activeShopkeeperId,
                replacementItem = replacementItem,
                replacementTriggered = replacementTriggered,
                replacementConsumed = replacementConsumed,
                replacementUnlockDialoguePlayed = replacementUnlockDialoguePlayed,
                PurchaseCount = PurchaseCount
            };

            clone.baseSlotItems.AddRange(baseSlotItems);
            clone.purchasedFlags.AddRange(purchasedFlags);
            clone.priceSnapshot.AddRange(priceSnapshot);
            return clone;
        }
    }

    [Serializable]
    public struct ShopOffer
    {
        public string OfferId;
        public PlayerItemType ItemType;
        public ShopOfferCategory Category;
        public bool IsSpecial;
        public bool IsOneUp;
    }

    public enum ShopOfferCategory
    {
        Utility = 0,
        Defense = 1,
        Recovery = 2,
        Economy = 3,
        Special = 4
    }
}
