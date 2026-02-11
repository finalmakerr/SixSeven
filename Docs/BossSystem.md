# Boss System Rules

## Boss data structure

```csharp
public enum BossDamageSource
{
    Bomb,
    SpecialWeapon
}

public enum BossTriggerType
{
    OnDamaged,
    OnPhaseStart
}

[Serializable]
public class BossAbilityData
{
    public string abilityId;
    public string displayName;
    public int cooldownTurns;
    public int priority;
    public BossTriggerType triggerType;
    public int minPhaseIndex;
}

[Serializable]
public class BossPhaseData
{
    public int phaseIndex;
    public int hpThreshold;          // Phase starts when currentHP <= hpThreshold
    public int damageTakenMultiplier; // Percent. 100 = normal, 75 = resistant
    public List<string> enabledAbilityIds;
    public string phaseStartEffectId;
}

[Serializable]
public class BossData
{
    public string bossId;
    public string displayName;

    // Spawn rules
    public List<int> appearsOnLevels;  // e.g. [6, 12, 18]

    // Grid occupancy
    public List<Vector2Int> occupiedTiles; // Relative offsets from boss anchor tile

    // HP model (raw numbers only)
    public int baseHP;
    public int hpPerDifficultyTier;
    public int currentHP;
    public int maxHP;

    // Phase model
    public int currentPhaseIndex;
    public List<BossPhaseData> phases;

    // Abilities and reactions
    public List<BossAbilityData> abilities;
    public List<string> onDamagedEffectIds;
}
```

### HP scaling formula

Use raw HP and calculate max HP per level:

`maxHP = baseHP + (difficultyTier * hpPerDifficultyTier)`

Example tuning:

- `baseHP = 80`
- `hpPerDifficultyTier = 25`
- Difficulty tier 0: `80 HP`
- Difficulty tier 3: `155 HP`
- Difficulty tier 6: `230 HP`

### Player-facing HP display

- Always show `currentHP / maxHP` in the boss HUD.
- Also show phase markers on the HP bar using each `hpThreshold`.
- Update HP display immediately after each damage event.

## Turn order

1. **Level start**
   - If `currentLevel` is in `appearsOnLevels`, spawn boss and initialize HP/phase.
2. **Player turn**
   - Player moves/matches/uses item.
   - Resolve all player-created effects (matches, bombs, specials).
3. **Boss damage resolution window**
   - Apply any queued boss damage from bombs/specials.
   - Fire `OnDamaged` effects per accepted hit.
   - Check phase threshold crossings and process phase transition effects.
4. **Boss turn**
   - Boss chooses an enabled ability from current phase.
   - Boss executes ability.
5. **End-of-round cleanup**
   - Tick cooldowns/status effects.
   - Check victory/defeat.

This guarantees the boss always acts after the player turn in a deterministic turn-based loop.

## Damage intake rules

### Allowed damage sources

Boss only takes direct HP damage from:

- **Bomb** effects
- **Special weapon** effects

Any non-listed source is ignored unless explicitly tagged as `BossDamageSource`.

### Damage intake pipeline

For each incoming hit:

1. Validate source (`Bomb` or `SpecialWeapon`).
2. Read current phase `damageTakenMultiplier`.
3. Compute final damage:

   `finalDamage = max(1, floor(rawDamage * damageTakenMultiplier / 100f))`

4. Apply HP loss:

   `currentHP = max(0, currentHP - finalDamage)`

5. Trigger reactions:
   - Fire all `OnDamaged` effects.
   - If HP crossed one or more phase thresholds, advance phase(s) and fire `OnPhaseStart` effects.

### Phase transition rules

- A phase transition occurs when `currentHP <= nextPhase.hpThreshold`.
- If a single high-damage hit crosses multiple thresholds, process phases in descending order and trigger each phase start effect once.
- Phase transitions can:
  - unlock new boss abilities,
  - alter `damageTakenMultiplier`,
  - immediately trigger scripted effects (e.g., shield pulse, summon, tile lock).

### Example damage events

- Bomb hit with raw damage `18` in phase with multiplier `100` -> boss takes `18`.
- Special weapon hit with raw damage `30` in resistant phase with multiplier `70` -> boss takes `21`.
- HP drops from `92` to `58`, crossing threshold at `60` -> enter next phase and trigger phase-start effect.

## Boss ability catalog (by required type)

Bosses use one ability per turn from this shared category set:

1. **Direct grid damage**
   - `RuptureLine`: deal damage in a row/column crossing the player tile.
   - `CrushCross`: hit the player tile and the 4 orthogonal adjacent tiles.
2. **Tumor spawning/upgrading**
   - `SeedTumor`: spawn a tier-1 tumor on a high-pressure empty tile.
   - `MutateTumor`: upgrade the oldest/nearest existing tumor by +1 tier.
