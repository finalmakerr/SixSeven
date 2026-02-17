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
        [SerializeField] private DifficultyMode difficultyMode = DifficultyMode.Normal;

        [Header("References")]
        [SerializeField] private Board board;
        [SerializeField] private GameBalanceConfig balanceConfig;
        // CODEX BOSS PR1
        [SerializeField] private BossManager bossManager;
        [SerializeField] private PlayerAnimationStateController playerAnimationStateController;

        [Header("UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text movesText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text inventoryText;
        [SerializeField] private GameObject inventoryOverflowPanel;
        [SerializeField] private Text inventoryOverflowPromptText;
        [SerializeField] private Text inventoryOverflowIncomingItemText;
        [SerializeField] private Button[] inventoryOverflowReplaceButtons;
        [SerializeField] private Button inventoryOverflowDestroyButton;
        [SerializeField] private Text shieldIconText;
        [SerializeField] private Text toxicWarningIconText;
        [SerializeField] private Text meditationText;
        [SerializeField] private Text bugadaText;
        // CODEX: LEVEL_LOOP
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        // CODEX BOSS PR1
        [SerializeField] private Text bossLabelText;
        // CODEX BOSS PR4
        [SerializeField] private Text bossPowersText;
        // CODEX BOSS PHASE PR1
        [SerializeField] private Text bossStateText;
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
        [SerializeField] private Text miniGoalsText; // CODEX REPLAYABILITY
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
        [SerializeField] private bool hardcoreModeEnabled;
        [SerializeField] private HardcoreConfig hardcoreConfig;
        [Header("Energy")]
        [SerializeField] private int maxEnergy = 3;
        [Header("Player Health")]
        [SerializeField] private int maxHP = 3;
        private int baseMaxEnergy;
        private int baseMaxHP;
        [Header("Player Inventory")]
        [SerializeField] private PlayerItemInventory playerItemInventory = new PlayerItemInventory(3);
        [Header("Pickup Radius")]
        [SerializeField] private int maxPickupRadius = 3;
        private const int BasePickupRadius = 1;
        [Header("Player Special Powers")]
        [SerializeField] private List<SpecialPowerDefinition> playerSpecialPowers = new List<SpecialPowerDefinition>();
        [Header("Monster Attack")]
        [SerializeField] private int monsterReachDistance = 2;
        [SerializeField] private MonsterAngerConfig monsterAngerConfig = new MonsterAngerConfig();
        [SerializeField] private GameObject monsterAttackMarkerPrefab;
        [SerializeField] private float monsterAttackVisualResetDelay = 0.4f;
        [Header("Bugada")]

        public static GameManager Instance { get; private set; }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public int CurrentLevel => CurrentLevelIndex;
        public DifficultyMode CurrentDifficulty => difficultyMode;
        public bool HasMetTarget { get; private set; }
        public int Energy => energy;
        public int MaxHP => maxHP;
        public int CurrentHP { get; private set; }
        public int PickupRadius => pickupRadius;
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
        public bool HasMonsterAttackTarget => CurrentBossState.IsAngry || CurrentBossState.IsEnraged || CurrentBossState.IsPermanentlyEnraged;
        public Vector2Int MonsterAttackTarget => CurrentBossState.AttackTarget;

        public bool IsHardcoreEnabled()
        {
            return hardcoreModeEnabled && hardcoreConfig != null;
        }

        public HardcoreConfig HardcoreConfig
        {
            get
            {
                return IsHardcoreEnabled() ? hardcoreConfig : null;
            }
        }

        public bool IsMonsterSwapLockedAtPosition(Vector2Int position)
        {
            return CurrentBossState.IsAngry && CurrentBossState.AggressorPosition == position;
        }

        public int GetEffectiveLevel()
        {
            if (IsHardcoreEnabled())
            {
                return CurrentLevel + HardcoreConfig.levelOffset;
            }

            return CurrentLevel;
        }

        public int GetEffectiveLevel(int baseLevel)
        {
            if (difficultyMode == DifficultyMode.Hardcore)
            {
                return Mathf.Max(1, baseLevel + balanceConfig.HardcoreLevelOffset);
            }

            return baseLevel;
        }

        public bool IsLevelScalingThreshold(int level)
        {
            return level > 0 && level % balanceConfig.LevelScalingStep == 0;
        }
        // CODEX BOSS PR4
        public BossPowerInventory BossPowerInventory => bossPowerInventory;
        public IReadOnlyList<SpecialPowerDefinition> PlayerSpecialPowers => playerSpecialPowers;

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
        private bool awaitingBossStatRewardChoice;
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
        [SerializeField] private int defaultBossPowerEnergyCost = 1;
        [SerializeField] private int defaultBossPowerCooldownTurns = 7;
        [SerializeField] private int maxBossPowersVisible = 3;
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
        private const string BonusStageFontKey = "MemoryBonusFont";
        private Font bonusStageFont;
        // CODEX BONUS PR5
        private Coroutine bonusMiniGameCleanupRoutine;
        private LevelRunDefinition currentRunDefinition; // CODEX REPLAYABILITY
        private readonly List<MiniGoalDefinition> activeMiniGoals = new List<MiniGoalDefinition>(); // CODEX REPLAYABILITY
        private int tumorsDestroyedThisLevel; // CODEX REPLAYABILITY
        private int turnsSurvivedThisLevel; // CODEX REPLAYABILITY
        private bool clearedPathGoalThisLevel; // CODEX REPLAYABILITY
        private readonly System.Random bossTumorRandom = new System.Random(); // CODEX BOSS TUMOR SYNERGY PR1
        private readonly List<ItemDropOption> itemDropOptionsBuffer = new List<ItemDropOption>();
        private bool awaitingInventoryOverflowDecision;
        private PlayerItemType pendingInventoryOverflowItem;
        private Vector2Int pendingInventoryOverflowPosition;
        private int energy;
        private bool applyRunStartResourceAdjustments;
        private int pickupRadius;
        private bool hasBossPickupRadiusUpgrade;
        private bool hasShopPickupRadiusUpgrade;
        private bool isShieldActive;
        private bool shieldJustActivated;
        private bool shieldCooldownJustStarted;
        private int shieldTurnsRemaining;
        private int shieldCooldownTurnsRemaining;
        private bool isMeditating;
        private int meditationTurnsRemaining;
        private int toxicStacks;
        private bool toxicDrainActive;
        private bool applyBottomLayerHazardOnNextTurn;
        private bool isHappy;
        private bool isExcited;
        private bool isStunned;
        private bool isWorried;
        private bool isTired;
        private bool hasGainedEnergy;
        private int bugadaTurnsRemaining;
        private bool bugadaJustActivated;
        private int stunnedTurnsRemaining;
        private bool skipMoveCostThisSwap;
        private GameObject monsterAttackMarkerInstance;
        private MonsterAttackTelegraph monsterAttackTelegraph;
        private MonsterAttackAnimationController monsterAttackAnimationController;
        private Coroutine monsterAttackVisualResetRoutine;
        private bool hasTriggeredMonsterWindup;
        private bool pendingMinorDamageAggro;
        // CODEX RAGE SCALE FINAL
        private bool monstersCanAttack;
        // CODEX RAGE SCALE FINAL
        private int rageCooldownTurns;
        // CODEX RAGE SCALE FINAL
        private int rageCooldownRemaining;
        private Piece monsterEnragePiece;
        private MonsterEnrageIndicator monsterEnrageIndicator;
        // CODEX BOSS PHASE PR1
        private readonly List<BossPower> unlockedBossPhasePowers = new List<BossPower>();
        private readonly Dictionary<SpecialPowerDefinition, int> specialPowerCooldowns = new Dictionary<SpecialPowerDefinition, int>();
        private readonly Dictionary<BossPower, int> bossPowerCooldowns = new Dictionary<BossPower, int>();
        private bool isBossPowerAccessActive;
        private int bossPowerSelectedIndex;
        private bool isActivatingSpecialPower;
        private SpecialPowerDefinition activeSpecialPower;
        private bool activeSpecialPowerAllowsHpModification;
        private bool isPlayerActionPhase;
        private bool isResolvingMonsterAttack;
        public bool IsPlayerStunned => playerAnimationStateController != null && playerAnimationStateController.IsStunned;
        public bool IsBugadaActive => bugadaTurnsRemaining > 0;
        public GameBalanceConfig BalanceConfig => balanceConfig;

        // CODEX CHEST PR2
        public int CrownsThisRun => crownsThisRun;

        private readonly struct ItemDropOption
        {
            public readonly PlayerItemType Type;
            public readonly int Weight;

            public ItemDropOption(PlayerItemType type, int weight)
            {
                Type = type;
                Weight = weight;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (balanceConfig == null)
            {
                Debug.LogError("GameBalanceConfig is not assigned in GameManager.");
            }

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

            if (playerItemInventory == null)
            {
                playerItemInventory = new PlayerItemInventory(3);
            }

            if (playerSpecialPowers == null)
            {
                playerSpecialPowers = new List<SpecialPowerDefinition>();
            }

            baseMaxEnergy = Mathf.Max(1, maxEnergy);
            baseMaxHP = Mathf.Max(1, maxHP);

            InitializeSpecialPowerCooldowns();
            InitializeBossPowerCooldowns();

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

            LoadLevelDatabaseFromSceneAssetLoader();

            LoadBonusStageAssetsFromSceneAssetLoader();
            ConfigureBossChallengeButtons();
            ConfigureBossPowerDiscardConfirmButtons();
            ConfigureInventoryOverflowButtons();
            ConfigureBonusStageButton();

            // CODEX BONUS PR5
            RegisterBonusMiniGame("Memory", () => CreateBonusMiniGame<MemoryBonusGame>());
        }

        private void OnEnable()
        {
            if (levelDatabase == null)
            {
                LoadLevelDatabaseFromSceneAssetLoader();
            }

            if (levelDatabase == null)
            {
                Debug.LogError("LevelDatabase not assigned. Ensure SceneAssetLoader has finished loading before enabling gameplay systems.", this);
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
            applyRunStartResourceAdjustments = true;

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
            awaitingBossStatRewardChoice = false;
            awaitingInventoryOverflowDecision = false;
            pendingInventoryOverflowItem = default;
            pendingInventoryOverflowPosition = default;
            HideInventoryOverflowPanel();
            if (applyRunStartResourceAdjustments)
            {
                RecalculateMaxResourcesFromBase();
                energy = maxEnergy;
                if (IsHardcoreEnabled())
                {
                    energy = Mathf.Max(1, energy - HardcoreConfig.startingEnergyPenalty);
                }

                applyRunStartResourceAdjustments = false;
            }
            else
            {
                energy = 0;
            }

            ResetPlayerHealth();
            ResetPickupRadius();
            playerItemInventory?.Clear();
            toxicStacks = 0;
            toxicDrainActive = false;
            applyBottomLayerHazardOnNextTurn = false;
            isPlayerActionPhase = false;
            isResolvingMonsterAttack = false;
            InitializeSpecialPowerCooldowns();
            InitializeBossPowerCooldowns();
            EndBossPowerAccess();
            SetShieldActive(false);
            shieldJustActivated = false;
            shieldCooldownJustStarted = false;
            shieldTurnsRemaining = 0;
            shieldCooldownTurnsRemaining = 0;
            CancelMeditation();
            ClearTransientEmotionFlags();
            isStunned = false;
            isWorried = false;
            isTired = false;
            hasGainedEnergy = false;
            ResetBugadaState();
            stunnedTurnsRemaining = 0;
            skipMoveCostThisSwap = false;
            ResetMonsterAttackState();
            // CODEX RAGE SCALE FINAL
            rageCooldownRemaining = 0;
            pendingMinorDamageAggro = false;
            HideComboText();
            displayedScore = Score;
            // CODEX: LEVEL_LOOP
            SetEndPanels(false, false);
            UpdateUI();
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
        }

        private void RecalculateMaxResourcesFromBase()
        {
            if (IsHardcoreEnabled())
            {
                maxEnergy = Mathf.Max(1, baseMaxEnergy - HardcoreConfig.maxEnergyCapReduction);
                maxHP = Mathf.Max(1, baseMaxHP - HardcoreConfig.maxHpCapReduction);
            }
            else
            {
                maxEnergy = baseMaxEnergy;
                maxHP = baseMaxHP;
            }
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
            var initialBossHp = CurrentBoss != null && CurrentBoss.maxHP > 0 ? CurrentBoss.maxHP : 100;
            CurrentBossState = new BossState
            {
                bossPosition = new Vector2Int(gridSize.x / 2, gridSize.y / 2),
                bossAlive = IsBossLevel,
                MaxHP = initialBossHp,
                CurrentHP = initialBossHp,
                CurrentPhaseIndex = 0,
                IsPermanentlyEnraged = false,
                IsAngry = false,
                IsEnraged = false,
                HasCharmResistance = false,
                AggressorPosition = default,
                AggressorPieceId = 0,
                AttackTarget = default,
                TurnsUntilAttack = 0,
                TumorShield = 0
            };
            currentRunDefinition = LevelRunGeneration.BuildRunDefinition(levelIndex, level, IsBossLevel); // CODEX REPLAYABILITY
            activeMiniGoals.Clear();
            if (currentRunDefinition.miniGoals != null)
            {
                activeMiniGoals.AddRange(currentRunDefinition.miniGoals);
            }
            tumorsDestroyedThisLevel = 0;
            turnsSurvivedThisLevel = 0;
            clearedPathGoalThisLevel = false;
            // CODEX RAGE SCALE FINAL
            monstersCanAttack = !IsBossLevel;
            // CODEX RAGE SCALE FINAL
            var effectiveLevel = GetEffectiveLevel();
            rageCooldownTurns = Mathf.Max(1, 7 - Mathf.FloorToInt(effectiveLevel / 10f));
            // CODEX RAGE SCALE FINAL
            rageCooldownRemaining = 0;
            pendingMinorDamageAggro = false;
            // CODEX BOSS PR4
            UpdateBombDetonationSubscription();

            if (board != null)
            {
                // CODEX DIFFICULTY PR7
                board.ConfigureColorCount(level.colorCount);
                board.SetRandomSeed(currentRunDefinition.seed);
                board.InitializeBoard(gridSize.x, gridSize.y);
                board.PlaceTumors(currentRunDefinition.tumors);
            }

            ResetGame();
            UpdateMiniGoalsUI();

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
            InitializeBossPhaseState();
        }

        // CODEX BOSS PHASE PR1
        private void InitializeBossPhaseState()
        {
            if (!IsBossLevel)
            {
                return;
            }

            var bossState = CurrentBossState;
            var maxHp = CurrentBoss != null && CurrentBoss.maxHP > 0 ? CurrentBoss.maxHP : 100;
            bossState.MaxHP = maxHp;
            bossState.CurrentHP = maxHp;
            bossState.CurrentPhaseIndex = 0;
            bossState.IsPermanentlyEnraged = false;
            bossState.IsAngry = false;
            bossState.IsEnraged = false;
            bossState.HasCharmResistance = false;
            bossState.AggressorPosition = default;
            bossState.AggressorPieceId = 0;
            bossState.AttackTarget = default;
            bossState.TurnsUntilAttack = 0;
            bossState.TumorShield = 0;
            CurrentBossState = bossState;
            unlockedBossPhasePowers.Clear();
            UpdateUI();
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
            var energyGain = 0;
            var reachedMaxEnergy = false;
            if (cascadeCount == 1)
            {
                energyGain = AddEnergyFromMatches(matchRunLengths, out reachedMaxEnergy);
            }
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

        public bool HasEnoughEnergy(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (energy < 0)
            {
                energy = 0;
            }

            return energy >= amount;
        }

        public bool TryEnergyHeal()
        {
            if (!CanUseManualAbility())
            {
                return false;
            }

            if (CurrentHP >= maxHP)
            {
                return false;
            }

            const int energyHealCost = 2;
            if (!HasEnoughEnergy(energyHealCost))
            {
                return false;
            }

            HealPlayer(1);
            TrySpendEnergy(energyHealCost);
            return true;
        }

        private void GainEnergy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (isActivatingSpecialPower)
            {
                Debug.LogWarning("Special powers cannot generate energy.", this);
                return;
            }

            if (energy < 0)
            {
                energy = 0;
            }

            energy = Mathf.Min(maxEnergy, energy + amount);
            if (energy > 0)
            {
                hasGainedEnergy = true;
            }

            UpdateTiredState();
            UpdateUI();
        }

        private void HealPlayer(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (isActivatingSpecialPower && !activeSpecialPowerAllowsHpModification)
            {
                Debug.LogWarning("Special powers cannot directly modify player HP.", this);
                return;
            }

            CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
            UpdateUI();
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

            if (shieldCooldownTurnsRemaining > 0)
            {
                return false;
            }

            if (playerItemInventory == null || !playerItemInventory.HasItem(PlayerItemType.Shield))
            {
                return false;
            }

            const int shieldEnergyCost = 2;
            if (!HasEnoughEnergy(shieldEnergyCost))
            {
                return false;
            }

            if (!playerItemInventory.TryConsumeItem(PlayerItemType.Shield))
            {
                return false;
            }

            if (!TrySpendEnergy(shieldEnergyCost))
            {
                playerItemInventory.TryAddItem(PlayerItemType.Shield);
                return false;
            }

            CancelMeditation();
            SetShieldActive(true);
            shieldTurnsRemaining = 2;
            shieldJustActivated = true;
            UpdateUI();
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
            if (!HasEnoughEnergy(meditationEnergyCost))
            {
                return false;
            }

            BeginMeditation(3);
            TrySpendEnergy(meditationEnergyCost);
            return true;
        }

        public bool TryUseEnergyPack()
        {
            if (!CanUseManualAbility())
            {
                return false;
            }

            if (energy >= maxEnergy)
            {
                return false;
            }

            if (playerItemInventory == null || !playerItemInventory.HasItem(PlayerItemType.EnergyPack))
            {
                return false;
            }

            if (!playerItemInventory.TryConsumeItem(PlayerItemType.EnergyPack))
            {
                return false;
            }

            GainEnergy(2);
            return true;
        }

        public bool TryBlockPlayerDamage(PlayerDamageType damageType)
        {
            if (IsBugadaActive)
            {
                return true;
            }

            if (damageType == PlayerDamageType.ToxicDrain)
            {
                return false;
            }

            if (!isShieldActive)
            {
                return false;
            }

            SetShieldActive(false);
            shieldTurnsRemaining = 0;
            BeginShieldCooldown();
            UpdateUI();
            return true;
        }

        private bool HasShieldVisual()
        {
            return isShieldActive || (playerItemInventory != null && playerItemInventory.HasItem(PlayerItemType.Shield));
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

            var maxRunLength = 0;
            foreach (var runLength in matchRunLengths)
            {
                if (runLength > maxRunLength)
                {
                    maxRunLength = runLength;
                }
            }

            var energyGain = GetEnergyGainForRun(maxRunLength);
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
            if (runLength >= 6)
            {
                return 3;
            }

            if (runLength >= balanceConfig.MinRunForLootRoll)
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

            isPlayerActionPhase = false;
            ClearTransientEmotionFlags();
            UpdatePlayerAnimationFlags();
            if (skipMoveCostThisSwap)
            {
                skipMoveCostThisSwap = false;
                return;
            }

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

            ApplyBottomLayerHazardIfNeeded();
            TickSpecialPowerCooldowns();
            TickBossPowerCooldowns();
            TryPickupAdjacentItems();

            var hasPlayerPosition = board.TryGetPlayerPosition(out var playerPosition);
            var isOnBottomRow = hasPlayerPosition && playerPosition.y == 0;
            if (!IsBugadaActive)
            {
                applyBottomLayerHazardOnNextTurn = isOnBottomRow;
            }
            if (!isOnBottomRow)
            {
                ClearToxicStacks();
            }
            else if (!IsBugadaActive)
            {
                toxicStacks += 1;
                if (toxicStacks >= balanceConfig.ToxicGraceStacks)
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

            TickBugadaDuration();
            TickShieldStatus();
            turnsSurvivedThisLevel += 1;
            EvaluateMiniGoalsProgress();
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
            UpdateMonsterEnrageVisuals();
            if (CanEnemiesReactAtTurnEnd())
            {
                TickRageCooldown(); // CODEX RAGE SCALE FINAL
                TickMonsterAttackMarker();
                TryTriggerMonsterEnrage();
                ProcessBossTumorTurn();
            }

            RegenerateEnergyAtPlayerTurnStart();
            HandleStartOfPlayerTurn();
            isPlayerActionPhase = true;
        }

        private void HandleStartOfPlayerTurn()
        {
            // Poison floor hook.
            // Do not implement logic here yet.
            // Next steps will add poison spread, debuff application, UI warning, etc.
        }

        private void RegenerateEnergyAtPlayerTurnStart()
        {
            if (maxEnergy <= 0)
            {
                energy = 0;
                hasGainedEnergy = false;
                return;
            }

            int previousEnergy = energy;
            energy = Mathf.Min(maxEnergy, energy + 1);
            hasGainedEnergy = energy > previousEnergy;
            UpdateTiredState();
            UpdateUI();
        }


        private void ApplyBottomLayerHazardIfNeeded()
        {
            if (!applyBottomLayerHazardOnNextTurn)
            {
                return;
            }

            applyBottomLayerHazardOnNextTurn = false;
            if (hasEnded || CurrentHP <= 0 || IsBugadaActive)
            {
                return;
            }

            ApplyPlayerDamage(1);
            ApplyEnergyDrain(1);
        }

        // CODEX BOSS TUMOR SYNERGY PR1
        private void ProcessBossTumorTurn()
        {
            if (!IsBossLevel || board == null || !CurrentBossState.bossAlive || CurrentBoss == null)
            {
                return;
            }

            var clampedMaxTier = Mathf.Clamp(CurrentBoss.maxBossTumorTier, 1, 4);
            for (var i = 0; i < Mathf.Max(0, CurrentBoss.tumorsPerTurn); i++)
            {
                board.TrySpawnBossTumor(bossTumorRandom, clampedMaxTier, out _);
            }

            for (var i = 0; i < Mathf.Max(0, CurrentBoss.tumorUpgradeAttemptsPerTurn); i++)
            {
                board.TryUpgradeBossTumor(bossTumorRandom, clampedMaxTier, out _, out _);
            }

            ApplyBossTumorPassiveEffects();
            EvaluateMiniGoalsProgress();
            UpdateUI();
        }

        // CODEX BOSS TUMOR SYNERGY PR1
        private void ApplyBossTumorPassiveEffects()
        {
            if (board == null || CurrentBoss == null)
            {
                return;
            }

            var tumorCount = board.CountTumorsAndTotalTier(out var totalTumorTier);
            if (tumorCount <= 0 || totalTumorTier <= 0)
            {
                return;
            }

            var bossState = CurrentBossState;
            if (CurrentBoss.tumorBehavior == BossTumorBehavior.HealFromTumors)
            {
                var healAmount = Mathf.Max(0, CurrentBoss.healPerTumorTier) * totalTumorTier;
                if (healAmount > 0)
                {
                    bossState.CurrentHP = Mathf.Clamp(bossState.CurrentHP + healAmount, 0, bossState.MaxHP);
                }
            }
            else if (CurrentBoss.tumorBehavior == BossTumorBehavior.ShieldFromTumors)
            {
                var shieldAmount = Mathf.Max(0, CurrentBoss.shieldPerTumorTier) * totalTumorTier;
                if (shieldAmount > 0)
                {
                    bossState.TumorShield += shieldAmount;
                }
            }

            CurrentBossState = bossState;
        }

        // CODEX BOSS TUMOR SYNERGY PR1
        private void HandleBossTumorDestroyed()
        {
            if (!IsBossLevel || !CurrentBossState.bossAlive || CurrentBoss == null)
            {
                return;
            }

            var bossState = CurrentBossState;
            if (bossState.TumorShield > 0)
            {
                bossState.TumorShield = Mathf.Max(0, bossState.TumorShield - 1);
                CurrentBossState = bossState;
            }
        }

        private bool CanEnemiesReactAtTurnEnd()
        {
            if (hasEnded || board == null)
            {
                return false;
            }

            if (isPlayerActionPhase)
            {
                return false;
            }

            if (board.IsBusy)
            {
                return false;
            }

            if (isResolvingMonsterAttack)
            {
                return false;
            }

            return true;
        }

        private void TryPickupAdjacentItems()
        {
            if (awaitingInventoryOverflowDecision || board == null)
            {
                return;
            }

            if (!board.TryGetPlayerPosition(out var playerPosition))
            {
                return;
            }

            var effectivePickupRadius = Mathf.Max(1, pickupRadius);
            for (var dx = -effectivePickupRadius; dx <= effectivePickupRadius; dx++)
            {
                for (var dy = -effectivePickupRadius; dy <= effectivePickupRadius; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var position = playerPosition + new Vector2Int(dx, dy);
                    if (DistanceManhattan(playerPosition, position) > effectivePickupRadius)
                    {
                        continue;
                    }

                    if (!board.TryGetPieceAt(position, out var piece))
                    {
                        continue;
                    }

                    if (piece.SpecialType != SpecialType.Item)
                    {
                        if (piece.SpecialType == SpecialType.Bugada)
                        {
                            ActivateBugada();
                            board.TryDestroyPieceAt(position, DestructionReason.ItemPickup);
                        }

                        continue;
                    }

                    var pickupResult = TryPickupItem(position);
                    if (pickupResult == PickupItemResult.PickedUp)
                    {
                        board.TryDestroyPieceAt(position, DestructionReason.ItemPickup);
                    }
                    else if (pickupResult == PickupItemResult.PendingDecision)
                    {
                        return;
                    }
                }
            }
        }

        private enum PickupItemResult
        {
            Failed = 0,
            PickedUp = 1,
            PendingDecision = 2
        }

        private PickupItemResult TryPickupItem(Vector2Int pickupPosition)
        {
            if (!TryRollItemTypeForPickup(out var itemType))
            {
                return PickupItemResult.Failed;
            }

            switch (itemType)
            {
                case PlayerItemType.BasicHeal:
                    HealPlayer(1);
                    return PickupItemResult.PickedUp;
                case PlayerItemType.EnergyPack:
                case PlayerItemType.Shield:
                case PlayerItemType.SecondChance:
                    if (TryAddInventoryItem(itemType))
                    {
                        UpdateShieldAnimationState();
                        UpdateUI();
                        return PickupItemResult.PickedUp;
                    }

                    BeginInventoryOverflowDecision(itemType, pickupPosition);
                    return PickupItemResult.PendingDecision;
                default:
                    return PickupItemResult.Failed;
            }
        }

        public bool CanRollItemDrop()
        {
            BuildWeightedItemDropOptions();
            return itemDropOptionsBuffer.Count > 0;
        }

        private bool TryRollItemTypeForPickup(out PlayerItemType itemType)
        {
            var options = BuildWeightedItemDropOptions();
            var totalWeight = 0;
            for (var i = 0; i < options.Count; i++)
            {
                totalWeight += options[i].Weight;
            }

            if (totalWeight <= 0)
            {
                itemType = PlayerItemType.BasicHeal;
                return false;
            }

            var roll = UnityEngine.Random.Range(0, totalWeight);
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (roll < option.Weight)
                {
                    itemType = option.Type;
                    return true;
                }

                roll -= option.Weight;
            }

            itemType = options[options.Count - 1].Type;
            return true;
        }

        private List<ItemDropOption> BuildWeightedItemDropOptions()
        {
            itemDropOptionsBuffer.Clear();

            var missingHp = Mathf.Max(0, maxHP - CurrentHP);
            var missingEnergy = Mathf.Max(0, maxEnergy - energy);
            var shieldCount = GetShieldCount();

            var potionDropChance = missingHp > 0 ? missingHp * 2 : 0;
            if (IsHardcoreEnabled())
            {
                potionDropChance *= HardcoreConfig.potionDropMultiplier;
            }

            AddWeightedDropOption(PlayerItemType.BasicHeal, Mathf.RoundToInt(potionDropChance));
            AddWeightedDropOption(PlayerItemType.EnergyPack, missingEnergy > 0 ? missingEnergy : 0);
            AddWeightedDropOption(PlayerItemType.Shield, CanCarryItem(PlayerItemType.Shield) ? (shieldCount == 0 ? 3 : 1) : 0);
            AddWeightedDropOption(PlayerItemType.SecondChance, CanCarryItem(PlayerItemType.SecondChance) ? (CurrentHP <= 1 ? 5 : 1) : 0);

            return itemDropOptionsBuffer;
        }

        private void AddWeightedDropOption(PlayerItemType itemType, int weight)
        {
            if (weight <= 0)
            {
                return;
            }

            itemDropOptionsBuffer.Add(new ItemDropOption(itemType, weight));
        }

        private bool TryAddInventoryItem(PlayerItemType itemType)
        {
            if (playerItemInventory == null)
            {
                playerItemInventory = new PlayerItemInventory(3);
            }

            return playerItemInventory.TryAddItem(itemType);
        }

        private bool CanCarryItem(PlayerItemType itemType)
        {
            if (itemType != PlayerItemType.Shield && itemType != PlayerItemType.SecondChance && itemType != PlayerItemType.EnergyPack)
            {
                return false;
            }

            if (playerItemInventory == null)
            {
                return true;
            }

            return playerItemInventory.Count < playerItemInventory.MaxSlots;
        }

        private int GetShieldCount()
        {
            var inventoryShieldCount = playerItemInventory != null ? playerItemInventory.CountOf(PlayerItemType.Shield) : 0;
            if (isShieldActive)
            {
                inventoryShieldCount += 1;
            }

            return inventoryShieldCount;
        }

        // CODEX STAGE 7D: Bugada activation + duration handling.
        private void ActivateBugada()
        {
            if (IsBugadaActive)
            {
                return;
            }

            bugadaTurnsRemaining = 3;
            bugadaJustActivated = true;
            ClearPlayerDebuffsForBugada();
            UpdateBugadaMusic(true);
            UpdateUI();
        }

        private void TickBugadaDuration()
        {
            if (bugadaTurnsRemaining <= 0)
            {
                return;
            }

            if (bugadaJustActivated)
            {
                bugadaJustActivated = false;
                return;
            }

            bugadaTurnsRemaining -= 1;
            if (bugadaTurnsRemaining <= 0)
            {
                EndBugadaEffect();
            }
            else
            {
                UpdateUI();
            }
        }

        private void EndBugadaEffect()
        {
            bugadaTurnsRemaining = 0;
            bugadaJustActivated = false;
            UpdateBugadaMusic(false);
            UpdateUI();
        }

        private void TickShieldStatus()
        {
            var updated = false;

            if (isShieldActive && shieldTurnsRemaining > 0)
            {
                if (shieldJustActivated)
                {
                    shieldJustActivated = false;
                }
                else
                {
                    shieldTurnsRemaining -= 1;
                    if (shieldTurnsRemaining <= 0)
                    {
                        SetShieldActive(false);
                        BeginShieldCooldown();
                        updated = true;
                    }
                }
            }

            if (shieldCooldownTurnsRemaining > 0)
            {
                if (shieldCooldownJustStarted)
                {
                    shieldCooldownJustStarted = false;
                }
                else
                {
                    shieldCooldownTurnsRemaining -= 1;
                    if (shieldCooldownTurnsRemaining < 0)
                    {
                        shieldCooldownTurnsRemaining = 0;
                    }
                    updated = true;
                }
            }

            if (updated)
            {
                UpdateUI();
            }
        }

        private void BeginShieldCooldown()
        {
            shieldCooldownTurnsRemaining = 3;
            shieldCooldownJustStarted = true;
        }

        private void ResetBugadaState()
        {
            bugadaTurnsRemaining = 0;
            bugadaJustActivated = false;
            UpdateBugadaMusic(false);
        }

        private void UpdateBugadaMusic(bool active)
        {
            var runtimeAudioService = audio_service.Instance;
            if (runtimeAudioService == null)
            {
                return;
            }

            if (active)
            {
                runtimeAudioService.play_music("music/bugada");
            }
            else
            {
                runtimeAudioService.stop_music();
            }
        }

        private void ClearBossAttackState()
        {
            var bossState = CurrentBossState;
            bossState.IsAngry = false;
            bossState.IsEnraged = false;
            bossState.IsPermanentlyEnraged = false;
            bossState.HasCharmResistance = false;
            bossState.AggressorPosition = Vector2Int.zero;
            bossState.AggressorPieceId = 0;
            bossState.AttackTarget = Vector2Int.zero;
            bossState.TurnsUntilAttack = 0;
            CurrentBossState = bossState;
            hasTriggeredMonsterWindup = false;
            RemoveBossTelegraph();
            UpdateWorriedState();
            UpdatePlayerAnimationFlags();
            UpdateMonsterEnrageVisuals();
        }

        private void ResetMonsterAttackState(bool delayVisualReset = false)
        {
            ClearBossAttackState();
            if (!delayVisualReset)
            {
                return;
            }

            monsterAttackVisualResetRoutine = StartCoroutine(DelayedMonsterAttackVisualReset());
        }

        private void SpawnBossTelegraph(Vector2Int targetPosition)
        {
            if (monsterAttackMarkerInstance != null)
            {
                return;
            }

            if (board == null || !board.IsWithinBounds(targetPosition))
            {
                return;
            }

            SpawnMonsterAttackMarker(targetPosition);
        }

        private void RemoveBossTelegraph()
        {
            if (monsterAttackVisualResetRoutine != null)
            {
                StopCoroutine(monsterAttackVisualResetRoutine);
                monsterAttackVisualResetRoutine = null;
            }

            DestroyMonsterAttackMarker();
            ClearMonsterEnrageIndicator();
        }

        // CODEX RAGE SCALE FINAL
        private void TickMonsterAttackMarker()
        {
            var bossState = CurrentBossState;
            if (!bossState.IsAngry && !bossState.IsEnraged && !bossState.IsPermanentlyEnraged)
            {
                return;
            }

            if (!IsAggressorAlive(bossState))
            {
                ClearBossAttackState();
                return;
            }

            if (bossState.IsAngry)
            {
                bossState.IsAngry = false;
                bossState.IsEnraged = true;
                CurrentBossState = bossState;
                TriggerMonsterAttackWindup();
                hasTriggeredMonsterWindup = true;
                SpawnBossTelegraph(bossState.AttackTarget);

                if (!board.IsWithinBounds(CurrentBossState.AttackTarget))
                {
                    ClearBossAttackState();
                    return;
                }

                UpdateMonsterAttackTelegraph();
                UpdateMonsterEnrageVisuals();
                return;
            }

            if (bossState.TurnsUntilAttack > 0)
            {
                bossState.TurnsUntilAttack--;
                CurrentBossState = bossState;

                if (!board.IsWithinBounds(CurrentBossState.AttackTarget))
                {
                    ClearBossAttackState();
                    return;
                }

                UpdateMonsterAttackTelegraph();
                return;
            }

            ResolveMonsterAttack(bossState.AttackTarget);
            ClearBossAttackState();
        }


        // CODEX RAGE SCALE FINAL
        private void TickRageCooldown()
        {
            if (rageCooldownRemaining <= 0)
            {
                return;
            }

            rageCooldownRemaining = Mathf.Max(0, rageCooldownRemaining - 1);
            if (debugMode)
            {
                Debug.Log($"RageCooldownRemaining: {rageCooldownRemaining}", this);
            }
        }

        // CODEX RAGE SCALE FINAL
        private void TryTriggerMonsterEnrage()
        {
            if (board == null)
            {
                return;
            }

            if (!monstersCanAttack)
            {
                return;
            }

            if (isPlayerActionPhase || board.IsBusy || isResolvingMonsterAttack)
            {
                return;
            }

            if (CurrentBossState.IsPermanentlyEnraged)
            {
                return;
            }

            if (monsterAngerConfig != null && monsterAngerConfig.maxAngryPerTurn <= 0)
            {
                return;
            }

            var bossState = CurrentBossState;
            if (bossState.IsAngry || bossState.IsEnraged)
            {
                return;
            }

            if (!board.TryGetPlayerPosition(out var playerPosition))
            {
                return;
            }

            var canUseAdjacency = monsterAngerConfig == null || monsterAngerConfig.angerAdjacencyRequired;
            var adjacencyCandidateFound = false;
            var bestScore = 0;
            var bestPosition = default(Vector2Int);
            var adjacentOffsets = new[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            if (canUseAdjacency)
            {
                for (var i = 0; i < adjacentOffsets.Length; i++)
                {
                    var candidatePosition = playerPosition + adjacentOffsets[i];
                    if (!board.TryGetPieceAt(candidatePosition, out _))
                    {
                        continue;
                    }

                    var score = board.GetMonsterRageMatchabilityScore(candidatePosition);
                    if (score <= 0)
                    {
                        continue;
                    }

                    if (!adjacencyCandidateFound || score > bestScore || (score == bestScore && (candidatePosition.y < bestPosition.y || (candidatePosition.y == bestPosition.y && candidatePosition.x < bestPosition.x))))
                    {
                        adjacencyCandidateFound = true;
                        bestScore = score;
                        bestPosition = candidatePosition;
                    }
                }
            }

            var damageTriggered = (monsterAngerConfig == null || monsterAngerConfig.allowDamageTrigger) && pendingMinorDamageAggro;
            var scalingTriggered = monsterAngerConfig != null && GetEffectiveLevel() >= monsterAngerConfig.aggressionScalingByLevel;

            if (!adjacencyCandidateFound && !damageTriggered && !scalingTriggered)
            {
                return;
            }

            var aggressorPosition = adjacencyCandidateFound ? bestPosition : FindNearestMonsterPosition(playerPosition);
            if (!aggressorPosition.HasValue)
            {
                pendingMinorDamageAggro = false;
                return;
            }

            if (!CanMonsterReachPlayer(aggressorPosition.Value, playerPosition))
            {
                pendingMinorDamageAggro = false;
                return;
            }

            pendingMinorDamageAggro = false;
            ActivateMonsterAnger(aggressorPosition.Value, playerPosition);
        }

        private bool IsAggressorAlive(BossState bossState)
        {
            if (bossState.IsPermanentlyEnraged)
            {
                return true;
            }

            return TryResolveAggressorPiece(ref bossState, out _);
        }

        private Vector2Int? FindNearestMonsterPosition(Vector2Int playerPosition)
        {
            if (board == null)
            {
                return null;
            }

            var bestDistance = int.MaxValue;
            Vector2Int? bestPosition = null;
            var width = board.Width;
            var height = board.Height;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (!board.TryGetPieceAt(position, out var piece) || piece.IsPlayer)
                    {
                        continue;
                    }

                    var distance = Mathf.Abs(playerPosition.x - x) + Mathf.Abs(playerPosition.y - y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPosition = position;
                    }
                }
            }

            return bestPosition;
        }

        private bool TryResolveAggressorPiece(ref BossState bossState, out Piece aggressor)
        {
            aggressor = null;
            if (board == null || bossState.AggressorPieceId == 0)
            {
                return false;
            }

            if (board.TryGetPieceAt(bossState.AggressorPosition, out var occupant)
                && !occupant.IsPlayer
                && occupant.GetInstanceID() == bossState.AggressorPieceId)
            {
                aggressor = occupant;
                return true;
            }

            var width = board.Width;
            var height = board.Height;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (!board.TryGetPieceAt(position, out var candidate) || candidate.IsPlayer)
                    {
                        continue;
                    }

                    if (candidate.GetInstanceID() != bossState.AggressorPieceId)
                    {
                        continue;
                    }

                    bossState.AggressorPosition = position;
                    aggressor = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool CanMonsterReachPlayer(Vector2Int monsterPosition, Vector2Int playerPosition)
        {
            return DistanceManhattan(monsterPosition, playerPosition) <= GetCurrentMonsterRange();
        }

        private bool IsBossInstantKillBlocked()
        {
            return CurrentBoss != null && CurrentBoss.preventInstantKillFromPlayerActions;
        }

        private void ActivateMonsterAnger(Vector2Int aggressorPosition, Vector2Int targetPosition)
        {
            var bossState = CurrentBossState;
            bossState.IsAngry = true;
            bossState.IsEnraged = false;
            bossState.HasCharmResistance = true;
            if (!board.TryGetPieceAt(aggressorPosition, out var aggressorPiece) || aggressorPiece.IsPlayer)
            {
                return;
            }

            bossState.AggressorPosition = aggressorPosition;
            bossState.AggressorPieceId = aggressorPiece.GetInstanceID();
            bossState.AttackTarget = targetPosition;
            bossState.TurnsUntilAttack = balanceConfig.BossAttackDelayTurns;
            CurrentBossState = bossState;
            hasTriggeredMonsterWindup = false;
            SpawnBossTelegraph(CurrentBossState.AttackTarget);
            UpdateMonsterAttackTelegraph();
            UpdateMonsterEnrageVisuals();
            UpdateWorriedState();
            UpdatePlayerAnimationFlags();
        }

        // CODEX RAGE SCALE FINAL
        private void ResolveMonsterAttack(Vector2Int targetPosition)
        {
            if (board == null || hasEnded)
            {
                return;
            }

            isResolvingMonsterAttack = true;
            try
            {
                TriggerMonsterAttackExecuteVisuals();

                if (!board.TryGetPieceAt(targetPosition, out var targetPiece))
                {
                    return;
                }

                if (targetPiece.IsPlayer)
                {
                    TriggerStunnedAnimation();
                    if (!TryBlockPlayerDamage(PlayerDamageType.HeavyHit))
                    {
                        ApplyPlayerDamage(GetCurrentMonsterDamage());
                    }

                    return;
                }

                board.TryDestroyPieceAt(targetPosition, DestructionReason.MonsterAttack);
            }
            finally
            {
                isResolvingMonsterAttack = false;
            }
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
                monsterAttackTelegraph = monsterAttackMarkerInstance.GetComponent<MonsterAttackTelegraph>();
                if (monsterAttackTelegraph == null)
                {
                    monsterAttackTelegraph = monsterAttackMarkerInstance.AddComponent<MonsterAttackTelegraph>();
                }
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
            monsterAttackTelegraph = markerObject.AddComponent<MonsterAttackTelegraph>();
        }

        private void DestroyMonsterAttackMarker()
        {
            if (monsterAttackMarkerInstance == null)
            {
                return;
            }

            Destroy(monsterAttackMarkerInstance);
            monsterAttackMarkerInstance = null;
            monsterAttackTelegraph = null;
        }

        private void InitializeSpecialPowerCooldowns()
        {
            specialPowerCooldowns.Clear();
            if (playerSpecialPowers == null)
            {
                return;
            }

            for (var i = 0; i < playerSpecialPowers.Count; i++)
            {
                var power = playerSpecialPowers[i];
                if (power == null)
                {
                    continue;
                }

                specialPowerCooldowns[power] = 0;
            }
        }

        private void TickSpecialPowerCooldowns()
        {
            if (specialPowerCooldowns.Count == 0)
            {
                return;
            }

            var powers = new List<SpecialPowerDefinition>(specialPowerCooldowns.Keys);
            for (var i = 0; i < powers.Count; i++)
            {
                var power = powers[i];
                var remaining = specialPowerCooldowns[power];
                if (remaining <= 0)
                {
                    continue;
                }

                specialPowerCooldowns[power] = Mathf.Max(0, remaining - 1);
            }
        }

        public int GetSpecialPowerCooldownRemaining(SpecialPowerDefinition power)
        {
            if (power == null)
            {
                return 0;
            }

            return specialPowerCooldowns.TryGetValue(power, out var remaining) ? remaining : 0;
        }

        public SpecialPowerActivationResult TryActivateSpecialPower(SpecialPowerDefinition power, SpecialPowerTarget target)
        {
            return TryActivateSpecialPower(power, target, null);
        }

        public SpecialPowerActivationResult TryActivateSpecialPower(
            SpecialPowerDefinition power,
            SpecialPowerTarget target,
            Action onActivate)
        {
            if (power == null)
            {
                var missing = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.MissingPower,
                    "Power was null.");
                LogSpecialPowerActivationFailure(power, missing);
                return missing;
            }

            if (playerSpecialPowers != null && !playerSpecialPowers.Contains(power))
            {
                var missing = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.MissingPower,
                    "Power is not available to the player.");
                LogSpecialPowerActivationFailure(power, missing);
                return missing;
            }

            if (hasEnded)
            {
                var ended = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.LevelEnded,
                    "Level has ended.");
                LogSpecialPowerActivationFailure(power, ended);
                return ended;
            }

            if (CurrentHP <= 0)
            {
                var dead = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.PlayerDead,
                    "Player is dead.");
                LogSpecialPowerActivationFailure(power, dead);
                return dead;
            }

            if (IsPlayerStunned)
            {
                var stunned = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.PlayerStunned,
                    "Player is stunned.");
                LogSpecialPowerActivationFailure(power, stunned);
                return stunned;
            }

            if (!isPlayerActionPhase)
            {
                var blocked = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.ActionBlocked,
                    "Player action phase is not active.");
                LogSpecialPowerActivationFailure(power, blocked);
                return blocked;
            }

            if (isMeditating)
            {
                var blocked = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.ActionBlocked,
                    "Player is meditating.");
                LogSpecialPowerActivationFailure(power, blocked);
                return blocked;
            }

            if (isResolvingMonsterAttack)
            {
                var blocked = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.ActionBlocked,
                    "Monster attack is resolving.");
                LogSpecialPowerActivationFailure(power, blocked);
                return blocked;
            }

            if (IsBossScriptedPhaseActive())
            {
                var blocked = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.ActionBlocked,
                    "Boss scripted phase is active.");
                LogSpecialPowerActivationFailure(power, blocked);
                return blocked;
            }

            if (board != null && board.IsBusy)
            {
                var busy = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.BoardBusy,
                    "Board is resolving.");
                LogSpecialPowerActivationFailure(power, busy);
                return busy;
            }

            if (isActivatingSpecialPower)
            {
                var blocked = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.ActivationInProgress,
                    "Special power activation is already in progress.");
                LogSpecialPowerActivationFailure(power, blocked);
                return blocked;
            }

            var cooldownRemaining = GetSpecialPowerCooldownRemaining(power);
            if (cooldownRemaining > 0)
            {
                var onCooldown = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.OnCooldown,
                    $"Cooldown remaining: {cooldownRemaining}.");
                LogSpecialPowerActivationFailure(power, onCooldown);
                return onCooldown;
            }

            if (power.EnergyCost < 0)
            {
                var invalidCost = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.InsufficientEnergy,
                    "Invalid energy cost.");
                LogSpecialPowerActivationFailure(power, invalidCost);
                return invalidCost;
            }

            if (energy < power.EnergyCost)
            {
                var insufficient = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.InsufficientEnergy,
                    "Not enough energy.");
                LogSpecialPowerActivationFailure(power, insufficient);
                return insufficient;
            }

            if (!TryValidateSpecialPowerTarget(power, target, out var targetFailure))
            {
                var invalidTarget = SpecialPowerActivationResult.Failed(targetFailure, "Invalid target.");
                LogSpecialPowerActivationFailure(power, invalidTarget);
                return invalidTarget;
            }

            if (power.EnergyCost > 0 && !TrySpendEnergy(power.EnergyCost))
            {
                var spendFailed = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.InsufficientEnergy,
                    "Not enough energy.");
                LogSpecialPowerActivationFailure(power, spendFailed);
                return spendFailed;
            }

            isActivatingSpecialPower = true;
            activeSpecialPower = power;
            activeSpecialPowerAllowsHpModification = power.AllowsDirectPlayerHpModification;
            try
            {
                onActivate?.Invoke();
                SetSpecialPowerCooldown(power, power.CooldownTurns);
            }
            finally
            {
                activeSpecialPower = null;
                activeSpecialPowerAllowsHpModification = false;
                isActivatingSpecialPower = false;
            }

            UpdateUI();
            return SpecialPowerActivationResult.Passed();
        }

        private bool TryValidateSpecialPowerTarget(
            SpecialPowerDefinition power,
            SpecialPowerTarget target,
            out SpecialPowerActivationFailureReason failureReason)
        {
            failureReason = SpecialPowerActivationFailureReason.None;
            if (power == null)
            {
                failureReason = SpecialPowerActivationFailureReason.MissingPower;
                return false;
            }

            if (power.TargetingMode == SpecialPowerTargetingMode.Self)
            {
                if (target.IsBoss)
                {
                    failureReason = SpecialPowerActivationFailureReason.InvalidTarget;
                    return false;
                }

                if (board == null || !board.TryGetPlayerPosition(out var playerPosition))
                {
                    failureReason = SpecialPowerActivationFailureReason.InvalidTarget;
                    return false;
                }

                if (target.HasPosition && target.Position != playerPosition)
                {
                    failureReason = SpecialPowerActivationFailureReason.InvalidTarget;
                    return false;
                }

                return true;
            }

            if (target.IsBoss)
            {
                if (!power.CanTargetBosses)
                {
                    failureReason = SpecialPowerActivationFailureReason.BossImmune;
                    return false;
                }

                if (!IsBossLevel || !CurrentBossState.bossAlive)
                {
                    failureReason = SpecialPowerActivationFailureReason.InvalidTarget;
                    return false;
                }

                if (CurrentBoss != null && !power.IgnoresBossImmunity)
                {
                    if (CurrentBoss.immuneTargetingModes != null
                        && CurrentBoss.immuneTargetingModes.Contains(power.TargetingMode))
                    {
                        failureReason = SpecialPowerActivationFailureReason.BossImmune;
                        return false;
                    }

                    if (CurrentBoss.specialPowerResistance == SpecialPowerBossResistance.Immune)
                    {
                        failureReason = SpecialPowerActivationFailureReason.BossImmune;
                        return false;
                    }
                }

                return true;
            }

            if (!target.HasPosition)
            {
                failureReason = SpecialPowerActivationFailureReason.InvalidTarget;
                return false;
            }

            if (board == null || !board.TryGetPieceAt(target.Position, out _))
            {
                failureReason = SpecialPowerActivationFailureReason.InvalidTarget;
                return false;
            }

            return true;
        }

        private void SetSpecialPowerCooldown(SpecialPowerDefinition power, int turns)
        {
            if (power == null)
            {
                return;
            }

            specialPowerCooldowns[power] = Mathf.Max(0, turns);
        }

        private void LogSpecialPowerActivationFailure(
            SpecialPowerDefinition power,
            SpecialPowerActivationResult result)
        {
            if (result.Success)
            {
                return;
            }

            var powerName = power != null ? power.Id : "UnknownPower";
            var reason = result.FailureReason;
            var message = string.IsNullOrEmpty(result.FailureMessage) ? reason.ToString() : result.FailureMessage;
            Debug.LogWarning($"Special power activation blocked: {powerName} ({reason}) {message}", this);
        }

        private bool IsBossScriptedPhaseActive()
        {
            return awaitingBossChallengeChoice
                || awaitingBossPowerRewardChoice
                || awaitingBossPowerDiscard
                || awaitingBossPowerLossDiscard
                || awaitingBossPowerLossConfirm
                || awaitingBossStatRewardChoice
                || awaitingBonusBossPowerRewardChoice
                || awaitingInventoryOverflowDecision
                || awaitingBonusStageBan
                || isBonusStageActive;
        }

        private bool IsPlayerActionBlocked(out string reason)
        {
            reason = null;
            if (hasEnded)
            {
                reason = "Level has ended.";
                return true;
            }

            if (!isPlayerActionPhase)
            {
                reason = "Player action phase is not active.";
                return true;
            }

            if (board != null && board.IsBusy)
            {
                reason = "Board is resolving.";
                return true;
            }

            if (isResolvingMonsterAttack)
            {
                reason = "Monster attack is resolving.";
                return true;
            }

            if (CurrentHP <= 0)
            {
                reason = "Player is dead.";
                return true;
            }

            if (IsPlayerStunned)
            {
                reason = "Player is stunned.";
                return true;
            }

            if (isMeditating)
            {
                reason = "Player is meditating.";
                return true;
            }

            if (IsBossScriptedPhaseActive())
            {
                reason = "Boss scripted phase is active.";
                return true;
            }

            return false;
        }

        public bool CanUseManualAbility()
        {
            return !IsPlayerActionBlocked(out _);
        }

        private void ClearToxicStacks()
        {
            toxicStacks = 0;
            toxicDrainActive = false;
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
        }

        private void ClearPlayerDebuffsForBugada()
        {
            ClearToxicStacks();
            applyBottomLayerHazardOnNextTurn = false;

            isStunned = false;
            stunnedTurnsRemaining = 0;
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

            if (piece.IsPlayer)
            {
                ClearBossAttackState();
            }

            TryDefeatBossFromDestruction(piece, reason);

            if (CurrentBossState.IsAngry || CurrentBossState.IsEnraged)
            {
                var bossState = CurrentBossState;
                if (monsterEnragePiece == piece || (bossState.AggressorPieceId != 0 && piece.GetInstanceID() == bossState.AggressorPieceId))
                {
                    ClearBossAttackState();
                }
            }

            if (piece.SpecialType == SpecialType.Tumor)
            {
                tumorsDestroyedThisLevel += 1;
                EvaluateMiniGoalsProgress();
                HandleBossTumorDestroyed();
            }

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


        private void EvaluateMiniGoalsProgress()
        {
            if (activeMiniGoals.Count == 0)
            {
                UpdateMiniGoalsUI();
                return;
            }

            if (!clearedPathGoalThisLevel && board != null && IsBossLevel && board.TryGetPlayerPosition(out var playerPosition))
            {
                var bossPosition = CurrentBossState.bossPosition;
                clearedPathGoalThisLevel = board.IsPathToBossClearOfTumors(playerPosition, bossPosition);
            }

            UpdateMiniGoalsUI();
        }

        private void UpdateMiniGoalsUI()
        {
            if (miniGoalsText == null)
            {
                return;
            }

            if (activeMiniGoals.Count == 0)
            {
                miniGoalsText.text = string.Empty;
                return;
            }

            var lines = new List<string>();
            for (var i = 0; i < activeMiniGoals.Count; i++)
            {
                var goal = activeMiniGoals[i];
                lines.Add(BuildMiniGoalLine(goal));
            }

            miniGoalsText.text = string.Join("\n", lines);
        }

        private string BuildMiniGoalLine(MiniGoalDefinition goal)
        {
            switch (goal.type)
            {
                case MiniGoalType.DestroyTumors:
                {
                    var target = Mathf.Max(1, goal.targetValue);
                    var progress = Mathf.Clamp(tumorsDestroyedThisLevel, 0, target);
                    return $"[Mini] Destroy tumors {progress}/{target}";
                }
                case MiniGoalType.SurviveTurns:
                {
                    var target = Mathf.Max(1, goal.targetValue);
                    var progress = Mathf.Clamp(turnsSurvivedThisLevel, 0, target);
                    return $"[Mini] Survive turns {progress}/{target}";
                }
                case MiniGoalType.ReachTile:
                {
                    var reached = board != null && !board.HasTumorAt(goal.targetTile);
                    var marker = reached ? "Done" : "Pending";
                    return $"[Mini] Reach tile ({goal.targetTile.x},{goal.targetTile.y}) {marker}";
                }
                case MiniGoalType.ClearPathToBoss:
                {
                    return $"[Mini] Clear path to boss {(clearedPathGoalThisLevel ? "Done" : "Pending")}";
                }
                default:
                    return "[Mini] Optional challenge";
            }
        }

        private void TryDefeatBossFromDestruction(Piece piece, DestructionReason reason)
        {
            if (!IsBossLevel)
            {
                return;
            }

            if (IsBossInstantKillBlocked())
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

            if (IsBossInstantKillBlocked())
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

            var phaseAdvanced = ApplyBossDamageAndPhaseProgress(1, source);
            if (CurrentBossState.bossAlive)
            {
                if (debugMode)
                {
                    Debug.Log($"BossHit {source} -> HP {CurrentBossState.CurrentHP}/{CurrentBossState.MaxHP} (phaseAdvanced={phaseAdvanced})", this);
                }

                return;
            }

            Debug.Log(
                $"BossDefeated {source} at {position.x},{position.y} boss at {bossState.bossPosition.x},{bossState.bossPosition.y}",
                this); // CODEX BOSS PR4

            TriggerInstantWin();
            TriggerWin();
        }


        // CODEX BOSS PHASE PR1
        private bool ApplyBossDamageAndPhaseProgress(int damage, string source)
        {
            if (damage <= 0)
            {
                return false;
            }

            var bossState = CurrentBossState;
            if (!bossState.bossAlive)
            {
                return false;
            }

            var previousPhase = bossState.CurrentPhaseIndex;
            if (bossState.TumorShield > 0)
            {
                var absorbed = Mathf.Min(bossState.TumorShield, damage);
                bossState.TumorShield -= absorbed;
                damage -= absorbed;
            }

            if (damage <= 0)
            {
                CurrentBossState = bossState;
                UpdateUI();
                return false;
            }

            var minorDamageThreshold = monsterAngerConfig != null ? Mathf.Max(1, monsterAngerConfig.minorDamageThreshold) : 1;
            if (damage <= minorDamageThreshold)
            {
                pendingMinorDamageAggro = true;
            }

            bossState.CurrentHP = Mathf.Max(0, bossState.CurrentHP - damage);
            if (bossState.CurrentHP <= 0)
            {
                bossState.bossAlive = false;
                CurrentBossState = bossState;
                ClearBossAttackState();
                UpdateUI();
                return false;
            }

            CurrentBossState = bossState;
            var phaseAdvanced = EvaluateBossPhaseTransitions();
            UpdateUI();
            if (debugMode)
            {
                Debug.Log($"BossPhaseCheck source={source} hp={CurrentBossState.CurrentHP}/{CurrentBossState.MaxHP} phase={previousPhase}->{CurrentBossState.CurrentPhaseIndex}", this);
            }

            return phaseAdvanced;
        }

        // CODEX BOSS PHASE PR1
        private bool EvaluateBossPhaseTransitions()
        {
            if (CurrentBoss == null)
            {
                return false;
            }

            var thresholds = CurrentBoss.phaseThresholdPercentages;
            if (thresholds == null || thresholds.Count == 0)
            {
                return false;
            }

            var bossState = CurrentBossState;
            var hpPercent = bossState.MaxHP > 0
                ? (bossState.CurrentHP * 100f) / bossState.MaxHP
                : 0f;
            var phaseAdvanced = false;

            while (bossState.CurrentPhaseIndex < thresholds.Count)
            {
                var threshold = Mathf.Clamp(thresholds[bossState.CurrentPhaseIndex], 1, 100);
                if (hpPercent > threshold)
                {
                    break;
                }

                bossState.CurrentPhaseIndex += 1;
                phaseAdvanced = true;
                UnlockBossPhasePower(bossState.CurrentPhaseIndex - 1);
            }

            var shouldEnrage = hpPercent <= 25f;
            if (shouldEnrage && !bossState.IsPermanentlyEnraged)
            {
                bossState.IsPermanentlyEnraged = true;
                bossState.IsAngry = false;
                bossState.IsEnraged = true;
                bossState.HasCharmResistance = true;
                if (CurrentBoss.enragePower != BossPower.None)
                {
                    unlockedBossPhasePowers.Add(CurrentBoss.enragePower);
                }
            }

            CurrentBossState = bossState;
            if (phaseAdvanced || shouldEnrage)
            {
                UpdateMonsterEnrageVisuals();
            }

            return phaseAdvanced;
        }

        // CODEX BOSS PHASE PR1
        private void UnlockBossPhasePower(int phaseIndex)
        {
            if (CurrentBoss == null || CurrentBoss.phaseUnlockedPowers == null)
            {
                return;
            }

            if (phaseIndex < 0 || phaseIndex >= CurrentBoss.phaseUnlockedPowers.Count)
            {
                return;
            }

            var power = CurrentBoss.phaseUnlockedPowers[phaseIndex];
            if (power == BossPower.None || unlockedBossPhasePowers.Contains(power))
            {
                return;
            }

            unlockedBossPhasePowers.Add(power);
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
            if (IsBossLevel && !CurrentBossState.bossAlive)
            {
                if (!TryBeginBossStatRewardChoice())
                {
                    var bossPowerRewardStarted = GrantBossPowerIfEligible();
                    if (!bossPowerRewardStarted)
                    {
                        SetBoardInputLock(false);
                    }
                }
            }
            OnWin?.Invoke();
        }

        private void TriggerLose()
        {
            if (hasEnded)
            {
                return;
            }

            EndBossPowerAccess();
            if (bossPowerInventory != null && bossPowerInventory.Count > 0)
            {
                DiscardRandomBossPower();
            }
            else
            {
                playerItemInventory?.Clear();
            }

            ResetPickupRadius();
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

        private void ResetPickupRadius()
        {
            pickupRadius = BasePickupRadius;
            hasBossPickupRadiusUpgrade = false;
            hasShopPickupRadiusUpgrade = false;
        }

        private bool TryApplyPickupRadiusUpgrade(bool fromShop)
        {
            if (fromShop)
            {
                if (hasShopPickupRadiusUpgrade)
                {
                    return false;
                }

                hasShopPickupRadiusUpgrade = true;
            }
            else
            {
                if (hasBossPickupRadiusUpgrade)
                {
                    return false;
                }

                hasBossPickupRadiusUpgrade = true;
            }

            pickupRadius = Mathf.Min(Mathf.Max(1, maxPickupRadius), pickupRadius + 1);
            return true;
        }

        public bool TryApplyShopPickupRadiusUpgrade()
        {
            return TryApplyPickupRadiusUpgrade(true);
        }

        private void ResetPlayerHealth()
        {
            if (maxHP < 1)
            {
                maxHP = 1;
            }

            CurrentHP = maxHP;
        }

        private int GetCurrentMonsterRange()
        {
            if (CurrentBoss == null)
            {
                return monsterReachDistance;
            }

            return CurrentBoss.range > 0 ? CurrentBoss.range : monsterReachDistance;
        }

        private int GetCurrentMonsterDamage()
        {
            if (CurrentBoss == null)
            {
                return 0;
            }

            var baseDamage = CurrentBoss.damage > 0
                ? CurrentBoss.damage
                : Mathf.Max(1, CurrentBoss.tier);

            if (!CurrentBossState.IsPermanentlyEnraged)
            {
                return baseDamage;
            }

            var multiplier = Mathf.Max(1f, CurrentBoss.enrageDamageMultiplier);
            var bonus = Mathf.Max(0, CurrentBoss.enrageExtraDamage);
            return Mathf.Max(baseDamage + 1, Mathf.CeilToInt(baseDamage * multiplier) + bonus);
        }

        private void ApplyPlayerDamage(int damage)
        {
            if (damage <= 0 || hasEnded || IsBugadaActive)
            {
                return;
            }

            if (isActivatingSpecialPower && !activeSpecialPowerAllowsHpModification)
            {
                Debug.LogWarning("Special powers cannot directly modify player HP.", this);
                return;
            }

            var remainingHp = CurrentHP - damage;
            if (remainingHp <= 0 && playerItemInventory != null && playerItemInventory.TryConsumeItem(PlayerItemType.SecondChance))
            {
                CurrentHP = 1;
                UpdateUI();
                return;
            }

            CurrentHP = Mathf.Max(0, remainingHp);
            if (CurrentHP <= 0)
            {
                TriggerLose();
            }

            UpdateUI();
        }

        public void TriggerJetpackDoubleSuccess()
        {
            TriggerExcitedAnimation();
        }

        public void RegisterJetpackMove()
        {
            skipMoveCostThisSwap = true;
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
            if (IsBugadaActive)
            {
                return;
            }

            if (!isStunned)
            {
                isStunned = true;
            }

            stunnedTurnsRemaining = Mathf.Max(stunnedTurnsRemaining, 1);
            ClearBossAttackState();
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
            var isMonsterThreat = false;
            if (board != null && (CurrentBossState.IsAngry || CurrentBossState.IsEnraged || CurrentBossState.IsPermanentlyEnraged))
            {
                if (board.TryGetPlayerPosition(out var playerPosition))
                {
                    isMonsterThreat = playerPosition == CurrentBossState.AttackTarget;
                }
            }
            isWorried = toxicStacks == 1 || isBombAdjacent || isMonsterThreat;
        }

        private void UpdateMonsterAttackTelegraph()
        {
            var bossState = CurrentBossState;
            if ((!bossState.IsAngry && !bossState.IsEnraged) || board == null)
            {
                DestroyMonsterAttackMarker();
                return;
            }

            if (!TryResolveAggressorPiece(ref bossState, out _))
            {
                CurrentBossState = bossState;
                ClearBossAttackState();
                return;
            }

            CurrentBossState = bossState;

            if (monsterAttackMarkerInstance == null &&
                (bossState.IsAngry || bossState.IsEnraged))
            {
                SpawnBossTelegraph(bossState.AttackTarget);
            }

#if UNITY_EDITOR
            if (bossState.IsEnraged && monsterAttackMarkerInstance == null)
            {
                Debug.LogWarning("Telegraph missing during enraged state.");
            }
#endif

            if (monsterAttackTelegraph != null)
            {
                var worldPosition = board.GridToWorld(bossState.AttackTarget.x, bossState.AttackTarget.y);
                monsterAttackTelegraph.SetWorldPosition(worldPosition);
                monsterAttackTelegraph.SetTurnsRemaining(bossState.TurnsUntilAttack);
            }
        }

        // CODEX RAGE SCALE FINAL
        private void UpdateMonsterEnrageVisuals()
        {
            if (board == null)
            {
                ClearMonsterEnrageIndicator();
                return;
            }

            var bossState = CurrentBossState;
            if (!bossState.IsAngry && !bossState.IsEnraged && !bossState.IsPermanentlyEnraged)
            {
                ClearMonsterEnrageIndicator();
                return;
            }

            Piece enragedPiece;
            if (bossState.IsPermanentlyEnraged)
            {
                var indicatorPosition = bossState.bossPosition;
                if (!board.TryGetPieceAt(indicatorPosition, out enragedPiece))
                {
                    ClearMonsterEnrageIndicator();
                    return;
                }
            }
            else
            {
                if (!TryResolveAggressorPiece(ref bossState, out enragedPiece))
                {
                    CurrentBossState = bossState;
                    ClearBossAttackState();
                    return;
                }

                CurrentBossState = bossState;
            }

            if (monsterEnragePiece == null)
            {
                monsterEnragePiece = enragedPiece;
                monsterEnrageIndicator = enragedPiece.GetComponent<MonsterEnrageIndicator>();
                if (monsterEnrageIndicator == null)
                {
                    monsterEnrageIndicator = enragedPiece.gameObject.AddComponent<MonsterEnrageIndicator>();
                }

                monsterAttackAnimationController = enragedPiece.GetComponent<MonsterAttackAnimationController>();
                if (monsterAttackAnimationController == null)
                {
                    monsterAttackAnimationController = enragedPiece.gameObject.AddComponent<MonsterAttackAnimationController>();
                }
            }
            else if (monsterEnragePiece != enragedPiece)
            {
                ClearMonsterEnrageIndicator();
                return;
            }

            monsterEnrageIndicator.SetEnraged(true);
            if (monsterAttackAnimationController != null)
            {
                monsterAttackAnimationController.SetEnraged(true);
            }
        }

        private void ClearMonsterEnrageIndicator()
        {
            if (monsterEnrageIndicator != null)
            {
                monsterEnrageIndicator.SetEnraged(false);
            }

            if (monsterAttackAnimationController != null)
            {
                monsterAttackAnimationController.SetEnraged(false);
            }

            monsterEnrageIndicator = null;
            monsterEnragePiece = null;
            monsterAttackAnimationController = null;
        }

        private void TriggerMonsterAttackWindup()
        {
            if (monsterAttackAnimationController != null)
            {
                monsterAttackAnimationController.TriggerWindup();
            }
        }

        private void TriggerMonsterAttackExecuteVisuals()
        {
            if (monsterAttackAnimationController != null)
            {
                monsterAttackAnimationController.TriggerAttackExecute();
            }

            if (monsterAttackTelegraph != null)
            {
                monsterAttackTelegraph.PlayImpactPulse();
            }
        }

        private IEnumerator DelayedMonsterAttackVisualReset()
        {
            var delay = monsterAttackVisualResetDelay;
            if (monsterAttackAnimationController != null)
            {
                delay = Mathf.Max(delay, monsterAttackAnimationController.AttackExecuteDuration);
            }

            yield return new WaitForSeconds(delay);
            DestroyMonsterAttackMarker();
            ClearMonsterEnrageIndicator();
            monsterAttackVisualResetRoutine = null;
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
                HasShieldVisual(),
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
                var bossState = CurrentBossState;
                var phaseLabel = bossState.CurrentPhaseIndex > 0 ? $" P{bossState.CurrentPhaseIndex + 1}" : string.Empty;
                var enrageLabel = bossState.IsPermanentlyEnraged ? " ENRAGED" : string.Empty;
                bossLabelText.text = IsBossLevel ? $"BOSS{phaseLabel}{enrageLabel}" : "BOSS";
                bossLabelText.enabled = IsBossLevel;
                bossLabelText.color = bossState.IsPermanentlyEnraged ? new Color(1f, 0.2f, 0.2f, 1f) : Color.white;
            }

            if (bossStateText != null)
            {
                if (!IsBossLevel)
                {
                    bossStateText.enabled = false;
                }
                else
                {
                    var bossState = CurrentBossState;
                    bossStateText.enabled = true;
                    var shieldLabel = bossState.TumorShield > 0 ? $"  |  Shield {bossState.TumorShield}" : string.Empty;
                    bossStateText.text = $"HP {bossState.CurrentHP}/{bossState.MaxHP}{shieldLabel}  |  Phase {bossState.CurrentPhaseIndex + 1}{(bossState.IsPermanentlyEnraged ? "  |  ENRAGE" : string.Empty)}";
                    bossStateText.color = bossState.IsPermanentlyEnraged ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white;
                }
            }

            // CODEX BOSS PR4
            UpdateBossPowerUI();

            if (energyText != null)
            {
                energyText.text = $"Energy: {energy}/{maxEnergy}";
            }

            if (shieldIconText != null)
            {
                shieldIconText.enabled = HasShieldVisual();
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

            if (bugadaText != null)
            {
                var showBugada = bugadaTurnsRemaining > 0;
                bugadaText.enabled = showBugada;
                if (showBugada)
                {
                    bugadaText.text = $"Bugada: {bugadaTurnsRemaining}";
                }
            }

            if (inventoryText != null && playerItemInventory != null)
            {
                inventoryText.text = playerItemInventory.BuildDisplayString();
            }
        }

        private void ConfigureInventoryOverflowButtons()
        {
            if (inventoryOverflowReplaceButtons != null)
            {
                for (var i = 0; i < inventoryOverflowReplaceButtons.Length; i++)
                {
                    var button = inventoryOverflowReplaceButtons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    var slotIndex = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => ResolveInventoryOverflowByReplacing(slotIndex));
                }
            }

            if (inventoryOverflowDestroyButton != null)
            {
                inventoryOverflowDestroyButton.onClick.RemoveAllListeners();
                inventoryOverflowDestroyButton.onClick.AddListener(ResolveInventoryOverflowByDestroyingLoot);
            }

            HideInventoryOverflowPanel();
        }

        private void BeginInventoryOverflowDecision(PlayerItemType itemType, Vector2Int pickupPosition)
        {
            if (awaitingInventoryOverflowDecision)
            {
                return;
            }

            awaitingInventoryOverflowDecision = true;
            pendingInventoryOverflowItem = itemType;
            pendingInventoryOverflowPosition = pickupPosition;

            SetBoardInputLock(true);
            ShowInventoryOverflowPanel();
            UpdateUI();
        }

        private void ShowInventoryOverflowPanel()
        {
            if (inventoryOverflowPanel == null)
            {
                Debug.LogWarning("Inventory overflow UI not configured. Destroying loot by default.", this);
                ResolveInventoryOverflowByDestroyingLoot();
                return;
            }

            inventoryOverflowPanel.SetActive(true);

            if (inventoryOverflowPromptText != null)
            {
                inventoryOverflowPromptText.text = "Inventory full! Choose: replace an existing item or destroy the new loot.";
            }

            if (inventoryOverflowIncomingItemText != null)
            {
                inventoryOverflowIncomingItemText.text = $"New loot: {pendingInventoryOverflowItem}";
            }

            var inventoryCount = playerItemInventory != null ? playerItemInventory.Count : 0;
            if (inventoryOverflowReplaceButtons != null)
            {
                for (var i = 0; i < inventoryOverflowReplaceButtons.Length; i++)
                {
                    var button = inventoryOverflowReplaceButtons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    var canReplace = i < inventoryCount;
                    button.gameObject.SetActive(canReplace);

                    var label = button.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = canReplace
                            ? $"Replace Slot {i + 1}: {playerItemInventory.Items[i]}"
                            : $"Replace Slot {i + 1}";
                    }
                }
            }
        }

        private void HideInventoryOverflowPanel()
        {
            if (inventoryOverflowPanel != null)
            {
                inventoryOverflowPanel.SetActive(false);
            }
        }

        private void ResolveInventoryOverflowByReplacing(int slotIndex)
        {
            if (!awaitingInventoryOverflowDecision || playerItemInventory == null)
            {
                return;
            }

            if (!playerItemInventory.TryReplaceItemAt(slotIndex, pendingInventoryOverflowItem))
            {
                return;
            }

            CompleteInventoryOverflowDecision();
        }

        private void ResolveInventoryOverflowByDestroyingLoot()
        {
            if (!awaitingInventoryOverflowDecision)
            {
                return;
            }

            CompleteInventoryOverflowDecision();
        }

        private void CompleteInventoryOverflowDecision()
        {
            if (board != null)
            {
                board.TryDestroyPieceAt(pendingInventoryOverflowPosition, DestructionReason.ItemPickup);
            }

            awaitingInventoryOverflowDecision = false;
            pendingInventoryOverflowItem = default;
            pendingInventoryOverflowPosition = default;
            HideInventoryOverflowPanel();
            SetBoardInputLock(false);
            UpdateShieldAnimationState();
            UpdateUI();
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

            if (!fightBoss)
            {
                ClearBossAttackState();
            }

            if (fightBoss)
            {
                EnsureBossSelected();
                InitializeBossPhaseState();
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

        private void LoadBonusStageAssetsFromSceneAssetLoader()
        {
            var sceneAssetLoader = FindObjectOfType<SceneAssetLoader>();
            if (sceneAssetLoader == null)
            {
                Debug.LogWarning("SceneAssetLoader not found; using default UI font for bonus stage.", this);
                return;
            }

            var catalog = sceneAssetLoader.GetLoadedAsset<MemoryBonusAssetCatalog>();
            if (catalog == null)
            {
                Debug.LogWarning("MemoryBonusAssetCatalog not found in SceneAssetGroup; using default UI font for bonus stage.", this);
                return;
            }

            bonusStageFont = catalog.GetFont(BonusStageFontKey);
            if (bonusStageFont == null)
            {
                Debug.LogWarning($"Missing font key '{BonusStageFontKey}' in MemoryBonusAssetCatalog; using default UI font for bonus stage.", this);
            }
        }
        private void LoadLevelDatabaseFromSceneAssetLoader()
        {
            var sceneAssetLoader = FindObjectOfType<SceneAssetLoader>();
            if (sceneAssetLoader == null)
            {
                Debug.LogWarning("SceneAssetLoader not found; LevelDatabase must be assigned manually.", this);
                return;
            }

            if (!sceneAssetLoader.IsLoaded)
            {
                Debug.LogWarning("SceneAssetLoader has not finished loading; LevelDatabase may not be available yet.", this);
            }

            var loadedLevelDatabase = sceneAssetLoader.GetLoadedAsset<LevelDatabase>();
            if (loadedLevelDatabase == null)
            {
                Debug.LogWarning("LevelDatabase not found in SceneAssetGroup.", this);
                return;
            }

            levelDatabase = loadedLevelDatabase;
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
            if (bonusStageFont != null)
            {
                text.font = bonusStageFont;
            }
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
            if (bonusStageFont != null)
            {
                text.font = bonusStageFont;
            }
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
        private bool GrantBossPowerIfEligible()
        {
            if (!IsBossLevel || CurrentBossState.bossAlive)
            {
                return false;
            }

            if (bossPowerInventory == null)
            {
                bossPowerInventory = new BossPowerInventory(3);
            }

            // CODEX POWER PR5
            if (!TryBeginBossPowerRewardChoice())
            {
                return false;
            }

            return true;
        }

        private bool TryBeginBossStatRewardChoice()
        {
            if (bossPowerRewardPanel == null || bossPowerRewardButtons == null || bossPowerRewardButtons.Length == 0)
            {
                Debug.LogWarning("Boss reward UI is not configured; skipping reward choices.", this);
                return false;
            }

            awaitingBossStatRewardChoice = true;
            bossPowerRewardPanel.SetActive(true);

            if (bossPowerRewardPromptText != null)
            {
                bossPowerRewardPromptText.text = "Choose a reward.";
            }

            for (var i = 0; i < bossPowerRewardButtons.Length; i++)
            {
                var button = bossPowerRewardButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i == 0)
                {
                    ConfigureBossStatRewardButton(button, "+1 Heart", true);
                }
                else if (i == 1)
                {
                    ConfigureBossStatRewardButton(button, "+1 Max Energy", false);
                }
                else
                {
                    button.onClick.RemoveAllListeners();
                    button.gameObject.SetActive(false);
                }
            }

            SetBoardInputLock(true);
            return true;
        }

        private void ConfigureBossStatRewardButton(Button button, string label, bool isHeartReward)
        {
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            var textLabel = button.GetComponentInChildren<Text>();
            if (textLabel != null)
            {
                textLabel.text = label;
            }

            if (isHeartReward)
            {
                button.onClick.AddListener(() => ResolveBossStatRewardChoice(true));
            }
            else
            {
                button.onClick.AddListener(() => ResolveBossStatRewardChoice(false));
            }
        }

        private void ResolveBossStatRewardChoice(bool chooseHeart)
        {
            if (!awaitingBossStatRewardChoice)
            {
                return;
            }

            awaitingBossStatRewardChoice = false;
            if (bossPowerRewardPanel != null)
            {
                bossPowerRewardPanel.SetActive(false);
            }

            if (chooseHeart)
            {
                maxHP = Mathf.Max(1, maxHP + 1);
                CurrentHP = Mathf.Min(maxHP, CurrentHP + 1);
            }
            else
            {
                maxEnergy = Mathf.Max(1, maxEnergy + 1);
                energy = Mathf.Min(maxEnergy, energy);
            }

            UpdateUI();

            var bossPowerRewardStarted = GrantBossPowerIfEligible();
            if (!bossPowerRewardStarted)
            {
                SetBoardInputLock(false);
            }
        }

        // CODEX BOSS PR4
        private void UpdateBossPowerUI()
        {
            if (bossPowersText == null || bossPowerInventory == null)
            {
                return;
            }

            if (isBossPowerAccessActive && bossPowerInventory.Count > 0)
            {
                var powers = bossPowerInventory.Powers;
                var builder = new System.Text.StringBuilder();
                builder.Append("Boss Powers (hold): ");
                var clampedSelection = Mathf.Clamp(bossPowerSelectedIndex, 0, powers.Count - 1);
                var visible = Mathf.Clamp(maxBossPowersVisible, 1, bossPowerInventory.MaxSlots);
                var start = Mathf.Clamp(clampedSelection - (visible / 2), 0, Mathf.Max(0, powers.Count - visible));
                var end = Mathf.Min(powers.Count, start + visible);
                for (var i = start; i < end; i++)
                {
                    if (i > start)
                    {
                        builder.Append(" | ");
                    }

                    var power = powers[i];
                    var cooldown = GetBossPowerCooldownRemaining(power);
                    if (i == clampedSelection)
                    {
                        builder.Append('[');
                    }

                    builder.Append(power);
                    if (cooldown > 0)
                    {
                        builder.Append($" CD:{cooldown}");
                    }
                    else
                    {
                        builder.Append(" READY");
                    }

                    if (i == clampedSelection)
                    {
                        builder.Append(']');
                    }
                }

                if (powers.Count > visible)
                {
                    builder.Append("  < >");
                }

                bossPowersText.text = builder.ToString();
                return;
            }

            bossPowersText.text = bossPowerInventory.BuildDisplayString();
        }


        private void InitializeBossPowerCooldowns()
        {
            bossPowerCooldowns.Clear();
            if (bossPowerInventory == null)
            {
                return;
            }

            var powers = bossPowerInventory.Powers;
            for (var i = 0; i < powers.Count; i++)
            {
                var power = powers[i];
                if (power == BossPower.None)
                {
                    continue;
                }

                bossPowerCooldowns[power] = 0;
            }
        }

        private void TickBossPowerCooldowns()
        {
            if (bossPowerCooldowns.Count == 0)
            {
                return;
            }

            var powers = new List<BossPower>(bossPowerCooldowns.Keys);
            for (var i = 0; i < powers.Count; i++)
            {
                var power = powers[i];
                var remaining = bossPowerCooldowns[power];
                if (remaining <= 0)
                {
                    continue;
                }

                bossPowerCooldowns[power] = Mathf.Max(0, remaining - 1);
            }

            if (isBossPowerAccessActive)
            {
                UpdateBossPowerUI();
            }
        }

        public bool HasBossPowersForAccess()
        {
            return bossPowerInventory != null && bossPowerInventory.Count > 0;
        }

        public bool IsBossPowerAccessActive => isBossPowerAccessActive;

        public void BeginBossPowerAccess()
        {
            if (!HasBossPowersForAccess())
            {
                return;
            }

            isBossPowerAccessActive = true;
            bossPowerSelectedIndex = Mathf.Clamp(bossPowerSelectedIndex, 0, bossPowerInventory.Count - 1);
            UpdateBossPowerUI();
        }

        public void EndBossPowerAccess()
        {
            if (!isBossPowerAccessActive)
            {
                return;
            }

            isBossPowerAccessActive = false;
            UpdateBossPowerUI();
        }

        public void CycleBossPowerSelection(int direction)
        {
            if (!isBossPowerAccessActive || bossPowerInventory == null || bossPowerInventory.Count == 0 || direction == 0)
            {
                return;
            }

            var count = bossPowerInventory.Count;
            bossPowerSelectedIndex = (bossPowerSelectedIndex + direction) % count;
            if (bossPowerSelectedIndex < 0)
            {
                bossPowerSelectedIndex += count;
            }

            UpdateBossPowerUI();
        }

        public int GetBossPowerCooldownRemaining(BossPower power)
        {
            if (power == BossPower.None)
            {
                return 0;
            }

            return bossPowerCooldowns.TryGetValue(power, out var remaining) ? remaining : 0;
        }

        public bool TryUseSelectedBossPower()
        {
            if (!isBossPowerAccessActive || bossPowerInventory == null || bossPowerInventory.Count == 0)
            {
                return false;
            }

            var index = Mathf.Clamp(bossPowerSelectedIndex, 0, bossPowerInventory.Count - 1);
            return TryUseBossPower(bossPowerInventory.Powers[index]);
        }

        private bool TryUseBossPower(BossPower power)
        {
            if (power == BossPower.None || bossPowerInventory == null || !bossPowerInventory.HasPower(power))
            {
                return false;
            }

            if (!CanUseManualAbility())
            {
                return false;
            }

            if (GetBossPowerCooldownRemaining(power) > 0)
            {
                return false;
            }

            var energyCost = Mathf.Max(0, defaultBossPowerEnergyCost);
            if (energyCost > 0 && !TrySpendEnergy(energyCost))
            {
                return false;
            }

            bossPowerCooldowns[power] = Mathf.Max(0, defaultBossPowerCooldownTurns);
            UpdateUI();
            return true;
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
                bossPowerCooldowns.Remove(pendingBossPowerLossDiscard);
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
                bossPowerCooldowns.Remove(power);
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
            bossPowerCooldowns.Remove(power);
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
            if (added)
            {
                bossPowerCooldowns[power] = 0;
                if (power == BossPower.PickupMagnet)
                {
                    TryApplyPickupRadiusUpgrade(false);
                }
            }
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
            InitializeBossPowerCooldowns();
        }
    }
}
