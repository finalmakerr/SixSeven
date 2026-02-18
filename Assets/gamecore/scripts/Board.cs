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
        // CODEX STAGE 7B: board-spawned items.
        [SerializeField] private int itemLifetimeTurns = 2;
        [SerializeField] private float itemExpireFadeDuration = 0.2f;
        // CODEX CHEST PR1
        [SerializeField] [Range(1, 10)] private int chestSpawnChancePercent = 5; // CODEX CHEST PR1
        [SerializeField] private int chestCooldownMovesRemaining; // CODEX CHEST PR1
        [SerializeField] private bool chestPresent; // CODEX CHEST PR1

        private Piece[,] pieces;
        private Sprite[] sprites;
        private TileSpriteCatalog tileSpriteCatalog;
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
        // CODEX STAGE 7B: track item spawns + lifetimes.
        private readonly HashSet<Vector2Int> pendingItemSpawnPositions = new HashSet<Vector2Int>();
        private readonly HashSet<Piece> activeItems = new HashSet<Piece>();
        // CODEX STAGE 7D: track Bugada spawn.
        private bool bugadaSpawnedThisLevel;
        private Vector2Int? pendingBugadaSpawnPosition;
        private int moveId; // CODEX VERIFY 2: monotonic id for accepted swaps.
        private int activeMoveId; // CODEX VERIFY 2: current move id for resolve diagnostics.
        // CODEX CHEST PR1
        private Sprite treasureChestSprite;

        // CODEX BOMB TIERS: sprite cache for bomb tiers loaded from scene assets.
        private Sprite bomb4Sprite;
        private Sprite bomb7Sprite;
        private Sprite bombXXSprite;
        private Sprite bomb6SpriteA;
        private Sprite bomb6SpriteB;
        private BoardSpecialSpriteCatalog boardSpecialSpriteCatalog;
        private SceneAssetLoader sceneAssetLoader;

        public bool IsBusy => isBusy || externalInputLock; // CODEX VERIFY: input lock gate for stable board state.
        public int Width => width;
        public int Height => height;
        // CODEX BOSS PR1
        public int RandomSeed => randomSeed;

        // CODEX REPLAYABILITY: set run-level seed before initialization for deterministic board generation.
        public void SetRandomSeed(int seed)
        {
            randomSeed = seed;
        }

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            CacheSceneAssetLoader();
            sprites = GenerateSprites();
            CacheSpecialSpritesFromSceneAssets();
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


        public void SetBottomRowHazardVisual(Color color)
        {
            for (int x = 0; x < width; x++)
            {
                if (!IsInBounds(x, 0))
                {
                    continue;
                }

                var tile = pieces[x, 0];
                if (tile != null)
                {
                    tile.SetHazardOverlay(color);
                }
            }
        }

        public void ClearBottomRowHazardVisual()
        {
            for (int x = 0; x < width; x++)
            {
                if (!IsInBounds(x, 0))
                {
                    continue;
                }

                var tile = pieces[x, 0];
                if (tile != null)
                {
                    tile.ClearHazardOverlay();
                }
            }
        }


        public bool IsHazardTile(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

            if (!TryGetPieceAt(new Vector2Int(x, y), out var piece) || piece == null)
            {
                return false;
            }

            return piece.GetTileDebuff() == TileDebuffType.Entangled;
        }

        public int GetHazardDamageAt(int x, int y)
        {
            return IsHazardTile(x, y) ? 1 : 0;
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
            pendingItemSpawnPositions.Clear(); // CODEX STAGE 7B: reset item spawns.
            activeItems.Clear(); // CODEX STAGE 7B: reset item lifetimes.
            bugadaSpawnedThisLevel = false; // CODEX STAGE 7D: reset Bugada spawn tracking.
            pendingBugadaSpawnPosition = null; // CODEX STAGE 7D: reset Bugada pending spawn.
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

            if (TryStartLootIntercept(first, second))
            {
                return true;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsMonsterEnraged(second.GetInstanceID()))
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

        private bool TryStartLootIntercept(Piece first, Piece second)
        {
            if (first == null || second == null || GameManager.Instance == null)
            {
                return false;
            }

            Piece playerPiece;
            Piece lootPiece;
            if (first.IsPlayer && IsLootPiece(second))
            {
                playerPiece = first;
                lootPiece = second;
            }
            else if (second.IsPlayer && IsLootPiece(first))
            {
                playerPiece = second;
                lootPiece = first;
            }
            else
            {
                return false;
            }

            isBusy = true;
            var targetPosition = new Vector2Int(lootPiece.X, lootPiece.Y);
            GameManager.Instance.HandleLootIntercept(
                targetPosition,
                () => StartCoroutine(CommitLootInterceptMovementRoutine(playerPiece, lootPiece)),
                () => { isBusy = false; });
            return true;
        }

        private static bool IsLootPiece(Piece piece)
        {
            return piece != null && (piece.SpecialType == SpecialType.Item || piece.SpecialType == SpecialType.Bugada);
        }

        private IEnumerator CommitLootInterceptMovementRoutine(Piece playerPiece, Piece lootPiece)
        {
            if (playerPiece == null || lootPiece == null)
            {
                isBusy = false;
                yield break;
            }

            var sourcePosition = new Vector2Int(playerPiece.X, playerPiece.Y);
            SwapPieces(playerPiece, lootPiece);

            yield return new WaitForSeconds(0.05f);

            TryDestroyPieceAt(sourcePosition, DestructionReason.ItemPickup);

            activeMoveId = moveId + 1;
            moveId = activeMoveId;
            ValidSwap?.Invoke();
            TurnEnded?.Invoke();
            isBusy = false;
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
            return hasMatch
                || IsSpecialRecipePair(first.SpecialType, second.SpecialType)
                || IsBombMixPair(first, second)
                || ShouldAllowBugadaSwap(first, second);
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

        public bool HasMatchOpportunityForMonster(Piece monster)
        {
            if (monster == null || monster.IsPlayer)
            {
                return false;
            }

            return CanMatchPieceWithinEnergyDepth(new Vector2Int(monster.X, monster.Y), 1);
        }

        public bool CanMatchPieceWithinEnergyDepth(Vector2Int monsterPosition, int availableEnergy)
        {
            if (availableEnergy <= 0)
            {
                return false;
            }

            if (!TryGetPieceAt(monsterPosition, out var monsterPiece) || !IsMatchable(monsterPiece))
            {
                return false;
            }

            var originalGrid = CloneGridState(monsterPosition);
            if (!originalGrid.HasMonster)
            {
                return false;
            }

            try
            {
                var maxDepth = Mathf.Min(2, availableEnergy);
                return SearchDepth(originalGrid, maxDepth, 0);
            }
            finally
            {
                RestoreGridState(originalGrid);
            }
        }

        private bool SearchDepth(SimulationState state, int maxDepth, int currentDepth)
        {
            if (currentDepth >= maxDepth)
            {
                return false;
            }

            var legalSwaps = GetAllLegalSwaps(state);
            for (var i = 0; i < legalSwaps.Count; i++)
            {
                var branchState = CloneGridState(state);
                ApplySwap(branchState, legalSwaps[i]);

                if (!ResolveAllMatchesAndCascades(branchState))
                {
                    continue;
                }

                if (WasMonsterDirectlyMatched(branchState))
                {
                    return true;
                }

                if (!branchState.HasMonster)
                {
                    return true;
                }

                if (currentDepth + 1 >= maxDepth)
                {
                    continue;
                }

                if (SearchDepth(branchState, maxDepth, currentDepth + 1))
                {
                    return true;
                }
            }

            return false;
        }

        private bool WasMonsterDirectlyMatched(SimulationState state)
        {
            return state.MonsterMatchedDuringResolve;
        }

        private bool ResolveAllMatchesAndCascades(SimulationState state)
        {
            var anyMatch = false;
            state.MonsterMatchedDuringResolve = false;

            while (true)
            {
                var matches = FindMatches(state);
                if (matches.Count == 0)
                {
                    break;
                }

                anyMatch = true;
                for (var i = 0; i < matches.Count; i++)
                {
                    var position = matches[i];
                    if (!IsInBounds(position.x, position.y))
                    {
                        continue;
                    }

                    if (state.Cells[position.x, position.y].IsTargetMonster)
                    {
                        state.MonsterMatchedDuringResolve = true;
                    }

                    RemovePiece(state, position);
                }

                ApplyGravity(state);
                RefillBoardDeterministic(state);
            }

            return anyMatch;
        }

        public bool CanPlayerKillMonsterInTwoTurns(
            Vector2Int monsterPosition,
            Vector2Int playerPosition,
            int currentEnergy,
            int maxEnergy,
            int meditationRange,
            int telekinesisCost)
        {
            if (!TryGetPieceAt(monsterPosition, out var monsterPiece) || !IsMatchable(monsterPiece))
            {
                return false;
            }

            var initialState = CreateSimulationState(monsterPosition);
            if (!initialState.HasMonster)
            {
                return false;
            }

            var reachableTurn1 = EnumerateReachableSwaps(
                initialState,
                playerPosition,
                currentEnergy,
                meditationRange,
                telekinesisCost);

            for (var i = 0; i < reachableTurn1.Count; i++)
            {
                var branch1 = CloneSimulationState(initialState);

                var energyAfterTurn1 = currentEnergy;
                if (reachableTurn1[i].Cost > 0)
                {
                    energyAfterTurn1 -= reachableTurn1[i].Cost;
                }

                ApplySimulationSwap(branch1, reachableTurn1[i]);
                if (ResolveSimulationBoard(branch1))
                {
                    return true;
                }

                var energyTurn2 = Mathf.Min(maxEnergy, energyAfterTurn1 + 1);
                var reachableTurn2 = EnumerateReachableSwaps(
                    branch1,
                    playerPosition,
                    energyTurn2,
                    meditationRange,
                    telekinesisCost);

                for (var j = 0; j < reachableTurn2.Count; j++)
                {
                    var branch2 = CloneSimulationState(branch1);
                    ApplySimulationSwap(branch2, reachableTurn2[j]);
                    if (ResolveSimulationBoard(branch2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // CODEX RAGE SCALE FINAL
        public int GetMonsterRageMatchabilityScore(Vector2Int monsterPosition)
        {
            if (!TryGetPieceAt(monsterPosition, out var monsterPiece) || !IsMatchable(monsterPiece))
            {
                return 0;
            }

            if (CanMatchPieceInTwoSwaps(monsterPosition))
            {
                return 2;
            }

            return CanMatchPieceInOneSwap(monsterPosition) ? 1 : 0;
        }

        // CODEX RAGE SCALE FINAL
        private bool CanMatchPieceInOneSwap(Vector2Int monsterPosition)
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (!TryGetPieceAt(new Vector2Int(x, y), out var first) || !IsSwappable(first))
                    {
                        continue;
                    }

                    if (WouldSwapCreateMatchIncludingMonster(x, y, x + 1, y, monsterPosition)
                        || WouldSwapCreateMatchIncludingMonster(x, y, x, y + 1, monsterPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // CODEX RAGE SCALE FINAL
        private bool CanMatchPieceInTwoSwaps(Vector2Int monsterPosition)
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (!IsInBounds(x, y) || !IsSwappable(pieces[x, y]))
                    {
                        continue;
                    }

                    if (TryTwoSwapPathForMonster(x, y, x + 1, y, monsterPosition)
                        || TryTwoSwapPathForMonster(x, y, x, y + 1, monsterPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private SimulationState CreateSimulationState(Vector2Int monsterPosition)
        {
            var cells = new SimPiece[width, height];
            var hasMonster = false;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    cells[x, y] = CreateSimPiece(piece, x, y, x == monsterPosition.x && y == monsterPosition.y);
                    if (cells[x, y].IsTargetMonster)
                    {
                        hasMonster = true;
                    }
                }
            }

            var branchSeed = randomSeed ^ (monsterPosition.x * 48611) ^ (monsterPosition.y * 98473);
            return new SimulationState
            {
                Cells = cells,
                RandomState = branchSeed,
                HasMonster = hasMonster,
                MonsterMatchedDuringResolve = false,
            };
        }

        private SimulationState CloneSimulationState(SimulationState source)
        {
            var cloneCells = (SimPiece[,])source.Cells.Clone();
            return new SimulationState
            {
                Cells = cloneCells,
                RandomState = source.RandomState,
                HasMonster = source.HasMonster,
                MonsterMatchedDuringResolve = source.MonsterMatchedDuringResolve,
            };
        }

        private SimulationState CloneGridState(Vector2Int monsterPosition)
        {
            return CreateSimulationState(monsterPosition);
        }

        private SimulationState CloneGridState(SimulationState source)
        {
            return CloneSimulationState(source);
        }

        private void RestoreGridState(SimulationState state)
        {
            _ = state;
        }

        private SimPiece CreateSimPiece(Piece piece, int x, int y, bool isTargetMonster)
        {
            if (piece == null)
            {
                return SimPiece.Empty;
            }

            return new SimPiece
            {
                Occupied = true,
                ColorIndex = piece.ColorIndex,
                IsMatchable = IsMatchable(piece),
                IsSwappable = IsSwappable(piece),
                IsTargetMonster = isTargetMonster,
                IsBossLocked = GameManager.Instance != null
                    && GameManager.Instance.IsBossLevel
                    && GameManager.Instance.CurrentBossState.bossAlive
                    && x == GameManager.Instance.CurrentBossState.bossPosition.x
                    && y == GameManager.Instance.CurrentBossState.bossPosition.y
            };
        }

        private List<SimSwap> EnumerateReachableSwaps(
            SimulationState state,
            Vector2Int playerPosition,
            int availableEnergy,
            int meditationRange,
            int telekinesisCost)
        {
            var swaps = new List<SimSwap>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var dist = Mathf.Abs(playerPosition.x - x) + Mathf.Abs(playerPosition.y - y);
                    if (dist > meditationRange)
                    {
                        continue;
                    }

                    var cost = dist <= 1 ? 0 : telekinesisCost;
                    if (cost > availableEnergy)
                    {
                        continue;
                    }

                    TryAddReachableSwap(state, x, y, x + 1, y, cost, swaps);
                    TryAddReachableSwap(state, x, y, x, y + 1, cost, swaps);
                }
            }

            return swaps;
        }

        private void TryAddReachableSwap(
            SimulationState state,
            int x1,
            int y1,
            int x2,
            int y2,
            int cost,
            List<SimSwap> swaps)
        {
            if (!IsInBounds(x2, y2))
            {
                return;
            }

            var first = state.Cells[x1, y1];
            var second = state.Cells[x2, y2];
            if (!first.IsSwappable || !second.IsSwappable || first.ColorIndex == second.ColorIndex)
            {
                return;
            }

            if (first.IsBossLocked || second.IsBossLocked)
            {
                return;
            }

            if (!WouldSimulationSwapCreateMatch(state.Cells, x1, y1, x2, y2))
            {
                return;
            }

            swaps.Add(new SimSwap(x1, y1, x2, y2, cost));
        }

        private bool WouldSimulationSwapCreateMatch(SimPiece[,] cells, int x1, int y1, int x2, int y2)
        {
            SwapSimCells(cells, x1, y1, x2, y2);
            var createsMatch = HasSimulationMatchAt(cells, x1, y1) || HasSimulationMatchAt(cells, x2, y2);
            SwapSimCells(cells, x1, y1, x2, y2);
            return createsMatch;
        }

        private List<SimSwap> GetAllLegalSwaps(SimulationState state)
        {
            var swaps = new List<SimSwap>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    TryAddReachableSwap(state, x, y, x + 1, y, 0, swaps);
                    TryAddReachableSwap(state, x, y, x, y + 1, 0, swaps);
                }
            }

            return swaps;
        }

        private void ApplySwap(SimulationState state, SimSwap swap)
        {
            ApplySimulationSwap(state, swap);
        }

        private void ApplySimulationSwap(SimulationState state, SimSwap swap)
        {
            SwapSimCells(state.Cells, swap.X1, swap.Y1, swap.X2, swap.Y2);
        }

        private void SwapSimCells(SimPiece[,] cells, int x1, int y1, int x2, int y2)
        {
            var temp = cells[x1, y1];
            cells[x1, y1] = cells[x2, y2];
            cells[x2, y2] = temp;
        }

        private bool ResolveSimulationBoard(SimulationState state)
        {
            var cells = state.Cells;
            while (true)
            {
                var toRemove = FindSimulationMatches(cells);
                if (toRemove.Count == 0)
                {
                    return false;
                }

                for (var i = 0; i < toRemove.Count; i++)
                {
                    var position = toRemove[i];
                    if (!IsInBounds(position.x, position.y))
                    {
                        continue;
                    }

                    if (cells[position.x, position.y].IsTargetMonster)
                    {
                        return true;
                    }

                    cells[position.x, position.y] = SimPiece.Empty;
                }

                ApplySimulationGravity(state);
            }
        }

        private List<Vector2Int> FindMatches(SimulationState state)
        {
            return FindSimulationMatches(state.Cells);
        }

        private void RemovePiece(SimulationState state, Vector2Int position)
        {
            var cell = state.Cells[position.x, position.y];

            if (cell.IsTargetMonster)
            {
                state.MonsterMatchedDuringResolve = true;
                state.HasMonster = false;
            }

            state.Cells[position.x, position.y] = SimPiece.Empty;
        }

        private void ApplyGravity(SimulationState state)
        {
            ApplySimulationGravity(state);
        }

        private void RefillBoardDeterministic(SimulationState state)
        {
            // Refill is part of gravity in simulation path.
            _ = state;
        }

        private List<Vector2Int> FindSimulationMatches(SimPiece[,] cells)
        {
            var uniqueMatches = new HashSet<Vector2Int>();
            for (var y = 0; y < height; y++)
            {
                var x = 0;
                while (x < width)
                {
                    if (!cells[x, y].IsMatchable)
                    {
                        x++;
                        continue;
                    }

                    var color = cells[x, y].ColorIndex;
                    var start = x;
                    x++;
                    while (x < width && cells[x, y].IsMatchable && cells[x, y].ColorIndex == color)
                    {
                        x++;
                    }

                    if (x - start >= 3)
                    {
                        for (var runX = start; runX < x; runX++)
                        {
                            uniqueMatches.Add(new Vector2Int(runX, y));
                        }
                    }
                }
            }

            for (var x = 0; x < width; x++)
            {
                var y = 0;
                while (y < height)
                {
                    if (!cells[x, y].IsMatchable)
                    {
                        y++;
                        continue;
                    }

                    var color = cells[x, y].ColorIndex;
                    var start = y;
                    y++;
                    while (y < height && cells[x, y].IsMatchable && cells[x, y].ColorIndex == color)
                    {
                        y++;
                    }

                    if (y - start >= 3)
                    {
                        for (var runY = start; runY < y; runY++)
                        {
                            uniqueMatches.Add(new Vector2Int(x, runY));
                        }
                    }
                }
            }

            return new List<Vector2Int>(uniqueMatches);
        }

        private void ApplySimulationGravity(SimulationState state)
        {
            var cells = state.Cells;
            for (var x = 0; x < width; x++)
            {
                var writeY = 0;
                for (var y = 0; y < height; y++)
                {
                    if (!cells[x, y].Occupied)
                    {
                        continue;
                    }

                    if (writeY != y)
                    {
                        cells[x, writeY] = cells[x, y];
                        cells[x, y] = SimPiece.Empty;
                    }

                    writeY++;
                }

                for (var spawnY = writeY; spawnY < height; spawnY++)
                {
                    cells[x, spawnY] = new SimPiece
                    {
                        Occupied = true,
                        ColorIndex = NextSimulationColorIndex(state),
                        IsMatchable = true,
                        IsSwappable = true,
                        IsTargetMonster = false,
                        IsBossLocked = false
                    };
                }
            }
        }

        private int NextSimulationColorIndex(SimulationState state)
        {
            unchecked
            {
                state.RandomState = (state.RandomState * 1103515245) + 12345;
            }

            var value = state.RandomState & int.MaxValue;
            return value % Mathf.Max(1, colorCount);
        }

        private bool HasSimulationMatchAt(SimPiece[,] cells, int x, int y)
        {
            if (!IsInBounds(x, y) || !cells[x, y].IsMatchable)
            {
                return false;
            }

            var color = cells[x, y].ColorIndex;
            var horizontal = 1;
            horizontal += CountSimulationDirection(cells, x, y, 1, 0, color);
            horizontal += CountSimulationDirection(cells, x, y, -1, 0, color);
            if (horizontal >= 3)
            {
                return true;
            }

            var vertical = 1;
            vertical += CountSimulationDirection(cells, x, y, 0, 1, color);
            vertical += CountSimulationDirection(cells, x, y, 0, -1, color);
            return vertical >= 3;
        }

        private int CountSimulationDirection(SimPiece[,] cells, int startX, int startY, int stepX, int stepY, int color)
        {
            var count = 0;
            var x = startX + stepX;
            var y = startY + stepY;
            while (IsInBounds(x, y) && cells[x, y].IsMatchable && cells[x, y].ColorIndex == color)
            {
                count++;
                x += stepX;
                y += stepY;
            }

            return count;
        }

        private sealed class SimulationState
        {
            public SimPiece[,] Cells;
            public int RandomState;
            public bool HasMonster;
            public bool MonsterMatchedDuringResolve;
        }

        private readonly struct SimSwap
        {
            public readonly int X1;
            public readonly int Y1;
            public readonly int X2;
            public readonly int Y2;
            public readonly int Cost;

            public SimSwap(int x1, int y1, int x2, int y2, int cost)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
                Cost = cost;
            }
        }

        private struct SimPiece
        {
            public bool Occupied;
            public int ColorIndex;
            public bool IsMatchable;
            public bool IsSwappable;
            public bool IsTargetMonster;
            public bool IsBossLocked;

            public static SimPiece Empty => default;
        }

        // CODEX RAGE SCALE FINAL
        private bool TryTwoSwapPathForMonster(int x1, int y1, int x2, int y2, Vector2Int monsterPosition)
        {
            if (!IsInBounds(x2, y2))
            {
                return false;
            }

            var first = pieces[x1, y1];
            var second = pieces[x2, y2];
            if (!IsSwappable(first) || !IsSwappable(second) || first.ColorIndex == second.ColorIndex)
            {
                return false;
            }

            SwapPiecesInGrid(first, second);
            try
            {
                return CanMatchPieceInOneSwap(monsterPosition);
            }
            finally
            {
                SwapPiecesInGrid(first, second);
            }
        }

        // CODEX RAGE SCALE FINAL
        private bool WouldSwapCreateMatchIncludingMonster(int x1, int y1, int x2, int y2, Vector2Int monsterPosition)
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

            if (!IsSwappable(first) || !IsSwappable(second) || first.ColorIndex == second.ColorIndex)
            {
                return false;
            }

            var isMonsterPieceMoved = (x1 == monsterPosition.x && y1 == monsterPosition.y)
                || (x2 == monsterPosition.x && y2 == monsterPosition.y);
            if (isMonsterPieceMoved)
            {
                return HasMatchAtSimulated(x1, y1, x1, y1, x2, y2)
                    || HasMatchAtSimulated(x2, y2, x1, y1, x2, y2);
            }

            return HasMatchAtSimulated(monsterPosition.x, monsterPosition.y, x1, y1, x2, y2);
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

            if (ShouldBlockBossInstantKill(piece, reason))
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
            var bugadaSwapApplied = TryApplyBugadaSwapClears(first, second);
            var matches = FindMatches();
            if (matches.Count == 0 && !specialRecipeApplied && !bugadaSwapApplied)
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
            TryApplyBugadaSwapClears(first, second);

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

        // CODEX STAGE 7D: allow Bugada swaps without matches and queue clears.
        private bool ShouldAllowBugadaSwap(Piece first, Piece second)
        {
            if (!IsBugadaActive())
            {
                return false;
            }

            return IsBugadaTarget(first) || IsBugadaTarget(second);
        }

        private bool TryApplyBugadaSwapClears(Piece first, Piece second)
        {
            if (!IsBugadaActive())
            {
                return false;
            }

            var applied = false;
            if (IsBugadaTarget(first))
            {
                pendingSpecialClearPositions.Add(new Vector2Int(first.X, first.Y));
                applied = true;
            }

            if (IsBugadaTarget(second))
            {
                pendingSpecialClearPositions.Add(new Vector2Int(second.X, second.Y));
                applied = true;
            }

            return applied;
        }

        private bool IsBugadaTarget(Piece piece)
        {
            if (piece == null || piece.IsPlayer)
            {
                return false;
            }

            if (IsBossPiece(piece))
            {
                return false;
            }

            return piece.SpecialType == SpecialType.None || piece.SpecialType == SpecialType.Tumor;
        }

        private bool IsBugadaActive()
        {
            return GameManager.Instance != null && GameManager.Instance.IsBugadaActive;
        }

        private bool IsBossPiece(Piece piece)
        {
            if (piece == null || GameManager.Instance == null || !GameManager.Instance.IsBossLevel)
            {
                return false;
            }

            var bossState = GameManager.Instance.CurrentBossState;
            return bossState.bossAlive
                && piece.X == bossState.bossPosition.x
                && piece.Y == bossState.bossPosition.y;
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
                CacheSpecialSpritesFromSceneAssets();
            }

            piece.SetSpecialType(SpecialType.TreasureChest);
            if (treasureChestSprite == null) // CODEX CHEST PR1
            {
                if (debugMode) // CODEX CHEST PR1
                {
                    Debug.LogWarning("Treasure chest sprite missing from BoardSpecialSpriteCatalog.", this); // CODEX CHEST PR1
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
            pendingItemSpawnPositions.Clear(); // CODEX STAGE 7B: reset item spawn tracking per scan.

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

            if (runLength >= GameManager.Instance.BalanceConfig.MinRunForSpecialBomb)
            {
                var itemSpawnPosition = FindItemSpawnPosition(endX, endY, runLength, direction, bombPosition);
                if (itemSpawnPosition.HasValue)
                {
                    if (!bugadaSpawnedThisLevel && !pendingBugadaSpawnPosition.HasValue)
                    {
                        pendingBugadaSpawnPosition = itemSpawnPosition.Value; // CODEX STAGE 7D: Bugada spawn candidate.
                    }
                    else if (GameManager.Instance == null || GameManager.Instance.CanRollItemDrop())
                    {
                        pendingItemSpawnPositions.Add(itemSpawnPosition.Value); // CODEX STAGE 7I-A: weighted item drop candidate only when a valid drop exists.
                    }
                }
            }

            if (runLength >= GameManager.Instance.BalanceConfig.MinRunForLootRoll && debugMode)
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

        // CODEX STAGE 7B: choose an empty spot in a match run for item spawning.
        private Vector2Int? FindItemSpawnPosition(int endX, int endY, int runLength, Vector2Int direction, Vector2Int? bombPosition)
        {
            for (var i = 0; i < runLength; i++)
            {
                var x = endX - direction.x * i;
                var y = endY - direction.y * i;
                var candidate = new Vector2Int(x, y);
                if (bombPosition.HasValue && bombPosition.Value == candidate)
                {
                    continue;
                }

                if (bombCreationPositions.Contains(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
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

        // CODEX STAGE 7B: spawn items at recorded match positions.
        private void SpawnPendingItems()
        {
            SpawnPendingBugada();
            if (pendingItemSpawnPositions.Count == 0)
            {
                return;
            }

            foreach (var position in pendingItemSpawnPositions)
            {
                TrySpawnItemAt(position);
            }

            pendingItemSpawnPositions.Clear();
        }

        // CODEX STAGE 7D: spawn Bugada at recorded match position.
        private void SpawnPendingBugada()
        {
            if (!pendingBugadaSpawnPosition.HasValue)
            {
                return;
            }

            var position = pendingBugadaSpawnPosition.Value;
            pendingItemSpawnPositions.Remove(position);
            if (TrySpawnBugadaAt(position))
            {
                bugadaSpawnedThisLevel = true;
                pendingBugadaSpawnPosition = null;
            }
        }

        // CODEX STAGE 7B: try to place an item into an empty cell.
        private void TrySpawnItemAt(Vector2Int position)
        {
            if (!IsInBounds(position.x, position.y) || pieces == null)
            {
                return;
            }

            if (pieces[position.x, position.y] != null)
            {
                return;
            }

            var itemPiece = CreatePiece(position.x, position.y, GetRandomColorIndex());
            if (itemPiece == null)
            {
                return;
            }

            var lifetime = Mathf.Max(1, itemLifetimeTurns);
            itemPiece.ConfigureAsItem(lifetime);
            activeItems.Add(itemPiece);
        }

        // CODEX STAGE 7D: try to place a Bugada item into an empty cell.
        private bool TrySpawnBugadaAt(Vector2Int position)
        {
            if (!IsInBounds(position.x, position.y) || pieces == null)
            {
                return false;
            }

            if (pieces[position.x, position.y] != null)
            {
                return false;
            }

            var bugadaPiece = CreatePiece(position.x, position.y, GetRandomColorIndex());
            if (bugadaPiece == null)
            {
                return false;
            }

            bugadaPiece.ConfigureAsBugada();
            return true;
        }

        // CODEX STAGE 7B: decrement item turns at end of each match.
        private void TickItemTurns()
        {
            if (activeItems.Count == 0)
            {
                return;
            }

            var expiredItems = new List<Piece>();
            foreach (var item in activeItems)
            {
                if (item == null)
                {
                    expiredItems.Add(item);
                    continue;
                }

                var remaining = item.ItemTurnsRemaining - 1;
                item.UpdateItemTurns(remaining);
                if (remaining == 1)
                {
                    ApplyLootFadeVisual(item, 0.75f);
                }
                else
                {
                    ApplyLootFadeVisual(item, 1f);
                }

                if (remaining <= 0)
                {
                    expiredItems.Add(item);
                }
            }

            foreach (var item in expiredItems)
            {
                activeItems.Remove(item);
                if (item == null)
                {
                    continue;
                }

                StartCoroutine(FadeAndRemoveItem(item));
            }

        }

        private void ApplyLootFadeVisual(Piece piece, float alpha)
        {
            if (piece == null)
            {
                return;
            }

            var renderer = piece.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        // CODEX STAGE 7B: fade expired items before removing them.
        private IEnumerator FadeAndRemoveItem(Piece item)
        {
            if (item == null)
            {
                yield break;
            }

            var duration = Mathf.Max(0.05f, itemExpireFadeDuration);
            var elapsed = 0f;
            while (elapsed < duration && item != null)
            {
                var alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                item.SetItemFade(alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (item == null)
            {
                yield break;
            }

            item.SetItemFade(0f);
            if (IsInBounds(item.X, item.Y) && pieces[item.X, item.Y] == item)
            {
                pieces[item.X, item.Y] = null;
            }

            Destroy(item.gameObject);
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
            activeItems.Remove(piece); // CODEX STAGE 7B

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
                SpawnPendingItems(); // CODEX STAGE 7B: spawn items from 4+ matches.
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

        public void TickLootTurnsForTurnEnd()
        {
            TickItemTurns();
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
                    if (ShouldBlockBossInstantKill(piece, DestructionReason.NormalMatch))
                    {
                        continue;
                    }

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
                if (ShouldBlockBossInstantKill(piece, reason))
                {
                    continue;
                }

                OnPieceDestroyed?.Invoke(piece, reason); // CODEX CHEST PR2
                HandleTreasureChestDestroyed(piece); // CODEX CHEST PR1
                activeItems.Remove(piece); // CODEX STAGE 7B: remove items cleared by specials.
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

            if (ShouldBlockBossInstantKill(piece, DestructionReason.BombExplosion))
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
            return piece != null
                && !piece.IsPlayer
                && !ShouldBlockBossInstantKill(piece, DestructionReason.NormalMatch)
                && piece.SpecialType != SpecialType.Item
                && piece.SpecialType != SpecialType.Bugada;
        }

        private static bool IsSwappable(Piece piece)
        {
            if (piece == null || piece.IsPlayer)
            {
                return false;
            }

            if (piece.SpecialType == SpecialType.Item || piece.SpecialType == SpecialType.Bugada)
            {
                return false;
            }

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.IsMonsterSwapLockedAtPosition(new Vector2Int(piece.X, piece.Y)))
                {
                    return false;
                }

                if (GameManager.Instance.IsBossLevel)
                {
                    var bossState = GameManager.Instance.CurrentBossState;
                    if (bossState.bossAlive && piece.X == bossState.bossPosition.x && piece.Y == bossState.bossPosition.y)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsRegularMatchable(Piece piece)
        {
            return piece != null && !piece.IsPlayer && piece.SpecialType == SpecialType.None;
        }

        // CODEX REPLAYABILITY: apply procedural tumor placements for this run.
        public int PlaceTumors(IReadOnlyList<TumorSpawnData> tumors)
        {
            if (tumors == null || pieces == null)
            {
                return 0;
            }

            var placed = 0;
            for (var i = 0; i < tumors.Count; i++)
            {
                var tumor = tumors[i];
                if (!TryPlaceTumor(tumor.position, tumor.tier))
                {
                    continue;
                }

                placed++;
            }

            return placed;
        }

        public bool HasTumorAt(Vector2Int position)
        {
            if (!IsInBounds(position.x, position.y) || pieces == null)
            {
                return false;
            }

            var piece = pieces[position.x, position.y];
            return piece != null && piece.SpecialType == SpecialType.Tumor;
        }

        public bool IsPathToBossClearOfTumors(Vector2Int from, Vector2Int to)
        {
            if (!IsInBounds(from.x, from.y) || !IsInBounds(to.x, to.y))
            {
                return false;
            }

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            queue.Enqueue(from);
            visited.Add(from);
            var offsets = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == to)
                {
                    return true;
                }

                for (var i = 0; i < offsets.Length; i++)
                {
                    var next = current + offsets[i];
                    if (!IsInBounds(next.x, next.y) || visited.Contains(next))
                    {
                        continue;
                    }

                    var piece = pieces[next.x, next.y];
                    if (piece == null)
                    {
                        continue;
                    }

                    if (piece.SpecialType == SpecialType.Tumor)
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private bool TryPlaceTumor(Vector2Int position, int tier)
        {
            if (!IsInBounds(position.x, position.y) || pieces == null)
            {
                return false;
            }

            var piece = pieces[position.x, position.y];
            if (piece == null || piece.IsPlayer)
            {
                return false;
            }

            if (piece.SpecialType == SpecialType.Bugada || piece.SpecialType == SpecialType.Item || piece.SpecialType == SpecialType.TreasureChest)
            {
                return false;
            }

            piece.ConfigureAsTumor(tier);
            return true;
        }

        // CODEX BOSS TUMOR SYNERGY PR1
        public bool TrySpawnBossTumor(System.Random random, int maxTier, out Vector2Int position)
        {
            position = default;
            if (pieces == null || random == null)
            {
                return false;
            }

            var candidates = new List<Vector2Int>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (piece == null || piece.IsPlayer)
                    {
                        continue;
                    }

                    if (piece.SpecialType != SpecialType.None)
                    {
                        continue;
                    }

                    candidates.Add(new Vector2Int(x, y));
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var selected = candidates[random.Next(0, candidates.Count)];
            var pieceToMutate = pieces[selected.x, selected.y];
            if (pieceToMutate == null)
            {
                return false;
            }

            pieceToMutate.ConfigureAsTumor(Mathf.Clamp(maxTier, 1, 4));
            position = selected;
            return true;
        }

        // CODEX BOSS TUMOR SYNERGY PR1
        public bool TryUpgradeBossTumor(System.Random random, int maxTier, out Vector2Int position, out int newTier)
        {
            position = default;
            newTier = 0;
            if (pieces == null || random == null)
            {
                return false;
            }

            var candidates = new List<Piece>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (piece == null || piece.SpecialType != SpecialType.Tumor)
                    {
                        continue;
                    }

                    if (piece.TumorTier >= maxTier)
                    {
                        continue;
                    }

                    candidates.Add(piece);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var selected = candidates[random.Next(0, candidates.Count)];
            newTier = Mathf.Clamp(selected.TumorTier + 1, 1, maxTier);
            selected.ConfigureAsTumor(newTier);
            position = new Vector2Int(selected.X, selected.Y);
            return true;
        }

        // CODEX BOSS TUMOR SYNERGY PR1
        public int CountTumorsAndTotalTier(out int totalTier)
        {
            totalTier = 0;
            if (pieces == null)
            {
                return 0;
            }

            var count = 0;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (piece == null || piece.SpecialType != SpecialType.Tumor)
                    {
                        continue;
                    }

                    count += 1;
                    totalTier += Mathf.Max(1, piece.TumorTier);
                }
            }

            return count;
        }

        private Sprite[] GenerateSprites()
        {
            if (tileSpriteCatalog == null)
            {
                CacheTileSpriteCatalogFromSceneAssets();
            }

            if (tileSpriteCatalog != null && tileSpriteCatalog.Sprites != null && tileSpriteCatalog.Sprites.Count > 0)
            {
                var catalogSprites = new Sprite[tileSpriteCatalog.Sprites.Count];
                for (var i = 0; i < tileSpriteCatalog.Sprites.Count; i++)
                {
                    catalogSprites[i] = tileSpriteCatalog.Sprites[i];
                }

                return catalogSprites;
            }

            Debug.LogWarning("TileSpriteCatalog missing from SceneAssetLoader; using generated fallback sprites.", this);

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

        private static bool ShouldBlockBossInstantKill(Piece piece, DestructionReason reason)
        {
            if (piece == null || GameManager.Instance == null || !GameManager.Instance.IsBossLevel)
            {
                return false;
            }

            var bossState = GameManager.Instance.CurrentBossState;
            if (!bossState.bossAlive || piece.X != bossState.bossPosition.x || piece.Y != bossState.bossPosition.y)
            {
                return false;
            }

            var boss = GameManager.Instance.CurrentBoss;
            if (boss == null || !boss.preventInstantKillFromPlayerActions)
            {
                return false;
            }

            return reason != DestructionReason.MonsterAttack;
        }

        private void CacheSpecialSpritesFromSceneAssets()
        {
            CacheSceneAssetLoader();
            if (boardSpecialSpriteCatalog == null)
            {
                if (sceneAssetLoader != null)
                {
                    boardSpecialSpriteCatalog = sceneAssetLoader.GetLoadedAsset<BoardSpecialSpriteCatalog>();
                }
            }

            if (boardSpecialSpriteCatalog == null)
            {
                return;
            }

            bomb4Sprite = boardSpecialSpriteCatalog.Bomb4Sprite;
            bomb7Sprite = boardSpecialSpriteCatalog.Bomb7Sprite;
            bombXXSprite = boardSpecialSpriteCatalog.BombXXSprite;
            bomb6SpriteA = boardSpecialSpriteCatalog.Bomb6SpriteA;
            bomb6SpriteB = boardSpecialSpriteCatalog.Bomb6SpriteB;
            treasureChestSprite = boardSpecialSpriteCatalog.TreasureChestSprite;

            WarnIfMissingSprite(bomb4Sprite, nameof(boardSpecialSpriteCatalog.Bomb4Sprite));
            WarnIfMissingSprite(bomb7Sprite, nameof(boardSpecialSpriteCatalog.Bomb7Sprite));
            WarnIfMissingSprite(bombXXSprite, nameof(boardSpecialSpriteCatalog.BombXXSprite));
            WarnIfMissingSprite(bomb6SpriteA, nameof(boardSpecialSpriteCatalog.Bomb6SpriteA));
            WarnIfMissingSprite(bomb6SpriteB, nameof(boardSpecialSpriteCatalog.Bomb6SpriteB));
            WarnIfMissingSprite(treasureChestSprite, nameof(boardSpecialSpriteCatalog.TreasureChestSprite));
        }

        // CODEX BOMB TIERS: sprite selection per tier with safe fallback.
        private Sprite GetBombSpriteForTier(int bombTier)
        {
            CacheSpecialSpritesFromSceneAssets();
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
                Debug.LogWarning($"Missing board special sprite '{resourcePath}' in BoardSpecialSpriteCatalog.", this);
            }
        }


        private void CacheTileSpriteCatalogFromSceneAssets()
        {
            CacheSceneAssetLoader();
            if (sceneAssetLoader == null)
            {
                return;
            }

            tileSpriteCatalog = sceneAssetLoader.GetLoadedAsset<TileSpriteCatalog>();
            if (tileSpriteCatalog == null && debugMode)
            {
                Debug.LogWarning("TileSpriteCatalog not found in SceneAssetGroup.", this);
            }
        }

        private void CacheSceneAssetLoader()
        {
            if (sceneAssetLoader == null)
            {
                sceneAssetLoader = FindObjectOfType<SceneAssetLoader>();
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
