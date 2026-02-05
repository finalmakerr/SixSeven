using System;
using System.Collections;
using System.Collections.Generic;
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
        // CODEX BOSS PR1
        [SerializeField] private BossManager bossManager;

        [Header("UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text movesText;
        // CODEX: LEVEL_LOOP
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        // CODEX BOSS PR1
        [SerializeField] private Text bossLabelText;
        // CODEX BOSS PR4
        [SerializeField] private Text bossPowersText;
        // CODEX BOSS PR2
        [SerializeField] private GameObject bossChallengePanel;
        // CODEX BOSS PR2
        [SerializeField] private Text bossChallengePromptText;
        // CODEX BOSS PR2
        [SerializeField] private Text bossChallengeWarningText;
        // CODEX BOSS PR2
        [SerializeField] private Button bossChallengeFightButton;
        // CODEX BOSS PR2
        [SerializeField] private Button bossChallengeSkipButton;
        // CODEX BOSS PR5
        [SerializeField] private GameObject bossPowerDiscardPanel;
        // CODEX BOSS PR5
        [SerializeField] private Button[] bossPowerDiscardButtons;
        // CODEX WIPE PR6
        [SerializeField] private GameObject bossPowerDiscardConfirmPanel;
        // CODEX WIPE PR6
        [SerializeField] private Text bossPowerDiscardConfirmText;
        // CODEX WIPE PR6
        [SerializeField] private Button bossPowerDiscardConfirmYesButton;
        // CODEX WIPE PR6
        [SerializeField] private Button bossPowerDiscardConfirmNoButton;
        // CODEX POWER PR5
        [SerializeField] private GameObject bossPowerRewardPanel;
        // CODEX POWER PR5
        [SerializeField] private Text bossPowerRewardPromptText;
        // CODEX POWER PR5
        [SerializeField] private Button[] bossPowerRewardButtons;
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
        public bool IsOptionalBossLevel { get; private set; }
        // CODEX BOSS PR1
        public BossState CurrentBossState { get; private set; }
        // CODEX BOSS PR1
        public BossDefinition CurrentBoss => bossManager != null ? bossManager.CurrentBoss : null;
        // CODEX BOSS PR4
        public BossPowerInventory BossPowerInventory => bossPowerInventory;

        private bool hasEnded;
        private Coroutine comboRoutine;
        // STAGE 5
        private Coroutine scoreRoutine;
        private Coroutine shakeRoutine;
        private int displayedScore;
        // CODEX BOSS PR5
        private bool awaitingBossPowerDiscard;
        // CODEX WIPE PR6
        private bool awaitingBossPowerLossDiscard;
        // CODEX WIPE PR6
        private bool awaitingBossPowerLossConfirm;
        // CODEX WIPE PR6
        private BossPower pendingBossPowerLossDiscard;
        // CODEX POWER PR5
        private bool awaitingBossPowerRewardChoice;
        // CODEX POWER PR5
        private bool pendingBossPowerRewardAfterDiscard;
        // CODEX POWER PR5
        private readonly List<BossPower> bossPowerRewardOptions = new List<BossPower>();
        // CODEX BOSS PR2
        private bool awaitingBossChallengeChoice;
        // CODEX BOSS PR4
        [SerializeField] private BossPowerInventory bossPowerInventory = new BossPowerInventory(3);
        // CODEX POWER PR5
        [SerializeField] private bool persistBossPowersToPlayerPrefs;
        // CODEX POWER PR5
        [SerializeField] private string bossPowerPrefsKey = "BossPowers";

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

            if (bossManager == null)
            {
                bossManager = FindObjectOfType<BossManager>();
            }

            if (bossPowerInventory == null)
            {
                bossPowerInventory = new BossPowerInventory(3);
            }

            // CODEX POWER PR5
            if (persistBossPowersToPlayerPrefs)
            {
                LoadBossPowerInventory();
            }

            if (screenShakeTarget == null && Camera.main != null)
            {
                screenShakeTarget = Camera.main.transform;
            }

            ConfigureBossChallengeButtons();
            ConfigureBossPowerDiscardConfirmButtons();
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
            var isBossLevel = levelIndex == 5;
            var isOptionalBossLevel = levelIndex == 6;
            IsOptionalBossLevel = isOptionalBossLevel;
            IsBossLevel = isBossLevel;
            level.isBossLevel = IsBossLevel;
            // CODEX BOSS PR1
            CurrentBossState = new BossState
            {
                bossPosition = new Vector2Int(gridSize.x / 2, gridSize.y / 2),
                bossAlive = IsBossLevel
            };
            // CODEX BOSS PR4
            UpdateBombDetonationSubscription();

            if (board != null)
            {
                board.InitializeBoard(gridSize.x, gridSize.y);
            }

            ResetGame();

            // CODEX BOSS PR1
            EnsureBossSelected();
            // CODEX BOSS PR2
            if (IsOptionalBossLevel)
            {
                BeginBossChallengeChoice();
            }
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

        // CODEX BOSS PR1
        private void EnsureBossSelected()
        {
            if (!IsBossLevel || bossManager == null || bossManager.CurrentBoss != null)
            {
                return;
            }

            var seed = board != null ? board.RandomSeed : 0;
            bossManager.SelectBossForRun(seed, debugMode);
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
            // CODEX BOSS PR4
            UpdateBombDetonationSubscription();
        }

        private void UnregisterBoardEvents()
        {
            if (board == null)
            {
                return;
            }

            // CODEX BOSS PR4
            SetBombDetonationSubscription(false);
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

            var requiredDistance = CurrentBoss != null ? CurrentBoss.requiredBombDistance : 2;
            if (DistanceManhattan(position, bossState.bossPosition) > requiredDistance)
            {
                return;
            }

            Debug.Log(
                $"BossHit bomb at {position.x},{position.y} boss at {bossState.bossPosition.x},{bossState.bossPosition.y}",
                this); // CODEX BOSS PR4
            bossState.bossAlive = false;
            CurrentBossState = bossState;

            Debug.Log(
                $"BossDefeated bomb at {position.x},{position.y} boss at {bossState.bossPosition.x},{bossState.bossPosition.y}",
                this); // CODEX BOSS PR4

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
            // CODEX BOSS PR4
            GrantBossPowerIfEligible();
            OnWin?.Invoke();
        }

        private void TriggerLose()
        {
            if (hasEnded)
            {
                return;
            }

            if (bossPowerInventory != null && bossPowerInventory.Count > 0)
            {
                hasEnded = true;
                BeginBossPowerLossDiscard();
                return;
            }

            CompleteLoseFlow();
        }

        // CODEX WIPE PR6
        private void CompleteLoseFlow()
        {
            if (!hasEnded)
            {
                hasEnded = true;
            }

            // CODEX: LEVEL_LOOP
            SetEndPanels(false, true);
            SetBoardInputLock(false);
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

            // CODEX BOSS PR4
            UpdateBossPowerUI();
        }

        // CODEX BOSS PR2
        private void ConfigureBossChallengeButtons()
        {
            if (bossChallengeFightButton != null)
            {
                bossChallengeFightButton.onClick.RemoveAllListeners();
                bossChallengeFightButton.onClick.AddListener(() => ResolveBossChallengeChoice(true));
            }

            if (bossChallengeSkipButton != null)
            {
                bossChallengeSkipButton.onClick.RemoveAllListeners();
                bossChallengeSkipButton.onClick.AddListener(() => ResolveBossChallengeChoice(false));
            }
        }

        // CODEX WIPE PR6
        private void ConfigureBossPowerDiscardConfirmButtons()
        {
            if (bossPowerDiscardConfirmYesButton != null)
            {
                bossPowerDiscardConfirmYesButton.onClick.RemoveAllListeners();
                bossPowerDiscardConfirmYesButton.onClick.AddListener(ConfirmBossPowerLossDiscard);
            }

            if (bossPowerDiscardConfirmNoButton != null)
            {
                bossPowerDiscardConfirmNoButton.onClick.RemoveAllListeners();
                bossPowerDiscardConfirmNoButton.onClick.AddListener(CancelBossPowerLossDiscard);
            }
        }

        // CODEX BOSS PR2
        private void BeginBossChallengeChoice()
        {
            if (bossChallengePanel == null)
            {
                return;
            }

            awaitingBossChallengeChoice = true;
            bossChallengePanel.SetActive(true);

            if (bossChallengePromptText != null)
            {
                bossChallengePromptText.text = "Face a Boss for extra reward?";
            }

            if (bossChallengeWarningText != null)
            {
                bossChallengeWarningText.text = "If you wipe, you must discard 1 boss power (confirmed).";
            }

            SetBoardInputLock(true);
        }

        // CODEX BOSS PR2
        private void ResolveBossChallengeChoice(bool fightBoss)
        {
            if (!awaitingBossChallengeChoice)
            {
                return;
            }

            awaitingBossChallengeChoice = false;

            if (bossChallengePanel != null)
            {
                bossChallengePanel.SetActive(false);
            }

            IsBossLevel = fightBoss;
            var bossState = CurrentBossState;
            bossState.bossAlive = fightBoss;
            CurrentBossState = bossState;

            if (fightBoss)
            {
                EnsureBossSelected();
            }

            // CODEX BOSS PR4
            UpdateBombDetonationSubscription();
            SetBoardInputLock(false);
            UpdateUI();
        }

        // CODEX BOSS PR4
        private void UpdateBombDetonationSubscription()
        {
            SetBombDetonationSubscription(IsBossLevel);
        }

        // CODEX BOSS PR4
        private void SetBombDetonationSubscription(bool shouldSubscribe)
        {
            if (board == null)
            {
                return;
            }

            if (shouldSubscribe)
            {
                board.OnBombDetonated -= HandleBombDetonated;
                board.OnBombDetonated += HandleBombDetonated;
                return;
            }

            board.OnBombDetonated -= HandleBombDetonated;
        }

        // CODEX BOSS PR4
        private void OnDrawGizmos()
        {
            if (board == null || !IsBossLevel)
            {
                return;
            }

            var bossState = CurrentBossState;
            var bossWorld = board.GridToWorld(bossState.bossPosition.x, bossState.bossPosition.y);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(bossWorld, 0.35f);
        }

        // CODEX BOSS PR2
        private void SetBoardInputLock(bool locked)
        {
            if (board == null)
            {
                return;
            }

            board.SetExternalInputLock(locked);
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

        // CODEX BOSS PR4
        private void GrantBossPowerIfEligible()
        {
            if (!IsBossLevel || CurrentBossState.bossAlive)
            {
                return;
            }

            if (bossPowerInventory == null)
            {
                bossPowerInventory = new BossPowerInventory(3);
            }

            // CODEX POWER PR5
            if (!TryBeginBossPowerRewardChoice())
            {
                return;
            }
        }

        // CODEX BOSS PR4
        private void UpdateBossPowerUI()
        {
            if (bossPowersText == null || bossPowerInventory == null)
            {
                return;
            }

            bossPowersText.text = bossPowerInventory.BuildDisplayString();
        }

        // CODEX BOSS PR5
        public bool HasBossPower(BossPower power)
        {
            return bossPowerInventory != null && bossPowerInventory.HasPower(power);
        }

        // CODEX WIPE PR6
        private void BeginBossPowerLossDiscard()
        {
            if (bossPowerInventory == null || bossPowerInventory.Count == 0)
            {
                CompleteLoseFlow();
                return;
            }

            if (TryShowBossPowerDiscardPanel(HandleBossPowerLossDiscardSelection))
            {
                awaitingBossPowerLossDiscard = true;
                SetBoardInputLock(true);
                return;
            }

            Debug.LogWarning("Boss power discard UI is not configured for loss flow.", this);
            CompleteLoseFlow();
        }

        // CODEX BOSS PR5
        private bool TryShowBossPowerDiscardPanel(Action<BossPower> onSelect)
        {
            if (bossPowerDiscardPanel == null || bossPowerDiscardButtons == null || bossPowerDiscardButtons.Length == 0)
            {
                return false;
            }

            bossPowerDiscardPanel.SetActive(true);
            var powers = bossPowerInventory.Powers;
            for (var i = 0; i < bossPowerDiscardButtons.Length; i++)
            {
                var button = bossPowerDiscardButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < powers.Count)
                {
                    var power = powers[i];
                    button.gameObject.SetActive(true);
                    button.onClick.RemoveAllListeners();
                    var label = button.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = power.ToString();
                    }
                    button.onClick.AddListener(() => onSelect?.Invoke(power));
                }
                else
                {
                    button.onClick.RemoveAllListeners();
                    button.gameObject.SetActive(false);
                }
            }

            return true;
        }

        // CODEX WIPE PR6
        private void HandleBossPowerLossDiscardSelection(BossPower power)
        {
            if (!awaitingBossPowerLossDiscard)
            {
                return;
            }

            awaitingBossPowerLossDiscard = false;
            pendingBossPowerLossDiscard = power;

            if (bossPowerDiscardPanel != null)
            {
                bossPowerDiscardPanel.SetActive(false);
            }

            ShowBossPowerDiscardConfirm(power);
        }

        // CODEX WIPE PR6
        private void ShowBossPowerDiscardConfirm(BossPower power)
        {
            if (bossPowerDiscardConfirmPanel == null)
            {
                Debug.LogWarning("Boss power discard confirmation UI is not configured.", this);
                awaitingBossPowerLossConfirm = true;
                ConfirmBossPowerLossDiscard();
                return;
            }

            if (bossPowerDiscardConfirmText != null)
            {
                bossPowerDiscardConfirmText.text = $"Discard {power}?";
            }

            bossPowerDiscardConfirmPanel.SetActive(true);
            awaitingBossPowerLossConfirm = true;
        }

        // CODEX WIPE PR6
        private void ConfirmBossPowerLossDiscard()
        {
            if (!awaitingBossPowerLossConfirm)
            {
                return;
            }

            awaitingBossPowerLossConfirm = false;
            if (bossPowerDiscardConfirmPanel != null)
            {
                bossPowerDiscardConfirmPanel.SetActive(false);
            }

            if (bossPowerInventory != null)
            {
                bossPowerInventory.TryRemovePower(pendingBossPowerLossDiscard);
            }

            SaveBossPowerInventory();
            UpdateBossPowerUI();
            Debug.Log($"Discarded boss power on loss: {pendingBossPowerLossDiscard}", this);
            CompleteLoseFlow();
        }

        // CODEX WIPE PR6
        private void CancelBossPowerLossDiscard()
        {
            if (!awaitingBossPowerLossConfirm)
            {
                return;
            }

            awaitingBossPowerLossConfirm = false;
            if (bossPowerDiscardConfirmPanel != null)
            {
                bossPowerDiscardConfirmPanel.SetActive(false);
            }

            pendingBossPowerLossDiscard = default;
            BeginBossPowerLossDiscard();
        }

        // CODEX BOSS PR5
        private void ResolveBossPowerDiscard(BossPower power)
        {
            if (!awaitingBossPowerDiscard)
            {
                return;
            }

            awaitingBossPowerDiscard = false;
            if (bossPowerInventory != null)
            {
                bossPowerInventory.TryRemovePower(power);
            }

            if (bossPowerDiscardPanel != null)
            {
                bossPowerDiscardPanel.SetActive(false);
            }

            // CODEX POWER PR5
            if (pendingBossPowerRewardAfterDiscard)
            {
                pendingBossPowerRewardAfterDiscard = false;
                ShowBossPowerRewardPanel();
            }
            else
            {
                SetBoardInputLock(false);
            }

            SaveBossPowerInventory();
            UpdateBossPowerUI();
            Debug.Log($"Discarded boss power: {power}", this);
        }

        // CODEX BOSS PR5
        private void DiscardRandomBossPower()
        {
            if (bossPowerInventory == null || bossPowerInventory.Count == 0)
            {
                return;
            }

            var index = UnityEngine.Random.Range(0, bossPowerInventory.Count);
            var power = bossPowerInventory.Powers[index];
            bossPowerInventory.TryRemovePower(power);
            UpdateBossPowerUI();
            SaveBossPowerInventory();
            Debug.Log($"Discarded boss power on loss: {power}", this);
        }

        // CODEX POWER PR5
        private bool TryBeginBossPowerRewardChoice()
        {
            if (bossPowerRewardPanel == null || bossPowerRewardButtons == null || bossPowerRewardButtons.Length == 0)
            {
                Debug.LogWarning("Boss power reward UI is not configured; skipping reward choices.", this);
                return false;
            }

            bossPowerRewardOptions.Clear();
            bossPowerRewardOptions.AddRange(BuildBossPowerRewardOptions());
            if (bossPowerRewardOptions.Count == 0)
            {
                return false;
            }

            if (bossPowerInventory.Count >= bossPowerInventory.MaxSlots)
            {
                pendingBossPowerRewardAfterDiscard = true;
            if (TryShowBossPowerDiscardPanel(ResolveBossPowerDiscard))
            {
                awaitingBossPowerDiscard = true;
                SetBoardInputLock(true);
            }
                else
                {
                    pendingBossPowerRewardAfterDiscard = false;
                    Debug.LogWarning("Boss power inventory is full and discard UI is not configured.", this);
                }

                return true;
            }

            ShowBossPowerRewardPanel();
            return true;
        }

        // CODEX POWER PR5
        private List<BossPower> BuildBossPowerRewardOptions()
        {
            var availablePowers = new List<BossPower>();
            var powerValues = (BossPower[])Enum.GetValues(typeof(BossPower));
            for (var i = 0; i < powerValues.Length; i++)
            {
                if (bossPowerInventory != null && bossPowerInventory.HasPower(powerValues[i]))
                {
                    continue;
                }

                availablePowers.Add(powerValues[i]);
            }

            if (availablePowers.Count == 0)
            {
                return availablePowers;
            }

            var desiredCount = Mathf.Clamp(UnityEngine.Random.Range(2, 4), 2, 3);
            var optionCount = Mathf.Min(desiredCount, availablePowers.Count);

            for (var i = 0; i < availablePowers.Count; i++)
            {
                var swapIndex = UnityEngine.Random.Range(i, availablePowers.Count);
                var temp = availablePowers[i];
                availablePowers[i] = availablePowers[swapIndex];
                availablePowers[swapIndex] = temp;
            }

            return availablePowers.GetRange(0, optionCount);
        }

        // CODEX POWER PR5
        private void ShowBossPowerRewardPanel()
        {
            if (bossPowerRewardPanel == null || bossPowerRewardButtons == null)
            {
                return;
            }

            awaitingBossPowerRewardChoice = true;
            bossPowerRewardPanel.SetActive(true);

            if (bossPowerRewardPromptText != null)
            {
                var isFull = bossPowerInventory != null && bossPowerInventory.Count >= bossPowerInventory.MaxSlots;
                bossPowerRewardPromptText.text = isFull
                    ? "Inventory full! Discard a power first."
                    : "Choose a boss power.";
            }

            for (var i = 0; i < bossPowerRewardButtons.Length; i++)
            {
                var button = bossPowerRewardButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < bossPowerRewardOptions.Count)
                {
                    var power = bossPowerRewardOptions[i];
                    button.gameObject.SetActive(true);
                    button.onClick.RemoveAllListeners();
                    var label = button.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = power.ToString();
                    }

                    button.onClick.AddListener(() => ResolveBossPowerRewardChoice(power));
                }
                else
                {
                    button.onClick.RemoveAllListeners();
                    button.gameObject.SetActive(false);
                }
            }

            SetBoardInputLock(true);
        }

        // CODEX POWER PR5
        private void ResolveBossPowerRewardChoice(BossPower power)
        {
            if (!awaitingBossPowerRewardChoice)
            {
                return;
            }

            awaitingBossPowerRewardChoice = false;
            if (bossPowerRewardPanel != null)
            {
                bossPowerRewardPanel.SetActive(false);
            }

            var added = bossPowerInventory != null && bossPowerInventory.TryAddPower(power);
            if (!added)
            {
                Debug.LogWarning($"Failed to add boss power {power}; inventory may be full.", this);
            }

            SetBoardInputLock(false);
            SaveBossPowerInventory();
            UpdateBossPowerUI();
            Debug.Log($"Selected boss power: {power}", this);
        }

        // CODEX POWER PR5
        private void SaveBossPowerInventory()
        {
            if (!persistBossPowersToPlayerPrefs || bossPowerInventory == null)
            {
                return;
            }

            var powerNames = bossPowerInventory.Powers;
            var serialized = string.Empty;
            for (var i = 0; i < powerNames.Count; i++)
            {
                serialized += powerNames[i];
                if (i < powerNames.Count - 1)
                {
                    serialized += ",";
                }
            }

            PlayerPrefs.SetString(bossPowerPrefsKey, serialized);
            PlayerPrefs.Save();
        }

        // CODEX POWER PR5
        private void LoadBossPowerInventory()
        {
            if (!PlayerPrefs.HasKey(bossPowerPrefsKey) || bossPowerInventory == null)
            {
                return;
            }

            var saved = PlayerPrefs.GetString(bossPowerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(saved))
            {
                bossPowerInventory.Clear();
                return;
            }

            var parts = saved.Split(',');
            var parsed = new List<BossPower>();
            for (var i = 0; i < parts.Length; i++)
            {
                if (Enum.TryParse(parts[i], out BossPower power))
                {
                    parsed.Add(power);
                }
            }

            bossPowerInventory.ReplacePowers(parsed);
        }
    }
}
