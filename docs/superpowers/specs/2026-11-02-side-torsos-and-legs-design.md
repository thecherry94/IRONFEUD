# Side torsos and separate legs (design, 2026-11-02)

Philipp: "Do our mechs also have side torsos? The ones in battletech have arms, side torsos, center torso, cockpit
and legs." Agreed order: (1) this damage model, (2) a richer shape kit for the rig, (3) visual redesign one chassis at
a time around the new section volumes (research: `docs/research/mech-genre/notes/visual_design_language.md`).
Section damage as a mechanic is free to use; avoid the genre's record-sheet armour diagram and its terms.

## Sections
`Torso` (the centre torso; name kept, it is still the body's core and aim point), `TorsoLeft`, `TorsoRight`,
`ArmLeft`, `ArmRight`, `LegLeft`, `LegRight`, `Cockpit` (8, was 5: one torso and one pooled "Legs").

## Rules
- **Out:** centre torso structure gone = destroyed; cockpit gone = pilot killed; **both legs gone = destroyed**
  (the genre rule; watched for a legging meta in the kill-section report).
- **Transfer:** hits on a destroyed arm or leg go to the side torso on that side; on a destroyed side torso, to the
  centre torso.
- **A side torso takes its arm with it:** destroying it zeroes the arm on that side (weapon offline, plates gone).
- **Rear plates on all three torso sections** (each 15 % of its total, front 55 %, structure 30 %, as the torso had).
- **Mounts belong to a section:** arm weapons to their arm; the shoulder rack to the side torso on the rack's side
  (`TorsoMount.X`, centre torso if within 0.3 m of the middle), so losing that side torso silences the rack.
- **Effects:** one leg exposed limps by its structure, one leg gone cripples (the old "legs destroyed" cap); heat
  sinks: centre torso exposure as before (down to 40 %), each destroyed side torso costs another 15 %.

## Numbers (first cut, then balance runs)
- HP from the chassis' existing figures: centre torso 0.8 × `TorsoHp`, each side torso 0.25 × `TorsoHp` (measured over
  12 lance battles the centre takes 80 % of torso hits, the sides ~10 % each, so the centre's time to kill stays the
  old torso's), each leg 0.6 × `LegsHp`.
- Hit volumes derived from the old torso sphere (offset O, radius R): centre sphere radius 0.8 R at O (it wraps round the canopy less than the old sphere:
  the cockpit stays under 0.4 % of torso-aimed hits), side spheres
  radius 0.6 R at O ± 0.6 R sideways (91 % of the old frontal area; a first cut at 0.7/0.55/0.65 covered 81 % and
  let rockets aimed at the chest miss and the cockpit show). Legs: one sphere per leg at the knee, radius 0.6 × `LegsHitRadius`.
  Per-chassis overrides come with the visual redesign.

## Verification
Unit tests for the rules above; all existing tests ported; lance report (80 games) and model duels: kills per class,
survival, time to kill, and the section-of-kill distribution (centre torso / legs / cockpit) so legging does not
become the dominant kill.
