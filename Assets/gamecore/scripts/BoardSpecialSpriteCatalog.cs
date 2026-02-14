using UnityEngine;

namespace GameCore
{
    [CreateAssetMenu(fileName = "BoardSpecialSpriteCatalog", menuName = "SixSeven/Board Special Sprite Catalog")]
    public sealed class BoardSpecialSpriteCatalog : ScriptableObject
    {
        [SerializeField] private Sprite bomb4Sprite;
        [SerializeField] private Sprite bomb7Sprite;
        [SerializeField] private Sprite bombXXSprite;
        [SerializeField] private Sprite bomb6SpriteA;
        [SerializeField] private Sprite bomb6SpriteB;
        [SerializeField] private Sprite treasureChestSprite;

        public Sprite Bomb4Sprite => bomb4Sprite;
        public Sprite Bomb7Sprite => bomb7Sprite;
        public Sprite BombXXSprite => bombXXSprite;
        public Sprite Bomb6SpriteA => bomb6SpriteA;
        public Sprite Bomb6SpriteB => bomb6SpriteB;
        public Sprite TreasureChestSprite => treasureChestSprite;
    }
}
