using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "SceneAssetGroup", menuName = "SixSeven/Scene Asset Group")]
public class SceneAssetGroup : ScriptableObject
{
    [SerializeField] private List<AssetReference> assetReferences = new();
    [SerializeField] private AssetReferenceT<AudioAssetCatalog> audioAssetCatalog;

    public IReadOnlyList<AssetReference> AssetReferences
    {
        get
        {
            var merged = new List<AssetReference>(assetReferences);
            if (audioAssetCatalog != null && !merged.Contains(audioAssetCatalog))
            {
                merged.Add(audioAssetCatalog);
            }

            return merged;
        }
    }

    public AssetReferenceT<AudioAssetCatalog> AudioAssetCatalog => audioAssetCatalog;
}
