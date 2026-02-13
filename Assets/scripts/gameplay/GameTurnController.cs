using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Normal,
    Tumor,
    Bomb,
    Special
}

public enum BombState
{
    Idle,
    Angry,
    Enrage,
    Explode
}

public enum BossState
{
    Sleep,
    Calm,
    Angry,
    Enrage,
    Attack,
    Meditate,
    Stunned,
    Tired
}

public enum TurnPhase
{
    BossTurnStart,
    BossAction,
    BossResolve,
    PlayerTurnStart,
    PlayerAction,
    MatchEvaluation,
    CascadeResolution,
    BombResolution,
    TumorResolution,
    StateUpdate,
    EnergyUpdate,
    TurnCleanup
}

[Serializable]
public class Tile
{
    public int row;
    public int col;
    public TileType type;
    public object tileData;
}

[Serializable]
public class Board
{
    public Tile[,] grid;
    public bool IsBoardLocked;

    public Tile GetTile(int row, int col)
    {
        return IsInsideBounds(row, col) ? grid[row, col] : null;
    }

    public void SetTile(int row, int col, Tile tile)
    {
        if (!IsInsideBounds(row, col))
            return;

        grid[row, col] = tile;

        if (tile != null)
        {
            tile.row = row;
            tile.col = col;
        }
    }

    public bool IsInsideBounds(int row, int col)
    {
        return grid != null
               && row >= 0
               && col >= 0
               && row < grid.GetLength(0)
               && col < grid.GetLength(1);
    }
}

[Serializable]
public class Bomb
{
    public BombState state;
    public int tier;
    public int turnsUntilExplosion;
    public bool wasAdvancedThisTurn;
}

[Serializable]
public class Tumor
{
    public int maxHP;
    public int currentHP;
}

[Serializable]
public class Boss
{
    public int HP;
    public int maxHP;
    public int energy;
    public int maxEnergy;
    public BossState state;
    public int specialCooldown;
    public BossPower specialPower;
}

[Serializable]
public class Player
{
    public int HP;
    public int maxHP;
    public int energy;
    public Vector2Int position;
    public List<Bomb> inventory = new List<Bomb>();
}

public abstract class BossPower
{
    public abstract void Execute(Boss boss, Board board);
}

public class GameTurnController : MonoBehaviour
{
    [Header("Core Combat Models")]
    public Board board;
    public Boss boss;
    public Player player;
    public TurnPhase currentPhase;

    public event Action OnBossTurnStarted;
    public event Action<Vector2Int> OnBossMoved;
    public event Action<BossState> OnBossStateChanged;
    public event Action<int> OnBossDamaged;
    public event Action OnPlayerTurnStarted;
    public event Action OnMatchResolved;
    public event Action OnCascadeTriggered;
    public event Action<Bomb> OnBombStateChanged;
    public event Action<Bomb> OnBombExploded;
    public event Action<Tumor, int> OnTumorDamaged;
    public event Action<int, int> OnEnergyChanged;

    private bool isLevelActive = true;
    private bool playerActionConfirmed;

    private void Start()
    {
        StartCoroutine(GameLoop());
    }

    public IEnumerator GameLoop()
    {
        while (isLevelActive)
        {
            yield return StartCoroutine(HandleBossTurn());
            yield return StartCoroutine(HandlePlayerTurn());
            yield return StartCoroutine(ResolveBoard());
            yield return StartCoroutine(UpdateStates());
            yield return StartCoroutine(UpdateEnergy());
            CleanupTurn();
        }
    }

    private IEnumerator HandleBossTurn()
    {
        currentPhase = TurnPhase.BossTurnStart;

        if (boss == null)
            yield break;

        if (boss.state == BossState.Sleep)
            yield break;

        board.IsBoardLocked = true;
        OnBossTurnStarted?.Invoke();

        currentPhase = TurnPhase.BossAction;
        ResolveBossActionSkeleton();

        currentPhase = TurnPhase.BossResolve;
        ResolveBossActionResultSkeleton();

        board.IsBoardLocked = false;
        yield break;
    }

    private void ResolveBossActionSkeleton()
    {
        // AI decision placeholder only (move / attack / throw bomb / special).
        // This method should only choose and execute gameplay logic, then raise events.
    }

    private void ResolveBossActionResultSkeleton()
    {
        // Result placeholder for boss action.
        // Emit event hooks for external systems that will animate/present outcomes.
        // Examples for future integration:
        // OnBossMoved?.Invoke(new Vector2Int(targetCol, targetRow));
        // OnBossStateChanged?.Invoke(boss.state);
        // OnBossDamaged?.Invoke(damageTaken);
    }

    private IEnumerator HandlePlayerTurn()
    {
        currentPhase = TurnPhase.PlayerTurnStart;

        if (board.IsBoardLocked)
            yield break;

        playerActionConfirmed = false;
        OnPlayerTurnStarted?.Invoke();

        currentPhase = TurnPhase.PlayerAction;

        // Wait until input layer submits a valid action.
        // Input system must block interaction while board is locked.
        yield return new WaitUntil(() => playerActionConfirmed);

        board.IsBoardLocked = true;
    }

