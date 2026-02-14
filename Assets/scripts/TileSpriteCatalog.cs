using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileSpriteCatalog", menuName = "SixSeven/Tile Sprite Catalog")]
public class TileSpriteCatalog : ScriptableObject
{
    [SerializeField] private List<Sprite> sprites = new();

    public IReadOnlyList<Sprite> Sprites => sprites;
}
