using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [CreateAssetMenu(menuName = "SixSeven/Shop/Shopkeeper Profile", fileName = "ShopkeeperProfile")]
    public sealed class ShopkeeperProfile : ScriptableObject
    {
        [SerializeField] private string shopkeeperId;
        [SerializeField, Min(0.1f)] private float priceMultiplier = 1f;
        [SerializeField] private List<CategoryWeightModifier> categoryWeightModifiers = new List<CategoryWeightModifier>();
        [SerializeField] private List<string> dialogueBlocks = new List<string>();
        [SerializeField] private List<ReplacementUnlockBlock> replacementUnlockBlocks = new List<ReplacementUnlockBlock>();
        [SerializeField] private AudioClip identitySound;

        public string ShopkeeperId => string.IsNullOrWhiteSpace(shopkeeperId) ? name : shopkeeperId;
        public float PriceMultiplier => Mathf.Max(0.1f, priceMultiplier);
        public IReadOnlyList<CategoryWeightModifier> CategoryWeightModifiers => categoryWeightModifiers;
        public IReadOnlyList<string> DialogueBlocks => dialogueBlocks;
        public IReadOnlyList<ReplacementUnlockBlock> ReplacementUnlockBlocks => replacementUnlockBlocks;
        public AudioClip IdentitySound => identitySound;

        public float GetWeightMultiplier(ShopOfferCategory category)
        {
            for (var i = 0; i < categoryWeightModifiers.Count; i++)
            {
                if (categoryWeightModifiers[i].Category == category)
                {
                    return Mathf.Max(0f, categoryWeightModifiers[i].WeightMultiplier);
                }
            }

            return 1f;
        }
    }

    [Serializable]
    public struct ReplacementUnlockBlock
    {
        public ShopOfferCategory Category;
        public List<string> Lines;
    }

    [Serializable]
    public struct CategoryWeightModifier
    {
        public ShopOfferCategory Category;
        [Min(0f)] public float WeightMultiplier;
    }
}
