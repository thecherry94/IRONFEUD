# Loadout / customisation systems in mech games and balance lessons against a single optimal loadout

Scope: mechlab rules (MW2-4, MWO, MW5, HBS BattleTech, Armored Core VI), the meta builds they produced, the fixes tried,
theory sources on keeping options viable, utility equipment, and single-player vs PvP balance.
Franchise item names appear only as citations. Everything transferable is framed for an original game (IRONFEUD).
Source quality note: much of the genre's balance history exists only as forum posts and fan wikis. Primary developer
sources found: Piranha's 2013 heat-scale post, the 2015 MWO rebalance PTS notes, MWO 2013 patch notes (jump-jet
shake), an HBS 2018 dev Q&A, the Bandai Namco AC6 1.07 patch notes, and Sirlin's own articles. The rest is community
material and is labelled as such. This run made about 25 tool calls (budget ~15), so several sub-questions were left as gaps.

## 1. Mechlab rules per game: what each constrained, and why

### Takeaway
The genre moved from free-form mechlabs (MW2/MW3: any weapon nearly anywhere, so a few optimal builds) to **typed
hardpoints** (MW4 slots by type, with omni and dual-type slots), then to **typed hardpoint counts per variant plus
separate critical slots and tonnage** (MWO), then **typed and sized hardpoints** (MW5). Each step exists to make the
chassis, not only the weapon list, decide the build. Armored Core VI drops weapon-type hardpoints entirely and uses
several **interlocking budgets** instead (leg load limit, arm load, generator EN output, EN capacity, stagger stability).

