using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class Board : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int width = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private float spacing = 1.1f;
        [SerializeField] private int colorCount = 5;
        [SerializeField] private float refillDelay = 0.1f;
        [SerializeField] private float swapDuration = 0.12f;
        [SerializeField] private float invalidSwapDuration = 0.1f;
        [SerializeField] private float fallDuration = 0.1f;

        [Header("References")]
        [SerializeField] private Piece piecePrefab;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip swapClip;
        [SerializeField] private AudioClip matchClearClip;
        [SerializeField] private AudioClip cascadeFallClip;
        [SerializeField] private AudioClip specialActivationClip;

        private Piece[,] pieces;
        private Sprite[] sprites;
        private bool isBusy;
        private bool hasInitialized;
        private readonly List<Piece> matchBuffer = new List<Piece>();
        private readonly List<MatchGroup> matchGroupsBuffer = new List<MatchGroup>();

        private class MatchGroup
        {
            public List<Piece> Pieces { get; }

            public MatchGroup(List<Piece> pieces)
            {
                Pieces = pieces;
            }
        }

        private void Awake()
        {
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
            width = Mathf.Max(3, newWidth);
            height = Mathf.Max(3, newHeight);
            hasInitialized = true;
            ResetBoardState();
        }

        private void ResetBoardState()
        {
            StopAllCoroutines();
            isBusy = false;
            ClearExistingPieces();
            pieces = new Piece[width, height];
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
                    CreatePiece(x, y, GetRandomColorIndex());
                }
            }
        }

        private int GetRandomColorIndex()
        {
            return Random.Range(0, colorCount);
        }

        private Piece CreatePiece(int x, int y, int colorIndex)
        {
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
            if (isBusy)
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
                StartCoroutine(InvalidSwapRoutine(first, second));
                return false;
            }

            StartCoroutine(SwapRoutine(first, second));
            return true;
        }

        public bool IsSwapValid(Piece first, Piece second)
        {
            if (first == null || second == null)
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
            SwapPieces(first, second, swapDuration);
            PlayClip(swapClip);

            yield return new WaitForSeconds(swapDuration);

            var matches = FindMatches();
            if (matches.Count == 0)
            {
                SwapPieces(first, second, invalidSwapDuration);
                yield return new WaitForSeconds(invalidSwapDuration);
                isBusy = false;
                yield break;
            }

            yield return StartCoroutine(ClearMatchesRoutine());
            isBusy = false;
        }

        private IEnumerator InvalidSwapRoutine(Piece first, Piece second)
        {
            if (isBusy)
            {
                yield break;
            }

            isBusy = true;
            SwapPieces(first, second, invalidSwapDuration);
            PlayClip(swapClip);
            yield return new WaitForSeconds(invalidSwapDuration);
            SwapPieces(first, second, invalidSwapDuration);
            yield return new WaitForSeconds(invalidSwapDuration);
            isBusy = false;
        }

        private void SwapPieces(Piece first, Piece second, float duration)
        {
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
            pieces[first.X, first.Y] = second;
            pieces[second.X, second.Y] = first;

            var firstX = first.X;
            var firstY = first.Y;

            first.UpdateGridPosition(second.X, second.Y);
            second.UpdateGridPosition(firstX, firstY);
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
            FindMatchGroups();
            return matchBuffer;
        }

        private List<MatchGroup> FindMatchGroups()
        {
            matchBuffer.Clear();
            matchGroupsBuffer.Clear();

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

            return matchGroupsBuffer;
        }

        private void AddRunMatches(int endX, int endY, int runLength, Vector2Int direction)
        {
            if (runLength < 3)
            {
                return;
            }

            var runPieces = new List<Piece>(runLength);

            for (var i = 0; i < runLength; i++)
            {
                var x = endX - direction.x * i;
                var y = endY - direction.y * i;
                var piece = pieces[x, y];
                if (piece == null)
                {
                    continue;
                }

                runPieces.Add(piece);
                if (!matchBuffer.Contains(piece))
                {
                    matchBuffer.Add(piece);
                }
            }

            if (runPieces.Count >= 3)
            {
                matchGroupsBuffer.Add(new MatchGroup(runPieces));
            }
        }

        private IEnumerator ClearMatchesRoutine()
        {
            var matchGroups = FindMatchGroups();
            while (matchGroups.Count > 0)
            {
                var protectedPieces = CreateSpecialTiles(matchGroups);
                ClearMatches(matchBuffer, protectedPieces);
            while (true)
            {
                ClearMatches(matches);
                PlayClip(matchClearClip);
                var matchGroups = FindMatchGroups();
                if (matchGroups.Count == 0)
                {
                    yield break;
                }

                ClearMatches(matchGroups[0].Pieces);
                yield return new WaitForSeconds(refillDelay);
                CollapseColumns();
                PlayClip(cascadeFallClip);
                yield return new WaitForSeconds(refillDelay);
                RefillBoard();
                yield return new WaitForSeconds(refillDelay);
                matchGroups = FindMatchGroups();
            }
        }

        private void ClearMatches(List<Piece> matches, HashSet<Piece> protectedPieces)
        {
            foreach (var piece in matches)
            {
                if (piece == null)
                {
                    continue;
                }

                if (piece.Special != Piece.SpecialType.None)
                {
                    continue;
                }

                if (protectedPieces.Contains(piece))
                {
                    continue;
                }

                pieces[piece.X, piece.Y] = null;
                Destroy(piece.gameObject);
            }
        }

        private HashSet<Piece> CreateSpecialTiles(List<MatchGroup> matchGroups)
        {
            var protectedPieces = new HashSet<Piece>();
            foreach (var group in matchGroups)
            {
                var specialType = GetSpecialTypeForMatch(group.Pieces.Count);
                if (specialType == Piece.SpecialType.None)
                {
                    continue;
                }

                Piece candidate = null;
                foreach (var piece in group.Pieces)
                {
                    if (piece == null)
                    {
                        continue;
                    }

                    if (piece.Special != Piece.SpecialType.None)
        private List<MatchGroup> FindMatchGroups()
        {
            var matched = new bool[width, height];

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

                    if (protectedPieces.Contains(piece))
                    {
                        continue;
                    }

                    candidate = piece;
                    break;
                }

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
                4 => Piece.SpecialType.Bomb,
                5 => Piece.SpecialType.StrongBomb,
                6 => Piece.SpecialType.MegaBomb,
                >= 7 => Piece.SpecialType.UltimateBomb,
                _ => Piece.SpecialType.None
            };
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

        private class MatchGroup
        {
            public MatchGroup(List<Piece> pieces)
            {
                Pieces = pieces;
            }

            public List<Piece> Pieces { get; }

            public int Size => Pieces.Count;
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

            audioSource.PlayOneShot(clip);
        }
    }
}
