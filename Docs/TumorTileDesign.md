# Tumor Obstacle Tile Design

## Overview
The **Tumor** tile is a layered board obstacle that blocks both tile movement and pathing.
It is intended to increase randomness/replayability by creating variable dead-zones that must be cleared with tactical damage sources.

Core behavior:
- Prevents matches from passing through it.
- Blocks grid connectivity (no adjacency through a tumor cell).
- Cannot be swapped.
- Must be destroyed in strict layer order.

## Tier Structure (7 Tiers)

| Tier | Material | Max HP | Crack Stage Range |
|---|---|---:|---|
| 1 | Grass | 2 | 0-1 |
| 2 | Wood | 4 | 0-2 |
| 3 | Bronze | 6 | 0-2 |
| 4 | Silver | 8 | 0-3 |
| 5 | Gold | 10 | 0-3 |
| 6 | Platinum | 12 | 0-4 |
| 7 | Diamond | 14 | 0-4 |

### Progression Rule
- Tumor starts at a chosen tier (1-7).
- Damage reduces HP of the current tier only.
- When HP reaches `0`, the tier breaks and immediately reveals the next lower tier.
- Tumor is removed only after Tier 1 (Grass) reaches `0` HP.

## Damage Handling

### Valid Damage Sources
- Bombs
- Special weapons
- Boss attacks

### Damage Contract
1. Ignore swap-based damage (tumor cannot be swapped).
2. Accept only the 3 allowed damage source categories.
3. Apply incoming damage to current exposed tier HP.
4. Clamp HP to `>= 0`.
5. If HP is depleted, transition to next layer and reset HP to that tier's max HP.
6. Emit state events for gameplay systems:
   - `OnTumorDamaged`
   - `OnTumorLayerBroken`
   - `OnTumorDestroyed`

### Suggested Data Shape

```csharp
public enum TumorDamageSource
{
    Bomb,
    SpecialWeapon,
    BossAttack
}

public sealed class TumorTierDefinition
{
    public int TierIndex;      // 1..7
    public string Name;        // Grass..Diamond
    public int MaxHp;          // 2..14 step 2
    public int CrackStages;    // visual sub-steps
}
```

## Visual State Progression

### Visual Layers
For each tier, visuals should combine:
1. **Material base sprite** (Grass/Wood/Bronze/Silver/Gold/Platinum/Diamond).
2. **Crack overlay** driven by remaining HP ratio.
3. **Break VFX** when a layer is destroyed.

### Crack Stage Rule
- Crack intensity increases as HP falls.
- Compute: `damageRatio = 1 - (currentHp / maxHp)`.
- Convert to crack stage using tier-specific stage count.

Example:
- 0% damage: no crack
- ~25% damage: crack stage 1
- ~50% damage: crack stage 2
- ~75% damage: crack stage 3
- ~100% damage: layer break + reveal next tier

### UX Signals
- Brief hit flash per accepted damage.
- Distinct break sound/VFX per material family.
- Stronger shake on higher-tier breaks to communicate significance.

## Board Interaction Rules
- **Match blocking:** match scans must treat tumor cells as hard stops in row/column runs.
- **Connectivity blocking:** flood/path checks must not traverse tumor cells.
- **Swap blocking:** attempts involving tumor cells are invalid and should not consume turns.

## Balance Notes
- Higher tiers add strategic variance by changing board topology over multiple turns.
- Boss attacks can intentionally re-shape board flow by focusing tumor weak points.
- Combined bomb/special play remains the fastest and most readable counterplay.
