# SIXSEVEN – Shop System Pre-Implementation Audit Questions

## Scope confirmation
1. Which runtime flow is authoritative for gameplay/shop integration?
   - `Assets/scripts/*` (e.g., `GameState`, `UIManager`)
   - `Assets/gamecore/scripts/*` (e.g., `GameCore.GameManager`)
   - If mixed, what is the exact authority boundary?

2. Which scene(s) are currently used for active gameplay?
   - `Assets/scenes/Game.unity`
   - `Assets/gamecore/scenes/*`
   - Other (please list exact scene paths + load order)

## 1) Run management
3. Does a persistent per-run object already exist (e.g., `RunState`)?
   - If yes: where is it created/stored, and how does it survive scene loads?
   - If no: confirm we should introduce one.

4. Which fields must persist for the duration of a run?
   - Gold
   - Owned items
   - 1UP count
   - Selected shopkeeper
   - Last shop inventory snapshot
   - Other (please list)

5. If app closes mid-run, should shop/run state be restored on relaunch or treated as session-only?

## 2) Level flow / before-boss hook
6. Is boss detection authoritative via `LevelDefinition.isBossLevel` only?

7. Are optional/secret boss levels present, and should shop trigger before all boss types or only mandatory bosses?

8. What is the canonical level transition hook for inserting the shop step?
   - `CompleteLevel`
   - `LoadNextLevel`
   - Custom pipeline (please identify method/event)

## 3) Inventory + shop catalog
9. Should shop inventory reuse the current item model (enum-based), or move to data-driven catalog (`ScriptableObject`)?

10. If data-driven, confirm required catalog fields:
    - `id`
    - category
    - base price
    - stack rules
    - icon
    - description
    - other required metadata?

11. What item categories are required for weighted rolls?

12. Can a single item belong to multiple categories/tags for roll weighting?

13. Should purchasing reuse existing transaction flow, or should shop use a dedicated purchase pipeline?

## 4) Currency / economy
14. Which currency is authoritative for shop purchases, and where is it stored?

15. Are global price modifiers already implemented?
    - If yes: what is precedence order relative to shopkeeper multipliers?

16. Is the shop buy-only, or buy + sell-back?

## 5) 1UP + revive
17. Which current 1UP counter/state is canonical?

18. “Guaranteed if player has 0” means which behavior?
    - Guaranteed 1UP item slot in shop inventory
    - Auto-grant 1UP on shop open

19. Confirm fixed 1UP price and whether it is constant across all difficulties/modes.

20. On revive return, should we restore:
    - Same shopkeeper
    - Same inventory snapshot
    - Same prices
    - Or regenerate inventory/prices?

## 6) UI architecture + board visibility
21. Should shop be implemented as:
    - Overlay panel in current scene
    - Dedicated shop scene

22. Which GameObject/root canvas represents the board/gameplay area that must be hidden/disabled during shop?

23. During shop, should simulation be fully paused (game logic + timers), or only board visuals hidden?

## 7) Dialogue
24. Is there an existing dialogue system to reuse?
    - If yes: how is it triggered and fed data?

25. If no reusable system exists, confirm we should build a lightweight shop-only dialogue presenter.

26. How should dialogue conditions be authored?
    - Code predicates
    - ScriptableObject data
    - Localization-key driven
    - Hybrid (please specify)

## 8) Audio
27. What is the single authoritative audio entry point (e.g., `AudioManager`)?

28. Confirm required audio hooks:
    - Shop open
    - Purchase success
    - Insufficient funds
    - Shop exit
    - Revive return

## 9) Architecture / DI
29. For new shop modules, should we follow:
    - Existing singleton/service-locator pattern
    - Explicit dependency wiring/constructor-style composition

30. Where should shop config live?
    - Inspector-tunable serialized fields
    - ScriptableObject config assets
    - Mixed (please define boundary)

## 10) Project constraints
31. Confirm Unity version.

32. Confirm target platforms (PC, mobile, both).

33. Confirm required namespace and folder conventions for new systems.
