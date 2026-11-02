# Genre gap audit (2026-11-02)

IRONFEUD's battle loop against the systems that make up "the heart and soul of MechWarrior" (Philipp's brief), from
the research (`report.md`, `notes/`) and the code as of commit 9b667cb. Prompted by side torsos having been missing
although the research named them: this list is meant to surface every such pillar plainly, before content work.
Campaign-layer systems are listed last; the battle loop comes first by Philipp's call.

Legend: **have** = in and tuned; **partial** = a simpler form exists; **missing** = not in the game.

## Combat core

| Pillar | Genre | IRONFEUD | Status |
|---|---|---|---|
| Legs vs torso, throttle, momentum | Throttle hold, reverse, torso twist, accel limits | All of it, procedural gait | have |
| Heat as a budget | Heat scale, running hot, shutdown, override | Running hot 0.6-1, gun lock, shutdown, coolant flush; **no shutdown override** | have (override missing) |
| Section damage | Head, CT, LT, RT, arms, legs; front/rear torso armour | 8 sections, rear plates on the torso, transfer rules | have (2026-11-02) |
| **Weapons per mech** | 4-10 weapons, energy + ballistic + missile mixes, "alpha strike" | **2 arm weapons + 1 rack** (+ point defense) | **partial: the biggest feel gap** |
| **Weapon groups, chain fire** | 4-6 groups, chain/alpha per group, group display | 3 fixed triggers (one per weapon) | **missing** |
| **Ammunition** | Ballistics and missiles carry ammo by tonnage; energy weapons pay in heat | **Every weapon is infinite**; only heat limits anything | **missing** (the research's "each family hits a different wall") |
| **Arm vs torso aiming** | Arm weapons aim in a cone faster than the torso (arm lock toggle); torso weapons fixed | All weapons converge on the torso aim ray | **missing** |
| Criticals, components, ammo explosions | Engine/gyro/actuator/heat-sink criticals, ammo cooks off, XL side-torso death | Proportional degradation of exposed structure (limp, slow cycle, weak sinks, blurred sensors) | partial |
| Stability, stagger | MW5 none; HBS/AC6 meters | Stability meter, stagger, impact per weapon | have |
| **Falls, knockdowns** | Tabletop, HBS, MW2-era | None | **missing** (designed: capture point) |
| **Melee, ram, death from above** | MW5 (since 2021), tabletop | None | **missing** |
| Jump jets | Most games | Jets, fuel, landing, jump AI | have |
| **Scale through destruction** | Assaults flatten buildings and trees, lights use them as cover | Static blocks only | **missing** |
| Mount height vs cover | Praised in both MW5 reviews | Real muzzle positions, hull-down AI | have |

## Information and control

| Pillar | Genre | IRONFEUD | Status |
|---|---|---|---|
| **Zoom** | Every MechWarrior has a zoom view | None | **missing** |
| **Player radar / minimap** | Radar with contacts, battle grid map | AI has scans, marks, ECM; **the player HUD has no radar** | **missing (player side)** |
| **Target readout** | Target paper doll, distance, weapons, damage | Target name and lock progress; sections only in the debug panel | **partial** |
| Sensors and EW | Active/passive, ECM, BAP, TAG | Scans, marks (designator), ECM that degrades | have (AI side) |
| Cockpit warning voice | "Bitching Betty" style | Synthesised placeholders | partial |
| **Lancemate orders** | F-keys, radial, battle grid (Clans) | None | **missing** |
| Objective missions | Raid, defend, escort, recon, assault | Deathmatch only | **missing** |

## Mechs and loadouts

| Pillar | Genre | IRONFEUD | Status |
|---|---|---|---|
| Chassis per class with identity | Dozens of chassis | 6 designs, quirks | partial (visual redesign pending) |
| **Variants per chassis** | 3-6 variants, different hardpoints | One fixed loadout per design | **missing** |
| **Mechlab / hardpoints** | Typed and sized hardpoints, slots, tonnage | None | **missing** (research: semi-fixed mounts, not a free lab) |
| Tonnage as a number | 20-100 t | Four classes, no tonnage | missing (needed for drop caps and campaign) |

## Campaign layer (after the battle loop, by Philipp's call)
Salvage, repair time and cost, pilots with skills and injuries, ejection, contracts and reputation, drop tonnage caps,
mech purchase, the galaxy layer. All **missing** by design for now; `BattleSetup` / `BattleResult` and persistent
`MechDamage` are already shaped for it.

## Recommended order for the battle loop

1. **Mounts, weapon groups and ammo together** (one rework): a mount list per chassis (type, size, section, height),
   4-6 weapons on most designs, groups with chain/alpha fire, ammo for ballistics and missiles (with ammo in a section
   that can cook off), arm mounts aiming in a cone. Before the visual redesign, because the bodies are built around
   their hardpoints. Balance re-run afterwards.
2. **Player instruments**: zoom, radar/minimap with graded contacts, target paper doll, weapon-group panel.
3. **Visual redesign** one chassis at a time with Philipp, on the shape kit, around the mounts from step 1.
4. **Criticals and components**, ammo explosions, shutdown override.
5. **Falls, then melee and ramming** (the research's capture-point design).
6. **Destructible cover** (trees, buildings; assaults walk through what lights hide behind).
7. **Lancemate orders**, then **objective missions** (they give lights their jobs: the research's main finding).
