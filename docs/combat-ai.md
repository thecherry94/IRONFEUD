# Combat, damage & AI

Reference: `reference/web-prototype/arena.html` (open it in a browser; tap mechs to inspect their decisions).
Prototype convention: +Z forward (see CLAUDE.md for Godot conversion).

## Rescaled for genre speeds (Godot port)
The prototype's mechs walked at 4.2 m/s; ours run at 11-36 m/s (see docs/locomotion.md). Everything spatial was
rescaled, damage/heat/cooldowns were not. Numbers below in **bold** are the current values; the rest of this doc
describes the prototype.
- Cannon: **450 m/s**, gravity 4 m/s² (real gravity put the laser, which shares the drop-compensated aim ray, on the
  cockpit too often: 5 of 12 duels ended in pilot kills), range **640 m**, lifetime 2 s. Laser: range **320 m**,
  falloff **200→320 m**.
- Personality ranges ×2: Brawler **110**, Balanced **200**, Sniper **330** m. Sensor **600** m, radar **260** m.
- Arena: **240 m** ring, **~70** blocks of 8-26 m footprint, 6-34 m tall, 14 m gaps, spawns at z = **±190**.
  Fewer blocks than a straight area scale-up keeps the ratio of engagement range to mean free sight line close to
  the prototype's; with 90 tall blocks the AIs saw their target only ~17 % of the time.
- AI: cover search radius 200 m, distance weights halved; aim error counts target speed against **20 m/s**;
  steering probes look **1.2 s** ahead at current speed; arrival speed is capped by the chassis' braking distance;
  backing up applies inside 90 m. With a weapon ready, the target visible and more than 85 % of max twist needed,
  the AI swings its legs toward the target (fast mechs running across the bearing otherwise never got a firing angle;
  one light-vs-medium duel went 400 s without a winner).
- Headless duels (12 matchups): 13-116 s, mostly 70-115 s; 2 in 12 end with a cockpit kill. Lights beat mediums in
  3 of 4 (they are hard to hit at 36 m/s).

## Terrain tactics (Godot port, not in the prototype)
- **Partial sight:** a contact is visible if the torso centre, the top of the torso or the cockpit is in line of
  sight; the AI aims at the lowest visible one. Hull-down mechs are seen and shot at by what shows.
- **Firing positions (ENGAGE on terrain):** 49 candidates (7 rings at 0.75-1.25x preferred range x 7 bearings within
  ±0.6 rad of the orbit bearing) plus the current spot. Score: lowest working muzzle has line of fire to the target
  (+1, else -0.5), legs hidden from the target's cockpit (+0.6), torso hidden too while able to shoot (+0.25),
  -|range error|/450, -travel/300, +0.015/m of height over the target *only when hull-down*, +0.1 near the current
  destination (no dithering). The fine rings matter: on a low ridge the only hull-down band is the reverse slope just
  behind the crest. Flat maps keep the prototype's orbit point.
- **Terrain cover:** `ArenaMap` adds cover points on a 12 m grid wherever the ground rises 5 m within 22 m (behind
  berms and mounds, in craters and trenches); COOL/RETREAT pick among them with the usual hidden-from-target check.
