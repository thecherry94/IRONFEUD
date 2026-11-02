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
- **Personalities** (Bascinet vs Bascinet, 40 games each): Brawler vs Balanced 19-21 / 18-21, Brawler vs Sniper
  18-20 / 13-26, Balanced vs Sniper 18-21 / 17-22 (No Man's Land / hills). Brawler is not overpowered; Sniper is a
  little strong on the open hills.
- **Models:** the Sicklefoot won ~80 % against an equal Bascinet: smaller, lower hit spheres (its hull hides behind
  crests; the Bascinet's tall torso shows), so it was hit 45-50 % vs 28-35 %. Rather than nerf it, the Bascinet carries
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
Since 2026-11-02 (weapons rework, `docs/superpowers/specs/2026-11-02-weapons-rework-design.md`): a chassis carries a
`Loadout` of weapons on hardpoints (arms, shoulder rack, chest left/right), 5-6 per mech; fire bit i = mount i (the
right arm's first weapon is still bit 0, the left arm's bit 1, the rack's bit 2). Guns and racks carry rounds (about a
minute of fire, 45 s for racks); the rounds sit in the mount's section (an arm weapon's in its side torso) and a fifth
of what is left goes off in the centre torso when that section is destroyed. Arm weapons aim along the arms' ray,
which leads the torso by up to ±20° yaw / ±25° pitch at 2.5x the twist rate; torso weapons along the torso's; the AI
checks alignment per weapon. The player fires weapon groups (1-6, LMB/RMB/MMB; alpha or chain), the AI single
weapons. Older notes below describe the two-arm + rack era.

Right arm = fire bit 0, left arm = bit 1. Every weapon: full damage out to its optimal range, then linear to zero at
max range (shells measure the distance they flew and expire there; the AI fires within 40 % damage). Before, shells
kept full damage at any range and beams fell to 20-25 %.

| weapon | kind | dmg | heat | cooldown | optimal / max range | shell m/s | used by |
|---|---|---|---|---|---|---|---|
| Pulse laser | 3 pulses, 0.09 s apart | 9 (3 × 3) | .09 | 0.9 s | 180 / 280 | | Bascinet light (L) |
| Autocannon | 2 barrels | 4 | .03 | 0.25 s | 300 / 520 | 560 | Sicklefoot medium (L) |
| Incendiary cannon | 2 barrels, +0.06 target heat a round | 2.6 | .03 | 0.25 s | 260 / 460 | 520 | Bascinet light (R) |
| Machine gun | 2 barrels, x0.15 vs armour, x1.6 vs structure | 1.5 | .004 | 0.08 s | 160 / 260 | 900 | Sicklefoot light (R) |
| Flamethrower | 1.2 s stream, +0.5 target heat a burn | 4 | .08 | 2 s | 50 / 80 | | Sicklefoot light (L) |
| Laser | 1.25 s burn | 20 | .24 | 2.25 s | 200 / 320 | | Bascinet medium (L) |
| Cannon | shell | 14 | .08 | 0.75 s | 400 / 640 | 450 | (prototype) |
| Heavy laser | 2 s burn | 32 | .35 | 3.5 s | 240 / 380 | | heavy, Bascinet assault (L) |
| Heavy cannon | shell | 32 | .20 | 2.2 s | 360 / 600 | 330 | Bascinet medium, Bascinet heavy (R) |
| Particle cannon | bolt, no drop, ramps to full over 90 m, scrambles sensors 1.5 s | 26 | .32 | 3.2 s | 450 / 650 | 1300 | Sicklefoot medium, heavy (R), Sicklefoot assault (L) |
| Siege cannon | shell | 75 | .36 | 4.5 s | 450 / 700 | 240 | assault (R) |
| Rocket pod | 6 rockets, 0.05 s | 18 | .12 | 3 s | 130 / 220 | 200 | light (shoulder) |
| Strike rack | 10 rockets, 0.06 s | 25 | .16 | 4 s | 300 / 400 | 260 | medium (shoulder) |
| Arc missiles | 10 guided, 0.1 s | 30 | .20 | 5 s | 650 / 800 (min 160) | 140 | heavy (shoulder) |

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

**Armour and internal structure** (player request: "sturdy armour plates and below that their core internals that
are extremely fragile ... once exposed they should act accordingly"). Each section is a plate over structure (limbs and
head 65/35 of the chassis' HP figure; the torso 55 % front plate, 15 % thin rear plate, 30 % structure; rear hits
strike the rear plate, replacing the old x1.75 rear multiplier). Hits strip the plate first, the rest goes into
structure; a section is destroyed when its structure is gone. Weapons carry VsArmor / VsStructure factors (a machine
gun barely scratches plates and shreds exposed structure). Exposed structure degrades its section in proportion to
what is left: legs limp (top speed down toward the crippled cap), an arm's weapon cycles slower (up to 2x), torso heat
sinks lose capacity (dissipation down to 40 %), the head's sensors blur (AI aim error up to 2.5x, sight range down).
The HUD shows armour/structure per section ("!" = exposed); the AI panel draws both bars.

**Heat and specialist weapons** (the lights package: lights must be a real nuisance to the backline, not just
spotters). Each model now carries its own loadout (table above): the Sicklefoot light pairs machine guns with a
flamethrower (heat it, then shred whatever the plates no longer cover), the Bascinet light an incendiary cannon and
a pulse laser; Sicklefoot mediums to assaults carry the particle cannon. Heat weapons add heat to the target in
proportion to the damage they land. A mech's own guns lock at heat 1, but outside heat (and jets) can push it on: at
1.2 the reactor scrams for 3 s (`MechSim.ShutdownHeat`): no legs, no guns, torso slack, heat sinks still working; the
HUD and the mech tag say so and the attacker is credited (`UnitStats.ShutdownsCaused`). The restart flushes the
coolant (`MechSim.RestartHeat` 0.6) and outside heat doesn't stick for 4 s after (`CoolantFlushTime`, HUD "COOLANT
FLUSH"). Player call "flamethrowers are a little too OP": a shutdown bled only ~0.17 heat, so the victim restarted
above the line and scrammed again on the spot, half of all shutdowns in 40 watch lances were such re-shuts (chains up
to 15.6 s, 7 deaths while shut down). After: 33 -> 15 shutdowns, none chained, longest 3 s, 3 deaths while shut down;
lance report light 110 dealt / 0.75 kills / 38 % survived / 0.20 shutdowns per game (was 108 / 0.68 / 40 % / 0.50).
A particle bolt is a
projectile, not hitscan (player call: "extremely fast, sounds scary, hits hard, lots of heat"): 400 m in ~0.31 s,
less damage inside 90 m, and a hit halves the target's sensor quality for 1.5 s (AI aim error and sight range).
Flamethrower lights are brawlers: they aren't assigned as spotters and don't huddle under point defense, and against
a heavy, an assault or a target the scan shows running hot (> 0.45) they circle at 55 m and go mostly straight in
while far out (before, the 0.9 rad flank step around a 300 m circle made the "spiral" an orbit: median 170 m from the
target, in flame range 12 % of the time; after, 21 % and 4x the heat put in). The machine-gun AI prefers targets the
scan shows with exposed structure. Flamer heat per burn 0.3 -> 0.5: at 0.3 lance games saw no shutdowns at all.
Lance report (20 games): light 50 dealt / 0.30 kills / 25 % survived / 0.51 heat in / 0.05 shutdowns per game,
medium 97 / 0.50 / 40 %, heavy 163 / 1.45 / 43 %, assault 111 / 0.33 / 35 %. Lights were still the weakest class here (see marks and ECM below).

**Signature by size.** Sight (600 m), radar (260 m), team spotting and so locks scale with the target's chassis
signature: light 0.6, medium 0.8, heavy 1, assault 1.15 (`MechChassis.Signature`). Firing or a jet burn shows a mech
at full size for 2 s (`BattleSim.RevealTime`): a light picks its moment, but is as visible as anyone while shooting
(so it stays hittable; "lights unhittable at range" is an open item). An assault sees a quiet light at 360 m, the
light sees it at 690 m. Lance report: light 60 dealt / 0.38 kills / 23 % survived / 0.45 shutdowns per game (was 50 /
0.30 / 25 % / 0.05), medium 90 / 0.48 / 30 %, heavy 146 / 1.35 / 33 %, assault 125 / 0.65 / 30 %.

**Marks** (player call: marking must not leave a light "sitting in the open like a duck"). Lights carry a designator
(`MechChassis.Designator`): a completed lock marks the target, renewed only while the light itself sees it (by its
unmarked signature) and holding 6 s after (`BattleSim.Marked`, team tactic `Marks`). Marking is passive, part of the
light's normal fighting: no extra time on station. For the marking lance a marked target counts as spotted (locks
from cover), locks in a third of the time (arc missiles 1.5 -> 0.5 s), shows at full size (a marked light is seen at
600 m, not 360), is known wherever it goes, is aimed at with 25 % less error and is preferred (+0.3; focus +0.8).
A spotter whose mark is fresh (> 3 s left) leaves its post: the heavy locks through the mark. The marked pilot gets
the designator's bearing; the HUD shows "MARKED BY DESIGNATOR" and tags marked targets, the AI overlay rings them.
`Marks_WhatTheyAreWorth`, marks vs the same lance without (48 games): Bascinet light 31-17 (light survived 46 %),
Sicklefoot flamer light 24-24 (it marks from flame range, mid-brawl); passive marks alone, before full-size painting
and the preference, were 25-23 / 21-27. Enemies are marked ~13 % of the time. Lance report: light 55 / 0.20 / 18 %,
medium 84 / 0.43 / 28 %, heavy 142 / 1.63 / 38 %, assault 121 / 0.58 / 35 % (marks also expose enemy lights).