### Cited Findings
- **MW2 / MW3 (free-form):** a player retrospective says the MW2 mechlab was so open that it undercut variety, and
  describes MW3's as similarly open — [backloggd review](https://backloggd.com/u/Cappn/likes/). A community recollection
  of MW2: Mercenaries says side torsos had 12 free critical slots each, with fewer in the centre torso and legs —
  [MWO forums Q&A thread](https://mwomercs.com/forums/topic/7024-community-qa-6-mechlab/page__st__40). Players remember
  MW2 Mercs letting you swap weapons freely across a mech — [MWO forums](https://mwomercs.com/forums/topic/7085-gyros-and-engines/nextunread).
  (No primary MW3 manual source found: see Gaps.)
- **MW4:** a forum commenter says MW4 merged weapon type and critical slots into one "critical slot type" system, while
  MWO kept them separate — [MWO forums](https://mwomercs.com/forums/topic/7085-gyros-and-engines/nextunread). MW4 mechs had
  omni slots and dual-type slots (energy/missile, missile/ballistic, energy/ballistic) — [Steam discussion](https://steamcommunity.com/app/342200/discussions/0/2579854400752878423).
  A 2013 proposal describes MW4 omni tech as generic hardpoints that any weapon type could fill —
  [MWO forums](https://mwomercs.com/forums/topic/5980-a-proposal-for-combining-the-mw4-hardpoint-system-with-cbt-build-rules/page__st__140).
- **MWO:** every weapon uses exactly one hardpoint, but its critical-slot cost varies (hardpoint count and slot count
  are separate constraints) — [Steam discussion](https://steamcommunity.com/app/784080/discussions/0/4337609830360929525).
  The original design is in Piranha's 2012 [Dev Blog 6 - MechLab](https://wwww.mwomercs.com/news/2012/04/193-dev-blog-6-mechlab)
  and [Engadget's summary](https://www.engadget.com/2012-04-04-step-inside-mechwarrior-onlines-mechlab-and-choose-your-weapon.html)
  (not fetched in detail). Per-variant quirks exist in two kinds, generic (energy/ballistic/missile) and weapon-specific —
  [MWO forums](https://mwomercs.com/forums/topic/208811-concept-quirks-based-on-roles/).
- **MW5 (Mercenaries / Clans):** hardpoints are typed (ballistic/missile/energy) *and* sized. Size caps the largest
  weapon a hardpoint accepts. Non-omni chassis lock each hardpoint to one type; omnimechs accept any type —
  [Steam MW5 thread](https://steamcommunity.com/app/784080/discussions/0/4337609830360929525). Example: a chassis with 12
  energy hardpoints, none of which fit a 2-slot large energy weapon —
  [Steam MW5: Clans thread](https://steamcommunity.com/app/2000890/discussions/0/4691154153116798972). The popular YAML
  mod removes hardpoint size limits, which players say causes "0 issues" — same thread
  ([Steam workshop](https://steamcommunity.com/workshop/filedetails/discussion/2549720490/3768985348695109497)). This is
  community testimony that size limits are felt as friction in single-player.
- **MW5: Clans omni effect:** players say omnis favour the single most efficient design, and that Clans has 16 mechs
  against about 50 core mechs in Mercenaries — [Steam MW5: Clans "Every Single Meta Build: LASERBOAT"](https://steamcommunity.com/app/2000890/discussions/0/4629230582747973584/).
  One poster counters that Mercenaries' variety came *only* from hardpoint and slot restrictions — same thread.
- **Armored Core VI budgets** (fextralife wiki):
  - Total Load (everything except the legs) must stay within the legs' Load Limit, or the AC "cannot sortie"
    (an OS upgrade can bypass this).
  - Arms Load over the Arms Load Limit is allowed, but it lowers target tracking and recoil control (a soft penalty).
  - Total EN Load must stay within the generator's EN Output, or no sortie (a hard cap).
  - EN Capacity, recharge and supply efficiency limit boosting and energy weapons in use.
  - Attitude Stability (summed over frame parts) sets how hard the AC is to stagger. Attitude Recovery improves at
    lower weight.
  - Firearm Specialization on the arms feeds Target Tracking.
  — [Armored Core 6 wiki: Stats](https://armoredcore6.wiki.fextralife.com/Stats)

### Inferences
- There are three kinds of constraint, and they do different jobs. **Hard caps** (tonnage, EN output, leg load) define
  the feasible set. **Soft penalties** (arm overload lowering tracking, heat) let players trade inside it. **Typed and
  sized slots on a chassis** give each chassis its own feasible set, which is what produces per-chassis identity.
  AC6 shows that you can get identity without typed hardpoints if several *orthogonal* budgets bind on different parts
  (legs, arms, generator).
- The MW5 Clans vs Mercenaries comparison suggests that hardpoint restriction does more for build diversity than
  weapon balance does. When restrictions are loosened (omnis, size-removing mods), builds converge on the
  best-efficiency weapon.
- For IRONFEUD's design goal of per-mech quirks and no single optimal loadout: fixed or semi-fixed mounts per model
  (type + size + location + firing arc/height) are the strongest lever. A free mechlab is the weakest.

### Gaps
- No primary source (manual) found for MW3's exact mechlab rules or MW4's slot sizes. They are only described in
  community recollections.
- MWO's current engine caps / Omnipod rules, and MW5's official hardpoint-size documentation, were not fetched.
- HBS BattleTech's mechlab rules (typed hardpoints per location, tonnage, slots; no sizes) were not sourced in this run.

## 2. Meta builds per game, their causes, the fixes tried, and how players judged them

### Takeaway
The dominant metas share one root cause: **damage that is delivered instantly to one point, at range, from behind
cover** (MWO PPC+gauss "poptarts", high-count alphas; AC6 stagger-burst combos). The other root cause is **one weapon
family with no logistics downside** (MW5 laser boats: no ammo, no spread, rapid cooldown). Fixes were mostly
*systemic taxes* (ghost heat, jump-jet shake, cooldowns, spread) plus *per-chassis quirks*. Players accepted the
quirks as a way to create identity, and resented ghost heat as opaque and as punishing whole builds.

### Cited Findings
- **MWO heat scale ("ghost heat"), July 2013.** Paul Inouye (Piranha) introduced a "Max Alpha" per weapon type: firing
  more than that many in one group makes heat "grow exponentially". The first table:

  | Weapon | Max Alpha |
  |---|---|
  | PPC | 2 |
  | ERPPC | 2 |
  | LRM-15 | 2 |
  | Large Laser | 2 |
  | Medium Laser | 6 |
  | AC/20 | 1 |
  | SRM-2 | 4 |
  | SSRM-2 | 4 |
  | SRM-4 | 4 |
  | SRM-6 | 3 |

  He framed it as "very experimental" and aimed at "the immediate effect on the current meta-game". In the same post he
  refused an SRM damage buff to 2.5 because a 6×SRM6 build "will decimate any Assault class Mech in 3 volleys", and
  dropped the overheat-damage threshold from 120% to 100% —
  [MWO forums, "Heat Scale and General Update"](https://mwomercs.com/forums/topic/126651-heat-scale-and-general-update/).
- The fan wiki states the purpose as reducing "alpha strike builds (maximizing damage per volley) and high DPS builds",
  with each additional weapon adding more heat than the last — [MWO wiki: Heat Scaling](https://mwo.fandom.com/wiki/Heat_Scaling).
- **Cross-type linking.** Heat-scale groups were later linked across types: a gauss fired with PPCs is billed as if it
  had the hottest weapon's heat. Players said this "squashed many Gauss-PPC builds" — [MWO forums](https://mwomercs.com/forums/topic/255356-ghost-heat);
  [MWO forums PPC thread](https://mwomercs.com/forums/topic/209578-are-ppcs-and-large-lasers-linked-in-heat-scale).
  Critics called it an item whose best counter is itself, and complained that the system is opaque. Community heat
  simulators exist because the rule is hard to see in-game —
  [nav-alpha ghost-heat tool](https://mwo.nav-alpha.com/equipment/ghostheat), [heat simulator](http://keikun17.github.io/mwo_heat_simulator/).
- **Poptarting.** In 2013 players described the build that jump-jets up, "rips off a ppc or two in conjunction with a
  guass rifle", and drops back behind cover — [MWO forums](https://mwomercs.com/forums/topic/113540-sniperpoptart-problems-mechwarrior-3-has-your-solution).
  When gauss was taxed the meta moved to PPC+AC5 ("very similar projectile speed"), so the "meta mech" became jump
  jets + PPCs + AC5s — [MWO forums](https://mwomercs.com/forums/topic/164805-noob-after-a-definition/TimePeriod+).
  Fixes:
  - June 2013 patch 1.2.226 added screen shake, crosshair shake and random spread while jump jets fire —
    [patch notes thread](https://mwomercs.com/forums/topic/120882-patch-notes-12226-04-jun-2013).
  - The July 2014 Jump Jet Update reworked jet heat and thrust, so one jet gave almost as much thrust as four —
    [MWO forums](https://mwomercs.com/forums/topic/164389-jump-jet-update).
- **Matched projectile speeds** made mixed weapons act like one pinpoint weapon. The meta paired weapons whose
  projectiles travel at similar speeds, so they converge on one point at range — [same thread](https://mwomercs.com/forums/topic/164805-noob-after-a-definition/TimePeriod+).
- **MWO 2015 rebalance (PTS 4)** stated its goal as reducing "the friction points between the 'Mechs that are commonly
  fielded". It worked through ranges, cooldowns, spread and quirks, not damage:
  - Clan ER medium laser max range 810 → 688 m; ER small 400 → 360 m.
  - Gauss cooldown 4.0 → 5.5 s; IS LRM20 4.75 → 5.5 s; Clan LRM20 5.0 → 6.5 s.
  - LRM spread tightened; SRM speed 300 → 400 m/s; AC/2 heat 1.0 → 0.8.
  - IS medium pulse lasers given medium-laser heat scaling.
  - Mobility "efficiencies" cut hard (e.g. twist 10/20% → 2.5%).
  - Percentage armour quirks replaced by flat per-location armour.
  - Clan XL engines lose 20% engine power when a side torso is lost.
  - ECM protective range 180 → 90 m.
  - A planned "reduced optimal range on non-locked targets" rule was dropped. Information-warfare features (size-based
    sensor profiles, shared info, aggregated targeting) were held back because "the systems simply need a lot more time
    to tune and get right".

  — [MWO News: Rebalance PTS 4 (Nov 2015)](https://mwomercs.com/news/2015/11/1403-rebalance-pts-4)
- **Quirks** were later contested. A thread reports a plan to remove or reduce weapon quirks to simplify testing of
  infowar quirks, and a reply notes this was unconfirmed — [MWO forums](https://mwomercs.com/forums/topic/205325-changes-removal-of-quirk-system/);
  quirk list snapshot (2015) — [mechspecs quirks](https://www.mechspecs.com/quirks/).
- **Community proposals for non-alpha play** (a "what would it take to popularize non-alpha builds" thread) show the
  alpha meta persisted into the late MWO era — [MWO forums](https://mwomercs.com/forums/topic/282913-what-would-it-take-to-popularize-non-alpha-strike-builds/).
- **MW5: Clans laser boats.** Players list laser boats as "every single meta build". Reasons given:
  - lasers shred faster than PPC or gauss and are "super forgiving if you miss with a rapid cooldown";
  - missiles carry "high heat and weight requirements" and "waste ammo" through spread;
  - ammo needs tonnage ("five tons of ammo will never ever fit");
  - small maps with "tight corners" keep most fights under 500 m.

  Counterpoints: one player "beat the game on expert using a mix of everything"; another says the meta only reflects
  max-damage optimisation — [Steam MW5: Clans](https://steamcommunity.com/app/2000890/discussions/0/4629230582747973584/).
  Another player says laser boating makes the game "boring and easier" and that overheating punishes less than in
  earlier games — [Steam MW5: Clans](https://steamcommunity.com/app/2000890/discussions/0/4691154661545222251). A
  Mercenaries balance mod *raises* laser damage and reworks UACs, and its author still finds one missile type
  overpowered — [TA Balance Mod, Nexus](https://www.nexusmods.com/mechwarrior5mercenaries/mods/1335).
- **HBS BattleTech:**
  - A 2018 player documents a 415-damage alpha strike from heavy autocannons + SRMs + medium lasers, but notes that one
    mech alone still needs a second mech to finish a knockdown — [Steam](https://steamcommunity.com/app/637090/discussions/0/1696046342874219248).
  - LRM lances are used to knock targets down for called shots that preserve salvage — [Steam](https://steamcommunity.com/app/637090/discussions/0/1697175413670359725).
  - A Nexus guide pairs an LRM boat (destabilise) with an SRM boat (knock down) — [Nexus Mods](https://www.nexusmods.com/battletech/mods/233).
  - A recent (1.9.1) community guide says HBS cut missile raw damage but gave missiles high stability damage, and that
    SRMs lost a per-shot hit bonus. It ranks rapid-fire autocannons > snub PPC > SRMs > lasers > LRMs — [233leyuan guide](https://www.233leyuan.com/post-detail/2022657167324962816)
    (community source, translated summary).
- **Armored Core VI:**
  - Patch 1.03.1 (Oct 2023) nerfed the dominant dual shotgun + dual shoulder-grenade-cannon stagger-burst combo and
    buffed underused weapons (e.g. more range and rounds, shorter reload on one autoshotgun) —
    [GameSpot](https://Gamespot.Com/articles/major-armored-core-6-balance-update-nerfs-its-most-op-combo-full-patch-notes/1100-6518203/)
    (fetch blocked; summary from search snippet), [Yahoo/Kotaku](https://malaysia.news.yahoo.com/major-patch-nerfs-armored-core-161000363.html).
  - Patch 1.02 adjusted bosses (Balteus etc.) and Automaton measured the effects — [Automaton](https://automaton-media.com/en/?p=21577).
  - Patch 1.07's official notes: "adjust game balance". Most of the changes are **buffs**:
    - weapons: lasers, many rifles and machine guns (mostly projectile speed), missile lock and reload times;
    - frame parts: stability, AP and generator output;
    - arms: melee specialization;
    - nerfs are few (plasma thrower, a burst machine gun, a stun-bomb launcher, a few frames and generators).

    — [Bandai Namco EU: AC6 patch notes 1.07](https://en.bandainamcoent.eu/armored-core/news/armored-core-vi-fires-of-rubicon-patch-notes-107)
  - Later patches: 1.007 (Sept 2024) — [MP1st](https://mp1st.com/title-updates-and-patches/armored-core-vi-update-1-007-released-for-balance-adjustments-this-sept-19).

### Inferences
- **Pinpoint + range + cover is the genre's recurring dominant pattern.** Every damage source that lands
  instantly on one hit location from behind cover becomes meta, because it beats a section-damage model: it
  concentrates damage on one section, and the exposure time to return fire is close to zero. MWO's fixes taxed the
  *symptom* (volley size, airborne accuracy), and the meta moved to the next matching pair (PPC+AC5).
- **Systemic taxes are disliked when they are invisible.** Ghost heat needed external simulators. A visible rule
  (screen shake while jetting, spread, an on-HUD heat projection) was accepted better. For IRONFEUD: if a volley tax
  exists, show it before the shot.
- **AC6 buffed far more than it nerfed** (1.07). This matches IRONFEUD's "buff the weaker option" pillar, and shows
  that a long-lived single-player/PvP hybrid can keep diversity mainly by raising the floor.
- **MW5 shows that the weapon with no logistics cost wins in single-player.** Lasers have no ammo, no spread and a
  short cooldown. If heat punishes too little, nothing balances them. Make heat bite, or give the ammo weapons a
  payoff that lasers can't match (burst, armour-stripping, stability/knockdown as in HBS BattleTech, indirect fire).
- **HBS BattleTech's fix for "missiles are weak" was a role, not raw damage:** stability damage leads to knockdown,
  then called shots. Complementary roles across a lance (destabiliser + finisher) are a good model for IRONFEUD's lances.

### Gaps
- No primary source for MWO's later "Skill Tree" (2017) effects on builds, nor for the Gauss charge mechanic.
- MWO cone-of-fire / spread mechanics (other than jump-jet spread) and Piranha "Command Chair" posts on quirks were not
  found in this run.
- MW4 meta (e.g. which builds dominated online ladders) was not sourced.
- No sourced dev statement on how AC6 judged stagger in balance (official 1.07 notes list stagger only as bug fixes).

## 3. Design techniques and theory that keep multiple loadouts viable

### Takeaway
The theory says to aim for many viable options at high skill, no dominant move, and counters three layers deep. Give
options *different kinds of effects*, not just different damage, speed and cost. Use **soft** counters (an edge, not a
hard win), and judge balance globally (lance vs lance, mission vs mission) instead of in every skirmish. In mech terms:
range bands with falloff, separate heat/ammo/weight/energy budgets, DoT vs pinpoint, defensive utility that answers
specific threats, and per-chassis mount geometry.

### Cited Findings
- **Sirlin, Balancing Multiplayer Games:**
  - Part 1 defines a balanced game as one where a reasonably large number of options are viable, especially in
    high-level play — [Sirlin Part 1](https://www.sirlin.net/articles/balancing-multiplayer-games-part-1-definitions).
  - Part 2 points:
    - a dominant move, "a move that is strictly better than any other you could do", is the worst thing a competitive
      game can have;
    - every good move needs a counter, and play should support "Yomi Layer 3", the counter to the counter;
    - balance is **global, not local**: a single skirmish may be lopsided if the overall matchup is fair;
    - touch "as many corners of the design space as possible", with options that differ in *effect*, not just in
      damage, speed and cost (his Kongai example);
    - remove "chaff" (non-viable options): MvC2 has 54 characters and about 10 viable, against SSF2T with nearly all 16 viable;
    - "Weaker moves become inherently better in a double-blind game";
    - be sceptical of playtests, but theory doesn't replace them.

    — [Sirlin Part 2: Viable Options](https://sirlin.net/articles/balancing-multiplayer-games-part-2-viable-options)
  - Part 3 covers fairness among starting options (asymmetric starts are harder to balance) —
    [Sirlin Part 3](https://www.sirlin.net/articles/balancing-multiplayer-games-part-3-fairness).
  - A related Game Developer article covers "opportunity, power and relativity" — [Game Developer](https://www.gamedeveloper.com/design/balancing-multiplayer-games-opportunity-power-and-relativity).
- **Intransitivity.** A lecture deck calls a balanced three-way intransitive relationship (A beats B beats C beats A) a
  classic technique against dominant strategies — [SlideShare: Fundamentals of Game Design](https://www.slideshare.net/slideshow/game-balancing-66908546/66908546).
  A 2020 paper says balanced intransitive relationships are critical to strategic depth in MOBAs and form the
  metagame — [IADIS digital library](https://www.iadisportal.org/digital-library/balancing-intransitive-relationships-in-moba-games-using-deep-reinforcement-learning).
  Balancing notes warn that, unless done carefully, intransitive design can make the game feel like "glorified
  rock-paper-scissors" — [Mechanics of Magic: Game Balancing Notes](https://mechanicsofmagic.com/2023/11/29/game-balancing-notes/).
- **Soft counters.** Game Maker's Toolkit's "How Games Get Balanced" starts from RPS (nothing is overpowered because
  everything has a counter), then explains soft counters, where one option usually beats another but not always —
  [GMTK transcript (Amara)](https://amara.org/subtitles/53cqjOHT6p9I/en/1/download/How%20Games%20Get%20Balanced%20%20Game%20Makers%20Toolkit.en.txt).
- **Single-player counters.** One analysis argues that against single-player enemies there can at best be soft counters
  that make things harder depending on the player's choice — [critpoints.net](https://critpoints.net/2017/02/page/3).
- **Quantifying metagame balance.** A GDC talk ("Metagame", from balancing PlayStation All-Stars) presents a
  principled way to measure the strength of asymmetric options — [GDC Vault](https://gdcvault.com/play/1022156/Metagame)
  (content not fetched).
- **Mech-genre instances of these techniques (from section 2):**
  - range falloff and cooldown changes as the main 2015 MWO levers — [PTS4](https://mwomercs.com/news/2015/11/1403-rebalance-pts-4);
  - weight/ammo budgets keeping missiles honest but over-tight in MW5 — [Steam](https://steamcommunity.com/app/2000890/discussions/0/4629230582747973584/);
  - stability damage giving missiles a role in HBS BattleTech — [Steam](https://steamcommunity.com/app/637090/discussions/0/1697175413670359725);
  - separate leg / arm / EN budgets in AC6 — [fextralife](https://armoredcore6.wiki.fextralife.com/Stats).

### Inferences (applied to IRONFEUD's weapon/sensor set)
- **Pinpoint vs spread vs DoT as a triangle.** Pinpoint (cannons, siege cannon, particle bolt) wins against fragile
  sections and through cover peeks. Spread and DoT (lasers held on target, machine guns, flamer, incendiary, rocket pods)
  win against agile targets and strip armour widely, but lose to cover-peeking. Point defense and facing armour
  counter missiles and pinpoint respectively. Keep the edges *soft*: a 55-65% expected win, not a 90% hard counter.
- **Make the counter-to-the-counter exist (Yomi Layer 3).** Example chain: a guided-missile lock is countered by
  weaving and ECM; ECM is countered by a designator mark or by firing (which reveals the light); a mark is countered by
  breaking line of sight. Each link should be a situational choice, not a fixed loadout slot.
- **Different kinds of effect, not different numbers.** Heat-into-target, sensor disruption, stability/knockdown,
  armour-strip vs crit-seek, and indirect fire are distinct axes. A weapon that only differs in DPS/range becomes
  chaff or the dominant option.
- **Global over local balance.** Judge balance per lance composition × mission type (as CLAUDE.md already requires),
  not per 1v1 duel. A light losing every 1v1 to an assault is acceptable if lights win scouting, objective and
  flanking missions.
- **Anti-pinpoint levers that suit IRONFEUD's physics:**
  - **Convergence on torso aim plus per-mount geometry.** Arm and shoulder mounts at different heights and offsets
    converge differently, so mixed-mount volleys spread across sections.
  - **Mismatched projectile speeds.** Avoid two weapon families that share one speed band, or they fuse into one
    pinpoint volley (MWO's PPC+AC5 lesson).
  - **Jump and recoil instability as visible accuracy cost** (MWO's 2013 shake fix, which players could read).
  - **A volley heat tax shown before firing,** if one is needed at all.
- **Separate budgets for separate weapon families.** Heat for energy weapons; ammo and tonnage for ballistics and
  missiles; an energy/capacitor budget, if added, for burst weapons. Each family should hit a *different* limit, so
  boating one family runs into its own wall, while mixing spreads load across budgets (the AC6 lesson).
- **Situational utility needs situational missions.** ECM, designators, point defense and sensor scans only stay
  meaningful if mission and enemy composition vary (missile-heavy enemies one mission, close-quarters urban the next).

### Gaps
- No GDC talk specifically on mech-game loadout balance was found. The "Metagame" GDC talk's content was not read.
- Magic: The Gathering R&D's writing (e.g. on "linear" vs "modular" design or power-level bands) was not sourced in this run.

## 4. Utility and sensor equipment: mandatory vs ignored, and why

### Takeaway
In MWO the stealth/ECM item was the textbook "mandatory" utility. It was too strong because it bundled several jobs
(stealth bubble, counter-ECM, lock denial) into one cheap slot, and its best counter was itself. It was cut back over
years (e.g. bubble radius halved in 2015). Jump jets became near-mandatory for the poptart meta until their accuracy
and thrust were taxed. Other utility items lacked sourced evidence in this run.

### Cited Findings
- **ECM (MWO).**
  - Players complained in late 2012. A dev post describes counter mode as able to cancel only one enemy ECM and give
    none of disrupt mode's benefits — [MWO forums](https://mwomercs.com/forums/topic/78080-guardian-ecm-suite).
  - A Dec 2012 forum post quotes a developer raising the target-designator range from 450 to 750 m as a counter, and
    mentions a proposed one-ECM-per-mech limit (implementation not confirmed) — [MWO forums](https://mwomercs.com/forums/topic/81362-think-ecm-is-unbalanced-tell-me-how-to-fix-it/page__st__40).
  - A 2013 critique: the item was "forced to wear too many hats", great as a shield and terrible as counter-ECM —
    [MWO forums](https://mwomercs.com/forums/topic/110187-the-state-of-guardian-ecm).
  - Posters called an item whose best counter is itself "BAD GAME DESIGN" — [MWO forums](https://mwomercs.com/forums/topic/99703-ecm-is-fine-and-has-plenty-of-counters-and-will-continue-to-have-more-counters-as-time-goes-by/page__st__80).
  - A 2015 thread blamed sensor and targeting design for much of ECM's strength — [MWO forums](https://mwomercs.com/forums/topic/203955-ecm-change-feedback/page__st__340).
  - ECM protective range was cut from 180 to 90 m in the 2015 rebalance — [MWO News PTS4](https://mwomercs.com/news/2015/11/1403-rebalance-pts-4).
- **Jump jets (MWO):** the 2013 accuracy shake and spread, then the 2014 thrust and heat rework, were aimed at the
  jump-sniping meta — [patch 1.2.226](https://mwomercs.com/forums/topic/120882-patch-notes-12226-04-jun-2013),
  [Jump Jet Update](https://mwomercs.com/forums/topic/164389-jump-jet-update).
- **Information warfare as a balancing tool:** MWO planned size-based sensor profiles and shared target information
  in 2015, but held them back because they needed more tuning time — [PTS4](https://mwomercs.com/news/2015/11/1403-rebalance-pts-4).
- **Coolant:** an MWO forum discussion about coolant flush as a consumable exists —
  [MWO forums](https://mwomercs.com/forums/topic/4727-the-coolant-discussion/nextunread) (not read in detail).

### Inferences
- **Utility becomes mandatory when it is cheap, always useful, and multi-role,** and when the counter is the same
  item. Utility gets ignored when its threat is absent in most missions. IRONFEUD's ECM (Sicklefoot light only) and
  designator marks (lights) are already model- and class-bound, which avoids the "every mech carries ECM" failure.
  Keep ECM single-purpose (lock and scan denial in a small radius) and keep its counters *different* items: a
  designator mark, firing to reveal the light, scans, or line-of-sight.
- **Size-based signature plus shared lance scans** (already in IRONFEUD) is the information-warfare layer MWO planned
  but shelved. It makes sensors a team-composition question rather than a per-mech checkbox, which is good for
  "lights keep jobs assaults can't do".
- **Point defense** is only meaningful if missile-heavy enemy lances appear often enough. Vary enemy composition per
  mission so PD is sometimes crucial and sometimes dead weight. That is a healthy situational choice, not chaff.

### Gaps
- No sourced evidence in this run on which other MWO/MW5/HBS utility items players treated as mandatory or ignored:
  the active probe / BAP, AMS, TAG and NARC, targeting computers, advanced zoom, the MASC and TSM speed boosts.
  Community wisdom (e.g. AMS widely carried in high-LRM eras, BAP as a soft ECM counter, targeting computers favoured
  on Clan mechs) is commonly repeated but was not verified here.

## 5. Single-player vs PvP balance philosophy, and campaign scarcity

### Takeaway
Against AI, "overpowered fun" is tolerated, and modders push it further (MW5 mods removing hardpoint limits). Yet
players still complain when one build trivialises everything (MW5 Clans laser boats "boring and easier"). Campaigns
shape loadouts mainly through **scarcity of chassis and parts** (HBS BattleTech's 3-part salvage, salvage pick limits,
expensive parts against poor resale) rather than through weapon balance.

### Cited Findings
- **HBS BattleTech economy (Feb 2018 dev Q&A):** mechs come as salvage parts, and three parts make a mech. Whole mechs
  can be bought but are "not cheap", "You won't get rich selling mech parts", and your own destroyed mech's parts are
  not added to the salvage pool — [Paradox forum dev Q&A write-up](https://admin-forum.paradoxplaza.com/forum/threads/feb-2018-dev-q-a-write-up.1071495).
- **How a mech dies sets the salvage:** players report that a centre-torso kill gives one part, two legs give two, and
  a pilot kill gives three. This pushes a playstyle (called shots, headshots, knockdowns) — [Steam](https://steamcommunity.com/app/637090/discussions/0/1697175413670359725);
  this is community reading. Salvage picks are limited per contract regardless of value — [Steam](https://steamcommunity.com/app/637090/discussions/0/1700541698699416489).
- **Modders loosen scarcity:** one mod notes that a completed mech sells for little while individual parts are very
  expensive to buy, and cuts part prices to a third — [Nexus Mods](https://www.nexusmods.com/battletech/mods/397).
  Salvage overhaul mods tune per-location salvage fractions — [Nexus Mods](https://www.nexusmods.com/battletech/mods/442).
- **MW5:** players with mixed loadouts beat the game on expert, while others find laser boating trivialises it. The
  balance debate is about fun and variety, not fairness — [Steam MW5: Clans](https://steamcommunity.com/app/2000890/discussions/0/4629230582747973584/),
  [Steam](https://steamcommunity.com/app/2000890/discussions/0/4691154661545222251). Hardpoint-size-removing mods are
  popular and "0 issues" for many — [Steam thread](https://steamcommunity.com/app/2000890/discussions/0/4691154153116798972).
- **Single-player counters are soft:** enemies at best impose soft counters that make some player choices harder —
  [critpoints.net](https://critpoints.net/2017/02/page/3).

### Inferences
- **In single-player, the goal shifts from fairness to keeping decisions interesting.** Overpowered is acceptable
  locally (a found rare weapon, a hero mech), but a build that is *best in every mission* removes the reason to own
  different mechs. That breaks IRONFEUD's pillar 4 (every weight class viable, no composition wins every mission
  type). So the matrix test (mission type × lance composition) is the single-player equivalent of PvP meta tracking.
- **Use the campaign as the balancing layer:**
  - scarcity and rarity of parts and weapons (HBS style);
  - tonnage and budget caps per contract;
  - upkeep per ton;
  - transport capacity;
  - salvage rules that reward particular kill methods.

  These make "the best build" depend on what you own and what the contract allows, not on a spreadsheet optimum.
- **AI opponents must exploit dominant builds, or players will.** The AI pilots use the same `MechInput` and
  perceive only what a pilot could. So enemy lances should *field* varied compositions and *adapt*: hug cover against
  pinpoint snipers, rush laser boats to force heat, spread out against missiles. Then the soft counters show up in
  the campaign, not only in PvP.
- **Persistent damage plus repair cost** (already a design consequence in CLAUDE.md) is a natural tax on alpha-heavy,
  heat-reckless play, and on mechs that are expensive to field. It is a campaign lever that PvP games lack.

### Gaps
- No HBS postmortem or dev diary on the rationale behind salvage/part scarcity was found. Only the 2018 Q&A.
- No MW5 developer statement on single-player balance philosophy, or on why hardpoint sizes were added, was found.
- No source compared AC6's single-player campaign balance with its PvP balance (patch notes don't separate them).
