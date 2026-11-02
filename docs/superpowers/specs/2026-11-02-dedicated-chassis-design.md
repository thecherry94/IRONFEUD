# Dedicated chassis per weight class (design, 2026-11-02)

Source: `docs/research/mech-genre/report.md` (chassis identity, handling numbers, draft slate). Philipp's brief:
"distinct silhouettes and handling per weight class; different mechs have quirks but in such a way that there isn't
one optimal weapon/sensor/utility loadout". Battle loop first: no missions/campaign in this milestone.

## Goals
- Each weight class gets its own body: proportions, silhouette, mount heights, hit volumes, armour split, handling.
  Heavy and assault stop being scaled copies of the medium.
- A design is "body plan + class stats + loadout", kept apart in code so a new chassis is mostly data.
- Every class stays viable: lance report, model duels and stuck-time report stay inside their limits.
- Original IP: names checked against the BattleTech wiki; silhouettes avoid franchise signature combinations.

## Non-goals (this milestone)
- JSON chassis files and a general mount list / section set (deferred: every new body here is a two-armed biped,
  which the current 2 arms + 1 rack + 5 sections already express; the loader is infrastructure nobody needs yet).
- Quadrupeds, armless bodies, reverse knees, melee, stability/bracing, campaign quirks (upkeep, transport).

## Decisions
- **Rename Hauberk -> Bascinet** (Hauberk is a BattleTech mech name). Bascinet stays the medium reference body.
- **Designs:** `MechModel` lists body plans; each has a native class. Rosters use native classes; `Create(cls, model)`
  still builds any combination (tests, range).

| Design | Class | Body plan | Silhouette cue | Handling vs band | Weapons (R / L / rack) |
|---|---|---|---|---|---|
| Sicklefoot | Light | digitigrade hull, nose cockpit, pods | long low hull | as today | MG / flamer / rockets, ECM |
| Bodkin | Light | digitigrade, upright compact drum torso, thin arms ending in long barrels | short torso on tall bent legs, barrels reaching forward | widest twist (+-110 deg), fastest twist | incendiary / pulse / rockets, designator |
| Bascinet | Medium | the prototype humanoid | boxy torso, pauldrons | as today | heavy cannon / laser / strike rack |
| Brigandine | Medium | plantigrade, shoulder bar ABOVE a low recessed cockpit | high level shoulder line, small chest slot | mid band | particle / autocannon / strike rack (arms ride high) |
| Harrow | Heavy | squat, wide stance, short thick legs, cockpit sunk between two shoulder humps, big forearms | low and broad, head hidden | slow, narrow twist (+-70 deg), quick off the mark | heavy cannon / heavy laser / arc missiles |
| Barbican | Assault | stubby legs, huge feet, very wide chest, shoulder line well above a visor slit | wide upper block on a short base | slowest, narrowest twist (+-60 deg) | siege cannon / heavy laser / point defense |

- **Handling from tonnage:** nominal tons light 30, medium 50, heavy 70, assault 95. Twist rate ~ tons^-0.75 (the
  current per-class rates already match within 3 %); twist limits now narrow with weight (research: assaults +-62-90
  deg vs lights far wider). Per-design offsets on top (Bodkin wider, Harrow and Barbican narrower).
- **Size:** `Scale` stays the size measure (authored at reference size, scaled per class). Spring frequencies of the
  secondary motion scale with 1/sqrt(Scale) so big machines sway slower (pendulum).
- **Quirks expressible today:** twist range/rate, armour split per section, hit-volume sizes, cockpit size and
  position, mount height (Brigandine fires over cover its low cockpit cannot see over: sight from the head, fire from
  the muzzles), signature. Later quirks (stability, brace, shove, melee, logistics) are recorded per design in code
  comments and wait for their systems.

## Verification
- Refactor steps are behaviour-preserving: all tests green and a fixed headless lance fingerprint unchanged.
- New bodies: ChassisTests (heights, reach, poses natural across No Man's Land), hit-volume sanity, captures
  (side-by-side lineup, silhouette sheet), then the balance reports with the new roster; buff the weaker side.

## Left to the builder
Exact proportions, colours and dressing of each body; loadout tuning from the balance runs.

## Built (2026-11-02) and deviations
- Brigandine: sight runs from the cockpit and the AI only fires at what it sees, so a cockpit lower than the guns would
  only blind it. Built instead as a hull-down shooter: arms high under the bar, cockpit just below it, so its guns clear
  a crest together with its eyes. Particle cannon + autocannon; armour factor 1.3 like the Bascinet (1.2 lost every
  duel series).
- Harrow and Barbican: legs ~10 % longer and hips narrower than first drafted (planted feet were off target 8-10 % of
  the time across No Man's Land); they keep the squat look by crouching deeper (stance extension 0.75).
- Lineup check: `--lineup` (all designs in a row). Balance: medium duels 21-19 / 18-22 / 22-18 / 21-18; lance kills
  per game light 0.66, medium 0.57, heavy 0.85-0.97, assault 0.55, survival 31-40 % each (80 games).
