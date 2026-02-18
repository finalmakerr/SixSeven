using UnityEngine;

[CreateAssetMenu(fileName = "GameOverConfig", menuName = "SixSeven/Config/GameOverConfig")]
public sealed class GameOverConfig : ScriptableObject
{
    [Header("Death Rules")]
    public bool consumeOneUpOnDeath = true;
    public bool allowHardcoreMode;

    [Header("Resurrection")]
    public string resurrectionVideoPath;
    public bool shopForceOfferOneUp = true;
    public float fadeToBlackDuration = 0.4f;
    public float resurrectionVideoDuration = 2.5f;

}
