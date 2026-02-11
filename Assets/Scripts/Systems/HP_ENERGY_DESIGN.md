# HP + Energy System Design

## Data structures

- `PlayerVitalsSystem`
  - **HP scale:** integer units where `1 unit = 1 HP = half-heart`.
  - `UnlockedHearts` (1..7)
  - `CurrentHp` (0..`UnlockedHearts * 2`)
  - `CurrentShield` (0..`UnlockedHearts`), max one shield layer per heart.
  - `MaxTotalHp = UnlockedHearts * 3` (`2 base + 1 shield per heart`)
  - `EnergyUnlockedHearts` (1..7)
  - `CurrentEnergy` (0..`EnergyUnlockedHearts * 2`)
  - `DamageResolution { ShieldLost, HpLost, IsFatal }`

- `VitalsBarUIController`
  - Horizontal icon lists for HP and Energy.
  - Tracks `maxHpHeartsShown` and `maxEnergyHeartsShown` to enforce **never shrink below previously reached size**.
  - Supports half/full/empty sprites and optional shield overlays.

- `VitalsPresenter`
  - Runtime flow coordinator for game actions.
  - Explicit energy spend/gain APIs (no passive regen).
  - Triggers UI refresh after each state change.

## UI update flow

1. Gameplay event updates model (`TrySpendEnergy`, `GainEnergy`, `ApplyDamage`, `AddShield`, cap increase).
2. `VitalsPresenter.RefreshUI()` calls `VitalsBarUIController.Refresh(vitals)`.
3. Bar controller grows icon count only when max hearts increase:
   - `maxHpHeartsShown = max(maxHpHeartsShown, UnlockedHearts)`
   - `maxEnergyHeartsShown = max(maxEnergyHeartsShown, EnergyUnlockedHearts)`
4. Visuals are recalculated:
   - HP and Energy hearts mapped to full/half/empty by 2-unit heart buckets.
   - Shield overlay shown per protected heart slot.
   - Locked hearts beyond current max (but below historical max shown) are dimmed to preserve width.

## Damage / shield resolution order

`ApplyDamage(amount)` resolves in strict order:

1. Subtract damage from shield pool first (`CurrentShield`).
2. Remaining damage reduces base HP (`CurrentHp`).
3. Fatal check is performed after HP reduction (`CurrentHp <= 0`).

This guarantees shield layers always absorb damage before base heart HP.
