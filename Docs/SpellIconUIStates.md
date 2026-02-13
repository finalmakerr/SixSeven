# Spell Icon UI Behavior Specification

This document is the implementation-ready runtime UI spec for spell icon feedback states in the 2D grid magic game.

- Base artwork for all spell icons: `Assets/Art/overlays/spells/<school>.png`
- No state requires creating or editing PNG assets.
- All visuals are driven at runtime with Unity UI layers, shader/material parameters, and animation curves.

## 1) UI Architecture

Each spell icon is a root `UnityEngine.UI.Image` with optional child layers:

- `SpellIconImage` (root `Image`)
- `DarkOverlay` (child `Image`, black, alpha controlled)
- `CooldownMask` (child `Image`, black, `Image.Type = Filled`, `FillMethod = Radial360`)
- `LockOverlay` (child `Image`, lock glyph/stamp)
- `Tooltip / Info Overlay` (child object, optional)
- `CooldownText` (optional TMP child for numeric countdown)

### Material Parameters (runtime-driven)

- `_Saturation` (float)
- `_Brightness` (float)
- `_GlowStrength` (float)
- `_OutlineWidth` (float)
- `_OutlineColor` (Color)

## 2) Canonical State Values

### 2.1 Normal

- Scale: `1.00`
- Tint (`Image.color`): `RGBA(1,1,1,1)`
- `_Saturation`: `1.00`
- `_Brightness`: `1.00`
- `_GlowStrength`: `0.00`
- `_OutlineWidth`: `0.00`
- `_OutlineColor`: transparent/ignored
- `DarkOverlay.alpha`: `0.00` (hidden)
- `CooldownMask`: hidden (`alpha = 0`, `fillAmount` unchanged)
- `LockOverlay`: hidden
- Animation: none

### 2.2 Hover / Selected

- Enter transition scale: `1.00 -> 1.08` over `0.08s` with ease-out
- Exit transition scale: `current -> 1.00` over `0.06s` with ease-in
- Tint: white (optional school accent only via glow/outline)
- `_Saturation`: `1.00`
- `_Brightness`: `1.10`
- `_GlowStrength`: `0.18` (subtle)
- `_OutlineWidth`: `1.5` px target range `1-2` px
- `_OutlineColor`: school accent with low alpha (e.g. `a=0.6`)
- `DarkOverlay`: hidden
- `CooldownMask`: remains visible if cooldown is active
- `LockOverlay`: hidden
- Optional idle hover pulse while hovered:
  - Scale oscillation: `1.06 <-> 1.08`
  - Frequency: `1.0 Hz`
  - Curve: sine/ease-in-out
  - Keep subtle; do not exceed `1.08`

### 2.3 Disabled / Unavailable

- Scale: `1.00`
- Tint: unchanged white (`RGBA(1,1,1,1)`) so grayscale comes from material
- `_Saturation`: `0.20`
- `_Brightness`: `0.70`
- `_GlowStrength`: `0.00`
- `_OutlineWidth`: `0.00`
- `DarkOverlay`: visible, `RGBA(0,0,0,0.42)` (allowed range `0.35-0.50`)
- `CooldownMask`: hidden unless also in cooldown
- `LockOverlay`: hidden
- Animation: none

### 2.4 Cooldown

- Scale: `1.00` (or hover scale if hover is allowed by precedence)
- Base saturation/brightness: inherited from Normal or Disabled base
- `CooldownMask`: visible
  - `Image.Type = Filled`
  - `FillMethod = Radial360`
  - `fillAmount = remainingCooldown / totalCooldown`
  - Color: `RGBA(0,0,0,0.52)` (allowed range `0.45-0.60`)
- `DarkOverlay`: optional additional dimming if readability requires
- `CooldownText` (optional):
  - content: `ceil(remainingCooldown)`
  - hide at `remainingCooldown <= 0`
- Glow/outline: off during most cooldown
- Animation:
  - update `fillAmount` each UI tick/frame

### 2.4B Cooldown Completion Ready Pop

Triggered once when cooldown reaches zero.

- Scale pop: `1.00 -> 1.12 -> 1.00` over `0.15s` total
  - up phase: `0.05s` ease-out
  - down phase: `0.10s` ease-in
- `_Brightness` spike: `+0.20` additive for `0.08s`, then return
- `_GlowStrength` flash: `0.35` peak, decay to `0` by end
- Optional audio: ready cue

### 2.5 Locked

- Base visuals: same as Disabled
  - `_Saturation = 0.20`
  - `_Brightness = 0.70`
  - `DarkOverlay.alpha ~= 0.42`
- `LockOverlay`: visible, centered, alpha `0.9-1.0`
- `CooldownMask`: hidden
- Hover glow/scale: suppressed for readability
- Animation: none (except click rejection feedback)

### 2.6 Insufficient Energy Click Feedback

Triggered when user clicks while Disabled or Locked.

- One-shot rejection feedback (non-stateful, overlay animation on top of current state):
  - Tint flash: `RGBA(1.0,0.3,0.3,1.0)` then return in `0.15s`
  - Shake: horizontal jitter `±2px` for `0.08s`
  - Optional audio: blocked/reject SFX
- Post-feedback: restore original Disabled or Locked visuals exactly
- Must not modify cooldown timing/state

## 3) Animation Curves and Timings

Recommended reusable curves:

- `HoverEnterScaleCurve`: ease-out cubic, duration `0.08s`
- `HoverExitScaleCurve`: ease-in cubic, duration `0.06s`
- `HoverPulseCurve`: sine wave, period `1.0s`
- `ReadyPopUpCurve`: ease-out back-like (small overshoot feel), `0.05s`
- `ReadyPopDownCurve`: ease-in quad, `0.10s`
- `InsufficientTintCurve`: fast rise (`0.03s`) + smooth decay (`0.12s`)
- `InsufficientShakeCurve`: damped oscillation at ~`25 Hz` for `0.08s`

## 4) State Precedence and Conflict Rules

Apply visual state in this strict order:

1. `Locked`
2. `Cooldown`
3. `Disabled / Unavailable`
4. `Hover / Selected`
5. `Normal`

Conflict handling:

- Locked overrides everything (always show lock, suppress hover glamor).
- Cooldown overlays on top of Normal/Disabled base values.
- Disabled suppresses glow/outline and keeps readability over hover.
- Hover can affect scale/brightness only when not Locked; if Disabled+Hover, allow at most subtle scale (`<=1.03`) or suppress entirely per UX readability target.
- Insufficient Energy feedback is a temporary effect layer; after completion, recompute from precedence stack.

## 5) Runtime Parameter Summary (Implementation Checklist)

Update these runtime properties only:

- `Transform.localScale`
- `Image.color` (tint/flash)
- Material `_Saturation`
- Material `_Brightness`
- Material `_GlowStrength`
- Material `_OutlineWidth`
- Material `_OutlineColor`
- `DarkOverlay.color.a`
- `CooldownMask.color.a`
- `CooldownMask.fillAmount`
- `LockOverlay.enabled` / alpha
- Optional status text (`CooldownText` / Tooltip)

## 6) C# State Application Order (Reference)

1. Resolve logical flags: `isLocked`, `isOnCooldown`, `isAvailable`, `isHovered`, `wasRejectedClick`.
2. Choose base state by precedence.
3. Apply base visual constants (scale, material, overlays).
4. Apply cooldown overlay if active.
5. Apply hover transition only if allowed by resolved state.
6. Play transient clips (`ready pop`, `insufficient energy`) as additive one-shots.
7. On transient end, re-apply resolved state snapshot.

End specification.
