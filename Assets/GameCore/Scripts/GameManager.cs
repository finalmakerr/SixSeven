using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        public event Action OnWin;
        public event Action OnLose;

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
        // STAGE 3: Assign a Text element for combo callouts (e.g., "COMBO x2") in the Inspector.
        [SerializeField] private Text comboText;
        // STAGE 3: Duration to display combo callouts.
        [SerializeField] private float comboDisplayDuration = 0.75f;

        public static GameManager Instance { get; private set; }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public bool HasMetTarget { get; private set; }

        private bool hasEnded;
        private Coroutine comboRoutine;

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

            RegisterBoardEvents();
        }

        private void OnDisable()
        {
            UnregisterBoardEvents();
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
            hasEnded = false;
            HideComboText();
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

        // STAGE 3: Apply cascade multiplier to match scoring.
        public void AddScore(int piecesCleared, int cascadeCount)
        {
            var baseMatchScore = piecesCleared * scorePerPiece;
            Score += baseMatchScore * Mathf.Max(1, cascadeCount);
            if (Score >= TargetScore)
            {
                HasMetTarget = true;
            }
            UpdateUI();

            if (HasMetTarget && !hasEnded)
            {
                TriggerWin();
            }
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

        private void RegisterBoardEvents()
        {
            if (board == null)
            {
                return;
            }

            board.MatchesCleared += HandleMatchesCleared;
            board.ValidSwap += HandleValidSwap;
        }

        private void UnregisterBoardEvents()
        {
            if (board == null)
            {
                return;
            }

            board.MatchesCleared -= HandleMatchesCleared;
            board.ValidSwap -= HandleValidSwap;
        }

        private void HandleMatchesCleared(int clearedCount, int cascadeCount)
        {
            if (hasEnded)
            {
                return;
            }

            AddScore(clearedCount, cascadeCount);
            if (cascadeCount >= 2)
            {
                ShowComboText(cascadeCount);
            }
        }

        private void HandleValidSwap()
        {
            if (hasEnded)
            {
                return;
            }

            TryUseMove();
            if (MovesRemaining <= 0 && !HasMetTarget)
            {
                TriggerLose();
            }
        }

        private void TriggerWin()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            OnWin?.Invoke();
        }

        private void TriggerLose()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            OnLose?.Invoke();
        }

        public void TriggerInstantWin()
        {
            HasMetTarget = true;
            if (Score < TargetScore)
            {
                Score = TargetScore;
            }
            UpdateUI();
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

        // STAGE 3: Combo callout helper.
        private void ShowComboText(int cascadeCount)
        {
            if (comboText == null)
            {
                return;
            }

            if (comboRoutine != null)
            {
                StopCoroutine(comboRoutine);
            }

            comboRoutine = StartCoroutine(ComboTextRoutine(cascadeCount));
        }

        private IEnumerator ComboTextRoutine(int cascadeCount)
        {
            comboText.text = $"COMBO x{cascadeCount}";
            comboText.enabled = true;
            yield return new WaitForSeconds(comboDisplayDuration);
            HideComboText();
        }

        private void HideComboText()
        {
            if (comboText == null)
            {
                return;
            }

            comboText.enabled = false;
        }
    }
}
