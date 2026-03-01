# CODEX Audit: HP & Damage Paths

## Scope
Searched project for:
- `.CurrentHP`
- `-=`
- `ApplyDamage`
- `TakeDamage`
- `HP -`
- `SetHP`
- `Health`
- `remainingHp`
- `GetMonsterHitPoints`

Primary commands used:
- `rg -n "\.CurrentHP|-=|ApplyDamage|TakeDamage|HP -|SetHP|Health|remainingHp|GetMonsterHitPoints" /workspace/SixSeven`
- `rg -n "ApplyDamage\(|TakeDamage\(|SetCurrentHp\(|SetHP\(|GetMonsterHitPoints|remainingHp|currentHP\s*[-+*/]?=|\.CurrentHP|\bHP\s*=\s*Mathf\.Max\(0,\s*\w+\.HP\s*-" --glob '!functions/node_modules/**' --glob '*.cs'`

---

## Prompt 1 — All HP Modifications / Damage Application

### Monster HP modified

| File | Method | Exact HP-modification line |
|---|---|---|
| `Assets/gamecore/scripts/GameManager.cs` | `ApplyMonsterDamage(int pieceId, int amount)` | `state.CurrentHP = Mathf.Max(0, state.CurrentHP - amount);` |
| `Assets/gamecore/scripts/GameManager.cs` | `ForceKillMonster(Piece piece)` | `state.CurrentHP = 0;` |
| `Assets/gamecore/scripts/GameManager.cs` | `SyncBossVitalsToState(ref BossState bossState)` | `bossState.CurrentHP = bossVitalsSystem.CurrentHp;` |
| `Assets/scripts/systems/BossVitalsSystem.cs` | `ApplyDamage(int amount)` | `currentHp -= hpLost;` |
| `Assets/scripts/systems/BossVitalsSystem.cs` | `ApplyHeal(int amount)` | `currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);` |
| `Assets/scripts/systems/BossVitalsSystem.cs` | `SetCurrentHp(int amount)` | `currentHp = Mathf.Clamp(amount, 0, maxHp);` |

### Player HP modified

| File | Method | Exact HP-modification line |
|---|---|---|
| `Assets/gamecore/scripts/GameManager.cs` | `TakeDamage(int halfUnits, DamageSource source)` | `var damageResult = ApplyDamage(halfUnits);` *(delegates to helper that changes HP)* |
| `Assets/gamecore/scripts/GameManager.cs` | `TakeDamage(int halfUnits, DamageSource source)` | `SetPlayerCurrentHp(Mathf.Min(maxHearts * 2, 2));` *(second-chance revive)* |
| `Assets/gamecore/scripts/GameManager.cs` | `ApplyDamage(int amount)` | `return playerVitalsSystem.ApplyDamage(amount);` *(delegates to `PlayerVitalsSystem`)* |
| `Assets/gamecore/scripts/GameManager.cs` | `SetPlayerCurrentHp(int amount)` | `playerVitalsSystem.SetCurrentHp(amount);` |
| `Assets/scripts/systems/PlayerVitalsSystem.cs` | `ApplyDamage(int amount)` | `hpUnits -= hpLost;` |
| `Assets/scripts/systems/PlayerVitalsSystem.cs` | `SetCurrentHp(int amount)` | `hpUnits = Mathf.Clamp(amount, 0, MaxBaseHp);` |
| `Assets/scripts/systems/PlayerVitalsSystem.cs` | `RestoreHp(int amount)` | `hpUnits = Mathf.Clamp(hpUnits + amount, 0, MaxBaseHp);` |
| `Assets/scripts/systems/PlayerVitalsSystem.cs` | `RefillHpToBaseMaximum()` | `hpUnits = MaxBaseHp;` |
| `Assets/scripts/gameplay/GameTurnController.cs` | `ResolveBossActionSkeleton()` | `player.HP = Mathf.Max(0, player.HP - damage);` *(appears 3 times in this method)* |

### Damage applied / direct HP reduction hotspots

