# Campaign / meta loop of mercenary mech games and "overworld plus battles" sandboxes

Scope: what makes the campaign loop of MechWarrior Mercenaries titles, HBS BattleTech, Mount & Blade, Starsector,
Battle Brothers (and RimWorld as a difficulty-scaling reference) compelling or grindy, with concrete mechanisms that
stop "biggest is best". Written for IRONFEUD (original IP): frame everything as mechanisms, not content to copy.
Research date 2026-11-02. Many official wikis (Fandom BattleTech/MW5) refused automated access (HTTP 402); where a
number comes from a forum or mod, it is flagged as such.

## 1. Mercenary loops (MW2/MW4/MW5 Mercenaries, HBS BattleTech + mods): contracts, salvage, repair, pilots, upkeep, tonnage

### Takeaway
The merc loop is a three-way trade at contract time (cash vs salvage vs reputation/standing, plus damage cover in MW5)
followed by a cost-of-damage phase (money AND time in the repair bay) and a fixed monthly bill; it is compelling when
damage has opportunity cost and salvage can jump you a weight class, and grindy when the economy is loose (MW5 at
launch) and nothing rewards going lighter (HBS BattleTech has no drop-weight limit, so lights become "useless" by
3-4 skull missions). MW5's tonnage cap is the closest genre precedent for IRONFEUD's pillar, but it is a soft cap with
a payout penalty and is tied to mission difficulty.

### Cited Findings
**HBS BattleTech (2018)**
- Before each contract the player divides the reward between cash, priority salvage rights and faction reputation;
  players tend to over-weight cash early — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- Taking less pay and less salvage raises reputation with the employer, which unlocks harder jobs and store
  discounts; per-house reputation changes which contracts are offered at all — [Twinfinite](https://twinfinite.net/guides/battletech-how-to-earn-reputation/); [earlyguides tips](https://earlyguides.com/battletech/tips) (search-snippet level; a [GOG forum post](https://gog.com/forum/battletech/tips_what_ive_learned_so_far/page1) contradicts this, unverified)
- Salvage pool depends on what was actually damaged/destroyed during the mission; salvage comes as gear items and mech
  parts; **three parts of the same mech complete it, and you immediately get the mech with its stock loadout**; the
  reviewer's campaign hinged on head-shotting an assault pilot to salvage the whole mech, which "put me into a much
  higher weight class" — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- Failing monthly upkeep is a critical fail state; **repair time is more costly than repair money** (opportunity cost
  of a mech in the shop); the pressure eases later with more mechs and upgraded mech bays; sometimes it is worth
  spending a whole month on repairs instead of taking work — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- Armour is "basically free to replace"; structure damage is what costs limbs, weapons and pilots — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- Pilot progression: XP into four paths (Gunnery, Piloting, Guts, Tactics); only first abilities from two trees and
  the second ability from one, so pilots become emergent classes (e.g. mobile skirmisher vs stationary sniper) — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- Morale (from paying above base wage, habitat upgrades) feeds an in-battle resource pool (Precision Shot / Vigilance);
  upgrades raise the monthly bill; players report morale commonly hitting its 50 cap — [Steam discussion via search](https://steamcommunity.com/app/637090/discussions/0/1696045708640582965) (anecdotal)
- Players: once you reach 3-4 skull missions "the light and medium Mechs are useless"; there is little reason to bring
  lighter mechs because there is no drop-weight limit and no extra money or XP for going lighter; lights remain viable
  in mixed lances or when you don't need kills — [Steam discussions via search](https://steamcommunity.com/app/637090/discussions/0/2828702372996753978) (forum consensus, not dev statement)
- A mod raising/gating drop tonnage (max drop ~400 t → ~660 t behind Leopard tonnage + Argo command upgrades) led the
  poster to keep using lighter mechs late; a forum suggestion is missions with preset drop weights — [Penny Arcade forum](https://forums.penny-arcade.com/discussion/comment/42904367) (mod unnamed)
- Vanilla lance is a hard cap of 4 mechs; BTA 3062 reportedly adds upgrades for 2-3 lances — [Steam forum via search](https://steamcommunity.com/discussions/forum/1/3114770913747731471) (anecdotal)
- AI concentrates fire on the weakest exposed component and uses spotters for indirect fire; a scout that pokes once
  can eat the whole enemy force's LRMs — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- A mod sets per-mech monthly cost as a % of chassis cost (default 0.003 of price/month), i.e. upkeep scaling with
  value — [Nexus mod via search](https://www.nexusmods.com/battletech/mods/598) (mod config, not vanilla)

**MechWarrior 5: Mercenaries (2019 → DLC to 2026)**
- Negotiation points depend on overall Reputation and Standing with the offering faction; points buy higher base pay,
  larger salvage share, higher damage insurance (employer pays repairs), and limited airstrikes — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries); [Impulse Gamer review](https://www.impulsegamer.com/mechwarrior-5-mercenaries-review/)
- Fighting for a faction raises Standing, fighting against it lowers it; high Standing = negotiation bonus and store
  discounts, low = penalties and higher prices; aborting a contract lowers both Standing and Reputation — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)
- Map structure: Industrial Hubs offer faster, cheaper repairs, pilots for hire and big markets; Conflict Zones have
  slower/costlier repairs, small markets with damaged mechs, no pilots — i.e. **where you are changes what damage
  costs** — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)
- Pilot injuries require healing time; extensive recovery can sideline pilots for several missions — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)
- Travel between systems costs time and money; Rise of Rasalhague expanded bays from 12 to 40, extra bays leased for
  a quarterly fee — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)
