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
        [SerializeField] private PlayerAnimationStateController playerAnimationStateController;

        [Header("UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text movesText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text shieldIconText;
        [SerializeField] private Text toxicWarningIconText;
        [SerializeField] private Text meditationText;
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
        // CODEX BONUS PR3
        [SerializeField] private GameObject bonusStagePanel;
        // CODEX BONUS PR3
        [SerializeField] private Text bonusStageText;
        // CODEX BONUS PR4
        [SerializeField] private Text bonusStageInstructionText;
        // CODEX BONUS PR4
        [SerializeField] private Text bonusStageRouletteText;
        // CODEX BONUS PR4
        [SerializeField] private Text bonusStageResultText;
        // CODEX BONUS PR4
        [SerializeField] private Transform bonusStageListRoot;
        // CODEX BONUS PR3
        [SerializeField] private Button bonusStageContinueButton;
        // CODEX BONUS PR4
        [SerializeField] private List<string> bonusStageGames = new List<string>
        {
            "Chess",
            "Checkers",
            "Memory",
            "Sudoku",
            "Simon",
            "Minesweeper",
            "Wordle",
            "SlidingPuzzle",
            "MatchPairs"
        };
        // CODEX BONUS PR4
        [SerializeField] private float bonusStageRouletteDuration = 2.5f;
        // CODEX BONUS PR4
        [SerializeField] private float bonusStageRouletteStep = 0.08f;
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
        [Header("Energy")]
        [SerializeField] private int maxEnergy = 3;
        [Header("Monster Attack")]
        [SerializeField] private int monsterReachDistance = 2;
        [SerializeField] private GameObject monsterAttackMarkerPrefab;

        public static GameManager Instance { get; private set; }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public bool HasMetTarget { get; private set; }
        public int Energy => energy;
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
        public bool HasMonsterAttackTarget => CurrentBossState.IsEnraged;
        public Vector2Int MonsterAttackTarget => CurrentBossState.AttackTarget;
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
        // CODEX BONUS PR6
        private bool awaitingBonusBossPowerRewardChoice;
        // CODEX POWER PR5
        private bool pendingBossPowerRewardAfterDiscard;
        // CODEX BONUS PR6
        private bool pendingBonusBossPowerRewardChoice;
        // CODEX POWER PR5
        private readonly List<BossPower> bossPowerRewardOptions = new List<BossPower>();
        // CODEX BONUS PR6
        private readonly List<BossPower> bossPowersLostThisRun = new List<BossPower>();
        // CODEX BOSS PR2
        private bool awaitingBossChallengeChoice;
        // CODEX BOSS PR4
        [SerializeField] private BossPowerInventory bossPowerInventory = new BossPowerInventory(3);
        // CODEX POWER PR5
        [SerializeField] private bool persistBossPowersToPlayerPrefs;
        // CODEX POWER PR5
        [SerializeField] private string bossPowerPrefsKey = "BossPowers";
        // CODEX CHEST PR2
        [SerializeField] private bool persistCrowns;
        // CODEX CHEST PR2
        [SerializeField] private string crownsPrefsKey = "CrownsThisRun";
        // CODEX CHEST PR2
        private int crownsThisRun;
        // CODEX BONUS PR3
        private bool isBonusStageActive;
        // CODEX BONUS PR4
        private readonly List<Button> bonusStageBanButtons = new List<Button>();
        // CODEX BONUS PR4
        private readonly List<string> bonusStageRemainingGames = new List<string>();
        // CODEX BONUS PR4
        private Coroutine bonusStageRouletteRoutine;
        // CODEX BONUS PR4
        private System.Random bonusStageRandom;
        // CODEX BONUS PR4
        private string bonusStageBannedGame;
        // CODEX BONUS PR4
        private string bonusStageSelectedGame;
        // CODEX BONUS PR4
        private bool awaitingBonusStageBan;
        // CODEX BONUS PR4
        private bool bonusStageRouletteComplete;
        // CODEX BONUS PR5
        private readonly Dictionary<string, Func<BonusMiniGameBase>> bonusMiniGameFactories = new Dictionary<string, Func<BonusMiniGameBase>>();
        // CODEX BONUS PR5
        private BonusMiniGameBase activeBonusMiniGame;
        // CODEX BONUS PR5
        private Coroutine bonusMiniGameCleanupRoutine;
        private int energy;
        private bool isShieldActive;
        private bool isMeditating;
        private bool manualAbilityUsedThisTurn;
        private int meditationTurnsRemaining;
        private int toxicStacks;
        private bool toxicDrainActive;
        private bool isHappy;
        private bool isExcited;
        private bool isStunned;
        private bool isWorried;
        private bool isTired;
        private bool hasGainedEnergy;
        private int stunnedTurnsRemaining;
        private GameObject monsterAttackMarkerInstance;
        private const int ToxicGraceStacks = 2;
        public bool IsPlayerStunned => playerAnimationStateController != null && playerAnimationStateController.IsStunned;

        // CODEX CHEST PR2
        public int CrownsThisRun => crownsThisRun;

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

            if (playerAnimationStateController == null)
            {
                playerAnimationStateController = FindObjectOfType<PlayerAnimationStateController>();
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

            // CODEX CHEST PR2
            if (persistCrowns)
            {
                LoadCrowns();
            }

            if (screenShakeTarget == null && Camera.main != null)
            {
                screenShakeTarget = Camera.main.transform;
            }

            ConfigureBossChallengeButtons();
            ConfigureBossPowerDiscardConfirmButtons();
            ConfigureBonusStageButton();

            // CODEX BONUS PR5
            RegisterBonusMiniGame("Memory", () => CreateBonusMiniGame<MemoryBonusGame>());
        }

        private void OnEnable()
        {
            if (levelDatabase == null)
            {
                levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");
            }

            RegisterBoardEvents();
            UpdateShieldAnimationState();
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
            // CODEX CHEST PR2
            crownsThisRun = 0;
            SaveCrownsIfNeeded();
            // CODEX BONUS PR6
            bossPowersLostThisRun.Clear();
            if (bossManager != null)
            {
                bossManager.ResetRunPool();
                // CODEX BOSS PR7
                ApplyBossPowerRunPoolExclusions();
            }
            // CODEX: LEVEL_LOOP
            MovesRemaining = MovesLimit > 0 ? MovesLimit : startingMoves;
            HasMetTarget = false;
            hasEnded = false;
            energy = 0;
            toxicStacks = 0;
            toxicDrainActive = false;
            manualAbilityUsedThisTurn = false;
            SetShieldActive(false);
            CancelMeditation();
            ClearTransientEmotionFlags();
            isStunned = false;
            isWorried = false;
            isTired = false;
            hasGainedEnergy = false;
            stunnedTurnsRemaining = 0;
            ResetMonsterAttackState();
            HideComboText();
            displayedScore = Score;
            // CODEX: LEVEL_LOOP
            SetEndPanels(false, false);
            UpdateUI();
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
        }

        public void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            // CODEX DIFFICULTY PR7
            var level = DifficultyScaling.GenerateLevelDefinition(levelIndex, fallbackGridSize);

            // CODEX: LEVEL_LOOP
            MovesLimit = level.movesLimit > 0 ? level.movesLimit : startingMoves;
            TargetScore = level.targetScore > 0 ? level.targetScore : fallbackTargetScore;
            var gridSize = level.gridSize == Vector2Int.zero ? fallbackGridSize : level.gridSize;
            // CODEX BOSS PR7
            var levelIndexMod = Mathf.Abs(levelIndex) % 10;
            var isBossLevel = levelIndexMod == 6;
            var isOptionalBossLevel = levelIndexMod == 7;
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
                // CODEX DIFFICULTY PR7
                board.ConfigureColorCount(level.colorCount);
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

        // CODEX BOSS PR7
        private void ApplyBossPowerRunPoolExclusions()
        {
            if (bossManager == null || bossPowerInventory == null || bossPowerInventory.Count == 0)
            {
                return;
            }

            var powers = bossPowerInventory.Powers;
            for (var i = 0; i < powers.Count; i++)
            {
                bossManager.RemoveBossFromRunPool(powers[i]);
            }
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
            board.TurnEnded += HandleTurnEnded;
            // CODEX CHEST PR2
            board.OnPieceDestroyed += HandlePieceDestroyed;
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
            board.TurnEnded -= HandleTurnEnded;
            // CODEX CHEST PR2
            board.OnPieceDestroyed -= HandlePieceDestroyed;
        }

        private void HandleMatchesCleared(int clearedCount, int cascadeCount, IReadOnlyList<int> matchRunLengths)
        {
            if (hasEnded)
            {
                return;
            }

            AddScore(clearedCount, cascadeCount);
            var energyGain = AddEnergyFromMatches(matchRunLengths, out var reachedMaxEnergy);
            var hasMatchFourPlus = HasMatchRunAtLeast(matchRunLengths, 4);
            var hasMatchFivePlus = HasMatchRunAtLeast(matchRunLengths, 5);
            if (energyGain >= 2 && hasMatchFourPlus)
            {
                TriggerHappyAnimation();
            }

            if (hasMatchFivePlus || reachedMaxEnergy)
            {
                TriggerExcitedAnimation();
            }

            UpdateTiredState();
            if (cascadeCount >= 2)
            {
                ShowComboText(cascadeCount);
            }

            if (clearedCount >= bigClearThreshold)
            {
                TriggerScreenShake();
            }
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (energy < 0)
            {
                energy = 0;
            }

            if (energy < amount)
            {
                return false;
            }

            energy = Mathf.Max(0, energy - amount);
            if (energy > 0)
            {
                hasGainedEnergy = true;
            }
            UpdateTiredState();
            return true;
        }

        public bool TryActivateShield()
        {
            if (!CanUseManualAbility())
            {
                return false;
            }

            if (isShieldActive)
            {
                return false;
            }

            const int shieldEnergyCost = 2;
            if (!TrySpendEnergy(shieldEnergyCost))
            {
                return false;
            }

            CancelMeditation();
            SetShieldActive(true);
            RegisterManualAbilityUse();
            return true;
        }

        public bool TryActivateMeditation()
        {
            if (!CanUseManualAbility())
            {
                return false;
            }

            if (isMeditating)
            {
                return false;
            }

            const int meditationEnergyCost = 2;
            if (!TrySpendEnergy(meditationEnergyCost))
            {
                return false;
            }

            BeginMeditation(3);
            RegisterManualAbilityUse();
            return true;
        }

        public bool TryBlockPlayerDamage(PlayerDamageType damageType)
        {
            if (damageType == PlayerDamageType.ToxicDrain)
            {
                return false;
            }

            if (!isShieldActive)
            {
                return false;
            }

            if (damageType == PlayerDamageType.HeavyHit || damageType == PlayerDamageType.Explosion)
            {
                SetShieldActive(false);
                return true;
            }

            return false;
        }

        private void SetShieldActive(bool active)
        {
            if (isShieldActive == active)
            {
                return;
            }

            isShieldActive = active;
            UpdateShieldAnimationState();
        }

        private void UpdateShieldAnimationState()
        {
            UpdatePlayerAnimationFlags();
        }

        private void BeginMeditation(int turns)
        {
            if (turns <= 0)
            {
                return;
            }

            isMeditating = true;
            meditationTurnsRemaining = turns;
            UpdateMeditationAnimationState();
        }

        public void CancelMeditation()
        {
            if (!isMeditating && meditationTurnsRemaining <= 0)
            {
                return;
            }

            isMeditating = false;
            meditationTurnsRemaining = 0;
            UpdateMeditationAnimationState();
        }

        private void UpdateMeditationAnimationState()
        {
            UpdatePlayerAnimationFlags();
        }

        private int AddEnergyFromMatches(IReadOnlyList<int> matchRunLengths, out bool reachedMaxEnergy)
        {
            reachedMaxEnergy = false;
            if (energy < 0)
            {
                energy = 0;
            }
            if (matchRunLengths == null || matchRunLengths.Count == 0 || energy >= maxEnergy)
            {
                return 0;
            }

            var energyGain = 0;
            foreach (var runLength in matchRunLengths)
            {
                energyGain += GetEnergyGainForRun(runLength);
            }

            if (energyGain <= 0)
            {
                return 0;
            }

            var previousEnergy = energy;
            energy = Mathf.Min(maxEnergy, energy + energyGain);
            reachedMaxEnergy = energy >= maxEnergy && previousEnergy < maxEnergy;
            hasGainedEnergy = hasGainedEnergy || energy > 0;
            UpdateTiredState();
            return energyGain;
        }

        private int GetEnergyGainForRun(int runLength)
        {
            if (runLength >= 5)
            {
                return 3;
            }

            if (runLength == 4)
            {
                return 2;
            }

            return runLength == 3 ? 1 : 0;
        }

        private void HandleValidSwap()
        {
            if (hasEnded)
            {
                return;
            }

            ClearTransientEmotionFlags();
            UpdatePlayerAnimationFlags();
            TryUseMove();
            if (MovesRemaining <= 0 && !HasMetTarget)
            {
                TriggerLose();
            }
        }

        private void HandleTurnEnded()
        {
            if (hasEnded || board == null)
            {
                return;
            }

            manualAbilityUsedThisTurn = false;

            var hasPlayerPosition = board.TryGetPlayerPosition(out var playerPosition);
            if (!hasPlayerPosition || playerPosition.y > 0)
            {
                ClearToxicStacks();
            }
            else
            {
                toxicStacks += 1;
                if (toxicStacks >= ToxicGraceStacks)
                {
                    toxicDrainActive = true;
                    ApplyEnergyDrain(1);
                }
                else
                {
                    toxicDrainActive = false;
                }
            }

            if (isMeditating && meditationTurnsRemaining > 0)
            {
                if (board != null && board.CanMovePlayerUp())
                {
                    meditationTurnsRemaining -= 1;
                    board.TryMovePlayerUp();
                }
                else
                {
                    CancelMeditation();
                }

                if (meditationTurnsRemaining <= 0)
                {
                    isMeditating = false;
                    UpdateMeditationAnimationState();
                }
            }

            if (stunnedTurnsRemaining > 0)
            {
                stunnedTurnsRemaining -= 1;
                if (stunnedTurnsRemaining <= 0)
                {
                    isStunned = false;
                }
            }

            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
            TickMonsterAttackMarker();
            TryTriggerMonsterEnrage();
        }

        private void ResetMonsterAttackState()
        {
            var bossState = CurrentBossState;
            bossState.IsEnraged = false;
            bossState.AttackTarget = default;
            bossState.TurnsUntilAttack = 0;
            CurrentBossState = bossState;
            DestroyMonsterAttackMarker();
        }

        private void TickMonsterAttackMarker()
        {
            var bossState = CurrentBossState;
            if (!bossState.IsEnraged)
            {
                return;
            }

            if (!bossState.bossAlive)
            {
                ResetMonsterAttackState();
                return;
            }

            bossState.TurnsUntilAttack = Mathf.Max(0, bossState.TurnsUntilAttack - 1);
            if (bossState.TurnsUntilAttack > 0)
            {
                CurrentBossState = bossState;
                return;
            }

            var targetPosition = bossState.AttackTarget;
            CurrentBossState = bossState;
            ResolveMonsterAttack(targetPosition);
            ResetMonsterAttackState();
        }

        private void TryTriggerMonsterEnrage()
        {
            if (!IsBossLevel || board == null)
            {
                return;
            }

            var bossState = CurrentBossState;
            if (!bossState.bossAlive || bossState.IsEnraged)
            {
                return;
            }

            if (!board.TryGetPlayerPosition(out var playerPosition))
            {
                return;
            }

            if (!CanMonsterReachPlayer(bossState.bossPosition, playerPosition))
            {
                return;
            }

            ActivateMonsterAttack(playerPosition);
        }

        private bool CanMonsterReachPlayer(Vector2Int monsterPosition, Vector2Int playerPosition)
        {
            return DistanceManhattan(monsterPosition, playerPosition) <= monsterReachDistance;
        }

        private void ActivateMonsterAttack(Vector2Int targetPosition)
        {
            var bossState = CurrentBossState;
            bossState.IsEnraged = true;
            bossState.AttackTarget = targetPosition;
            bossState.TurnsUntilAttack = 2;
            CurrentBossState = bossState;
            SpawnMonsterAttackMarker(targetPosition);
        }

        private void ResolveMonsterAttack(Vector2Int targetPosition)
        {
            if (board == null || hasEnded)
            {
                return;
            }

            if (!CurrentBossState.bossAlive)
            {
                return;
            }

            if (board.TryGetPlayerPosition(out var playerPosition)
                && playerPosition == targetPosition)
            {
                if (!TryBlockPlayerDamage(PlayerDamageType.HeavyHit))
                {
                    TriggerLose();
                }

                return;
            }

            board.TryDestroyPieceAt(targetPosition, DestructionReason.MonsterAttack);
        }

        private void SpawnMonsterAttackMarker(Vector2Int targetPosition)
        {
            DestroyMonsterAttackMarker();
            if (board == null)
            {
                return;
            }

            var worldPosition = board.GridToWorld(targetPosition.x, targetPosition.y);
            var parent = board.transform;
            if (monsterAttackMarkerPrefab != null)
            {
                monsterAttackMarkerInstance = Instantiate(
                    monsterAttackMarkerPrefab,
                    worldPosition,
                    Quaternion.identity,
                    parent);
                return;
            }

            var markerObject = new GameObject("MonsterAttackMarker");
            markerObject.transform.SetParent(parent);
            markerObject.transform.position = worldPosition;
            var text = markerObject.AddComponent<TextMesh>();
            text.text = "!";
            text.fontSize = 64;
            text.characterSize = 0.1f;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            var renderer = markerObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
            }
            monsterAttackMarkerInstance = markerObject;
        }

        private void DestroyMonsterAttackMarker()
        {
            if (monsterAttackMarkerInstance == null)
            {
                return;
            }

            Destroy(monsterAttackMarkerInstance);
            monsterAttackMarkerInstance = null;
        }

        public bool CanUseManualAbility()
        {
            if (hasEnded)
            {
                return false;
            }

            if (board != null && board.IsBusy)
            {
                return false;
            }

            if (IsPlayerStunned)
            {
                return false;
            }

            if (isMeditating)
            {
                return false;
            }

            return !manualAbilityUsedThisTurn;
        }

        public void RegisterManualAbilityUse()
        {
            manualAbilityUsedThisTurn = true;
        }

        private void ClearToxicStacks()
        {
            toxicStacks = 0;
            toxicDrainActive = false;
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
        }

        private void ApplyEnergyDrain(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (energy < 0)
            {
                energy = 0;
            }

            energy = Mathf.Max(0, energy - amount);
            if (energy > 0)
            {
                hasGainedEnergy = true;
            }
            UpdateTiredState();
        }

        // CODEX CHEST PR2
        private void HandlePieceDestroyed(Piece piece, DestructionReason reason)
        {
            if (piece == null)
            {
                return;
            }

            TryDefeatBossFromDestruction(piece, reason);

            if (piece.SpecialType == SpecialType.TreasureChest && reason == DestructionReason.BombExplosion)
            {
                GrantCrown();
                return;
            }

            if (piece.SpecialType == SpecialType.TreasureChest)
            {
                if (debugMode)
                {
                    Debug.Log("CrownIgnored", this); // CODEX CHEST PR2
                }
            }
        }

        private void TryDefeatBossFromDestruction(Piece piece, DestructionReason reason)
        {
            if (!IsBossLevel)
            {
                return;
            }

            if (reason != DestructionReason.NormalMatch && reason != DestructionReason.BombExplosion)
            {
                return;
            }

            var bossState = CurrentBossState;
            if (!bossState.bossAlive)
            {
                return;
            }

            if (piece.X != bossState.bossPosition.x || piece.Y != bossState.bossPosition.y)
            {
                return;
            }

            DefeatBoss(new Vector2Int(piece.X, piece.Y), "match/explosion");
        }

        // CODEX BOSS PR3
        private void HandleBombDetonated(Vector2Int position)
        {
            if (hasEnded || !IsBossLevel)
            {
                return;
            }

            if (board != null && board.TryGetPlayerPosition(out var playerPosition))
            {
                if (DistanceManhattan(position, playerPosition) <= 1)
                {
                    TriggerStunnedAnimation();
                }
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

            DefeatBoss(position, "bomb");
        }

        private void DefeatBoss(Vector2Int position, string source)
        {
            var bossState = CurrentBossState;
            if (!bossState.bossAlive)
            {
                return;
            }

            Debug.Log(
                $"BossHit {source} at {position.x},{position.y} boss at {bossState.bossPosition.x},{bossState.bossPosition.y}",
                this); // CODEX BOSS PR4
            bossState.bossAlive = false;
            CurrentBossState = bossState;
            ResetMonsterAttackState();

            Debug.Log(
                $"BossDefeated {source} at {position.x},{position.y} boss at {bossState.bossPosition.x},{bossState.bossPosition.y}",
                this); // CODEX BOSS PR4

            TriggerInstantWin();
            TriggerWin();
        }

        // CODEX CHEST PR2
        private void GrantCrown()
        {
            crownsThisRun++;
            SaveCrownsIfNeeded();
            if (debugMode)
            {
                Debug.Log($"CrownGained -> crownsThisRun = {crownsThisRun}", this); // CODEX CHEST PR2
            }

            // CODEX BONUS PR3
            if (crownsThisRun >= 3)
            {
                EnterBonusStage(); // CODEX CHEST PR2
            }
        }

        // CODEX CHEST PR2
        private void EnterBonusStage()
        {
            BeginBonusStage();
        }

        // CODEX CHEST PR2
        private void LoadCrowns()
        {
            crownsThisRun = PlayerPrefs.GetInt(crownsPrefsKey, 0);
        }

        // CODEX CHEST PR2
        private void SaveCrownsIfNeeded()
        {
            if (!persistCrowns)
            {
                return;
            }

            PlayerPrefs.SetInt(crownsPrefsKey, crownsThisRun);
            PlayerPrefs.Save();
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

        public void TriggerJetpackDoubleSuccess()
        {
            TriggerExcitedAnimation();
        }

        private void TriggerHappyAnimation()
        {
            isHappy = true;
            UpdatePlayerAnimationFlags();
        }

        private void TriggerExcitedAnimation()
        {
            isExcited = true;
            UpdatePlayerAnimationFlags();
        }

        private void TriggerStunnedAnimation()
        {
            if (!isStunned)
            {
                isStunned = true;
            }

            stunnedTurnsRemaining = Mathf.Max(stunnedTurnsRemaining, 1);
            UpdatePlayerAnimationFlags();
        }

        private void ClearTransientEmotionFlags()
        {
            isHappy = false;
            isExcited = false;
        }

        private void UpdateWorriedState()
        {
            var isBombAdjacent = board != null && board.IsPlayerAdjacentToBomb();
            isWorried = toxicStacks == 1 || isBombAdjacent;
        }

        private void UpdateTiredState()
        {
            isTired = toxicDrainActive || (hasGainedEnergy && energy == 0);
        }

        private void UpdatePlayerAnimationFlags()
        {
            if (playerAnimationStateController == null)
            {
                return;
            }

            playerAnimationStateController.SetStateFlags(
                isHappy,
                isExcited,
                isStunned,
                isWorried,
                isMeditating,
                isShieldActive,
                isTired);
        }

        private static bool HasMatchRunAtLeast(IReadOnlyList<int> matchRunLengths, int length)
        {
            if (matchRunLengths == null)
            {
                return false;
            }

            for (var i = 0; i < matchRunLengths.Count; i++)
            {
                if (matchRunLengths[i] >= length)
                {
                    return true;
                }
            }

            return false;
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

            if (energyText != null)
            {
                energyText.text = $"Energy: {energy}/{maxEnergy}";
            }

            if (shieldIconText != null)
            {
                shieldIconText.enabled = isShieldActive;
            }

            if (toxicWarningIconText != null)
            {
                toxicWarningIconText.enabled = toxicStacks > 0 || toxicDrainActive;
            }

            if (meditationText != null)
            {
                var showMeditation = meditationTurnsRemaining > 0;
                meditationText.enabled = showMeditation;
                if (showMeditation)
                {
                    meditationText.text = $"Meditation: {meditationTurnsRemaining}";
                }
            }
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

        // CODEX BONUS PR3
        private void ConfigureBonusStageButton()
        {
            if (bonusStageContinueButton == null)
            {
                return;
            }

            bonusStageContinueButton.onClick.RemoveAllListeners();
            bonusStageContinueButton.onClick.AddListener(HandleBonusStageContinue);
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
                bossChallengeWarningText.text = "If you wipe, you must discard ONE boss power (confirm required)";
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

        // CODEX BONUS PR3
        private void BeginBonusStage()
        {
            if (isBonusStageActive)
            {
                return;
            }

            BuildBonusStageUIIfNeeded();
            if (bonusStagePanel == null)
            {
                return;
            }

            isBonusStageActive = true;
            Debug.Log("BonusStageEnter", this);
            SetBoardInputLock(true);

            bonusStagePanel.SetActive(true);
            PrepareBonusStageSelection();
        }

        // CODEX BONUS PR3
        private void EndBonusStage()
        {
            if (!isBonusStageActive)
            {
                return;
            }

            isBonusStageActive = false;
            Debug.Log("BonusStageExit", this);

            if (bonusStageRouletteRoutine != null)
            {
                StopCoroutine(bonusStageRouletteRoutine);
                bonusStageRouletteRoutine = null;
            }

            if (bonusStagePanel != null)
            {
                bonusStagePanel.SetActive(false);
            }

            // CODEX BONUS PR5
            if (bonusMiniGameCleanupRoutine != null)
            {
                StopCoroutine(bonusMiniGameCleanupRoutine);
                bonusMiniGameCleanupRoutine = null;
            }

            if (activeBonusMiniGame != null)
            {
                activeBonusMiniGame.StopGame();
                activeBonusMiniGame = null;
            }

            crownsThisRun = 0;
            SaveCrownsIfNeeded();
            if (!awaitingBossPowerRewardChoice && !awaitingBossPowerDiscard && !awaitingBossPowerLossDiscard && !awaitingBossPowerLossConfirm)
            {
                SetBoardInputLock(false);
            }
        }

        // CODEX BONUS PR4
        private void BuildBonusStageUIIfNeeded()
        {
            if (bonusStagePanel != null)
            {
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("BonusStageUI missing Canvas.", this);
                return;
            }

            bonusStagePanel = new GameObject("BonusStagePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bonusStagePanel.transform.SetParent(canvas.transform, false);
            var panelRect = bonusStagePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = bonusStagePanel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            bonusStageText = CreateBonusStageText(
                bonusStagePanel.transform,
                "BonusStageHeader",
                48,
                TextAnchor.UpperCenter,
                new Vector2(0f, -40f),
                new Vector2(900f, 80f));
            bonusStageInstructionText = CreateBonusStageText(
                bonusStagePanel.transform,
                "BonusStageInstruction",
                28,
                TextAnchor.UpperCenter,
                new Vector2(0f, -130f),
                new Vector2(900f, 120f));

            var listRoot = new GameObject(
                "BonusStageList",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            listRoot.transform.SetParent(bonusStagePanel.transform, false);
            bonusStageListRoot = listRoot.transform;
            var listRect = listRoot.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0f, -60f);
            listRect.sizeDelta = new Vector2(700f, 620f);

            var layout = listRoot.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = listRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bonusStageRouletteText = CreateBonusStageText(
                bonusStagePanel.transform,
                "BonusStageRoulette",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 260f),
                new Vector2(900f, 60f));
            bonusStageResultText = CreateBonusStageText(
                bonusStagePanel.transform,
                "BonusStageResult",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 190f),
                new Vector2(900f, 90f));

            bonusStageContinueButton = CreateBonusStageButton(
                bonusStagePanel.transform,
                "BonusStageContinue",
                "Continue",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 80f),
                new Vector2(320f, 70f));
            ConfigureBonusStageButton();

            bonusStagePanel.SetActive(false);
        }

        // CODEX BONUS PR4
        private Text CreateBonusStageText(
            Transform parent,
            string name,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        // CODEX BONUS PR4
        private Button CreateBonusStageButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            var button = buttonObject.GetComponent<Button>();
            var text = CreateBonusStageLabel(buttonObject.transform, "Label", label);
            text.color = Color.black;
            return button;
        }

        // CODEX BONUS PR4
        private Text CreateBonusStageLabel(Transform parent, string name, string label)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<Text>();
            text.text = label;
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        // CODEX BONUS PR4
        private void PrepareBonusStageSelection()
        {
            bonusStageRandom = CreateBonusStageRandom();
            bonusStageBannedGame = null;
            bonusStageSelectedGame = null;
            awaitingBonusStageBan = true;
            bonusStageRouletteComplete = false;

            if (bonusStageText != null)
            {
                bonusStageText.text = "BONUS STAGE";
            }

            if (bonusStageInstructionText != null)
            {
                bonusStageInstructionText.text = "Choose ONE game to ban.";
            }

            if (bonusStageRouletteText != null)
            {
                bonusStageRouletteText.text = string.Empty;
            }

            if (bonusStageResultText != null)
            {
                bonusStageResultText.text = string.Empty;
            }

            if (bonusStageContinueButton != null)
            {
                bonusStageContinueButton.interactable = false;
                UpdateBonusStageButtonLabel("Continue");
            }

            if (bonusStageListRoot != null)
            {
                bonusStageListRoot.gameObject.SetActive(true);
            }

            RebuildBonusStageBanButtons();
        }

        // CODEX BONUS PR4
        private void RebuildBonusStageBanButtons()
        {
            foreach (var button in bonusStageBanButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            bonusStageBanButtons.Clear();

            if (bonusStageListRoot == null)
            {
                return;
            }

            foreach (var game in GetBonusStageGamePool())
            {
                var button = CreateBonusStageButton(
                    bonusStageListRoot,
                    $"Ban{game}Button",
                    $"BAN {game}",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    new Vector2(480f, 60f));
                var gameName = game;
                button.onClick.AddListener(() => HandleBonusStageBanSelected(gameName));
                bonusStageBanButtons.Add(button);
            }
        }

        // CODEX BONUS PR4
        private List<string> GetBonusStageGamePool()
        {
            if (bonusStageGames != null && bonusStageGames.Count > 0)
            {
                return bonusStageGames;
            }

            return new List<string>
            {
                "Chess",
                "Checkers",
                "Memory",
                "Sudoku",
                "Simon",
                "Minesweeper",
                "Wordle",
                "SlidingPuzzle",
                "MatchPairs"
            };
        }

        // CODEX BONUS PR4
        private void HandleBonusStageBanSelected(string gameName)
        {
            if (!awaitingBonusStageBan)
            {
                return;
            }

            awaitingBonusStageBan = false;
            bonusStageBannedGame = gameName;

            foreach (var button in bonusStageBanButtons)
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }

            if (bonusStageInstructionText != null)
            {
                bonusStageInstructionText.text = $"BANNED: {bonusStageBannedGame}";
            }

            bonusStageRemainingGames.Clear();
            foreach (var game in GetBonusStageGamePool())
            {
                if (!string.Equals(game, bonusStageBannedGame, StringComparison.OrdinalIgnoreCase))
                {
                    bonusStageRemainingGames.Add(game);
                }
            }

            if (bonusStageListRoot != null)
            {
                bonusStageListRoot.gameObject.SetActive(false);
            }

            if (bonusStageRouletteRoutine != null)
            {
                StopCoroutine(bonusStageRouletteRoutine);
            }

            bonusStageRouletteRoutine = StartCoroutine(BonusStageRouletteRoutine());
        }

        // CODEX BONUS PR4
        private IEnumerator BonusStageRouletteRoutine()
        {
            if (bonusStageRemainingGames.Count == 0)
            {
                yield break;
            }

            var duration = Mathf.Max(0.5f, bonusStageRouletteDuration);
            var step = Mathf.Max(0.04f, bonusStageRouletteStep);
            var elapsed = 0f;
            var index = bonusStageRandom != null
                ? bonusStageRandom.Next(bonusStageRemainingGames.Count)
                : UnityEngine.Random.Range(0, bonusStageRemainingGames.Count);

            while (elapsed < duration)
            {
                elapsed += step;
                index = (index + 1) % bonusStageRemainingGames.Count;
                if (bonusStageRouletteText != null)
                {
                    bonusStageRouletteText.text = $"Roulette: {bonusStageRemainingGames[index]}";
                }

                yield return new WaitForSeconds(step);
            }

            bonusStageSelectedGame = SelectBonusStageGame();
            bonusStageRouletteComplete = true;

            if (bonusStageRouletteText != null)
            {
                bonusStageRouletteText.text = $"Roulette Stopped";
            }

            UpdateBonusStageResult();
            bonusStageRouletteRoutine = null;
        }

        // CODEX BONUS PR4
        private string SelectBonusStageGame()
        {
            if (bonusStageRemainingGames.Count == 0)
            {
                return string.Empty;
            }

            var selectionIndex = bonusStageRandom != null
                ? bonusStageRandom.Next(bonusStageRemainingGames.Count)
                : UnityEngine.Random.Range(0, bonusStageRemainingGames.Count);
            return bonusStageRemainingGames[selectionIndex];
        }

        // CODEX BONUS PR4
        private void UpdateBonusStageResult()
        {
            if (bonusStageResultText != null)
            {
                bonusStageResultText.text = $"Selected: {bonusStageSelectedGame}";
            }

            if (bonusStageInstructionText != null)
            {
                bonusStageInstructionText.text = bonusStageSelectedGame == "Memory"
                    ? "Memory will launch next!"
                    : $"{bonusStageSelectedGame} is coming soon.";
            }

            if (bonusStageContinueButton != null)
            {
                bonusStageContinueButton.interactable = true;
                UpdateBonusStageButtonLabel(bonusStageSelectedGame == "Memory" ? "Play Memory" : "Continue");
            }
        }

        // CODEX BONUS PR4
        private void UpdateBonusStageButtonLabel(string label)
        {
            if (bonusStageContinueButton == null)
            {
                return;
            }

            var text = bonusStageContinueButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        // CODEX BONUS PR4
        private void HandleBonusStageContinue()
        {
            if (!isBonusStageActive || !bonusStageRouletteComplete)
            {
                return;
            }

            if (bonusStageSelectedGame == "Memory")
            {
                Debug.Log("BonusStageMemorySelected", this);
                StartBonusMiniGame(bonusStageSelectedGame);
                return;
            }
            else
            {
                Debug.Log($"BonusStage{bonusStageSelectedGame}ComingSoon", this);
            }

            EndBonusStage();
        }

        // CODEX BONUS PR5
        private void RegisterBonusMiniGame(string gameName, Func<BonusMiniGameBase> factory)
        {
            if (string.IsNullOrWhiteSpace(gameName) || factory == null)
            {
                return;
            }

            bonusMiniGameFactories[gameName] = factory;
        }

        // CODEX BONUS PR5
        private void StartBonusMiniGame(string gameName)
        {
            if (!bonusMiniGameFactories.TryGetValue(gameName, out var factory))
            {
                EndBonusStage();
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("BonusStageMemory missing Canvas.", this);
                EndBonusStage();
                return;
            }

            if (bonusStagePanel != null)
            {
                bonusStagePanel.SetActive(false);
            }

            if (activeBonusMiniGame != null)
            {
                activeBonusMiniGame.StopGame();
                activeBonusMiniGame = null;
            }

            activeBonusMiniGame = factory.Invoke();
            if (activeBonusMiniGame == null)
            {
                EndBonusStage();
                return;
            }

            activeBonusMiniGame.Completed += HandleBonusMiniGameCompleted;
            activeBonusMiniGame.Begin(canvas.transform, bonusStageRandom ?? CreateBonusStageRandom());
        }

        // CODEX BONUS PR5
        private BonusMiniGameBase CreateBonusMiniGame<T>() where T : BonusMiniGameBase
        {
            var gameObject = new GameObject(typeof(T).Name);
            return gameObject.AddComponent<T>();
        }

        // CODEX BONUS PR5
        private void HandleBonusMiniGameCompleted(bool success)
        {
            if (activeBonusMiniGame == null)
            {
                EndBonusStage();
                return;
            }

            activeBonusMiniGame.Completed -= HandleBonusMiniGameCompleted;
            if (success)
            {
                TryBeginBonusBossPowerRewardChoice();
            }

            if (bonusMiniGameCleanupRoutine != null)
            {
                StopCoroutine(bonusMiniGameCleanupRoutine);
            }

            bonusMiniGameCleanupRoutine = StartCoroutine(BonusMiniGameCleanupRoutine());
        }

        // CODEX BONUS PR5
        private IEnumerator BonusMiniGameCleanupRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            EndBonusStage();
            bonusMiniGameCleanupRoutine = null;
        }

        // CODEX BONUS PR4
        private System.Random CreateBonusStageRandom()
        {
            var seed = board != null ? board.RandomSeed : 0;
            return seed != 0 ? new System.Random(seed + 131) : new System.Random();
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
            RegisterBossPowerLostThisRun(pendingBossPowerLossDiscard);
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
                awaitingBonusBossPowerRewardChoice = pendingBonusBossPowerRewardChoice;
                pendingBonusBossPowerRewardChoice = false;
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
            RegisterBossPowerLostThisRun(power);
            Debug.Log($"Discarded boss power on loss: {power}", this);
        }

        // CODEX BONUS PR6
        private void RegisterBossPowerLostThisRun(BossPower power)
        {
            if (!bossPowersLostThisRun.Contains(power))
            {
                bossPowersLostThisRun.Add(power);
            }
        }

        // CODEX BONUS PR6
        private bool TryBeginBonusBossPowerRewardChoice()
        {
            if (bossPowerRewardPanel == null || bossPowerRewardButtons == null || bossPowerRewardButtons.Length == 0)
            {
                Debug.LogWarning("Bonus boss power reward UI is not configured; skipping reward choices.", this);
                return false;
            }

            bossPowerRewardOptions.Clear();
            bossPowerRewardOptions.AddRange(BuildBonusBossPowerRewardOptions());

            var optionsDebug = bossPowerRewardOptions.Count == 0
                ? "(none)"
                : string.Join(", ", bossPowerRewardOptions);
            Debug.Log($"RewardOptions: {optionsDebug}", this);

            if (bossPowerRewardOptions.Count == 0)
            {
                return false;
            }

            if (bossPowerInventory.Count >= bossPowerInventory.MaxSlots)
            {
                pendingBossPowerRewardAfterDiscard = true;
                pendingBonusBossPowerRewardChoice = true;
                if (TryShowBossPowerDiscardPanel(ResolveBossPowerDiscard))
                {
                    awaitingBossPowerDiscard = true;
                    SetBoardInputLock(true);
                }
                else
                {
                    pendingBossPowerRewardAfterDiscard = false;
                    pendingBonusBossPowerRewardChoice = false;
                    Debug.LogWarning("Boss power inventory is full and discard UI is not configured.", this);
                }

                return true;
            }

            awaitingBonusBossPowerRewardChoice = true;
            ShowBossPowerRewardPanel();
            return true;
        }

        // CODEX BONUS PR6
        private List<BossPower> BuildBonusBossPowerRewardOptions()
        {
            var options = new List<BossPower>();
            var lostCandidates = new List<BossPower>();

            for (var i = 0; i < bossPowersLostThisRun.Count; i++)
            {
                var power = bossPowersLostThisRun[i];
                if (bossPowerInventory != null && bossPowerInventory.HasPower(power))
                {
                    continue;
                }

                if (!lostCandidates.Contains(power))
                {
                    lostCandidates.Add(power);
                }
            }

            var randomSource = bonusStageRandom ?? new System.Random();
            ShuffleList(lostCandidates, randomSource);

            var desiredCount = 3;
            for (var i = 0; i < lostCandidates.Count && options.Count < desiredCount; i++)
            {
                options.Add(lostCandidates[i]);
            }

            if (options.Count >= desiredCount)
            {
                return options;
            }

            var newCandidates = new List<BossPower>();
            var powerValues = (BossPower[])Enum.GetValues(typeof(BossPower));
            for (var i = 0; i < powerValues.Length; i++)
            {
                var power = powerValues[i];
                if (bossPowerInventory != null && bossPowerInventory.HasPower(power))
                {
                    continue;
                }

                if (options.Contains(power) || bossPowersLostThisRun.Contains(power))
                {
                    continue;
                }

                newCandidates.Add(power);
            }

            ShuffleList(newCandidates, randomSource);

            for (var i = 0; i < newCandidates.Count && options.Count < desiredCount; i++)
            {
                options.Add(newCandidates[i]);
            }

            return options;
        }

        // CODEX BONUS PR6
        private void ShuffleList<T>(List<T> list, System.Random randomSource)
        {
            if (list == null || list.Count <= 1)
            {
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var swapIndex = randomSource != null
                    ? randomSource.Next(i, list.Count)
                    : UnityEngine.Random.Range(i, list.Count);
                var temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
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
            var isBonusReward = awaitingBonusBossPowerRewardChoice;
            awaitingBonusBossPowerRewardChoice = false;
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

            if (isBonusReward)
            {
                Debug.Log($"RewardChosen: {power}", this);
                if (bossManager != null)
                {
                    bossManager.RemoveBossFromRunPool(power);
                }
            }
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
