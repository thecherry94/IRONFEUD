# Locomotion: procedural heavy-mech walking

Reference implementation: `reference/web-prototype/drivable.html` (single mech, player-driven) and
`arena.html` (same model, per-mech state). Formulas below use the **prototype convention**
(+Z forward, +X = mech's left, forward = (sin ψ, 0, cos ψ)). Convert for Godot (see CLAUDE.md).

## Layers
```
MechInput (throttle, turn, twistTarget, aimTarget, fire…)
  → body kinematics (speed, heading, torso twist, aim)       rate-limited, inertial
  → gait phase + planted foot targets (world space)          no foot sliding, ever
  → 2-bone analytic IK per leg + world-space foot orientation
  → secondary motion springs (+ footfall/recoil/hit impulses)
  → "jank" high-frequency shake from trauma
```

## Body kinematics (per fixed step h = 1/120)
| constant | value | meaning |
|---|---|---|
| VMAX / VREV | 4.2 / 1.8 m/s | forward / reverse top speed (legs destroyed: ×0.45) |
| ACC / DEC | 1.5 / 2.8 m/s² | accel toward |target| / braking |
| OMAX | 0.6 rad/s | leg turn rate, reduced by up to 35 % at full speed |
| turn accel | 1.2 rad/s² | ω slews toward target |
| TWIST_MAX / TWIST_RATE | 1.75 rad / 1.3 rad/s | torso twist limit and slew rate |
| AIM_MIN / AIM_MAX / rate | −0.35 / 0.45 rad / 0.8 rad/s | torso pitch |

Throttle is set-and-hold (−1..1). `v` slews toward `throttle·VMAX` (or `·VREV`).

## Gait
| constant | value |
|---|---|
| DUTY (stance fraction) | 0.62 |
| DMAX (max distance per full cycle) | 6.2 m |
| FMIN (min cycle freq while active) | 0.36 Hz |
| AMAX (max half-stride) | 1.95 m |
| leg lengths L1 / L2 | 3.2 / 3.3 m, hip width 1.6 m, ankle height 0.62 m, hip height H0 5.75 m |

- Frequency `f = max(FMIN, |v| / DMAX)`; stride per cycle `D = |v| / f`; half-stride `A = clamp(v/f · DUTY/2, ±AMAX)` (signed).
- Legs offset by 0.5 in phase. A leg is in **stance** for `lp < DUTY`, **swing** otherwise.
- **Stance:** the foot stays at its stored world `plant` (x, z, yaw). Nothing else.
- **Swing start** (crossing DUTY): `from = plant`.
- **Swing:** `s = (lp − DUTY)/(1 − DUTY)`, remaining time `Tr = (1−s)(1−DUTY)/f`.
  Landing target is *predicted*: body pos + v·Tr along heading, heading + ω·Tr, plus rotated local offset
  `(side·HIPW, A)`. Foot xz = `lerp(from, landing, smootherstep(s))`, yaw lerped the same way.
  Lift `= 0.95 · clamp(stepDist/1.6, 0.35, 1) · sin(π · s^0.75)` (fast lift, hang, hard drop).
  Small outward arc `0.22 · sin(πs)` scaled by step length.
- **Swing end** (phase wraps): `plant = landing(Tr=0)`, then the **footfall** event fires.
- **Activity gating / settling:** the phase only advances while `|v| > 0.12 || |ω| > 0.04 || any foot in swing ||
  a planted foot is > 0.45 m or 0.2 rad off its neutral spot`. At v = 0 the landing target becomes neutral,
  so the mech takes small settling steps and then stands still. Gait amplitude `g` (0..1) eases with activity
  and scales the sway/bob terms so an idle mech doesn't sway.

## Foot pitch (heel-strike / toe-off) and keeping contact
- Stance: `pitch = 0.38·smoothstep(DUTY−0.16, DUTY, lp) − 0.14·(1 − smoothstep(0, 0.07, lp))`
- Swing: `pitch = lerp(0.38, −0.14, smoothstep(0, 0.75, s))`
- Scale pitch by `clamp(stepLen/3, 0, 1)` (shuffle steps stay flat) and ×0.3 when reversing.
- Keep the pivot on the ground by raising the ankle: toe-down pivots on the toe `rise = 1.7·sin(pitch)`;
  toe-up pivots on the heel `rise = 1.05·sin(−pitch)`.

## 2-bone IK (per leg, in the hip-root frame, which is unrotated relative to the pelvis)
```
d      = ankleTarget expressed in hipRoot local space
roll   = atan2(d.x, −d.y)                 // hip roll, applied OUTSIDE the pitch (Euler order ZXY)
r      = hypot(d.x, d.y)
L      = clamp(hypot(r, d.z), ε, L1+L2−ε)
knee   = π − acos((L1²+L2²−L²)/(2·L1·L2))  // flexion, positive = shin swings back
alpha  = atan2(d.z, r)
beta   = acos((L1²+L²−L2²)/(2·L1·L))
hip.pitch = −(alpha + beta)               // forward-pointing knee
```
Foot orientation is set in **world space**: `ankle.localRot = inverse(knee.worldRot) · Rot(yaw, pitch)`.
The foot then stays flat and keeps its planted yaw while the body turns over it.
Hydraulic pistons are world-space segments between a point on the thigh and a point on the shin.

## Pelvis & torso motion
- Pelvis lateral sway toward the stance leg: `0.32·g·cos(2π(p − 0.31))` (rotated by heading).
- Pelvis bob, lowest just after each footfall: `−0.17·g·cos(2π(2p − 0.1))`.
- Pelvis yaw counter-swing `−0.07·g·cos(2πp)`; small roll.
- Springs (`Spring(freqHz, zeta)`, semi-implicit Euler at 120 Hz):

| spring | freq / ζ | target |
|---|---|---|
| bounceY (pelvis) | 3.0 / 0.22 | 0 |
| torso yaw | 1.4 / 0.35 | `0.13·g·cos(2πp)` (counter-rotates against hips) |
| torso roll | 1.5 / 0.30 | `−0.05·g·cos(2π(p−0.31)) + 0.18·ω·speedFrac` (lean out of turns) |
| torso pitch | 1.7 / 0.28 | `0.05 + 0.02·g·cos(2π(2p−0.1)) − 0.06·accel` (lags under acceleration) |
| arms (shoulder pitch) | 1.1 / 0.25 | `∓0.14·g·cos(2πp)` opposite to legs, plus −aim |
| elbows | 2.3 / 0.18 | 0 |

## Footfall impulse (the "weight")
`k = impact · (0.45 + 0.55·min(1, stepLen/3))`:
bounceY.v −= 2.4k; torsoPitch.v += 0.55k; torsoRoll.v −= side·0.35k; arms.v += 0.8k; elbows.v += 1.6k·rand(0.7..1.3);
`trauma += 0.55k`; dust + footprint decal.

## Jank shake
`trauma` decays 1.6/s. `jank = trauma² · (0.6·sin(53.1t) + 0.4·sin(97.3t + 1.7))`.
Applied to pelvis y (×0.09), torso pitch/roll (×0.01/0.015), head, and chase/cockpit camera.
Tunable "Jank" multiplier. Recoil and hits also add trauma.

## Known gaps to solve in Godot
- Flat ground only. Next: raycast landing targets onto terrain, and set the pelvis height from the
  average planted-foot height plus slope pitch/roll.
- Mid-stride speed changes can shift the predicted landing by a few cm. Fine so far.
- Mech-vs-geometry collision in the prototype is AABB push-out; use `CharacterBody3D` or a custom capsule sweep.

## Chassis classes and running (Godot addition)
All constants above are the web prototype's walker, kept as `MechChassis.Prototype()` (field defaults) and pinned by
tests. The game's classes run at genre speeds:

| class | top speed | scale | notes |
|---|---|---|---|
| Light | 36 m/s (130 km/h) | 0.72, legs +10 % | quick turns/twist, thin armour |
| Medium | 22 m/s (80 km/h) | 1.0 | the prototype's body |
| Heavy | 16.5 m/s (60 km/h) | 1.15 | |
| Assault | 11 m/s (40 km/h) | 1.3 | only half-breaks into a run |

**Walk → run:** duty blends from `Duty` (0.62) to `DutyRun` (0.32, a flight phase: both feet up between steps) between
`RunStartSpeed` and `RunFullSpeed`, set from the hip height h by the Froude number v²/(g·h) = 0.5 … 2.5
(where animals switch gaits). Each leg **latches its duty at landing**, so a speed change mid-step never skips or
truncates a swing.

**Cadence:** `f = max(FMIN, (1 − run)·|v|/DMAX, |v|·duty_stance/(2·AMAX))`, `run` = 0..1 walk-to-run blend.
- The reach term caps stance travel at 2·AMAX, so the leg never over-extends; `duty_stance` is the largest duty
  latched by a leg currently in stance (it is higher than the current duty while accelerating).
- The walking cap DMAX fades out with the run. Left on, it held the light's stride to 4.5 m and made it shuffle at
  16 steps/s ("walk of shame").
- At top speed: light 2.7 Hz (5.4 steps/s), medium 1.35 Hz, heavy 0.9 Hz, assault 0.65 Hz.

**Running posture** (all scaled by `run`): hips 0.25 m·scale higher; bob amplitude ×1.8 with its low point moved from
just after footfall to mid-stance (compression), high in the flight phase; pelvis pitched 0.1 rad forward; lateral
sway and pelvis counter-yaw/roll reduced to 25 % (side-to-side sway at a running cadence reads as a waddle);
swing lift rises to 0.28·(L1+L2) with an earlier peak (`sin(π·s^0.55)`) so the recovering foot comes up under the hip.
**Footfall impulses** are scaled by `min(1, 0.7 Hz / f)` so fast cadences don't saturate the springs and trauma.
`--parade` (F11) runs all four classes on a concentric track through walk → run → walk with live duty/cadence.

`MechChassis.Scale` multiplies the gait's built-in lengths (lift, outward arc, foot pivot lengths, settle threshold,
pelvis sway/bob, footfall bounce); the rig builds prototype-size meshes and scales the pelvis node.
Not yet: spring frequencies don't scale with size, the pelvis doesn't drop or bounce for a running flight phase,
and heavier classes are scaled copies of the same body.

