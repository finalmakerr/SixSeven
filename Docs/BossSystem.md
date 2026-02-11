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