    public void ConfirmPlayerAction()
    {
        if (currentPhase != TurnPhase.PlayerAction)
            return;

        if (board.IsBoardLocked)
            return;

        playerActionConfirmed = true;
    }

    private IEnumerator ResolveBoard()
    {
        board.IsBoardLocked = true;

        // Resolution order is strict and fully turn-based:
        // 1) MatchEvaluation -> 2) CascadeResolution -> 3) BombResolution -> 4) TumorResolution
        currentPhase = TurnPhase.MatchEvaluation;
        EvaluateMatches();
        OnMatchResolved?.Invoke();

        currentPhase = TurnPhase.CascadeResolution;
        ResolveCascades();
        OnCascadeTriggered?.Invoke();

        currentPhase = TurnPhase.BombResolution;
        ResolveBombs();

        currentPhase = TurnPhase.TumorResolution;
        ResolveTumors();

        board.IsBoardLocked = false;
        yield break;
    }

    private void EvaluateMatches()
    {
        // Match scan and mark logic placeholder.
    }

    private void ResolveCascades()
    {
        // Gravity/refill cascade placeholder.
    }

    private void ResolveBombs()
    {
        // Bomb resolve placeholder.
        // Actual state advancement must happen only in UpdateStates.
    }

    private void ResolveTumors()
    {
        // Tumor resolve placeholder.
        // Example event usage:
        // OnTumorDamaged?.Invoke(tumor, damageAmount);
    }

    private IEnumerator UpdateStates()
    {
        currentPhase = TurnPhase.StateUpdate;

        ProgressBombStates();
        ProgressBossState();
        DecrementBossSpecialCooldown();
        ResetBombAdvanceFlags();

        yield break;
    }

    private void ProgressBombStates()
    {
        foreach (Bomb bomb in EnumerateAllBombs())
        {
            if (bomb == null)
                continue;

            if (bomb.turnsUntilExplosion > 0)
                bomb.turnsUntilExplosion--;

            if (bomb.turnsUntilExplosion <= 0)
            {
                bomb.state = BombState.Explode;
                bomb.wasAdvancedThisTurn = true;
                OnBombStateChanged?.Invoke(bomb);
                OnBombExploded?.Invoke(bomb);
                continue;
            }

            BombState previousState = bomb.state;
            bomb.state = GetNextBombState(bomb.state);
            bomb.wasAdvancedThisTurn = true;

            if (bomb.state != previousState)
                OnBombStateChanged?.Invoke(bomb);
        }
    }

    private BombState GetNextBombState(BombState currentState)
    {
        switch (currentState)
        {
            case BombState.Idle:
                return BombState.Angry;
            case BombState.Angry:
                return BombState.Enrage;
            case BombState.Enrage:
                return BombState.Explode;
            default:
                return BombState.Explode;
        }
    }

    private IEnumerable<Bomb> EnumerateAllBombs()
    {
        HashSet<Bomb> yielded = new HashSet<Bomb>();

        if (player != null && player.inventory != null)
        {
            foreach (Bomb bomb in player.inventory)
            {
                if (bomb != null && yielded.Add(bomb))
                    yield return bomb;
            }
        }

        if (board?.grid == null)
            yield break;

        int rows = board.grid.GetLength(0);
        int cols = board.grid.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Tile tile = board.grid[row, col];

                if (tile == null || tile.type != TileType.Bomb)
                    continue;

                Bomb bomb = tile.tileData as Bomb;
                if (bomb != null && yielded.Add(bomb))
                    yield return bomb;
            }
        }
    }

    private void ProgressBossState()
    {
        // Boss state machine progression placeholder.
        // Future implementation can set boss.state and raise OnBossStateChanged as needed.
    }

    private void DecrementBossSpecialCooldown()
    {
        if (boss == null)
            return;

        if (boss.specialCooldown > 0)
            boss.specialCooldown--;
    }

    private void ResetBombAdvanceFlags()
    {
        foreach (Bomb bomb in EnumerateAllBombs())
        {
            if (bomb != null)
                bomb.wasAdvancedThisTurn = false;
        }
    }

    private IEnumerator UpdateEnergy()
    {
        currentPhase = TurnPhase.EnergyUpdate;

        if (boss == null)
            yield break;

        boss.energy = Mathf.Min(boss.maxEnergy, boss.energy + 1);

        if (boss.state == BossState.Meditate)
        {
            // Meditate hook placeholder for future rules.
        }

        OnEnergyChanged?.Invoke(boss.energy, boss.maxEnergy);
        yield break;
    }

    private void CleanupTurn()
    {
        currentPhase = TurnPhase.TurnCleanup;

        playerActionConfirmed = false;

        if (board != null)
            board.IsBoardLocked = false;
    }
}
