# Combat, damage & AI

Reference: `reference/web-prototype/arena.html` (open it in a browser; tap mechs to inspect their decisions).
Prototype convention: +Z forward (see CLAUDE.md for Godot conversion).

## Weapons
| | cannon (right arm) | laser (left arm) |
|---|---|---|
| type | projectile 150 m/s, gravity 4 m/s² | hitscan beam, 0.22 s visual |
| damage | 14 | 9, linear falloff 100→160 m (min 20 %) |
| heat / cooldown | 0.08 / 0.75 s | 0.11 / 1.0 s |
| range | 320 m | 160 m |

- **Convergence:** weapons fire from their muzzles toward the point on the **torso aim ray** at the
  convergence distance (AI sets it to target range). The torso ray uses torso yaw (including sway) and the aim pitch.
  The arms' gait swing is cosmetic and doesn't ruin accuracy.
- Recoil: cannon kicks arm/elbow springs, torso yaw −0.35 and pitch −0.25 (rad/s), trauma +0.3.
- Heat: dissipates `0.055 − 0.015·speedFrac` per second, floor 0.08. Weapons lock at heat ≥ 1.

## Damage model
Hit volumes (spheres, updated each frame): torso r 2.4, each arm r 1.4 (at forearm), legs r 2.0 (below pelvis).
HP: torso 100, arms 45, legs 70. Damage to an already destroyed section transfers to the torso.
Arm destroyed → arm hidden, its weapon offline. Legs destroyed → speed ×0.45. Torso 0 → wreck
(pelvis collapses ~2.5 m over 1.4 s, legs fold via the same IK, smoke and fire emitters).
Hits add trauma and random torso pitch/roll spring kicks proportional to damage.

## AI architecture
AI writes only to `MechInput`. Layers:

### 1. Perception (think tick every 0.35 s, staggered per mech)
- Visual: line of sight from cockpit (y≈8.3) to enemy torso (y≈7) within 300 m, blocked by geometry.
- Radar: position known without LOS inside 130 m (`vis = false`).
- Getting hit reveals the attacker's current position.
- Per-contact memory: position, smoothed velocity estimate, last-seen time, `first` (start of current visibility).
- Contacts older than 15 s are ignored. Dead mechs are dropped.

### 2. Target selection
`score = 0.6·visible − dist/250 + 0.5·(team focus) + 0.4·(1 − targetTorsoHP%) − 0.03·age + 0.15·(current target)`

### 3. Utility actions (+0.12 commitment bonus to the current action)
| action | score | destination |
|---|---|---|
| ENGAGE | `(vis ? 0.55 : 0.25)·aggr + 0.25·hp − 0.6·max(0, heat−0.55)` | orbit point at preferred range, ±0.35 rad around target; orbit side flips every 6–12 s |
| COOL | `heat > 0.5 ? 2.4·(heat−0.5) + 0.15·(recent dmg > 12) : 0` | best cover point hidden from target |
| FLANK | not visible but known (age < 4 s): `max(0.1, 0.3 + 0.07·min(noLos,6) − 0.06·max(0, noLos−9))` | point ±1.4 rad around target at 0.85·range, preferring one with LOS |
| RETREAT | `hp < 0.45 ? 2.6·(0.45−hp) + 0.15 + 0.15·(visible & too close) : 0` | cover hidden from target, far from it, near allies |
| SEARCH | no target 0.6; stale contact (age ≥ 4 s) 0.55 | last known position, else sweep toward the enemy half |

`hp = torso% · (0.7 + 0.15·hasLaserArm + 0.15·hasCannonArm)`. Log every transition with a human-readable reason.

**Cover points:** 8 per block (4 sides + 4 corners, 4.5 m out), filtered for arena bounds and overlap.
Score: `1.2·hidden − dist/50 + (retreat ? distToEnemy/140 : −|distToEnemy − range|/220) + 0.15·(tall block)`,
−1 if within 25 m of the enemy, −0.8 if an ally already claimed it, +0.35·exp(−d/40) per nearby ally when retreating.

### 4. Control (every physics tick)
- **Steering:** probe candidate headings `0, ±0.3, ±0.6 … ±2.4` rad around the destination bearing with 3 parallel rays
  (±2.4 m) up to 16 m. Take the first clear one. Turn = `clamp(2.2·err)`, throttle = `clamp(dist/20, 0.3, 1)·max(0.1, cos err)`.
- **Backing up:** in ENGAGE/RETREAT, if the destination is behind (|err| > 2.1) and < 45 m away, reverse at −0.9 while facing the enemy.
- **Torso aim:** lead = contact position + velocity·(age + range/shellSpeed). Torso yaw target = bearing + aim error,
  pitch includes shell drop `0.5·g·tof²`. If the needed twist exceeds 0.9·TWIST_MAX while standing, turn the legs.
- **Aim error:** σ = `0.006 + 0.014·speedFrac + 0.03·max(0, heat−0.45) + 0.008·targetSpeedFrac` rad, Gaussian, resampled each think.
- **Fire discipline:** contact visible for ≥ reaction time; aim-ray angle < `0.02 + 1.6/range`; range OK;
  `heat + weaponHeat (+ heat of a weapon fired this tick) < heatLimit` (+0.05 when retreating);
  clear line of fire vs geometry **and allies** (segment vs ally sphere r 3.5 → "holding fire").
- **Spread damage (twist-away):** while both weapons recycle (>0.3 s) and the target is visible, add ±0.8 rad twist
  to present the healthier arm. Never during FLANK.

### 5. Squad coordinator (1 Hz)
Team focus = enemy maximizing `1.5·(1 − hp%) + Σ allies(visible ? 1 : known ? 0.3 : 0)`.

### Personalities
| | range | heat limit | aggression | reaction | twist-away |
|---|---|---|---|---|---|
| Brawler | 55 m | 0.92 | 1.25 | 0.25 s | yes |
| Balanced | 100 m | 0.82 | 1.0 | 0.30 s | yes |
| Sniper | 165 m | 0.75 | 0.85 | 0.35 s | no |

## Observed in prototype matches (things to fix/tune)
- Matches last 70–140 s. Brawlers dominate in dense layouts; snipers often lack sight lines.
- Two flankers can circle the same block for 20+ s. Fix: squad-level flank side assignment, or one commits to a push.
- Weapon alignment fails often because twist is rate-limited. That's intended, but watch for "never fires" cases.
- AI doesn't yet use terrain height, doesn't predict enemy cooldowns, and doesn't break off to protect a damaged ally.

## Planned: cockpit warning voice
Priority queue with per-line cooldown, hysteresis on thresholds (e.g. heat warning at 0.8, re-arm below 0.7),
max one pending line, drop lines older than 2 s; higher priority interrupts lower.
Voice lines are original, generated with a designed (not cloned) voice, and processed with
band-pass 300–3400 Hz → saturation → hard compression → short metallic room reverb.
