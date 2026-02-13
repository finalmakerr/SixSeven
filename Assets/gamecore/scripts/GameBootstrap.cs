using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace GameCore
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Core Assets")]
        [SerializeField] private AssetReferenceT<LevelDatabase> levelDatabaseRef;
        [SerializeField] private AssetReferenceT<SpriteAtlas> tilesAtlasRef;

        public static LevelDatabase LevelDatabase { get; private set; }
        public static SpriteAtlas TilesAtlas { get; private set; }

        private async void Start()
        {
            await InitializeGame();
        }

        private async Task InitializeGame()
        {
            await Addressables.InitializeAsync().Task;

            LevelDatabase = await levelDatabaseRef.LoadAssetAsync().Task;
            TilesAtlas = await tilesAtlasRef.LoadAssetAsync().Task;

            SceneManager.LoadScene("Main");
        }
    }
}
