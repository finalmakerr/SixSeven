using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private int startingMoves = 30;
        [SerializeField] private int scorePerPiece = 10;
        [SerializeField] private int startingLevelIndex = 0;
        [SerializeField] private Vector2Int fallbackGridSize = new Vector2Int(7, 7);
        [SerializeField] private int fallbackTargetScore = 500;
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private LevelManager levelManager;

        [Header("References")]
        [SerializeField] private Board board;

        [Header("UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text movesText;

        public static GameManager Instance { get; private set; }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public bool HasMetTarget { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (board == null)
            {
                board = FindObjectOfType<Board>();
            }
        }

        private void OnEnable()
        {
            if (levelDatabase == null)
            {
                levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
            }
        }

        private void Start()
        {
            if (levelManager != null)
            {
                levelManager.LoadStartingLevel(this);
                return;
            }

            LoadLevel(startingLevelIndex);
        }

        public void ResetGame()
        {
            Score = 0;
            MovesRemaining = startingMoves;
            HasMetTarget = false;
            UpdateUI();
        }

        public void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            var level = levelDatabase != null ? levelDatabase.GetLevel(levelIndex) : LevelDefinition.Default;

            startingMoves = level.moves > 0 ? level.moves : startingMoves;
            TargetScore = level.targetScore > 0 ? level.targetScore : fallbackTargetScore;
            var gridSize = level.gridSize == Vector2Int.zero ? fallbackGridSize : level.gridSize;

            if (board != null)
            {
                board.InitializeBoard(gridSize.x, gridSize.y);
            }

            ResetGame();
        }

        public bool LoadNextLevel()
        {
            if (levelManager != null)
            {
                return levelManager.LoadNextLevel(this);
            }

            LoadLevel(CurrentLevelIndex + 1);
            return true;
        }

        public void AddScore(int piecesCleared)
        {
            Score += piecesCleared * scorePerPiece;
            if (Score >= TargetScore)
            {
                HasMetTarget = true;
            }
            UpdateUI();
        }

        public bool TryUseMove()
        {
            if (MovesRemaining <= 0)
            {
                return false;
            }

            MovesRemaining -= 1;
            UpdateUI();
            return true;
        }

        private void UpdateUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {Score}";
            }

            if (movesText != null)
            {
                movesText.text = $"Moves: {MovesRemaining}";
            }
        }
    }
}
