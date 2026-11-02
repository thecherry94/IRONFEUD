# Weapons rework: mounts, groups, ammo, arm aiming (design, 2026-11-02)

From the genre gap audit (`docs/research/mech-genre/gap-audit.md`): IRONFEUD mechs carry 2 arm weapons + 1 rack,
fire each on its own trigger, never run out of ammunition, and aim everything along the torso. Philipp: "yes do the
weapons rework first" (before the visual redesign, because the bodies are built around their hardpoints).

## Goals
- 4-6 weapons on most designs, mixed families, so the alpha strike and heat juggling of the genre exist.
- Each weapon family hits its own wall: energy weapons pay in heat, ballistics and missiles in ammunition (and
  ammunition can cook off).
- Weapon groups with alpha and chain fire for the player; the AI keeps choosing weapons individually.
- Arm weapons aim within a cone, faster than the torso twists; torso weapons fire along the torso.
- Every weight class stays viable: lance report and model duels inside their limits afterwards.

## Non-goals
A mechlab or free hardpoint editing (later: semi-fixed mounts per chassis, variants), tonnage numbers, criticals
beyond ammunition cook-off (separate item), new visual bodies (the redesign follows this).

## Decisions
- **Mount list.** A chassis carries `Mounts`: weapon, section (`ArmLeft`/`ArmRight`/`Torso`/`TorsoLeft`/`TorsoRight`),
  offset (elbow frame for arms, torso frame for torso mounts) and ammunition. Bodies define the anchor points (arm
  mounts, shoulder rack, chest mounts); loadouts put weapons on them. Replaces `RightArmWeapon`/`LeftArmWeapon`/
  `TorsoWeapon`. Step one is a behaviour-preserving refactor (tests and a fixed headless fingerprint unchanged).
- **Ammunition.** Ballistic and missile weapons get a number of rounds (salvos for missiles), sized in seconds of
  continuous fire (a first cut of about a minute, tuned by "ran dry per game" in the lance report); energy weapons
  none. The rounds sit in the mount's section, an arm weapon's in the side torso it hangs from. Empty = offline.
  **Cook-off:** when a section holding ammunition is destroyed, a fifth of the damage still in its rounds goes into
  the centre torso's structure (armour does not help). Side torsos become worth shooting.
- **Weapon groups (player).** Six groups, each a set of mounts with alpha (all ready weapons fire) or chain (the next
  ready one, then a 0.25 s beat) fire. Defaults: 1 long-range direct fire, 2 short-range direct fire, 3 missiles.
  Keys 1-6 fire groups (LMB/RMB/MMB = 1/2/3); a small HUD panel lists weapons with group membership, ammunition and
  cooldown, and edits groups (select a weapon with [ ], Shift+N toggles it in group N, Ctrl+N toggles chain). V cycles
  the camera in play modes (1-4 stay cameras when watching). Logic in `IronFeud.Logic` (a pure fire-mask builder).
- **Arm aiming.** Arms track the commanded aim (twist and pitch targets) at 2.5x the torso's twist rate (the
  genre's ratio), within a cone around the torso's aim (about ±20° yaw, ±25° pitch, per chassis). Arm weapons converge
  on the arm aim ray, torso weapons on the torso's. The player sees two reticles (torso cross, arm circle). The AI
  checks alignment per weapon on the ray that weapon fires along.
- **New weapon:** a small laser (short range, cheap heat) as the filler weapon of lights and the secondary of bigger
  mechs. Names stay generic.
- **Loadouts** (first cut, then balance runs): lights 4 weapons, mediums 5, heavies 5-6, assaults 6, every design
  mixing families; armour and heat sinks retuned so time to kill stays near today's.

## Verification
Unit tests per piece (mount sections, ammunition and cook-off, group masks with chain timing, arm cone limits and
rates, per-weapon alignment); all existing tests ported; lance report (80 games) and model duels; a "ran dry" and
"cook-off" column in the lance report.

## Left to the builder
Exact ammunition counts, anchor positions per body, default group rules, the HUD panel's look.
