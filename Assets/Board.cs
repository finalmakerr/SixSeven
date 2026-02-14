using UnityEngine;

public class Board : MonoBehaviour
{
    private const int FixedGridSize = 7;

    [Header("Grid")]
    [SerializeField] private float baseTileSpacing = 1f;
    [SerializeField] private Tile tilePrefab;

    [Header("Responsive Layout")]
    [SerializeField, Range(0f, 0.45f)] private float topUiReservedViewport = 0.16f;
    [SerializeField, Range(0f, 0.45f)] private float bottomSlotsReservedViewport = 0.2f;
    [SerializeField, Range(0f, 0.2f)] private float horizontalViewportPadding = 0.04f;
    [SerializeField] private Camera layoutCamera;

    [Header("Player Tile")]
    [SerializeField] private Vector2Int playerGridPosition = new Vector2Int(3, 3);

    private Sprite[] sprites;
    private TileSpriteCatalog tileSpriteCatalog;
    private Tile[,] tiles;
    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private void Awake()
    {
        var loader = FindObjectOfType<SceneAssetLoader>();
        if (loader != null)
        {
            tileSpriteCatalog = loader.GetLoadedAsset<TileSpriteCatalog>();
        }

        if (tileSpriteCatalog != null && tileSpriteCatalog.Sprites != null && tileSpriteCatalog.Sprites.Count > 0)
        {
            sprites = new Sprite[tileSpriteCatalog.Sprites.Count];
            for (var i = 0; i < tileSpriteCatalog.Sprites.Count; i++)
            {
                sprites[i] = tileSpriteCatalog.Sprites[i];
            }
        }
        else
        {
            Debug.LogWarning("TileSpriteCatalog missing from SceneAssetLoader; using generated fallback sprites.");
            sprites = CreateFallbackSprites();
        }

        if (layoutCamera == null)
            layoutCamera = Camera.main;
    }

    private void Start()
    {
        Generate();
        ApplyResponsiveLayout(forceUpdate: true);
        RefreshPlayerTileHighlight();
    }

    private void LateUpdate()
    {
        ApplyResponsiveLayout(forceUpdate: false);
    }

    public void SetPlayerGridPosition(Vector2Int gridPosition)
    {
        playerGridPosition = gridPosition;
        RefreshPlayerTileHighlight();
    }

    private void Generate()
    {
        tiles = new Tile[FixedGridSize, FixedGridSize];

        float startX = -(FixedGridSize - 1) * baseTileSpacing * 0.5f;
        float startY = -(FixedGridSize - 1) * baseTileSpacing * 0.5f;

        for (int y = 0; y < FixedGridSize; y++)
        {
            for (int x = 0; x < FixedGridSize; x++)
            {
                Vector3 pos = new Vector3(startX + x * baseTileSpacing, startY + y * baseTileSpacing, 0f);
                Tile tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                tile.SetSprite(sprite);
                tile.SetGridPosition(x, y);

                tiles[x, y] = tile;
            }
        }
    }


    private static Sprite[] CreateFallbackSprites()
    {
        var palette = new[]
        {
            new Color(0.9f, 0.2f, 0.2f),
            new Color(0.2f, 0.6f, 0.9f),
            new Color(0.2f, 0.8f, 0.4f),
            new Color(0.9f, 0.8f, 0.2f),
            new Color(0.7f, 0.3f, 0.9f),
            new Color(0.9f, 0.5f, 0.2f)
        };

        var fallbackSprites = new Sprite[palette.Length];
        for (var i = 0; i < palette.Length; i++)
        {
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (var pIndex = 0; pIndex < pixels.Length; pIndex++)
            {
                pixels[pIndex] = palette[i];
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            fallbackSprites[i] = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        return fallbackSprites;
    }

    private void ApplyResponsiveLayout(bool forceUpdate)
    {
        if (layoutCamera == null || !layoutCamera.orthographic)
            return;

        if (!forceUpdate && Screen.width == cachedScreenWidth && Screen.height == cachedScreenHeight)
            return;

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        float viewportWidth = Mathf.Max(0.1f, 1f - (horizontalViewportPadding * 2f));
        float viewportHeight = Mathf.Max(0.1f, 1f - topUiReservedViewport - bottomSlotsReservedViewport);

        float worldHeight = layoutCamera.orthographicSize * 2f;
        float worldWidth = worldHeight * layoutCamera.aspect;

        float availableWorldWidth = worldWidth * viewportWidth;
        float availableWorldHeight = worldHeight * viewportHeight;

        float targetTileSize = Mathf.Min(availableWorldWidth / FixedGridSize, availableWorldHeight / FixedGridSize);
        float targetScale = targetTileSize / baseTileSpacing;

        transform.localScale = Vector3.one * targetScale;

        float boardCenterY = (-worldHeight * 0.5f + (worldHeight * bottomSlotsReservedViewport)) + (availableWorldHeight * 0.5f);
        transform.position = new Vector3(0f, boardCenterY, transform.position.z);

        // Keep camera centered on horizontal grid focus, preserving top/bottom UI breathing room.
        layoutCamera.transform.position = new Vector3(0f, 0f, layoutCamera.transform.position.z);

    }

    private void RefreshPlayerTileHighlight()
    {
        if (tiles == null)
            return;

        for (int y = 0; y < FixedGridSize; y++)
        {
            for (int x = 0; x < FixedGridSize; x++)
            {
                bool isPlayerTile = x == playerGridPosition.x && y == playerGridPosition.y;
                tiles[x, y]?.SetPlayerHighlight(isPlayerTile);
            }
        }
    }
}
