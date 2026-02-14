using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "SceneAssetGroup", menuName = "SixSeven/Scene Asset Group")]
public class SceneAssetGroup : ScriptableObject
{
    [SerializeField] private List<AssetReference> assetReferences = new();

    public IReadOnlyList<AssetReference> AssetReferences => assetReferences;
}
