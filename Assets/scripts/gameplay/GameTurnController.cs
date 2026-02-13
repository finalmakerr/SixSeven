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
    public Vector2Int position;
    public List<Bomb> inventory = new List<Bomb>();
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
    public event Action<int> OnPlayerDamaged;
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

        yield break;
    }

    private void ResolveBossActionSkeleton()
    {
        if (boss == null || board == null || player == null)
            return;
    
        if (boss.state == BossState.Sleep)
            return;
    
        // Priority 1: pick up adjacent bomb
        if (TryPickUpBomb())
            return;
    
        // Priority 2: special power (cost 3 energy, 7 turn cooldown)
        if (boss.energy >= 3 && boss.specialPower != null && boss.specialCooldown == 0)
        {
            SetBossState(BossState.Enrage);
    
            boss.specialPower.Execute(boss, board);
    
            boss.specialCooldown = 7;
            boss.energy -= 3;
    
            SetBossState(BossState.Tired);
            return;
        }
    
        // Priority 3: attack if adjacent (aggressive behavior)
        if (GetManhattanDistance(boss.position, player.position) <= 1)
        {
            int damage = 1;
    
            player.HP = Mathf.Max(0, player.HP - damage);
    
            boss.energy = Mathf.Max(0, boss.energy - 1);
    
            SetBossState(BossState.Attack);
            OnPlayerDamaged?.Invoke(damage);
    
            return;
        }
    
        // Priority 4: throw bomb (0 energy cost but forces Tired)
        if (TryThrowBomb())
            return;
    
        // Priority 5: move toward player
        if (TryMoveToward(player.position))
            return;
    
        // Priority 6: move toward nearest loot
        if (TryMoveTowardNearestLoot())
            return;
    
        // Priority 7: idle
    }


    private bool CanThrowBomb(Bomb bomb, out Vector2Int placement, out Vector2Int stepBack)
    {
        placement = boss.position;
        stepBack = boss.position;

        if (bomb == null || board == null)
            return false;

        Vector2Int[] cardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (Vector2Int dir in cardinalDirections)
        {
            Vector2Int candidatePlacement = boss.position + (dir * 2);
            Vector2Int candidateStepBack = boss.position - dir;

            if (!board.IsInsideBounds(candidatePlacement.y, candidatePlacement.x))
                continue;

            if (!board.IsInsideBounds(candidateStepBack.y, candidateStepBack.x))
                continue;

            if (candidatePlacement == player.position || candidatePlacement == boss.position)
                continue;

            if (candidateStepBack == player.position)
                continue;

            Tile placementTile = board.GetTile(candidatePlacement.y, candidatePlacement.x);
            if (placementTile != null && placementTile.type == TileType.Bomb)
                continue;

            placement = candidatePlacement;
            stepBack = candidateStepBack;
            return true;
        }

        return false;
    }

    private bool TryThrowBomb()
    {
        if (boss?.inventory == null || boss.inventory.Count == 0)
            return false;

        Bomb bombToThrow = boss.inventory[0];
        if (!CanThrowBomb(bombToThrow, out Vector2Int placement, out Vector2Int stepBack))
            return false;

        Tile placedBombTile = new Tile
        {
            row = placement.y,
            col = placement.x,
            type = TileType.Bomb,
            tileData = bombToThrow
        };
        board.SetTile(placement.y, placement.x, placedBombTile);

        boss.inventory.RemoveAt(0);
        OnBombStateChanged?.Invoke(bombToThrow);

        boss.position = stepBack;
        OnBossMoved?.Invoke(boss.position);

        SetBossState(BossState.Tired);
        return true;
    }

    private bool TryPickUpBomb()
    {
        if (board?.grid == null || boss == null)
            return false;

        Vector2Int[] cardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (Vector2Int dir in cardinalDirections)
        {
            Vector2Int target = boss.position + dir;
            if (!board.IsInsideBounds(target.y, target.x))
                continue;

            if (target == player.position)
                continue;

            Tile tile = board.GetTile(target.y, target.x);
            if (tile == null || tile.type != TileType.Bomb)
                continue;

            Bomb bomb = tile.tileData as Bomb;
            if (bomb == null)
                continue;

            board.SetTile(target.y, target.x, null);
            boss.inventory.Add(bomb);
            OnBombStateChanged?.Invoke(bomb);

            boss.position = target;
            OnBossMoved?.Invoke(boss.position);

            boss.energy = Mathf.Max(0, boss.energy - 1);
            return true;
        }

        return false;
    }

    private bool TryMoveToward(Vector2Int target)
    {
        if (boss == null || board == null)
            return false;

        Vector2Int delta = target - boss.position;
        Vector2Int horizontalStep = Vector2Int.zero;
        Vector2Int verticalStep = Vector2Int.zero;

        if (delta.x != 0)
            horizontalStep = new Vector2Int(Math.Sign(delta.x), 0);

        if (delta.y != 0)
            verticalStep = new Vector2Int(0, Math.Sign(delta.y));

        if (horizontalStep != Vector2Int.zero && TryMoveByStep(horizontalStep))
            return true;

        if (verticalStep != Vector2Int.zero && TryMoveByStep(verticalStep))
            return true;

        return false;
    }

    private bool TryMoveByStep(Vector2Int step)
    {
        if (boss == null || board == null || player == null)
            return false;

        Vector2Int target = boss.position + step;
        if (!board.IsInsideBounds(target.y, target.x))
            return false;

        if (target == player.position || target == boss.position)
            return false;

        boss.position = target;
        boss.energy = Mathf.Max(0, boss.energy - 1);
        OnBossMoved?.Invoke(boss.position);
        return true;
    }

    private bool TryMoveTowardNearestLoot()
    {
        if (boss == null || player == null)
            return false;

        Vector2Int? nearestLoot = FindNearestLootPosition();
        if (!nearestLoot.HasValue)
            return false;

        int lootDistance = GetManhattanDistance(boss.position, nearestLoot.Value);
        int playerDistance = GetManhattanDistance(boss.position, player.position);

        if (lootDistance >= playerDistance)
            return false;

        return TryMoveToward(nearestLoot.Value);
    }

    private Vector2Int? FindNearestLootPosition()
    {
        if (board?.grid == null)
            return null;

        int rows = board.grid.GetLength(0);
        int cols = board.grid.GetLength(1);

        int bestDistance = int.MaxValue;
        Vector2Int best = default;
        bool found = false;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Tile tile = board.grid[row, col];
                if (tile == null || tile.type != TileType.Bomb)
                    continue;

                Vector2Int bombPos = new Vector2Int(col, row);
                int distance = GetManhattanDistance(boss.position, bombPos);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = bombPos;
                found = true;
            }
        }

        return found ? best : (Vector2Int?)null;
    }

    private static int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void SetBossState(BossState nextState)
    {
        if (boss == null || boss.state == nextState)
            return;

        boss.state = nextState;
        OnBossStateChanged?.Invoke(boss.state);
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
    
            if (bomb.wasAdvancedThisTurn)
                continue;
    
            BombState previousState = bomb.state;
    
            bomb.state = GetNextBombState(bomb.state);
            bomb.wasAdvancedThisTurn = true;
    
            if (bomb.state != previousState)
                OnBombStateChanged?.Invoke(bomb);
    
            if (bomb.state == BombState.Explode)
                OnBombExploded?.Invoke(bomb);
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
        if (boss == null)
            return;
    
        if (boss.state == BossState.Tired)
        {
            SetBossState(BossState.Sleep);
            return;
        }
    
        if (boss.state == BossState.Sleep)
        {
            // After sleeping 1 turn, return to Calm
            SetBossState(BossState.Calm);
            return;
        }
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
    
        int gain = 1;
    
        if (boss.state == BossState.Sleep)
            gain = 2;
    
        boss.energy = Mathf.Min(boss.maxEnergy, boss.energy + gain);
    
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
