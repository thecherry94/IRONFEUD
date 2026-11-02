# IRONFEUD codebase: what dedicated chassis per weight class would touch (internal survey, 2026-11-02)

Source: read-only survey of the repo (paths under src/IronFeud.Logic unless noted).

## Today
- One mutable `MechChassis` class (~90 fields), no data files. `Create(cls, model)` = body (Hauberk defaults or
  `SicklefootBody()`) -> per-class `Scaled(s)` (light 0.72, heavy 1.15, assault 1.3) + class stats -> `FitGaitToLegs()`
  -> armour factor. Heavy/assault are scaled copies of the medium body.
- Per model: leg layout (plantigrade/digitigrade), segment lengths, hip width, torso/shoulder/elbow/head offsets,
  twist max, arm mounts, hit spheres (incl. cockpit), shoulder cover plates (Hauberk only), armour factor.
- Per class: scale, speed/accel/turn/twist rates, jump jets (light/medium), HP, heat dissipation, torso rack weapon,
  point defense, designator, signature. Class x model: arm weapons, ECM.
- Visuals: parametric Godot box/cylinder primitives in `src/Mech/MechRig.cs`, one `Dress*` method per model (~100
  lines each), no authored meshes.

## Hard-coded assumptions
- Exactly 2 legs (gait leg table, pose arrays, rig arrays, servo audio, biped pelvis sway/roll formulas).
- Exactly 2 arms + 1 torso rack: fixed section enum {Torso, ArmLeft, ArmRight, Legs, Cockpit}, three fire bits bound
  to player controls, weapons only on elbow frames or the single rack.
- One torso twist ring. 5 hit spheres (one per section) placed by hand, plates mirrored and owned by arms.
- Don't scale with size: all spring frequencies, ground-follow rate, angular amplitudes, ankle/sole limits.
- `Scale` is used as a size proxy in ~15 places (gait lift, sway, collision radius 3*Scale, AI blocker sphere,
  camera, nameplate, audio).

## Cost of new body plans
- Cheap (~1 day + balance runs): a new biped (plantigrade or digitigrade) with own proportions and 2 arm mounts.
- Medium: reverse knee (other IK root + layout value), size-scaled spring frequencies (f ~ 1/sqrt(size)), mount
  layouts beyond 2 arms + 1 rack (torso guns, armless, one-armed, >3 weapons) -> needs a mount list and a section set
  not tied to the enum, plus fire-group mapping for the player.
- Expensive: 4+ legs (N-leg gait with per-leg phase, support-polygon pelvis attitude, per-leg hit volumes); tanks
  (separate locomotion, loses the footfall weight that is the game's identity).

## Recommended refactor before a third body
Split chassis into (a) BodyPlan data: frame tree, leg list, mount list (frame, offset, section), hit-volume list,
plate list, cockpit/camera frames, spring params, footprint radius, base signature, dressing as MeshPart list;
(b) class stats; (c) loadout (weapon per mount). Store as JSON in the logic library (no Godot dependency) with an
optional Godot Resource wrapper for editor tuning. Then each per-class chassis is a data file; only non-biped plans
touch code. Replace `Scale` proxies with a real size measure (hip height / hit-volume extent).
