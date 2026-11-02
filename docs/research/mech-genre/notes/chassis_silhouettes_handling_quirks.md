# Chassis design per weight class: silhouettes, body plans, hitbox geometry, handling and quirks

Research date: 2026-11-02. Scope note: this pass got good primary or near-primary sources for MWO quirks and mobility
history, BattleTech design quirks, Armored Core VI leg-type mechanics, and the TF2 readability method. It got only
thin or no sources for Titanfall 2 art direction, Zoids/Front Mission/Heavy Gear/Gundam/Horizon body plans, MW4/MW5
numbers, and visual-scale-vs-tonnage practice. Those gaps are marked. Franchise mechs are named only as citations.
Everything under "Inferences" is framed as generic principles for original IRONFEUD chassis.

## (a) Recurring body-plan archetypes and how they map to roles and weight classes

### Takeaway
The archetype that recurs most clearly in the sources is Armored Core's leg-type split: biped all-rounder,
reverse-joint (digitigrade) jumper, multi-leg hover/stability platform, and tracked heavy. Each type trades load
capacity and stability against mobility and air time. In MWO the torso shape works as a second, independent axis:
"blimp-body" or "thin rod with high mounts" builds suit snipers, skinny humanoids with shield arms suit brawlers, and
tall-cockpit spider shapes suit scouts.

