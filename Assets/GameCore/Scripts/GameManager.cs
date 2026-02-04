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
        // CODEX: LEVEL_LOOP
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        // CODEX BOSS PR1
        [SerializeField] private Text bossLabelText;
        // STAGE 3: Assign a Text element for combo callouts (e.g., "COMBO x2") in the Inspector.
        [SerializeField] private Text comboText;
        // STAGE 5: Optional camera transform for screen shake.
        [SerializeField] private Transform screenShakeTarget;
        // STAGE 3: Duration to display combo callouts.
        [SerializeField] private float comboDisplayDuration = 0.75f;
        // STAGE 5: Score count-up duration.
        [SerializeField] private float scoreLerpDuration = 0.25f;
        // STAGE 5: Big clear screen shake settings.
        [SerializeField] private int bigClearThreshold = 5;
        [SerializeField] private float bigClearShakeDuration = 0.1f;
        [SerializeField] private float bigClearShakeMagnitude = 0.08f;
        [SerializeField] private bool debugMode; // CODEX VERIFY: toggle lightweight stability instrumentation.

        public static GameManager Instance { get; private set; }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public bool HasMetTarget { get; private set; }
        // CODEX: LEVEL_LOOP
        public int MovesLimit { get; private set; }
        // CODEX BOSS PR1
        public bool IsBossLevel { get; private set; }
        // CODEX BOSS PR1
        public BossState CurrentBossState { get; private set; }

        private bool hasEnded;
        private Coroutine comboRoutine;
        // STAGE 5
        private Coroutine scoreRoutine;
        private Coroutine shakeRoutine;
        private int displayedScore;

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

            if (screenShakeTarget == null && Camera.main != null)
            {
                screenShakeTarget = Camera.main.transform;
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
            // CODEX: LEVEL_LOOP
            MovesRemaining = MovesLimit > 0 ? MovesLimit : startingMoves;
            HasMetTarget = false;
            hasEnded = false;
            HideComboText();
            displayedScore = Score;
            // CODEX: LEVEL_LOOP
            SetEndPanels(false, false);
            UpdateUI();
        }

        public void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            var level = levelDatabase != null ? levelDatabase.GetLevel(levelIndex) : LevelDefinition.Default;

            // CODEX: LEVEL_LOOP
            MovesLimit = level.movesLimit > 0 ? level.movesLimit : startingMoves;
            TargetScore = level.targetScore > 0 ? level.targetScore : fallbackTargetScore;
            var gridSize = level.gridSize == Vector2Int.zero ? fallbackGridSize : level.gridSize;
            // CODEX BOSS PR1
            var isBossLevel = (levelIndex + 1) % 6 == 0;
            level.isBossLevel = isBossLevel;
            IsBossLevel = isBossLevel;
            // CODEX BOSS PR1
            CurrentBossState = new BossState
            {
                bossPosition = new Vector2Int(gridSize.x / 2, gridSize.y / 2),
                bossAlive = isBossLevel
            };

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
            if (debugMode)
            {
                Debug.Log($"MovesRemaining: {MovesRemaining}", this); // CODEX VERIFY: move counter log.
            }
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
            // CODEX BOSS PR3
            board.OnBombDetonated += HandleBombDetonated;
        }

        private void UnregisterBoardEvents()
        {
            if (board == null)
            {
                return;
            }

            board.MatchesCleared -= HandleMatchesCleared;
            board.ValidSwap -= HandleValidSwap;
            // CODEX BOSS PR3
            board.OnBombDetonated -= HandleBombDetonated;
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

            if (clearedCount >= bigClearThreshold)
            {
                TriggerScreenShake();
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

        // CODEX BOSS PR3
        private void HandleBombDetonated(Vector2Int position)
        {
            if (hasEnded || !IsBossLevel)
            {
                return;
            }

            var bossState = CurrentBossState;
            if (!bossState.bossAlive)
            {
                return;
            }

            if (DistanceManhattan(position, bossState.bossPosition) > 2)
            {
                return;
            }

            bossState.bossAlive = false;
            CurrentBossState = bossState;

            if (debugMode)
            {
                Debug.Log($"BossDefeated by bomb at {position.x},{position.y}", this);
            }

            TriggerInstantWin();
            TriggerWin();
        }

        private void TriggerWin()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            // CODEX: LEVEL_LOOP
            SetEndPanels(true, false);
            OnWin?.Invoke();
        }

        private void TriggerLose()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            // CODEX: LEVEL_LOOP
            SetEndPanels(false, true);
            OnLose?.Invoke();
        }

        // CODEX BOSS PR3
        private static int DistanceManhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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
            UpdateScoreText();

            if (movesText != null)
            {
                // CODEX: LEVEL_LOOP
                movesText.text = MovesLimit > 0
                    ? $"Moves: {MovesRemaining}/{MovesLimit}"
                    : $"Moves: {MovesRemaining}";
            }

            // CODEX BOSS PR1
            if (bossLabelText != null)
            {
                bossLabelText.text = "BOSS";
                bossLabelText.enabled = IsBossLevel;
            }
        }

        // STAGE 5: Score count-up helper.
        private void UpdateScoreText()
        {
            if (scoreText == null)
            {
                return;
            }

            if (scoreRoutine != null)
            {
                StopCoroutine(scoreRoutine);
            }

            if (scoreLerpDuration <= 0f)
            {
                displayedScore = Score;
                // CODEX: LEVEL_LOOP
                scoreText.text = $"Score: {Score}/{TargetScore}";
                return;
            }

            scoreRoutine = StartCoroutine(ScoreLerpRoutine(displayedScore, Score, scoreLerpDuration));
        }

        // STAGE 5
        private IEnumerator ScoreLerpRoutine(int startScore, int endScore, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var lerpedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
                displayedScore = lerpedScore;
                // CODEX: LEVEL_LOOP
                scoreText.text = $"Score: {lerpedScore}/{TargetScore}";
                yield return null;
            }

            displayedScore = endScore;
            // CODEX: LEVEL_LOOP
            scoreText.text = $"Score: {endScore}/{TargetScore}";
            scoreRoutine = null;
        }

        // CODEX: LEVEL_LOOP
        private void SetEndPanels(bool showWin, bool showLose)
        {
            if (winPanel != null)
            {
                winPanel.SetActive(showWin);
            }

            if (losePanel != null)
            {
                losePanel.SetActive(showLose);
            }
        }

        // STAGE 5
        private void TriggerScreenShake()
        {
            if (screenShakeTarget == null)
            {
                return;
            }

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            shakeRoutine = StartCoroutine(ScreenShakeRoutine(bigClearShakeDuration, bigClearShakeMagnitude));
        }

        // STAGE 5
        private IEnumerator ScreenShakeRoutine(float duration, float magnitude)
        {
            var basePosition = screenShakeTarget.localPosition;
            if (duration <= 0f || magnitude <= 0f)
            {
                screenShakeTarget.localPosition = basePosition;
                shakeRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var offset = (Vector3)UnityEngine.Random.insideUnitCircle * magnitude;
                screenShakeTarget.localPosition = basePosition + offset;
                yield return null;
            }

            screenShakeTarget.localPosition = basePosition;
            shakeRoutine = null;
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
