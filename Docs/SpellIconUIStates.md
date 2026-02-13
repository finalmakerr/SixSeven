# Spell Icon UI Feedback States

This specification defines runtime-only UI feedback for spell icons in the 2D grid magic game.

- Base artwork source for all states: `Assets/Art/overlays/spells/<school>.png`
- No state creates or edits PNG assets.
- All visuals are driven by UI layers, shaders/material parameters, and animation curves at runtime.

## Shared Runtime Setup

- **Base Icon**: `Image` using the school sprite loaded from `Assets/Art/overlays/spells/<school>.png`.
- **State Controller**: a script/state machine sets visual properties based on gameplay state (`Normal`, `HoverSelected`, `Disabled`, `Cooldown`, `Locked`).
- **Optional Child Layers**:
  - `DarkOverlay` (`Image`, black with alpha, raycast off)
  - `CooldownMask` (`Image`, black with alpha, `Type = Filled`, `Fill Method = Radial360`)
  - `LockOverlay` (`Image` lock glyph/sprite, anchored center)
  - `CooldownText` (TMP text centered above icon)
- **Material Controls** (if shader-based): `_Saturation`, `_Brightness`, `_GlowStrength`, `_OutlineWidth`, `_OutlineColor`.

## UI State Definitions

### 1) Normal (default)

- **Scale**: `1.00`
- **Tint/Color**: `RGBA(1,1,1,1)`
- **Saturation**: `1.00` (no desaturation)
- **Brightness**: `1.00` (no boost or dim)
- **Glow/Outline**: Off (`0` strength or disabled component)
- **DarkOverlay**: hidden (`alpha = 0`)
- **CooldownMask**: hidden (`alpha = 0`, fill irrelevant)
- **LockOverlay**: hidden
- **Animation**: none

### 2) Hover / Selected

- **Scale**: animate from `1.00 -> 1.08` over ~`0.08s` (ease-out)
- **Tint/Color**: keep white tint unless style guide adds school accent
- **Saturation**: `1.00`
- **Brightness**: `1.10` (+10%)
- **Glow/Outline**:
  - enable subtle outline/glow
  - recommended: width `1-2 px`, glow strength `0.15-0.25`, low alpha
- **DarkOverlay**: hidden
- **CooldownMask**: hidden unless cooldown is active (cooldown visuals can stack)
- **LockOverlay**: hidden
- **Animation**:
  - optional slow pulse while hovered (e.g., scale oscillates `1.06 <-> 1.08` at `0.8-1.2 Hz`)
  - optional glow intensity pulse (`±10%`)

### 3) Disabled (no mana)

- **Scale**: `1.00`
- **Tint/Color**: neutral white tint
- **Saturation**: `0.20` (80% desaturated, near grayscale)
- **Brightness**: `0.70` (-30%)
- **Glow/Outline**: Off
- **DarkOverlay**:
  - visible over base icon
  - color `RGBA(0,0,0,0.35-0.50)`
- **CooldownMask**: hidden unless cooldown is also active
- **LockOverlay**: hidden
- **Animation**: none (static communicates unavailable)

### 4) Cooldown (active cooldown)

- **Scale**: typically `1.00` (or keep hover scale if selected)
- **Base Color/Saturation/Brightness**: inherit from availability state (normal/disabled)
- **CooldownMask**:
  - visible
  - `Image Type = Filled`
  - `Fill Method = Radial 360`
  - `Fill Origin` and `Clockwise` set by UX preference
  - mask color `RGBA(0,0,0,0.45-0.60)`
  - animate `fillAmount` from `1.0 -> 0.0` as `remainingCooldown / totalCooldown`
- **DarkOverlay**: optional separate layer not required if mask already provides dimming
- **CooldownText** (optional):
  - centered over icon
  - display remaining seconds (`ceil(remainingCooldown)`)
  - fade out when cooldown reaches zero
- **Glow/Outline**: optional off during cooldown, or subtle on when cooldown nearly complete
- **Animation**:
  - radial fill update each frame or at fixed UI tick
  - optional "ready" pop (short scale bump) at cooldown completion

### 5) Locked (spell locked)

- **Base visuals**: use the full **Disabled** treatment
  - saturation `0.20`
  - brightness `0.70`
  - dark overlay visible
- **LockOverlay**:
  - visible child UI element on top of icon
  - small centered lock glyph/sprite
  - suggested alpha `0.85-1.0`
- **CooldownMask**: hidden unless design explicitly supports pre-locked cooldown display
- **Animation**: none by default; optional tiny lock bounce when user attempts to click

## State Blending/Precedence

Recommended precedence when multiple flags exist:

1. `Locked`
2. `Cooldown`
3. `Disabled`
4. `HoverSelected`
5. `Normal`

Notes:
- `Locked` should always show lock overlay even if hovered.
- `Cooldown` mask can be layered over `Normal` or `Disabled` base treatment.
- Hover effects should not override locked/disabled readability.

## Runtime-Driven Property Summary

- **Transform**: local scale
- **Image Color**: alpha/tint
- **Material Params**: saturation, brightness, glow/outline
- **Overlay Alpha**: dark layer intensity
- **Radial Fill**: cooldown progress mask
- **Text Content/Visibility**: remaining cooldown
- **Animation Curves**: hover pulse, transition easing, cooldown completion pop

All state feedback is achieved at runtime through UI components, shaders, and animations while reusing the same base spell PNG per school.
