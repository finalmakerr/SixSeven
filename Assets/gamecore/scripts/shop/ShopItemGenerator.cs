using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public sealed class ShopItemGenerator : MonoBehaviour
    {
        [Serializable]
        public struct ShopItemDefinition
        {
            public string OfferId;
            public PlayerItemType ItemType;
            public ShopOfferCategory Category;
            [Min(0)] public int BasePrice;
            [Min(0)] public int Weight;
            public bool UpgradeAware;
        }

        [SerializeField] private List<ShopItemDefinition> regularPool = new List<ShopItemDefinition>();
        [SerializeField] private List<ShopItemDefinition> specialPool = new List<ShopItemDefinition>();
        [SerializeField] private int baseSlotCount = 3;
        [SerializeField, Range(0f, 1f)] private float replacementSpecialChance = 0.6f;
        [SerializeField, Range(0f, 1f)] private float discountMultiplier = 0.5f;
        [SerializeField, Min(1)] private int fixedOneUpPrice = 10;
        [SerializeField, Min(1)] private int specialPriceBaseOffset = 4;
        [SerializeField, Min(1)] private int specialPriceGoldScaleDivisor = 3;

        public int BaseSlotCount => baseSlotCount;
        public int FixedOneUpPrice => fixedOneUpPrice;

        public ShopRuntimeState GenerateState(ShopkeeperProfile shopkeeper, PlayerItemInventory inventory, int gold, bool shouldGuaranteeOneUp)
        {
            var state = new ShopRuntimeState
            {
                ActiveShopkeeperId = shopkeeper != null ? shopkeeper.ShopkeeperId : string.Empty
            };

            state.EnsureShape(baseSlotCount);

            for (var i = 0; i < baseSlotCount; i++)
            {
                var offer = GenerateRegularOffer(shopkeeper, state.BaseSlotItems);
                state.BaseSlotItems.Add(offer);
                state.PriceSnapshot[i] = ComputePrice(offer, shopkeeper, gold, false);
            }

            if (shouldGuaranteeOneUp)
            {
                var slot = UnityEngine.Random.Range(0, baseSlotCount);
                state.BaseSlotItems[slot] = CreateOneUpOffer();
                state.PriceSnapshot[slot] = fixedOneUpPrice;
            }

            var replacementIsSpecial = UnityEngine.Random.value <= replacementSpecialChance;
            var replacement = replacementIsSpecial
                ? GenerateSpecialOffer(shopkeeper, inventory, state.BaseSlotItems)
                : GenerateRegularOffer(shopkeeper, state.BaseSlotItems);

            replacement.IsOneUp = false;
            state.ReplacementItem = replacement;
            state.PriceSnapshot[baseSlotCount] = replacementIsSpecial
                ? ComputePrice(replacement, shopkeeper, gold, true)
                : Mathf.Max(1, Mathf.RoundToInt(ComputePrice(replacement, shopkeeper, gold, false) * discountMultiplier));

            return state;
        }

        private ShopOffer CreateOneUpOffer()
        {
            return new ShopOffer
            {
                OfferId = "1UP",
                ItemType = PlayerItemType.SecondChance,
                Category = ShopOfferCategory.Defense,
                IsOneUp = true,
                IsSpecial = false
            };
        }

        private ShopOffer GenerateRegularOffer(ShopkeeperProfile shopkeeper, List<ShopOffer> disallow)
        {
            return GenerateFromPool(regularPool, shopkeeper, disallow, false);
        }

        private ShopOffer GenerateSpecialOffer(ShopkeeperProfile shopkeeper, PlayerItemInventory inventory, List<ShopOffer> disallow)
        {
            var offer = GenerateFromPool(specialPool, shopkeeper, disallow, true);
            if (inventory != null && inventory.HasItem(offer.ItemType))
            {
                offer.IsSpecial = true;
            }

            return offer;
        }

        private ShopOffer GenerateFromPool(List<ShopItemDefinition> pool, ShopkeeperProfile shopkeeper, List<ShopOffer> disallow, bool special)
        {
            if (pool == null || pool.Count == 0)
            {
                return new ShopOffer
                {
                    OfferId = "fallback-energy-pack",
                    ItemType = PlayerItemType.EnergyPack,
                    Category = ShopOfferCategory.Utility,
                    IsSpecial = special,
                    IsOneUp = false
                };
            }

            var weights = new float[pool.Count];
            var total = 0f;
            for (var i = 0; i < pool.Count; i++)
            {
                var entry = pool[i];
                var weight = Mathf.Max(0, entry.Weight);
                if (shopkeeper != null)
                {
                    weight *= shopkeeper.GetWeightMultiplier(entry.Category);
                }

                if (disallow != null)
                {
                    for (var d = 0; d < disallow.Count; d++)
                    {
                        if (disallow[d].OfferId == entry.OfferId)
                        {
                            weight = 0f;
                            break;
                        }
                    }
                }

                weights[i] = weight;
                total += weight;
            }

            if (total <= 0f)
            {
                var fallbackEntry = pool[UnityEngine.Random.Range(0, pool.Count)];
                return CreateOffer(fallbackEntry, special);
            }

            var roll = UnityEngine.Random.Range(0f, total);
            var acc = 0f;
            for (var i = 0; i < pool.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc)
                {
                    return CreateOffer(pool[i], special);
                }
            }

            return CreateOffer(pool[pool.Count - 1], special);
        }

        private static ShopOffer CreateOffer(ShopItemDefinition entry, bool special)
        {
            return new ShopOffer
            {
                OfferId = entry.OfferId,
                ItemType = entry.ItemType,
                Category = entry.Category,
                IsSpecial = special,
                IsOneUp = false
            };
        }

        public int ComputePrice(ShopOffer offer, ShopkeeperProfile shopkeeper, int gold, bool isSpecialReplacement)
        {
            var basePrice = ResolveBasePrice(offer);
            var multiplier = shopkeeper != null ? shopkeeper.PriceMultiplier : 1f;
            var price = Mathf.RoundToInt(basePrice * multiplier);

            if (isSpecialReplacement)
            {
                var scaled = specialPriceBaseOffset + Mathf.Max(0, gold) / Mathf.Max(1, specialPriceGoldScaleDivisor);
                price += scaled;
            }

            return Mathf.Max(1, price);
        }

        private int ResolveBasePrice(ShopOffer offer)
        {
            if (offer.IsOneUp)
            {
                return fixedOneUpPrice;
            }

            for (var i = 0; i < regularPool.Count; i++)
            {
                if (regularPool[i].OfferId == offer.OfferId)
                {
                    return Mathf.Max(1, regularPool[i].BasePrice);
                }
            }

            for (var i = 0; i < specialPool.Count; i++)
            {
                if (specialPool[i].OfferId == offer.OfferId)
                {
                    return Mathf.Max(1, specialPool[i].BasePrice);
                }
            }

            return fixedOneUpPrice;
        }
    }
}