- **Steering** refuses climbs steeper than 0.8 averaged over 8 m (trench walls pass, cliffs don't).
- Headless No Man's Land duels: AIs spend ~75 % of their engaging time hull-down; 23-140 s matches.
  A light Sicklefoot beats a heavy outright (the heavy hit 0-1 of ~40 shots at a 130 km/h target): balance item.

## Balance (headless reports)
`IRONFEUD_BALANCE=1 dotnet test tests/ --filter BalanceTests --logger "console;verbosity=detailed"` (≈ 3 min): medium
duels over 20 seeds x both spawn sides per matchup, on No Man's Land and the hills arena; fails if anything wins > 70 %.
- **Spawn side:** no bias (40 seeds per map: 17-23, 21-19, 18-22).
- **Personalities** (Hauberk vs Hauberk, 40 games each): Brawler vs Balanced 19-21 / 18-21, Brawler vs Sniper
  18-20 / 13-26, Balanced vs Sniper 18-21 / 17-22 (No Man's Land / hills). Brawler is not overpowered; Sniper is a
  little strong on the open hills.
- **Models:** the Sicklefoot won ~80 % against an equal Hauberk: smaller, lower hit spheres (its hull hides behind
  crests; the Hauberk's tall torso shows), so it was hit 45-50 % vs 28-35 %. Rather than nerf it, the Hauberk carries
  1.3x armour on torso/arms/legs (cockpit unchanged): now 18-22, 18-21, and 24-16 when brawling up close.

## Class roles (Godot port)
Classes are meant to complement each other, not to be equal. Loadouts step up: light no heavy gun (pulse laser +
autocannon), medium one (heavy cannon + laser), heavy two (heavy cannon + heavy laser), assault the obliterator
(siege cannon + heavy laser). Heat sinks grow with the guns (dissipation .06 / .055 / .065 / .075 per second).
`AiRole` comes from the class:
- **Skirmisher (light):** never parks: runs for a point 0.9 rad further round the target at 0.9x range (re-picked each
  think), reverses the circle every 5-8 s, weaves ±0.35 rad, carves turns at ≥ 50 % throttle, never backs up, and
  doesn't swing its legs toward the target. Rough ground slows lights half as much as others.
- **Flex (medium):** fights from firing positions but relocates (≥ 30 m) when hit or after 8-12 s on one spot.
- **Line (heavy) / Anchor (assault):** hold their hull-down firing positions (stronger stay bonus).

**Evasion and fire discipline** (all AI):
- Aim error grows with the target's crossing rate (angular speed across the line of sight, x0.12 s); a jinking
  target also spoils the lead estimate. Personalities scale aim error: Sniper 0.6, Balanced 0.9, Brawler 1.1.
- Fire when the reticle is on the target: tolerance 0.004 rad + target radius / distance (the prototype's 0.02 + 1.6/d
  was wider than a light's torso). Heavy guns (≥ 30 dmg): 0.003 + 0.8·radius/d, and only if the target crosses
  slower than 0.05 rad/s or is within 90 m. Lighter weapons leave heat for a (nearly) loaded heavy gun.
- Test numbers (4 seeds x 40 s, light at 160 m): a medium hits a standing light 45/61, a circling one 4/53; an assault
  lands 28/39 on a standing light and 1/41 on a running one.

**Lance report** (`IRONFEUD_BALANCE=1 ... --filter Lances_ShowTheClassRoles`, 20 lance battles, both maps):

| class | dealt/game | taken/game | kills/game | hit % | survived | fast | moving | hull-down |
|---|---|---|---|---|---|---|---|---|
| Light | 141 | 94 | 0.68 | 61 | 40 % | 45 % | 88 % | 62 % |
| Medium | 136 | 137 | 0.65 | 45 | 28 % | 11 % | 33 % | 75 % |
| Heavy | 153 | 180 | 0.55 | 51 | 20 % | 13 % | 36 % | 78 % |
| Assault | 192 | 209 | 0.88 | 36 | 38 % | 14 % | 37 % | 81 % |

## Weapon roster and class loadouts (Godot port)
Right arm = fire bit 0, left arm = bit 1. Projectiles do full damage at any range; beams fall off past the start.

| weapon | kind | dmg | heat | cooldown | range | shell m/s | used by |
|---|---|---|---|---|---|---|---|
| Pulse laser | 3 pulses, 0.09 s apart | 9 (3 × 3) | .09 | 0.9 s | 280 (falloff 180) | | light (L) |
| Autocannon | 2 barrels | 4 | .03 | 0.25 s | 520 | 560 | light (R) |
| Laser | 1.25 s burn | 20 | .24 | 2.25 s | 320 (falloff 200) | | medium (L) |
| Cannon | shell | 14 | .08 | 0.75 s | 640 | 450 | (prototype) |
| Heavy laser | 2 s burn | 32 | .35 | 3.5 s | 380 (falloff 240) | | heavy, assault (L) |
| Heavy cannon | shell | 32 | .20 | 2.2 s | 600 | 330 | medium, heavy (R) |
| Siege cannon | shell | 75 | .36 | 4.5 s | 700 | 240 | assault (R) |

**Lasers burn** (a deviation from the prototype's instant beams): the beam stays on for its burn time and every tick
applies its share of the damage along wherever the arm points *then*, so holding it on target is the skill and a
dodging light takes a fraction smeared across sections. Cooldown counts from the trigger; damage and heat per cycle
were scaled up so damage and heat per second held on target match the old instant lasers (laser 9 → 8.9/s, heavy
9.2 → 9.1/s, pulse 10/s). A burn counts as one shot and at most one hit. Pulse lasers fire a burst of instant pulses
with the damage up front. Beam lasers (on while the trigger is held) fit the same model later.
Lance report after the change (20 games): light 129 dealt / 88 taken / 0.90 kills, medium 139 / 151 / 0.55,
heavy 149 / 188 / 0.53, assault 201 / 191 / 1.00: roles unchanged within noise.

**Stabilised weapons.** The aim ray is the pilot's commanded line plus 15 % (`AimStabilization` 0.85) of the torso's
actual motion; the torso's walking yaw swing also fades into the run (at a running cadence it drove its 1.4 Hz
spring at resonance). Aim wobble at 200 m: 8-27 m rms before (a running medium swung 7.6°), 0.7-1.8 m after.
Player feedback: aiming was "hard as fuck and punishing". It made every gun more accurate on the move (siege cannon
vs a standing light 32/38 -> 62/77 hits, running light still evasive: medium 7/46).

**Facing armour.** Torso hits from behind take `RearDamageMultiplier` (1.75), blending from x1 at ~80° off dead
astern to full inside ~50° (`HitFacing`). Facing is the torso's aim direction, so twisting toward a threat protects
the back. Lights are the flankers: a skirmisher heads the short way round toward its target's rear (`Contact.Facing`,
which a pilot can see), on a spiral rather than a radial charge, with side hysteresis when dead ahead (the short way
flips sides there). While the target faces it, it closes to its tightest flat-out circle (1.25x its turning radius)
if that out-turns the target's legs by 30 %; lights got more agile at speed for this (turn penalty 0.55 -> 0.3,
1.26 rad/s flat out). That works against assaults (0.6 rad/s legs) but not heavies or mediums, which it waits on at
range. Circling skirmishers don't arrival-brake and weave less when close. Mediums' flank moves prefer the side
nearer the target's rear. Measured: lights alone vs an assault 6 % of damage from behind, with a medium alongside
30 % (was ~0 %); lance games: lights 181 dealt/game (was 125), heavies take 24 % of their damage from behind.
Known gap: a retreating light parks in cover for ~20 s instead of staying mobile.

**Escorts.** The squad coordinator gives each medium a ward (an assault first, else a heavy; one escort each).
Escorts are on call: they fight freely (sweeping forward, focus-firing) until an enemy threatens the ward (within
100 m, weighted up in its rear arc and for lights), then intercept 30 m out from the ward, ~45° off the line to the
threat so the ward keeps its own line of fire. History: a leashed escort that stood between ward and threat did well
while guns wobbled (lights lost 2 -> 8 of 12), but once weapons were stabilised it blocked the ward's shots and kept
the medium from finding lights early, and turned harmful (7 -> 2 of 24). On call: 7 -> 9 of 24, damage on lights +35 %.

**Jump tactics** (`AiPilot.Jumps.cs`, all through `MechInput.Jump`; `BattleSim.JumpTactics` switches them per battle
for measurement, `JumpTactics_WhatEachIsWorth` reports them):
- *Pop-up* (mediums): hidden from a target seen in the last 8 s, a gun ready, heat to spare: burn just long enough for
  the lowest muzzle to clear the crest (+1.5 m), hold position, check the target every tick while up (the pilot knows
  where to look: no reaction delay), fire, drop back. Firing positions also value spots fully hidden that a pop clears.
  Medium vs heavy on No Man's Land: damage 1714 -> 2154, mediums lost 10 -> 8 of 12, heavies killed 2 -> 4.
- *Vault* (lights, only alone on the target): head-on to a heavy or assault with a full tank and a run-up clear of
  blocks, run straight at where it will be and take off once a full burn clears its torso top + 3 m there; in the air
  yaw round to land facing it. Lone light vs assault: lights lost 9 -> 7, kills 3 -> 4, rear share 3 -> 10 %. With a
  teammate on the target it cost lights and kills (plain flanking already reaches the back), hence alone only.
  Getting it right took: a bigger light tank (apex 17 -> 23 m to clear an assault), leading the target (it moves
  20-30 m in the flight), a 0.08 rad take-off alignment, and flight momentum separate from the body's yaw.
- *Traverse*: a wall (no walkable heading within ±35°) inside jet reach is jumped. Jumping every steep slope instead
  of a short detour cost lights 2x the damage taken; walls only, it is neutral in the arena (helps on bigger walls).
- *Dodge*: a heavy shell on course for the torso 0.5-1.5 s out (visible tracer) triggers a 0.6 s hop. Light + medium
  vs assault: lights lost 1 -> 0, assault killed 11 -> 12, the light takes 25 % less.
All together, lone light vs assault: lights lost 9 -> 6, kills 3 -> 5; light + medium: 1 -> 0, 11 -> 12. Lance report
after: light 171 dealt / 38 % survived, medium 140 / 28 %, heavy 132 / 25 %, assault 142 / 35 %. Escorts are about
neutral now (`Escorts_WhatTheyAreWorth`); revisit with the heavies' role.

**Reactions and support** (player feedback: the AI "doesn't react well to lights flanking from behind").
A hit from behind gives the pilot its bearing (damage display): the attacker becomes a contact, gets top target
priority for 6 s, and enemies inside the rear arc within 200 m rank higher too. Non-skirmishers pivot to face a rear
attacker or anything within 120 m past ~70 % of their twist, guns ready or not, easing to 30 % throttle so the legs
turn faster. Lights prefer heavies and assaults as targets unless an enemy light or medium is within 110 m of them
(then that pursuer first). Any pilot in great danger (a quarter of its torso armour lost within ~5 s), or a heavy or
assault with a light putting 15+ damage into its back, calls for support; the squad coordinator sends the free
teammate with the shortest ETA (distance / top speed, at most 20 s; never an assault), which escorts the caller and
hunts the pest for 20 s, sharing the caller's sightings. `Behaviour_ReactionsJumpsAndMovement` reports it: mechs hit
from behind facing the attacker within 15 s 52 -> 85 % (3.6 -> 2.0 s); ~12 calls per 12 lance games.
Shoot-and-scoot: a medium relocates after every pop-up and pops at most every 5 s (it popped 2.4/min from one spot).
Lights jink (a 0.3-0.5 s hop half the time they're hit) and bound over rough ground at speed: 0.9 -> ~6.3 jumps/min.
Lance report after: light 202 dealt / 43 % survived, medium 128 / 30 %, heavy 139 / 28 %, assault 158 / 40 %.

**Fair matches.** Battle maps are point-symmetric through the centre like the spawns (`MirrorThroughCentre`: the
south half is the north half rotated 180°, crossfaded over ±30 m so there is no seam; mid-field features are
generated in the north half only so mirroring doubles rather than halves them). No Man's Land has a 13 m staging berm
between each trench and its spawn ground, so the lances form up out of sight. The watch lance fields the same lance
on both sides. Before: on NML seed 7 (the default) the north assault's spawn was under fire from three mechs within
1-2 s (median death 23 s), the south side won 13-18 of 24, mirrored model mixes tilted another ~59-41. After: 12-12
and 14-10 / 7-17 on two seeds, assaults' median death 57-64 s. Pop-up spots only count within 1.1x the medium's range
and no better than hull-down (mediums sat in their own trench popping at targets 300 m off).

The siege cannon kills a light (60 torso HP) with one clean torso hit, but its 240 m/s shell needs ~0.8 s to cross
200 m: a running light is gone by then. Weapons carry their look (length, calibre, barrels/lenses); the rig builds
them at each model's arm mount point and flashes, beams, shells and impacts scale with damage.

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

**Godot additions (not in the prototype):**
- **Cockpit** section: 5 HP, a 0.3 m sphere on the canopy glass, just outside the torso sphere so it can only be
  struck from the front. Destroyed = pilot killed: the mech slumps like a wreck but without fire/smoke and with the
  frame intact (salvage). One cannon hit, or one laser hit inside ~127 m, kills the pilot. Cannon shells drop
  ~0.3 m over 60 m, so a cockpit shot needs holdover.
- Mechs that are out keep their hit spheres (shots stop on wrecks) but take no further damage.
- Section damage lives in `MechDamage` (plain data) and can be passed into a new `MechSim` to persist.

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

## Godot port (`IronFeud.Logic/Battle`, `IronFeud.Logic/AI`)
The whole battle runs headless in plain C#: `ArenaMap` (ground + axis-aligned blocks, cover points, rays, LOS),
`BattleSim` (units, AI, shells, beams, damage, collision, outcome), `AiPilot` (everything above). Godot only renders.
`tests/BattleTests.cs` plays AI duels over several seeds and prints a summary (run with
`dotnet test tests/ --filter AiDuels --logger "console;verbosity=detailed"`).

Deviations from the prototype, and why:
- **Retreat nerve:** RETREAT is multiplied by `exp(−max(0, t − lastHurt − 10)/12)`. Without it two damaged pilots hid
  behind cover forever (seen in headless duels).
- **Heights from the pose:** eye = cockpit sphere, target = torso sphere (prototype: constants 8.3/7.0). Contacts remember
  the target's torso/cockpit height, so aim and hiding work for any chassis. Cover hides a point at 0.85 × own torso height.
- **Weapons are generic:** fire discipline loops over mounts (projectiles get drop compensation, beams fire inside 0.9·range);
  `hp` uses the fraction of weapons online; twist-away waits for every online weapon to recycle.
- **Aim error scales with target speed / 4.2 m/s** (the medium's top speed), so fast lights are genuinely harder to hit.
- **Aiming is open loop like the prototype** (twist = bearing + error − heading). A closed-loop `AimSolver` version that
  cancelled torso sway made duels last ~15 s with ~75 % hit rates; open loop gives 25–70 s for mediums.
- **Destination clamp** leaves a block by the nearest face that stays inside the ring (prototype: dominant axis, could
  push points outside).
- **Collision:** mechs push apart (radius 3 m × chassis scale each, wrecks don't move) and are pushed out of blocks
  (the prototype had no mech-vs-block collision; steering avoided blocks).
- Seeded RNG per pilot: battles are deterministic for a given seed.

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
