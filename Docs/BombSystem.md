# Bomb System Rules

## Bomb creation from match size

- **X4** creates a bomb with **1-2** damage.
- **X5** creates a bomb with **3-4** damage.
- **X6** creates a bomb with **5-6** damage.
- **X7** creates a bomb with **7-8** damage.

All created bombs start with a **2-turn timer**.

## Turn countdown logic

1. Keep active bombs in a list.
2. At end of turn, decrement each bomb timer by 1.
3. Bombs that reach 0 are removed from active list and explode automatically.

This flow is implemented by `BombSystem.TickBombs(...)`.

## Pickup interaction rules

- If player moves onto the bomb's tile before countdown ends, remove that bomb from the active list.
- No explosion happens for picked bombs.

This flow is implemented by `BombSystem.TryPickupBomb(...)`.

## Explosion damage map

- Radius is always **2 tiles** from center.
- Manhattan distance **0-1**: full bomb damage.
- Manhattan distance **2**: half bomb damage (`max(1, damage/2)`).

This flow is implemented by `BombSystem.BuildExplosionDamageMap(...)`.


## Boss bomb damage calculation

- Bomb radius includes boss tile checks.
- Distance **1** from blast center deals full damage.
- Distance **2** from blast center deals half damage.
- Boss phase can reduce incoming bomb damage via resistance %.
- Final boss damage is rounded down.
- Boss cannot be one-shot when starting the hit at full HP.

### Pseudocode

```text
function CalculateBossBombDamage(bombDamage, distance, bossCurrentHp, bossMaxHp, phaseResistancePercent):
    if bombDamage <= 0 or bossCurrentHp <= 0:
        return 0

    if distance > 2:
        return 0

    if distance <= 1:
        baseDamage = bombDamage
    else:
        baseDamage = floor(bombDamage / 2)

    resistance = clamp(phaseResistancePercent, 0, 100)
    reducedDamage = floor(baseDamage * (100 - resistance) / 100)

    if reducedDamage <= 0:
        return 0

    isFullHp = bossCurrentHp >= max(1, bossMaxHp)
    if isFullHp and reducedDamage >= bossCurrentHp:
        return max(0, bossCurrentHp - 1)

    return min(reducedDamage, bossCurrentHp)
```

### Edge cases handled

1. **Distance outside bomb radius** (`distance > 2`) returns `0` damage.
2. **Invalid/empty damage values** (`bombDamage <= 0`) return `0` damage.
3. **Dead boss state** (`bossCurrentHp <= 0`) returns `0` damage.
4. **Resistance bounds** are clamped to `[0, 100]`.
5. **Round down behavior** is applied by integer division in half damage and resistance reduction.
6. **One-shot protection** leaves boss at `1 HP` when hit from full HP by a lethal bomb.
7. **Overkill clamp** ensures result never exceeds current HP.