### Cited Findings
- AC6 leg types and their roles. Biped is "balanced for ground combat, air combat, and speed" with a low load limit
  and fairly light defence. Reverse joint is nimble and light and gets a free (no-energy) boost when launching off
  surfaces, but has lower load limit, defence and impact resistance than biped. Tetrapod locks elevation while
  hovering, drains energy much more slowly in hover, and "absorbs" weapon recoil while hovering. It has high load and
  defence but is slower in the air and a bigger target. Tank has strong damage absorption, drifts after a quick boost
  and gives "flat recoil reduction to weapons". It has high load, struggles in the air and is an easier target due to
  its size. — [Fextralife AC6 Legs](https://armoredcore6.wiki.fextralife.com/Legs)
- Representative AC6 leg stats show the trade-offs as numbers:
  - Reverse joint KASUAR/42Z: 3,400 AP, 530 attitude stability, 47,820 load limit, 19,060 weight.
  - Heavy biped VE-42A: 5,200 AP, 877 stability, 85,700 load, 28,950 weight.
  - Tetrapod VP-424: 5,600 AP, 1,366 stability, 69,800 load, 31,600 weight.
  - Tank LG-022T Bornemissza: 10,040 AP, 1,630 stability, 100,300 load, ~48,900 weight. One source says 49,800.
  - Exception: the reverse-joint RC-2000 Spring Chicken has a 68,360 load limit, so within-type variation is large.

  — [Charlie INTEL leg types](https://www.charlieintel.com/games/all-armored-core-6-leg-types-explained-bipedal-tetrapod-tank-more-267452/), [TheGamer best legs](https://www.thegamer.com/armored-core-6-best-legs-list/), [Fextralife](https://armoredcore6.wiki.fextralife.com/Legs)
- Across 13 AC6 legs the stat spread is wide: AP 3,400-10,040, stability 530-1,630, load 47,820-100,300, weight
  11,200-49,800. — [Fextralife AC6 Legs](https://armoredcore6.wiki.fextralife.com/Legs)
- Jump numbers from fandom snapshots (patch not stated): reverse-joint KASUAR/42Z has jump distance 386 and height 80,
  while the biped 06-041 MIND ALPHA has jump distance 83-103 and height 22-25. So reverse joints out-jump bipeds by
  roughly 3-4x. — [Armored Core Wiki KASUAR/42Z](https://armoredcore.fandom.com/wiki/KASUAR/42Z), [06-041 MIND ALPHA](https://armoredcore.fandom.com/wiki/06-041_MIND_ALPHA)
- Players say reverse joints "will always leave the ground after a dodge or jump" (bursty), and that basic bipeds
  don't suck at anything except jump height. This is anecdotal. — [Steam AC6 discussion](https://steamcommunity.com/app/1888160/discussions/0/3820795505273008998)
- AC6 patch 1.06 raised jump height on several bipeds and gave reverse joints more AP and stability plus weight
  cuts. The developer buffed the weaker leg type's weakness instead of nerfing the stronger one. — [Steam AC6 patch discussion via search](https://steamcommunity.com/app/1888160/discussions/0/4200238454189760494)
- MWO community archetypes by body shape:
  - Sniping: "blimp body" shapes appear "even smaller than they are" at long range, and "top mounts" make a
    purpose-built sniper.
  - Firing line at medium range (400-600 m): humanoids that hide parts behind cover, or round bodies that spread
    damage.
  - Brawling: skinny humanoids with a dedicated shield arm and arm-mounted guns. Arm guns let you "aim and shoot
    faster" than torso-mounted ones.
  - Scouting: cockpit "right at the top", so a scout can hide almost the whole mech, or very small and fast.

  — [MWO forums, "What Is Ideal Mech Geometry?" (2016)](https://mwomercs.com/forums/topic/229826-what-is-ideal-mech-geometry/)
- Titanfall 2 shares two frames across six titans. Northstar (hover sniper) and Ronin (fast melee hit-and-run) use
  the light Stryder frame. Scorch (area-denial fire) and Legion (minigun tank) use the heavy Ogre frame. — [Titanfall 2, Wikipedia](https://en.wikipedia.org/wiki/Titanfall_2), [Inverse loadouts guide](https://inverse.com/article/22999-titanfall-2-multiplayer-loadouts-guide)
- BattleTech treats limb layout as a gameplay quirk. Examples: "Low-Mounted Arms" (arms fire from lower and are
  blocked by partial cover), "Overhead Arms" (arm weapons fire from higher and can negate partial cover),
  "No/Minimal Arms" (no physical attacks, stand-up penalty) and "Directional Torso Mount" (a front-or-rear shoulder
  turret; quads get a full 360° turret). — [Sarna, List of Design Quirks](https://www.sarna.net/wiki/List_of_Design_Quirks)

### Inferences
- A clean archetype set for IRONFEUD's procedural rig, all original, could be:
  - **Plantigrade biped:** all-rounder, arms that can shield, good on rough ground.
  - **Digitigrade / reverse-joint biped:** jump and burst specialist, light load, poor stagger resistance.
  - **Multi-leg (4 or 6):** stable firing platform, absorbs recoil, wide footprint, slow turn, big side profile.
  - **Tracked or wheeled heavy:** maximum armour and load, no jump, weak on broken terrain.

  This maps to the existing rig: Hauberk = plantigrade, Sicklefoot = digitigrade. A quadruped is the most valuable
  addition because it creates a distinct "stability vs agility" axis.
- Give each weight class its own body plan rather than scaled copies. Body plans then cut across weight classes
  (for example a digitigrade light and a digitigrade heavy). The rule "bigger is not simply better" holds if each
  plan has a job, such as jump access, cover peeking, recoil absorption or terrain access, that tonnage alone
  cannot buy.
- Titanfall's pattern (two shared frames, three to six kits) suggests sharing rigs while varying silhouette
  additions (mounts, shoulders, head) and weapon kits. That fits a small hobby team.

### Gaps
- No sourced first-hand analysis was found for Zoids (animal body plans), Front Mission (wanzer part swapping),
  Heavy Gear (small agile gears with roller "secondary movement systems") or Horizon (animal-machine weak points).
  These descriptions come from training knowledge, not this research pass.
- No sourced Gundam/real-robot design-philosophy material was gathered.
- Exact AC6 per-leg travel speed and QB numbers were not found side by side. Only a tank travel speed of 194 for the
  EL-TL-11 Fortaleza turned up. — [Fextralife Fortaleza](https://armoredcore6.wiki.fextralife.com/EL-TL-11+FORTALEZA)

## (b) How geometry and hitboxes affect gameplay (MWO focus)

### Takeaway
In MWO, a chassis' value came largely from geometry, independent of stats. The traits that mattered:
- Where the hardpoints sit: high mounts let you peek over cover.
- How narrow the centre torso is.
- Whether side torsos are covered by arms that can actually absorb hits (shield arms).
- Overall profile: tall-and-thin versus compact or round.

Community lore says the ideal is a "thin rod" from the front with high-mounted weapons. Developers patched hitboxes
silently, so specific rankings drift between patches.

### Cited Findings
- Forum consensus on the ideal geometry: a thin rod with narrow torso hitboxes from the front, and side torsos
  covered by shield arms that only pivot on the Y-axis. One poster calls a high-mounted-ballistic assault design
  close to ideal. — [MWO forums via search summary](https://mwomercs.com/forums/topic/229826-what-is-ideal-mech-geometry/)
- Shield arms only work if the arm hitbox is big enough to catch shots. "Some mechs have miniscule tiny arm
  hitboxes", and a couple of chassis would make awful shield-arm mechs. You still have to twist to make enemies hit
  the arm. — [MWO forums, "How Much Armor Can A Shield Arm Shield?"](https://mwomercs.com/forums/topic/234748-how-much-armor-can-a-shield-arm-shield/), [MWO forums, Shields](https://mwomercs.com/forums/topic/174147-shields/)
- From the side, some round-bodied assaults are covered entirely by side-torso hitboxes, which spreads damage away
  from the centre torso. — [Rak's Armory weak-spots guide](http://raksarmory.blogspot.com/p/mechwarrior-online-mech-weakspots-guide.html)
- Mapping threads report that PGI edited hitboxes without telling the community, so per-mech rankings are
  patch-dependent. — [Taro's Hitbox Repository](https://mwomercs.com/forums/topic/216117-taros-hitbox-repository/), [Hitbox Localization 2](https://mwomercs.com/forums/topic/274181-hitbox-localization-2/)
- Players weigh hardpoint location and count above everything else. One user says it is the only geometric property
  he evaluates. Another notes that PGI can override bad geometry with hitboxes and quirks. — [MWO forums, Best Hitboxes](https://mwomercs.com/forums/topic/262240-best-hitboxes/)
- PGI's 2015 rebalance explicitly rated "geometric layout" next to loadout and agility when comparing variants: "Each
  variant for a given chassis is examined based on its loadout, agility, and geometric layout." This is quoted from
  Paul Inouye via the search summary of the PTS thread. — [MWO forums, Mech Rebalance And PTS](https://mwomercs.com/forums/topic/208441-mech-rebalance-and-pts/)
- BattleTech encodes geometry as quirks:
  - "Narrow/Low Profile" (3 pts): penalty to enemy ranged attacks.
  - "Oversized" (rebate 3, heavy/assault only): easier to hit, worse with terrain and cover. Mutually exclusive with
    Narrow/Low Profile.
  - "Cowl" (4 pts): +3 armour vs side and rear head hits.
  - "Low-Mounted Arms" / "Overhead Arms": interact with partial cover.
  - "Compact 'Mech": half transport space.

  — [Sarna, List of Design Quirks](https://www.sarna.net/wiki/List_of_Design_Quirks)
- Tabletop-to-real-time translation problem: random hit locations balance weapons on the tabletop, but aimed fire in
  MWO lets players stack damage on one spot, so "The incentive to boat is massive". This is why hitbox geometry
  matters so much more in a shooter. — [Sarna News, "The Challenge With Balance in MechWarrior Online" (~2018)](https://www.sarna.net/news/the-challenge-with-balanace-in-mechwarrior-online/amp/)

### Inferences
- IRONFEUD already models hit spheres per model, and the headless data showed Sicklefoot's smaller, lower spheres
  decided balance (CLAUDE.md lessons). Geometry is a primary balance lever, so treat these as per-chassis design
  parameters to tune and pin with tests:
  - centre-torso width vs side-torso width
  - arm sphere size (shield capacity)
  - hardpoint height above hip
  - cockpit height vs total height (peek ability)
  - frontal vs side cross-section
- Role-giving geometry trades:
  - High mounts and a tall cockpit: peek over cover. Pay with a tall target or a fragile head.
  - Round blimp body: spreads damage, small at range. Pay with no shield arm and low arm mounts.
  - Skinny humanoid with a big shield arm: brawler. Pay with less armour on the shield side.
  - Wide quad: stable. Pay with a big side profile, which works well with IRONFEUD's facing armour.
- With aimed fire, any chassis whose geometry hides its centre torso becomes the default pick unless something
  offsets it. Budget geometry advantages like stats (the BattleTech point-cost idea).

### Gaps
- Quantitative hitbox-area data (for example m² of CT per chassis) was not retrieved. Smurfy's mechlab and the
  hitbox repositories hold images and descriptions, not tables I could access.

## (c) Mobility profiles per class and chassis: numbers and formulas

### Takeaway
In MWO before 2017, top speed and agility (turn and torso-twist rates) both scaled with engine rating ÷ tonnage. That
caused an "engine arms race", so in 2017 PGI tied baseline agility to chassis tonnage and kept top speed tied to
the engine. Mobility quirks were folded into per-chassis baselines, tuned against peers in the same tonnage bracket.
IRONFEUD's equivalent: agility is a per-chassis, per-class property, decoupled from top speed.

### Cited Findings
- Community-derived MWO speed formula: speed (km/h) = engine rating / tonnage × 16.2. It checks out against an
  assault's unskilled top speed. — [MWO forums, engine ratings and speeds](https://mwomercs.com/forums/topic/146088-engine-ratings-and-speeds), [MWO forums, engines](https://mwomercs.com/forums/topic/154814-engines)
- Turn speed in the old game files: turn speed = 11.25 × engine rating / tonnage. MWO keeps separate rates for leg
  turn, torso pitch, torso yaw, arm pitch and arm yaw. — [MWO forums, engines thread (search summary)](https://mwomercs.com/forums/topic/154814-engines), [Victor torso turn rate thread](https://mwomercs.com/forums/topic/154833-victor-torso-turn-rate-the-same-as-an-atlas/page__view__findpost__p__3273071)
- PGI, February 2017 (quoted in the forum post):
  - "Too much was gained by upgrading Engines to higher ratings."
  - Engine restrictions drove baseline mech viability.
  - "Mobility will instead be determined by the overall tonnage of the chassis… this will not affect the speed of a
    'Mech."
  - Mobility becomes "roughly equal across tonnage lines". The slowest and fastest 30-tonners share mobility, and the
    two 100-tonners shared mobility.
  - Mobility quirks were removed from Inner Sphere mechs and Clan CT omnipods, and "integrated into the inherent
    Mobility attributes" and "adjusted against similar 'Mechs within their tonnage bracket". Example: one 45 t
    medium got higher baseline mobility than another 45 t medium.
  - Goal: "a drastic reduction in inherent 'Mech Quirks" alongside the Skill Tree.

  — [MWO forums, mobility decoupled from engine (2017)](https://mwomercs.com/forums/topic/245742-new-skill-tree-changes-as-well-as-mech-mobility-decuppled-from-engine/)
- Sensor range as a class-scaled mobility/info stat (2015 PTS Phase 2): baseline 500 m. Lights ~900 m, mediums
  ~800 m, heavies ~700-750 m, assaults ~500-650 m. Same PTS: ECM radius 180 m → 90 m, and ECM delays lock by 3 s
  instead of blocking it. — [MWO news, Mech Re-Balance PTS Phase 2 (Oct 2015)](https://mwomercs.com/news/2015/10/1370-mech-rebalance-pts-phase-2)
- AC6 "attitude stability" (resistance to stagger) scales with leg type: reverse joint 530, heavy biped 877,
  tetrapod 1,366, tank 1,630 (examples above). Tetrapod hover absorbs recoil, and tanks get flat recoil reduction. — [Fextralife AC6 Legs](https://armoredcore6.wiki.fextralife.com/Legs), [Charlie INTEL](https://www.charlieintel.com/games/all-armored-core-6-leg-types-explained-bipedal-tetrapod-tank-more-267452/)
- BattleTech treats mobility handling as quirks:
  - "Extended Torso Twist" (3 pts): one hex-side further.
  - "Poor Performance" (rebate 3): must spend a turn at reduced speed before full movement, effectively poor
    acceleration.
  - "Nimble Jumper" (3 pts): lights and mediums can deviate one hex while jumping.
  - "Stable" (2): bonus vs knockdown from physical attacks.
  - "Unbalanced" (1): piloting penalty.
  - "Hard to Pilot" / "Easy to Pilot".
  - "No Torso Twist": removed in Errata 6.0 (June 2022).

  — [Sarna, List of Design Quirks](https://www.sarna.net/wiki/List_of_Design_Quirks)

### Inferences
- For IRONFEUD, set per chassis: turn rate, torso-twist rate and range, arm yaw/pitch range, accel/decel, reverse
  speed and stagger/knockdown resistance. Set them per class first, then offset per chassis within a band. This
  copies the post-2017 MWO structure: one set of tonnage-band baselines, plus per-chassis offsets inside a class.
  It also avoids the "engine upgrade = strictly better" trap.
- Concrete levers that keep lights and mediums viable without making them bigger:
  - Faster twist and turn rates, so they can circle-strafe inside an assault's twist arc. This only works if
    assault twist rate × range can't track a light at close range.
  - Wider twist ranges on mediums. The "Extended Torso Twist" analog lets them fight from cover while moving.
  - Sensor range that falls with size (MWO's 900 m → 500-650 m). IRONFEUD already has scans and signature by size.
  - Stagger resistance that grows with weight and body plan (the AC6 stability analog), so assaults are hard to
    knock about but slow to recover their aim after turning.
- IRONFEUD's absolute speeds (130/80/60/40 km/h) match the genre band. MWO's formula gives a 100 t assault with a
  300 rating about 48.6 km/h, and a 35 t light with a 245 rating about 113 km/h. These are computed from the
  community formula, not quoted.

### Gaps
- No reliable MWO accel/decel formulas or tables were found. Smurfy's mechlab exposes per-chassis values but wasn't
  fetched.
- No MW4, MW5 or MW5: Clans mobility numbers were retrieved.
- Knockdown: MWO removed knockdowns early in its life (training knowledge, not sourced here). Reverse-speed ratios
  were not sourced.
- AC6 quick-boost / travel speed per leg type was not found in a comparable table.

## (d) Quirk systems: MWO, MW5 and BattleTech, and which designs created variety

### Takeaway
MWO used quirks first as the main balance tool for bad chassis. The quirks stacked into large min-max gaps, so PGI
wiped them (2015 PTS), moved mobility into baselines (2017), and from 2021 explicitly repositioned quirks as "flavour
and diversity" rather than primary balance. BattleTech design quirks are mostly small binary traits that pair a
positive with a negative, with point costs but no hard balance. Quirks that create variety change *how* a mech
plays (a cover, twist or jump behaviour). Quirks that make one build mandatory are large percentage buffs to one
weapon type on a weak chassis.

### Cited Findings
- 2015: PGI removed all mech and weapon quirks for the Phase 2 PTS. Only sensor-range quirks were kept, from a 500 m
  baseline. Stated goals were "re-balancing the 'Mechs across the board" and "effectively integrating Information
  and Role-based Warfare mechanics". — [MWO news, PTS Phase 2](https://mwomercs.com/news/2015/10/1370-mech-rebalance-pts-phase-2)
- Rationale (Paul Inouye, PTS thread, per search summary): quirk stacking produced "an ever-growing disparity between
  a standard 'Mech chassis and the min/max end result of quirked 'Mechs." Community criticism: removing IS weapon
  quirks gave the Clans a big range advantage, because the quirks had been patching tech-base imbalance. — [MWO forums, Mech Rebalance And PTS](https://mwomercs.com/forums/topic/208441-mech-rebalance-and-pts/), [mechspecs mirror](https://mechspecs.com/posts/77664/)
- 2017: mobility quirks were folded into baselines along with the Skill Tree. The stated aim was a "drastic reduction
  in inherent 'Mech Quirks". — [MWO forums, 2017 PGI post](https://mwomercs.com/forums/topic/245742-new-skill-tree-changes-as-well-as-mech-mobility-decuppled-from-engine/)
- 2021-2023: the balance push that began with the 20 April 2021 patch "reduced the reliance on quirks for primary
  balance and instead focused their use on diversity and flavor". By 2022 there had been nine quirk balance passes.
  The 2023 "Legends" rebrand added legendary variants with exclusive "outlier" quirks. — [MassivelyOP, 2022 patch](https://massivelyop.com/2022/04/22/mechwarrior-onlines-latest-patch-adjusts-weapons-and-equipment-makes-a-quirk-pass-adds-shiny-mechs/), [MassivelyOP, 2023 Legends](https://massivelyop.com/2023/04/17/mechwarrior-online-legends-adds-two-more-legendary-mechs-tweaks-maps-and-adjusts-chassis-quirks/), [Sarna, MWO Legends](https://www.sarna.net/wiki/MechWarrior_Online_Legends)
- BattleTech design quirks: optional rules, first formalised in *Strategic Operations* and expanded in the
  *BattleMech Manual*. They cover fluff features the core rules don't. There is no Battle Value for quirks. *StratOps*
  "encourages designers to balance positive quirks with negative ones of equal or greater value", but some canon
  designs don't. Some quirks have no battlefield effect, which can unbalance one-off games. — [Sarna, List of Design Quirks](https://www.sarna.net/wiki/List_of_Design_Quirks)
- BattleTech quirk categories worth copying as patterns, with point costs:
  - **Sensors/info:** Improved Sensors 3 (acts as an active probe). Sensor Ghosts −2. EM Interference −1 (energy
    fire blinds your own electronics for a round).
  - **Fire control:** Accurate/Inaccurate Weapon ±1 per 5 damage. Improved/Poor Targeting by range bracket. Variable
    Range Targeting (each turn pick a bracket: −1 there, +1 at the other). Multi-Trac. Stabilized Weapon (less
    running penalty).
  - **Heat:** Combat Computer 3 (−4 heat per turn). Improved/Poor/No Cooling Jacket per weapon. Cooling System Flaws
    −3.
  - **Body:** Narrow/Low Profile 3. Oversized −3. Cowl 4. Protected/Exposed Actuators. Reinforced/Weak Legs.
    Overhead/Low-Mounted Arms. Hyper-Extending Actuators (arms flip to the rear arc). Directional Torso Mount.
  - **Handling:** Stable, Unbalanced, Easy/Hard to Pilot, Nimble Jumper, Poor Performance, Extended Torso Twist.
  - **Command/morale:** Command BattleMech, Battle Computer, Distracting.
  - **Campaign/logistics:** Rugged (2-3× maintenance interval), Easy/Difficult to Maintain, Ubiquitous, Non-Standard
    Parts, Obsolete, Good/Bad Reputation (resale), Compact 'Mech (half transport space), Prototype, Ramshackle
    (random negative each battle), Cramped Cockpit, Difficult/No Ejection.

  — [Sarna, List of Design Quirks](https://www.sarna.net/wiki/List_of_Design_Quirks)
- Sarna community note: positive quirks can carry downsides and negative ones upsides, as long as the net balance
  holds. — [Sarna forums, Custom Design Quirks Listing](https://www.sarna.net/forums/showflat.php/Cat/0/Number/183686/Main/183470)

### Inferences
- Variety-creating quirks, to prefer:
  - Situational trade-offs where the bonus and the cost show up in different situations, like Variable Range
    Targeting (the pilot picks each turn), EM Interference, or Overhead vs Low-Mounted Arms (cover-dependent).
  - Body traits that change where and how you fight (cowl, twist range, nimble jumper).
  - Campaign/logistics quirks (rugged, compact for transport, upkeep). These push lights and mediums in the campaign
    layer without touching battle stats, which supports CLAUDE.md goal 4 directly.
- Build-mandating quirks, to avoid: large flat percentage buffs to one weapon family on a chassis (the MWO pattern
  that PGI later called "min/max" disparity). They tell the player exactly what to mount. If weapon quirks are used,
  make them small and tied to a hardpoint location rather than a weapon type. For example, "left-arm mount has a
  wider yaw" instead of "+25% laser range".
- Fix weak chassis through baseline geometry and agility first, quirks second (MWO's 2017 and 2021 lesson). Give
  every chassis at least one positive and one negative quirk, point-budgeted. Validate with the headless matrix
  rather than relying on the point budget, since BattleTech admits points don't truly balance.
- IRONFEUD's AI can read quirks. Quirks then become readable, situational behaviour: a "cramped cockpit" pilot
  reacts slower, and an "unstable" chassis avoids firing while sprinting. That supports the "readable, fallible
  enemies" pillar.

### Gaps
- MW5: Mercenaries / MW5: Clans quirk lists and their effects were not retrieved.
- No quantitative study was found of build diversity before and after MWO quirk passes (for example,
  meta-build share).

## (e) Silhouette and readability principles; visual coherence across a fleet

### Takeaway
Valve's TF2 method is the best-documented readability recipe:
- Design so the read hierarchy goes team (colour), then class (silhouette and proportions), then selected weapon
  (highest contrast at chest level, where the weapon is).
- Validate designs in pure silhouette at concept stage.
- Omit high-frequency detail.
- Make interior shapes echo the silhouette.
- Use a value gradient from dark feet to light chest.

### Cited Findings
- TF2 "Read Hierarchy": Team ("Friend or Foe?", colour) → Class ("Run or Attack?", distinctive silhouettes, body
  proportions, weapons) → Selected weapon ("What's he packin'?"). "Highest contrast at chest level, where weapon is
  held." "Gradient from dark feet to light chest." — [Valve, GDC 2008 "Stylization with a Purpose" slides](https://cdn.steamstatic.com/apps/valve/2008/GDC2008_StylizationWithAPurpose_TF2.pdf)
- TF2 character-creation pipeline: (1) character silhouette, (2) interior shapes, (3) model sheet, (4) 3D model,
  (5) skin, (6) in game. The silhouette is the "building block of character design" and must be "identifiable at
  first read". Designers "solve design problems using silhouette only", then solve the interior with shadow shapes.
  Team distinction comes through material hue, value and saturation. — [Valve GDC 2008 slides](https://cdn.steamstatic.com/apps/valve/2008/GDC2008_StylizationWithAPurpose_TF2.pdf)
- The nine classes' silhouettes were "carefully designed to be very distinct". Even "viewed only in silhouette with
  no internal shading at all" they are identifiable, and this test "was used to validate the character design during
  the concept phase". Body proportions, weapons and silhouette lines (footwear, hats, folds) were "explicitly
  designed to give each character a unique silhouette". Rules: omit high-frequency detail, make interior details
  echo silhouette shapes, and emphasise silhouettes with rim highlights rather than dark outlines. Players must
  identify others "at a variety of distances and viewpoints". — [Mitchell, Francke, Eng, "Illustrative Rendering in Team Fortress 2", NPAR/SIGGRAPH 2007](https://www.cs.princeton.edu/courses/archive/fall07/cos597B/papers/mitchell-team-fortress.pdf)
- Colours border on realism but with more saturation and value contrast. Muted colours dominate, with small areas of
  saturation. — [Mitchell et al. 2007](https://www.cs.princeton.edu/courses/archive/fall07/cos597B/papers/mitchell-team-fortress.pdf)
- Titanfall 2 keeps the fleet coherent by sharing frames: two titans each on the Stryder, Atlas and Ogre frames.
  Kits and add-ons differentiate the titans built on the same frame. — [Titanfall 2, Wikipedia](https://en.wikipedia.org/wiki/Titanfall_2)

### Inferences
- For IRONFEUD:
  - Each weight class needs a body-plan-level silhouette difference: leg count and joint direction, torso shape
    (tall-thin, blimp, wide-flat), and head/cockpit height. Size alone isn't enough, because size can't be judged at
    distance without a reference.
  - Run a silhouette test: render all chassis as flat black at 200-600 m on the No Man's Land skyline. Each should
    be identifiable by class and model.
  - Put weapon mounts at high contrast near the torso centre so the loadout (and so the threat) reads.
  - Darken legs and feet. This also helps the procedural gait read as weight, since the eye stays on the torso.
  - Fleet coherence: one faction shape language (for example, shared armour-plate bevel angles, panel rhythm and
    cockpit lens shape) across all chassis. Vary only the macro silhouette.
- Cockpit and weak-point placement double as readable intent: a high cockpit says "scout/peeker", and a big shield
  plate says "brawler". The silhouette then shows the gameplay role, and enemy roles become readable, as the
  CLAUDE.md pillars require.

### Gaps
- No Titanfall 2 developer interview on titan silhouette design was found in this pass. Searches returned only
  ability guides.
- No Overwatch GDC hero-readability talk or ArtStation mech concept-art article was retrieved. Armored Core
  part-silhouette design commentary was also not found.

## (f) Visual size vs tonnage, and apparent height vs hitbox

### Takeaway
No sources were found in this pass. The only sourced points are indirect:
- MWO's "blimp body" observation: some shapes look smaller than they are at range.
- BattleTech's Oversized and Narrow/Low Profile quirks, which make target size a tunable trait separate from
  tonnage.
- PGI's freedom to set hitboxes independently of the visual design.

### Cited Findings
- Shapes can read smaller at range than their volume suggests ("blimp body" mechs "appear even smaller than they
  are"). — [MWO forums, ideal geometry](https://mwomercs.com/forums/topic/229826-what-is-ideal-mech-geometry/)
- PGI "can give a mech whatever hitboxes and quirks it wants regardless of the design". Hitboxes are decoupled from
  the visual model. — [MWO forums, Best Hitboxes](https://mwomercs.com/forums/topic/262240-best-hitboxes/)
- BattleTech prices target size as a quirk: Narrow/Low Profile is +3, and Oversized is −3 and heavy/assault only.
  — [Sarna, List of Design Quirks](https://www.sarna.net/wiki/List_of_Design_Quirks)

### Inferences
- Mass scales with volume (~L³). If density stayed constant, the linear size ratio between a 4× heavier assault and
  a light would only be ∛4 ≈ 1.6×. Mechs that scale height linearly with tonnage look and play wrong, so scale
  height sub-linearly and width/depth more. This is a physics inference, not a sourced industry practice.
- IRONFEUD today scales one body per class. Giving each class its own proportions (light: tall, thin, high cockpit;
  assault: wide, squat, low head with cowl-like shoulders) separates "apparent size" from "hit area". The CT hit area
  can then be tuned per chassis, which matches the CLAUDE.md lesson that the hit spheres decided the measured
  balance.
- Keep the hitbox matched to the visual (spheres on the actual parts). MWO's community lists silently edited
  hitboxes as a grievance, and IRONFEUD's "only perceive what a pilot could see" AI rule implies that what you see is
  what you hit.

### Gaps
- No sourced mech heights per tonnage were found (MWO 2014-era rescale, MW5 scale, AC6 leg-type heights).
- No sourced discussion of camera framing and apparent size by class was found.
- These are worth a follow-up pass if visual scale becomes a design decision.
