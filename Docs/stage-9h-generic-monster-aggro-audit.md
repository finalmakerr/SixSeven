# Stage 9H – Full Generic Monster Aggro Audit & Safety Verification

Scope: read-only behavior audit of generic monster aggro logic in `GameManager.cs`, forecast simulation in `Board.cs`, and aggro config in `MonsterAggroConfig.cs`.

## Section 1 – State machine integrity

**Result: ❌ Critical Issue**

- Idle → Angry is present via `SetGenericMonsterAngry`.
- Angry → Enraged is present in `UpdateMonsterStates`.
- Enraged → Attack is **broken**: `ProcessMonsterAttacks` currently attacks only when `state.IsAngry && state.TurnsUntilAttack <= 0`, but `UpdateMonsterStates` turns Angry off immediately and sets Enraged on before attack processing.
- Attack → Tired transition exists.
- Tired → Sleep and Sleep → Idle transitions exist.
- Hurt is terminal by guard in `EnterIdleState` and by Hurt checks before re-aggro.
- Confused interrupts anger cycle when a monster moves while Angry/Enraged.
- State durations are sourced from `MonsterAggroConfig` getters.
- Duplicate transition protection exists (guards for already Angry/Enraged and blocked states).

## Section 2 – Hybrid forecast verification

**Result: ✅ Verified & Safe**

- `CanMatchPieceWithinEnergyDepth()` exists and gates by `availableEnergy`.
- Search depth is clamped to `Mathf.Min(2, availableEnergy)` (max 2 swaps).
- Cascades are simulated through repeated match-resolve-gravity/refill in `ResolveAllMatchesAndCascades`.
- Real board is not mutated; simulation operates on cloned `SimulationState`/`SimPiece` arrays.
- Deterministic behavior is used via seeded simulation RNG (`RandomState`) and deterministic refill.
- No item/inventory logic is included in this forecast path.

## Section 3 – HP survivability logic

**Result: ✅ Verified & Safe**

- `WillMonsterSurviveUntilAttack()` exists.
- Prediction only includes guaranteed debuff + hazard tile damage (`PredictGuaranteedDamageNextTick`).
- Player match damage is not estimated.
- Survivability gate prevents anger and routes to Hurt (`EnterHurtState`) when failing.

## Section 4 – Damage trigger filtering

**Result: ⚠️ Minor Issue (non-breaking for current flow)**

- Damage-trigger processing supports multiple piece IDs via `HashSet<int> damagedThisTurn` and iterates all entries.
- Survivability check is applied before setting Angry.
- Hurt blocks re-aggro in both damage and adjacency paths.
- Damage-triggered Angry suppresses telegraph spawn via `allowTelegraph: false`.
- `damagedThisTurn` is cleared after processing, but clear is inside `IsDamageTriggerAllowed()` branch; if config disables damage trigger while entries exist, stale IDs can persist until re-enabled.
- Population of `damagedThisTurn` is currently only observed in boss damage path (`ApplyBossDamageAndPhaseProgress`), limiting practical multi-monster damage reactivity.

## Section 5 – Telegraph system verification

**Result: ❌ Critical Issue**

- Generic telegraph spawn is position-bound (`Dictionary<Vector2Int, GameObject>`).
- Spawn timing is aligned to Angry → Enraged for generic telegraph path.
- Damage-triggered aggro does not spawn telegraph (explicit `allowTelegraph: false`).
- Cleanup is incomplete for the per-monster attack telegraph (`AttackTelegraphSystem`):
  - Hurt cleanup exists.
  - Attack-resolve cleanup exists.
  - **Confused/Tired/Sleep transitions do not remove attack telegraph IDs.**
  - This can leave stale attack telegraphs if a monster is interrupted before attack resolution.
- Generic telegraph dictionary is proactively cleaned (`Remove`, `Clear`, orphan cleanup).
- Generic telegraph path does not call `AttackTelegraphSystem.ClearAllTelegraphs()`, so boss telegraph ownership is not globally disrupted.

## Section 6 – Config extraction

**Result: ✅ Verified & Safe**

- Durations are provided via `MonsterAggroConfig` getters (`turnsBeforeAttack`, `confusedDuration`, `tiredDuration`, `sleepDuration`, `enrageDuration` for boss threshold math).
- `defaultMonsterHP` is used for state initialization.
- Config flags gate behavior (`adjacencyRequiresMatchForecast`, `damageTriggerAllowed`, `telegraphOnlyOnEnrage`, `requireHpSurvivalCheck`).
- Null safety exists via `EnsureMonsterAggroConfig()` and `GetMonsterAggroConfig()`.

## Section 7 – Performance & edge cases

**Result: ⚠️ Minor Issue**

- No unbounded recursive simulation: forecast recursion depth is capped at 2.
- No dictionary mutation-during-enumeration issues in aggro update loops (uses temp dictionaries/lists before mutation).
- No key duplication risk in `monsterStates` (dictionary keyed by piece ID).
- `TryFindPieceById` has null/bounds safety checks.
- `adjacencyAggroUsedThisTurn` is reset at turn-end processing and set only on successful adjacency aggro.
- Minor risk remains from stale attack telegraph IDs when interrupted states are entered (see Section 5).

---

## Overall verdict

The system is **not yet production-stable** due to two critical issues:
1. Enraged monsters do not satisfy current attack trigger condition.
2. Attack telegraphs may persist across Confused/Tired/Sleep interruptions.
