# Squad / Lancemate Command Interfaces and Buddy-AI Design (for a game where the player also fights)

Scope: command UX in shooters and mech games where the player is also a combatant, time-control options, buddy-AI
design lessons from shipped games, and what makes a squad feel trustworthy. Written for IRONFEUD (original IP; the
player pilots one mech and commands 3+ AI lancemates, later a company; the AI uses the same `MechInput` as the
player and decides only on shared scans). Inferences are marked as such and are not sourced facts.

Source quality note: the strongest primary source found is Max Dyckhoff's Game AI Pro 2 chapter on Ellie (read
in full). Most command-UX facts come from reviews, guides and wikis; developer postmortems on command-menu design
(SWAT 4, Republic Commando, MW4) were not found. Where a fact comes only from a search-engine summary of a page
rather than a page I read, it is marked "(via search summary)".

## 1. Command UX patterns, and which scale from 3 squadmates to a company

### Takeaway
Shipped games converge on three layers: (a) a handful of one-key "intent" orders that work in combat without
looking away (attack my target / follow / hold, tap = one unit, hold = all), (b) context-sensitive orders resolved
from what the crosshair points at (door, cover, enemy), and (c) an overview/map layer for positional orders. Only
(a) plus a grouping layer (select group, then order, as in Mount & Blade) scales to a company; per-unit context
menus do not.

