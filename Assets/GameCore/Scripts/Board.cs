using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class Board : MonoBehaviour
    {
        // CODEX: LEVEL_LOOP
        public event Action<int, int> MatchesCleared;
        public event Action ValidSwap;
        // CODEX BOSS PR2
        public event Action<Vector2Int> OnBombDetonated;

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

        private Piece[,] pieces;
        private Sprite[] sprites;
        private bool isBusy;
        private bool hasInitialized;
        // CODEX: RNG_BAG
        private readonly List<int> colorBag = new List<int>();
        private System.Random randomGenerator;

        private readonly List<Piece> matchBuffer = new List<Piece>();
        private readonly HashSet<Vector2Int> specialCreationLogged = new HashSet<Vector2Int>(); // CODEX VERIFY 2: track special creation logs once per run.
        private readonly HashSet<Vector2Int> specialActivationLogged = new HashSet<Vector2Int>(); // CODEX VERIFY 2: prevent double activation logs per resolve step.
        // CODEX BOSS PR2
        private readonly HashSet<Vector2Int> bombCreationPositions = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> bombDetonationsThisStep = new HashSet<Vector2Int>();
        private int moveId; // CODEX VERIFY 2: monotonic id for accepted swaps.
        private int activeMoveId; // CODEX VERIFY 2: current move id for resolve diagnostics.

        public bool IsBusy => isBusy; // CODEX VERIFY: input lock gate for stable board state.
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
        }

        private void Start()
        {
            if (!hasInitialized)
            {
                InitializeBoard(width, height);
            }
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

        public bool IsSwapValid(Piece first, Piece second)
        {
            if (first == null || second == null || pieces == null)
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
            return hasMatch;
        }

        private IEnumerator SwapRoutine(Piece first, Piece second)
        {
            isBusy = true;
            SwapPieces(first, second);

            yield return new WaitForSeconds(0.05f);

            activeMoveId = moveId + 1; // CODEX VERIFY 2: stage upcoming move id for match diagnostics.
            var matches = FindMatches();
            if (matches.Count == 0)
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
            yield return StartCoroutine(ClearMatchesRoutine());
            if (debugMode)
            {
                Debug.Log($"MoveEnd({activeMoveId}): resolve complete.", this); // CODEX VERIFY 2: move end log once per accepted swap.
            }
            isBusy = false;
        }

        private void SwapPieces(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
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

            pieces[first.X, first.Y] = second;
            pieces[second.X, second.Y] = first;

            var firstX = first.X;
            var firstY = first.Y;

            first.SetPosition(second.X, second.Y, first.transform.position);
            second.SetPosition(firstX, firstY, second.transform.position);
        }

        private bool HasMatchAt(int x, int y)
        {
            var piece = pieces[x, y];
            if (piece == null)
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
                if (candidate == null || candidate.ColorIndex != colorIndex)
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
            specialCreationLogged.Clear(); // CODEX VERIFY 2: reset special creation logs per scan.
            specialActivationLogged.Clear(); // CODEX VERIFY 2: reset special activation logs per scan.
            // CODEX BOSS PR2
            bombCreationPositions.Clear();

            // Scan horizontally for runs of 3+ matching pieces.
            for (var y = 0; y < height; y++)
            {
                var runLength = 1;
                for (var x = 1; x < width; x++)
                {
                    var current = pieces[x, y];
                    var previous = pieces[x - 1, y];
                    if (current != null && previous != null && current.ColorIndex == previous.ColorIndex)
                    {
                        runLength++;
                    }
                    else
                    {
                        AddRunMatches(x - 1, y, runLength, Vector2Int.right);
                        runLength = 1;
                    }
                }

                AddRunMatches(width - 1, y, runLength, Vector2Int.right);
            }

            // Scan vertically for runs of 3+ matching pieces.
            for (var x = 0; x < width; x++)
            {
                var runLength = 1;
                for (var y = 1; y < height; y++)
                {
                    var current = pieces[x, y];
                    var previous = pieces[x, y - 1];
                    if (current != null && previous != null && current.ColorIndex == previous.ColorIndex)
                    {
                        runLength++;
                    }
                    else
                    {
                        AddRunMatches(x, y - 1, runLength, Vector2Int.up);
                        runLength = 1;
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

            // CODEX BOSS PR2
            Vector2Int? bombPosition = null;
            if (runLength >= 5 && ShouldAllowBombCreation())
            {
                var candidatePosition = new Vector2Int(endX, endY);
                var candidatePiece = pieces[endX, endY];
                if (candidatePiece != null
                    && candidatePiece.SpecialType == SpecialType.None
                    && bombCreationPositions.Add(candidatePosition))
                {
                    candidatePiece.SetSpecialType(SpecialType.Bomb);
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

        private IEnumerator ClearMatchesRoutine()
        {
            isBusy = true;
            var matches = FindMatches();
            var cascadeCount = 0;
            while (matches.Count > 0)
            {
                cascadeCount++;
                if (debugMode)
                {
                    Debug.Log($"CascadeCount({activeMoveId}): {cascadeCount}", this); // CODEX VERIFY 2: cascade instrumentation with move id.
                }
                // CODEX: LEVEL_LOOP
                MatchesCleared?.Invoke(matches.Count, cascadeCount);
                ClearMatches(matches);
                yield return new WaitForSeconds(refillDelay);
                CollapseColumns();
                yield return new WaitForSeconds(refillDelay);
                RefillBoard();
                yield return new WaitForSeconds(refillDelay);
                // Continue clearing until the board settles with no matches.
                matches = FindMatches();
            }

            EnsurePlayableBoard();
            isBusy = false;
        }

        private void ClearMatches(List<Piece> matches)
        {
            var clearedPositions = new HashSet<Vector2Int>();
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
                    DetonateBomb(position, clearedPositions);
                    continue;
                }

                if (IsInBounds(piece.X, piece.Y))
                {
                    clearedPositions.Add(position);
                }
            }

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

                pieces[position.x, position.y] = null;
                Destroy(piece.gameObject);
            }
        }

        // CODEX BOSS PR2
        private void DetonateBomb(Vector2Int position, HashSet<Vector2Int> clearedPositions)
        {
            if (!bombDetonationsThisStep.Add(position))
            {
                return;
            }

            OnBombDetonated?.Invoke(position);

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var x = position.x + dx;
                    var y = position.y + dy;
                    if (!IsInBounds(x, y))
                    {
                        continue;
                    }

                    var piece = pieces[x, y];
                    if (piece == null)
                    {
                        continue;
                    }

                    var targetPosition = new Vector2Int(x, y);
                    clearedPositions.Add(targetPosition);
                    if (piece.SpecialType == SpecialType.Bomb)
                    {
                        DetonateBomb(targetPosition, clearedPositions);
                    }
                }
            }
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

            var validMovesAfterShuffle = ShuffleBoard();
            if (debugMode)
            {
                Debug.Log($"ShuffleTriggered({activeMoveId}): validMoves={validMovesAfterShuffle}", this); // CODEX VERIFY 2: shuffle instrumentation with move id.
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
                    if (piece == null)
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
                    if (pieces[x, y] == null)
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
                return pieces[swapX2, swapY2] != null ? pieces[swapX2, swapY2].ColorIndex : -1;
            }

            if (x == swapX2 && y == swapY2)
            {
                return pieces[swapX1, swapY1] != null ? pieces[swapX1, swapY1].ColorIndex : -1;
            }

            return pieces[x, y] != null ? pieces[x, y].ColorIndex : -1;
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