**ECM** (generic genre jammer; the Sicklefoot light carries it, `MechChassis.Ecm`, so the two light models differ:
Bascinet = ranged designator, Sicklefoot = flamer brawler with a jammer). Inside its 120 m bubble
(`BattleSim.Jammed`): enemy mechs can't lock or scan and drop off their lance's shared sightings and radar; its own
lance can't be locked (so not scanned, nor fired on with arc missiles) unless marked, and designators burn through,
so the enemy light is the answer. A salvo already in the air keeps homing. Pilots that are jammed, or whose target is
shielded, go for a visible jammer (+0.6: "kill the jammer"). The HUD says "ECM: SENSORS JAMMED", the AI panel too;
the overlay draws every bubble. `Ecm_WhatItIsWorth` (the default lance vs the same with the jammer off, 48 games),
by effect alone: jamming the locker 24-24, shielding targets from locks 30-18, cutting the network 28-19, shaking
off missiles in flight 35-13 (the lance moves within 120 m of its light: arc missiles, half a heavy's damage, were
gutted); all of it 37-11. Without the missile shake-off and with the jammer drawing fire: 31-17, the ECM light
surviving 50 %. Lance report: light 58 dealt / 0.40 kills / 33 % survived / 0.35 shutdowns per game, medium 89 / 0.38
/ 38 %, heavy 143 / 1.35 / 43 %, assault 103 / 0.38 / 38 % (lights started this round at 0.23 kills, 20 %).
2026-11-02, from the genre research (an ECM that blocks became the genre's mandatory pick and was walked back to lock
delays): jamming now degrades instead of blocking. In a hostile bubble locks take 2.5x as long (`JamLockFactor`;
designators and marks still cut through), radar reaches half range, a jammed mate's sightings reach the lance every
2 s, and every enemy within 180 m knows where the jammer is (`JammerRevealRadius`). Heavy's lock: 1.51 s clear,
3.75 s jammed, 1.52 s with a designator marking. Head-to-head 29-19 blocking -> 26-22 degrading; the ECM light now
survives 35 % (was 46 %).