3. **Bomb disruption**
   - `FuseDelay`: increase nearby bomb timers by +1 turn.
   - `FuseSnap`: decrease selected bomb timers by -1 turn (minimum 0).
4. **Tile locking/corruption**
   - `LockRing`: lock a ring of tiles around the player for N turns.
   - `CorruptLane`: convert a lane into corrupted tiles that penalize movement.
5. **Player debuffs**
   - `EnergyLeech`: reduce player energy by a flat amount and prevent regen this turn.
   - `ShieldShatter`: remove active shield stacks and add a short vulnerability window.

## Ability cooldown system

### Data model

```csharp
[Serializable]
public class BossAbilityRuntimeData
{
    public string abilityId;
    public BossAbilityCategory category;
    public int baseCooldownTurns;   // e.g. 1-4
    public int currentCooldownTurns;
    public int minPhaseIndex;
    public int maxPhaseIndex;       // -1 for no cap
}
```

- `currentCooldownTurns == 0` means the ability is available.
- Cooldowns tick at end of boss turn (after execution).
- After an ability is used:

  `currentCooldownTurns = baseCooldownTurns`

- At cleanup:

  `currentCooldownTurns = max(0, currentCooldownTurns - 1)`

### Turn contract

1. Gather abilities valid for the current phase.
2. Filter to `currentCooldownTurns == 0`.
3. If at least one exists, select exactly one and execute it.
4. If none are available, execute fallback `BasicStrike` (no cooldown).
5. Set used ability cooldown, then tick all cooldowns in cleanup.

This enforces the rule **boss uses exactly 1 ability per turn** while ensuring no dead turns.

## Ability selection logic (semi-random, phase-weighted)

### Weight table by phase

Each ability has per-phase weights; `0` disables that entry.

| Category | Phase 1 weight | Phase 2 weight | Phase 3+ weight |
|---|---:|---:|---:|
| Direct grid damage | 35 | 30 | 20 |
| Tumor spawn/upgrade | 25 | 30 | 25 |
| Bomb disruption | 10 | 20 | 25 |
| Tile lock/corruption | 15 | 10 | 20 |
| Player debuffs | 15 | 10 | 10 |

Boss-specific kits can override these values, but this baseline gives early pressure from damage and scaling control later from disruption/corruption.

### Weighted roll algorithm

```csharp
BossAbilityRuntimeData ChooseAbility(List<BossAbilityRuntimeData> readyAbilities, int phaseIndex, Random rng)
{
    var weighted = new List<(BossAbilityRuntimeData ability, int weight)>();
    foreach (var ability in readyAbilities)
    {
        int w = ability.GetWeightForPhase(phaseIndex);
        if (w > 0)
            weighted.Add((ability, w));
    }

    if (weighted.Count == 0)
        return BasicStrike;

    int total = weighted.Sum(x => x.weight);
    int roll = rng.Next(0, total);

    foreach (var entry in weighted)
    {
        if (roll < entry.weight)
            return entry.ability;
        roll -= entry.weight;
    }

    return weighted[weighted.Count - 1].ability;
}
```

### Anti-repeat safety

- If the rolled ability was also used last turn, apply a **repeat penalty** (e.g. `weight *= 0.4`) and reroll once.
- Never reroll if only one ability is ready.

This keeps behavior readable but not predictable.

## Targeting rules

Targeting uses deterministic heuristics before random tie-breaks so players can learn patterns.

### Shared targeting context

- `playerPosition`
- active bombs (`position`, `turnsRemaining`)
- tumor tiles (`position`, `tier`)
- locked/corrupted tiles
- board bounds + valid target mask

### Category-specific rules

1. **Direct grid damage**
   - Primary target: tile maximizing overlap with player path options for next turn.
   - Tie-breaker: closest to player Manhattan distance.
   - Final tie-breaker: random among equals.

2. **Tumor spawn/upgrade**
   - If tumors below cap -> spawn at empty tile with highest adjacency to existing tumors/corruption.
   - Else upgrade tumor with highest threat score:

     `threat = tier * 3 + proximityToPlayer + clusterBonus`

3. **Bomb disruption**
   - `FuseSnap` prefers bombs with `turnsRemaining == 1` (immediate threat).
   - `FuseDelay` prefers bombs inside player's likely collection route.
   - If no bombs exist, retarget to secondary effect (small direct damage ping).

4. **Tile locking/corruption**
   - Avoid fully sealing the player (must leave at least one legal move).
   - Prefer tiles that reduce next-turn match opportunities.
   - Do not lock already locked tiles unless upgrading duration.

5. **Player debuffs**
   - `ShieldShatter` only when shield > 0; otherwise weight is treated as 0.
   - `EnergyLeech` priority increases with player energy above a threshold.

### Validity guardrails

- Every ability validates candidate targets before cast.
- If target set is invalid, fallback order:
  1. Retarget within same category.
  2. Select another ready ability.
  3. `BasicStrike` fallback.

This prevents null turns and keeps boss intent consistent.