- Campaign mission tonnage limits scale with progression: early missions 30-50 t; reputation-11 story missions
  250-360 t; mid-game high-reward quests 160-275 t — [Sarna campaign missions](https://sarna.net/wiki/MechWarrior_5:_Mercenaries/Campaign_Missions); [Sarna HRQ](https://www.sarna.net/wiki/High_Reward_Quest)
- The tonnage limit is a **soft cap**: exceeding it gives C-bill and salvage penalties (one poster cites up to -95%
  payout on low-difficulty missions); it is "based off battle difficulty", meant to match what you face; "missions
  are generally designed so you can do them with half the allowed tonnage" — [Steam discussion](https://steamcommunity.com/app/784080/discussions/0/3723946017833861170) (player claims); Dragon's Gambit custom difficulty can remove over-tonnage penalties — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)
- Career mode adds five "Career Reputations" (Equipment Collector, Mech Collector, Mech Hunter, Treasure Hunter, War
  Dog) fed by cantina jobs that unlock mech quirks; later DLCs added rival merc companies, bounty intel, occupation
  zones that change contract tempo, guerrilla missions, pilot traits/academies, vehicle salvage — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)
- Launch criticism: most content is procedural missions with little to distinguish them from story missions; grind
  in lower tiers for funds; Vice reported never coming close to the red through the first seven reputation levels
  (~15 h) and called the economy "busted"; Gamereactor: "horrendous grind, and repetitive missions"; praise went to the
  combat itself — [Vice](https://www.vice.com/en/article/mechwarrior-5-mercenaries-review); [Windows Central](https://windowscentral.com/mechwarrior-5-mercenaries-review); [OpenCritic](https://www.opencritic.com/game/7412/mechwarrior-5-mercenaries/reviews?page=2); [GameRevolution](https://www.gamerevolution.com/review/622541-mechwarrior-5-mercenaries-review); [PC Gamer review](https://www.pcgamer.com/uk/mechwarrior-5-mercenaries-review/)
- MW5 sold 2M+ units by Oct 2024 — [Sarna MW5](https://www.sarna.net/wiki/MechWarrior_5:_Mercenaries)

**MechWarrior 4: Mercenaries (2002)**
- Salvage was a major source of pay; repairing captured chassis and selling them was a notable change from earlier
  games; campaign set in a civil war where merc work is plentiful — [GameSpot Q&A](https://www.gamespot.com/articles/mechwarrior-4-mercenaries-qanda/1100-2874574/); Metacritic 83 — [Wikipedia](https://en.wikipedia.org/wiki/MechWarrior_4:_Mercenaries)

### Inferences
- The contract slider is a good, cheap decision point because the three currencies have different time horizons:
  cash (now), salvage (a step change in force), standing (future contract quality). IRONFEUD can add a fourth that
  directly serves the weight-class pillar: **"lighter-than-cap bonus"** (pay/rep bonus per ton under the cap) or
  objectives scored on speed/stealth, so going lighter is a negotiated advantage, not just a handicap.
- HBS shows the failure mode precisely: no drop cap + no reward for going lighter = assault lance. MW5 shows a
  partial fix (difficulty-scaled soft cap). Neither makes lights *needed*; IRONFEUD's in-battle jobs (spotting, marks,
  ECM, terrain access) must be required by mission types, and the cap must bind tight enough that "fill to cap with
  one assault + light" competes with "4 mediums".
- "Repair time is the real cost" is valuable: since BattleResult already carries persistent component damage,
  repair throughput (bay slots, tech crew, location: hub vs warzone) can be the limiter rather than cash. Heavier
  mechs taking longer to repair (BTA-style; see Gaps) is a natural, legible tax on size.
- Salvage that completes a whole mech in one lucky fight is the main snowball accelerator in HBS. Consider salvage
  from partial wrecks scaled by what's left (persistent component damage is already modelled) and a market that
  doesn't sell assaults outside hubs.
- MW5's busted early economy shows money must stay tight long enough for the player to feel trade-offs; measure it
  with the headless campaign sim (net income per in-game month by roster) instead of by feel.

### Gaps
- Exact vanilla HBS numbers (MechTech points, repair days per ton of structure, monthly cost per mech, expenditure
  levels, pilot death chance on mech destruction, medbay days) — Fandom wikis refused automated access; not verified.
- MW5 negotiation-point formula and exact over-tonnage penalty curve: no official source found.
- MW2: Mercenaries contract/salvage rules: StrategyWiki page refused access (403). No sourced details.
- RogueTech / BattleTech Advanced 3062 drop-tonnage and difficulty systems: no reliable source found.

## 2. Mount & Blade (Warband, Bannerlord): party growth, renown, lords/factions, economy, battles → campaign, weaknesses

### Takeaway
M&B's loop works because battles you personally fight feed every campaign layer (renown → party size, prisoners,
loot, relations, fief claims) and because AI lords run the same world rules you do, producing emergent stories; its
well-documented weaknesses are faction snowballing, constant AI war declarations that turn the late game into
siege-and-wage grind, and an auto-resolve that ignores troop quality.

### Cited Findings
- Warband party size (community formula): base 30, +1 per Charisma, +5 per Leadership, +1 per 25 renown; Leadership
  also boosts morale and lowers troop wages — [Steam discussion](https://steamcommunity.com/app/48700/discussions/0/350532795330467237); [ludo.guide](https://www.ludo.guide/guide/mount-blade-warband/mount-blade-warband-part-4) (unofficial)
- Warband: heavy cavalry reportedly counts double against party size — [Steam discussion via search](https://steamcommunity.com/app/48700/discussions/0/45350791393480729) (anecdotal)
- Bannerlord: wages paid daily (Warband weekly); upgrading troops can push a party over its wage limit and cause
  desertion — [Steam discussions via search](https://steamcommunity.com/app/261550/discussions/0/3038228835132945473) (anecdotal)
- Bannerlord clan tiers (renown 50 at T1 to 6150 at T6) each add +15 party size per clan party plus more parties,
  companions, workshops; another guide gives a T5 + Steward-300 quartermaster party limit of 220 — [M&B Fandom Clan](https://mountandblade.fandom.com/wiki/Clan); [gamerempire](https://gamerempire.net/mount-and-blade-2-bannerlord-how-to-increase-party-size/) (sources conflict on numbers)
- Design intent: TaleWorlds wanted the sandbox to encourage emergent narrative and preferred player-made stories over
  a heavy scripted plot; the original M&B had quests but no linear storyline — [TaleWorlds dev blog](https://www.taleworlds.com/en/Games/Bannerlord/Blog/42); [Wccftech interview](https://wccftech.com/mount-blade-ii-bannerlord-gamescom-interview/2/amp/); [RPGFan interview](https://classic.rpgfan.com/features/mountandblade/)
- TaleWorlds cited Warband's feast mechanic (spend resources to repair war-damaged lord relations) as something not to
  repeat in Bannerlord — [TaleWorlds dev blog 07/09/17](https://www.taleworlds.com/en/Games/Bannerlord/Blog/24)
- Snowballing: one faction wiping out the others in a relatively short time, worst in early versions — [Stopgame review](https://stopgame.ru/show/108432/mount_blade_2_bannerlord_review_v_rannem_dostupe); [Steam discussions](https://steamcommunity.com/app/261550/discussions/0/3835424220122659041)
- War spam: AIs "constantly declare war on each other, regardless of the situation"; reported cause: declarations
  weigh potential as well as current strength; "constant and endless war just grind"; war is the only income that
  matters late; late game called repetitive with little depth in politics — [Steam discussions](https://steamcommunity.com/app/261550/discussions/0/3415432674396171690); [vaporlens summary](https://vaporlens.app/app/261550/mount_blade_ii_bannerlord)
- Common mitigation (mods/settings): minimum war duration (up to 500 days) and a post-war cooldown before the same
  pair can fight again — [Steam discussion via search](https://steamcommunity.com/app/261550/discussions/0/3493131640461031782)
- Sieges are the most praised content — [vaporlens summary](https://vaporlens.app/app/261550/mount_blade_ii_bannerlord)
- Vanilla Bannerlord auto-resolve (per mod author): a random troop from each side attacks/defends and its power is
  applied as damage — "very simplified", mostly RNG; players widely report high-tier troops dying to low-tier troops;
  tier power gaps are smaller than in Warband — [Better Auto Calc mod docs](https://www.nexusmods.com/mountandblade2bannerlord/mods/673?tab=docs); [Steam discussions](https://steamcommunity.com/app/261550/discussions/0/2248930420025685447)
- Warband auto-resolve example: "25 master archers ... and 30 mounted knights" still losing nine men to twelve forest
  bandits — [comment on Fractal Softworks blog](https://fractalsoftworks.com/2011/11/09/automatically-resolving-combat/)

### Inferences
- The M&B structure maps cleanly onto IRONFEUD's planned boundary: the campaign sim owns renown/party caps/wages, the
  battle returns a BattleResult. The key lesson is that **every battle should move several campaign meters at once**
  (cash, salvage, standing, pilot XP, injuries, local control) so fights never feel like filler.
- Anti-war-spam: faction AI should go to war on *current* strength with war weariness, minimum durations and cooldowns
  from day one; let deterministic headless campaign runs report "wars per faction-year" and "faction count over time"
  as regression metrics (snowballing is measurable).
- Wage/upkeep that rises with unit quality (Warband/Bannerlord) is the classic size limiter; in IRONFEUD the analogue
  is per-mech upkeep scaling with tonnage/value, with a soft "logistics" cap rather than a hard party size.

### Gaps
- No official Warband/Bannerlord per-tier wage table or vanilla auto-resolve formula found.
- No developer statement on Bannerlord snowballing fixes found; did not verify current-patch behaviour.

## 3. Comparable sandboxes: Starsector, Battle Brothers, others — mechanisms limiting "biggest is best"

### Takeaway
Starsector is the best reference for IRONFEUD's pillar: it limits big fleets with (a) a per-battle deployment-point
cap that objectives on the map can raise, (b) per-deployment combat-readiness costs that are higher for big ships and
recover slower and cost supplies, (c) peak-performance timers that are long for big ships but short for small ones,
and (d) fleet-wide skills that only reach full effect at or below 240 DP. Battle Brothers limits growth with wages
that rise per level, a 12-man field cap from a 20-man roster, contract difficulty that scales with roster strength,
and late-game crises that change the world.

### Cited Findings
**Starsector**
- Combat readiness (CR) abstracts crew fatigue, wear and spare parts; base max 70%, skills up to 100% — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR)
- Each deployment costs CR, from ~9% (Gemini) to 40% (Hyperion); small and low-tech ships pay
  less, large and high-tech more; smaller ships generally recover CR faster — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR)
- CR effects: above 70% linear bonuses up to ~+10% speed, +10% damage, -10% damage taken at 100%; below 50% mirrored
  penalties; ≤40% random malfunctions; ≤20% critical malfunctions (weapon out for the battle, engine out long); 0% no
  shields/systems — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR)
- Peak performance time (s by hull size, low/mid/high tech): frigate 5/4/3, destroyer 7/6/5, cruiser 9/8/7,
  battlecruiser 10, battleship 12 (table units as listed on wiki); after PPT, CR drops 0.25%/s (15%/min); PPT only
  runs while enough comparable enemies are in sight — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR) (note: wiki table appears to be in minutes/arbitrary units; check in game)
- Recovery costs supplies (example: 20 CR/deployment, 10%/day, 20 supplies → 10/day for 2 days); every ship also
  burns monthly maintenance supplies; running out of supplies drains CR; mothballed ships 0% CR and no supplies; the
  winner regains some CR free based on a hidden "hard fought" ratio (DP destroyed vs deployed) — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR)
- Wiki advice: carry 100× daily maintenance before undocking, 200× for combat — [Starsector wiki Economy](https://starsector.wiki.gg/wiki/Economy)
- Short crew scales max CR down proportionally (50% crew → CR halved) — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR)
- Battle size default 400; each side's starting DP is 40-60% of it (160-240) depending on fleet size and officer
  quality; capturing objectives (nav buoy, sensor jammer +5% each, comm relay +10%) raises the cap, total from
  objectives capped at 60%; you can always deploy one ship regardless of cost; clean disengage needs ~100 DP of enemy
  destroyed/routed at default size — [Starsector wiki Combat screen](https://starsector.wiki.gg/wiki/Combat_screen); [Starsector wiki Objective](https://starsector.wiki.gg/wiki/Objective)
- Fleet-wide skills reach full effect only at ≤240 total combat DP: Tactical Drills (+5% damage), Crew Training
  (+15% max CR), Flux Regulation (+10%), Field Repairs (25% free post-combat repair), Containment Procedures; Automated
  Ships ≤120; Carrier Group ≤8 fighter bays; Hull Restoration's d-mod avoidance 75% (≥60 DP ships) to 90% (≤5 DP) — [Starsector wiki Skills](https://starsector.wiki.gg/wiki/Skills)
- Developer rationale (2013 logistics overhaul): separate "deployment points" (combat value) from logistics cost so
  each can be accurate; a single logistics rating gives "a dynamic choice between having a smaller, elite fleet, and a
  larger, less organized one"; over-capacity is a soft cap paid in supplies; mothballing cuts maintenance 90% — [Fractal Softworks blog](https://fractalsoftworks.com/2013/05/25/logistics-fleet-management/) (historical; the logistics stat itself was later replaced, but the DP/supply split persists per the current wiki)

**Battle Brothers**
- Start with 3 men, roster up to 20, field max 12; all men cost daily gold and provisions (2 food/day) — [BB wiki Game Mechanics](https://battlebrothers.fandom.com/wiki/Game_Mechanics)
- Wage rises +2 crowns per level-up (trait "greedy" +2); unpaid/unfed men lose mood and desert — [BB wiki Game Mechanics](https://battlebrothers.fandom.com/wiki/Game_Mechanics); forum estimate ~+10%/level to 11 — [Steam discussion](https://steamcommunity.com/app/365360/discussions/0/575994948960164271)
- XP: killer gets 20%, other 80% split evenly among the battle party; level curve 200 → 15000 XP to level 11; veteran
  levels beyond 11 without perks — [BB wiki Game Mechanics](https://battlebrothers.fandom.com/wiki/Game_Mechanics)
- **Contract difficulty scales with roster strength** = sum of [10 + 2·(lvl-1)] over the 12 strongest brothers (gear
  not counted), times skull rating and contract type; renown does not affect contract difficulty but raises pay;
  non-contract locations scale with distance from civilisation and time only, roaming parties slightly with player
  strength — [BB wiki Game Mechanics](https://battlebrothers.fandom.com/wiki/Game_Mechanics)
- Renown ladder (0 Unknown … 4000 Legendary … 14000 Immortal), -3/day decay, retreat -15, fail -75, cancel -100,
  betray -200, win +5, contract +25 — [BB wiki Game Mechanics](https://battlebrothers.fandom.com/wiki/Game_Mechanics)
- Injuries can become permanent (lost limb, internal damage) and specific (collapsed lung → stamina, broken knee →
  actions); characters can survive a "death" with a permanent injury — [Keengamer review](https://www.keengamer.com/articles/reviews/battle-brothers-review/); [BB wiki loading tips](https://battlebrothers.fandom.com/wiki/Loading_Screen_Tips)
- Late-game crises (noble war, greenskin invasion, undead) were added to give "purpose and challenge": a buildup phase
  with warning events, then the world changes (settlements burned or captured, factions ally), unique contracts;
  ending a crisis is a "soft end" offering retirement, else a new crisis follows. Origin: in early access unchecked
  enemy factions grew and overran the world, which had balance problems but made the world feel changed by you — [Overhype Dev Blog #92](https://www.moddb.com/news/dev-blog-92-late-game-crises)

**RimWorld (difficulty reference)**
- Raid points = (wealth points + per-colonist points) × difficulty × starting factor × adaptation factor; buildings
  count at half wealth; range 35-10,000 — [RimWorld wiki Raid points](https://rimworldwiki.com/wiki/Raid_points) (pages conflict on thresholds)

### Inferences
- Starsector's four levers transfer almost 1:1 to mechs: **drop cap in tonnage/"deployment points" set by the
  mission** (raisable by capturing map objectives: comms relay, sensor mast); **per-sortie readiness cost** that is
  higher for assaults (more maintenance, slower recovery, needs a hub); **endurance** (heat/ammo/coolant per sortie)
  shorter for lights but readiness recovery faster; **fleet-wide officer bonuses that fade above a tonnage
  threshold** (a doctrine skill that rewards lighter lances).
- Starsector's "hard fought" free CR return rewards trading efficiently, not winning big; an IRONFEUD analogue is a
  repair-cost discount scaled by enemy tonnage destroyed per own tonnage deployed, which favours fielding exactly
  what's needed.
- Battle Brothers' roster-strength scaling is effective anti-snowball but also a trap: if contract difficulty scales
  with the sum of your units, bringing more/bigger is punished only by tougher enemies, not by mission failure.
  Combine it with fixed-difficulty world locations (BB does) so growth still feels like growth.
- Late-game crises are a proven fix for "nothing left to do" in sandboxes and suit the faction layer (an invasion that
  needs fast responders in many systems at once is a natural reason lights/mediums matter: assaults can't be
  everywhere).

### Gaps
- Kenshi, X4, MechWarrior: Living Legends and specific M&B mods: not researched (time budget); no sourced findings.
- Starsector's current fleet ship-count cap and whether the PPT table is in seconds: not confirmed from sources fetched.
- No developer statement on why the 240 DP threshold was chosen.

## 4. Auto-resolve design: how games compute it and what players dislike

### Takeaway
The best documented design (Starsector, 2011) treats auto-resolve as a fast turn-based mini-sim that uses all the
same stats, counters and upgrades as real combat, guarantees plausibly zero losses when one side is overwhelming, and
deliberately does not model player piloting skill; players hate auto-resolve that ignores their upgrades/counters,
kills elite units to trash, distributes casualties implausibly, or makes skipping a fight a punishment.

### Cited Findings
- Starsector's dev framed the core problem: auto-resolve "can obliterate their progress ... unfairly" by playing by
  hidden rules different from the real game, so the player cannot learn or improve at it — [Fractal Softworks blog](https://fractalsoftworks.com/2011/11/09/automatically-resolving-combat/)
- His "unfair" list: ignoring any fleet improvements (weapons, mods, officers, skills); ignoring known counters
  (carriers boost fighters, bombers vs big ships, PD vs fighters); destroying ships that "would absolutely never, ever
  get a dent" in a real fight; randomness must not become "anything can happen" — [Fractal Softworks blog](https://fractalsoftworks.com/2011/11/09/automatically-resolving-combat/)
- Method: per-ship stats Hits, Shields (speed folded into both), Attack, Point Defense, Strike Attack, Movement,
  Maneuverability, flight decks; a no-damage "skirmish phase" where fast ships decide who gets initiative (reduced for
  tiny fleets); then random attacker picks biased by skirmish winner, target picks biased by weapon role and
  commander skill, damage with coefficients by target type, one counterattack per turn; damage reduced when few ships
  remain to limit luck; 10,000 simulations ~1 s; tuned until known missions gave plausible win rates (75%, 62%, 14%,
  0.1%) — [Fractal Softworks blog](https://fractalsoftworks.com/2011/11/09/automatically-resolving-combat/)
- He rejected scaling auto-resolve by the player's past performance (it would reward not fighting and be opaque), said
  auto-resolve is "not meant as an equal-effectiveness replacement" for fighting, suggested bonus XP for auto-resolved
  battles and a second-in-command opinion of the likely outcome; full-speed headless sim was rejected in 2011 because
  a 5-min battle would take ~15 s; with NPC-vs-NPC battles also needing resolution, a simple "no losses above strength
  ratio X" rule wasn't enough — [Fractal Softworks blog and comments](https://fractalsoftworks.com/2011/11/09/automatically-resolving-combat/)
- Total War players: a 70-30 advantage ending in a big loss; artillery over-valued; unrealistic casualties; whole
  units wiped instead of spread casualties; abilities/spells ignored; "it punishes players for not liking the combat";
  devs (Sins of a Solar Empire) wanted auto-resolve that doesn't make players feel they give something up — [TWCenter](https://www.twcenter.net/threads/auto-resolve-a-load-of.364595/); [Steam TW:WH2](https://steamcommunity.com/app/594570/discussions/0/1480982971175660014); [legacyofgames](https://legacyofgames.com/2021/07/09/the-autoresolve-rework-made-it-even-worse/); [forums.wincustomize](https://forums.wincustomize.com/394167)
- Bannerlord vanilla auto-resolve is mostly RNG per random troop pairing; elite units die to recruits — [Better Auto Calc docs](https://www.nexusmods.com/mountandblade2bannerlord/mods/673?tab=docs)

### Inferences
- IRONFEUD's plan (auto-resolve calibrated against headless AI battles) is exactly what Starsector couldn't afford in
  2011; because BattleSim already runs headless, it can (a) run real fast-forward battles for important fights, and
  (b) fit a cheap Starsector-style mini-sim for galaxy-wide NPC battles, regression-tested so its win-rate/loss
  distribution matches headless battles per mission-type × lance-composition cell.
- The mini-sim must include the same counters that make lights viable (spotting/sensor lock, ECM, speed vs
  turn rate, facing armour), or auto-resolve will silently reintroduce "biggest is best" in the campaign layer.
- Show a predicted outcome (lancemate/XO estimate) and keep it honest; give a small consolation (XP) rather than a
  hidden penalty; never kill an elite pilot in an overwhelming-superiority auto-resolve.

### Gaps
- No primary source for how Total War or Bannerlord compute auto-resolve internally; no systematic player data.

## 5. Difficulty scaling, anti-snowball and keeping weaker units useful

### Takeaway
Games that keep weaker units useful make size cost something *continuous* (supplies/CR per sortie, wages per level,
repair time) and make the battlefield cap *situational* (deployment points per battle, mission tonnage), while
difficulty tracks either the player's roster (Battle Brothers contracts, RimWorld wealth) or world time/crises; games
that let the player stack quality without a running cost (HBS BattleTech vanilla) collapse to the heaviest units.

### Cited Findings
- Starsector: deployment-point cap per battle, CR cost per deployment higher for big ships, faster recovery for small
  ships, supply maintenance, skills full only at ≤240 DP — [Starsector wiki CR](https://starsector.wiki.gg/wiki/CR); [Skills](https://starsector.wiki.gg/wiki/Skills); [Combat screen](https://starsector.wiki.gg/wiki/Combat_screen)
- Starsector dev: soft caps (pay supplies to exceed) give "a dynamic choice between ... a smaller, elite fleet, and a
  larger, less organized one" — [Fractal Softworks blog](https://fractalsoftworks.com/2013/05/25/logistics-fleet-management/)
- Battle Brothers: wages +2/level, roster 20 / field 12, contract difficulty scales with strength of 12 strongest,
  world locations don't scale (become "juicier targets" late), economic difficulty cuts contract pay 10%/level — [BB wiki Game Mechanics](https://battlebrothers.fandom.com/wiki/Game_Mechanics)
- Player complaint on BB scaling: a level-11 brother counts the same as three level-1s in the strength formula,
  which some felt undervalues veterans — [Steam discussion via search](https://steamcommunity.com/app/365360/discussions/0/2971775284257693403) (anecdotal)
- MW5: mission tonnage cap tied to difficulty with payout/salvage penalties for exceeding it — [Steam discussion](https://steamcommunity.com/app/784080/discussions/0/3723946017833861170)
- HBS BattleTech: no drop-weight cap and no reward for going light → lights/mediums "useless" by 3-4 skulls — [Steam discussions via search](https://steamcommunity.com/app/637090/discussions/0/2828702372996753978); a drop-tonnage mod restored light use — [Penny Arcade forum](https://forums.penny-arcade.com/discussion/comment/42904367)
- HBS BattleTech in-battle light roles: movement grants Evasion stacks; any hit (even a miss) strips a stack, so
  lights/out-of-range weapons can strip evasion for heavy hitters; Piloting+Tactics pilots in fast mechs act first to
  Sensor Lock priority targets — [PC Gamer guide](https://www.pcgamer.com/battletech-guide/)
- Bannerlord: snowballing and endless wars reported as the main late-game problem; mods add war minimum duration and
  cooldowns — [Steam discussions](https://steamcommunity.com/app/261550/discussions/0/3835424220122659041); [Stopgame](https://stopgame.ru/show/108432/mount_blade_2_bannerlord_review_v_rannem_dostupe)
- RimWorld: threat scales with colony wealth and colonist count with an adaptation factor — [RimWorld wiki](https://rimworldwiki.com/wiki/Raid_points)
- Battle Brothers late-game crises re-raise the challenge and change the world after the company is established — [Overhype Dev Blog #92](https://www.moddb.com/news/dev-blog-92-late-game-crises)

### Inferences
Concrete mechanism set for IRONFEUD (each measurable in the headless campaign + battle matrix):
1. **Mission drop cap in "deployment points"** (tonnage-weighted, not raw tons, so tuning can price a 75 t heavy above
   1.9 × 40 t) set by mission type and employer; map objectives (relay, sensor mast) raise it mid-battle, which itself
   is a fast-unit job.
2. **Per-sortie readiness**: each mech spends readiness per deployment; assaults spend more and recover slower, need
   hub facilities; consecutive contracts (or a multi-battle campaign op) exhaust heavies first.
3. **Upkeep and repair throughput scale super-linearly with tonnage** (bay time, parts availability by system
   type: hubs vs conflict zones as in MW5), so a lance of assaults is an expensive, slow company.
4. **Transport**: dropship tonnage capacity and jump costs per ton (the HBS mod lesson: gated drop capacity kept
   lights in use).
5. **Contract terms reward going under cap** (pay/standing per DP unused) and mission types with time limits,
   detection rules or wide maps where speed is the win condition.
6. **Doctrine/officer bonuses that fade above a lance DP threshold** (Starsector ≤240 DP).
7. **Difficulty**: contracts scale with roster *fielded* strength (BB) while world points of interest scale with
   distance and time; late-game galaxy crises create many simultaneous fires so the player needs several cheap fast
   lances, not one perfect assault lance.
8. **Anti-snowball in the faction layer**: war weariness, minimum war duration and cooldowns, decisions on current
   strength; track faction count and war frequency in deterministic headless runs as regression metrics.

### Gaps
- No sourced quantitative evidence on how often players actually keep light units in Starsector late game (frigate
  viability is debated in community, not measured here).
- No sourced numbers for HBS vanilla repair-time scaling by tonnage.
