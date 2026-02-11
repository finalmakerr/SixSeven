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

## Boss-level outcomes

This section defines post-battle resolution for boss levels (win, defeat, retry, and rewards).

### Outcome contract

- Boss victory and defeat are resolved immediately at end-of-round cleanup.
- Rewards are granted only on **boss win**.
- Auto-retry can only trigger on **boss defeat** when player has exactly `3 stars` available to consume.
- Boss retries always reset boss combat state (HP, phase, cooldowns, spawned boss-only entities).
- Boss defeats never apply permanent max-stat penalties.

---

### Win flow

1. Detect boss HP `<= 0`.
2. Lock board input and stop turn simulation.
3. Calculate and grant rewards:
   - coins,
   - +1 star,
   - optional max HP increase roll,
   - optional max Energy increase roll,
   - optional shop unlock roll(s).
4. Persist account/meta-progression updates.
5. Show boss-clear summary UI with reward breakdown.
6. Advance to next level/scene.

Pseudo flow:

```text
if boss.currentHP <= 0:
  rewards = RollBossRewards(levelId, difficultyTier)
  GrantCoins(rewards.coins)
  GrantStar(1)
  if rewards.maxHpIncrease > 0: IncreaseMaxHP(rewards.maxHpIncrease)
  if rewards.maxEnergyIncrease > 0: IncreaseMaxEnergy(rewards.maxEnergyIncrease)
  UnlockShopItems(rewards.shopUnlocks)
  SaveProgress()
  ShowWinFlow(rewards)
```

---

### Lose flow

1. Detect player death (`HP <= 0`).
2. Evaluate star-based retry gate:
   - If player has `3 stars`:
     - consume/reset stars to `0`,
     - auto-retry same boss level,
     - reset boss combat state before restarting.
   - Otherwise:
     - enter standard game-over flow.
3. On boss-level defeat, do **not** reduce max HP or max Energy.

Pseudo flow:

```text
if player.hp <= 0:
  if player.stars >= 3:
    player.stars = 0
    ResetBossStateForRetry()
    RestartCurrentLevel(autoRetry=true)
  else:
    TriggerGameOverFlow()
```

---

### Retry logic (boss-specific)

On any retry path (auto-retry or manual retry), apply all of the following:

1. Reset boss HP to computed max HP for current level tier.
2. Reset boss phase index to phase `0`.
3. Clear boss cooldowns/runtime statuses.
4. Despawn boss-summoned entities and clear boss-owned tile effects.
5. Reinitialize deterministic encounter seed (or same seed policy per design).
6. Restore player to level-start vitals using current max values (without reducing max HP/Energy).

Explicit non-goals on boss failure:

- No permanent reduction to `MaxHP`.
- No permanent reduction to `MaxEnergy`.
- No retention of prior failed attempt boss damage/progress.

---

### Reward tables

Use this baseline table unless a boss has an explicit override profile.

#### Boss win reward table

| Reward type | Rule | Notes |
|---|---|---|
| Coins | `baseCoins + (difficultyTier * tierCoins)` where `baseCoins=30`, `tierCoins=10` | Example: tier 0 = 30, tier 3 = 60 |
| Star | `+1` guaranteed | Counts toward 3-star auto-retry bank/chest systems |
| Max HP increase | `20%` chance, `+1 heart` | Clamp to global max heart cap |
| Max Energy increase | `20%` chance, `+1 heart` | Clamp to global max energy-heart cap |
| Shop unlock | `35%` chance to unlock 1 item from boss pool | Ignore if no locked items remain |

#### Suggested boss reward profile structure

```csharp
[Serializable]
public sealed class BossRewardProfile
{
    public int baseCoins = 30;
    public int coinsPerDifficultyTier = 10;
    [Range(0f, 1f)] public float maxHpIncreaseChance = 0.20f;
    public int maxHpHeartsGranted = 1;
    [Range(0f, 1f)] public float maxEnergyIncreaseChance = 0.20f;
    public int maxEnergyHeartsGranted = 1;
    [Range(0f, 1f)] public float shopUnlockChance = 0.35f;
    public int shopUnlockCount = 1;
    public List<string> shopUnlockPoolItemIds;
}
```

#### Reward roll resolution order

1. Compute deterministic reward RNG seed (`playerId + levelId + clearCount`).
2. Grant guaranteed rewards first (coins, star).
3. Roll max HP increase chance.
4. Roll max Energy increase chance.
5. Roll shop unlock(s).
6. Persist and broadcast reward payload to UI.

This order keeps reward generation deterministic and auditable.

---

## Boss encounter replayability framework

This framework introduces controlled run-to-run variance while preserving readability, fairness, and deterministic debugging.

### 1) Randomization rules

#### Seed model

Use one encounter seed per boss attempt, derived from:

`encounterSeed = hash(runSeed, levelId, bossId, attemptIndex)`

Sub-streams should be split so each system is stable when another system changes:

- `abilityRng` for action ordering/variants
- `spawnRng` for tumor placement
- `goalRng` for mini-goal selection/parameters

