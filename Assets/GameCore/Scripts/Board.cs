using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class Board : MonoBehaviour
    {
        // CODEX: LEVEL_LOOP
        public event Action<int, int, IReadOnlyList<int>> MatchesCleared;
        public event Action ValidSwap;
        public event Action TurnEnded;
        // CODEX BOSS PR2
        public event Action<Vector2Int> OnBombDetonated;
        // CODEX CHEST PR2
        public event Action<Piece, DestructionReason> OnPieceDestroyed;

        [Header("Board Settings")]
        [SerializeField] private int width = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private float spacing = 1.1f;
        [SerializeField] private int colorCount = 5;
        [SerializeField] private float refillDelay = 0.1f;
        // CODEX BOSS PR2
        [SerializeField] private bool bombOnlyOnBossLevels;
        // CODEX: RNG_BAG
        [Header("Randomness")]
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private List<float> tileWeights = new List<float>();

        [Header("References")]
        [SerializeField] private Piece piecePrefab;
        [SerializeField] private bool debugMode; // CODEX VERIFY: toggle lightweight stability instrumentation.
        [SerializeField] private int maxSpawnAttempts = 6; // CODEX VERIFY: cap retry attempts for spawn/refill.
        [SerializeField] private int maxShuffleAttempts = 10; // CODEX VERIFY: cap shuffle retries for dead boards.
        // CODEX CHEST PR1
        [SerializeField] [Range(1, 10)] private int chestSpawnChancePercent = 5; // CODEX CHEST PR1
        [SerializeField] private int chestCooldownMovesRemaining; // CODEX CHEST PR1
        [SerializeField] private bool chestPresent; // CODEX CHEST PR1

        private Piece[,] pieces;
        private Sprite[] sprites;
        private bool isBusy;
        // CODEX BOSS PR2
        private bool externalInputLock;
        private bool hasInitialized;
        // CODEX: RNG_BAG
        private readonly List<int> colorBag = new List<int>();
        private System.Random randomGenerator;

        private readonly List<Piece> matchBuffer = new List<Piece>();
        private readonly List<int> matchRunLengths = new List<int>();
        private readonly HashSet<Vector2Int> specialCreationLogged = new HashSet<Vector2Int>(); // CODEX VERIFY 2: track special creation logs once per run.
        private readonly HashSet<Vector2Int> specialActivationLogged = new HashSet<Vector2Int>(); // CODEX VERIFY 2: prevent double activation logs per resolve step.
        // CODEX BOSS PR2
        private readonly HashSet<Vector2Int> bombCreationPositions = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> bombDetonationsThisStep = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> specialRecipeBombPositions = new HashSet<Vector2Int>(); // CODEX BOMB PR3: prevent double recipe bombs per swap.
        private readonly HashSet<Vector2Int> pendingSpecialClearPositions = new HashSet<Vector2Int>(); // CODEX BOMB TIERS: force clears for bomb mixes.
        private int moveId; // CODEX VERIFY 2: monotonic id for accepted swaps.
        private int activeMoveId; // CODEX VERIFY 2: current move id for resolve diagnostics.
        // CODEX CHEST PR1
        private Sprite treasureChestSprite;

        // CODEX BOMB TIERS: sprite cache for bomb tiers (Resources.Load paths).
        private Sprite bomb4Sprite;
        private Sprite bomb7Sprite;
        private Sprite bombXXSprite;
        private Sprite bomb6SpriteA;
        private Sprite bomb6SpriteB;

        public bool IsBusy => isBusy || externalInputLock; // CODEX VERIFY: input lock gate for stable board state.
        // CODEX BOSS PR1
        public int RandomSeed => randomSeed;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            sprites = GenerateSprites();
            LoadBombSprites(); // CODEX BOMB TIERS: cache special bomb sprites.
            LoadTreasureChestSprite(); // CODEX CHEST PR1
        }

        private void Start()
        {
            if (!hasInitialized)
            {
                InitializeBoard(width, height);
            }
        }

        // CODEX BOSS PR2
        public void SetExternalInputLock(bool locked)
        {
            externalInputLock = locked;
        }

        public void InitializeBoard(int newWidth, int newHeight)
        {
            if (!enabled)
            {
                return;
            }

            width = Mathf.Max(3, newWidth);
            height = Mathf.Max(3, newHeight);
            hasInitialized = true;
            ResetBoardState();
        }

        // CODEX DIFFICULTY PR7
        public void ConfigureColorCount(int newColorCount)
        {
            if (newColorCount <= 0)
            {
                return;
            }

            colorCount = newColorCount;
            sprites = GenerateSprites();
        }

        private void ResetBoardState()
        {
            StopAllCoroutines();
            isBusy = true; // CODEX VERIFY: lock input while board initializes.
            activeMoveId = 0; // CODEX VERIFY 2: reset move diagnostics for initialization clears.
            moveId = 0; // CODEX VERIFY 2: reset move id for new board sessions.
            ClearExistingPieces();
            pieces = new Piece[width, height];
            // CODEX: RNG_BAG
            ResetRandomGenerator();
            colorBag.Clear();
            pendingSpecialClearPositions.Clear(); // CODEX BOMB TIERS: reset pending bomb clears on new board.
            chestCooldownMovesRemaining = 0; // CODEX CHEST PR1
            chestPresent = false; // CODEX CHEST PR1
            CreateBoard();
            StartCoroutine(ClearMatchesRoutine());
        }

        private void ClearExistingPieces()
        {
            if (pieces == null)
            {
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }

                return;
            }

            for (var x = 0; x < pieces.GetLength(0); x++)
            {
                for (var y = 0; y < pieces.GetLength(1); y++)
                {
                    var piece = pieces[x, y];
                    if (piece != null)
                    {
                        Destroy(piece.gameObject);
                    }
                }
            }

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateBoard()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    CreatePiece(x, y, GetRandomColorIndexAvoidingMatch(x, y));
                }
            }
        }

        private int GetRandomColorIndex()
        {
            // CODEX: RNG_BAG
            EnsureColorBag();
            var bagIndex = RandomRange(0, colorBag.Count);
            var colorIndex = colorBag[bagIndex];
            colorBag.RemoveAt(bagIndex);
            return colorIndex;
        }

        private int GetRandomColorIndexAvoidingMatch(int x, int y)
        {
            // CODEX VERIFY: avoid spawning immediate matches with a capped retry.
            EnsureColorBag();
            var attempts = Mathf.Max(1, maxSpawnAttempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var bagIndex = RandomRange(0, colorBag.Count);
                var colorIndex = colorBag[bagIndex];
                if (!WouldFormMatch(x, y, colorIndex))
                {
                    colorBag.RemoveAt(bagIndex);
                    return colorIndex;
                }
            }

            // CODEX VERIFY: fallback to prevent infinite loops if all colors match.
            return GetRandomColorIndex();
        }

        // CODEX: RNG_BAG
        private void ResetRandomGenerator()
        {
            randomGenerator = randomSeed > 0 ? new System.Random(randomSeed) : new System.Random();
        }

        // CODEX: RNG_BAG
        private void EnsureColorBag()
        {
            if (colorBag.Count > 0)
            {
                return;
            }

            for (var i = 0; i < colorCount; i++)
            {
                var weight = GetTileWeight(i);
                if (weight <= 0f)
                {
                    continue;
                }

                var count = Mathf.Max(1, Mathf.RoundToInt(weight));
                for (var j = 0; j < count; j++)
                {
                    colorBag.Add(i);
                }
            }

            if (colorBag.Count == 0)
            {
                for (var i = 0; i < colorCount; i++)
                {
                    colorBag.Add(i);
                }
            }
        }

        // CODEX: RNG_BAG
        private float GetTileWeight(int colorIndex)
        {
            if (tileWeights == null || tileWeights.Count == 0)
            {
                return 1f;
            }

            if (colorIndex < 0 || colorIndex >= tileWeights.Count)
            {
                return 1f;
            }

            return tileWeights[colorIndex];
        }

        // CODEX: RNG_BAG
        private int RandomRange(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return randomGenerator.Next(minInclusive, maxExclusive);
        }

        private Piece CreatePiece(int x, int y, int colorIndex)
        {
            if (piecePrefab == null || sprites == null || sprites.Length == 0)
            {
                return null;
            }

            var worldPosition = GridToWorld(x, y);
            var piece = Instantiate(piecePrefab, worldPosition, Quaternion.identity, transform);
            piece.Initialize(x, y, colorIndex, sprites[colorIndex]);
            pieces[x, y] = piece;
            return piece;
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(x * spacing, y * spacing, 0f) + transform.position;
        }

        public bool TrySwap(Piece first, Vector2Int direction)
        {
            if (isBusy || first == null || pieces == null)
            {
                return false;
            }

            var targetX = first.X + direction.x;
            var targetY = first.Y + direction.y;

            if (!IsInBounds(targetX, targetY))
            {
                return false;
            }

            var second = pieces[targetX, targetY];
            if (second == null)
            {
                return false;
            }

            if (!IsSwapValid(first, second))
            {
                return false;
            }

            StartCoroutine(SwapRoutine(first, second));
            return true;
        }

        public bool CanJetpackSwap(Piece playerPiece, Vector2Int direction)
        {
            if (isBusy || playerPiece == null || pieces == null)
            {
                return false;
            }

            if (!playerPiece.IsPlayer)
            {
                return false;
            }

            var targetX = playerPiece.X + direction.x;
            var targetY = playerPiece.Y + direction.y;

            if (!IsInBounds(targetX, targetY))
            {
                return false;
            }

            var target = pieces[targetX, targetY];
            if (target == null)
            {
                return false;
            }

            return IsSwappable(target);
        }

        public bool CanJetpackDouble(Piece playerPiece, Vector2Int direction)
        {
            if (isBusy || playerPiece == null || pieces == null)
            {
                return false;
            }

            if (!playerPiece.IsPlayer)
            {
                return false;
            }

            if (direction != Vector2Int.up)
            {
                return false;
            }

            var middleY = playerPiece.Y + 1;
            var targetY = playerPiece.Y + 2;
            if (!IsInBounds(playerPiece.X, middleY) || !IsInBounds(playerPiece.X, targetY))
            {
                return false;
            }

            var middle = pieces[playerPiece.X, middleY];
            var target = pieces[playerPiece.X, targetY];
            if (middle == null || target == null)
            {
                return false;
            }

            return IsSwappable(middle) && IsSwappable(target);
        }

        public bool TryJetpackSwap(Piece playerPiece, Vector2Int direction)
        {
            if (!CanJetpackSwap(playerPiece, direction))
            {
                return false;
            }

            var targetX = playerPiece.X + direction.x;
            var targetY = playerPiece.Y + direction.y;
            var target = pieces[targetX, targetY];
            if (target == null)
            {
                return false;
            }

            StartCoroutine(JetpackSwapRoutine(playerPiece, target));
            return true;
        }

        public bool TryJetpackDouble(Piece playerPiece, Vector2Int direction)
        {
            if (!CanJetpackDouble(playerPiece, direction))
            {
                return false;
            }

            var targetY = playerPiece.Y + 2;
            var target = pieces[playerPiece.X, targetY];
            if (target == null)
            {
                return false;
            }

            StartCoroutine(JetpackMoveRoutine(playerPiece, target));
            return true;
        }

        public bool TryMovePlayerUp()
        {
            if (pieces == null)
            {
                return false;
            }

            if (!TryGetPlayerPosition(out var playerPosition))
            {
                return false;
            }

            var targetY = playerPosition.y + 1;
            if (!IsInBounds(playerPosition.x, targetY))
            {
                return false;
            }

            var playerPiece = pieces[playerPosition.x, playerPosition.y];
            var targetPiece = pieces[playerPosition.x, targetY];
            if (playerPiece == null || targetPiece == null)
            {
                return false;
            }

            if (!IsSwappable(targetPiece))
            {
                return false;
            }

            SwapPieces(playerPiece, targetPiece);
            return true;
        }

        public bool CanMovePlayerUp()
        {
            if (pieces == null)
            {
                return false;
            }

            if (!TryGetPlayerPosition(out var playerPosition))
            {
                return false;
            }

            var targetY = playerPosition.y + 1;
            if (!IsInBounds(playerPosition.x, targetY))
            {
                return false;
            }

            var playerPiece = pieces[playerPosition.x, playerPosition.y];
            var targetPiece = pieces[playerPosition.x, targetY];
            if (playerPiece == null || targetPiece == null)
            {
                return false;
            }

            return IsSwappable(targetPiece);
        }

        public bool IsSwapValid(Piece first, Piece second)
        {
            if (first == null || second == null || pieces == null)
            {
                return false;
            }

            if (!IsSwappable(first) || !IsSwappable(second))
            {
                return false;
            }

            if (Mathf.Abs(first.X - second.X) + Mathf.Abs(first.Y - second.Y) != 1)
            {
                return false;
            }

            SwapPiecesInGrid(first, second);
            var hasMatch = HasMatchAt(first.X, first.Y) || HasMatchAt(second.X, second.Y);
            SwapPiecesInGrid(first, second);
            return hasMatch || IsSpecialRecipePair(first.SpecialType, second.SpecialType) || IsBombMixPair(first, second);
        }

        public bool TryGetPlayerPosition(out Vector2Int position)
        {
            position = default;
            if (pieces == null)
            {
                return false;
            }

            for (var x = 0; x < pieces.GetLength(0); x++)
            {
                for (var y = 0; y < pieces.GetLength(1); y++)
                {
                    var piece = pieces[x, y];
                    if (piece != null && piece.IsPlayer)
                    {
                        position = new Vector2Int(x, y);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetPieceAt(Vector2Int position, out Piece piece)
        {
            piece = null;
            if (pieces == null || !IsInBounds(position.x, position.y))
            {
                return false;
            }

            piece = pieces[position.x, position.y];
            return piece != null;
        }

        public bool TryDestroyPieceAt(Vector2Int position, DestructionReason reason)
        {
            if (pieces == null || !IsInBounds(position.x, position.y))
            {
                return false;
            }

            var piece = pieces[position.x, position.y];
            if (piece == null || piece.IsPlayer)
            {
                return false;
            }

            RemovePiece(piece, reason);
            return true;
        }

        public bool IsPlayerAdjacentToBomb()
        {
            if (!TryGetPlayerPosition(out var playerPosition))
            {
                return false;
            }

            return IsBombAt(playerPosition.x + 1, playerPosition.y)
                || IsBombAt(playerPosition.x - 1, playerPosition.y)
                || IsBombAt(playerPosition.x, playerPosition.y + 1)
                || IsBombAt(playerPosition.x, playerPosition.y - 1);
        }

        private bool IsBombAt(int x, int y)
        {
            if (!IsInBounds(x, y) || pieces == null)
            {
                return false;
            }

            var piece = pieces[x, y];
            return piece != null && piece.SpecialType == SpecialType.Bomb;
        }

        private IEnumerator SwapRoutine(Piece first, Piece second)
        {
            isBusy = true;
            SwapPieces(first, second);

            yield return new WaitForSeconds(0.05f);

            activeMoveId = moveId + 1; // CODEX VERIFY 2: stage upcoming move id for match diagnostics.
            specialRecipeBombPositions.Clear(); // CODEX BOMB PR3: reset per swap to avoid double-trigger.
            var specialRecipeApplied = TryApplySpecialRecipe(first, second);
            var matches = FindMatches();
            if (matches.Count == 0 && !specialRecipeApplied)
            {
                SwapPieces(first, second);
                activeMoveId = moveId; // CODEX VERIFY 2: restore current move id on invalid swaps.
                isBusy = false;
                yield break;
            }

            // CODEX: LEVEL_LOOP
            moveId = activeMoveId; // CODEX VERIFY 2: commit staged move id for accepted swaps.
            if (debugMode)
            {
                Debug.Log($"MoveStart({activeMoveId}): ({first.X},{first.Y}) -> ({second.X},{second.Y})", this); // CODEX VERIFY 2: move start log once per accepted swap.
            }
            ValidSwap?.Invoke();
            HandleTreasureChestSwap(); // CODEX CHEST PR1
            yield return StartCoroutine(ClearMatchesRoutine());
            if (debugMode)
            {
                Debug.Log($"MoveEnd({activeMoveId}): resolve complete.", this); // CODEX VERIFY 2: move end log once per accepted swap.
            }
            TurnEnded?.Invoke();
            isBusy = false;
        }

        private IEnumerator JetpackSwapRoutine(Piece first, Piece second)
        {
            yield return JetpackMoveRoutine(first, second);
        }

        private IEnumerator JetpackMoveRoutine(Piece first, Piece second)
        {
            isBusy = true;
            SwapPieces(first, second);

            yield return new WaitForSeconds(0.05f);

            activeMoveId = moveId + 1; // CODEX VERIFY 2: stage upcoming move id for match diagnostics.
            specialRecipeBombPositions.Clear(); // CODEX BOMB PR3: reset per swap to avoid double-trigger.
            TryApplySpecialRecipe(first, second);

            moveId = activeMoveId; // CODEX VERIFY 2: commit staged move id for accepted swaps.
            if (debugMode)
            {
                Debug.Log($"MoveStart({activeMoveId}): ({first.X},{first.Y}) -> ({second.X},{second.Y})", this); // CODEX VERIFY 2: move start log once per accepted swap.
            }
            ValidSwap?.Invoke();
            HandleTreasureChestSwap(); // CODEX CHEST PR1
            yield return StartCoroutine(ClearMatchesRoutine());
            if (debugMode)
            {
                Debug.Log($"MoveEnd({activeMoveId}): resolve complete.", this); // CODEX VERIFY 2: move end log once per accepted swap.
            }
            TurnEnded?.Invoke();
            isBusy = false;
        }

        private void SwapPieces(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
                return;
            }

            if (!IsInBounds(first.X, first.Y) || !IsInBounds(second.X, second.Y))
            {
                if (debugMode)
                {
                    Debug.LogWarning("Swap aborted due to out-of-bounds piece coordinates.", this);
                }
                return;
            }

            pieces[first.X, first.Y] = second;
            pieces[second.X, second.Y] = first;

            var firstX = first.X;
            var firstY = first.Y;

            first.SetPosition(second.X, second.Y, GridToWorld(second.X, second.Y));
            second.SetPosition(firstX, firstY, GridToWorld(firstX, firstY));
        }

        private void SwapPiecesInGrid(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
                return;
            }

            if (!IsInBounds(first.X, first.Y) || !IsInBounds(second.X, second.Y))
            {
                if (debugMode)
                {
                    Debug.LogWarning("Swap grid aborted due to out-of-bounds piece coordinates.", this);
                }
                return;
            }

            pieces[first.X, first.Y] = second;
            pieces[second.X, second.Y] = first;

            var firstX = first.X;
            var firstY = first.Y;

            first.SetPosition(second.X, second.Y, first.transform.position);
            second.SetPosition(firstX, firstY, second.transform.position);
        }

        // CODEX CHEST PR1
        private void HandleTreasureChestSwap()
        {
            if (chestCooldownMovesRemaining > 0)
            {
                chestCooldownMovesRemaining--;
            }

            if (chestPresent || chestCooldownMovesRemaining > 0)
            {
                return;
            }

            var clampedChance = Mathf.Clamp(chestSpawnChancePercent, 1, 10);
            var roll = RandomRange(1, 101);
            if (debugMode) // CODEX CHEST PR1
            {
                Debug.Log($"ChestSpawnRoll chance={clampedChance} result={roll}", this); // CODEX CHEST PR1
            }
            if (roll > clampedChance)
            {
                return;
            }

            SpawnTreasureChest();
        }

        // CODEX CHEST PR1
        private void SpawnTreasureChest()
        {
            if (pieces == null)
            {
                return;
            }

            var candidates = new List<Vector2Int>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (pieces[x, y] != null)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var chosenIndex = RandomRange(0, candidates.Count);
            var chosenPosition = candidates[chosenIndex];
            var piece = pieces[chosenPosition.x, chosenPosition.y];
            if (piece == null)
            {
                return;
            }

            if (treasureChestSprite == null) // CODEX CHEST PR1
            {
                LoadTreasureChestSprite(); // CODEX CHEST PR1
            }

            piece.SetSpecialType(SpecialType.TreasureChest);
            if (treasureChestSprite == null) // CODEX CHEST PR1
            {
                if (debugMode) // CODEX CHEST PR1
                {
                    Debug.LogWarning("Treasure chest sprite missing at Resources path 'Tiles/Specials/Chest'.", this); // CODEX CHEST PR1
                }
                piece.SetTreasureChestVisual(null, false); // CODEX CHEST PR1
            }
            else // CODEX CHEST PR1
            {
                piece.SetTreasureChestVisual(treasureChestSprite, debugMode); // CODEX CHEST PR1
            }
            chestPresent = true;
            if (debugMode)
            {
                Debug.Log($"ChestSpawned at ({chosenPosition.x},{chosenPosition.y})", this); // CODEX CHEST PR1
            }
        }

        private bool HasMatchAt(int x, int y)
        {
            var piece = pieces[x, y];
            if (!IsMatchable(piece))
            {
                return false;
            }

            var colorIndex = piece.ColorIndex;
            var horizontal = 1;
            var vertical = 1;

            horizontal += CountDirectionMatches(x, y, 1, 0, colorIndex);
            horizontal += CountDirectionMatches(x, y, -1, 0, colorIndex);
            if (horizontal >= 3)
            {
                return true;
            }

            vertical += CountDirectionMatches(x, y, 0, 1, colorIndex);
            vertical += CountDirectionMatches(x, y, 0, -1, colorIndex);
            return vertical >= 3;
        }

        private int CountDirectionMatches(int startX, int startY, int stepX, int stepY, int colorIndex)
        {
            var count = 0;
            var x = startX + stepX;
            var y = startY + stepY;
            while (IsInBounds(x, y))
            {
                var candidate = pieces[x, y];
                if (!IsMatchable(candidate) || candidate.ColorIndex != colorIndex)
                {
                    break;
                }

                count++;
                x += stepX;
                y += stepY;
            }

            return count;
        }

        private List<Piece> FindMatches()
        {
            matchBuffer.Clear();
            matchRunLengths.Clear();
            specialCreationLogged.Clear(); // CODEX VERIFY 2: reset special creation logs per scan.
            specialActivationLogged.Clear(); // CODEX VERIFY 2: reset special activation logs per scan.
            // CODEX BOSS PR2
            bombCreationPositions.Clear();

            // Scan horizontally for runs of 3+ matching pieces.
            for (var y = 0; y < height; y++)
            {
                var runLength = IsMatchable(pieces[0, y]) ? 1 : 0;
                for (var x = 1; x < width; x++)
                {
                    var current = pieces[x, y];
                    var previous = pieces[x - 1, y];
                    if (IsMatchable(current) && IsMatchable(previous) && current.ColorIndex == previous.ColorIndex)
                    {
                        runLength++;
                    }
                    else
                    {
                        AddRunMatches(x - 1, y, runLength, Vector2Int.right);
                        runLength = IsMatchable(current) ? 1 : 0;
                    }
                }

                AddRunMatches(width - 1, y, runLength, Vector2Int.right);
            }

            // Scan vertically for runs of 3+ matching pieces.
            for (var x = 0; x < width; x++)
            {
                var runLength = IsMatchable(pieces[x, 0]) ? 1 : 0;
                for (var y = 1; y < height; y++)
                {
                    var current = pieces[x, y];
                    var previous = pieces[x, y - 1];
                    if (IsMatchable(current) && IsMatchable(previous) && current.ColorIndex == previous.ColorIndex)
                    {
                        runLength++;
                    }
                    else
                    {
                        AddRunMatches(x, y - 1, runLength, Vector2Int.up);
                        runLength = IsMatchable(current) ? 1 : 0;
                    }
                }

                AddRunMatches(x, height - 1, runLength, Vector2Int.up);
            }

            return matchBuffer;
        }

        private void AddRunMatches(int endX, int endY, int runLength, Vector2Int direction)
        {
            if (runLength < 3)
            {
                return;
            }

            matchRunLengths.Add(runLength);

            // CODEX BOSS PR2
            Vector2Int? bombPosition = null;
            if ((runLength == 4 || runLength == 7) && ShouldAllowBombCreation())
            {
                var candidatePosition = new Vector2Int(endX, endY);
                var candidatePiece = pieces[endX, endY];
                if (candidatePiece != null
                    && candidatePiece.SpecialType == SpecialType.None
                    && bombCreationPositions.Add(candidatePosition))
                {
                    var bombTier = runLength == 4 ? 4 : 7;
                    CreateBombAt(candidatePosition, bombTier); // CODEX BOMB TIERS: match-4 and match-7 bomb creation.
                    bombPosition = candidatePosition;
                }
            }

            if (runLength >= 4 && debugMode)
            {
                var specialPosition = new Vector2Int(endX, endY); // CODEX VERIFY 2: log once per match run.
                if (specialCreationLogged.Add(specialPosition))
                {
                    Debug.Log($"SpecialCreate({activeMoveId}): ({specialPosition.x},{specialPosition.y}) length={runLength}", this); // CODEX VERIFY 2: special creation log with move id.
                }
            }

            for (var i = 0; i < runLength; i++)
            {
                var x = endX - direction.x * i;
                var y = endY - direction.y * i;
                if (bombPosition.HasValue && bombPosition.Value.x == x && bombPosition.Value.y == y)
                {
                    continue;
                }
                if (bombCreationPositions.Contains(new Vector2Int(x, y)))
                {
                    continue;
                }
                var piece = pieces[x, y];
                if (piece != null && !matchBuffer.Contains(piece))
                {
                    matchBuffer.Add(piece);
                }
            }
        }

        private IReadOnlyList<int> GetMatchRunLengthsSnapshot()
        {
            return new List<int>(matchRunLengths);
        }

        // CODEX BOMB PR3: SpecialRecipe system for mixing special tiles.
        // Recipes (deterministic):
        // - RowClear + ColumnClear => Bomb
        // - ColorBomb + any special => Bomb
        private bool TryApplySpecialRecipe(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (TryApplyBombMix(first, second))
            {
                return true;
            }

            if (first.SpecialType == SpecialType.None || second.SpecialType == SpecialType.None)
            {
                return false;
            }

            if (!IsSpecialRecipePair(first.SpecialType, second.SpecialType))
            {
                return false;
            }

            var bombPosition = new Vector2Int(first.X, first.Y);
            if (!specialRecipeBombPositions.Add(bombPosition))
            {
                return true;
            }

            CreateBombFromRecipe(first, second, bombPosition);
            return true;
        }

        // CODEX BOMB TIERS: allow Bomb7 + Bomb6 mix into BombXX (ultra).
        private bool IsBombMixPair(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (first.SpecialType != SpecialType.Bomb || second.SpecialType != SpecialType.Bomb)
            {
                return false;
            }

            return (first.BombTier == 7 && second.BombTier == 6) || (first.BombTier == 6 && second.BombTier == 7);
        }

        // CODEX BOMB TIERS: trigger BombXX immediately in the current resolve step.
        private bool TryApplyBombMix(Piece first, Piece second)
        {
            if (!IsBombMixPair(first, second))
            {
                return false;
            }

            var bombPosition = new Vector2Int(first.X, first.Y);
            if (!IsInBounds(bombPosition.x, bombPosition.y))
            {
                return false;
            }

            CreateBombAt(bombPosition, 99);
            RemovePiece(second, DestructionReason.NormalMatch); // CODEX CHEST PR2
            QueueForcedClear(bombPosition);
            return true;
        }

        // CODEX BOMB PR3
        private bool IsSpecialRecipePair(SpecialType first, SpecialType second)
        {
            if (first == SpecialType.None || second == SpecialType.None)
            {
                return false;
            }

            if ((first == SpecialType.RowClear && second == SpecialType.ColumnClear)
                || (first == SpecialType.ColumnClear && second == SpecialType.RowClear))
            {
                return true;
            }

            if (first == SpecialType.ColorBomb || second == SpecialType.ColorBomb)
            {
                return true;
            }

            return false;
        }

        // CODEX BOMB PR3
        private void CreateBombFromRecipe(Piece first, Piece second, Vector2Int bombPosition)
        {
            if (first != null)
            {
                first.SetSpecialType(SpecialType.None);
            }

            if (second != null)
            {
                second.SetSpecialType(SpecialType.None);
            }

            if (!IsInBounds(bombPosition.x, bombPosition.y))
            {
                return;
            }

            var bombPiece = pieces[bombPosition.x, bombPosition.y];
            if (bombPiece != null)
            {
                CreateBombAt(bombPosition, 6); // CODEX BOMB TIERS: default recipe bomb to tier-6.
            }
        }

        // CODEX BOMB TIERS: create a bomb special with tier-specific sprite.
        private void CreateBombAt(Vector2Int position, int bombTier)
        {
            if (!IsInBounds(position.x, position.y))
            {
                return;
            }

            var piece = pieces[position.x, position.y];
            if (piece == null)
            {
                return;
            }

            var sprite = GetBombSpriteForTier(bombTier);
            if (sprite == null && debugMode)
            {
                Debug.LogWarning($"Missing bomb sprite for tier {bombTier} at ({position.x},{position.y}).", this);
            }

            piece.SetBombTier(bombTier, sprite);
        }

        // CODEX BOMB TIERS: enqueue a forced clear (used for bomb mixing).
        private void QueueForcedClear(Vector2Int position)
        {
            pendingSpecialClearPositions.Add(position);
        }

        // CODEX BOMB TIERS: remove a piece cleanly from the grid.
        private void RemovePiece(Piece piece, DestructionReason reason) // CODEX CHEST PR2
        {
            if (piece == null)
            {
                return;
            }

            OnPieceDestroyed?.Invoke(piece, reason); // CODEX CHEST PR2
            HandleTreasureChestDestroyed(piece); // CODEX CHEST PR1

            if (IsInBounds(piece.X, piece.Y) && pieces[piece.X, piece.Y] == piece)
            {
                pieces[piece.X, piece.Y] = null;
            }

            Destroy(piece.gameObject);
        }

        // CODEX CHEST PR1
        private void HandleTreasureChestDestroyed(Piece piece)
        {
            if (piece == null || piece.SpecialType != SpecialType.TreasureChest)
            {
                return;
            }

            chestPresent = false;
            chestCooldownMovesRemaining = RandomRange(6, 8);
            if (debugMode)
            {
                Debug.Log($"ChestDestroyed -> cooldown set to {chestCooldownMovesRemaining}", this); // CODEX CHEST PR1
            }
        }

        private IEnumerator ClearMatchesRoutine()
        {
            isBusy = true;
            var matches = FindMatches();
            var matchRunLengthsSnapshot = GetMatchRunLengthsSnapshot();
            var cascadeCount = 0;
            while (matches.Count > 0 || pendingSpecialClearPositions.Count > 0)
            {
                cascadeCount++;
                if (debugMode)
                {
                    Debug.Log($"CascadeCount({activeMoveId}): {cascadeCount}", this); // CODEX VERIFY 2: cascade instrumentation with move id.
                }
                AppendPendingSpecialClears(matches);
                var clearedCount = ClearMatches(matches);
                // CODEX: LEVEL_LOOP
                MatchesCleared?.Invoke(clearedCount, cascadeCount, matchRunLengthsSnapshot);
                yield return new WaitForSeconds(refillDelay);
                CollapseColumns();
                yield return new WaitForSeconds(refillDelay);
                RefillBoard();
                yield return new WaitForSeconds(refillDelay);
                // Continue clearing until the board settles with no matches.
                matches = FindMatches();
                matchRunLengthsSnapshot = GetMatchRunLengthsSnapshot();
            }

            EnsurePlayableBoard();
            isBusy = false;
        }

        private int ClearMatches(List<Piece> matches)
        {
            var clearedPositions = new HashSet<Vector2Int>();
            var clearedReasons = new Dictionary<Vector2Int, DestructionReason>();
            bombDetonationsThisStep.Clear();

            foreach (var piece in matches)
            {
                if (piece == null)
                {
                    continue;
                }

                if (debugMode)
                {
                    var position = new Vector2Int(piece.X, piece.Y); // CODEX VERIFY 2: log special activation once per resolve step.
                    if (specialCreationLogged.Contains(position) && specialActivationLogged.Add(position))
                    {
                        Debug.Log($"SpecialActivate({activeMoveId}): ({position.x},{position.y})", this); // CODEX VERIFY 2: special activation log with move id.
                    }
                }

                var position = new Vector2Int(piece.X, piece.Y);
                if (piece.SpecialType == SpecialType.Bomb)
                {
                    DetonateBomb(position, clearedPositions, clearedReasons);
                    continue;
                }

                if (IsInBounds(piece.X, piece.Y))
                {
                    clearedPositions.Add(position);
                    SetClearReason(clearedReasons, position, DestructionReason.NormalMatch);
                }
            }

            var clearedCount = clearedPositions.Count;
            foreach (var position in clearedPositions)
            {
                if (!IsInBounds(position.x, position.y))
                {
                    continue;
                }

                var piece = pieces[position.x, position.y];
                if (piece == null)
                {
                    continue;
                }

                var reason = GetClearReason(clearedReasons, position);
                OnPieceDestroyed?.Invoke(piece, reason); // CODEX CHEST PR2
                HandleTreasureChestDestroyed(piece); // CODEX CHEST PR1
                pieces[position.x, position.y] = null;
                Destroy(piece.gameObject);
            }

            return clearedCount;
        }

        // CODEX BOMB TIERS: merge forced clear positions into the current match list.
        private void AppendPendingSpecialClears(List<Piece> matches)
        {
            if (pendingSpecialClearPositions.Count == 0)
            {
                return;
            }

            foreach (var position in pendingSpecialClearPositions)
            {
                if (!IsInBounds(position.x, position.y))
                {
                    continue;
                }

                var piece = pieces[position.x, position.y];
                if (piece != null && !matches.Contains(piece))
                {
                    matches.Add(piece);
                }
            }

            pendingSpecialClearPositions.Clear();
        }

        // CODEX BOSS PR2
        private void DetonateBomb(
            Vector2Int position,
            HashSet<Vector2Int> clearedPositions,
            Dictionary<Vector2Int, DestructionReason> clearedReasons)
        {
            if (!bombDetonationsThisStep.Add(position))
            {
                return;
            }

            if (!IsInBounds(position.x, position.y))
            {
                return;
            }

            var sourcePiece = pieces[position.x, position.y];
            if (sourcePiece == null || sourcePiece.SpecialType != SpecialType.Bomb)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"Bomb detonation skipped at ({position.x},{position.y}) due to missing bomb piece.", this);
                }
                return;
            }

            var bombTier = sourcePiece.BombTier;
            if (bombTier == 0 && debugMode)
            {
                Debug.LogWarning($"Bomb detonation missing tier at ({position.x},{position.y}), defaulting to tier 4.", this);
            }

            OnBombDetonated?.Invoke(position);
            clearedPositions.Add(position);
            SetClearReason(clearedReasons, position, DestructionReason.BombExplosion);

            var resolvedTier = bombTier == 0 ? 4 : bombTier;
            if (resolvedTier == 6 || resolvedTier == 7 || resolvedTier == 99)
            {
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        AddBombClearTarget(new Vector2Int(x, y), clearedPositions, clearedReasons);
                    }
                }

                return;
            }

            var directions = resolvedTier == 5
                ? new[]
                {
                    Vector2Int.up,
                    Vector2Int.right,
                    Vector2Int.down,
                    Vector2Int.left,
                    new Vector2Int(1, 1)
                }
                : new[]
                {
                    Vector2Int.up,
                    Vector2Int.right,
                    Vector2Int.down,
                    Vector2Int.left
                };

            foreach (var direction in directions)
            {
                AddBombClearTarget(position + direction, clearedPositions, clearedReasons);
            }
        }

        // CODEX BOMB TIERS: add a target tile to clear list and trigger chained bombs once.
        private void AddBombClearTarget(
            Vector2Int targetPosition,
            HashSet<Vector2Int> clearedPositions,
            Dictionary<Vector2Int, DestructionReason> clearedReasons)
        {
            if (!IsInBounds(targetPosition.x, targetPosition.y))
            {
                return;
            }

            var piece = pieces[targetPosition.x, targetPosition.y];
            if (piece == null)
            {
                return;
            }

            clearedPositions.Add(targetPosition);
            SetClearReason(clearedReasons, targetPosition, DestructionReason.BombExplosion);
            if (piece.SpecialType == SpecialType.Bomb)
            {
                DetonateBomb(targetPosition, clearedPositions, clearedReasons);
            }
        }

        // CODEX CHEST PR2
        private void SetClearReason(
            Dictionary<Vector2Int, DestructionReason> clearedReasons,
            Vector2Int position,
            DestructionReason reason)
        {
            if (reason == DestructionReason.BombExplosion)
            {
                clearedReasons[position] = reason;
                return;
            }

            if (!clearedReasons.ContainsKey(position))
            {
                clearedReasons[position] = reason;
            }
        }

        // CODEX CHEST PR2
        private DestructionReason GetClearReason(
            Dictionary<Vector2Int, DestructionReason> clearedReasons,
            Vector2Int position)
        {
            return clearedReasons.TryGetValue(position, out var reason)
                ? reason
                : DestructionReason.NormalMatch;
        }

        // CODEX BOSS PR2
        private bool ShouldAllowBombCreation()
        {
            if (!bombOnlyOnBossLevels)
            {
                return true;
            }

            return GameManager.Instance != null && GameManager.Instance.IsBossLevel;
        }

        private void CollapseColumns()
        {
            for (var x = 0; x < width; x++)
            {
                var nextEmptyY = -1;
                for (var y = 0; y < height; y++)
                {
                    if (pieces[x, y] == null)
                    {
                        if (nextEmptyY == -1)
                        {
                            nextEmptyY = y;
                        }
                    }
                    else if (nextEmptyY != -1)
                    {
                        var piece = pieces[x, y];
                        pieces[x, y] = null;
                        pieces[x, nextEmptyY] = piece;
                        // Move the piece down to the lowest available slot.
                        piece.SetPosition(x, nextEmptyY, GridToWorld(x, nextEmptyY));
                        nextEmptyY++;
                    }
                }
            }
        }

        private void RefillBoard()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (pieces[x, y] == null)
                    {
                        CreatePiece(x, y, GetRandomColorIndexAvoidingMatch(x, y));
                    }
                }
            }
        }

        private void EnsurePlayableBoard()
        {
            if (HasAnyValidMoves())
            {
                return;
            }

            var resolved = ShuffleBoard();
            if (!resolved)
            {
                resolved = RegenerateBoardForSolvableState();
            }

            if (debugMode)
            {
                Debug.Log($"ShuffleTriggered({activeMoveId}): validMoves={resolved}", this); // CODEX VERIFY 2: shuffle instrumentation with move id.
            }
        }

        private bool ShuffleBoard()
        {
            var piecesList = new List<Piece>();
            var colors = new List<int>();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (!IsMatchable(piece))
                    {
                        continue;
                    }

                    piecesList.Add(piece);
                    colors.Add(piece.ColorIndex);
                }
            }

            if (piecesList.Count == 0)
            {
                return false;
            }

            var attempts = Mathf.Max(1, maxShuffleAttempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                ShuffleColors(colors);
                for (var i = 0; i < piecesList.Count; i++)
                {
                    var colorIndex = colors[i];
                    piecesList[i].SetColor(colorIndex, sprites[colorIndex]);
                }

                if (FindMatches().Count == 0 && HasAnyValidMoves())
                {
                    return true;
                }
            }

            // CODEX VERIFY: fallback re-roll to avoid dead boards after shuffles.
            ResetRandomGenerator();
            colorBag.Clear();
            for (var i = 0; i < piecesList.Count; i++)
            {
                var piece = piecesList[i];
                var colorIndex = GetRandomColorIndexAvoidingMatch(piece.X, piece.Y);
                piece.SetColor(colorIndex, sprites[colorIndex]);
            }

            return FindMatches().Count == 0 && HasAnyValidMoves();
        }

        private bool RegenerateBoardForSolvableState()
        {
            var piecesList = new List<Piece>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (!IsMatchable(piece))
                    {
                        continue;
                    }

                    piecesList.Add(piece);
                }
            }

            if (piecesList.Count == 0)
            {
                return false;
            }

            var attempts = Mathf.Max(1, maxShuffleAttempts * 2);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                ResetRandomGenerator();
                colorBag.Clear();
                for (var i = 0; i < piecesList.Count; i++)
                {
                    var piece = piecesList[i];
                    var colorIndex = GetRandomColorIndexAvoidingMatch(piece.X, piece.Y);
                    piece.SetColor(colorIndex, sprites[colorIndex]);
                }

                if (FindMatches().Count == 0 && HasAnyValidMoves())
                {
                    return true;
                }
            }

            return ForceCreateValidMove();
        }

        private bool ForceCreateValidMove()
        {
            for (var x = 0; x < width - 2; x++)
            {
                for (var y = 0; y < height - 1; y++)
                {
                    var left = pieces[x, y];
                    var middle = pieces[x + 1, y];
                    var right = pieces[x + 2, y];
                    var aboveMiddle = pieces[x + 1, y + 1];

                    if (!IsRegularMatchable(left) || !IsRegularMatchable(middle) || !IsRegularMatchable(right) || !IsRegularMatchable(aboveMiddle))
                    {
                        continue;
                    }

                    var originalLeft = left.ColorIndex;
                    var originalMiddle = middle.ColorIndex;
                    var originalRight = right.ColorIndex;
                    var originalAbove = aboveMiddle.ColorIndex;

                    for (var colorA = 0; colorA < colorCount; colorA++)
                    {
                        for (var colorB = 0; colorB < colorCount; colorB++)
                        {
                            if (colorA == colorB)
                            {
                                continue;
                            }

                            left.SetColor(colorA, sprites[colorA]);
                            middle.SetColor(colorB, sprites[colorB]);
                            right.SetColor(colorA, sprites[colorA]);
                            aboveMiddle.SetColor(colorA, sprites[colorA]);

                            if (FindMatches().Count == 0 && HasAnyValidMoves())
                            {
                                return true;
                            }
                        }
                    }

                    left.SetColor(originalLeft, sprites[originalLeft]);
                    middle.SetColor(originalMiddle, sprites[originalMiddle]);
                    right.SetColor(originalRight, sprites[originalRight]);
                    aboveMiddle.SetColor(originalAbove, sprites[originalAbove]);
                }
            }

            return false;
        }

        private void ShuffleColors(List<int> colors)
        {
            for (var i = colors.Count - 1; i > 0; i--)
            {
                var swapIndex = RandomRange(0, i + 1);
                var temp = colors[i];
                colors[i] = colors[swapIndex];
                colors[swapIndex] = temp;
            }
        }

        private bool HasAnyValidMoves()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (!IsSwappable(pieces[x, y]))
                    {
                        continue;
                    }

                    if (HasValidSwapAt(x, y, x + 1, y) || HasValidSwapAt(x, y, x, y + 1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasValidSwapAt(int x1, int y1, int x2, int y2)
        {
            if (!IsInBounds(x2, y2))
            {
                return false;
            }

            var first = pieces[x1, y1];
            var second = pieces[x2, y2];
            if (first == null || second == null)
            {
                return false;
            }

            if (!IsSwappable(first) || !IsSwappable(second))
            {
                return false;
            }

            if (first.ColorIndex == second.ColorIndex)
            {
                return false;
            }

            return WouldSwapCreateMatch(x1, y1, x2, y2);
        }

        private bool WouldSwapCreateMatch(int x1, int y1, int x2, int y2)
        {
            return HasMatchAtSimulated(x1, y1, x1, y1, x2, y2) || HasMatchAtSimulated(x2, y2, x1, y1, x2, y2);
        }

        private bool HasMatchAtSimulated(int x, int y, int swapX1, int swapY1, int swapX2, int swapY2)
        {
            var colorIndex = GetColorIndexForSwap(x, y, swapX1, swapY1, swapX2, swapY2);
            if (colorIndex < 0)
            {
                return false;
            }

            var horizontal = 1;
            horizontal += CountDirectionMatchesForColor(x, y, 1, 0, colorIndex, swapX1, swapY1, swapX2, swapY2);
            horizontal += CountDirectionMatchesForColor(x, y, -1, 0, colorIndex, swapX1, swapY1, swapX2, swapY2);
            if (horizontal >= 3)
            {
                return true;
            }

            var vertical = 1;
            vertical += CountDirectionMatchesForColor(x, y, 0, 1, colorIndex, swapX1, swapY1, swapX2, swapY2);
            vertical += CountDirectionMatchesForColor(x, y, 0, -1, colorIndex, swapX1, swapY1, swapX2, swapY2);
            return vertical >= 3;
        }

        private int CountDirectionMatchesForColor(int startX, int startY, int stepX, int stepY, int colorIndex, int swapX1, int swapY1, int swapX2, int swapY2)
        {
            var count = 0;
            var x = startX + stepX;
            var y = startY + stepY;
            while (IsInBounds(x, y))
            {
                var candidateColor = GetColorIndexForSwap(x, y, swapX1, swapY1, swapX2, swapY2);
                if (candidateColor != colorIndex)
                {
                    break;
                }

                count++;
                x += stepX;
                y += stepY;
            }

            return count;
        }

        private int GetColorIndexForSwap(int x, int y, int swapX1, int swapY1, int swapX2, int swapY2)
        {
            if (x == swapX1 && y == swapY1)
            {
                var swappedPiece = pieces[swapX2, swapY2];
                return IsMatchable(swappedPiece) ? swappedPiece.ColorIndex : -1;
            }

            if (x == swapX2 && y == swapY2)
            {
                var swappedPiece = pieces[swapX1, swapY1];
                return IsMatchable(swappedPiece) ? swappedPiece.ColorIndex : -1;
            }

            var piece = pieces[x, y];
            return IsMatchable(piece) ? piece.ColorIndex : -1;
        }

        private bool WouldFormMatch(int x, int y, int colorIndex)
        {
            // CODEX VERIFY: used for spawn/refill to avoid instant matches.
            var horizontal = 1;
            horizontal += CountDirectionMatchesForColor(x, y, 1, 0, colorIndex, -1, -1, -1, -1);
            horizontal += CountDirectionMatchesForColor(x, y, -1, 0, colorIndex, -1, -1, -1, -1);
            if (horizontal >= 3)
            {
                return true;
            }

            var vertical = 1;
            vertical += CountDirectionMatchesForColor(x, y, 0, 1, colorIndex, -1, -1, -1, -1);
            vertical += CountDirectionMatchesForColor(x, y, 0, -1, colorIndex, -1, -1, -1, -1);
            return vertical >= 3;
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private static bool IsMatchable(Piece piece)
        {
            return piece != null && !piece.IsPlayer;
        }

        private static bool IsSwappable(Piece piece)
        {
            return piece != null && !piece.IsPlayer;
        }

        private static bool IsRegularMatchable(Piece piece)
        {
            return piece != null && !piece.IsPlayer && piece.SpecialType == SpecialType.None;
        }

        private Sprite[] GenerateSprites()
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

            var spriteList = new List<Sprite>();
            for (var i = 0; i < colorCount; i++)
            {
                var color = palette[i % palette.Length];
                var texture = new Texture2D(32, 32);
                var pixels = new Color[32 * 32];
                for (var p = 0; p < pixels.Length; p++)
                {
                    pixels[p] = color;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                texture.filterMode = FilterMode.Point;

                var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                spriteList.Add(sprite);
            }

            return spriteList.ToArray();
        }

        // CODEX BOMB TIERS: load bomb sprites from Resources.
        private void LoadBombSprites()
        {
            bomb4Sprite = Resources.Load<Sprite>("Tiles/Specials/Svinino-Bombondino-X4");
            bomb7Sprite = Resources.Load<Sprite>("Tiles/Specials/Svinino-Bombondino-X7");
            bombXXSprite = Resources.Load<Sprite>("Tiles/Specials/Svinino-Bombondino-XX");
            bomb6SpriteA = Resources.Load<Sprite>("Tiles/Specials/Crocodilio-Sixventilio");
            bomb6SpriteB = Resources.Load<Sprite>("Tiles/Specials/Brainio-Sixventilio");

            if (debugMode)
            {
                WarnIfMissingSprite(bomb4Sprite, "Tiles/Specials/Svinino-Bombondino-X4");
                WarnIfMissingSprite(bomb7Sprite, "Tiles/Specials/Svinino-Bombondino-X7");
                WarnIfMissingSprite(bombXXSprite, "Tiles/Specials/Svinino-Bombondino-XX");
                WarnIfMissingSprite(bomb6SpriteA, "Tiles/Specials/Crocodilio-Sixventilio");
                WarnIfMissingSprite(bomb6SpriteB, "Tiles/Specials/Brainio-Sixventilio");
            }
        }

        // CODEX CHEST PR1
        private void LoadTreasureChestSprite() // CODEX CHEST PR1
        {
            treasureChestSprite = Resources.Load<Sprite>("Tiles/Specials/Chest"); // CODEX CHEST PR1
        }

        // CODEX BOMB TIERS: sprite selection per tier with safe fallback.
        private Sprite GetBombSpriteForTier(int bombTier)
        {
            switch (bombTier)
            {
                case 4:
                    return bomb4Sprite;
                case 6:
                    return RandomRange(0, 2) == 0 ? bomb6SpriteA : bomb6SpriteB;
                case 7:
                    return bomb7Sprite;
                case 99:
                    return bombXXSprite;
                case 5:
                    return bomb4Sprite;
                default:
                    return bomb4Sprite;
            }
        }

        // CODEX BOMB TIERS: debug warning for missing sprite paths.
        private void WarnIfMissingSprite(Sprite sprite, string resourcePath)
        {
            if (sprite == null)
            {
                Debug.LogWarning($"Missing bomb sprite at Resources path '{resourcePath}'.", this);
            }
        }

        private bool ValidateConfiguration()
        {
            if (piecePrefab == null)
            {
                Debug.LogError("Board is missing a Piece prefab reference.", this);
                return false;
            }

            if (colorCount <= 0)
            {
                Debug.LogError("Board color count must be greater than zero.", this);
                return false;
            }

            return true;
        }
    }
}
