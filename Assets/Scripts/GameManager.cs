using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private int width = 7;
    [SerializeField] private int height = 7;
    [SerializeField] private float spacing = 8f;
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private TileView tilePrefab;

    [Header("UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text movesText;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private Text endScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Rules")]
    [SerializeField] private int startingMoves = 30;
    [SerializeField] private int scorePerTile = 10;
    [SerializeField] private float swapPulseScale = 0.9f;
    [SerializeField] private int shuffleSafety = 50;

    private Sprite[] sprites;
    private TileView[,] tiles;
    private TileView selected;
    private int score;
    private int moves;
    private bool isBusy;
    private bool gameOver;

    private void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private IEnumerator Start()
    {
        sprites = Resources.LoadAll<Sprite>("Tiles");
        if (sprites == null || sprites.Length == 0)
            Debug.LogError("No sprites found in Assets/Resources/Tiles");

        moves = startingMoves;
        score = 0;
        UpdateUI();

        if (endPanel != null)
            endPanel.SetActive(false);

        BuildGrid();
        if (HasAnyMatch())
            yield return ResolveMatchesCoroutine();

        if (!HasAvailableMoves())
            ShuffleBoard();
    }

    private void BuildGrid()
    {
        if (boardRoot == null || tilePrefab == null)
            return;

        tiles = new TileView[width, height];

        float boardSize = Mathf.Min(boardRoot.rect.width, boardRoot.rect.height);
        if (boardSize <= 1f)
            boardSize = 700f;

        float cellSize = (boardSize - spacing * (width - 1)) / width;
        Vector2 start = new Vector2(
            -boardSize * 0.5f + cellSize * 0.5f,
            -boardSize * 0.5f + cellSize * 0.5f
        );

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileView tile = Instantiate(tilePrefab, boardRoot);
                RectTransform rt = tile.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = start + new Vector2(x * (cellSize + spacing), y * (cellSize + spacing));

                tile.Init(this, x, y);
                tiles[x, y] = tile;

                int type = GetRandomTypeAvoidingMatch(x, y);
                SetTileType(tile, type);
            }
        }
    }

    private int GetRandomTypeAvoidingMatch(int x, int y)
    {
        if (sprites == null || sprites.Length == 0)
            return 0;

        int type = Random.Range(0, sprites.Length);
        int safety = 0;
        while (WouldMatch(x, y, type) && safety < 50)
        {
            type = Random.Range(0, sprites.Length);
            safety++;
        }
        return type;
    }

    private bool WouldMatch(int x, int y, int type)
    {
        if (x >= 2)
        {
            if (tiles[x - 1, y] != null && tiles[x - 2, y] != null)
            {
                if (tiles[x - 1, y].Type == type && tiles[x - 2, y].Type == type)
                    return true;
            }
        }
        if (y >= 2)
        {
            if (tiles[x, y - 1] != null && tiles[x, y - 2] != null)
            {
                if (tiles[x, y - 1].Type == type && tiles[x, y - 2].Type == type)
                    return true;
            }
        }
        return false;
    }

    public void OnTileClicked(TileView tile)
    {
        if (isBusy || gameOver || tile == null)
            return;

        if (selected == null)
        {
            Select(tile);
            return;
        }

        if (selected == tile)
        {
            Deselect();
            return;
        }

        if (!AreAdjacent(selected, tile))
        {
            Deselect();
            Select(tile);
            return;
        }

        StartCoroutine(HandleSwap(selected, tile));
    }

    public void OnTileDragged(TileView tile, Vector2 direction)
    {
        if (isBusy || gameOver || tile == null)
            return;

        TileView neighbor = GetNeighbor(tile, direction);
        if (neighbor == null)
            return;

        StartCoroutine(HandleSwap(tile, neighbor));
    }

    private void Select(TileView tile)
    {
        selected = tile;
        selected.SetSelected(true);
    }

    private void Deselect()
    {
        if (selected != null)
            selected.SetSelected(false);
        selected = null;
    }

    private bool AreAdjacent(TileView a, TileView b)
    {
        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);
        return (dx + dy) == 1;
    }

    private IEnumerator HandleSwap(TileView a, TileView b)
    {
        isBusy = true;
        Deselect();

        moves--;
        UpdateUI();

        yield return AnimateSwapPulse(a, b);
        SwapTypes(a, b);

        if (HasAnyMatch())
        {
            yield return ResolveMatchesCoroutine();
        }
        else
        {
            SwapTypes(a, b);
        }

        if (moves <= 0 && !gameOver)
            EndGame();

        isBusy = false;
    }

    private void SwapTypes(TileView a, TileView b)
    {
        int temp = a.Type;
        SetTileType(a, b.Type);
        SetTileType(b, temp);
    }

    private void SetTileType(TileView tile, int type)
    {
        tile.Type = type;
        if (type >= 0 && sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(type, 0, sprites.Length - 1);
            tile.SetSprite(sprites[index]);
        }
        else
        {
            tile.SetSprite(null);
        }
    }

    private bool HasAnyMatch()
    {
        bool[,] matches = FindMatches();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (matches[x, y])
                    return true;
        return false;
    }

    private void ResolveAllMatches()
    {
        StartCoroutine(ResolveMatchesCoroutine());
    }

    private IEnumerator ResolveMatchesCoroutine()
    {
        while (true)
        {
            int cleared = ClearMatches();
            if (cleared == 0)
                break;

            DropAndFill();
            UpdateUI();
            yield return null;
        }

        if (!HasAvailableMoves())
            ShuffleBoard();
    }

    private int ClearMatches()
    {
        bool[,] matches = FindMatches();
        int cleared = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!matches[x, y])
                    continue;

                TileView tile = tiles[x, y];
                if (tile != null && tile.Type >= 0)
                {
                    SetTileType(tile, -1);
                    cleared++;
                }
            }
        }

        if (cleared > 0)
        {
            score += cleared * scorePerTile;
            UpdateUI();
        }

        return cleared;
    }

    private void DropAndFill()
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < height; y++)
            {
                TileView tile = tiles[x, y];
                if (tile.Type >= 0)
                {
                    if (writeY != y)
                    {
                        TileView target = tiles[x, writeY];
                        SetTileType(target, tile.Type);
                        SetTileType(tile, -1);
                    }
                    writeY++;
                }
            }

            for (int y = writeY; y < height; y++)
            {
                TileView tile = tiles[x, y];
                int type = Random.Range(0, sprites.Length);
                SetTileType(tile, type);
            }
        }
    }

    private bool[,] FindMatches()
    {
        bool[,] matches = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            int runLength = 1;
            for (int x = 1; x < width; x++)
            {
                if (tiles[x, y].Type >= 0 && tiles[x, y].Type == tiles[x - 1, y].Type)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 3)
                    {
                        for (int i = 0; i < runLength; i++)
                            matches[x - 1 - i, y] = true;
                    }
                    runLength = 1;
                }
            }
            if (runLength >= 3)
            {
                for (int i = 0; i < runLength; i++)
                    matches[width - 1 - i, y] = true;
            }
        }

        for (int x = 0; x < width; x++)
        {
            int runLength = 1;
            for (int y = 1; y < height; y++)
            {
                if (tiles[x, y].Type >= 0 && tiles[x, y].Type == tiles[x, y - 1].Type)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 3)
                    {
                        for (int i = 0; i < runLength; i++)
                            matches[x, y - 1 - i] = true;
                    }
                    runLength = 1;
                }
            }
            if (runLength >= 3)
            {
                for (int i = 0; i < runLength; i++)
                    matches[x, height - 1 - i] = true;
            }
        }

        return matches;
    }

    private bool HasAvailableMoves()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileView tile = tiles[x, y];
                if (tile == null || tile.Type < 0)
                    continue;

                if (x + 1 < width && WouldSwapCreateMatch(tile, tiles[x + 1, y]))
                    return true;
                if (y + 1 < height && WouldSwapCreateMatch(tile, tiles[x, y + 1]))
                    return true;
            }
        }

        return false;
    }

    private bool WouldSwapCreateMatch(TileView a, TileView b)
    {
        if (a == null || b == null)
            return false;

        SwapTypes(a, b);
        bool hasMatch = HasMatchAt(a.X, a.Y) || HasMatchAt(b.X, b.Y);
        SwapTypes(a, b);
        return hasMatch;
    }

    private bool HasMatchAt(int x, int y)
    {
        int type = tiles[x, y].Type;
        if (type < 0)
            return false;

        int count = 1;
        for (int i = x - 1; i >= 0 && tiles[i, y].Type == type; i--)
            count++;
        for (int i = x + 1; i < width && tiles[i, y].Type == type; i++)
            count++;
        if (count >= 3)
            return true;

        count = 1;
        for (int i = y - 1; i >= 0 && tiles[x, i].Type == type; i--)
            count++;
        for (int i = y + 1; i < height && tiles[x, i].Type == type; i++)
            count++;
        return count >= 3;
    }

    private void ShuffleBoard()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        Deselect();

        int attempts = 0;
        do
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int type = GetRandomTypeAvoidingMatch(x, y);
                    SetTileType(tiles[x, y], type);
                }
            }
            attempts++;
        } while (!HasAvailableMoves() && attempts < shuffleSafety);
    }

    private IEnumerator AnimateSwapPulse(TileView a, TileView b)
    {
        if (swapPulseScale <= 0f)
            yield break;

        a.SetScale(swapPulseScale);
        b.SetScale(swapPulseScale);
        yield return null;
        a.SetScale(1f);
        b.SetScale(1f);
    }

    private TileView GetNeighbor(TileView tile, Vector2 direction)
    {
        int targetX = tile.X + Mathf.RoundToInt(direction.x);
        int targetY = tile.Y + Mathf.RoundToInt(direction.y);

        if (targetX < 0 || targetX >= width || targetY < 0 || targetY >= height)
            return null;

        return tiles[targetX, targetY];
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
        if (movesText != null)
            movesText.text = $"Moves: {moves}";
        if (endScoreText != null)
            endScoreText.text = $"Final Score: {score}";
    }

    private void EndGame()
    {
        gameOver = true;
        UpdateUI();
        if (endPanel != null)
            endPanel.SetActive(true);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene("Game");
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
