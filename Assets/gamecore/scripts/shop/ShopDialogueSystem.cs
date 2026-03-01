using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public sealed class ShopDialogueSystem : MonoBehaviour
    {
        [SerializeField] private AudioSource dialogueAudioSource;
        private readonly Dictionary<string, int> lastLineByShopkeeper = new Dictionary<string, int>();

        public string SelectLine(ShopkeeperProfile profile)
        {
            if (profile == null || profile.DialogueBlocks == null || profile.DialogueBlocks.Count == 0)
            {
                return string.Empty;
            }

            var lines = profile.DialogueBlocks;
            var count = lines.Count;
            var previous = lastLineByShopkeeper.TryGetValue(profile.ShopkeeperId, out var index) ? index : -1;
            var chosen = Random.Range(0, count);
            if (count > 1 && chosen == previous)
            {
                chosen = (chosen + 1) % count;
            }

            lastLineByShopkeeper[profile.ShopkeeperId] = chosen;
            TriggerIdentitySound(profile);
            return lines[chosen];
        }

        public string SelectReplacementUnlockLine(ShopkeeperProfile profile, ShopOfferCategory category)
        {
            if (profile == null)
            {
                return string.Empty;
            }

            var unlockLines = GetReplacementUnlockLines(profile, category);
            if (unlockLines != null && unlockLines.Count > 0)
            {
                return SelectLineFromList(profile, unlockLines);
            }

            if (profile.DialogueBlocks == null || profile.DialogueBlocks.Count == 0)
            {
                return string.Empty;
            }

            return SelectLineFromList(profile, profile.DialogueBlocks);
        }

        private static IReadOnlyList<string> GetReplacementUnlockLines(ShopkeeperProfile profile, ShopOfferCategory category)
        {
            var blocks = profile.ReplacementUnlockBlocks;
            if (blocks == null || blocks.Count == 0)
            {
                return null;
            }

            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block.Category == category && block.Lines != null && block.Lines.Count > 0)
                {
                    return block.Lines;
                }
            }

            return null;
        }

        private string SelectLineFromList(ShopkeeperProfile profile, IReadOnlyList<string> lines)
        {
            var count = lines.Count;
            var previous = lastLineByShopkeeper.TryGetValue(profile.ShopkeeperId, out var index) ? index : -1;
            var chosen = Random.Range(0, count);
            if (count > 1 && chosen == previous)
            {
                chosen = (chosen + 1) % count;
            }

            lastLineByShopkeeper[profile.ShopkeeperId] = chosen;
            return lines[chosen];
        }

        private void TriggerIdentitySound(ShopkeeperProfile profile)
        {
            if (profile == null || profile.IdentitySound == null)
            {
                return;
            }

            if (dialogueAudioSource != null)
            {
                dialogueAudioSource.PlayOneShot(profile.IdentitySound);
                return;
            }

            AudioManager.Instance?.PlaySFX(profile.IdentitySound);
        }
    }
}