## Leg layouts and models (Godot addition)
`MechChassis.Create(class, model)`: the **model** is the body plan and look, the **class** sets size, speed, armour.
- **Hauberk** (the prototype): upright torso, hanging arms, plantigrade legs (thigh, shin, flat foot, knee forward).
- **Sicklefoot**: chicken walker. Long low hull slung forward over a turret ring, cockpit pod under the nose, weapon
  pods on short pylons (the "arms": they pitch with aim and barely swing), dorsal radiator spine, rear counterweight.
  **Digitigrade legs**: thigh L1 2.3, shin L2 3.0, raised foot bone L3 1.9 (medium), walking on the toe joint.

**Digitigrade IK** (`LegIK.SolveDigitigrade`): the foot bone is held parallel to the thigh (a pantograph leg, like
Agility's Cassie/Digit or a bird). Then hip → toe = (L1 + L3)·thigh + L2·shin, an ordinary 2-bone problem with a
virtual thigh of L1 + L3: solve it with the forward-knee solution and set the hock to −knee. Exact, closed-form,
no jitter. Knee points forward, hock backward. The gait is unchanged: its "ankle" target is the toe joint
(`AnkleHeight` 0.35 m), foot pivots come from `ToeLength`/`HeelLength`.

`FitGaitToLegs` derives each mech's hip height (from `StanceExtension`: 0.766 Hauberk, 0.72 Sicklefoot), stride limit
AMAX (70 % of the horizontal reach at 95 % extension) and walk-to-run speeds from its leg geometry, so new models
only describe their bodies. A plain reverse knee (2 segments, knee back) would be the other root of the 2-bone solve
(hip pitch α − β); not used yet.