This avoids cross-coupling (e.g., adding one spawn roll should not silently change ability order).

#### Boss behavior variance per run

Define each boss ability with small parameter bands rather than fully different logic:

- Damage abilities: ±5% to ±12% coefficient
- Utility abilities: cooldown variance `-1/0/+1` turns within safe bounds
- Status effects: duration variance `±1` turn

Guardrails:

- Never exceed telegraphed one-shot thresholds unless in explicit enrage state.
- Keep per-run variance bounded by boss tier profile.
- Keep phase identity intact (Phase 1 should still feel like setup, Phase 3 like climax).

#### Ability order randomization

Use a weighted deck model per phase:

1. Build deck from enabled abilities and phase weights.
2. Ban exact repeats for `N=1` turn unless only one valid ability exists.
3. Apply soft anti-streak rule: if a category has been used 2 turns in a row, reduce its weight by 30-50% for next draw.
4. Keep signature moves on cadence via hard constraints (e.g., must occur once every 4 turns).

Result: order feels different each run, but never chaotic or unreadable.

#### Tumor spawn location randomization

Compute candidate tiles from empty valid cells, then select probabilistically by score:

`spawnScore = pressureWeight * localPressure + adjacencyWeight * tumorAdjacency + objectiveWeight * objectiveInfluence + noise(0..k)`

Rules:

- Exclude blocked/special invalid tiles.
- Maintain minimum distance from immediate player spawn if required by tutorial or fairness mode.
- Inject small noise for run variance while preserving tactical logic.
- Re-roll with fallback if chosen tile becomes invalid before resolution.

Objective influence examples:

- If mini-goal is `ProtectTile`, increase spawn pressure near that tile.
- If mini-goal is `DestroyTumors`, slightly distribute spawns across multiple lanes instead of single-clump stacking.

### 2) Mini-goal integration

Mini-goals are encounter modifiers with three effects:

1. **Victory-side progress track** (player objective)
2. **Boss mutation hook** (how boss behavior shifts while goal is active)
3. **Reward/difficulty budget impact**

#### Goal selection rules

At encounter start:

- Roll 1 primary mini-goal from allowed set by boss + biome + difficulty tier.
- Optional 20-35% chance for a secondary lightweight modifier on high tiers.
- Reject combinations that conflict with boss kit (e.g., `Destroy X Tumors` on boss with no tumor actions).

#### Example mini-goals and hooks

1. **Destroy X tumors to weaken boss**
   - Progress: increment on tumor destruction.
   - Boss hook: each tumor destroyed applies `-shield`, `-healFromTumor`, or temporary `+damageTaken` stack.
   - Completion payoff: immediate stagger (skip next utility action) or phase armor break.

2. **Survive Y turns before boss enrages**
   - Progress: round counter.
   - Boss hook: pre-enrage pattern uses lower burst and more setup skills.
   - At Y turns: enrage triggers predictable pattern swap (higher damage, lower control) so players can plan windows.

3. **Protect a specific tile**
   - Progress: tracked each turn tile remains intact/unoccupied.
   - Boss hook: boss prioritizes corruption/push effects toward protected tile using `objectiveInfluence` in spawn/action targeting.
   - Failure state: tile broken -> apply boss buff or remove player boon; do not hard-end run unless explicitly designed.

#### Integration pipeline in turn loop

- Start of encounter: select mini-goal(s), initialize counters, publish UI intent text.
- End of player turn: evaluate goal progress events.
- Boss decision step: read mini-goal state and apply behavior modifiers before ability selection.
- End of round: resolve milestone effects (threshold rewards, warnings, enrage timers).

### 3) Difficulty scaling model

Scale by a total encounter budget so randomness stays fair.

`totalBudget = baseTierBudget + runDepthBudget + mutatorBudget`

Distribute budget across four knobs:

- `B1` Boss power (HP, damage coefficients)
- `B2` Pattern complexity (ability weights, cadence constraints)
- `B3` Board pressure (tumor spawn count/tier/frequency)
- `B4` Mini-goal strictness (X, Y values; penalty severity)

#### Tier recommendations

- **Low tiers**
  - Small behavior variance bands
  - One mini-goal only
  - Tumor spawn noise high but count low
  - Softer penalties on mini-goal failure

- **Mid tiers**
  - Moderate variance
  - Goal parameters tighten (higher X, lower Y)
  - Ability anti-streak still strong for readability
  - Add one milestone mutation event

- **High tiers / challenge mode**
  - Full variance bands with strict guardrails
  - Possible secondary mini-goal
  - Faster tumor escalation or higher max tier
  - Harder enrage breakpoint but explicit telegraphing and deterministic cadence

#### Anti-frustration constraints (all tiers)

- Never randomize away required counterplay tools.
- Keep at least one predictable recovery window every `M` turns.
- Cap combined pressure spikes (ability burst + tumor spike + goal penalty) within one round.
- Prefer visible telegraphs over hidden RNG whenever behavior meaningfully changes.
