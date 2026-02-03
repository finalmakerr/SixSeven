using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SixSevenSmokeTests
{
    [UnityTest]
    public IEnumerator LoadsScenes_AndSpawnsBoardWithUI()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;

        SceneManager.LoadScene("Game");
        yield return null;
        yield return null;

        TileView[] tiles = Object.FindObjectsOfType<TileView>();
        Assert.AreEqual(49, tiles.Length);

        Text scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
        Text movesText = GameObject.Find("MovesText")?.GetComponent<Text>();

        Assert.IsNotNull(scoreText);
        Assert.IsNotNull(movesText);
    }
}
