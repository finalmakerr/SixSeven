using UnityEngine;

namespace GameCore
{
    public class LevelManager : MonoBehaviour
    {
        private LevelDatabase levelDatabase;
        [SerializeField] private int startingLevelIndex;

        public int CurrentLevelIndex { get; private set; }
        public int LevelCount => levelDatabase != null ? levelDatabase.LevelCount : 0;

        private void Start()
        {
            var loader = FindObjectOfType<SceneAssetLoader>();
            if (loader == null)
            {
                Debug.LogWarning("SceneAssetLoader not found in scene; cannot load LevelDatabase yet.");
                return;
            }

            if (!loader.IsLoaded)
            {
                Debug.LogWarning("SceneAssetLoader has not finished loading assets; cannot load LevelDatabase yet.");
                return;
            }

            levelDatabase = loader.GetLoadedAsset<LevelDatabase>();

            if (levelDatabase == null)
            {
                Debug.LogWarning("LevelDatabase not found in SceneAssetGroup.");
                return;
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
