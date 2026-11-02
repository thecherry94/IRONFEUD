# Mission and objective design that keeps light, medium, heavy and assault units each necessary, plus procedural mission/contract generation

Scope note: this covers objective modes and mission structures, and how each one shifts value between fast/fragile and slow/tough units. Campaign economy, upkeep, auto-resolve and Mount & Blade/Starsector meta loops are in the sibling file `campaign_and_sandbox_loop.md`, so they appear here only where they bear on mission design. Several fandom wikis (402), Reddit (blocked), Nexus Mods (blank page) and the live MWO wiki (redirected away) could not be read directly. The MWO wiki was read through an archive.org snapshot. Claims taken only from search-result snippets are marked "(snippet)".

---

## 1. Multiplayer objective modes in mech and vehicle games: which made scouts/lights matter and which collapsed into deathmatch?

### Takeaway
Modes that pay out for *territory over time* (Conquest, Domination) or put a *destructible objective behind the lines* (Assault, Incursion, Siege) give fast units a job on paper. In practice most collapsed into "kill the other team, then cap", because kills also end the match and paid as well or better. Two kinds of mode made scouts matter. The first were small single-life objective modes built around information (MWO Scouting, 4v4, a low tonnage cap). The second were systems that pay the spotter for damage the team then deals (World of Tanks spotting credits, War Thunder's 30 s scouting mark with a kill bonus). Asymmetric modes (Escort, Rush, Siege) give each side a different job, but they live or die on objective tuning and on map and AI pathing.

### Cited Findings
**MechWarrior Online (MWO) mode rules (archived official wiki, 2022 snapshot)**
- MWO splits play into Quick Play (Conquest, Assault, Skirmish, Domination, Escort, Incursion) and Faction Play (Siege, Counter Attack, Scouting). The wiki says "simply destroying the enemy team is always a viable option" in every mode. — [MWO Wiki: Game Modes (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Conquest**: first to 750 Resource Points. Each held capture zone yields 1 RP/s. A team can also win by destroying all enemies while holding enough zones to guarantee an RP win in the remaining time. Official framing: "a well-coordinated Lance of Light 'Mechs can easily snatch victory from an enemy too focused on sheer firepower." — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- At launch Conquest used the Assault maps with 5 capture zones, and the C-bill payout was resources gained ×25 (snippet). — [PC Gamer](https://www.pcgamer.com/au/mechwarrior-online-conquest-mode-release)
- **Assault**: capture the enemy base or destroy all enemy mechs. Official framing: "a well-armed diversionary force can mount a convincing distraction … while your faster 'Mechs go for the base capture." — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Domination**: hold one central Communications Beacon for a cumulative 1 minute (each team's countdown starts at 1:00). Alternatively, destroy all enemies and enter the radius with enough time left to finish the capture. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Escort**: asymmetric. Defenders must get an AI-controlled, ECM-equipped VIP assault mech to an extraction zone in time; attackers must stop it. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Incursion**: win by destroying the enemy base, by killing all enemies while having less base damage, or by having a base-damage lead at the timer. Players fetch Fuel Cells from supply points to power support towers. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- Incursion bases (April 2017 notes): 4 mobile field bases, 3 base towers with generators, an air-control tower, a radar tower and a jamming tower. Towers take damage only through their generators. Each Fuel Cell keeps the jamming tower on for 2 minutes (snippet). — [MWO patch notes](https://mwomercs.com/game/patch-notes/1798)
- **Faction Play Siege**: attackers destroy generators to open the blast doors, then 3 inner generators, then the Space Defense System. "Attackers can increase the odds of victory … in the Siege game mode by launching missions in the Scouting game mode prior to the Siege." **Counter Attack**: attackers must kill all enemies *and* their Mobile Field Base; defenders win by surviving the timer. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Scouting**: "a 4v4 single-spawn game mode focused on capturing or protecting Data Points" (attackers gather intel; defenders protect Data Points). It contrasts with the 12v12 respawn Siege modes. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- Scouting tonnage (Dec 2016): Inner Sphere Scouting drop deck **55 t**, unchanged; Inner Sphere Invasion 265 t; Clan Invasion cut to 240 t. — [MWO: Faction Play Tonnage Change 12/15/2016](https://w.mwomercs.com/news/2016/12/1684-faction-play-tonnage-change-12152016)
- The 20 Dec 2016 hotfix cut Clan Scouting tonnage to 50 t (from 55) and the Scouting "Tug of War" intel bar to 200 (from 300). — [MWO Hot-Fix 20-DEC-2016](https://ww9.mwomercs.com/news/2016/12/1685-hotfix-scheduled-for-20dec2016)
- Players described two Scouting styles, "hide and seek" (favours fast builds) and "move to contact" (snippet). — [MWO forum: good IS mechs for scout mode](https://mwomercs.com/forums/topic/245949-good-is-mechs-for-scout-mode/unread/)
- From an early preview: scouts earn points and XP for damage dealt *and* for enemies they reveal (snippet). — [GameBanshee MWO preview](https://gamebanshee.com/d4hh)

**Did they collapse into deathmatch? (community)**
- A Conquest thread's title quote: "most everyone I meet thinks you deathball in it, still". Players reported that most matches were decided by kills, and argued the ideal play was "kill 11 and then cap" (snippet). — [MWO forum: Conquest cap times still too long](https://mwomercs.com/forums/topic/193033-conquest-cap-times-still-too-long)
- The "Overhauling Conquest" proposal makes capping pay by cutting flip time to about 70 s solo (about 10 s from barely-red to barely-blue), scaling resource rate to map size, raising the cap to 1000 at 75 C-bills/point, and making capture assists stack and pay every player in the box. Rationale: "Mechs that played the cap game wouldn't be stuck in place for a third or more of the match … Even players who want to win on kills would also want to gain points." — [MWO forum: Overhauling Conquest](https://mwomercs.com/forums/topic/174254-overhauling-conquest)
- An Incursion player guide gives a role split per class:
  - Lights "rush power cells and fuel power", choosing a tower to match what the enemy is doing, intercept enemy lights, scout and screen flanks, and call out flankers. They "should avoid rushing the bases unless you go in numbers" because turrets shred a lone light.
  - Mediums are "flex spots": "two medium mechs can easily wreck havok in an opposing base", and "this is a mode that can VERY much be won by a few medium mechs getting to the base."
  - Heavies and assaults "focus the most on fighting the other mechs … or to 'hold the line'", but must decide whether to fall back and defend.
  - The same guide admits "most people play the mode as just yet another version of skirmish." — [MWO forum: Incursion](https://mwomercs.com/forums/topic/250237-incursion)
- Escort critique: the VIP is "wholly useless and gets stuck frequently due to map issues", and the poster proposed pulling the mode from Quick Play (snippet). — [MWO forum: Escort replacement concept](https://mwomercs.com/forums/topic/263389-escort-replacement-concept)
- A Japanese MWO wiki says Escort was removed in v1.4.192.0 because of low popularity (snippet; removal date not cross-checked). — [wikiwiki.jp MWO: Escort](https://wikiwiki.jp/mwojp/Escort)
- A player anecdote: in Faction Play, lights were played "like light should be": scouts and raiders hunting lone mechs, mostly mediums (snippet). — [Steam MWO discussion](https://steamcommunity.com/app/342200/discussions/0/1474222595301851480)

**Titanfall**
- Respawn on its AI grunts: "It gave players who are newer something to shoot at and gave them a way to help their team." On mode-dependence: "If you're playing a Hardpoint game and there's AI streaming in to a capture point, yes, you're going to want to take them out. If it's Attrition, maybe you focus on the higher point kills." — [Xbox Wire: Inside Titanfall's AI](https://news.xbox.com/en-us/2014/02/18/games-titanfall-ai/)
- Attrition scoring: grunt/Spectre 1 point, Pilot 4, Titan 5 (snippet). In Hardpoint, minions push to the three hardpoints, fight the opposing minions, and the winners start capping (snippet). — [Prima Titanfall eGuide: game modes](https://primagames.com/eguides/titanfall-eguide/training/game-modes)
- A hands-on found grunts useful in Attrition as filler fights, but "almost extraneous" in Hardpoint, where players are drawn to the objectives anyway (snippet). — [PC Gamer Titanfall hands-on](https://www.pcgamer.com/uk/titanfall-hands-on-jetpacks-rocket-attacks-and-the-many-modes-of-mech-warfare)
- Frontier Defense (Titanfall 2): 4-player co-op that defends a Harvester against escalating waves. — [Neowin](https://www.neowin.net/news/titanfall-2s-upcoming-free-dlc-brings-the-cooperative-four-player-mode-frontier-defense)
- A community view on Frontier Defense says Ronin and Scorch are strongest, Monarch if the team is good, and Legion is weak (opinion, 2023). — [Steam TF2 discussion](https://steamcommunity.com/app/1237970/discussions/0/3877094315121906961)

**World of Tanks / War Thunder (spotting as a paid role)**
- WoT credits come from damage, detection and team performance. Damage dealt to a target a teammate spotted pays the shooter half rate, and the spotter can earn up to half rate as well. Initial detection pays a flat amount (double for an SPG) (snippet; exact current values not found). — [Wargaming wiki: Credits](https://wiki.wargaming.net/en/Credits)
- Light tanks generally have larger view ranges for scouting. — [WoT Asia Nooblog: Visibility System](https://worldoftanks.asia/en/news/featured/nooblog-06)
- War Thunder **Active Scouting** applies to light tanks of rank II and above, a few tank destroyers and AA-missile vehicles. The mark lasts **30 s**, and only one player can scout a target at a time (re-scout after 30 s). In Realistic mode the crosshair must be on the target, and a miss triggers "a lengthy cooldown". The scout earns RP/SL per scout, plus a bonus if an ally kills the target inside the 30 s window. In Arcade that bonus pays only "if the target wasn't clearly visible to the ally beforehand." In Realistic, the scout's aircraft spawns cost 7% less (14% with the Airstrike mod). Scout-capable vehicles also repair allies faster (3× vs 2×). — [War Thunder wiki: Scouting](https://wiki.warthunder.com/mechanics/238-scouting)
- Scout drones were added for rank VI–VII light tanks. In RB they can be "reloaded" on captured points, and in AB a new one comes after about 3 min. — [War Thunder old wiki: Drones](https://old-wiki.warthunder.com/Drones)
- Players argue drones with direct scouting "collapsed the risk-reward balance of scouting" (the scout no longer has to expose itself). — [WT forum: Why the current Scout UAV implementation is a mistake](https://forum.warthunder.com/t/why-the-current-scout-uav-implementation-is-a-mistake/300028)

**Planetside 2 / Battlefield (territory, asymmetry)**
- Planetside 2's territory system made back-capping (taking a base behind the lines) possible but harder: a base surrounded by enemy territory takes much longer to capture, and one surrounded by friendly territory re-secures quickly (snippet). — [Ten Ton Hammer: Matt Higby interview](https://www.tentonhammer.com/articles/matt-higby-talks-class-advancement-and-territory-capture-in-planetside-2.amp)
- The lattice links bases into battle lanes so that large fights flow more cohesively. — [Engadget](https://www.engadget.com/2013-07-24-planetside-2-update-brings-lattice-system-to-esamir.html)
- Battlefield Rush is asymmetric: attackers have a limited team ticket pool and defenders unlimited respawns. In BF3, an armed M-COM keeps the round alive even after attacker tickets reach 0. Ticket counts differ by source and era (75 vs 100). A Sept 2014 patch refocused Rush on infantry and moved M-COM layouts. — [Battlefield Wiki: Rush](https://battlefield.fandom.com/wiki/Rush)
- BF6 lists Rush as "asymmetrical", unlike Conquest. — [EA: Battlefield 6 modes](https://www.ea.com/games/battlefield/battlefield-6/news/modes)
- A community critique says the last few attacker tickets are "essentially useless" because a late push can only end in a loss. The proposed fix: attackers lose only at 0 tickets with no M-COM armed. — [BF feedback thread](https://app.betahub.io/projects/pr-7682550116/feature_requests/593)
- MechWarrior: Living Legends (Crysis mod): up to 32 players across Team Solaris Arena, **Terrain Control**, Test of Strength, Solaris Arena and Last Man Standing. It mixes mechs with tanks, VTOLs, aerospace and battle armour. — [Sarna: MW:LL](https://www.sarna.net/wiki/Mechwarrior:Living_Legends)

### Inferences
- **Universal failure mode:** if killing everyone ends the match *and* kills pay at least as well as objectives, the objective is a tiebreaker and fast units are just weak combatants. Every MWO fix proposal attacks that link: faster caps, objective pay comparable to kills, assists that pay everyone. IRONFEUD should make objective completion the *dominant* win and payout, with annihilation a slower or partial win. Cap the reward for kills in objective missions, end the mission on the objective clock, and make "kill 4 then cap" costlier than "cap while they hunt you".
- **Cumulative capture timers (Domination, 1 min total) suit heavies;** spread multi-zone resource races (Conquest) suit lights. The variables are zone count × distance × flip time × hold rate. Few zones close together means a deathball. Many zones far apart with fast flips means the light lance wins unless someone hunts them.
- **Paying for information, not just damage, made scouts real in WoT/WT.** IRONFEUD already has per-lance shared scans and designator marks, so a direct mapping exists. Credit the marking unit for damage dealt to its marks within a window (WT's 30 s), and only if the target was not already scanned (WT's "not clearly visible" rule). That feeds both the BattleResult payout and the AI utility score.
- **Single-life, low-tonnage, small-team intel modes** (MWO Scouting: 4v4, 50–55 t per mech) are the cleanest light-only content. MWO tied them to the big battle ("increase the odds" of a Siege win), and that link is a template for a campaign chain: a scouting sortie that buffs the follow-up assault.
- **AI-escort objectives die on pathing** (MWO's Escort VIP getting stuck). IRONFEUD's AI already steers around obstacles "the way the legs meet them". An escorted unit must reuse that steering and be pinned by headless tests, or the mode will be hated.
- **Drone or remote spotting can erase the scout's risk** (WT). If IRONFEUD adds remote sensors or support calls that reveal enemies, keep lights' value tied to exposure (line of sight, signature reveal on firing).

### Gaps
- No current (2024–26) MWO data on win rates by mode or class, nor on whether lights cap more in Conquest. The official MWO wiki is offline, and only a 2022 archive snapshot was readable.
- I could not read the full Escort or Conquest forum threads (Cloudflare). Their claims rest on search snippets.
- No reliable source on Planetside 2 "ghost capping" or Flash rushes, or on MW:LL Terrain Control roles.
- I found no developer analysis (GDC etc.) of class-role usage per mode in MWO or WoT.

---

## 2. Single-player mech mission types: which reward speed, sensors, stealth or endurance?

### Takeaway
HBS BattleTech's contract list is the clearest catalogue of objectives tilted toward speed or toughness. The wiki text itself prescribes the mech type for each: fast mechs for Target Acquisition, Assassinate, Escort, Recovery and Ambush Convoy; armour and ammo for Defend Base and Capture Base; frontline mechs for Battle. In practice, players still filled their bays with assaults, because few contracts *forced* speed and there was no drop-weight limit. MW5's base procedural set (Raid, Defense, Assassination, Warzone, Multi-Mission, with demolition and destroy objectives) reviewed as repetitive, "kill anything red".

### Cited Findings
**HBS BattleTech (2018) contracts (official mechanics as described on the wiki)**
- **Ambush Convoy**: defeat a convoy before it reaches an escape region; the convoy flees when engaged; killing its defenders is optional. — [BattleTech Wiki: Contracts](https://battletech.fandom.com/wiki/Contracts)
- **Assassinate**: destroy one target; other kills optional. The wiki suggests "fast-moving sniper 'Mechs to strike the target and escape." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- A player guide says the target always has an escort lance and sometimes an extra "ambusher" lance (snippet). — [r/Battletechgame contract guide (mirror)](https://pol1.lr.ggtyler.dev/r/Battletechgame/comments/10paii3/battletech_contract_guide)
- **Attack and Defend** (DLC): take an enemy facility while defending the employer's. "You will have to split your lance to complete both objectives simultaneously." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Battle**: "a stand-up fight, so frontline combat units are recommended." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Capture Base**: clear and occupy a facility for N rounds against turrets and retaking forces; "Armor and sufficient ammunition are recommended." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Defend Base**: hold for N rounds; "Armor and sufficient ammunition for a long holdout engagement are more valuable here than speed and mobility." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Destroy Base**: destroy the marked buildings; defenders optional; long-range fixed defences, so "Mechs that can withstand sustained fire, or fire back from long range." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Escort**: reach a rendezvous, then protect a convoy to the exit; "The convoy will move quickly and could outpace slower 'Mechs." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Recovery**: reach a region, pick up the target, escape. The carrier is attacked "and thus should be able to withstand some punishment. Moving quickly to the escape region is critical." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Target Acquisition** (DLC): plant beacons to triangulate a facility under AA and ground defences; "Time Boxed. So bring Fast 'Mechs and move quickly." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- Payment scales with difficulty and reputation. Salvage is X/Y (priority picks / total). Trading pay and salvage for reputation is possible. A retreat with no objective done and no kill is "bad faith" (no pay, rep loss); one objective plus one kill counts as good faith and earns partial pay. — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- The Mission Control mod adds Solo Duel, Duo Duel and Blackout contracts (snippet). — [r/Battletechgame contract guide (mirror)](https://pol1.lr.ggtyler.dev/r/Battletechgame/comments/10paii3/battletech_contract_guide)
- Mission Control also varies spawn locations, lance counts, map boundary size and objectives at runtime, and can reuse story maps. — [GitHub: CWolfs/MissionControl](https://github.com/CWolfs/MissionControl)
- A player: "Right now my mech bay is all assault mechs. Because there is no reason to ever have a light or medium mech once you have better mech." The same thread proposed preset drop weights per mission and a "logistic" cost that rises with drop tonnage (snippet). — [Paradox forum: BattleTech what's next](https://admin-forum.paradoxplaza.com/forum/threads/battletech-whats-next.1094890/post-24303823)

**MechWarrior 5: Mercenaries**
- Between story missions the player runs one of five procedural types (Raid, Defense, Assassination, Warzone, Multi-Mission). Warzone unlocks with reputation and "puts you smack in the middle of massive conflicts, asking you to pick a side". In Multi-Mission, stages run back to back and damage carries over. — [Gaming Trend review](https://gamingtrend.com/feature/reviews/all-systems-nominal-mechwarrior-5-mercenaries-review/)
- Campaign listings show tonnage-limited contracts, e.g. a Raid with a Demolition secondary at Difficulty 5 and a **30 t tonnage limit**. — [Sarna: MW5 Campaign Missions](https://sarna.net/wiki/MechWarrior_5:_Mercenaries/Campaign_Missions)
- Solaris Showdown DLC adds arena duelling contracts on Industrial Hubs (snippet). — [Sarna: MW5 Solaris Missions](https://cfw.sarna.net/wiki/MechWarrior_5:_Mercenaries/Solaris_Missions)
- Heroes of the Inner Sphere adds airstrikes, including precision strikes called on TAG/NARC-marked targets (snippet). — [Epic Store: Heroes of the Inner Sphere](https://store.epicgames.com/p/mechwarrior-5--heroes-of-the-inner-sphere?lang=es-ES)
- On repetition: outside the story, objectives "boil down to 'kill anything red' and 'wreck everything that is standing'" (snippet). — [MWO forum (MW5 thread)](https://mwomercs.com/forums/topic/275786-mw5-pro-tip-buy-those-hero-mechs/nextunread)

**Earlier MechWarrior games**
- MW2: Mercenaries random-mission objectives "are always in the three basic categories of recon (explore a target without destroying it), destroy (obliterate a target), and defend (ensure a target is not destroyed)" (snippet). — [StrategyWiki: MW2 Mercenaries](https://strategywiki.org/wiki/MechWarrior_2:_Mercenaries)
- MW4: Mercenaries missions mix "recon, assault, and stealth objectives". The game has three endings that depend on mission choices. — [GameSpot MW4 Mercenaries review](https://www.gamespot.com/pc/sim/mechwarrior4mercenaries/review.html)

**Starsector / Battle Brothers (non-mech references)**
- Starsector bounty fleets scale with bounty level: 5 + 4×level fleet points of combat ships, quality = level/10. Person bounties only require killing the flagship. — [Starsector wiki: Bounty](https://starsector.wiki.gg/wiki/Bounty)
- "Analyze" missions (scan a domain probe or derelict) are intel missions shown only with a rough location, i.e. a pure search-and-reach task. — [Starsector wiki: Missions](https://starsector.wiki.gg/wiki/Missions)
- Battle Brothers contract types include short jobs (escort a person or caravan, hunt beasts, *scout an area*, destroy an encampment) and long jobs ("protecting a whole region for two weeks"). — [Battle Brothers Dev Blog #26](https://www.moddb.com/news/dev-blog-26-contracts-and-greenlight-update)

### Inferences
- **Speed-tilted (light/fast medium):** Target Acquisition (time-boxed beacons), Assassinate (strike and escape), Escort (fast convoy), Ambush Convoy (fleeing targets), Recovery (run to extraction), MW2-style recon (explore without destroying), Starsector-style "analyze" (find and reach). **Endurance-tilted (heavy/assault):** Defend Base, Capture Base (hold for N), Destroy Base under fixed defences, Battle. **Split-tilted (needs a mixed lance):** Attack and Defend.
- **BattleTech's lesson is that recommendation text is not enough.** Its catalogue *suggests* fast mechs, but four assaults could still brute-force most contracts. With no tonnage cap and no hard clock, the strongest lance wins every type, which is exactly IRONFEUD's anti-goal. The fix must be mechanical:
  - **Hard clocks** (Target Acquisition, MW5's 30 t contracts).
  - **Fleeing targets** that an assault cannot catch (Ambush Convoy).
  - **Objective geometry**: two sites farther apart than an assault can cover in the time limit.
  - **Drop-weight caps or a per-ton logistics cost.**
- **Recovery is a nice hybrid:** speed matters for the run, but the carrier gets focused, which suits a fast medium or a jump-capable heavy. Assigning the carrier is a natural AI order ("lancemate X carries").
- **Multi-Mission** (damage persists across stages) fits IRONFEUD's persistent component damage. It rewards endurance and repair-light builds, and pairs well with a fast first stage and a heavy second.

### Gaps
- No complete, sourced list of MW5 base-game mission types and their timers. Whether there is a standalone recon or scout type is unconfirmed; no source mentioned one.
- I could not read Coyote's Mission Pack's own description (Nexus page did not render). Known from a Steam post only: it adds procedurally generated types "Tower Defense, Multiple Objectives, Convoy Escort, etc." — [Steam MW5 discussion](https://steamcommunity.com/app/784080/discussions/0/4036976796099634426). Players reportedly call the pack's convoy-attack missions its hardest, because the whole convoy fights back (snippet, thread not verified).
- No data on actual mech-class usage per HBS contract type.
- MW3/MW4 mission lists (timed objectives, nav-point recon) were not found in citable form.

---

## 3. Mechanisms that reward light/fast units

### Takeaway
The mechanisms that demonstrably create light/fast value are:
- **Time pressure** (hard clocks, objective-only win before the clock).
- **Geometry** (objectives spread farther apart than slow units can cover).
- **Information as an objective or payout** (scans, marks, spotting credit).
- **Fleeing or mobile targets** (convoys, VIPs, assassination targets that retreat).
- **Single-life small-team modes with tonnage caps.**
- **Splitting pressure** (simultaneous attack and defend).

Each needs a counter-mechanism so it doesn't flip into "lights win everything". Turrets punish lone lights (MWO Incursion). Holding objectives rewards armour (BattleTech Defend/Capture). Escorts pull fights to a moving point.

### Cited Findings
- **Time limits / time-boxed objectives:** "This mission is Time Boxed. So bring Fast 'Mechs and move quickly." (BattleTech Target Acquisition). — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- XCOM 2's mission timers, concealment and the Avatar clock "force you to push, killing off the turtling that carried the first game" (review). — [GameGeeker XCOM 2 review](https://gamegeeker.com/games/xcom-2/review?market=JP)
- Timers "achieved their intended effect" but drew controversy. — [Game Maker's Toolkit transcript (Amara)](https://amara.org/videos/efYjdTqQadCr/en/3100944)
- Jake Solomon on the backlash: "I was very surprised. I hadn't had an inkling of that yet." — [Quarter to Three forum excerpt](https://forum.quartertothree.com/t/xcom-2-give-me-real-ap-or-give-me-death/76898?page=82)
- **Multiple far-apart objectives / resource races:** Conquest's 1 RP/s per zone, first to 750. Officially, a light lance "can easily snatch victory" from a firepower-focused enemy. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- Proposed fix for slow caps: about 70 s solo flip and rates scaled to map size. — [MWO forum: Overhauling Conquest](https://mwomercs.com/forums/topic/174254-overhauling-conquest)
- **Fetch/runner objectives:** Incursion Fuel Cells powering towers (jammer 2 min per cell, radar sweep, air strike on a turtling enemy). Lights do the cell running; a light used for the tower's sensor sweep supports a central brawl. — [MWO forum: Incursion](https://mwomercs.com/forums/topic/250237-incursion); [MWO patch notes](https://mwomercs.com/game/patch-notes/1798)
- **Counter that keeps it honest:** "a single light will get torn up by the defensive turrets quickly" in an enemy base. — [MWO forum: Incursion](https://mwomercs.com/forums/topic/250237-incursion)
- **Base rush by small forces:** "two medium mechs can easily wreck havok in an opposing base." — [MWO forum: Incursion](https://mwomercs.com/forums/topic/250237-incursion)
- **Diversion plus fast cap:** a diversionary force draws attention "while your faster 'Mechs go for the base capture" (MWO Assault). — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Scouting rewards / intel reveal:**
  - WoT pays the spotter for allies' damage on its spotted targets. — [Wargaming wiki: Credits](https://wiki.wargaming.net/en/Credits)
  - War Thunder: a 30 s mark, a kill bonus inside the window, and no bonus if the target was already visible. — [War Thunder wiki: Scouting](https://wiki.warthunder.com/mechanics/238-scouting)
  - MWO: Scouting mode outcomes improve the next Siege. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Artillery/air spotting:**
  - War Thunder Arcade grants an extra artillery and air-strike point for a successful scout. — [War Thunder wiki: Scouting](https://wiki.warthunder.com/mechanics/238-scouting)
  - MW5 Heroes of the IS: precision airstrikes on TAG/NARC-marked targets (snippet). — [Epic Store](https://store.epicgames.com/p/mechwarrior-5--heroes-of-the-inner-sphere?lang=es-ES)
  - BattleTech Target Acquisition: beacons "allow friendly faction forces to make their move." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Escorts that need screening / fast convoys:** the BattleTech convoy "will move quickly and could outpace slower 'Mechs." — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Convoy interception:** the convoy "will try to flee if engaged" (BattleTech Ambush Convoy). — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- **Extraction:** BattleTech Recovery ("Moving quickly to the escape region is critical"); MWO Escort (VIP to an extraction zone in time). — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts); [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- **Deployment budgets / tonnage caps:**
  - MWO Scouting drop deck of 55 t (IS) or 50 t (Clan), i.e. one light per player. — [MWO tonnage change](https://w.mwomercs.com/news/2016/12/1684-faction-play-tonnage-change-12152016); [MWO hotfix](https://ww9.mwomercs.com/news/2016/12/1685-hotfix-scheduled-for-20dec2016)
  - MW5 contract with a 30 t limit. — [Sarna: MW5 Campaign Missions](https://sarna.net/wiki/MechWarrior_5:_Mercenaries/Campaign_Missions)
  - A per-ton logistics cost was proposed for BattleTech (snippet). — [Paradox forum](https://admin-forum.paradoxplaza.com/forum/threads/battletech-whats-next.1094890/post-24303823)
- **Squad size vs detection/difficulty (stealth trade-off):** in XCOM 2 Long War 2, "the more a squad is infiltrated, the less enemy resistance there is", and squad size changes infiltration time (bigger squads are slower). — [CogConnected LW2 impressions](https://cogconnected.com/2017/01/xcom-2-long-war-2-impressions/); [Pavonis forum: infiltration math for small squad ops](https://www.pavonisinteractive.com/phpBB3/viewtopic.php?p=38956)
- LW2 squads run up to 8 soldiers on most missions, 10–12 on special ones. — [UFOpaedia: Long War 2](https://ufopaedia.org/Long_War_2)
- **Back-capping deterrence:** Planetside 2 makes capturing a base deep in enemy territory much slower, so lone raiders cannot flip the map unopposed (snippet). — [Ten Ton Hammer: Higby](https://www.tentonhammer.com/articles/matt-higby-talks-class-advancement-and-territory-capture-in-planetside-2.amp)

### Inferences
Mapping to IRONFEUD's existing systems (scans per lance, marks, ECM, jets, signature by size, firing reveals lights):
- **Intel as an objective.** "Scan N target classes" or "keep a scan on the enemy assault for a cumulative X s" supports a recon mission where kills are optional. A scan-uplink event mid-mission (WT's mark → strike point) could unlock a support call for the employer side. This reuses the existing scan and mark code.
- **Information-gated objectives.** Hide the enemy objective (supply cache, command unit, convoy route) until a scan finds it. Heavies can't commit until a light or ECM unit locates it. This uses existing fog: the AI only decides on scanned state.
- **Geometry and clock calibration.** At 130 / 80 / 60 / 40 km/h (36 / 22 / 17 / 11 m/s), a 1.5 km leg takes about 42 / 68 / 90 / 135 s. Pick objective spacing and clocks so a pure-assault lance physically cannot do two sites before the clock, while a pure-light lance cannot *hold* a site against a counterattack. Make that a generator constraint ("sum of required travel at class speed < T_limit for lights only").
- **Terrain only small or jumping units can use.** IRONFEUD has rough-ground speed caps and jets. Generators can put an objective on a ridge or behind a narrow pass that only jets or lights reach quickly.
- **Counter-mechanisms per light job:** turrets at bases (MWO), hold-for-N objectives (BattleTech), and an escort target that pulls the fight to it. These keep the matrix from tipping toward an all-light win.
- **Tonnage budget per contract** (a hard cap or BattleTech's proposed logistics cost) is the campaign-layer lever; mission rules are the battle-layer lever. Both are needed: caps alone make "the best 4 mechs under X tons" the meta. The sibling campaign notes cover upkeep.

### Gaps
- No quantitative evidence (win rates by class per mode) for any game. The class-value effects above come from design text and player guides, not data. The IRONFEUD headless matrix is the right instrument.
- No sourced XCOM 2 base-game timer numbers beyond player claims (8/12 turns, unsourced here), and no first-party rationale from Firaxis for timers.

---

## 4. Procedural mission generation: how MW5, HBS BattleTech, Starsector, Battle Brothers, XCOM 2 and others generate missions, what got repetitive and what fixed it

### Takeaway
The approaches that were praised are:
- XCOM 2's hand-built parcels in procedural plots, with designer-marked objective and spawn points.
- Battle Brothers' contracts generated from real world state: the bandits actually raid before the contract appears, and finishing it changes the settlement's situation.
- Contract terms (payment by head vs flat, advance) that change how a mission is played.

The approaches that were criticised are MW5's small template set over procedural maps ("kill anything red") and story missions that use the same templates. The fixes were modded mission packs (more types, multiple objectives, convoys, tower defense), runtime encounter variation (BattleTech Mission Control: spawns, lance count, boundaries, objectives), and later DLC twists (surprise artillery on raids).

### Cited Findings
**XCOM 2 (Firaxis)**
- XCOM: Enemy Unknown avoided procedural maps because they couldn't look convincing and stay mechanically interesting. XCOM 2 uses **plot and parcel**: the plot is the base layer and parcels (a parking lot, a building) are inserted. "We simply punch out the individual tiles in the quilt and replace them with whatever we want." The system "never creates the same map twice … You may recognize a building or two, but you don't recognize the overall map." — [Game Developer: Jake Solomon on randomness in XCOM 2](https://www.gamedeveloper.com/design/jake-solomon-explains-the-careful-use-of-randomness-in-i-xcom-2-i-)
- Dark Events (random enemy strategic buffs, often simultaneous) caused difficulty spikes. Solomon: "if you shave off the lows, a lot of the time you're shaving off the highs." At lower difficulties the displayed hit chance is quietly favourable (85% shown is closer to 95%). — [Game Developer](https://www.gamedeveloper.com/design/jake-solomon-explains-the-careful-use-of-randomness-in-i-xcom-2-i-)
- GDC 2018 talk "Plot and Parcel: Procedural Level Design in XCOM 2" (Brian Hess, Firaxis lead level designer). — [GDC Vault](https://gdcvault.com/play/1025213/Plot-and-Parcel-Procedural-Level)
- Designers hand-mark key mission objectives and spawn points, and the rest is filled procedurally (snippet). — [GDC news](https://gdconf.com/news/come-learn-xcom-2s-procedural-level-design-works)
- Long War 2: Pavonis wanted squad size determined organically rather than by fixed upgrades. Difficulty scales with strategic conditions and squad size (snippet). — [CogConnected](https://cogconnected.com/2017/01/xcom-2-long-war-2-impressions/)

**Battle Brothers (Overhype Studios)**
- "Contracts aren't just random missions thrown at you. They are the result of what really is going on in the world; if a caravan travels on a long journey through dangerous lands, it will offer an escort contract … The bandits won't be spawned just for the duration of the contract … Defeating their raiding parties will really lower bandit activity for a while, and burning down their camp may end it completely." — [Dev Blog #26](https://www.moddb.com/news/dev-blog-26-contracts-and-greenlight-update)
- Only one contract at a time, to avoid "an endless list of quests which you just work through." — [Dev Blog #26](https://www.moddb.com/news/dev-blog-26-contracts-and-greenlight-update)
- Variety from terms: "if you're promised 10 silver crowns for every orc head, you might want to take more risks to hunt down every last one as opposed to when you're promised a flat sum." Each contract has a story paragraph ("never just be a few bare bulletpoints, a price and an accept button"). — [Dev Blog #26](https://www.moddb.com/news/dev-blog-26-contracts-and-greenlight-update)
- **Situations**: settlement states (people disappearing, freshly supplied castle, granaries burned) change recruits and prices. Contracts resolve or create them; e.g. a successful caravan escort can resupply a castle, and a failure can leave granaries raided. — [Dev Blog #95](https://www.moddb.com/news/dev-blog-95-contract-changes)
- The negotiation rework made pay fixed per contract (type, difficulty, renown, a small random factor). Players now negotiate *modalities* (advance %, per-head vs flat), because "you just ask for more money until the employer denies" was "not an interesting mechanic." — [Dev Blog #95](https://www.moddb.com/news/dev-blog-95-contract-changes)

**MW5: Mercenaries**
- Reviews on repetition:
  - "Really repetitive … a grind that had little payoff." — [Windows Central](https://windowscentral.com/mechwarrior-5-mercenaries-review)
  - "Unbelievably boring and repetitive" missions. — [TechRaptor](https://techraptor.net/gaming/reviews/mechwarrior-5-mercenaries-review)
  - Milder: varied environments mitigate the repetition. — [TechSpot](https://www.techspot.com/products/pc-games/mechwarrior-5-mercenaries.212499/)
- A structural critique: the loop "would be much more compelling if there was much to differentiate the story missions from the optional procedural ones." — [Film Stories](https://filmstories.co.uk/reviews/mechwarrior-5-mercenaries-review-nu-metal/)
- Campaign missions "rarely deviate from the template used in the normal missions." — [TheSixthAxis](https://www.thesixthaxis.com/2021/09/28/mechwarrior-5-mercenaries-review-legend-of-the-kestrel-lancers/)
- Later content added variants such as surprise artillery on Raid missions (snippet). — [Thumb Culture](https://www.thumbculture.co.uk/mechwarrior-5-mercenaries-review)
- Modded fixes: Coyote's Mission Pack adds procedurally generated types (Tower Defense, Multiple Objectives, Convoy Escort, etc.). — [Steam MW5 discussion](https://steamcommunity.com/app/784080/discussions/0/4036976796099634426)
- "Better Mission Choices" doesn't add types but "greatly expands options for missions generation." — [Steam MW5 discussion](https://steamcommunity.com/app/784080/discussions/0/4036976796099634426)

**HBS BattleTech**
- Contracts are issued by factions, with difficulty-scaled pay, negotiated pay/salvage/reputation, and bonus pay for secondary objectives. — [BattleTech Wiki: Contracts](https://battletech.fandom.com/wiki/Contracts)
- Mission Control varies spawn locations, lance numbers, map boundary size and objectives at runtime, and adds new contract types (e.g. Blackout). — [GitHub: MissionControl](https://github.com/CWolfs/MissionControl)

**Starsector**
- Bounty strength is a formula of bounty level (5 + 4×level FP, quality level/10). Intel missions (analyze probe/derelict) give only a rough location. — [Starsector wiki: Bounty](https://starsector.wiki.gg/wiki/Bounty); [Starsector wiki: Missions](https://starsector.wiki.gg/wiki/Missions)

### Inferences
- **Generate missions from campaign state, not dice.** IRONFEUD's deterministic campaign sim (factions, convoys, bases) can emit BattleSetups the way Battle Brothers does. A real enemy convoy on a route yields an Ambush Convoy. A real raid on a planet yields a Defend. The outcome writes back as a "situation" (supply up or down, garrison weakened). This also gives auto-resolve a natural hook: missions the player declines resolve headlessly.
- **Variation axes beyond map** (cheap, high-yield):
  - **Objective template**: from the catalogue in sections 2–3.
  - **Geometry**: number of sites, spread, terrain access.
  - **Clock**: none, soft (bonus) or hard.
  - **Information state**: known vs hidden objective; prior scouting intel bought by an earlier mission.
  - **Opposing force composition and doctrine.**
  - **Mid-mission event**: reinforcements, artillery, convoy breakout, weather or sensor blackout.
  - **Payment modality**: per kill vs flat vs per objective vs salvage share. Battle Brothers shows these change behaviour.
  - **Tonnage or drop cap.**
- **Plot-and-parcel for IRONFEUD maps:** hand-authored "parcels" (a base compound, a convoy road segment, a ridge with a jet-only approach) dropped into procedural terrain, with designer-marked objective and spawn sockets. That keeps objective geometry tunable (needed for class-speed calibration) while maps vary.
- **Story missions must not reuse procedural templates verbatim** (the MW5 complaint). Reserve unique mechanics (multi-stage, asymmetric, scripted twists) for authored content, or use them rarely in procedural content.
- **One contract at a time** (Battle Brothers) avoids grinding a queue, and is a good default for a single-player merc company.

### Gaps
- Mount & Blade quest-generation criticism and fixes are not covered here (see the sibling campaign notes, which treat M&B).
- I could not access the full XCOM 2 GDC talk content (login wall). Its specifics on objective sockets and mission-type constraints are unverified.
- Coyote's Mission Pack author's rationale and full type list are not verified.
- No sourced detail on MW5's internal generator (how difficulty, tonnage and objective combos are rolled).

---

## 5. Asymmetric missions, reinforcements/waves and dynamic mid-mission events

### Takeaway
Asymmetry works when each side's *job* differs, not just its numbers. Examples: attacker with a ticket or time budget vs defender with unlimited respawns (Rush), attacker who must destroy layered infrastructure vs defender who must last (Siege, Counter Attack), escort vs interceptor (MWO Escort, BattleTech Escort/Ambush Convoy). Common failure points are objective exposure tuning (too exposed vs too protected), pointless late game (the last tickets can't win), and AI escort pathing. Waves and reinforcements (Titanfall Frontier Defense, BattleTech ambusher lances, retaking forces in Capture Base) and mid-mission surprises (MW5 artillery on raids) are the main tools for dynamic missions. AI minions on objectives are only meaningful when the mode is about the objective.

### Cited Findings
- Rush: attackers have limited team tickets, defenders unlimited. An armed M-COM continues the round past 0 attacker tickets (BF3). Ticket count and M-COM layout were retuned (2014 patch). — [Battlefield Wiki: Rush](https://battlefield.fandom.com/wiki/Rush)
- Critics say BF6 Rush objectives feel either "too exposed" for attackers to defend or "too protected" for defenders to lose, and that the last tickets are "essentially useless". The proposed fix: lose only at 0 tickets with no M-COM armed. — [BF feedback thread](https://app.betahub.io/projects/pr-7682550116/feature_requests/593)
- MWO Siege is layered: gate generators, then 3 inner generators, then the SDS, with 12v12 respawns. Counter Attack inverts it: defenders win by surviving the timer, and attackers must kill all *and* destroy the Mobile Field Base. A prior Scouting mode improves Siege odds. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes)
- Faction Play tonnage was asymmetric by faction (IS Invasion 265 t vs Clan 240 t, Dec 2016) to balance tech differences. — [MWO Faction Play Tonnage Change](https://w.mwomercs.com/news/2016/12/1684-faction-play-tonnage-change-12152016)
- MWO Escort: an AI VIP defended to extraction against attackers. Criticised for the VIP getting stuck, and reportedly removed for low popularity. — [MWO Wiki (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Game_Modes); [MWO forum (snippet)](https://mwomercs.com/forums/topic/263389-escort-replacement-concept); [wikiwiki.jp (snippet)](https://wikiwiki.jp/mwojp/Escort)
- BattleTech reinforcements:
  - Capture Base has "additional enemy units that will attempt to retake the base".
  - Assassinate targets have an escort lance and sometimes an ambusher lance (snippet). — [r/Battletechgame guide (mirror)](https://pol1.lr.ggtyler.dev/r/Battletechgame/comments/10paii3/battletech_contract_guide)
  - Escort convoys are "attacked by enemy units on the way". — [BattleTech Wiki](https://battletech.fandom.com/wiki/Contracts)
- Mission Control randomises lance numbers and spawn locations at runtime. — [GitHub: MissionControl](https://github.com/CWolfs/MissionControl)
- Titanfall 2 Frontier Defense: co-op wave defence of a Harvester. — [Neowin](https://www.neowin.net/news/titanfall-2s-upcoming-free-dlc-brings-the-cooperative-four-player-mode-frontier-defense)
- Titanfall Hardpoint: minions stream to the capture points and their fights decide who starts capping. Respawn framed whether to clear them as a player choice that depends on the mode. — [Xbox Wire](https://news.xbox.com/en-us/2014/02/18/games-titanfall-ai/); [Prima (snippet)](https://primagames.com/eguides/titanfall-eguide/training/game-modes)
- MW5: mid-mission surprises such as artillery on Raid missions arrived with later content (snippet). — [Thumb Culture](https://www.thumbculture.co.uk/mechwarrior-5-mercenaries-review)
- In MW5 Multi-Mission, stages run back to back and damage carries over. — [Gaming Trend](https://gamingtrend.com/feature/reviews/all-systems-nominal-mechwarrior-5-mercenaries-review/)
- XCOM 2 Dark Events: random simultaneous modifiers caused hard-to-tune spikes ("if you shave off the lows … you're shaving off the highs"). — [Game Developer](https://www.gamedeveloper.com/design/jake-solomon-explains-the-careful-use-of-randomness-in-i-xcom-2-i-)
- Battle Brothers: outcome-driven settlement situations make the *next* contract context differ, a cross-mission dynamic event. — [Dev Blog #95](https://www.moddb.com/news/dev-blog-95-contract-changes)

### Inferences
- **Asymmetric compositions fall out naturally from objectives.** A light raiding lance vs a heavy garrison (Raid/Destroy under a clock). A heavy convoy guard vs a fast interceptor lance (Ambush Convoy). An assault siege lance vs a mixed defence with a light screen (Siege). For the "no composition wins every mission" test, run attacker-side and defender-side compositions separately. Asymmetric missions are where class roles are clearest.
- **Late-game futility (Rush) maps to IRONFEUD as:** once the clock or objective is mathematically lost, end or escalate rather than grind. E.g. "objective armed continues the mission" (BF3) so a last-ditch light dash stays meaningful.
- **Reinforcements must be visible and timed** to stay "readable, fallible, never a pattern to farm". Telegraph the inbound dropship or lance arrival, and vary the arrival window and entry edge from campaign state (where the enemy garrison actually is) rather than fixed timers. BattleTech's ambusher-lance chance and Mission Control's random spawns show the variation lever. XCOM's Dark Events show the risk of stacking random modifiers, so cap simultaneous modifiers per mission.
- **Mid-mission event catalogue fitting IRONFEUD:**
  - Convoy breakout (targets start fleeing).
  - Enemy artillery or support call on your scanned position (punishes static assaults, rewards movers).
  - Sensor blackout or ECM storm (devalues scan-dependent play for a window).
  - Objective relocation (an intel update moves the target).
  - Allied reinforcements gated on completing a scout or beacon objective (BattleTech Target Acquisition / WT strike points).
  - Extraction window opens late (endurance).

  Each shifts class value in a known direction and can be tagged per class in the generator.
- **AI-controlled escorted units** (VIP, convoy) are a dependency on the AI steering stack. Escort missions need headless regression tests of the convoy completing its route unopposed before balance runs.

### Gaps
- No primary developer analysis (GDC/postmortem) of asymmetric mode balance in MWO Faction Play, Rush or Frontier Defense was found.
- No data on how often MWO Siege attackers won, or on class usage in Siege vs Scouting.
- Frontier Defense specifics (wave count, titan locking) were only in unverified snippets and are omitted.
