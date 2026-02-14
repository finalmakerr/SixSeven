using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace GameCore
{
    public class GameBootstrap : MonoBehaviour
    {
        private async void Start()
        {
            await InitializeGame();
        }

        private async Task InitializeGame()
        {
            await Addressables.InitializeAsync().Task;
            SceneManager.LoadScene("Main");
        }
    }
}
