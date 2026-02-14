using UnityEngine;

namespace GameCore
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private int startingLevelIndex;

        public int CurrentLevelIndex { get; private set; }
        public int LevelCount => levelDatabase != null ? levelDatabase.LevelCount : 0;

        private void OnEnable()
        {
            if (levelDatabase == null)
            {
                Debug.LogError("LevelDatabase not assigned. Ensure SceneAssetLoader has finished loading before enabling gameplay systems.");
            }
        }

        public void LoadStartingLevel(GameManager gameManager)
        {
            LoadLevel(gameManager, startingLevelIndex);
        }

        public bool LoadLevel(GameManager gameManager, int levelIndex)
        {
            if (gameManager == null)
            {
                return false;
            }

            CurrentLevelIndex = levelIndex;
            gameManager.LoadLevel(levelIndex);
            return true;
        }

        public bool LoadNextLevel(GameManager gameManager)
        {
            if (levelDatabase != null && levelDatabase.LevelCount > 0)
            {
                if (CurrentLevelIndex >= levelDatabase.LevelCount - 1)
                {
                    return false;
                }
            }

            return LoadLevel(gameManager, CurrentLevelIndex + 1);
        }
    }
}
