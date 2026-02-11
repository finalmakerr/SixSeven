# 2D Mobile Game UI Layout Spec

## UI Hierarchy

```text
Canvas (Screen Space - Overlay, Canvas Scaler: Scale With Screen Size)
├── SafeAreaRoot (fits iOS/Android safe area)
│   ├── TopBarRoot (anchor: top stretch)
│   │   ├── HPPanel (anchor: top-left)
│   │   │   ├── HPLabel (optional "HP")
│   │   │   └── HPIconRow (7 reserved heart slots)
│   │   ├── CenterTopPanel (anchor: top-center)
│   │   │   ├── LevelText (e.g. "Level 12")
│   │   │   ├── CoinInlineGroup
│   │   │   │   ├── CoinIcon
│   │   │   │   └── CoinText (e.g. "1280")
│   │   │   └── StarRow (3 reserved star slots under coin group)
│   │   └── EnergyPanel (anchor: top-right)
│   │       ├── EnergyLabel (optional "Energy")
│   │       └── EnergyIconRow (7 reserved lightning slots)
│   └── BottomActionRoot (anchor: bottom-center)
│       └── ActionSlotRow (horizontal layout)
│           ├── ActionSlotButton_1 (large)
│           ├── ActionSlotButton_2 (large)
│           └── ActionSlotButton_3 (large)
└── Optional overlays (pause, settings, damage flash, etc.)
```

## Anchor Positions

- **TopBarRoot**
  - Anchor Min `(0,1)`, Anchor Max `(1,1)`, Pivot `(0.5,1)`
  - Stretch width across safe area, fixed height (about 16–20% of screen height).
- **HPPanel**
  - Anchor Min/Max `(0,1)`, Pivot `(0,1)`, offset from top-left safe margin.
- **CenterTopPanel**
  - Anchor Min/Max `(0.5,1)`, Pivot `(0.5,1)`.
  - `LevelText` centered; coin counter placed inline to the right of level text.
  - `StarRow` placed directly below coin counter.
- **EnergyPanel**
  - Anchor Min/Max `(1,1)`, Pivot `(1,1)`, offset from top-right safe margin.
- **BottomActionRoot**
  - Anchor Min/Max `(0.5,0)`, Pivot `(0.5,0)`.
  - 3 action buttons centered with equal spacing and touch-friendly minimum size.

## Scaling Rules

### Core state

- Initial HP hearts shown: **3** (where each heart = 2 HP).
- Initial Energy icons shown: **3**.
- Max HP slots: grows up to **7**.
- Max Energy slots: grows up to **7**.
- Star slots fixed at **3** max.

### Permanent run growth

- `maxHP` and `maxEnergy` can only increase during a run.
- They do not decrease unless run resets/game over.
- UI always keeps **7 reserved visual slots** in both HP and Energy rows to avoid layout jumping.

### Icon sizing behavior

- Start from a base icon size (for example 48 px).
- Compute required size from available row width and current max slots.
- If needed, shrink slightly as max increases to keep row inside its panel.
- Track the **smallest size ever reached** per row during the run.
- Never increase icon size above that stored minimum once shrunk.
  - This avoids jitter/pop when temporary width changes happen.

### Visibility rules

- HP row: show active heart icons up to `currentHearts`; dim reserved slots up to `maxHP`; hide or ultra-dim slots from `maxHP+1` to 7.
- Energy row: same pattern using lightning icons.
- Star row: 3 reserved stars, fill based on earned stars.

## Pseudocode for Dynamic Resizing

```pseudo
const MAX_CAP = 7
const BASE_ICON_SIZE = 48
const MIN_ABSOLUTE_ICON_SIZE = 26
const ICON_SPACING = 6

state:
  maxHP = 3
  currentHP = 6            // because 3 hearts * 2 hp each
  maxEnergy = 3
  currentEnergy = 3

  minReachedHpIconSize = BASE_ICON_SIZE
  minReachedEnergyIconSize = BASE_ICON_SIZE

function increaseMaxHP(byAmount):
  maxHP = clamp(maxHP + byAmount, 1, MAX_CAP)
  refreshTopUI()

function increaseMaxEnergy(byAmount):
  maxEnergy = clamp(maxEnergy + byAmount, 1, MAX_CAP)
  refreshTopUI()

function onGameOverResetRun():
  maxHP = 3
  maxEnergy = 3
  currentHP = 6
  currentEnergy = 3
  minReachedHpIconSize = BASE_ICON_SIZE
  minReachedEnergyIconSize = BASE_ICON_SIZE
  refreshTopUI()

function refreshTopUI():
  // HP hearts are counted in hearts for display
  currentHearts = ceil(currentHP / 2.0)

  hpIconSize = computeStickyIconSize(
    containerWidth = HPPanel.width,
    slotCount = maxHP,
    spacing = ICON_SPACING,
    baseSize = BASE_ICON_SIZE,
    minAbsolute = MIN_ABSOLUTE_ICON_SIZE,
    minReachedRef = minReachedHpIconSize
  )

  energyIconSize = computeStickyIconSize(
    containerWidth = EnergyPanel.width,
    slotCount = maxEnergy,
    spacing = ICON_SPACING,
    baseSize = BASE_ICON_SIZE,
    minAbsolute = MIN_ABSOLUTE_ICON_SIZE,
    minReachedRef = minReachedEnergyIconSize
  )

  applyRowVisuals(HPRow, currentValue = currentHearts, maxValue = maxHP, totalReserved = MAX_CAP)
  applyRowVisuals(EnergyRow, currentValue = currentEnergy, maxValue = maxEnergy, totalReserved = MAX_CAP)

  HPRow.setIconSize(hpIconSize)
  EnergyRow.setIconSize(energyIconSize)

function computeStickyIconSize(containerWidth, slotCount, spacing, baseSize, minAbsolute, minReachedRef):
  neededWidthAtBase = slotCount * baseSize + (slotCount - 1) * spacing

  if neededWidthAtBase <= containerWidth:
    candidate = baseSize
  else:
    candidate = floor((containerWidth - (slotCount - 1) * spacing) / slotCount)

  candidate = clamp(candidate, minAbsolute, baseSize)

  // Sticky-min rule: once shrunk, never grow larger than smallest reached this run
  minReachedRef = min(minReachedRef, candidate)
  return minReachedRef

function applyRowVisuals(row, currentValue, maxValue, totalReserved):
  for i in 1..totalReserved:
    slot = row.slot[i]
    if i <= currentValue:
      slot.visible = true
      slot.alpha = 1.0          // active
    else if i <= maxValue:
      slot.visible = true
      slot.alpha = 0.35         // unlocked but currently empty
    else:
      slot.visible = true
      slot.alpha = 0.12         // reserved future capacity
```

## Mobile Usability Notes

- Use safe-area padding to avoid notches and rounded corners.
- Keep action slots large (typically ≥ 96 px visual target with generous spacing).
- Prefer `LayoutGroup + ContentSizeFitter` (or equivalent) for resilience across aspect ratios.
- Keep top bar readable in both portrait and landscape by clamping min/max panel widths.