| File | Method | Exact line |
|---|---|---|
| `Assets/gamecore/scripts/GameManager.cs` | `TakeDamage(int halfUnits, DamageSource source)` | `var damageResult = ApplyDamage(halfUnits);` |
| `Assets/gamecore/scripts/GameManager.cs` | `ApplyMonsterDamage(int pieceId, int amount)` | `state.CurrentHP = Mathf.Max(0, state.CurrentHP - amount);` |
| `Assets/gamecore/scripts/GameManager.cs` | `ApplyBossDamage(int damage, string source)` | `var damageResolution = bossVitalsSystem.ApplyDamage(damage);` |
| `Assets/scripts/systems/PlayerVitalsSystem.cs` | `ApplyDamage(int amount)` | `hpUnits -= hpLost;` |
| `Assets/scripts/systems/BossVitalsSystem.cs` | `ApplyDamage(int amount)` | `currentHp -= hpLost;` |
| `Assets/scripts/systems/VitalsPresenter.cs` | `ApplyIncomingDamage(int amount)` | `var result = vitals.ApplyDamage(amount);` *(entry wrapper)* |
| `Assets/scripts/gameplay/GameTurnController.cs` | `ResolveBossActionSkeleton()` | `player.HP = Mathf.Max(0, player.HP - damage);` |

---

## Prompt 2 — Damage Entry Methods

| Method | File | Category | Direct HP change or helper? |
|---|---|---|---|
| `TakeDamage(int halfUnits)` | `Assets/gamecore/scripts/GameManager.cs` | Player damage entry (default hazard source) | Helper wrapper (`TakeDamage(int, DamageSource)`) |
| `TakeDamage(int halfUnits, DamageSource source)` | `Assets/gamecore/scripts/GameManager.cs` | Player damage entry | Helper (`ApplyDamage` -> `PlayerVitalsSystem.ApplyDamage`) |
| `ApplyBottomLayerHazardIfNeeded()` | `Assets/gamecore/scripts/GameManager.cs` | Hazard/DoT trigger | Calls `TakeDamage(1, DamageSource.Hazard)` |
| `ResolvePlayerHit()` | `Assets/gamecore/scripts/GameManager.cs` | Monster-to-player hit trigger | Calls `TakeDamage(1, DamageSource.Monster)` |
| `ApplyDamage(int amount)` | `Assets/gamecore/scripts/GameManager.cs` | Player damage helper | Delegates to `playerVitalsSystem.ApplyDamage` |
| `PlayerVitalsSystem.ApplyDamage(int amount)` | `Assets/scripts/systems/PlayerVitalsSystem.cs` | Player HP reducer | **Direct** (`hpUnits -= hpLost`) |
| `TryApplyMonsterDamage(Piece piece, int amount)` | `Assets/gamecore/scripts/GameManager.cs` | Monster damage entry | Calls helper `ApplyMonsterDamage` |
| `ApplyMonsterDamage(int pieceId, int amount)` | `Assets/gamecore/scripts/GameManager.cs` | Monster HP reducer | **Direct** (`state.CurrentHP = Mathf.Max(0, state.CurrentHP - amount)`) |
| `DefeatBoss(Vector2Int position, string source)` | `Assets/gamecore/scripts/GameManager.cs` | Boss damage trigger | Calls helper `ApplyBossDamage(1, source)` |
| `ApplyBossDamage(int damage, string source)` | `Assets/gamecore/scripts/GameManager.cs` | Boss damage entry/helper | Helper (`bossVitalsSystem.ApplyDamage`) + state sync |
| `BossVitalsSystem.ApplyDamage(int amount)` | `Assets/scripts/systems/BossVitalsSystem.cs` | Boss HP reducer | **Direct** (`currentHp -= hpLost`) |
| `VitalsPresenter.ApplyIncomingDamage(int amount)` | `Assets/scripts/systems/VitalsPresenter.cs` | UI/sample incoming damage entry | Helper (`vitals.ApplyDamage`) |
| `GameTurnController.ResolveBossActionSkeleton()` | `Assets/scripts/gameplay/GameTurnController.cs` | Boss attack trigger vs player | **Direct** (`player.HP = Mathf.Max(0, player.HP - damage)`) |

### Hazard / DoT / boss / coin-drop notes
- Hazard damage trigger found: `ApplyBottomLayerHazardIfNeeded()` -> `TakeDamage(1, DamageSource.Hazard)`.
- DoT-style damage found via repeated hazard tick path (bottom-layer poison flow).
- Boss damage trigger found: `DefeatBoss(...)` -> `ApplyBossDamage(1, source)`.
- No explicit **coin drop from hit** method was found in combat damage paths (`GameManager`, `Board`, `systems`) during this audit.
