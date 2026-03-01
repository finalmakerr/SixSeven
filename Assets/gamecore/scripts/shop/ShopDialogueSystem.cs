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
