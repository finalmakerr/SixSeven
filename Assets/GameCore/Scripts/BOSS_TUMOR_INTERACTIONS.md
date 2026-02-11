# Tumor–Boss Interaction Rules

## Core Interaction Rules
- At the end of each boss turn, bosses may **spawn Tumors on empty normal tiles** (`SpecialType.None`).
- Bosses may also **upgrade existing Tumors by +1 tier** each upgrade attempt.
- Boss-driven tumor progression is capped at **Diamond tier (Tier 4)**.
- Tumors spawned by bosses are configured as `SpecialType.Tumor` immediately, so they **block matches as soon as they appear**.

## Upgrade Constraints
- `maxBossTumorTier` controls the cap and is clamped to `[1,4]`.
- A tumor upgrade can only target tumors with `TumorTier < maxBossTumorTier`.
- If no valid tumor exists for upgrade, that specific upgrade attempt is skipped.
- If no valid empty tile exists for spawning, that spawn attempt is skipped.

## Advanced Behavior Hooks
- Bosses can opt into `tumorBehavior`:
  - `HealFromTumors`: heal each turn based on `healPerTumorTier * totalTumorTierOnBoard`.
  - `ShieldFromTumors`: gain shield each turn based on `shieldPerTumorTier * totalTumorTierOnBoard`.
- Shield is tracked as `BossState.TumorShield` and absorbs incoming boss damage before HP.
- Destroying a tumor weakens boss defenses by reducing `TumorShield` by 1 when shield is present.

## Strategic Counterplay Hooks
- **Priority clear:** focus tumor tiles early to deny heal/shield scaling.
- **Tier denial:** interrupt upgrades before tumors reach high tiers (especially Tier 4).
- **Shield break windows:** destroy tumors right before burst turns to strip shield and open HP damage windows.
- **Goal synergy:** tumor-clearing actions naturally progress `DestroyTumors` mini-goals while reducing boss scaling.
