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

        [Header("References")]
        [SerializeField] private Piece piecePrefab;

        private Piece[,] pieces;
        private Sprite[] sprites;
        private bool isBusy;

        private readonly List<Piece> matchBuffer = new List<Piece>();

        private void Awake()
        {
            sprites = GenerateSprites();
        }

        private void Start()
        {
            pieces = new Piece[width, height];
            CreateBoard();
            StartCoroutine(ClearMatchesRoutine());
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

            StartCoroutine(SwapRoutine(first, second));
            return true;
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

            if (GameManager.Instance != null && !GameManager.Instance.TryUseMove())
            {
                isBusy = false;
                yield break;
            }

            yield return StartCoroutine(ClearMatchesRoutine());
            isBusy = false;
        }

        private void SwapPieces(Piece first, Piece second)
        {
            pieces[first.X, first.Y] = second;
            pieces[second.X, second.Y] = first;

            var firstX = first.X;
            var firstY = first.Y;

            first.SetPosition(second.X, second.Y, GridToWorld(second.X, second.Y));
            second.SetPosition(firstX, firstY, GridToWorld(firstX, firstY));
        }

        private List<Piece> FindMatches()
        {
            matchBuffer.Clear();

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
            while (matches.Count > 0)
            {
                ClearMatches(matches);
                yield return new WaitForSeconds(refillDelay);
                CollapseColumns();
                yield return new WaitForSeconds(refillDelay);
                RefillBoard();
                yield return new WaitForSeconds(refillDelay);
                matches = FindMatches();
            }
        }

        private void ClearMatches(List<Piece> matches)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(matches.Count);
            }

            foreach (var piece in matches)
            {
                if (piece == null)
                {
                    continue;
                }

                pieces[piece.X, piece.Y] = null;
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
    }
}
