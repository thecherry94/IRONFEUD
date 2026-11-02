# Visual design language of the genre's mechs (study, 2026-11-02)

Sources: the official Steam store screenshots of MechWarrior 5: Mercenaries (12) and MechWarrior 5: Clans (9), and
image-search grids of in-game models and renders for Timber Wolf, Dire Wolf, Summoner, Hellbringer, Stormcrow, Mad
Dog, Executioner, Kit Fox, Adder, Mist Lynx (Clans) and Atlas, Marauder, Warhammer, King Crab, Catapult, Hunchback,
Awesome, Rifleman, Griffin (Inner Sphere). Purpose: extract *principles* for IRONFEUD's original designs. Nothing
here is a shape to copy; the IP rules in `ip_boundaries.md` still apply (no signature combinations, no faces).

Prompted by Philipp rejecting a round of theme-word mock-ups ("Yoke", "Bull", "Castle", ...) as "cringe": they
were gimmicks on the same thin box stack and ignored everything below.

## What makes them read as giant war machines

1. **Mass sits high and forward.** The torso is the biggest, widest, deepest block; it is carried forward of the
   hips on most Clan designs (the pelvis sits under the back half of the torso). Arms hang from the torso's top
   corners, often lower than the cockpit. Upright Inner Sphere humanoids (Atlas, Awesome, Rifleman) still have chests
   twice as wide as the hips.
2. **Legs are massive, not sticks.** Thighs and shins are armoured blocks almost as wide as the torso's side; knees
   carry a large armour cap; feet are long and wide with toes and a heel. Digitigrade legs (thigh raked forward, a
   backward "hock", long foot) are the genre's default for Clan mediums and heavies; plantigrade for IS humanoids.
3. **Two materials of shape.** Angular armour *plates* over round *machinery*: hip, knee and ankle drums, pistons,
   cable bundles, exposed actuators and joints in a darker value. The contrast between faceted plates and round
   joints is what makes them look built rather than modelled.
4. **Plates, not boxes.** Every armour piece has chamfered corners (45° cuts), sloped faces, and thickness; plates
   overlap like shingles, step back in layers, and carry recessed panels, vents and grilles. Almost no surface is a
   plain box face. Cross-sections are hexagons and octagons more often than squares.
5. **Weapons are big and part of the body.** A forearm is a gun housing with two or three fat muzzles; launchers
   are big rectangular boxes with a visible tube grid sitting on the shoulders; barrels are long and thick with
   muzzle shrouds and cooling sleeves. Weapons are the highest-contrast, most legible part of the silhouette.
6. **A face.** A cockpit canopy defines the character: a bulbous nose low and forward, a slit under a brow, a
   recessed window between shoulders. You read where the pilot sits.
7. **One signature, exaggerated.** Each design has one feature pushed hard (a dorsal cannon over a flat disc
   torso, two shoulder launchers, two huge claw arms, one huge shoulder gun on one side). Everything else is
   restrained so that feature carries the silhouette.
8. **Scale cues.** Small things on big surfaces make them big: antennas, rails and ladders, lamps, hazard stripes,
   unit numbers and insignia, vent slats, bolts. Without them a mech reads as a toy.
9. **Posture and stance.** Clan mechs lean into their hips (aggressive, predatory); IS assaults stand tall and square
   (imposing). Feet planted wide, knees bent.
10. **Paint and wear.** Camouflage and faction schemes in two or three colours, decals, numbers; heavy grime, chipped
    paint, scorch marks. Value: dark joints and legs, lighter armour plates, the brightest accents on weapons and
    cockpit.

## What IRONFEUD's mechs lack, by the same list

Thin stacked boxes for legs; small torsos; orthogonal box faces everywhere (no chamfers, slopes, layering); small
weapons hung beside the body; no face for most designs; no scale cues; one flat grey with orange trim. The armour
shader (bevel highlights, chipped edges, seams, grime) helps item 10 and a little of 4, nothing else.

## Implications for the build

- A richer primitive kit in `MechRig` before any redesign: chamfered box (octagonal-profile extrusion), wedge and
  slope plate, tapered block (frustum), hex/octagon prism, plate stacks, grilles and vent slats, muzzle clusters,
  tube-grid launchers, lamps and antennas, decals (numbers, stripes).
- Redesign one chassis first, judge it in the lineup against the principles above, then the rest.
- Keep the gameplay numbers (hit volumes, mounts, leg lengths) as the frame the art fits into.
