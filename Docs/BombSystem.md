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
