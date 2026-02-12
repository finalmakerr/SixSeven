using System;
using UnityEngine;

namespace GameCore
{
    // CODEX BONUS PR5
    public abstract class BonusMiniGameBase : MonoBehaviour
    {
        public event Action<bool> Completed;

        public abstract string GameName { get; }

        public abstract void Begin(Transform uiParent, System.Random randomSeed);

        public virtual void StopGame()
        {
        }

        protected void Complete(bool success)
        {
            Completed?.Invoke(success);
        }
    }
}