**Running hot and stability** (2026-11-02, `MechSim`). From heat 0.6 to the weapon lock at 1 the legs lose up to 15 %
of top speed and the gyro steadies the guns less (up to half its stabilisation), a visible slope before the cliff
(HUD "RUNNING HOT"); own guns can heat a mech to 1.15 at most, so only outside heat and jets scram it. Every hit
drains a stability budget (light 40, medium 70, heavy 100, assault 135; Bodkin x0.9, Harrow and Barbican x1.25) by
damage x the weapon's impact (shells 1, particle 0.9, autocannon 0.8, missiles 0.6, incendiary 0.5, MG 0.3, beams
0.15, flamer 0); drained, the aim wavers (up to 60 % of the stabilisation gone); emptied, the mech staggers 1.1 s
(guns off line, legs coasting, torso rocked away from the blow), refills half and can't stagger again for 3 s.
Lance report (80 games) staggers/game: medium 0.50, light 0.18, heavy 0.11, assault 0.04. Not yet: stumble steps
from the capture point, falls, the AI exploiting a staggered target.

**Lance tactics** (`BattleSim.Tactics` per team, `TeamTactics_HeadToHead` reports them). Radar: lancemates share
sightings (position, not sight: firing still needs one's own eyes; a missile lock can use a teammate's). Spotters: the
coordinator pairs each guided-missile mech with the nearest free light; while its heavy has a salvo nearly ready and
its target is in range but unseen by anyone else, the light takes an observation post 300-460 m out (cockpit sees the
target, torso hidden from it) and holds it; otherwise it skirmishes. Umbrella: a pilot under a lock or an inbound
guided volley closes on a teammate with point defense within 350 m (~50 m from it, away from the threat); without
one, a stationary pilot moves. Head-to-head vs an identical lance without them, 48 games each: all 29-19, radar
alone 29-19, radar + spotters 27-21, umbrella alone 24-24. Target choice also weighs who is hurting the pilot now
(damage in the last few seconds, any direction: 15 is worth +0.6), drops targets beyond every weapon's effective range
(-0.6), and a light counts as chased with an enemy light or medium within 160 m (player report: a light chased the
far assault while an enemy light harassed it from 150 m; pinned by `ALight_DealsWithTheLightOnItsTail`).
Tunnel vision (player report: a heavy fixated on a nearly dead enemy far off behind cover while a light sneaked up
and killed it): in 40 lance games, heavies/assaults hit by a light while busy with a far target died before turning
on it 5 of 23 times. Now anything within 150 m scores as a threat by being there (up to +0.8), recent damage weighs
0.075/pt (cap 1.2), the kill on offer (focus, its damage) counts fully only for a target in sight and reach, pressing
stops while another enemy is hurting the pilot, and a close hit triggers an immediate re-think: 1 of 19.

