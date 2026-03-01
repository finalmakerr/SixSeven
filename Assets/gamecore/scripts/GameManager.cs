using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SixSeven.Systems;
using SixSeven.Core;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        public enum DamageSource
        {
            Player,
            Monster,
            Boss,
            Bomb,
            Hazard,
            Spell
        }

        private const string HeatmapTutorialKey = "HeatmapTutorialShown";

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
        [SerializeField] private BottomRowHazardEffect bottomRowHazardEffect;
        [SerializeField] private GameBalanceConfig balanceConfig;
        // CODEX BOSS PR1
        [SerializeField] private BossManager bossManager;
        [SerializeField] private ShopManager shopManager;
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
        [SerializeField] private Button inventoryOverflowCancelButton;
        [SerializeField] private Text shieldIconText;
        [SerializeField] private Text toxicWarningIconText;
        [SerializeField] private Text meditationText;
        [SerializeField] private Text bugadaText;
        [SerializeField] private GameObject iceStatusIcon;
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
        [SerializeField] private CanvasGroup hazardTransitionCanvas;
        [SerializeField] private Text hazardTransitionText;
        [SerializeField] private AudioSource hazardTransitionAudio;
        [SerializeField] private AudioClip poisonTransitionClip;
        [SerializeField] private AudioClip fireTransitionClip;
        [SerializeField] private AudioClip iceTransitionClip;
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
        [SerializeField] private int maxEnergyCap = 7; // CODEX STAGE 10C
        [Header("Player Health")]
        [SerializeField] private int maxHearts = 1;
        [SerializeField] private int maxHeartsCap = 7; // CODEX STAGE 10C
        [SerializeField] private PlayerVitalsSystem playerVitalsSystem = new PlayerVitalsSystem();
        [SerializeField] private BossVitalsSystem bossVitalsSystem = new BossVitalsSystem();
        private int baseMaxEnergy;
        private int baseMaxHearts;
        [Header("Player Inventory")]
        [SerializeField] private PlayerItemInventory playerItemInventory = new PlayerItemInventory(3);
        [Header("Pickup Radius")]
        [SerializeField] private int maxPickupRadius = 3;
        [Header("Player Special Powers")]
        [SerializeField] private List<SpecialPowerDefinition> playerSpecialPowers = new List<SpecialPowerDefinition>();
        [Header("Monster Attack")]
        [SerializeField] private int monsterReachDistance = 2;
        [SerializeField] private int telekinesisCost = 1;
        [SerializeField] private MonsterAngerConfig monsterAngerConfig = new MonsterAngerConfig();
        [SerializeField] private MonsterAggroConfig monsterAggroConfig = new MonsterAggroConfig();
        [SerializeField] private GameObject monsterAttackMarkerPrefab;
        [SerializeField] private GameObject genericMonsterTelegraphPrefab;
        [SerializeField] private float monsterAttackVisualResetDelay = 0.4f;
        [SerializeField] private DamageHeatmapSystem damageHeatmapSystem;
        [SerializeField] private DamageHeatmapOverlay damageHeatmapOverlay;
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
        public int MaxHP => maxHearts;
        public int MaxHearts => maxHearts;
        public int CurrentHP => playerVitalsSystem != null ? playerVitalsSystem.CurrentHp : 0;
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
        public PlayerItemInventory PlayerInventory => playerItemInventory;
        public bool HasMonsterAttackTarget => CurrentBossState.EnrageState != EnrageState.Calm;
        public Vector2Int MonsterAttackTarget => CurrentBossState.AttackTarget;

        public HazardType CurrentHazardType => currentHazardType;

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
            if (board == null || !board.TryGetPieceAt(position, out var piece) || piece == null || piece.IsPlayer)
            {
                return false;
            }

            if (IsBossLevel)
            {
                var bossState = CurrentBossState;
                if (bossState.bossAlive && position == bossState.bossPosition)
                {
                    return false;
                }
            }

            if (!monsterStates.TryGetValue(piece.GetInstanceID(), out var state))
            {
                return false;
            }

            return state.EnrageState != EnrageState.Calm;
        }

        public bool IsMonsterEnraged(int pieceId)
        {
            return monsterStates.TryGetValue(pieceId, out var state) && state.EnrageState == EnrageState.Enraged;
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

        public int GetBossPickupRadius()
        {
            var effectiveLevel = GetEffectiveLevel(CurrentLevel);

            if (effectiveLevel >= balanceConfig.BossPickupLevelThreshold)
            {
                return balanceConfig.BossPickupIncreasedRadius;
            }

            return balanceConfig.BossPickupBaseRadius;
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
        private Action pendingLootCommitAction;
        private Action pendingLootCancelAction;
        private int energy;
        private bool applyRunStartResourceAdjustments;
        private int pickupRadius;
        private bool hasBossPickupRadiusUpgrade;
        private bool hasShopPickupRadiusUpgrade;
        private bool isBubbleActive;
        private int bubbleTurnsRemaining;
        private int bubbleCooldownRemaining;
        private bool isMeditating;
        private int meditationTurnsRemaining;
        private int toxicStacks;
        private bool toxicDrainActive;
        private HazardType currentHazardType;
        private HazardType previousHazardType;
        private bool wasOnBottomRowLastTurn;
        private bool applyBottomLayerHazardOnNextTurn;
        private int hazardTurnCounter;
        private readonly List<Vector2Int> infectionQueue = new List<Vector2Int>();
        private int infectionIndex;
        private bool isSlowedByIce;
        private bool forceMoveNextTurn;
        private bool iceEnergyPenaltyActive;
        private bool pendingIceEnergyCompensation;
        private bool pendingEntangleCompensation;
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
        private readonly HashSet<int> damagedThisTurn = new HashSet<int>();
        private bool adjacencyAggroUsedThisTurn;
        private int monsterTurnCounter;
        private readonly Dictionary<int, MonsterState> monsterStates = new Dictionary<int, MonsterState>();
        private readonly Dictionary<Vector2Int, GameObject> genericTelegraphs = new Dictionary<Vector2Int, GameObject>();
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
        public bool IsShielded => isBubbleActive;
        public GameBalanceConfig BalanceConfig => balanceConfig;

        // CODEX CHEST PR2
        public int CrownsThisRun => crownsThisRun;
        public int GetGold() => crownsThisRun;

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            crownsThisRun += amount;
            SaveCrownsIfNeeded();
            UpdateUI();
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || crownsThisRun < amount)
            {
                return false;
            }

            crownsThisRun -= amount;
            SaveCrownsIfNeeded();
            UpdateUI();
            return true;
        }

        public bool HasOneUpInInventory()
        {
            return playerItemInventory != null && playerItemInventory.HasItem(PlayerItemType.SecondChance);
        }

        public bool TryGrantShopOffer(ShopOffer offer)
        {
            if (offer.IsOneUp)
            {
                return playerItemInventory != null && playerItemInventory.TryAddItem(PlayerItemType.SecondChance);
            }

            return playerItemInventory != null && playerItemInventory.TryAddItem(offer.ItemType);
        }


        private enum BossShardRewardType
        {
            MaxHp,
            MaxEnergy,
            SpellPower
        }

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

        private struct MonsterState
        {
            public bool IsIdle;
            public EnrageState EnrageState;
            public bool IsAdjacencyTriggered;
            public StatusContainer Statuses;
            public Vector2Int TargetTile;
            public int TurnsUntilAttack;
            public Vector2Int CurrentTile;
            public int CurrentHP;
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

            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
            }

            if (playerAnimationStateController == null)
            {
                playerAnimationStateController = FindObjectOfType<PlayerAnimationStateController>();
            }

            if (damageHeatmapSystem == null)
            {
                damageHeatmapSystem = FindObjectOfType<DamageHeatmapSystem>();
                if (damageHeatmapSystem == null)
                {
                    damageHeatmapSystem = gameObject.AddComponent<DamageHeatmapSystem>();
                }
            }

            if (damageHeatmapOverlay == null)
            {
                damageHeatmapOverlay = FindObjectOfType<DamageHeatmapOverlay>();
                if (damageHeatmapOverlay == null)
                {
                    damageHeatmapOverlay = gameObject.AddComponent<DamageHeatmapOverlay>();
                }
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

            EnsureMonsterAggroConfig();

            baseMaxEnergy = Mathf.Max(1, maxEnergy);
            baseMaxHearts = Mathf.Max(1, maxHearts);

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
            UpdateBubbleAnimationState();
        }

        private void OnDisable()
        {
            UnregisterBoardEvents();
        }

        private void Start()
        {
            applyRunStartResourceAdjustments = true;
            currentHazardType = balanceConfig != null ? balanceConfig.DefaultHazardType : HazardType.Poison;
            previousHazardType = currentHazardType;

            if (levelManager != null)
            {
                levelManager.LoadStartingLevel(this);
                return;
            }

            LoadLevel(startingLevelIndex);
        }

        public void ResetGame()
        {
            AttackTelegraphSystem.Instance?.ClearAllTelegraphs();

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
            wasOnBottomRowLastTurn = false;
            applyBottomLayerHazardOnNextTurn = false;
            hazardTurnCounter = 0;
            infectionIndex = 0;
            isSlowedByIce = false;
            forceMoveNextTurn = false;
            iceEnergyPenaltyActive = false;
            pendingIceEnergyCompensation = false;
            pendingEntangleCompensation = false;
            if (iceStatusIcon != null)
            {
                iceStatusIcon.SetActive(false);
            }
            isPlayerActionPhase = false;
            isResolvingMonsterAttack = false;
            InitializeSpecialPowerCooldowns();
            InitializeBossPowerCooldowns();
            EndBossPowerAccess();
            SetBubbleActive(false);
            bubbleTurnsRemaining = 0;
            bubbleCooldownRemaining = 0;
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
            damagedThisTurn.Clear();
            adjacencyAggroUsedThisTurn = false;
            monsterTurnCounter = 0;
            monsterStates.Clear();
            ClearGenericMonsterTelegraphs();
            HideComboText();
            displayedScore = Score;
            // CODEX: LEVEL_LOOP
            SetEndPanels(false, false);
            UpdateUI();
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
            RefreshDamageHeatmap();
        }

        private void RecalculateMaxResourcesFromBase()
        {
            if (IsHardcoreEnabled())
            {
                maxEnergy = Mathf.Max(1, baseMaxEnergy - HardcoreConfig.maxEnergyCapReduction);
                maxHearts = Mathf.Max(1, baseMaxHearts - HardcoreConfig.maxHpCapReduction);
            }
            else
            {
                maxEnergy = baseMaxEnergy;
                maxHearts = baseMaxHearts;
            }
        }

        public void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            ResolveHazardTypeForLevel();
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
            InitializeBossVitals(initialBossHp);
            CurrentBossState = new BossState
            {
                bossPosition = new Vector2Int(gridSize.x / 2, gridSize.y / 2),
                bossAlive = IsBossLevel,
                MaxHP = bossVitalsSystem.MaxHp,
                CurrentHP = bossVitalsSystem.CurrentHp,
                CurrentPhaseIndex = 0,
                EnrageState = EnrageState.Calm,
                Statuses = new StatusContainer(),
                AggressorPosition = default,
                AggressorPieceId = 0,
                AttackTarget = default,
                TurnsUntilAttack = 0,
                TumorShield = bossVitalsSystem.ShieldUnits
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
            damagedThisTurn.Clear();
            adjacencyAggroUsedThisTurn = false;
            monsterTurnCounter = 0;
            monsterStates.Clear();
            ClearGenericMonsterTelegraphs();
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

            GenerateInfectionQueue();
            infectionIndex = 0;

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

        public bool IsBossLevel(int levelIndex)
        {
            var levelIndexMod = Mathf.Abs(levelIndex) % 10;
            return levelIndexMod == 6;
        }

        private void ResolveHazardTypeForLevel()
        {
            int effectiveLevel = GetEffectiveLevel(CurrentLevel);

            if (effectiveLevel >= balanceConfig.IceHazardLevelThreshold)
            {
                currentHazardType = HazardType.Ice;
            }
            else if (effectiveLevel >= balanceConfig.FireHazardLevelThreshold)
            {
                currentHazardType = HazardType.Fire;
            }
            else
            {
                currentHazardType = HazardType.Poison;
            }

            if (currentHazardType != previousHazardType)
            {
                TriggerHazardTransition(currentHazardType);
                previousHazardType = currentHazardType;
                ReevaluateAllMonsterEmotionalStates();
            }

            UpdateHazardVisuals();
        }

        private void TriggerHazardTransition(HazardType type)
        {
            if (hazardTransitionCanvas == null || hazardTransitionText == null)
            {
                return;
            }

            string label = string.Empty;
            AudioClip clip = null;

            switch (type)
            {
                case HazardType.Poison:
                    label = "Toxic Floor";
                    clip = poisonTransitionClip;
                    break;
                case HazardType.Fire:
                    label = "Fire Floor";
                    clip = fireTransitionClip;
                    break;
                case HazardType.Ice:
                    label = "Frozen Floor";
                    clip = iceTransitionClip;
                    break;
            }

            hazardTransitionText.text = label;
            StartCoroutine(HazardTransitionRoutine());

            if (hazardTransitionAudio != null && clip != null)
            {
                hazardTransitionAudio.PlayOneShot(clip);
            }

            if (bottomRowHazardEffect != null)
            {
                bottomRowHazardEffect.Pulse();
            }
        }

        private IEnumerator HazardTransitionRoutine()
        {
            hazardTransitionCanvas.alpha = 0f;
            hazardTransitionCanvas.gameObject.SetActive(true);

            float duration = 0.4f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                hazardTransitionCanvas.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                hazardTransitionCanvas.alpha = Mathf.Lerp(1f, 0f, timer / duration);
                yield return null;
            }

            hazardTransitionCanvas.gameObject.SetActive(false);
        }

        private void UpdateHazardVisuals()
        {
            if (bottomRowHazardEffect == null)
            {
                return;
            }

            bottomRowHazardEffect.SetHazard(currentHazardType);
        }

        public bool LoadNextLevel()
        {
            var nextIndex = CurrentLevelIndex + 1;
            if (shopManager != null && shopManager.TryOpenBeforeBossLevel(nextIndex, ContinueLoadNextLevel))
            {
                return true;
            }

            return ContinueLoadNextLevel();
        }

        private bool ContinueLoadNextLevel()
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
            EnsureBossStatuses(ref bossState);
            var maxHp = CurrentBoss != null && CurrentBoss.maxHP > 0 ? CurrentBoss.maxHP : 100;
            InitializeBossVitals(maxHp);
            SyncBossVitalsToState(ref bossState);
            bossState.CurrentPhaseIndex = 0;
            bossState.EnrageState = EnrageState.Calm;
            bossState.Statuses.Remove(StatusType.CharmResistant);
            bossState.AggressorPosition = default;
            bossState.AggressorPieceId = 0;
            bossState.AttackTarget = default;
            bossState.TurnsUntilAttack = 0;
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

            if (CurrentHP >= maxHearts * 2)
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

            ApplyHeal(amount * 2);
            UpdateUI();
        }
        private PlayerVitalsSystem.DamageResolution ApplyDamage(int amount)
        {
            EnsurePlayerVitalsSystem();
            return playerVitalsSystem.ApplyDamage(amount);
        }

        private int ApplyHeal(int amount)
        {
            EnsurePlayerVitalsSystem();
            return playerVitalsSystem.ApplyHeal(amount);
        }

        private int ApplyShieldDamage(int amount)
        {
            EnsurePlayerVitalsSystem();
            return playerVitalsSystem.ApplyShieldDamage(amount);
        }

        private void SetPlayerCurrentHp(int amount)
        {
            EnsurePlayerVitalsSystem();
            playerVitalsSystem.SetCurrentHp(amount);
        }

        private void EnsurePlayerVitalsSystem()
        {
            if (playerVitalsSystem == null)
            {
                playerVitalsSystem = new PlayerVitalsSystem();
            }

            playerVitalsSystem.SetUnlockedHearts(maxHearts);
        }


        public bool TryActivateBubble()
        {
            if (!CanUseManualAbility())
            {
                return false;
            }

            if (bubbleCooldownRemaining > 0)
            {
                return false;
            }

            if (isBubbleActive)
            {
                return false;
            }

            const int bubbleEnergyCost = 2;
            if (!HasEnoughEnergy(bubbleEnergyCost))
            {
                return false;
            }

            if (!TrySpendEnergy(bubbleEnergyCost))
            {
                return false;
            }

            CancelMeditation();
            SetBubbleActive(true);
            bubbleTurnsRemaining = 2;
            bubbleCooldownRemaining = 0;
            ClearPlayerDebuffs();
            UpdateUI();
            return true;
        }

        public bool TryActivateShield()
        {
            return TryActivateBubble();
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

            if (!isBubbleActive)
            {
                return false;
            }

            EndBubbleAndStartCooldown();
            UpdateUI();
            return true;
        }

        private bool HasShieldVisual()
        {
            return isBubbleActive || (playerItemInventory != null && playerItemInventory.HasItem(PlayerItemType.Shield));
        }

        private void SetBubbleActive(bool active)
        {
            if (isBubbleActive == active)
            {
                return;
            }

            isBubbleActive = active;
            UpdateBubbleAnimationState();
        }

        private void UpdateBubbleAnimationState()
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

            if (forceMoveNextTurn)
            {
                forceMoveNextTurn = false;
                isSlowedByIce = false;
                if (iceStatusIcon != null)
                {
                    iceStatusIcon.SetActive(false);
                }
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
            TickHazardPressure();
            TickSpecialPowerCooldowns();
            TickBossPowerCooldowns();
            board.TickLootTurnsForTurnEnd();

            if (IsBossLevel && !CurrentBossState.bossAlive && !board.HasActivePowerShard)
            {
                TriggerInstantWin();
                TriggerWin();
                return;
            }

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.TryGetPieceAt(new Vector2Int(x, y), out var tile) && tile != null)
                    {
                        tile.TickTileDebuff();
                    }
                }
            }

            ReevaluateAllMonsterEmotionalStates();

            var hasPlayerPosition = board.TryGetPlayerPosition(out var playerPosition);
            var isOnBottomRow = hasPlayerPosition && playerPosition.y == 0;
            if (hasPlayerPosition
                && board.TryGetPieceAt(playerPosition, out var playerTile)
                && playerTile != null
                && playerTile.GetTileDebuff() == TileDebuffType.Entangled
                && energy == 0)
            {
                pendingEntangleCompensation = true;
            }
            if (!IsBugadaActive)
            {
                applyBottomLayerHazardOnNextTurn = isOnBottomRow;
            }
            if (currentHazardType == HazardType.Poison)
            {
                if (!isOnBottomRow)
                {
                    ClearToxicStacks();
                }
                else if (!IsBugadaActive)
                {
                    if (isBubbleActive)
                    {
                        toxicDrainActive = false;
                    }
                    else
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
                }
            }

            if (currentHazardType == HazardType.Fire)
            {
                if (IsBugadaActive)
                {
                    wasOnBottomRowLastTurn = isOnBottomRow;
                }
                else
                {
                    if (isOnBottomRow && !wasOnBottomRowLastTurn)
                    {
                        ApplyBurnFromHazard();
                    }

                    wasOnBottomRowLastTurn = isOnBottomRow;
                }
            }

            if (currentHazardType == HazardType.Ice)
            {
                if (IsBugadaActive)
                {
                    wasOnBottomRowLastTurn = isOnBottomRow;
                }
                else
                {
                    if (isOnBottomRow && !wasOnBottomRowLastTurn)
                    {
                        ApplyIceHazard();
                    }

                    wasOnBottomRowLastTurn = isOnBottomRow;
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
            TickBubbleStatus();
            turnsSurvivedThisLevel += 1;
            EvaluateMiniGoalsProgress();
            UpdateWorriedState();
            UpdateTiredState();
            UpdatePlayerAnimationFlags();
            UpdateMonsterEnrageVisuals();
            if (CanEnemiesReactAtTurnEnd())
            {
                adjacencyAggroUsedThisTurn = false;
                TickRageCooldown(); // CODEX RAGE SCALE FINAL
                EvaluateMonsterAggro();
                UpdateMonsterStates();
                ProcessMonsterAttacks();
                ProcessBossTumorTurn();
            }

            int previousEnergy = energy;
            int gain = CalculateTurnStartEnergyGain();
            if (gain > 0)
            {
                energy = Mathf.Min(maxEnergy, energy + gain);
            }

            hasGainedEnergy = energy > previousEnergy;
            UpdateTiredState();
            UpdateUI();

            HandleStartOfPlayerTurn();
            RefreshDamageHeatmap();
            if (!isPlayerActionPhase)
            {
                return;
            }

            isPlayerActionPhase = true;
        }

        public void HandleLootIntercept(Vector2Int targetPosition, Action commitMovement, Action cancelMovement)
        {
            pendingLootCommitAction = commitMovement;
            pendingLootCancelAction = cancelMovement;
            pendingInventoryOverflowPosition = targetPosition;
            HandleLootIntercept(targetPosition);
        }

        private void HandleLootIntercept(Vector2Int targetPosition)
        {
            if (board == null)
            {
                pendingLootCancelAction?.Invoke();
                ClearPendingLootDecisionState();
                return;
            }

            SetBoardInputLock(true);

            if (!board.TryGetPieceAt(targetPosition, out var lootPiece)
                || lootPiece == null
                || (lootPiece.SpecialType != SpecialType.Item && lootPiece.SpecialType != SpecialType.Bugada && lootPiece.SpecialType != SpecialType.PowerShard))
            {
                pendingLootCancelAction?.Invoke();
                ClearPendingLootDecisionState();
                SetBoardInputLock(false);
                return;
            }

            if (lootPiece.SpecialType == SpecialType.Bugada)
            {
                ActivateBugada();
                board.TryDestroyPieceAt(targetPosition, DestructionReason.ItemPickup);
                pendingLootCommitAction?.Invoke();
                ClearPendingLootDecisionState();
                SetBoardInputLock(false);
                return;
            }

            if (lootPiece.SpecialType == SpecialType.PowerShard)
            {
                if (TryBeginBossStatRewardChoice())
                {
                    board.TryDestroyPieceAt(targetPosition, DestructionReason.ItemPickup);
                    pendingLootCommitAction?.Invoke();
                    ClearPendingLootDecisionState();
                    return;
                }

                pendingLootCancelAction?.Invoke();
                ClearPendingLootDecisionState();
                SetBoardInputLock(false);
                return;
            }

            if (!TryRollItemTypeForPickup(out var itemType))
            {
                pendingLootCancelAction?.Invoke();
                ClearPendingLootDecisionState();
                SetBoardInputLock(false);
                return;
            }

            switch (itemType)
            {
                case PlayerItemType.BasicHeal:
                    HealPlayer(1);
                    board.TryDestroyPieceAt(targetPosition, DestructionReason.ItemPickup);
                    pendingLootCommitAction?.Invoke();
                    ClearPendingLootDecisionState();
                    SetBoardInputLock(false);
                    return;
                case PlayerItemType.EnergyPack:
                case PlayerItemType.Shield:
                case PlayerItemType.SecondChance:
                    if (TryAddInventoryItem(itemType))
                    {
                        board.TryDestroyPieceAt(targetPosition, DestructionReason.ItemPickup);
                        pendingLootCommitAction?.Invoke();
                        ClearPendingLootDecisionState();
                        SetBoardInputLock(false);
                        return;
                    }

                    BeginInventoryOverflowDecision(itemType, targetPosition);
                    return;
                default:
                    pendingLootCancelAction?.Invoke();
                    ClearPendingLootDecisionState();
                    SetBoardInputLock(false);
                    return;
            }
        }

        private void RefreshDamageHeatmap()
        {
            if (monsterAngerConfig != null && !monsterAngerConfig.enableDamageHeatmap)
            {
                return;
            }

            if (damageHeatmapSystem == null)
            {
                return;
            }

            damageHeatmapSystem.RecalculateHeatmap();

            if (CurrentLevelIndex == 0 &&
                PlayerPrefs.GetInt(HeatmapTutorialKey, 0) == 0 &&
                damageHeatmapSystem.CurrentHeatmap != null &&
                damageHeatmapSystem.CurrentHeatmap.Count > 0)
            {
                ShowFloatingText("Tiles glow when monsters will strike next turn.", Color.white);
                PlayerPrefs.SetInt(HeatmapTutorialKey, 1);
                PlayerPrefs.Save();
            }

            if (damageHeatmapOverlay != null)
            {
                var fadeHeatmap = monsterAngerConfig != null &&
                    monsterAngerConfig.showHeatmapOnlyIfEnergyAvailable &&
                    Energy <= 0;
                var pulseEnabled = monsterAngerConfig == null || monsterAngerConfig.enableHeatmapPulseAnimation;
                damageHeatmapOverlay.SetPulseAnimationEnabled(pulseEnabled);
                damageHeatmapOverlay.SetFadedMode(fadeHeatmap);
                damageHeatmapOverlay.Render(damageHeatmapSystem.CurrentHeatmap);
            }
        }

        private void HandleStartOfPlayerTurn()
        {
            if (isSlowedByIce && energy <= 0)
            {
                pendingIceEnergyCompensation = true;
                isSlowedByIce = false;
                forceMoveNextTurn = false;
                if (iceStatusIcon != null)
                {
                    iceStatusIcon.SetActive(false);
                }
                ShowFloatingText("Frozen!", Color.cyan);
                SkipPlayerTurn();
                return;
            }

            isPlayerActionPhase = true;
        }

        private int CalculateTurnStartEnergyGain()
        {
            int gain = 1;
            if (iceEnergyPenaltyActive)
            {
                iceEnergyPenaltyActive = false;
                return 0;
            }

            if (pendingIceEnergyCompensation)
            {
                pendingIceEnergyCompensation = false;
                return 2;
            }

            if (pendingEntangleCompensation)
            {
                pendingEntangleCompensation = false;
                return 2;
            }

            return gain;
        }


        private void ApplyBottomLayerHazardIfNeeded()
        {
            if (currentHazardType != HazardType.Poison)
            {
                return;
            }

            if (!applyBottomLayerHazardOnNextTurn)
            {
                return;
            }

            applyBottomLayerHazardOnNextTurn = false;
            if (hasEnded || CurrentHP <= 0 || IsBugadaActive)
            {
                return;
            }

            TakeDamage(1, DamageSource.Hazard);
            ApplyEnergyDrain(1);
        }

        private void TickHazardPressure()
        {
            hazardTurnCounter++;

            if (hazardTurnCounter <= balanceConfig.HazardGraceTurns)
            {
                return;
            }

            var spreadInterval = Mathf.Max(1, balanceConfig.HazardSpreadInterval);
            if ((hazardTurnCounter - balanceConfig.HazardGraceTurns) % spreadInterval != 0)
            {
                return;
            }

            SpreadNextTile();
        }

        private void SpreadNextTile()
        {
            if (board == null)
            {
                return;
            }

            if (infectionIndex >= infectionQueue.Count)
            {
                return;
            }

            var debuffType = GetTileDebuffForCurrentWorld();
            if (debuffType == TileDebuffType.None)
            {
                return;
            }

            var targetPos = infectionQueue[infectionIndex];

            if (board.TryGetPieceAt(targetPos, out var tile) && tile != null)
            {
                var existing = tile.GetTileDebuff();

                // Golden blocks infection but still counts as progressed
                if (existing == TileDebuffType.Golden)
                {
                    infectionIndex++;
                    return;
                }

                if (existing == TileDebuffType.None)
                {
                    tile.ApplyTileDebuff(debuffType, balanceConfig.HazardTileDuration);
                    infectionIndex++;
                    ReevaluateMonsterEmotionalState(tile);
                }
            }
        }

        private void GenerateInfectionQueue()
        {
            infectionQueue.Clear();
            if (board == null)
            {
                return;
            }

            bool leftToRight = UnityEngine.Random.value < 0.5f;

            for (int y = 0; y < board.Height; y++)
            {
                if (leftToRight)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        infectionQueue.Add(new Vector2Int(x, y));
                    }
                }
                else
                {
                    for (int x = board.Width - 1; x >= 0; x--)
                    {
                        infectionQueue.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        public bool TryApplyGoldenTile(Vector2Int position)
        {
            if (board == null || !board.TryGetPieceAt(position, out var tile) || tile == null)
            {
                return false;
            }

            if (tile.GetTileDebuff() == TileDebuffType.Golden)
            {
                TriggerConfusedPose();
                return false;
            }

            tile.ApplyTileDebuff(TileDebuffType.Golden, balanceConfig.GoldenTileDuration);
            ReevaluateMonsterEmotionalState(tile);
            return true;
        }

        private TileDebuffType GetTileDebuffForCurrentWorld()
        {
            switch (currentHazardType)
            {
                case HazardType.Poison:
                    return TileDebuffType.Entangled;
                case HazardType.Fire:
                    return TileDebuffType.None;
                case HazardType.Ice:
                    return TileDebuffType.None;
                default:
                    return TileDebuffType.Entangled;
            }
        }

        private void ApplyBurnFromHazard()
        {
            if (IsPlayerStunned)
            {
                return;
            }

            ApplyOrRefreshBurn();
        }

        private void ApplyOrRefreshBurn()
        {
            if (isBubbleActive)
            {
                return;
            }

            // Integrate with debuff system:
            // If burn exists -> refresh duration.
            // Else -> apply burn with base duration.
            const int baseBurnDurationTurns = 1;
            ApplyBurnDebuff(baseBurnDurationTurns);
        }

        private void ApplyIceHazard()
        {
            if (isBubbleActive)
            {
                return;
            }

            isSlowedByIce = true;
            forceMoveNextTurn = true;
            iceEnergyPenaltyActive = true;
            if (iceStatusIcon != null)
            {
                iceStatusIcon.SetActive(true);
            }
        }

        private void SkipPlayerTurn()
        {
            isPlayerActionPhase = false;
            StartCoroutine(SkipPlayerTurnRoutine());
        }

        private IEnumerator SkipPlayerTurnRoutine()
        {
            yield return null;
            if (hasEnded || board == null)
            {
                yield break;
            }

            HandleTurnEnded();
        }

        private void ApplyBurnDebuff(int durationTurns)
        {
            // Burn debuff integration placeholder.
            // Hook into the debuff system when burn status effects are implemented.
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
                    ApplyBossHeal(healAmount);
                }
            }
            else if (CurrentBoss.tumorBehavior == BossTumorBehavior.ShieldFromTumors)
            {
                var shieldAmount = Mathf.Max(0, CurrentBoss.shieldPerTumorTier) * totalTumorTier;
                if (shieldAmount > 0)
                {
                    EnsureBossVitalsSystem();
                    bossVitalsSystem.AddShield(shieldAmount);
                }
            }

            SyncBossVitalsToState(ref bossState);
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
                EnsureBossVitalsSystem();
                bossVitalsSystem.ApplyShieldDamage(1);
                SyncBossVitalsToState(ref bossState);
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
                        UpdateBubbleAnimationState();
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

        public bool ShouldSpawnLootForRun(int activeLootCount)
        {
            var reductionCount = Mathf.Max(0, activeLootCount - 1);
            var effectiveChance = balanceConfig.BaseLootDropChance - (reductionCount * balanceConfig.LootDropReductionPerActiveLoot);
            effectiveChance = Mathf.Clamp(effectiveChance, balanceConfig.MinimumLootDropChance, 100);
            return UnityEngine.Random.Range(0, 100) < effectiveChance;
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

            var missingHp = Mathf.Max(0, ((maxHearts * 2) - CurrentHP + 1) / 2);
            var missingEnergy = Mathf.Max(0, maxEnergy - energy);
            var shieldCount = GetShieldInventoryCount();

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

        private int GetShieldInventoryCount()
        {
            return playerItemInventory != null ? playerItemInventory.CountOf(PlayerItemType.Shield) : 0;
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

        private void TickBubbleStatus()
        {
            var updated = false;

            if (isBubbleActive)
            {
                bubbleTurnsRemaining -= 1;
                if (bubbleTurnsRemaining <= 0)
                {
                    SetBubbleActive(false);
                    bubbleTurnsRemaining = 0;
                    bubbleCooldownRemaining = 2;
                }

                updated = true;
            }

            if (bubbleCooldownRemaining > 0)
            {
                bubbleCooldownRemaining -= 1;
                bubbleCooldownRemaining = Mathf.Max(0, bubbleCooldownRemaining);
                updated = true;
            }

            if (updated)
            {
                UpdateUI();
            }
        }

        private void EndBubbleAndStartCooldown()
        {
            SetBubbleActive(false);
            bubbleTurnsRemaining = 0;
            bubbleCooldownRemaining = 2;
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
            EnsureBossStatuses(ref bossState);
            bossState.EnrageState = EnrageState.Calm;
            bossState.Statuses.Remove(StatusType.CharmResistant);
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
            RefreshDamageHeatmap();
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
            RefreshDamageHeatmap();
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
            RefreshDamageHeatmap();
        }

        // CODEX RAGE SCALE FINAL
        private void TickMonsterAttackMarker()
        {
            var bossState = CurrentBossState;
            if (bossState.EnrageState == EnrageState.Calm)
            {
                return;
            }

            if (!IsAggressorAlive(bossState))
            {
                ClearBossAttackState();
                return;
            }

            if (bossState.EnrageState != EnrageState.Calm && bossState.TurnsUntilAttack > 0)
            {
                bossState.TurnsUntilAttack--;

                if (bossState.TurnsUntilAttack == GetBossEnrageThresholdTurns() && bossState.EnrageState == EnrageState.Angry)
                {
                    bossState.EnrageState = EnrageState.Enraged;
                    TriggerMonsterAttackWindup();
                    hasTriggeredMonsterWindup = true;
                    SpawnBossTelegraph(bossState.AttackTarget);
                    UpdateMonsterEnrageVisuals();
                }

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


        private void EvaluateMonsterAggro()
        {
            TryTriggerMonsterEnrage();
            RefreshDamageHeatmap();
        }

        private void UpdateMonsterStates()
        {
            if (board == null || monsterStates.Count == 0)
            {
                CleanupOrphanedGenericTelegraphs();
                RefreshDamageHeatmap();
                return;
            }

            var updatedStates = new Dictionary<int, MonsterState>(monsterStates.Count);
            foreach (var kvp in monsterStates)
            {
                var pieceId = kvp.Key;
                var state = kvp.Value;
                EnsureMonsterStatuses(ref state);
                if (!TryFindPieceById(pieceId, out var piece))
                {
                    ClearMonsterTelegraphs(pieceId, state.CurrentTile);
                    continue;
                }

                var previousTile = state.CurrentTile;
                var currentTile = new Vector2Int(piece.X, piece.Y);
                var moved = currentTile != state.CurrentTile;
                state.CurrentTile = currentTile;

                if (moved && state.EnrageState != EnrageState.Calm)
                {
                    ClearMonsterTelegraphs(pieceId, previousTile);
                    state = CreateConfusedState(currentTile, state.CurrentHP);
                    if (debugMode)
                    {
                        Debug.Log($"GenericMonsterState: piece {pieceId} entered Confused after moving to {currentTile.x},{currentTile.y}", this);
                    }

                    updatedStates[pieceId] = state;
                    ApplyMonsterVisualState(piece, state);
                    continue;
                }

                if (state.EnrageState == EnrageState.Angry)
                {
                    state.Statuses.Add(StatusType.CharmResistant, -1);
                }

                if (state.EnrageState != EnrageState.Calm && state.TurnsUntilAttack > 0)
                {
                    state.TurnsUntilAttack--;
                }

                if (state.Statuses.Has(StatusType.Confused) || state.Statuses.Has(StatusType.Tired) || state.Statuses.Has(StatusType.Sleeping))
                {
                    ClearMonsterTelegraphs(pieceId, state.CurrentTile);
                    var wasConfused = state.Statuses.Has(StatusType.Confused);
                    var wasTired = state.Statuses.Has(StatusType.Tired);
                    var wasSleeping = state.Statuses.Has(StatusType.Sleeping);
                    state.Statuses.Tick();
                    if (wasConfused && !state.Statuses.Has(StatusType.Confused))
                    {
                        ClearMonsterTelegraphs(pieceId, state.CurrentTile);
                        EnterIdleState(ref state);
                        state.CurrentTile = currentTile;
                        state.CurrentHP = kvp.Value.CurrentHP;
                        state.TurnsUntilAttack = 0;
                        state.TargetTile = default;
                        state.IsAdjacencyTriggered = false;
                    }
                    else if (wasTired && !state.Statuses.Has(StatusType.Tired))
                    {
                        state.IsIdle = false;
                        state.Statuses.Add(StatusType.Sleeping, GetMonsterSleepDuration());
                    }
                    else if (wasSleeping && !state.Statuses.Has(StatusType.Sleeping))
                    {
                        ClearMonsterTelegraphs(pieceId, state.CurrentTile);
                        EnterIdleState(ref state);
                        state.CurrentTile = currentTile;
                        state.CurrentHP = kvp.Value.CurrentHP;
                        state.TurnsUntilAttack = 0;
                        state.TargetTile = default;
                        state.IsAdjacencyTriggered = false;
                    }
                }

                updatedStates[pieceId] = state;
                ApplyMonsterVisualState(piece, state);
            }

            monsterStates.Clear();
            foreach (var kvp in updatedStates)
            {
                monsterStates[kvp.Key] = kvp.Value;
            }

            CleanupOrphanedGenericTelegraphs();
            RefreshDamageHeatmap();
        }

        public void ProcessMonsterAttacks()
        {
            if (board == null || hasEnded || monsterStates.Count == 0)
            {
                RefreshDamageHeatmap();
                return;
            }

            var toAttack = new List<int>();
            foreach (var kvp in monsterStates)
            {
                var state = kvp.Value;
                if (state.EnrageState == EnrageState.Enraged && state.TurnsUntilAttack <= 0)
                {
                    toAttack.Add(kvp.Key);
                }
            }

            if (toAttack.Count == 0)
            {
                RefreshDamageHeatmap();
                return;
            }

            foreach (var pieceId in toAttack)
            {
                if (!monsterStates.TryGetValue(pieceId, out var state))
                {
                    continue;
                }

                var targetTile = state.TargetTile;
                if (board.IsWithinBounds(targetTile) && board.TryGetPieceAt(targetTile, out var targetPiece))
                {
                    if (targetPiece.IsPlayer)
                    {
                        ResolvePlayerHit();
                    }
                    else
                    {
                        board.TryDestroyPieceAt(targetTile, DestructionReason.MonsterAttack);
                    }
                }

                ClearMonsterTelegraphs(pieceId, state.CurrentTile);
                state.EnrageState = EnrageState.Calm;
                state.IsAdjacencyTriggered = false;
                state.Statuses.Remove(StatusType.Hurt);
                state.Statuses.Remove(StatusType.CharmResistant);
                state.Statuses.Remove(StatusType.Confused);
                state.Statuses.Remove(StatusType.Sleeping);
                state.Statuses.Remove(StatusType.Tired);
                state.TurnsUntilAttack = 0;
                state.TargetTile = default;
                EnterIdleState(ref state);
                monsterStates[pieceId] = state;
                if (TryFindPieceById(pieceId, out var attackingPiece))
                {
                    ApplyMonsterVisualState(attackingPiece, state);
                }
                if (debugMode)
                {
                    Debug.Log($"GenericMonsterState: piece {pieceId} attacked target {targetTile.x},{targetTile.y}", this);
                }
            }

            RefreshDamageHeatmap();
        }

        private void SetGenericMonsterAngry(Piece piece, Vector2Int targetTile, bool triggeredByAdjacency, bool allowTelegraph = true)
        {
            if (piece == null || piece.IsPlayer)
            {
                return;
            }

            var pieceId = piece.GetInstanceID();
            var state = GetOrCreateMonsterState(piece);
            if (state.Statuses.Has(StatusType.Hurt))
            {
                monsterStates[pieceId] = state;
                ApplyMonsterVisualState(piece, state);
                return;
            }

            var hasBlockingState = state.EnrageState == EnrageState.Enraged || state.Statuses.Has(StatusType.Confused) || state.Statuses.Has(StatusType.Tired) || state.Statuses.Has(StatusType.Sleeping);
            if (hasBlockingState)
            {
                monsterStates[pieceId] = state;
                ApplyMonsterVisualState(piece, state);
                return;
            }

            if (IsHpSurvivalCheckRequired() && !WillMonsterSurviveUntilAttack(piece))
            {
                EnterHurtState(piece);
                return;
            }

            state.TargetTile = targetTile;
            state.CurrentTile = new Vector2Int(piece.X, piece.Y);
            state.IsIdle = false;
            state.EnrageState = EnrageState.Angry;
            state.IsAdjacencyTriggered = triggeredByAdjacency;
            state.Statuses.Remove(StatusType.Hurt);
            state.Statuses.Add(StatusType.CharmResistant, -1);
            state.Statuses.Remove(StatusType.Tired);
            state.Statuses.Remove(StatusType.Sleeping);
            state.Statuses.Remove(StatusType.Confused);
            state.TurnsUntilAttack = GetMonsterTurnsBeforeAttack();
            monsterStates[pieceId] = state;

            if (allowTelegraph && !IsTelegraphOnlyOnEnrage())
            {
                AttackTelegraphSystem.Instance?.SpawnTelegraph(pieceId, state.TargetTile);
                if (triggeredByAdjacency)
                {
                    SpawnGenericMonsterTelegraph(state.CurrentTile);
                }
            }

            ApplyMonsterVisualState(piece, state);

            if (debugMode)
            {
                Debug.Log($"GenericMonsterState: piece {pieceId} entered Angry at {piece.X},{piece.Y} targeting {targetTile.x},{targetTile.y}", this);
            }
        }

        private MonsterState CreateConfusedState(Vector2Int currentTile, int currentHp)
        {
            var state = new MonsterState
            {
                IsIdle = false,
                EnrageState = EnrageState.Calm,
                Statuses = new StatusContainer(),
                TurnsUntilAttack = 0,
                TargetTile = default,
                CurrentTile = currentTile,
                CurrentHP = currentHp
            };
            state.Statuses.Add(StatusType.Confused, GetMonsterConfusedDuration());
            return state;
        }


        private static void EnsureMonsterStatuses(ref MonsterState state)
        {
            state.Statuses ??= new StatusContainer();
        }

        private static void EnsureBossStatuses(ref BossState state)
        {
            state.Statuses ??= new StatusContainer();
        }

        private int GetMonsterHitPoints(Piece piece)
        {
            if (piece == null)
            {
                return 0;
            }

            var state = GetOrCreateMonsterState(piece);
            return Mathf.Max(0, state.CurrentHP);
        }

        private int PredictGuaranteedDamageNextTick(Piece piece)
        {
            if (piece == null)
            {
                return 0;
            }

            var damage = 0;

            // A) Debuff damage already on monster.
            damage += piece.GetGuaranteedDebuffDamage();

            // B) Environmental guaranteed damage (e.g., toxic floor).
            if (board != null && board.IsHazardTile(piece.X, piece.Y))
            {
                damage += board.GetHazardDamageAt(piece.X, piece.Y);
            }

            return damage;
        }

        private int GetPendingDamageThisTurn(Piece piece)
        {
            // Damage is committed immediately in current resolution paths.
            // Keep hook for future delayed-damage systems.
            return 0;
        }

        private int GetScheduledDotDamage(Piece piece)
        {
            if (piece == null)
            {
                return 0;
            }

            return piece.GetGuaranteedDebuffDamage();
        }

        public int GetPredictedEnvironmentDamage(Piece piece)
        {
            if (piece == null || board == null)
            {
                return 0;
            }

            if (!board.IsHazardTile(piece.X, piece.Y))
            {
                return 0;
            }

            return board.GetHazardDamageAt(piece.X, piece.Y);
        }

        private bool WillMonsterSurviveUntilAttack(Piece piece)
        {
            if (piece == null || piece.IsPlayer)
            {
                return false;
            }

            if (!IsHpSurvivalCheckRequired())
            {
                return true;
            }

            var predictedHP = GetMonsterHitPoints(piece);
            var ticksToPredict = GetMonsterTurnsBeforeAttack();
            for (var tick = 0; tick < ticksToPredict; tick++)
            {
                predictedHP -= PredictGuaranteedDamageNextTick(piece);
                if (predictedHP <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void EnterHurtState(Piece piece)
        {
            if (piece == null || piece.IsPlayer)
            {
                return;
            }

            var pieceId = piece.GetInstanceID();
            var state = GetOrCreateMonsterState(piece);
            state.IsIdle = false;
            state.EnrageState = EnrageState.Calm;
            state.IsAdjacencyTriggered = false;
            state.Statuses.Add(StatusType.Hurt, -1);
            state.Statuses.Remove(StatusType.CharmResistant);
            state.Statuses.Remove(StatusType.Tired);
            state.Statuses.Remove(StatusType.Sleeping);
            state.Statuses.Remove(StatusType.Confused);
            state.TurnsUntilAttack = 0;
            monsterStates[pieceId] = state;
            ClearMonsterTelegraphs(pieceId, state.CurrentTile);
            ApplyMonsterVisualState(piece, state);
        }

        private void EnterIdleState(ref MonsterState state)
        {
            // Do not allow Idle transition if monster is in terminal Hurt state
            if (state.Statuses.Has(StatusType.Hurt))
                return;

            state.IsIdle = true;
            state.EnrageState = EnrageState.Calm;
            state.Statuses.Remove(StatusType.CharmResistant);
            state.Statuses.Remove(StatusType.Confused);
            state.Statuses.Remove(StatusType.Tired);
            state.Statuses.Remove(StatusType.Sleeping);
        }

        private MonsterState CreateDefaultMonsterState(Vector2Int currentTile)
        {
            var defaultMonsterHP = GetMonsterDefaultHitPoints();
            return new MonsterState
            {
                IsIdle = true,
                EnrageState = EnrageState.Calm,
                Statuses = new StatusContainer(),
                CurrentHP = defaultMonsterHP,
                TurnsUntilAttack = 0,
                CurrentTile = currentTile,
                TargetTile = default,
                IsAdjacencyTriggered = false
            };
        }

        private bool WillMonsterSurviveFullAttackCycle(Piece piece)
        {
            if (!IsHpSurvivalCheckRequired())
            {
                return true;
            }

            var hp = GetMonsterHitPoints(piece);
            var tickDamage = PredictGuaranteedDamageNextTick(piece);
            var totalPredicted = tickDamage * GetMonsterTurnsBeforeAttack();
            return hp - totalPredicted > 0;
        }

        private bool WillMonsterDieNextTick(Piece piece)
        {
            var hp = GetMonsterHitPoints(piece);
            var tickDamage = PredictGuaranteedDamageNextTick(piece);
            return hp - tickDamage <= 0;
        }

        private MonsterState GetOrCreateMonsterState(Piece piece)
        {
            var pieceId = piece.GetInstanceID();
            if (!monsterStates.TryGetValue(pieceId, out var state))
            {
                state = CreateDefaultMonsterState(new Vector2Int(piece.X, piece.Y));
            }
            else
            {
                EnsureMonsterStatuses(ref state);
                state.CurrentTile = new Vector2Int(piece.X, piece.Y);
                if (state.CurrentHP <= 0)
                {
                    state.CurrentHP = GetMonsterDefaultHitPoints();
                }
            }

            return state;
        }

        private void ReevaluateMonsterEmotionalState(Piece piece)
        {
            if (piece == null || piece.IsPlayer)
            {
                return;
            }

            if (!IsHpSurvivalCheckRequired())
            {
                return;
            }

            var pieceId = piece.GetInstanceID();
            if (!monsterStates.TryGetValue(pieceId, out var state))
            {
                return;
            }

            if (state.Statuses.Has(StatusType.Hurt))
            {
                ApplyMonsterVisualState(piece, state);
                monsterStates[pieceId] = state;
                return;
            }

            var hasBlockingState = state.EnrageState == EnrageState.Enraged || state.Statuses.Has(StatusType.Confused) || state.Statuses.Has(StatusType.Tired) || state.Statuses.Has(StatusType.Sleeping);
            if (hasBlockingState)
            {
                ApplyMonsterVisualState(piece, state);
                monsterStates[pieceId] = state;
                return;
            }

            var diesNext = WillMonsterDieNextTick(piece);
            var survivesFull = WillMonsterSurviveFullAttackCycle(piece);

            EnterIdleState(ref state);
            state.IsAdjacencyTriggered = false;

            if (diesNext)
            {
                state.Statuses.Add(StatusType.Hurt, -1);
                state.Statuses.Remove(StatusType.CharmResistant);
                AttackTelegraphSystem.Instance?.RemoveTelegraph(pieceId);
                RemoveGenericMonsterTelegraph(state.CurrentTile);
            }
            else if (survivesFull)
            {
                state.IsIdle = false;
                state.EnrageState = EnrageState.Angry;
                state.IsAdjacencyTriggered = false;
                state.Statuses.Add(StatusType.CharmResistant, -1);
                state.TurnsUntilAttack = GetMonsterTurnsBeforeAttack();
                if (board != null && board.TryGetPlayerPosition(out var playerPosition))
                {
                    state.TargetTile = playerPosition;
                }
            }
            else
            {
                state.Statuses.Add(StatusType.Hurt, -1);
                state.Statuses.Remove(StatusType.CharmResistant);
                AttackTelegraphSystem.Instance?.RemoveTelegraph(pieceId);
                RemoveGenericMonsterTelegraph(state.CurrentTile);
            }

            if (state.EnrageState != EnrageState.Angry && !state.Statuses.Has(StatusType.Hurt))
            {
                EnterIdleState(ref state);
                state.Statuses.Remove(StatusType.CharmResistant);
                AttackTelegraphSystem.Instance?.RemoveTelegraph(pieceId);
                RemoveGenericMonsterTelegraph(state.CurrentTile);
            }
            else if (state.EnrageState != EnrageState.Angry)
            {
                state.Statuses.Remove(StatusType.CharmResistant);
                AttackTelegraphSystem.Instance?.RemoveTelegraph(pieceId);
                RemoveGenericMonsterTelegraph(state.CurrentTile);
            }

            ApplyMonsterVisualState(piece, state);
            monsterStates[pieceId] = state;
        }


        private void EnsureMonsterAggroConfig()
        {
            if (monsterAggroConfig != null)
            {
                return;
            }

            monsterAggroConfig = new MonsterAggroConfig();
            Debug.LogWarning("MonsterAggroConfig was not assigned in inspector. Using runtime defaults.", this);
        }

        private MonsterAggroConfig GetMonsterAggroConfig()
        {
            EnsureMonsterAggroConfig();
            return monsterAggroConfig;
        }

        private int GetMonsterDefaultHitPoints()
        {
            return Mathf.Max(1, GetMonsterAggroConfig().defaultMonsterHP);
        }

        private int GetMonsterTurnsBeforeAttack()
        {
            return 2;
        }

        private int GetBossEnrageThresholdTurns()
        {
            var turnsBeforeAttack = GetMonsterTurnsBeforeAttack();
            var config = GetMonsterAggroConfig();
            var enrageDuration = Mathf.Max(1, config.enrageDuration);
            return Mathf.Max(0, turnsBeforeAttack - enrageDuration);
        }

        private int GetMonsterConfusedDuration()
        {
            var config = GetMonsterAggroConfig();
            return Mathf.Max(1, config.confusedDuration);
        }

        private int GetMonsterTiredDuration()
        {
            var config = GetMonsterAggroConfig();
            return Mathf.Max(1, config.tiredDuration);
        }

        private int GetMonsterSleepDuration()
        {
            var config = GetMonsterAggroConfig();
            return Mathf.Max(1, config.sleepDuration);
        }

        private bool IsAdjacencyMatchForecastRequired()
        {
            return GetMonsterAggroConfig().adjacencyRequiresMatchForecast;
        }

        private bool IsDamageTriggerAllowed()
        {
            return GetMonsterAggroConfig().damageTriggerAllowed;
        }

        private bool IsTelegraphOnlyOnEnrage()
        {
            return GetMonsterAggroConfig().telegraphOnlyOnEnrage;
        }

        private bool IsHpSurvivalCheckRequired()
        {
            return GetMonsterAggroConfig().requireHpSurvivalCheck;
        }

        private void ApplyMonsterVisualState(Piece piece, MonsterState state)
        {
            if (piece == null || piece.IsPlayer)
            {
                return;
            }

            piece.SetMonsterEnragedVisual(state.EnrageState != EnrageState.Calm);
            piece.SetMonsterOutStateVisual(state.Statuses.Has(StatusType.Hurt));
        }

        private void ReevaluateAllMonsterEmotionalStates()
        {
            if (board == null || monsterStates.Count == 0)
            {
                return;
            }

            var pieceIds = new List<int>(monsterStates.Keys);
            foreach (var pieceId in pieceIds)
            {
                if (TryFindPieceById(pieceId, out var piece))
                {
                    ReevaluateMonsterEmotionalState(piece);
                }
            }
        }

        private bool TryFindPieceById(int pieceId, out Piece foundPiece)
        {
            foundPiece = null;
            if (board == null || pieceId == 0)
            {
                return false;
            }

            for (var x = 0; x < board.Width; x++)
            {
                for (var y = 0; y < board.Height; y++)
                {
                    if (!board.TryGetPieceAt(new Vector2Int(x, y), out var piece) || piece == null || piece.IsPlayer)
                    {
                        continue;
                    }

                    if (piece.GetInstanceID() != pieceId)
                    {
                        continue;
                    }

                    foundPiece = piece;
                    return true;
                }
            }

            return false;
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
            if (board == null || !monstersCanAttack || isPlayerActionPhase || board.IsBusy || isResolvingMonsterAttack)
            {
                return;
            }

            if (!board.TryGetPlayerPosition(out var playerPosition))
            {
                return;
            }

            monsterTurnCounter++;

            // Always clear damage buffer so stale IDs never persist across turns.
            damagedThisTurn.Clear();
            pendingMinorDamageAggro = false;

            if (adjacencyAggroUsedThisTurn)
            {
                return;
            }

            for (var x = 0; x < board.Width; x++)
            {
                for (var y = 0; y < board.Height; y++)
                {
                    if (!board.TryGetPieceAt(new Vector2Int(x, y), out var candidate)
                        || candidate == null
                        || candidate.IsPlayer)
                    {
                        continue;
                    }

                    var candidatePosition = new Vector2Int(candidate.X, candidate.Y);
                    var manhattanDistance = Mathf.Abs(candidatePosition.x - playerPosition.x) + Mathf.Abs(candidatePosition.y - playerPosition.y);
                    if (manhattanDistance != 1)
                    {
                        continue;
                    }

                    var pieceId = candidate.GetInstanceID();
                    var state = GetOrCreateMonsterState(candidate);
                    if (state.Statuses.Has(StatusType.Hurt) || state.Statuses.Has(StatusType.Confused) || state.Statuses.Has(StatusType.Tired) || state.Statuses.Has(StatusType.Sleeping))
                    {
                        monsterStates[pieceId] = state;
                        continue;
                    }

                    if (state.EnrageState == EnrageState.Angry)
                    {
                        state.EnrageState = EnrageState.Enraged;
                        state.IsIdle = false;
                        state.IsAdjacencyTriggered = false;
                        state.Statuses.Add(StatusType.CharmResistant, -1);
                        monsterStates[pieceId] = state;

                        if (IsTelegraphOnlyOnEnrage())
                        {
                            AttackTelegraphSystem.Instance?.SpawnTelegraph(pieceId, state.TargetTile);
                        }

                        ApplyMonsterVisualState(candidate, state);
                        adjacencyAggroUsedThisTurn = true;
                        return;
                    }

                    if (state.EnrageState == EnrageState.Enraged)
                    {
                        continue;
                    }

                    if (IsAdjacencyMatchForecastRequired()
                        && !board.CanMatchPieceWithinEnergyDepth(candidatePosition, Energy))
                    {
                        continue;
                    }

                    SetGenericMonsterAngry(candidate, playerPosition, true);
                    adjacencyAggroUsedThisTurn = true;
                    return;
                }
            }
        }


        private void SpawnGenericMonsterTelegraph(Vector2Int position)
        {
            if (board == null || !board.IsWithinBounds(position))
            {
                return;
            }

            if (genericTelegraphs.ContainsKey(position))
            {
                return;
            }

            var worldPosition = board.GridToWorld(position.x, position.y);
            GameObject markerObject;
            if (genericMonsterTelegraphPrefab != null)
            {
                markerObject = Instantiate(genericMonsterTelegraphPrefab, worldPosition, Quaternion.identity);
            }
            else
            {
                markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                markerObject.name = "GenericAttackTelegraph";
                markerObject.transform.position = worldPosition;
                markerObject.transform.localScale = Vector3.one * 0.9f;
                var renderer = markerObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Unlit/Color"));
                    renderer.material.color = new Color(1f, 0.2f, 0.2f, 0.75f);
                }
            }

            if (markerObject != null)
            {
                genericTelegraphs[position] = markerObject;
            }
        }

        private void RemoveGenericMonsterTelegraph(Vector2Int position)
        {
            if (!genericTelegraphs.TryGetValue(position, out var telegraph))
            {
                return;
            }

            if (telegraph != null)
            {
                Destroy(telegraph);
            }

            genericTelegraphs.Remove(position);
        }

        private void ClearMonsterTelegraphs(int pieceId, Vector2Int telegraphTile)
        {
            AttackTelegraphSystem.Instance?.RemoveTelegraph(pieceId);
            RemoveStaleGenericTelegraphForMonster(pieceId, telegraphTile);
            RemoveGenericMonsterTelegraph(telegraphTile);
        }

        private void RemoveStaleGenericTelegraphForMonster(int pieceId, Vector2Int telegraphTile)
        {
            if (board == null || !genericTelegraphs.ContainsKey(telegraphTile))
            {
                return;
            }

            if (!board.TryGetPieceAt(telegraphTile, out var occupant)
                || occupant == null
                || occupant.IsPlayer
                || occupant.GetInstanceID() != pieceId)
            {
                RemoveGenericMonsterTelegraph(telegraphTile);
            }
        }

        private void ClearGenericMonsterTelegraphs()
        {
            foreach (var telegraph in genericTelegraphs.Values)
            {
                if (telegraph != null)
                {
                    Destroy(telegraph);
                }
            }

            genericTelegraphs.Clear();
        }

        private void CleanupOrphanedGenericTelegraphs()
        {
            if (board == null || genericTelegraphs.Count == 0)
            {
                return;
            }

            var toRemove = new List<Vector2Int>();
            foreach (var kvp in genericTelegraphs)
            {
                var position = kvp.Key;
                if (!board.IsWithinBounds(position)
                    || !board.TryGetPieceAt(position, out var occupant)
                    || occupant == null
                    || occupant.IsPlayer)
                {
                    toRemove.Add(position);
                    continue;
                }

                if (!monsterStates.TryGetValue(occupant.GetInstanceID(), out var state)
                    || state.CurrentTile != position
                    || state.EnrageState != EnrageState.Enraged)
                {
                    toRemove.Add(position);
                }
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                RemoveGenericMonsterTelegraph(toRemove[i]);
            }
        }





        private bool IsAggressorAlive(BossState bossState)
        {
            if (bossState.EnrageState == EnrageState.Enraged)
            {
                return true;
            }

            return TryResolveAggressorPiece(ref bossState, out _);
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
            EnsureBossStatuses(ref bossState);
            bossState.EnrageState = EnrageState.Angry;
            bossState.Statuses.Add(StatusType.CharmResistant, -1);
            if (!board.TryGetPieceAt(aggressorPosition, out var aggressorPiece) || aggressorPiece.IsPlayer)
            {
                return;
            }

            bossState.AggressorPosition = aggressorPosition;
            bossState.AggressorPieceId = aggressorPiece.GetInstanceID();
            bossState.AttackTarget = targetPosition;
            bossState.TurnsUntilAttack = balanceConfig.BossAttackDelayTurns;
            CurrentBossState = bossState;
            AttackTelegraphSystem.Instance?.SpawnTelegraph(bossState.AggressorPieceId, targetPosition);

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

            AttackTelegraphSystem.Instance?.RemoveTelegraph(CurrentBossState.AggressorPieceId);

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
                    ResolvePlayerHit();
                    return;
                }

                board.TryDestroyPieceAt(targetPosition, DestructionReason.MonsterAttack);
            }
            finally
            {
                isResolvingMonsterAttack = false;
            }
        }

        private void ResolvePlayerHit()
        {
            TriggerStunnedAnimation();
            TakeDamage(1, DamageSource.Monster);
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

            if (forceMoveNextTurn)
            {
                var blocked = SpecialPowerActivationResult.Failed(
                    SpecialPowerActivationFailureReason.ActionBlocked,
                    "Ice hazard requires movement this turn.");
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

            if (forceMoveNextTurn)
            {
                reason = "Ice hazard requires movement this turn.";
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

        private void ClearPlayerDebuffs()
        {
            ClearToxicStacks();
            wasOnBottomRowLastTurn = false;
            applyBottomLayerHazardOnNextTurn = false;
            isSlowedByIce = false;
            forceMoveNextTurn = false;
            iceEnergyPenaltyActive = false;
            pendingIceEnergyCompensation = false;
            pendingEntangleCompensation = false;
            if (iceStatusIcon != null)
            {
                iceStatusIcon.SetActive(false);
            }

            isStunned = false;
            stunnedTurnsRemaining = 0;
            UpdatePlayerAnimationFlags();
        }

        private void ClearPlayerDebuffsForBugada()
        {
            ClearPlayerDebuffs();
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
            else
            {
                AttackTelegraphSystem.Instance?.RemoveTelegraph(piece.GetInstanceID());
                if (monsterStates.TryGetValue(piece.GetInstanceID(), out var state))
                {
                    RemoveGenericMonsterTelegraph(state.CurrentTile);
                }
            }

            TryDefeatBossFromDestruction(piece, reason);

            if (CurrentBossState.EnrageState != EnrageState.Calm)
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

            var phaseAdvanced = ApplyBossDamage(1, source);
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

            var spawnedShard = false;
            if (board != null)
            {
                spawnedShard = board.TrySpawnPowerShardAt(bossState.bossPosition);
            }

            if (!spawnedShard)
            {
                TriggerInstantWin();
                TriggerWin();
            }
        }


        // CODEX BOSS PHASE PR1
        private bool ApplyBossDamage(int damage, string source)
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

            EnsureBossVitalsSystem();
            var previousPhase = bossState.CurrentPhaseIndex;
            var damageResolution = bossVitalsSystem.ApplyDamage(damage);
            var hpDamage = damageResolution.HpLost;
            SyncBossVitalsToState(ref bossState);

            if (hpDamage <= 0)
            {
                CurrentBossState = bossState;
                UpdateUI();
                return false;
            }

            var minorDamageThreshold = monsterAngerConfig != null ? Mathf.Max(1, monsterAngerConfig.minorDamageThreshold) : 1;
            if (hpDamage <= minorDamageThreshold)
            {
                pendingMinorDamageAggro = true;
            }

            if (board != null && board.TryGetPieceAt(bossState.bossPosition, out var piece) && piece != null && !piece.IsPlayer)
            {
                var pieceId = piece.GetInstanceID();
                damagedThisTurn.Add(pieceId);
            }

            var phaseAdvanced = EvaluateBossPhaseTransitions();
            bossState = CurrentBossState;
            bossState.bossAlive = bossVitalsSystem.IsAlive;

            if (!bossVitalsSystem.IsAlive)
            {
                CurrentBossState = bossState;
                ClearBossAttackState();
                UpdateUI();
                return false;
            }

            CurrentBossState = bossState;
            UpdateUI();
            if (debugMode)
            {
                Debug.Log($"BossPhaseCheck source={source} hp={CurrentBossState.CurrentHP}/{CurrentBossState.MaxHP} phase={previousPhase}->{CurrentBossState.CurrentPhaseIndex}", this);
            }

            return phaseAdvanced;
        }

        private void EnsureBossVitalsSystem()
        {
            bossVitalsSystem ??= new BossVitalsSystem();
        }

        private void InitializeBossVitals(int maxHp)
        {
            EnsureBossVitalsSystem();
            bossVitalsSystem.Initialize(maxHp);
        }

        private void SyncBossVitalsToState(ref BossState bossState)
        {
            EnsureBossVitalsSystem();
            bossState.MaxHP = bossVitalsSystem.MaxHp;
            bossState.CurrentHP = bossVitalsSystem.CurrentHp;
            bossState.TumorShield = bossVitalsSystem.ShieldUnits;
        }

        private int ApplyBossHeal(int amount)
        {
            EnsureBossVitalsSystem();
            return bossVitalsSystem.ApplyHeal(amount);
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
            EnsureBossStatuses(ref bossState);
            EnsureBossVitalsSystem();
            var hpPercent = bossVitalsSystem.MaxHp > 0
                ? (bossVitalsSystem.CurrentHp * 100f) / bossVitalsSystem.MaxHp
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
            if (shouldEnrage && bossState.EnrageState != EnrageState.Enraged)
            {
                bossState.EnrageState = EnrageState.Enraged;
                bossState.Statuses.Add(StatusType.CharmResistant, -1);
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
            AttackTelegraphSystem.Instance?.ClearAllTelegraphs();

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

            AttackTelegraphSystem.Instance?.ClearAllTelegraphs();

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
            pickupRadius = GetBossPickupRadius();
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
            if (maxHearts < 1)
            {
                maxHearts = 1;
            }

            if (playerVitalsSystem == null)
            {
                playerVitalsSystem = new PlayerVitalsSystem();
            }

            playerVitalsSystem.SetUnlockedHearts(maxHearts);
            playerVitalsSystem.RefillHpToBaseMaximum();
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

            if (CurrentBossState.EnrageState != EnrageState.Enraged)
            {
                return baseDamage;
            }

            var multiplier = Mathf.Max(1f, CurrentBoss.enrageDamageMultiplier);
            var bonus = Mathf.Max(0, CurrentBoss.enrageExtraDamage);
            return Mathf.Max(baseDamage + 1, Mathf.CeilToInt(baseDamage * multiplier) + bonus);
        }

        public void PopulateIncomingDamageHeatmap(DamageHeatmapSystem heatmapSystem)
        {
            if (heatmapSystem == null || board == null)
            {
                return;
            }

            foreach (var kvp in monsterStates)
            {
                var state = kvp.Value;
                if (state.EnrageState != EnrageState.Enraged || state.TurnsUntilAttack > 0)
                {
                    continue;
                }

                if (!board.IsWithinBounds(state.CurrentTile) || !board.IsWithinBounds(state.TargetTile))
                {
                    continue;
                }

                if (!TryFindPieceById(kvp.Key, out var piece) || piece == null || piece.IsPlayer)
                {
                    continue;
                }

                heatmapSystem.AddPredictedDirectDamage(state.TargetTile, GetMonsterAttackDamage(piece));
            }

            if (CurrentBossState.EnrageState == EnrageState.Enraged && CurrentBossState.TurnsUntilAttack <= 0 && board.IsWithinBounds(CurrentBossState.AttackTarget))
            {
                heatmapSystem.AddPredictedDirectDamage(CurrentBossState.AttackTarget, GetCurrentMonsterDamage());
            }

            if (currentHazardType == HazardType.Poison
                && applyBottomLayerHazardOnNextTurn
                && !hasEnded
                && CurrentHP > 0
                && !IsBugadaActive
                && board.TryGetPlayerPosition(out var playerPosition)
                && playerPosition.y == 0
                && board.IsWithinBounds(playerPosition))
            {
                heatmapSystem.AddPredictedDirectDamage(playerPosition, 1f);
            }
        }

        private float GetMonsterAttackDamage(Piece piece)
        {
            if (piece == null || piece.IsPlayer)
            {
                return 0f;
            }

            return 1f;
        }

        public void TakeDamage(int halfUnits)
        {
            TakeDamage(halfUnits, DamageSource.Hazard);
        }

        public void TakeDamage(int halfUnits, DamageSource source)
        {
            if (halfUnits <= 0 || hasEnded || IsBugadaActive)
            {
                return;
            }

            if (isActivatingSpecialPower && !activeSpecialPowerAllowsHpModification)
            {
                Debug.LogWarning("Special powers cannot directly modify player HP.", this);
                return;
            }

            if (isBubbleActive)
            {
                EndBubbleAndStartCooldown();
                UpdateUI();
                return;
            }

            var damageResult = ApplyDamage(halfUnits);
            if (damageResult.IsFatal && playerItemInventory != null && playerItemInventory.TryConsumeItem(PlayerItemType.SecondChance))
            {
                SetPlayerCurrentHp(Mathf.Min(maxHearts * 2, 2));
                if (IsBossLevel && shopManager != null && shopManager.TryReviveToShopFromBossDeath())
                {
                    UpdateUI();
                    return;
                }

                UpdateUI();
                return;
            }

            if (CurrentHP <= 0)
            {
                GameOverFlow();
            }

            UpdateUI();
        }

        private void GameOverFlow()
        {
            TriggerLose();
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

        private void TriggerConfusedPose()
        {
            TriggerStunnedAnimation();
        }

        private void TriggerStunnedAnimation()
        {
            if (IsBugadaActive || isBubbleActive)
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
            if (board != null && CurrentBossState.EnrageState != EnrageState.Calm)
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
            if (bossState.EnrageState == EnrageState.Calm || board == null)
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
                bossState.EnrageState != EnrageState.Calm)
            {
                SpawnBossTelegraph(bossState.AttackTarget);
            }

#if UNITY_EDITOR
            if (bossState.EnrageState == EnrageState.Enraged && monsterAttackMarkerInstance == null)
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

            RefreshDamageHeatmap();
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
            if (bossState.EnrageState == EnrageState.Calm)
            {
                ClearMonsterEnrageIndicator();
                return;
            }

            Piece enragedPiece;
            if (bossState.EnrageState == EnrageState.Enraged)
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
                var enrageLabel = bossState.EnrageState == EnrageState.Enraged ? " ENRAGED" : string.Empty;
                bossLabelText.text = IsBossLevel ? $"BOSS{phaseLabel}{enrageLabel}" : "BOSS";
                bossLabelText.enabled = IsBossLevel;
                bossLabelText.color = bossState.EnrageState == EnrageState.Enraged ? new Color(1f, 0.2f, 0.2f, 1f) : Color.white;
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
                    bossStateText.text = $"HP {bossState.CurrentHP}/{bossState.MaxHP}{shieldLabel}  |  Phase {bossState.CurrentPhaseIndex + 1}{(bossState.EnrageState == EnrageState.Enraged ? "  |  ENRAGE" : string.Empty)}";
                    bossStateText.color = bossState.EnrageState == EnrageState.Enraged ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white;
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

            if (inventoryOverflowCancelButton != null)
            {
                inventoryOverflowCancelButton.onClick.RemoveAllListeners();
                inventoryOverflowCancelButton.onClick.AddListener(CancelInventoryOverflowDecision);
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

            CompleteInventoryOverflowDecision(commitMovement: true, destroyLoot: true);
        }

        private void ResolveInventoryOverflowByDestroyingLoot()
        {
            if (!awaitingInventoryOverflowDecision)
            {
                return;
            }

            CompleteInventoryOverflowDecision(commitMovement: true, destroyLoot: true);
        }

        private void CancelInventoryOverflowDecision()
        {
            if (!awaitingInventoryOverflowDecision)
            {
                return;
            }

            CompleteInventoryOverflowDecision(commitMovement: false, destroyLoot: false);
        }

        private void CompleteInventoryOverflowDecision(bool commitMovement, bool destroyLoot)
        {
            if (board != null && destroyLoot)
            {
                board.TryDestroyPieceAt(pendingInventoryOverflowPosition, DestructionReason.ItemPickup);
            }

            if (commitMovement)
            {
                pendingLootCommitAction?.Invoke();
            }
            else
            {
                pendingLootCancelAction?.Invoke();
            }

            awaitingInventoryOverflowDecision = false;
            pendingInventoryOverflowItem = default;
            pendingInventoryOverflowPosition = default;
            HideInventoryOverflowPanel();
            SetBoardInputLock(false);
            ClearPendingLootDecisionState();
            UpdateBubbleAnimationState();
            UpdateUI();
        }

        private void ClearPendingLootDecisionState()
        {
            pendingLootCommitAction = null;
            pendingLootCancelAction = null;
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

        public void SetShopActive(bool active)
        {
            SetBoardInputLock(active);
            if (board != null)
            {
                board.gameObject.SetActive(!active);
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

            var orderedRewards = BuildBossShardRewardOptionOrder();
            for (var i = 0; i < bossPowerRewardButtons.Length; i++)
            {
                var button = bossPowerRewardButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < orderedRewards.Count)
                {
                    ConfigureBossStatRewardButton(button, orderedRewards[i]);
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

        private List<BossShardRewardType> BuildBossShardRewardOptionOrder()
        {
            var orderedRewards = new List<BossShardRewardType>
            {
                BossShardRewardType.MaxHp,
                BossShardRewardType.MaxEnergy,
                BossShardRewardType.SpellPower
            };

            var priority = SelectBossShardRewardPriority();
            orderedRewards.Remove(priority);
            orderedRewards.Insert(0, priority);
            return orderedRewards;
        }

        private BossShardRewardType SelectBossShardRewardPriority()
        {
            if (CurrentBoss != null
                && CurrentBoss.associatedPower != BossPower.None
                && !HasBossPower(CurrentBoss.associatedPower))
            {
                return BossShardRewardType.SpellPower;
            }

            if (maxHearts == maxEnergy)
            {
                return UnityEngine.Random.Range(0, 2) == 0
                    ? BossShardRewardType.MaxHp
                    : BossShardRewardType.MaxEnergy;
            }

            return maxHearts < maxEnergy ? BossShardRewardType.MaxHp : BossShardRewardType.MaxEnergy;
        }

        private void ConfigureBossStatRewardButton(Button button, BossShardRewardType rewardType)
        {
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();

            var textLabel = button.GetComponentInChildren<Text>();
            if (textLabel != null)
            {
                textLabel.text = GetBossShardRewardLabel(rewardType);
            }

            button.onClick.AddListener(() => ResolveBossStatRewardChoice(rewardType));
        }

        private static string GetBossShardRewardLabel(BossShardRewardType rewardType)
        {
            switch (rewardType)
            {
                case BossShardRewardType.MaxHp:
                    return "+1 Max HP";
                case BossShardRewardType.MaxEnergy:
                    return "+1 Max Energy";
                case BossShardRewardType.SpellPower:
                    return "+1 Spell Power";
                default:
                    return "+1 Reward";
            }
        }

        private void ResolveBossStatRewardChoice(BossShardRewardType rewardType)
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

            ApplyBossShardReward(rewardType);
            UpdateUI();

            var bossPowerRewardStarted = GrantBossPowerIfEligible();
            if (!bossPowerRewardStarted)
            {
                SetBoardInputLock(false);
                if (IsBossLevel && !CurrentBossState.bossAlive)
                {
                    TriggerInstantWin();
                    TriggerWin();
                }
            }
        }

        private void ApplyBossShardReward(BossShardRewardType rewardType)
        {
            switch (rewardType)
            {
                case BossShardRewardType.MaxHp:
                {
                    var clampedHearts = Mathf.Min(Mathf.Max(1, maxHeartsCap), maxHearts + 1);
                    if (clampedHearts > maxHearts)
                    {
                        maxHearts = clampedHearts;
                        ApplyHeal(2);
                    }
                    break;
                }
                case BossShardRewardType.MaxEnergy:
                {
                    var clampedEnergy = Mathf.Min(Mathf.Max(1, maxEnergyCap), maxEnergy + 1);
                    if (clampedEnergy > maxEnergy)
                    {
                        maxEnergy = clampedEnergy;
                    }

                    energy = Mathf.Min(maxEnergy, energy);
                    break;
                }
                case BossShardRewardType.SpellPower:
                {
                    if (CurrentBoss != null
                        && CurrentBoss.associatedPower != BossPower.None
                        && !HasBossPower(CurrentBoss.associatedPower))
                    {
                        TryAddBossPower(CurrentBoss.associatedPower);
                    }
                    break;
                }
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
