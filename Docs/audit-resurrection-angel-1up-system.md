# Resurrection / Angel / 1UP System Audit

_Date:_ 2026-02-19
_Scope reviewed:_ `GameManager`, `UIManager`, `GameOverConfig`, `TipsConfig`, `BrainrotShopLogic`, `BrainrotTriviaBonusGame` (`BrainrotStarTracker`), and `HardcoreConfig`.

## SECTION A – Current Death Flow

- **Trigger method:** `GameManager.TriggerGameOver()` (guarded to run from `GameState.Playing`).
- **Does it check 1UP?** Yes. `TriggerGameOver()` calls `ShouldUseOneUp()`.
  - If true, `OneUps` is decremented immediately (`OneUps = Mathf.Max(0, OneUps - 1)`) and resurrection starts via `BeginResurrectionFlow()`.
  - `ShouldUseOneUp()` requires `OneUps > 0`, `gameOverConfig.consumeOneUpOnDeath == true`, and excludes one-up usage when `hardcoreModeEnabled && gameOverConfig.allowHardcoreMode` is true.
- **Does it redirect to shop?** Yes, in resurrection path. `ResurrectionRoutine()` ends with `SetState(GameState.Shop)` and emits `ShopOfferOneUpChanged`.
- **Does it block input?** No explicit gameplay input lock method was found.
  - Effective flow control happens by state transitions (`GameState`), plus UI panel switching in `UIManager.HandleStateChanged()`.
  - Fade overlay is visual (`deathFadeCanvasGroup`) and not used as a hard input blocker.

## SECTION B – Resurrection Flow

- **Where 1UP is consumed:** In `GameManager.TriggerGameOver()`, before coroutine launch.
- **Fade logic exists?** **Yes.**
  - `ResurrectionStarted` is raised from `ResurrectionRoutine()`.
  - `UIManager.HandleResurrectionStarted()` calls `FadeTo(1f)` and runs a fade coroutine.
  - `ResurrectionRoutine()` waits `gameOverConfig.fadeToBlackDuration` before video/tip.
  - On entering shop, `UIManager.HandleStateChanged()` fades back to transparent with `FadeTo(0f)`.
- **Tip logic exists?** **Yes.**
  - `GameManager.GetNextTip()` cycles ordered tips from `TipsConfig.tips`, persists index in player prefs, and returns text.
  - `ResurrectionRoutine()` invokes `ResurrectionTipRequested(nextTip)` when non-empty.
  - `UIManager.HandleResurrectionTipRequested()` writes tip text to UI.
- **Angel logic exists?** No dedicated “Angel” class/system reference was found in scripts reviewed.
  - Resurrection helper signals are currently `ResurrectionVideoRequested` + `ResurrectionTipRequested`.
- **Return-to-board logic exists?** **No direct return to `Playing` from resurrection.**
  - Current resurrection destination is shop (`GameState.Shop`), not immediate board restore.

## SECTION C – Shop Behavior After Death

- **Is 1UP guaranteed?**
  - **Core flow:** Not strictly guaranteed by inventory grant; it only forces one-up *offer signal* via `ShopOfferOneUpChanged(ForceOfferOneUpInShop())`.
  - `ForceOfferOneUpInShop()` returns false for Ironman, else defaults to true unless overridden by config.
  - **Bonus shop logic (`BrainrotShopLogic`):** `ShouldOfferOneUpOnResurrection()` always returns true.
- **Is shop forced?**
  - **Yes after resurrection** (`ResurrectionRoutine() -> SetState(GameState.Shop)`).
  - **No for non-resurrection death outcomes** (`SetState(GameState.GameOver)` path).
- **Is leaving without 1UP allowed?**
  - In `BrainrotShopLogic.TryLeaveShop()`: yes, leaving is allowed even with 0 lives.
  - A warning event is emitted (`ShopWarningRequested("ARE YOU THAT CRAZY??")`) but method still returns true.
  - No hard prevention was found in reviewed code.

## SECTION D – Hardcore Rules

- **Is hardcore config already present?** **Yes.**
  - Mode-level flagging and unlock flow are present in `GameManager` (`hardcoreModeEnabled`, `CurrentGameMode`, unlock gating).
  - `HardcoreConfig` exists as a ScriptableObject with economy and pressure modifiers (shop price/sell/one-up multipliers, reroll limit, etc.).
- **Does hardcore alter death penalties?** **Partially, via one-up usage gate in current death logic.**
  - `ShouldUseOneUp()` disables one-up consumption when `hardcoreModeEnabled && gameOverConfig.allowHardcoreMode`.
  - No distinct hardcore-only death penalty branch beyond this was found in `TriggerGameOver()`.

## Additional findings relevant to audit request

- **Auto-retry side path exists:** `TriggerGameOver()` optionally calls `TryAutoRetryOnDeath` hook before final game over.
  - `BrainrotStarTracker.TryAutoRetryOnDeath()` consumes chest/star state and returns true when available.
- **No `ShopManager` class found:** shop behavior in reviewed scope is event-driven from `GameManager` plus `BrainrotShopLogic` in bonus scripts.