**Scans** (player rule: "the mechs first have to get a lock on the target and analyze its internals before making
decisions; this is shared information across a lance"). Every mech now locks its pilot's candidate (spotted by the
team, within 20° of the torso aim, in 600 m, for 1.5 s; guided launchers use their own lock). While a lock holds it
writes a `ScanReport` (health, heat, any exposed section) into the team's intel (`BattleSim.Scan`), stale 10 s
after the lock is lost. The AI reads enemy state only from there (`KnownHealth`: unscanned enemies count as healthy):
target choice, pressing an advantage and the coordinator's focus fire. The player's HUD shows a target's sections
only once the lance has scanned it ("SCANNING n%" while locking, "NOT SCANNED" otherwise).

**Pressing and falling back** (player feedback: "is the AI aware it has an advantage?", "in retreat they sit in their
hidey hole ~20 s doing nothing"). Advantage = own health - the target's (its damage shows) + 0.3 per fighter the
sides differ by (the opposing roster minus confirmed kills; counting only current contacts made everyone feel ahead
early). Pressing starts when it is over 0.5 with the target under 60 % and the pilot over 60 %, stops under 0.25:
+0.35 to Engage, preferred range x0.6, no retreat. ~28 % of AI time in lance games. Retreat is a fighting
withdrawal: every ~6 s or on arrival a new spot 60-140 m away scored for distance gained from the threat, being
hidden from it, a new angle on it, friends nearby and a clear path; retreating pilots now move 65 % of the time.

**Missiles and point defense.** A third mount on the right shoulder (fire bit 2, in the torso frame, lost only
with the mech). Rocket pod (light): 6 unguided rounds, wide cone, 18 per salvo, 130/220 m. Strike rack (medium):
10 unguided fast rounds, tight pattern, 25, 300/400 m; no homing on purpose (hard to balance, would dominate).
Arc missiles (heavy): 10 slow guided rounds launched steeply (loft 0.9 rad) that home on the launcher's lock after a
0.5 s climb and dive over cover; 30, 650/800 m, duds inside 160 m. A lock needs the candidate spotted by any
teammate (cockpit sight within 600 m) and kept within 20° of the torso aim for 1.5 s. The AI fires them on the lock
whether or not it sees the target (indirect fire: a light spots, the heavy fires from cover); unguided salvos go
through the normal fire discipline. Guidance is top attack: cruise high over the target's area (aim above it by half the remaining ground distance,
20-120 m) and dive in the last 90 m at twice the cruise turn rate; a direct line came in at ~13° and buried itself in
crests in front of hull-down targets. Lance games: rounds hitting 37 -> 56 % (86 % of those not shot down), 22 damage
per salvo, ~half of heavies' damage (was 11.5 per salvo and 30 %). Damage 30 -> 40 per salvo, minimum range 160 ->
120 m. A proximity fuse was tried and dropped (the player prefers rounds that hit by flying well).
**Arc missiles vs lights** (player report: lights "obliterated" and "dominated by arc missiles"). Measured in 20
lance games: arc rounds hit lights 74 % of the time, more than any other class (assaults 58 %); 57 % of the hits
landed while the light was in the air (jink and bound hops) and with hops off it was still 78 %: the guidance led the
target with perfect information every tick and turned at up to 3.2 rad/s (~450 m/s² at 140 m/s against a light's
~45). Now the missiles steer on the lock's track, refreshed once a second (`BattleSim.MissileTrackInterval`) and
extrapolated in a straight line: a target holding its course, or too slow to leave it, is hit; one that keeps
turning makes the salvo dive on where it would have been. Lights and mediums with a guided salvo inbound evade
(`AiPilot.Evading`): flat out, weaving ±0.7 rad on a 2 s cycle, no hops; the HUD warns "INCOMING MISSILES" (and
"MISSILE LOCK"). Hit rates: lights 74 -> 60 % (0.5 s track) -> 53 % (+ evasion) -> 33 % (1 s track, 2 s weave);
mediums 63 %, heavies 67 %, assaults 59 %. Pinned by `ArcMissiles_MissAWeavingLight_ButNotAnAssault` (4 salvos:
light 160 standing, 13 weaving; assault 108 either way). Lance report: light 90 dealt / 0.40 kills / 43 % survived
(was 58 / 0.40 / 33 %), medium 110 / 0.53 / 38 %, heavy 167 / 0.73 / 38 % (kills were 1.35), assault 148 / 0.73 /
45 %; light deaths 30 -> 23 of 40, median 34 -> 42 s, only 1 before 20 s (was 7); arc kills on lights 13 -> 5.
The siege cannon is now the top light killer (the flamer light brawling beside an assault).
Point defense: turrets (assault 1; a second on the Bascinet medium made every lance a two-turret screen) each shoot down 1.5
missiles a second within 110 m that are closing on the mech or a teammate within 120 m; turrets add up. One arc
volley shot down: 1 turret 33 %, 2 turrets 73 %, 3 turrets 100 % (design: one turret thins, stopping a volley takes
investment, on one mech or across the lance; 5/s erased 9 of 10). Lance report after: light 122 dealt / 30 %
survived, medium 147 / 28 %, heavy 160 / 28 % (most kills earlier runs: missiles gave heavies their role), assault
180 / 43 %. Support calls were silently broken (Perceive consumed the attacker the danger check needed); fixed and
set to 30 % of torso armour in ~5 s: ~7 calls per lance game.

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
Prototype: torso, two arms, pooled legs. Since 2026-11-02 (Philipp: "The ones in battletech have arms, side torsos,
center torso, cockpit and legs"; design `docs/superpowers/specs/2026-11-02-side-torsos-and-legs-design.md`) eight
sections: centre torso (`Torso`), left and right side torsos, two arms, two legs, cockpit.
- **Hit volumes** (spheres, updated each frame), from the chassis' torso sphere (radius R, default 2.4): centre 0.8 R,
  side torsos 0.6 R at ±0.6 R (91 % of the old frontal area); each arm r 1.4 at the forearm; one sphere per leg at the
  middle of hip, knee and ankle, 0.6 x `LegsHitRadius`.
- **HP** from the chassis' figures: centre 0.8 x `TorsoHp`, each side torso 0.25 x, each leg 0.6 x `LegsHp`. Measured
  over 12 lance battles the centre takes ~76-80 % of torso hits and the sides ~10-12 % each, so the centre's time to
  kill is the old torso's; all kills stayed centre-torso kills (legs take ~2-3 % of damage: no legging meta).
- **Transfer:** a destroyed arm or leg passes hits to its side torso, a destroyed side torso to the centre. A side
  torso takes its arm with it, and the shoulder rack if it sits in it (`WeaponMount.Section`).
- **Out:** centre torso (wreck: pelvis collapses ~2.5 m over 1.4 s, legs fold via the same IK, smoke and fire),
  cockpit (pilot killed), or both legs. One leg destroyed → speed x0.45; lost side torsos cost 15 % heat sinks each.
- Rear plates on all three torso sections. The AI's twist-away turns its weaker side (side torso + arm) from the fire.
Hits add trauma and random torso pitch/roll spring kicks proportional to damage.

**Godot additions (not in the prototype):**
- **Cockpit** section: 5 HP, a small sphere on the canopy glass (player call: it kills the pilot in one solid
  hit, so it must be extremely hard to hit). Bascinet: r 0.09 m since 2026-11-02 (0.12 only passed through a
  ray-sphere precision error: at 300 m every tiny sphere was ~0.1 m), scaled by size, recessed into the torso sphere so
  ~5 cm of it shows, only head-on; Sicklefoot (head juts out of the torso): r 0.08 m. Share of torso-aimed hits
  (1.5 m scatter) striking it: 0.1-0.3 % head-on, from a diving missile or the side (was 1.3-3.2 %, r 0.3 m, proud
  of the torso: one 10-round arc salvo had a ~15 % chance to kill the pilot and lights died that way at 6-8 s).
  Pilot kills in 20 lance games 18 -> 6, light median death 28 -> 34 s. Originally it could only be
  struck from the front.
- **Cover plates** (player note: some genre mechs have torso/shoulder structures that give extra cover):
  `MechChassis.ShoulderCover`, oriented boxes in the shoulder frame belonging to that arm, drawn by the rig from the
  same data (F3 shows them). The Bascinet has its pauldrons and a raised flange on each, swept forward beside the
  head; the Sicklefoot has none (weapon pods on bare pylons). The section spheres are coarse (the torso sphere
  reaches ~1 m past the visible chest), the plates exact: a plate takes the hit if the ray reaches it first or meets
  it inside the sphere it struck. A destroyed arm's plates are gone. Torso-aimed hits on a Bascinet medium: torso
  share head-on 84 -> 80 %, at 45° 67 -> 51 %, side 78 -> 59 % (arms take the rest; the plates add a little
  silhouette: misses 20 -> 16 % head-on). Twisting a shoulder into the fire now spends an arm to spare the core.
  Medium duels Bascinet vs Sicklefoot (Models_AreBalanced): hills 28-12 before the plates (already lopsided since
  per-model loadouts: the Sicklefoot medium's particle cannon + machine gun does ~11/s on armour vs ~23/s), 34-6 after.
  Swapping its machine gun for a laser made it worse (34-5, overheats), a pulse laser too (31-9); an autocannon
  balanced it: nml 20-20, hills 26-14, brawlers 21-19, watch 1v1 27-13 (all under the 70 % limit). Destroyed = pilot killed: the mech slumps like a wreck but without fire/smoke and with the
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
  Godot port (`BattleSim.WalkClear`): the rays are footprint tests that count a block wherever its top is above the
  terrain they meet it on (the collision's rule, without its 0.6 m step: the probes stay the more cautious), wrecks in
  the corridor count too, and once the direct course is blocked the detour keeps to the side it took first until the
  course clears. Player report "got stuck on the corner of this building and became a sitting duck": knee-height rays
  passed over low and sunken blocks, the collision measured from the body (0.5 m below the terrain on slopes) and walled
  blocks flush with the ground, wrecks were invisible, and the detour side flipped with the orbit (every 6-12 s), so a
  mech ran back and forth along a wall. 20 watch lances per map, seconds pressing into obstacles: No Man's Land
  325 -> 51, hills 1198 -> 195; stalls over 3 s (up to 65 s) -> none (`SteeringTests`, opt-in `StuckTime` report).
  Missile evasion swings only to a walkable side, and slows to turn when coming about (flat out it ground along walls).
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
  whose top is more than 0.6 m above their feet (`ArenaMap.StepHeight`; the prototype had no mech-vs-block collision;
  steering avoided blocks).
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
