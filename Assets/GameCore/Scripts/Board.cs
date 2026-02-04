using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class Board : MonoBehaviour
    {
        // STAGE 3: Matches cleared with cascade count for combo scoring.
        public event Action<int, int> MatchesCleared;
        public event Action ValidSwap;
        public event Action OnBoardCleared;

        [Header("Board Settings")]
        [SerializeField] private int width = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private float spacing = 1.1f;
        [SerializeField] private int colorCount = 5;
        // STAGE 2: Delay timings for match confirmation, clears, and falls.
        [SerializeField] private float matchConfirmDelay = 0.12f;
        [SerializeField] private float clearDelay = 0.05f;
        [SerializeField] private float fallDelay = 0.10f;
        [SerializeField] private float swapDuration = 0.12f;
        [SerializeField] private float fallDuration = 0.1f;
        // STAGE 1
        [SerializeField] private float invalidShakeDuration = 0.06f;
        [SerializeField] private float invalidShakeMagnitude = 0.05f;
        // STAGE 5
        [SerializeField] private float matchPunchScale = 1.15f;
        [SerializeField] private float matchPunchDuration = 0.1f;

        [Header("References")]
        [SerializeField] private Piece piecePrefab;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        // STAGE 1
        [SerializeField] private AudioClip swapClip;
        [SerializeField] private AudioClip invalidSwapClip;
        [SerializeField] private AudioClip matchClearClip;
        [SerializeField] private AudioClip cascadeFallClip;
        [SerializeField] private AudioClip specialActivationClip;
        // STAGE 5
        [SerializeField] private float audioPitchVariance = 0.05f;

        // STAGE 0: Debug toggle for state transition logging.
        [Header("Debug")]
        [SerializeField] private bool logStateTransitions;

        private Piece[,] pieces;
        private Sprite[] sprites;

        // STAGE 0: Global busy gate for swap/resolve/refill.
        private bool isBusy;
        private bool hasInitialized;
        private const int MegaBombIndex = 6;
        private const int UltimateBombIndex = 7;
        // STAGE 6
        private HashSet<Vector2Int> pendingSwapPositions;
        // STAGE 7
        private const int ReshuffleAttemptLimit = 25;

        // STAGE 0: Read-only busy state for input gating.
        public bool IsBusy => isBusy;

        private class MatchGroup
        {
            public MatchGroup(List<Piece> pieces)
            {
                Pieces = pieces;
            }

            public List<Piece> Pieces { get; }

            public int Size => Pieces.Count;
        }

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
            // STAGE 0: Lock input during board reset/initial resolve.
            isBusy = true;
            ClearExistingPieces();
            pieces = new Piece[width, height];
            CreateBoard();
            StartCoroutine(ResolveBoardRoutine());
        }

        private void ClearExistingPieces()
        {
            if (pieces != null)
            {
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
                    CreatePiece(x, y, GetValidColorIndexForPosition(x, y));
                }
            }
        }

        private int GetRandomColorIndex()
        {
            return Random.Range(0, colorCount);
        }

        private int GetValidColorIndexForPosition(int x, int y)
        {
            var available = new List<int>();
            for (var color = 0; color < colorCount; color++)
            {
                available.Add(color);
            }

            var left1 = x - 1;
            var left2 = x - 2;
            if (left2 >= 0)
            {
                var first = pieces[left1, y];
                var second = pieces[left2, y];
                if (first != null && second != null && first.ColorIndex == second.ColorIndex)
                {
                    available.Remove(first.ColorIndex);
                }
            }

            var down1 = y - 1;
            var down2 = y - 2;
            if (down2 >= 0)
            {
                var first = pieces[x, down1];
                var second = pieces[x, down2];
                if (first != null && second != null && first.ColorIndex == second.ColorIndex)
                {
                    available.Remove(first.ColorIndex);
                }
            }

            if (available.Count == 0)
            {
                return GetRandomColorIndex();
            }

            return available[Random.Range(0, available.Count)];
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
                // STAGE 0: Atomic invalid swap gate.
                isBusy = true;
                StartCoroutine(InvalidSwapRoutine(first, second));
                return false;
            }

            ValidSwap?.Invoke();
            // STAGE 1
            StartCoroutine(SwapRoutine(first, second, _ => { }));
            // STAGE 0: Atomic swap gate + pipeline entry.
            isBusy = true;
            StartCoroutine(SwapAndResolveRoutine(first, second));
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

            if (IsMegaUltimateCombo(first, second))
            {
                return true;
            }

            SwapPiecesInGrid(first, second);
            var hasMatch = HasMatchAt(first.X, first.Y) || HasMatchAt(second.X, second.Y);
            SwapPiecesInGrid(first, second);
            return hasMatch;
        }

        // STAGE 1
        private IEnumerator SwapRoutine(Piece first, Piece second, Action<bool> onComplete)
        // STAGE 0: Swap -> Detect -> Resolve -> Refill -> Detect cascades -> Unlock input.
        private IEnumerator SwapAndResolveRoutine(Piece first, Piece second)
        {
            LogState("SwapStart");
            SwapPieces(first, second, swapDuration);
            PlayClip(swapClip);
            // STAGE 6: Track swap positions for RowClear creation.
            pendingSwapPositions = new HashSet<Vector2Int>
            {
                new Vector2Int(first.X, first.Y),
                new Vector2Int(second.X, second.Y)
            };

            yield return new WaitForSeconds(swapDuration);

            if (IsMegaUltimateCombo(first, second))
            {
                ClearBoard();
                SignalBoardCleared();
                SignalLevelWin();
                // STAGE 6
                pendingSwapPositions = null;
                isBusy = false;
                onComplete?.Invoke(true);
                LogState("Unlock");
                yield break;
            }

            var matchGroups = FindMatchGroups();
            if (matchGroups.Count == 0)
            {
                // STAGE 6
                pendingSwapPositions = null;
                // STAGE 1
                SwapPieces(first, second, swapDuration);
                PlayClip(invalidSwapClip);
                yield return new WaitForSeconds(swapDuration);
                yield return StartCoroutine(MicroShakePieces(first, second));
                isBusy = false;
                onComplete?.Invoke(false);
                LogState("Unlock");
                yield break;
            }

            // STAGE 3: Reset cascade count per player move.
            yield return StartCoroutine(ResolveBoardRoutine());
            isBusy = false;
            onComplete?.Invoke(true);
            LogState("Unlock");
        }

        private IEnumerator InvalidSwapRoutine(Piece first, Piece second)
        {
            if (isBusy)
            {
                yield break;
            }

            isBusy = true;
            // STAGE 1
            SwapPieces(first, second, swapDuration);
            PlayClip(invalidSwapClip);
            yield return new WaitForSeconds(swapDuration);
            SwapPieces(first, second, swapDuration);
            yield return new WaitForSeconds(swapDuration);
            yield return StartCoroutine(MicroShakePieces(first, second));
            LogState("InvalidSwap");
            SwapPieces(first, second, invalidSwapDuration);
            PlayClip(swapClip);
            yield return new WaitForSeconds(invalidSwapDuration);
            SwapPieces(first, second, invalidSwapDuration);
            yield return new WaitForSeconds(invalidSwapDuration);
            isBusy = false;
            LogState("Unlock");
        }

        // STAGE 1
        private IEnumerator MicroShakePieces(Piece first, Piece second)
        {
            if (first != null)
            {
                first.MicroShake(invalidShakeDuration, invalidShakeMagnitude);
            }

            if (second != null)
            {
                second.MicroShake(invalidShakeDuration, invalidShakeMagnitude);
            }

            yield return new WaitForSeconds(invalidShakeDuration);
        }

        private void SwapPieces(Piece first, Piece second, float duration)
        {
            if (first == null || second == null)
            {
                return;
            }

            pieces[first.X, first.Y] = second;
            pieces[second.X, second.Y] = first;

            var firstX = first.X;
            var firstY = first.Y;

            first.UpdateGridPosition(second.X, second.Y);
            second.UpdateGridPosition(firstX, firstY);

            first.MoveTo(GridToWorld(second.X, second.Y), duration);
            second.MoveTo(GridToWorld(firstX, firstY), duration);
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

            first.UpdateGridPosition(second.X, second.Y);
            second.UpdateGridPosition(firstX, firstY);
        }

        private bool HasMatchAt(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

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

        private List<MatchGroup> FindMatchGroups()
        {
            var matched = new bool[width, height];

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
                        MarkRunMatches(matched, x - 1, y, runLength, Vector2Int.right);
                        runLength = 1;
                    }
                }

                MarkRunMatches(matched, width - 1, y, runLength, Vector2Int.right);
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
                        MarkRunMatches(matched, x, y - 1, runLength, Vector2Int.up);
                        runLength = 1;
                    }
                }

                MarkRunMatches(matched, x, height - 1, runLength, Vector2Int.up);
            }

            var visited = new bool[width, height];
            var groups = new List<MatchGroup>();
            var queue = new Queue<Vector2Int>();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (!matched[x, y] || visited[x, y])
                    {
                        continue;
                    }

                    var groupPieces = new List<Piece>();
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        var piece = pieces[current.x, current.y];
                        if (piece != null)
                        {
                            groupPieces.Add(piece);
                        }

                        TryEnqueueMatchNeighbor(current.x + 1, current.y, matched, visited, queue);
                        TryEnqueueMatchNeighbor(current.x - 1, current.y, matched, visited, queue);
                        TryEnqueueMatchNeighbor(current.x, current.y + 1, matched, visited, queue);
                        TryEnqueueMatchNeighbor(current.x, current.y - 1, matched, visited, queue);
                    }

                    if (groupPieces.Count > 0)
                    {
                        groups.Add(new MatchGroup(groupPieces));
                    }
                }
            }

            groups.Sort((first, second) => second.Size.CompareTo(first.Size));
            return groups;
        }

        private void MarkRunMatches(bool[,] matched, int endX, int endY, int runLength, Vector2Int direction)
        {
            if (runLength < 3)
            {
                return;
            }

            for (var i = 0; i < runLength; i++)
            {
                var x = endX - direction.x * i;
                var y = endY - direction.y * i;
                matched[x, y] = true;
            }
        }

        private void TryEnqueueMatchNeighbor(int x, int y, bool[,] matched, bool[,] visited, Queue<Vector2Int> queue)
        {
            if (!IsInBounds(x, y) || visited[x, y] || !matched[x, y])
            {
                return;
            }

            visited[x, y] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        // STAGE 2: Resolve loop enforces detect -> pause -> clear -> pause -> collapse -> pause -> refill.
        // STAGE 3: Cascade count increments per clear batch during resolve.
        private IEnumerator ResolveBoardRoutine()
        {
            LogState("ResolveStart");
            var matchGroups = FindMatchGroups();
            var cascadeCount = 0;
            // STAGE 6: Only use the swap positions for the first resolve batch.
            var swapPositions = pendingSwapPositions;
            pendingSwapPositions = null;
            while (matchGroups.Count > 0)
            {
                cascadeCount++;
                // STAGE 5: Punch-scale matched tiles before clearing.
                TriggerMatchPunch(matchGroups);
                yield return new WaitForSeconds(matchConfirmDelay);
                var protectedPieces = CreateSpecialTiles(matchGroups, swapPositions);
                swapPositions = null;
                var clearedCount = ClearMatches(matchGroups, protectedPieces);
                if (clearedCount > 0)
                {
                    MatchesCleared?.Invoke(clearedCount, cascadeCount);
                    // STAGE 2: Play clear audio per clear batch.
                    PlayClip(matchClearClip);
                }

                yield return new WaitForSeconds(clearDelay);
                // STAGE 2: Falling begins with a cascade clip.
                PlayClip(cascadeFallClip);
                CollapseColumns();
                yield return new WaitForSeconds(fallDelay);
                RefillBoard();
                yield return new WaitForSeconds(fallDelay);

                matchGroups = FindMatchGroups();
                // STAGE 7: Dead board detection after refills.
                if (matchGroups.Count == 0 && !HasAnyValidMoves())
                {
                    yield return StartCoroutine(ReshuffleRoutine(true));
                    matchGroups = FindMatchGroups();
                }
            }

            LogState("ResolveComplete");
        }

        private int ClearMatches(List<MatchGroup> matchGroups, HashSet<Piece> protectedPieces)
        {
            var clearedCount = 0;
            var uniqueMatches = new HashSet<Piece>();

            foreach (var group in matchGroups)
            {
                foreach (var piece in group.Pieces)
                {
                    if (piece != null)
                    {
                        uniqueMatches.Add(piece);
                    }
                }
            }

            var rowClearSources = new List<Piece>();
            var rowClearAffected = new HashSet<Piece>();
            var clearSet = new HashSet<Piece>();

            foreach (var piece in uniqueMatches)
            {
                if (piece == null)
                {
                    continue;
                }

                if (protectedPieces.Contains(piece))
                {
                    continue;
                }

                if (piece.Special == Piece.SpecialType.RowClear)
                {
                    rowClearSources.Add(piece);
                }

                if (piece.Special == Piece.SpecialType.None || piece.Special == Piece.SpecialType.RowClear)
                {
                    clearSet.Add(piece);
                }
            }

            foreach (var rowClear in rowClearSources)
            {
                TriggerSpecialActivation(rowClear);
                for (var x = 0; x < width; x++)
                {
                    var candidate = pieces[x, rowClear.Y];
                    if (candidate == null || protectedPieces.Contains(candidate))
                    {
                        continue;
                    }

                    clearSet.Add(candidate);
                    rowClearAffected.Add(candidate);
                }
            }

            foreach (var piece in clearSet)
            {
                if (piece == null || protectedPieces.Contains(piece))
                {
                    continue;
                }

                if (piece.Special != Piece.SpecialType.None
                    && piece.Special != Piece.SpecialType.RowClear
                    && !rowClearAffected.Contains(piece))
                {
                    continue;
                }

                if (IsInBounds(piece.X, piece.Y))
                {
                    pieces[piece.X, piece.Y] = null;
                }

                Destroy(piece.gameObject);
                clearedCount++;
            }

            return clearedCount;
        }

        private HashSet<Piece> CreateSpecialTiles(List<MatchGroup> matchGroups, HashSet<Vector2Int> swapPositions)
        {
            var protectedPieces = new HashSet<Piece>();
            foreach (var group in matchGroups)
            {
                var specialType = GetSpecialTypeForMatch(group.Pieces.Count);
                if (specialType == Piece.SpecialType.None)
                {
                    continue;
                }

                var candidate = SelectSpecialCandidate(group.Pieces, specialType, swapPositions);

                if (candidate == null)
                {
                    continue;
                }

                candidate.SetSpecialType(specialType);
                protectedPieces.Add(candidate);
            }

            return protectedPieces;
        }

        private Piece.SpecialType GetSpecialTypeForMatch(int matchSize)
        {
            return matchSize switch
            {
                // STAGE 6
                4 => Piece.SpecialType.RowClear,
                5 => Piece.SpecialType.StrongBomb,
                6 => Piece.SpecialType.MegaBomb,
                >= 7 => Piece.SpecialType.UltimateBomb,
                _ => Piece.SpecialType.None
            };
        }

        // STAGE 6
        private Piece SelectSpecialCandidate(List<Piece> candidates, Piece.SpecialType specialType, HashSet<Vector2Int> swapPositions)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            if (specialType == Piece.SpecialType.RowClear && swapPositions != null)
            {
                foreach (var piece in candidates)
                {
                    if (piece == null || piece.Special != Piece.SpecialType.None)
                    {
                        continue;
                    }

                    if (swapPositions.Contains(new Vector2Int(piece.X, piece.Y)))
                    {
                        return piece;
                    }
                }
            }

            Piece fallback = null;
            foreach (var piece in candidates)
            {
                if (piece == null || piece.Special != Piece.SpecialType.None)
                {
                    continue;
                }

                if (fallback == null
                    || piece.Y < fallback.Y
                    || (piece.Y == fallback.Y && piece.X < fallback.X))
                {
                    fallback = piece;
                }
            }

            return fallback;
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
                        piece.UpdateGridPosition(x, nextEmptyY);
                        piece.MoveTo(GridToWorld(x, nextEmptyY), fallDuration);
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
                        var newPiece = CreatePiece(x, y, GetRandomColorIndex());
                        var spawnPosition = GridToWorld(x, height + 1);
                        newPiece.transform.position = spawnPosition;
                        newPiece.MoveTo(GridToWorld(x, y), fallDuration);
                    }
                }
            }
        }

        // STAGE 7
        public bool HasAnyValidMoves()
        {
            if (pieces == null)
            {
                return false;
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (piece == null)
                    {
                        continue;
                    }

                    if (x + 1 < width && IsSwapValid(piece, pieces[x + 1, y]))
                    {
                        return true;
                    }

                    if (y + 1 < height && IsSwapValid(piece, pieces[x, y + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // STAGE 7
        public void DebugReshuffle()
        {
            if (isBusy)
            {
                return;
            }

            StartCoroutine(ReshuffleRoutine(true));
        }

        // STAGE 7
        private IEnumerator ReshuffleRoutine(bool avoidImmediateMatches)
        {
            var wasBusy = isBusy;
            isBusy = true;

            var piecePool = new List<Piece>();
            var positions = new List<Vector2Int>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (pieces[x, y] == null)
                    {
                        continue;
                    }

                    piecePool.Add(pieces[x, y]);
                    positions.Add(new Vector2Int(x, y));
                }
            }

            if (piecePool.Count == 0 || positions.Count == 0)
            {
                isBusy = wasBusy;
                yield break;
            }

            var attempt = 0;
            while (attempt < ReshuffleAttemptLimit)
            {
                attempt++;
                ShuffleList(piecePool);
                AssignShuffledPieces(piecePool, positions);

                if (avoidImmediateMatches && FindMatchGroups().Count > 0)
                {
                    continue;
                }

                if (!HasAnyValidMoves())
                {
                    continue;
                }

                break;
            }

            var ensureMoveAttempt = 0;
            while (!HasAnyValidMoves() && ensureMoveAttempt < ReshuffleAttemptLimit)
            {
                ensureMoveAttempt++;
                ShuffleList(piecePool);
                AssignShuffledPieces(piecePool, positions);
            }

            foreach (var piece in piecePool)
            {
                if (piece == null)
                {
                    continue;
                }

                piece.MoveTo(GridToWorld(piece.X, piece.Y), fallDuration);
            }

            yield return new WaitForSeconds(fallDuration);
            isBusy = wasBusy;
        }

        // STAGE 7
        private void ShuffleList(List<Piece> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        // STAGE 7
        private void AssignShuffledPieces(List<Piece> piecePool, List<Vector2Int> positions)
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    pieces[x, y] = null;
                }
            }

            for (var i = 0; i < piecePool.Count && i < positions.Count; i++)
            {
                var piece = piecePool[i];
                var position = positions[i];
                pieces[position.x, position.y] = piece;
                if (piece != null)
                {
                    piece.UpdateGridPosition(position.x, position.y);
                }
            }
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private bool IsMegaUltimateCombo(Piece first, Piece second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            var firstIndex = first.ColorIndex;
            var secondIndex = second.ColorIndex;
            return (firstIndex == MegaBombIndex && secondIndex == UltimateBombIndex)
                || (firstIndex == UltimateBombIndex && secondIndex == MegaBombIndex);
        }

        private void ClearBoard()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var piece = pieces[x, y];
                    if (piece == null)
                    {
                        continue;
                    }

                    pieces[x, y] = null;
                    Destroy(piece.gameObject);
                }
            }
        }

        private void SignalBoardCleared()
        {
            OnBoardCleared?.Invoke();
        }

        private void SignalLevelWin()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.TriggerInstantWin();
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

        public void TriggerSpecialActivation(Piece piece)
        {
            if (piece == null)
            {
                return;
            }

            PlayClip(specialActivationClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            // STAGE 5: Slight random pitch variation.
            var originalPitch = audioSource.pitch;
            var pitchJitter = Mathf.Clamp(audioPitchVariance, 0f, 1f);
            audioSource.pitch = Random.Range(1f - pitchJitter, 1f + pitchJitter);
            audioSource.PlayOneShot(clip);
            audioSource.pitch = originalPitch;
        }

        // STAGE 5
        private void TriggerMatchPunch(List<MatchGroup> matchGroups)
        {
            if (matchGroups == null || matchGroups.Count == 0)
            {
                return;
            }

            var uniqueMatches = new HashSet<Piece>();
            foreach (var group in matchGroups)
            {
                if (group?.Pieces == null)
                {
                    continue;
                }

                foreach (var piece in group.Pieces)
                {
                    if (piece != null)
                    {
                        uniqueMatches.Add(piece);
                    }
                }
            }

            foreach (var piece in uniqueMatches)
            {
                piece.PunchScale(matchPunchScale, matchPunchDuration);
            }
        }

        // STAGE 0: Optional debug output for board pipeline state.
        private void LogState(string state)
        {
            if (!logStateTransitions)
            {
                return;
            }

            Debug.Log($"[Board] State -> {state}", this);
        }
    }
}
