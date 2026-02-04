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

        [Header("Board Settings")]
        [SerializeField] private int width = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private float spacing = 1.1f;
        [SerializeField] private int colorCount = 5;
        [SerializeField] private float refillDelay = 0.1f;

        [Header("References")]
        [SerializeField] private Piece piecePrefab;

        private Piece[,] pieces;
        private Sprite[] sprites;
        private bool isBusy;
        private bool hasInitialized;

        private readonly List<Piece> matchBuffer = new List<Piece>();

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

            var matches = FindMatches();
            if (matches.Count == 0)
            {
                SwapPieces(first, second);
                isBusy = false;
                yield break;
            }

            // CODEX: LEVEL_LOOP
            ValidSwap?.Invoke();
            yield return StartCoroutine(ClearMatchesRoutine());
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

            for (var i = 0; i < runLength; i++)
            {
                var x = endX - direction.x * i;
                var y = endY - direction.y * i;
                var piece = pieces[x, y];
                if (piece != null && !matchBuffer.Contains(piece))
                {
                    matchBuffer.Add(piece);
                }
            }
        }

        private IEnumerator ClearMatchesRoutine()
        {
            var matches = FindMatches();
            var cascadeCount = 0;
            while (matches.Count > 0)
            {
                cascadeCount++;
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
        }

        private void ClearMatches(List<Piece> matches)
        {
            foreach (var piece in matches)
            {
                if (piece == null)
                {
                    continue;
                }

                if (IsInBounds(piece.X, piece.Y))
                {
                    pieces[piece.X, piece.Y] = null;
                }
                Destroy(piece.gameObject);
            }
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
                        CreatePiece(x, y, GetRandomColorIndex());
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