### Cited Findings
**Mech games**
- MechWarrior 4: Mercenaries issues lancemate orders through the F keys, which a reviewer called "fairly intuitive";
  the review says lancemates follow orders well. Exact MW4 key mapping not confirmed. — [GameWatcher MW4:Mercs review](https://www.gamewatcher.com/mechwarrior-4-mercenaries/review) (via search summary)
- MechWarrior 5: Mercenaries default bindings (per a key-binding guide): F1 = command to whole lance / "Attack my
  Target", F2 = command to Lancemate 1 / "Form on me", F5 = cancel orders. — [MagicGameWorld MW5 key bindings](https://www.magicgameworld.com/mechwarrior-5-mercenaries-pc-keyboard-controls-and-key-bindings/) (via search summary)
- MW5: Clans offers either the "Battlegrid" overhead map or a quick command wheel; the wheel addresses the whole
  squad or single pilots with hold-area, regroup and attack-target. The previewer called the AI "the biggest
  problem" and suggested a cautious playstyle to compensate. — [playday.one MW5 Clans hands-on preview](https://playday.one/2024/09/24/mechwarrior-5-clans-hands-on-preview/)
- On the Battlegrid you select mechs and right-click ground to move, right-click a hostile to attack.
  CGMagazine calls the system a good idea but flags that gameplay does not stop while you issue commands. — [CGMagazine MW5 Clans review](https://www.cgmagonline.com/review/game/mechwarrior-5-clans-pc-review) (via search summary)
- A Steam player reports MW5 Clans AI "will usually not be very effective at fighting unless you give them orders";
  another thread asks for a review of AI movement/pathing. — [Steam MW5 Clans discussion](https://steamcommunity.com/app/2000890/discussions/0/4691153988128083747) (via search summary; anecdotal)

**Fire-team shooters**
- Brothers in Arms: Road to Hill 30: one or two 1-3 man teams per level. Fire team (Garand/BAR) pins enemies with
  suppressive fire; assault team (carbine/Thompson) flanks while the fire team holds. Orders: advance, suppress,
  regroup, take cover, attack. The tutorial teaches "Find, Fix, Flank, Finish". Weapons have deliberately erratic
  accuracy so the player relies on the team rather than marksmanship. Retired US Army Col. John Antal consulted. — [Wikipedia: BiA RtH30](https://en.wikipedia.org/wiki/Brothers_in_Arms:_Road_to_Hill_30)
- BiA criticism: Sydney Morning Herald found most encounters solved the same way (suppress then flank); Maxim
  found squad management possibly tedious but "a much-needed dimension". A reviewer found the button-activated
  overhead view disorienting. — [Wikipedia: BiA RtH30](https://en.wikipedia.org/wiki/Brothers_in_Arms:_Road_to_Hill_30)
- BiA suppression feedback: when suppression lands the enemy indicator turns grey and enemy fire rate drops,
  opening a window to flank; a fire order can be chained after a move order; enemies often stay put while being
  flanked. — [GameSpot BiA walkthrough](https://gamespot.com/articles/brothers-in-arms-road-to-hill-30-walkthrough/1100-6120725/) and reviews (via search summary)
- Freedom Fighters (2003): three orders (attack/scout, follow, defend) on three face buttons; **tap sends the order
  to the nearest recruit, hold sends it to the whole squad**. The attack order doubles as a directional move order
  (look in a direction, press). Squad cap of 12, recruited via a "charisma" score earned by rescuing people and
  completing goals. A preview said the interface made it easy to command a squad effectively in the heat of battle. — [GameSpot Freedom Fighters review](https://www.gamespot.com/reviews/freedom-fighters-review/1900-6075496/), [GameSpot preview](https://www.gamespot.com/articles/freedom-fighters-preview/1100-6073064/) (via search summary)
- Star Wars: Republic Commando: one context-sensitive command button resolves against marked battlefield spots
  (e.g. rubble as sniper position, panels to hack); tap issues the context order, hold opens a menu for secure
  area / fire on one target / form up / cancel. A critic called the squad command system "the unique selling point"
  on which "most of the game's imagination" was spent. — [Giant Bomb](https://giantbomb.com/wiki/Games/Star_Wars_Republic_Commando), [GameSpot user review](https://www.gamespot.com/star-wars-republic-commando/user-reviews/2200-12554576/), [Scientific Gamer](https://scientificgamer.com/thoughts-republic-commando/) (via search summary)
- SWAT 4: context menu keyed to the crosshair; right-click a door for e.g. "open, flashbang and clear" or "stack
  up"; a locked door offers pick lock / breaching charge / breaching shotgun. Four officers split into red and blue
  pairs; Tab switches which element (whole squad, red, blue) the menu commands. — [Worthplaying SWAT 4 review](https://www.worthplaying.com/article/2005/5/18/reviews/24695-pc-review-swat-4/), [GameSpot SWAT 4 review](https://gamespot.com/reviews/swat-4-review/1900-6121423/) (via search summary)
- Ready or Not players ask for SWAT 4 / Arma-style order menus and specific missing compound orders (e.g. open/breach
  and throw grenade without entering). — [Steam RoN discussion](https://steamcommunity.com/app/1144200/discussions/0/4633736978453592143) (via search summary; anecdotal)
- Full Spectrum Warrior (built from a US Army / USC ICT training project): the player never aims or fires
  (except grenades); fireteams are ordered to positions RTS-style; reviewers note a lag between order and
  execution. The Army technical report covers point fire, suppression fire and individual cover sectors. — [GameSpot FSW review](https://www.gamespot.com/xbox/strategy/fullspectrumwarrior/review.html), [DTIC ADA428443](https://apps.dtic.mil/sti/tr/pdf/ADA428443.pdf) (TOC only seen)
- Ghost Recon Wildlands/Breakpoint: "sync shot" lets the player tag targets and have teammates fire on the
  player's trigger. Breakpoint added AI teammates in a free update (July 2020): orders such as Cleared Hot, Go To,
  Regroup were team-wide only at first (no individual orders); later TU 4.0.0 (May 2021) added squad-size control
  (up to 3, toggled individually) and fixed AI killing enemies in breachable buildings from outside with a sync
  shot. Drone sync shots plus AI teammates allowed up to seven simultaneous kills. — [Ubisoft news](https://news.ubisoft.com/en-us/article/4SyaGiGbgskidYNiaxKGx4/ghost-recon-breakpoint-its-the-best-time-to-jump-back-in), [GameSpot](https://www.gamespot.com/articles/ghost-recon-breakpoint-update-ai-squadmates-free-t/1100-6479870/), [mp1st TU 4.0.0](https://mp1st.com/title-updates-and-patches/ghost-recon-breakpoint-update-1-15-may-25-shoots-out-for-tu-4-0-0), [Contains Moderate Peril](https://www.containsmoderateperil.com/blog/category/Ghost+Recon+Wildlands) (via search summary)
- Wildlands players report sync shots refused for lack of line of sight, and (as a bug) squad sync-tagging through
  multiple buildings; reviewers were split between "dependable, always has your back" and "bullet sponges". — [Steam Wildlands discussion](https://steamcommunity.com/app/460930/discussions/0/1489992080501136699), [Memeburn beta hands-on](https://memeburn.com/gearburn/2017/02/hands-on-ghost-recon-wildlands-beta/) (via search summary; anecdotal)

**Large-unit command while fighting**
- Mount & Blade II: Bannerlord: select group with number keys (1-4 typical, up to 8 groups), then a two-key
  F-sequence: F1 movement (move to position, follow me, charge, advance, fall back, stop, retreat), F2 facing,
  F3 formation (line, shield wall, loose, circle, square, skein, column, scatter), F4 fire at will / hold fire;
  Backspace opens an order-of-battle overview. Guides disagree on exact layout across early-access versions. — [Gamepur Bannerlord commands](https://www.gamepur.com/guides/troop-commands-in-mount-and-blade-ii-bannerlord), [GamerGuides tactics](https://gamerguides.com/mount-and-blade-ii-bannerlord/guide/introduction/useful-gameplay-tips/tactics) (via search summary)

**RPG party command**
- Mass Effect 2: power wheel pauses combat; one power per squadmate can be queued while it is open. BioWare wanted
  the wheel less intrusive because constant pausing reduced intensity, so a few powers can be mapped to buttons;
  D-pad orders squadmates to attack/move/rally without the wheel. Engadget said the pause never felt like the flow
  was interrupted and squad powers felt like an extension of the player's own character. — [Engadget ME2 review](https://www.engadget.com/2010-01-26-mass-effect-2-review.html), [Escapist: five things fixed in ME2](https://www.escapistmagazine.com/five-things-bioware-fixed-for-mass-effect-2/), [Fextralife ME2 combat](https://masseffect2.wiki.fextralife.com/Combat) (via search summary)
- Final Fantasy XII gambits: each gambit = target + condition + action, evaluated top-down by priority (first
  satisfied fires); direct player commands always override gambits; 4 slots at start, up to 12 via the License
  Board. — [Wikipedia: FFXII](https://en.wikipedia.org/wiki/Final_Fantasy_XII), [StrategyWiki gambits](https://strategywiki.org/wiki/Final_Fantasy_XII/Gambits) (via search summary)
- Dragon Age: Origins "Combat Tactics" slots set automatic reactions per character; players complain slots are scarce
  (cost skill points); some forum users found tactics of little use and RtwP micromanagement heavy. — [GamesRadar DA:O review](https://gamesradar.com/dragon-age-origins-13/2), [Obsidian forum thread](https://forums.obsidian.net/topic/68440-pause-with-real-time-combat/) (via search summary; forum opinion)

### Inferences
- Pattern A, "intent keys" (Freedom Fighters, MW4/MW5, Mass Effect D-pad): 3-4 verbs, tap = one/nearest, hold = all.
  Usable mid-fight and the only layer players reliably use (consistent with prior research that MW5 players mostly
  use attack-my-target / follow / hold). Must exist in IRONFEUD and be complete on its own.
- Pattern B, "context order at the crosshair" (Republic Commando, SWAT 4): one button, the meaning comes from what you
  aim at: enemy = focus it; cover/ridge = take position there; friendly = escort/guard; ground = move. Fits a mech
  game well because the torso reticle is already a precise pointer. Republic Commando relies on designer-placed
  markers; IRONFEUD would need to infer them from `ArenaMap` cover queries (hidden designer work otherwise).
- Pattern C, "fire team doctrine" (BiA, FSW): orders target *roles* (base of fire vs manoeuvre element), and the
  game shows the effect (suppression indicator). The mech analogue: a heavy/assault as the base of fire and lights as
  the flanking/spotting element; this directly supports CLAUDE.md design goal 4 (every weight class has a job). The BiA
  criticism (every fight solved the same way) warns that a single dominant doctrine becomes a farmable pattern.
- Pattern D, "sync/coordinated strike" (Ghost Recon): mark N targets, release on the player's trigger. A mech
  version: "fire on my mark" alpha strike for breaking a section. Its documented failure modes are LoS refusals
  and shooting through walls; IRONFEUD's "same inputs, no cheats" rule avoids the latter but then has to explain
  refusals (bark "no shot") or players read it as broken.
- Scaling: per-unit context menus (SWAT 4) work for 2 elements; Bannerlord works for hundreds because orders go to
  *groups* and are positional/formation verbs. For a company (3 lances): lance = group key, intent keys go to the
  selected lance, lance leaders (AI) translate into per-mech actions. That matches IRONFEUD's existing squad
  coordinator: the player's order should become one more input to the coordinator, not a direct per-mech override.
- The Breakpoint arc (team-only orders, then individual toggles added later after feedback) suggests shipping
  "all" plus "one" addressing from the start.

### Gaps
- Exact MW4 key layout and order list (manual not retrieved). The MW5 Mercs manual PDF exists at
  static.mw5mercs.com but was not read.
- No developer postmortem found for SWAT 4, Republic Commando or Freedom Fighters command-UI design.
- No source covered Arma/Squad high-command UX, Halo Wars, or Battlefront ally commands in this pass.
- No usage telemetry (which orders players actually issue) was found for any game.

## 2. Time control: real-time-with-pause, slow-mo and non-pausing command views

### Takeaway
Pausing command layers work when they are short, optional and the fast path exists without pausing (ME2: wheel
pauses, but buttons and D-pad bypass it). A non-pausing overhead map (MW5 Clans) is called a good idea that
critics fault for not stopping. Real-time-with-pause is divisive in RPGs; configurable auto-pause is the usual
compromise.

### Cited Findings
- ME2: BioWare reduced reliance on the pausing wheel because constant pausing reduced intensity; mapping powers to
  buttons let players use squad powers "without having combat grind to a halt". — [Escapist](https://www.escapistmagazine.com/five-things-bioware-fixed-for-mass-effect-2/), [Game Informer ME2 preview](https://gameinformer.com/games/mass_effect_2/b/xbox360/archive/2009/09/29/preview.aspx) (via search summary)
- Engadget: the ME2 power-wheel pause never felt like an interruption of battle flow. — [Engadget](https://www.engadget.com/2010-01-26-mass-effect-2-review.html) (via search summary)
- MW5 Clans' Battlegrid does not pause; CGMagazine flags this as the system's problem. — [CGMagazine](https://www.cgmagonline.com/review/game/mechwarrior-5-clans-pc-review) (via search summary)
- Dragon Age: Origins can be paused at any moment to issue orders and has an option to auto-pause when combat
  starts. — [Steam DA:O discussion](https://steamcommunity.com/app/47810/discussions/0/1474221865204456025) (via search summary)
- Pillars of Eternity: an Obsidian-forum user said RtwP plays "closer to turn based"; a commenter argued real time plus
  *configurable* automatic pause is the best balance. In Nov 2025 Obsidian added a turn-based mode to PoE1, swappable
  with RtwP in options. — [Obsidian forums](https://forums.obsidian.net/topic/68440-pause-with-real-time-combat/), [GamingOnLinux](https://www.gamingonlinux.com/2025/11/the-original-pillars-of-eternity-is-getting-a-turn-based-mode-beta-on-november-5/page=1/) (via search summary)
- A PC Gamer column (Jody Macgregor) argues against RtwP, saying developers should choose turn-based or full real
  time; it cites DA's long attack animations as hiding idle time. — [GameBanshee summary of the column](https://gamebanshee.com/k3rhc) (via search summary)
- Full Spectrum Warrior shows a lag between order and execution (noted by reviewers). — [GameSpot FSW review](https://www.gamespot.com/xbox/strategy/fullspectrumwarrior/review.html) (via search summary)
- BiA reviewer found the overhead view disorienting. — [Wikipedia: BiA](https://en.wikipedia.org/wiki/Brothers_in_Arms:_Road_to_Hill_30)

### Inferences
- For IRONFEUD the options are: (1) no time control, intent keys only; (2) "tactical slow" (e.g. 25% time while a
  command overlay is open), consistent with the 120 Hz fixed tick if implemented as a time-scale on the sim, not by
  skipping AI ticks; (3) full pause map. Since AI and player share `MechInput`, slow-mo is fair: everyone slows
  equally. A pause map makes the player strictly superior to AI lance leaders (they cannot pause), which is fine for
  commanding friendlies but should be a difficulty/immersion option.
- Diegetic framing avoids the immersion break: a cockpit "battle computer" overlay (the existing scan/sensor-lock
  model is a natural data source: the map shows only what the lance has scanned, keeping the perception rule).
- Command latency is a design lever: FSW's lag is realism; for a mech pilot, a short acknowledgement bark +
  visible turn toward the objective hides latency and gives feedback.
- Headless tests cannot judge feel here; this needs playtesting by the user.

### Gaps
- No primary source found on how MechWarrior or BattleTech-adjacent games tested pause vs no-pause; no data on
  Brigador (no squad command found).
- No source on slow-motion command modes specifically (e.g. bullet-time order menus) was retrieved.

## 3. Buddy/squadmate AI design: competence, readability, not stealing kills or blocking fire

### Takeaway
The best-documented shipped buddy (Ellie, The Last of Us) is built on: stay close and in the player's "room",
never stand in front of the player, mirror the player's posture, never break what the player is trying to do
(stealth), keep combat contribution consistent with balance (damage only lands when the player sees it or in key
moments), and put special helpful acts on long timers so they stay memorable. Callouts must only reference threats
the player can then see. Halo's lesson: mistakes are fine if players can understand them and correct them.

### Cited Findings
**Ellie, The Last of Us (Max Dyckhoff, Game AI Pro 2 ch. 35; read in full)** — [PDF](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter35_Ellie_Buddy_AI_in_The_Last_of_Us.pdf)
- Early buddies "position[ed] themselves awkwardly, shooting at the wrong time, and generally getting underfoot",
  so they had to be scripted to "stay back", which removed them from the encounter: unacceptable.
- Plan in order: (1) keep buddies very close; (2) give utility (noncombat and combat); (3) add character touches
  (vocalizations, ambient animation, conversations). Built in ~5 months by essentially one core engineer.
- Rationale for proximity: if she shares the player's space and behaves similarly, "her actions can by definition be
  no more stupid than what the player is doing"; if she is seen while positioned correctly, the player was seen too
  and blames themselves.
- Follow positions: candidates in a torus around the leader from navmesh rays; reject positions facing a wall;
  ray from the player to each candidate's "forward" point so the buddy stays in the same "room" (without it Ellie
  stood on the other side of doors/fences). Rated every frame on: distance to leader, staying on the same side of
  the leader, visibility of potential targets (hiding or exposing), **not being in front of the player**, distance
  to other buddies; criteria include smoothing against per-frame noise.
- Movement filters: suppress short moves unless needed (e.g. exposed); avoid running past the leader; "close
  enough" arrival; speed by distance (walk/run/sprint) with animation speed scaled up to 25% to avoid oscillating
  between gaits.
- Dodging: buddy does not pre-emptively move out of the player's way; only at the last second a canned dodge plus
  a vocal complaint, turning a frustrating behaviour into character.
- No teleporting to keep up (audio and believability); the only teleport is to rescue the player from a grapple,
  when camera control is locked so it cannot be seen.
- Cover: runtime cover generation (tool cover edges plus raycast-derived edges, cached); rated by visibility to
  enemies, proximity to leader, and *predicted future visibility* from projected enemy movement. Cover sharing with
  the player instead of popping out when the player approaches.
- Combat ideals: support the player rather than fight separately; perform consistently so encounter balance holds;
  never "overpower an encounter and make the player feel useless"; the player must *see* the buddy acting, otherwise
  "it is wasted effort".
- Brick throw triggered by predicting an enemy is about to see the player; on a long timer. Self-escape from
  grapples, then not targetable for 15-30 s (saving her every time was an escort-quest chore). Gifting ammo/health
  through the existing drop system, on a long timer (a stuck player got ammo every minute, which devalued it).
- Shooting logic inverted: she wants *not* to shoot and gets permission when the player is firing or in noisy melee,
  when the player is in immediate danger, and not when the player is sneaking away (detected by comparing the
  player's position with enemies' belief of it).
- Balance: with player-equal damage she was "a killing machine". They kept damage per shot equal ("if it takes the
  player three shots... it should take the buddy the same") and only slightly tuned rate/accuracy; instead, **hits
  the player does not see deal no damage**, with hits allowed to land when she has not been seen to hit anyone
  recently or the player is in immediate danger (charging enemy, low health, grapple). Players rarely noticed. A
  "furtive" idle animation padded the reduced fire rate.
- Stealth cheat: perfect buddy stealth worked 90-95% of the time; they made buddies invisible to enemies while the
  player is stealthy, because "if Ellie was seen even once and gave away the player's location, then the bond
  ... would become fractured".
- Callouts: model the player's view of the world and call out threats the player is not aware of; it is
  "absolutely imperative" that a called-out enemy is there for the player to see; calling glimpsed or re-hidden
  enemies "reflected badly on Ellie's intelligence" ("requirements for AI are actually more stringent than ... for
  human intelligence").
- Conclusion: the key "does not lie in the complexity of the systems ... but in the nuances of the character
  performance"; most time was iteration; debug and tuning tools essential; special abilities behind "really big
  timers"; "if they ruin what the player is trying to do just once, it's all in vain".
- Adult buddies (Tess, Bill) were meant to act more independently but "it always felt like they were abandoning the
  player", so they were given Ellie's logic with relaxed parameters.

**Halo (Damian Isla, Bungie)**
- Isla: "When the AI makes mistakes, it gives the player opportunities to feel smart"; good mistakes are
  readable (an enemy missing a melee and leaving a recovery window; misperception); bad mistakes are walking into
  walls or stacking on each other. Each AI keeps an internal, possibly wrong model of each target. — [Game Developer: Bungie on eight years of Halo AI](https://www.gamedeveloper.com/game-platforms/in-depth-bungie-on-eight-years-of-i-halo-i-ai)
- On allies: "The problem is not that mistakes are made, but there are no ways to correct it"; he wanted a simple
  symbolic feedback channel ("good job"/"bad job", "something like dog training") rather than explicit orders or
  natural language, and framed it as a design problem. Halo 3 put interaction knowledge in objects (which vehicle
  seat to take); a character whose vehicle tipped "remains in its vicinity fighting". — [same](https://www.gamedeveloper.com/game-platforms/in-depth-bungie-on-eight-years-of-i-halo-i-ai)
- At Develop 2008 Isla said Bungie's focus had moved to friendly AI, with player modelling the hard part; he called
  the regular marines the weakest AI. — [GameSpot: Develop 08](https://www.gamespot.com/articles/develop-08-bungie-ai-guru-lays-out-future-plans/1100-6195280/) (via search summary)

**Elizabeth, BioShock Infinite**
- John Abercrombie's GDC 2014 postmortem covers reconciling conflicting design goals for the companion by borrowing
  from other media, with before/after videos of failures. (Talk not watched.) — [GDC Vault](https://gdcvault.com/play/1020831/Bringing-BioShock-Infinite-s-Elizabeth)
- A Game Developer column argues companion AI should be *predictable* (players value matching expectations) while
  enemy AI should be somewhat unpredictable; the trick is "reliability without falling into dependency";
  companions that only support the player's damage are boring; companions that wait for the player to act first
  cannot convey autonomy; suggests stances (behaviour boundaries, e.g. "Expeditious" vs "Meticulous") and telling the
  companion what the player intends rather than issuing specific commands. — [Game Developer: Thinking about Elizabeth, part 2](https://www.gamedeveloper.com/design/thinking-about-elizabeth-part-2)

**F.E.A.R. (Jeff Orkin, GDC 2006)**
- Three-state FSM (Goto, Animate, UseSmartObject) with A* over actions (STRIPS-like planner); real-time planning
  freed time for squad behaviours such as suppression fire, advancing cover and search. Orkin: "Communication
  between enemies was a huge part of the AI"; soldiers bark instructions and intentions (e.g. calling for grenades to
  flush the player). — [Orkin paper PDF](https://pages.cs.wisc.edu/~dyer/cs540/handouts/gdc2006_orkin_jeff_fear.pdf), [GDC Vault](https://gdcvault.com/play/1013282/Three-States-and-a-Plan) (via search summary)

**Killzone**
- Killzone (2005, Beij & Straatman, GDC Europe): position evaluation that picks varied tactics from situation and
  terrain "anytime and anywhere", tactical pathfinding and suppression fire. — [Guerrilla gdce05 Killzone AI PDF](https://www.guerrilla-games.com/media/News/Files/gdce05_killzone_ai.pdf) (via search summary)
- Killzone 2: per-squad path analysis continuously updating the pathfinder with influences; squad HTN planning
  covered in a Utrecht thesis. A reviewer noted squadmates mostly stay out of the way but occasionally wander into the
  line of fire. — [Game Developer GDC 2010 AI summit recap](https://www.gamedeveloper.com/programming/gdc-2010-day-1-1-the-bottom-of-the-ai-summit), [Guerrilla VUA07 Verweij PDF](https://www.guerrilla-games.com/media/News/Files/VUA07_Verweij_Hierarchically-Layered-MP-Bot_System.pdf) (via search summary)

**Academic: complementary companions**
- Scott & Khosmood (Cal Poly, arXiv 1808.09079): a "complementary" action furthers the player's strategy rather than
  mimicking it. Guidelines: remove obstructions to the player's predicted next action; otherwise improve the game
  state; avoid actions that jeopardise the player's predicted next action; keep the companion's action distribution
  similar to the player's with limited experimentation. Study: 25 students, 20 min; 7/25 named the companion as a
  strength unprompted; some found it wasteful/unhelpful. — [arXiv 1808.09079](https://ar5iv.labs.arxiv.org/html/1808.09079)

### Inferences (mapped to IRONFEUD)
- **Line of fire / not in front:** Ellie's "not in front of the player" rating term generalises to "not inside the
  player's torso firing cone" (to the player's current target and along the reticle ray). This directly addresses the
  MW5 friendly-fire complaints. Because lancemates use the same `MechInput`, the cost belongs in their position and
  fire-permission scoring, not in a damage cheat. Also needed symmetrically: a lancemate must not fire if the player
  (or another friendly) is within its projectile/beam path or splash; missiles with arcs complicate this.
- **Kill stealing / overpowering:** Ellie's "damage only if the player sees it" is a hidden cheat that conflicts with
  IRONFEUD rule 2 (same physics, no shortcuts) and with headless auto-resolve calibration (results would differ
  between watched and headless battles). The rule-compatible equivalent is *behavioural*: lancemates prefer targets
  the player is not engaging unless ordered to focus, prefer section-breaking/support roles (spotting, marking,
  suppressing, flanking to expose rear armour) over finishing blows, and "assist" credit is shown in the HUD/debrief.
  This is a design decision for the user: pure fairness vs player-experience cheat.
- **Visible contribution:** Every useful act needs a visible or audible trace: barks on mark, scan, "rear armour
  exposed", "breaking left leg", HUD pips. The existing debug overlay (AiDebugDraw) is a starting point for a
  player-facing, reduced "intent" layer.
- **Callouts only for verified, visible threats:** IRONFEUD already decides on scans; callouts should fire only on
  contacts the player could confirm (in sensor range or LoS shortly), never on stale or one-frame scans.
- **Long timers for "heroic" assists** (e.g. body-blocking for a cored lancemate, coolant/support calls) keep them
  memorable; constant help becomes background noise.
- **Proximity vs independence:** TLOU found independent buddies felt like abandonment. For a lance, the analogue is a
  leash/formation radius relative to the player that only role-based orders (scout, flank) extend, and a lancemate
  sent away should report in.
- **Smart mistakes:** Isla's readable-mistake rule aligns with design goal 3 (fallible, readable enemies). For
  allies, mistakes must be explainable (a bark saying why: "no shot", "overheating", "leg's gone") and
  correctable (an order or a binary feedback key).

### Gaps
- The Elizabeth GDC talk content and AI and Games episodes were not watched; no transcript retrieved.
- No source found specifically on friendly-fire avoidance algorithms (LoF checks against allies) in shipped buddy AI,
  beyond the TLOU positioning term and the Killzone 2 reviewer remark.
- Halo marine/ally design specifics (e.g. Halo 3 ally dialogue system) not retrieved in detail.
- Game AI Pro chapters on squad coordination (beyond Ellie) were not read in this pass; gameaipro.com hosts them free.

## 4. Player trust: useful squad vs liability; delegation vs micromanagement; intent orders vs explicit

### Takeaway
Trust breaks on a single visible betrayal of the player's plan (breaking stealth, blocking a shot, refusing a
command without explanation); it builds on consistency, visible help at the right moment, and accurate
information. Intent-level control (stances, doctrine, gambit-like priorities) lets the player delegate; explicit
orders should be few, always available and always override autonomy.

### Cited Findings
- TLOU: one stealth-breaking incident "fractures" the bond; they accepted a realism cheat to prevent it. Players
  "feel thankful" when a buddy rescues them from a tight spot; a rescue from a grapple once in the game is remembered
  fondly. — [Dyckhoff, Game AI Pro 2](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter35_Ellie_Buddy_AI_in_The_Last_of_Us.pdf)
- TLOU: bad callouts (enemies the player cannot see) made Ellie look unintelligent; good callouts made her an
  information source, which "really improved the bond" given the minimal HUD. — [same](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter35_Ellie_Buddy_AI_in_The_Last_of_Us.pdf)
- Companion should be predictable and reliable without creating dependency; stances/intent rather than specific
  commands. — [Game Developer: Thinking about Elizabeth, part 2](https://www.gamedeveloper.com/design/thinking-about-elizabeth-part-2)
- Isla: the lack of a way to correct AI mistakes is the real problem; proposed binary feedback. — [Game Developer: Bungie Halo AI](https://www.gamedeveloper.com/game-platforms/in-depth-bungie-on-eight-years-of-i-halo-i-ai)
- FFXII: direct commands override gambits; gambits are ordered priority rules the player authors. — [Wikipedia: FFXII](https://en.wikipedia.org/wiki/Final_Fantasy_XII) (via search summary)
- MW5 Clans: AI seen as ineffective without explicit orders, which pushes players into micromanagement. — [Steam MW5 Clans discussion](https://steamcommunity.com/app/2000890/discussions/0/4691153988128083747) (via search summary; anecdotal)
- Breakpoint players wanted individual control after team-only orders shipped (later added). — [mp1st TU 4.0.0](https://mp1st.com/title-updates-and-patches/ghost-recon-breakpoint-update-1-15-may-25-shoots-out-for-tu-4-0-0) (via search summary)
- Complementary companions that ignore what the player is doing or block the player's next action are perceived as
  hurting the player's strategy. — [Scott & Khosmood](https://ar5iv.labs.arxiv.org/html/1808.09079)

### Inferences
- **Layered control model for IRONFEUD:**
  1. *Doctrine/stance per lancemate or lance* (set in the hangar or mid-fight): aggression, engagement range, heat
     discipline (allow overheating?), "protect the commander" vs "hunt", target priority (lights / damaged /
     whoever hits me). This maps onto existing utility weights and `Personality`; it is gambits without the
     programming. Keep it to a few discrete settings so the debrief can attribute outcomes.
  2. *Intent orders* (few, always available): focus my target, form on me / escort, hold here, flank (left/right of
     my target), spot/scan, fall back. Tap = selected/nearest, hold = whole lance; for a company, number keys select
     a lance first.
  3. *Optional tactical overlay* (sensor-limited, slow-mo or pause as an option).
  Orders override doctrine but decay or complete with an acknowledgement bark, so the AI is not stuck on a stale
  order; refusals explained.
- **Trust killers specific to mechs:** blocking the player's firing line, shooting through the player, crowding in
  narrow terrain (body collision), wandering out of the lance and dying alone, running hot into a shutdown in front
  of the enemy, silent order refusal. Each one is measurable in headless runs (friendly-fire damage, LoF-blocked
  ticks, distance-to-leader distribution, shutdowns per game, order-completion rate) and can become a regression
  test in the "measure before fixing, then pin it" style.
- **Avoid making orders a farm:** BiA's suppress-then-flank being the answer to every fight is the same anti-goal
  as a dominant lance composition. Enemy lances using the same command layer (their AI leader issues the same
  intents) keeps symmetry and lets the headless matrix test whether any single order pattern wins every mission.
- **Company scale:** the player cannot micromanage 12 mechs while piloting; the design should assume delegation to AI
  lance leaders (pilot skill: leadership), with the player giving lance-level intents. Auto-resolve calibration then
  covers AI-led lances directly.

### Gaps
- No quantitative study found on how often players issue orders, or on trust measured in playtests for squad
  shooters (only the n=25 companion study).
- No source found on how MechWarrior titles (MW4, MW5) handle lancemate order refusal, acknowledgement barks or
  friendly-fire mitigation; the earlier research's MW5 complaints remain the only data.
- Republic Commando/SWAT 4 developer intent behind context commands is unsourced (reviews only).
