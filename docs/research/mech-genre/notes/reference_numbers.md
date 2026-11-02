# Reference numbers and formulas from existing mech games (for tuning IRONFEUD)

These are reference values for calibrating an original game. Do not copy them verbatim. Names of other games' weapons and mechs below identify the data rows only. Labels used throughout: **[official]** means developer patch notes or the official wiki. **[datamined]** means a community extraction of game files. **[community]** means a fan wiki or forum post. **[derived]** means I computed it from the cited table. Fetched 2026-11-02.

Source health: the Smurfy MWO API (`mwo.smurfy-net.de/api/data/*.json`) failed with a self-signed TLS certificate. The official MWO wiki (`wiki.mwomercs.com`) now redirects to mwomercs.com/game, so I read it through archive.org snapshots. The battletech.fandom.com and mw5mercs.fandom.com wikis refused reading in the browser. The WebSearch budget ran out partway through, so some gaps below are search gaps, not proof that no data exists.

---

## MechWarrior Online: heat (capacity, sinks, dissipation, shutdown/override, ghost heat, weapons, maps)

### Takeaway
MWO uses a base capacity of 30. The first 10 heat sinks add 20 more (50 total). Each sink adds dissipation: single 0.14/s, double 0.22/s, the same in the engine or outside it. Shutdown is at 100%, and overriding costs internal damage every second you stay above 100%. Alpha strikes are limited by "ghost heat": per weapon group, each weapon beyond a limit adds base heat × a weapon multiplier × an escalating HeatScale factor (0.08 → 5.0). Map ambient heat shifts dissipation by -0.15 to +0.15.

### Cited Findings
**Heat capacity and sinks** (official MWO wiki, archive snapshot 2022-11-19) — [MWO Wiki: Heat (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Heat)
| Item | Value |
|---|---|
| Base heat capacity | 30 |
| Minimum heat sinks | 10 (the first 10 give "a base 20 heat capacity, no matter where equipped") |
| Single HS (SHS) | +0.14 heat/s dissipation, +0.75 capacity; 1 t, 1 slot. Same internal/external and IS/Clan |
| Double HS (DHS) | +0.22 heat/s, +0.5 capacity; 1 t, 3 slots (IS) / 2 slots (Clan). Same internal/external |
| Engine internal sinks | 1 per 25 engine rating, up to 10 at rating 250; above 250 the engine gains HS *slots*, and sinks in them count as external |
| Overheat | >100% capacity forces a shutdown until the excess heat dissipates. Exceeding capacity "by a large amount" deals incremental internal damage to the centre torso, ignoring armour |
| Override | Prevents the auto-shutdown; "severe internal damage every second" while above 100%, applied to a random section except the cockpit |
| Movement heat | 66% throttle generates heat equal to 1 SHS; 100% throttle equals 2 SHS (forward or reverse, scaled by fraction of top speed) |
| Lava (Terra Therma) | Raises heat to at most 90% (cannot cause a shutdown) plus leg damage |
| Water | Sinks in a submerged section gain cooling proportional to submersion (75% submerged = +75% for those sinks) |
| Stealth armour | Stops all dissipation while active |
| Jump jets | Add heat at a constant rate while firing |
| Flamer | Adds heat to the target "for each second they are hitting" (no number given) |
| Coolshot | Consumable that "quickly dissipate[s] a large amount of heat" (no number given) |
| Quirk "External Heat Transfer" | Reduces map heat effects; 100% on one chassis, 25% on one hero |

**Map dissipation modifiers** (same source): HPG Manifold -0.15, Frozen City -0.13, Boreal Vault -0.13, Polar Highlands -0.11, Alpine Peaks -0.10, Boreal Reach -0.10, Grim Plexus -0.08, Ishiyama Caves -0.08, Grim Portico -0.07, Mining Collective -0.02, Crimson Strait -0.01, River City 0, Steiner Colosseum 0, Emerald Taiga +0.01, Hellebore Springs +0.02, Forest Colony +0.03, Viridian Bog +0.04, Canyon Network +0.05, Mech Factory +0.08, Sulfurous Rift +0.09, Liao Jungle +0.10, Rubellite Oasis +0.11, Caustic Valley +0.12, Tourmaline Desert +0.13, Vitric Forge +0.14, Terra Therma +0.15. Some maps add regional modifiers, for example a volcano caldera. — [MWO Wiki: Heat (archive)](https://web.archive.org/web/20221119014212/https://wiki.mwomercs.com/index.php?title=Heat)

**Ghost heat, original 2013 design** (official forum post; feature live 16 July 2013, beta). Table of "Max Alpha", the number of a weapon you can fire without penalty: PPC 2, ERPPC 2, LRM-15 2, Large Laser 2, Medium Laser 6, AC/20 1, SRM-2 4, SSRM-2 4, SRM-4 4, SRM-6 3. Firing more than Max Alpha "makes heat generation grow exponentially". The same post lowered the threshold for overheat damage from 120% heat to 100%; the damage depends on how long you stay overheated. — [MWO forum: Heat Scale and General Update (2013)](https://mwomercs.com/forums/topic/126651-heat-scale-and-general-update/)

**Ghost heat today: cumulative heat for firing N weapons together** [datamined; MechDB v3.0.2b, ©2026] — [nav-alpha MechDB ghost heat tables](https://mwo.nav-alpha.com/equipment/ghostheat). Selected rows (total heat for 1…12 weapons):
| Weapon | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 12 |
|---|---|---|---|---|---|---|---|---|---|
| Medium Laser (group limit 6) | 3 | 6 | 9 | 12 | 15 | 18 | 23.4 | 29.7 | 76.2 |
| PPC (limit 3) | 7 | 14 | 21 | 42.7 | 71.75 | 108.15 | 154.35 | 215.25 | 806.75 |
| ER PPC (limit 2) | 12 | 24 | 51.12 | 88.32 | 138.12 | 200.52 | | | 1398 |
| Large Laser (limit 4) | 5.9 | 11.8 | 17.7 | 23.6 | 40.12 | 60.18 | | | 411.8 |
| ER Large Laser (limit 3) | 7 | 14 | 21 | 38.5 | 61.25 | | | | 600.25 |
| AC/20 (limit 1) | 5 | 19.6 | 46.2 | 87.2 | 146.2 | | | | 1861.2 |
| AC/10 (limit 3) | 2 | 4 | 6 | 17 | 32.5 | | | | 466.5 |
| AC/5 (limit 5) | 1 | 2 | 3 | 4 | 5 | 6.48 | | | 23.2 |
| Gauss Rifle (limit 6) | 1 | 2 | 3 | 4 | 5 | 6 | 13.8 | | 125.9 |
| SRM 6 (limit 4) | 3.4 | 6.8 | 10.2 | 13.6 | 20.98 | | | | 168.54 |
| LRM 20 (limit 2) | 5.4 | 10.8 | 19.5 | 30.41 | | | | | 338.91 |
| Flamer (limit 4) | 1 | 2 | 3 | 4 | 9.05 | | | | 142.05 |
| AC/2, small lasers, LB-X ACs, MGs | linear: no ghost heat | | | | | | | | |
Weapons share ghost-heat *groups*: for example ML/ERML/MPL form one group, and PPC/Gauss/HGR/ERPPC another. Other per-weapon limits read off the tables: UAC/5 4, UAC/10 2, UAC/20 1, HGR 2, LPL 3, LRM10 4, LRM5 6, SRM4 5, SSRM6 3, MRM40 1. The fan wiki says the system targets more than 2 PPC/ERPPC/LL/ERLL, 1 AC20, 3 SRMs or 6 ML — [MWO fandom: Heat Scaling](https://mwo.fandom.com/wiki/Heat_Scaling)

**Per-weapon table** [datamined; MechDB v3.0.2b] — [nav-alpha MechDB weapons](https://mwo.nav-alpha.com/equipment/weapons). Damage per shot (lasers: over the beam duration), heat per shot, cooldown s, beam duration s, optimal / max range m, projectile speed m/s, tons.
| Weapon | Dmg | Heat | CD s | Dur s | Opt/Max m | Vel m/s | DPS | Tons |
|---|---|---|---|---|---|---|---|---|
| Small Laser | 3.75 | 1.1 | 2.25 | 0.75 | 160/320 | – | 1.25 | 0.5 |
| Medium Laser | 5 | 3 | 3 | 0.9 | 270/540 | – | 1.28 | 1 |
| ER Medium Laser | 5 | 3.8 | 3.6 | 0.9 | 360/720 | – | 1.11 | 1 |
| Medium Pulse Laser | 6 | 3.4 | 2.8 | 0.6 | 240/480 | – | 1.76 | 2 |
| Large Laser | 9 | 5.9 | 3.5 | 1.0 | 480/960 | – | 2.0 | 5 |
| ER Large Laser | 9 | 7 | 4 | 1.0 | 700/1400 | – | 1.8 | 5 |
| Large Pulse Laser | 11 | 7 | 3 | 0.75 | 400/800 | – | 2.93 | 7 |
| PPC | 10 | 7 | 4 | – | 540/1080 | 1400 | 2.5 | 7 |
| ER PPC | 10 | 12 | 4 | – | 810/1620 | 2400 | 2.5 | 7 |
| Heavy PPC | 15 | 13 | 5 | – | 540/1080 | 1400 | 3.0 | 10 |
| AC/2 | 2 | 0.5 | 0.7 | – | 810/1620 | 2200 | 2.86 | 6 |
| AC/5 | 5 | 1 | 1.5 | – | 620/1240 | 1700 | 3.33 | 8 |
| AC/10 | 10 | 2 | 2 | – | 540/1080 | 1500 | 5.0 | 12 |
| AC/20 | 20 | 5 | 3.75 | – | 300/600 | 1100 | 5.33 | 14 |
| Ultra AC/5 | 5 | 1.5 | 1.5 | – | 600/1200 | 1500 | 4.37 | 9 |
| Gauss Rifle | 15 | 1 | 4 | – | 810/2050 | 2200 | 3.75 | 15 |
| Heavy Gauss | 25 | 2 | 4.5 | – | 250/1250 | 1800 | 5.56 | 18 |
| Machine Gun | 0.1/bullet | 0 | – | – | 130/260 | 100 | 1.0 | 0.5 |
| Heavy MG | 0.15 | 0 | – | – | 130/260 | 100 | 1.5 | 1 |
| Flamer | 0.1 | 1 (HPS 1.0) | – | ∞ | 90/90 | 100 | 0.1 | 1 |
| LRM 5/10/15/20 | 5/10/15/20 | 2.2/3.6/4.5/5.4 | 3.25/3.7/4.0/4.3 | – | 900 | 175 | 1.54/2.7/3.75/4.65 | 2/5/7/10 |
| SRM 2/4/6 | 4.3/8.6/12.9 | 1.45/2.7/3.4 | 2/3/3.75 | – | 270 | 400 | 2.15/2.87/3.44 | 1/2/3 |
| Streak SRM 2/4/6 | 4/8/12 | 1.4/2.8/3.5 | 1.75/2.85/3.75 | – | 270 | 280 | 2.29/2.81/3.2 | 1.5/3/4.5 |
| MRM 10/20/30/40 | 10/20/30/40 | 3.5/5.5/8/10 | 3.9/4.1/4.3/4.5 | – | 550 | 800 | 2.56–8.89 | 3/7/10/12 |
| AMS | 0.13 | 0 | – | – | 190/275 | – | – | 0.5 |
Missile rows also list a missile "health" against AMS: LRM 0.8–1.2, SRM 1.0–1.4, Streak 1.3–1.7, MRM 0.4–0.7.

**Recent official balance deltas** (patch 1.4.247.0, 19 Oct 2021): ATM3 cooldown 5→3 s; ATM6 5→4 s; MRM10 4.3→3.9 s; MRM20 4.3→4.1 s. ECM enemy-radar-range reduction reverted to 30% (skill nodes 22.5%). Quirk passes add "-20% overheat damage" (Jenner) and "-40% overheat damage" (WHM-6D), which shows that overheat damage is a scalable stat. — [MWO patch notes 1.4.247.0](https://mwomercs.com/forums/topic/281552-patch-notes-142470-19-october-2021)

### Inferences
- **[derived] Ghost-heat formula.** The penalty heat added by the n-th weapon of a group fired together is `baseHeat × P × HeatScale[n]`, applied only for n above the group limit. HeatScale[n] for n = 2…12 is **0.08, 0.18, 0.30, 0.45, 0.60, 0.80, 1.10, 1.50, 2.00, 3.00, 5.00**. The multiplier P comes out as ML 1.0, LL 4.0, ERLL 5.0, PPC 7.0 and AC/20 24.0. Checked against the table: the ML 7th–12th rows and PPC 4th–7th rows reproduce exactly. A 4-PPC alpha costs 42.7 heat, not 28 (+52%).
- **[derived] Normalised to IRONFEUD's 0..1 bar.** 10 DHS gives 2.2 heat/s against a capacity of 50, which is **0.044 of the bar per second**. 16 DHS gives 3.52/53 = **0.066/s**, and 20 DHS gives 4.4/55 = **0.08/s**. IRONFEUD's 0.055–0.075/s sits inside the range of typical MWO builds. With a ~50-heat bar, one shot costs: ML 3 heat = 0.06; LL 5.9 = 0.12; PPC 7 = 0.14; ERPPC 12 = 0.24; AC/20 5 = 0.10. Ballistics are cheap on heat and pay in tonnage and ammo.
- In MWO capacity barely grows with sinks (+0.5 per DHS) while dissipation grows linearly. **Sinks buy sustained fire, not alpha size.** That is a useful lever for IRONFEUD: separate "cap" stats from "dissipation" stats per chassis.
- MWO's flamer generates 1 heat/s for its user, and the official wiki gives no number for the heat it transfers to the target. IRONFEUD's 0.5 of the bar per burn is very strong next to MWO's self-heat cost (~2% of a 50 bar per second).
- Ambient modifiers of ±0.15 are a whole-map knob of roughly ±7–15% of typical dissipation (2.2–4.4/s), assuming they add directly to heat/s. Small enough to flavour a map without dominating it.

### Gaps
- The ghost-heat **time window** (commonly quoted by players as 0.5 s, with weapons fired >0.5 s apart not stacking) is **not verified** this pass; the archived official wiki just points to Smurfy. Treat 0.5 s as community lore.
- No numeric value found for override damage per second, flamer heat transferred to the target, or coolshot heat removed. The MWO wiki pages are qualitative, and the Smurfy data API was unreachable (TLS error).
- The weapon table carries no game patch number; MechDB is maintained (©2026, v3.0.2b) but does not state its data version.

---

## MechWarrior Online: mobility (accel/decel, turn, torso twist, arms, 2017 engine decoupling, jump jets)

### Takeaway
Since 2017 MWO agility (acceleration, deceleration, turn rate, torso and arm speeds) is a per-chassis stat, decoupled from engine rating. Official example values for a 45 t medium: turn 72.8, torso yaw 130.5, torso pitch 82, arm yaw/pitch 326 (degrees/s), acceleration 45.4, deceleration 52.8. I found no published per-tonnage formula.

### Cited Findings
- **Hotfix 14 Dec 2017, mobility values fixed for the HSN-7P** (a 45 t medium; the table is unlabelled but sits next to that fix), previous → new: Acceleration 30 → 45.39; Deceleration 41.11 → 52.78; Turn Rate 57.29 → 72.76; Torso Turn Speed Yaw 100 → 130.5; Torso Pitch 60 → 82; Arm Speed Yaw 250 → 326; Arm Speed Pitch 250 → 326. — [MWO news: Hot-Fix 14-DEC-2017](https://mwomercs.com/news/2017/12/1979-hotfix-scheduled-for-14dec2017) [official]
- **Griffin (55 t), patch 19 Oct 2021:** acceleration normalised to 47.71 and deceleration to 45.91 across all variants. **MASC Mk I:** fill rate 0.09 → 0.064 (longer burn); acceleration boost 2.0 → 1.8; deceleration boost 1.9 → 1.7. Some variants get quirks such as "+7.5% max speed", "+15% jump jet burn duration", "+45% jump jet initial thrust", "-25% fall damage", "15% hill climb". — [MWO patch notes 1.4.247.0](https://mwomercs.com/forums/topic/281552-patch-notes-142470-19-october-2021) [official]
- **2017 engine decoupling (PTS April 2017, with the skill tree):** players report agility no longer depends on the fitted engine, most agility quirks were removed, and the lost torso-twist *range* was the biggest change. Assaults with big engines lost their medium-like handling, which players saw as good for lights and mediums. — [MWO forum, 26 Apr 2017](https://mwomercs.com/forums/topic/249350-mobility-balance-change-buffed-clans-upper-tier-mechs) [community]
- **Jump-jet skill nodes (May 2017):** Heat Shielding (less JJ heat), Vent Calibration (longer burn), Vectoring (more forward thrust), Lift Speed (more initial boost). No numbers given. — [MWO Wiki: Jump Jets (archive)](https://web.archive.org/web/20221119014318/https://wiki.mwomercs.com/index.php?title=Jump_Jets) [official]
- MechDB exposes per-mech stats for acceleration, deceleration, reverse speed, torso pitch/yaw angle, torso pitch/yaw speed, turn rate, JJ burn time, JJ initial thrust and jump-jet heat; it is a source for further extraction. — [nav-alpha MechDB mechs](https://mwo.nav-alpha.com/mechs) [datamined]

### Inferences
- In the HSN-7P values, arm speed is exactly **2.5×** torso yaw speed (326/130.5), and torso pitch is **0.63×** yaw (82/130.5). MW5's data (next section) shows the same 2.5 and 0.625 ratios, so these look like shared design ratios across the franchise's engine.
- For a 45 t mech, turn rate (72.8°/s) is about 0.56× torso yaw (130.5°/s): the torso always outpaces the legs, so torso twist is the main aiming tool.

### Gaps
- No official per-tonnage or per-class formula for post-2017 MWO agility, torso twist **angles**, or jump-jet thrust/heat/burn time. Smurfy, which lists these per mech, was unreachable. The accel/decel units are not stated (likely km/h per second).

---

## MechWarrior 5: Mercenaries: heat, speed formula, armour/structure per tonnage, torso/arm rates (datamined)

### Takeaway
Datamined MW5 mech assets (341 variants) give exact formulas. Base heat capacity = 30 + 10 × cooling rate, where cooling rate = min(1, engine rating/250). Top speed = 450 cm/s × rating/tons, which is 16.2 km/h per tabletop walking MP. Reverse speed is ⅔ of top speed. Arm speed = 2.5 × torso yaw, torso pitch = 0.625 × yaw. Max armour = 2 × structure per location. Structure is 2× tabletop internal structure.

### Cited Findings
Source: MW5 mech data-asset JSON exports in [Career200/MW5-mechlab `src/data/MDA`](https://github.com/Career200/MW5-mechlab) (last commit 2026-07-16; MW5 game patch not stated) and [Career200/MW5-Mech-info](https://github.com/Career200/MW5-Mech-info) [datamined]. Example Atlas AS7-D (100 t, rating 300): `baseHeatCapacity` 40, `baseCoolingRate` 1, `topSpeed` 1350, `reverseTopSpeed` 899.1, `turnSpeed` 60.73, torso twist rate yaw 76.5 / pitch 47.81, torso bounds yaw ±90 / pitch ±26, arm twist rate 168.75 / 168.75, arm bounds yaw ±40 / pitch ±30, power-up 3 s, power-down 3 s, power initialisation 8 s.

Aggregated per tonnage [derived from the same files; median (min–max)]:
| Tons | n | Heat cap | Cool rate | Top cm/s | Turn °/s | Torso yaw °/s | Torso pitch °/s | Yaw bound ° | Arm °/s | Engine |
|---|---|---|---|---|---|---|---|---|---|---|
| 20 | 10 | 36.4 | 0.64 | 3600 (2700–4275) | 181 | 225 | 141 | 130 | 563 | 160 |
| 25 | 6 | 36 | 0.6 | 2700 | 158 | 198 | 124 | 90 | 495 | 150 |
| 30 | 12 | 37.2 | 0.72 | 2700 | 129 | 158 | 98 | 110 | 394 | 180 |
| 35 | 27 | 38.4 | 0.84 | 2700 | 118 | 149 | 93 | 110 | 371 | 210 |
| 40 | 15 | 40 | 1 | 3150 | 132 | 171 | 107 | 130 | 428 | 280 |
| 45 | 23 | 37.2 | 0.72 | 1800 | 83 | 104 | 65 | 110 | 259 | 180 |
| 50 | 30 | 40 | 1 | 2250 | 79 | 108 | 68 | 115 | 248 | 250 |
| 55 | 30 | 40 | 1 | 2250 | 97 | 122 | 76 | 110 | 287 | 275 |
| 60 | 25 | 40 | 1 | 2250 | 108 | 135 | 84 | 90 | 338 | 300 |
| 65 | 28 | 40 | 1 | 1800 | 75 | 95 | 53 | 100 | 214 | 260 |
| 70 | 28 | 40 | 1 | 1800 | 66 | 83 | 52 | 110 | 208 | 280 |
| 75 | 21 | 40 | 1 | 1800 | 61 | 90 | 56 | 90 | 191 | 300 |
| 80 | 29 | 40 | 1 | 1800 | 75 | 99 | 62 | 100 | 236 | 320 |
| 85 | 12 | 40 | 1 | 1800 | 65 | 86 | 53 | 62.5 | 203 | 340 |
| 90 | 19 | 40 | 1 | 1350 | 72 | 90 | 56 | 80 | 225 | 270 |
| 95 | 9 | 40 | 1 | 1800 | 72 | 90 | 56 | 90 | 225 | 380 |
| 100 | 17 | 40 | 1 | 1350 | 46 | 67.5 | 42 | 90 | 146 | 300 |
Torso pitch bounds sit around ±30° (range 15–45°). Arm yaw bounds are 0–45° (0 = fixed arms).

**Structure and max armour per tonnage** (sum over 8 locations; head is always 15 structure / 30 armour) [datamined, derived]: 20 t 75/150 · 25 t 95/190 · 30 t 111/222 · 35 t 125/250 · 40 t 143/286 · 45 t 159/318 · 50 t 175/350 · 55 t 191/382 · 60 t 207/414 · 65 t 217/434 · 70 t 223/446 · 75 t 237/474 · 80 t 253/506 · 85 t 269/538 · 90 t 285/570 · 95 t 299/598 · 100 t 313/626. Centre-torso structure runs from 12 (20 t) to 62 (100 t). — [Career200/MW5-Mech-info MechHealthStats.json](https://github.com/Career200/MW5-Mech-info)

### Inferences
- **[derived, exact over all 341 files]** `baseCoolingRate = min(1, engineRating/250)` and `baseHeatCapacity = 30 + 10 × baseCoolingRate`. That is MWO's rule (one internal sink per 25 rating, 10 max at 250) with each internal sink worth +1 capacity and +0.1 base cooling. Small-engine lights therefore run hotter **by design**: a 20 t mech with a 160 engine has 36.4 capacity and 0.64 cooling against 40 and 1.0 for an assault.
- **[derived, exact]** `topSpeed (cm/s) = 450 × engineRating / tons`; in km/h, `16.2 × rating/tons`. Reverse speed = 0.666 × top for every mech.
- **[derived]** Arm twist rate is 2.5 × torso yaw rate (median; range 1.98–2.51). Torso pitch rate is 0.625 × yaw (0.495–0.628). Turn rate is about 0.8 × torso yaw (0.67–0.93). Torso yaw falls roughly as **tons^-0.75**: 225 × (20/tons)^0.75 predicts 113°/s at 50 t (actual median 108), 83.5 at 75 t (actual 90) and 67.5 at 100 t (actual 67.5).
- Mapped to IRONFEUD's classes: light (20–35 t) torso about 150–225°/s, medium 100–170°/s, heavy 85–135°/s, assault 67–100°/s, so a light out-twists an assault by about 2.2–3.3×. Twist *bounds* also shrink for big mechs (85 t median ±62.5°, lights ±110–130°). Narrow twist bounds are a lever that gives lights flanking value.
- Max armour = exactly 2 × structure per location, and structure = 2 × tabletop internal structure (Atlas CT 62 against tabletop 31; Locust CT 12 against 6). Total protection grows nearly linearly with tonnage (≈3.1 structure/t), so durability per ton is flat. Assaults are not more armoured per ton; they are just larger.

### Gaps
- **MW5 weapon table** (damage/heat/cooldown/range), heat per *external* heat sink, the units of `baseCoolingRate` (heat/s presumed), and jump-jet values were not found. The datamine repos contain mech assets only; the MW5 fandom wiki refused access. Acceleration/deceleration are curve assets (`MechDefaultAcceleration`) with no values in the export.

---

## HBS BattleTech (2018): heat, stability/unsteady, melee, evasion, called shots

### Takeaway
HBS BattleTech scales tabletop heat about 3:1: standard sink 3/turn, double 6/turn. Weapons carry an "Instability" value: AC/20 and Gauss 40, PPC and AC/10 20, AC/5 10, AC/2 5, SRM 3 per missile, LRM 2 per missile, lasers 0. Pilot skills shift thresholds: Piloting raises the unsteady threshold to 60%, and Guts raises the overheat threshold by 15. Vanilla falls injure the pilot without structural damage. I could not verify vanilla max-stability per tonnage or the melee formulas.

### Cited Findings
**Weapon defs** [datamined: "STOCK" weapon JSONs kept, disabled with tonnage 999, under RogueTech's `NoUseVanillaOverrides`; probably vanilla numbers but unverified against an unmodded install] — [RogueTech repo, Core/RogueModuleTech/NoUseVanillaOverrides/Weapon](https://github.com/BattletechModders/RogueTech/tree/HEAD/Core/RogueModuleTech/NoUseVanillaOverrides)
| Weapon | Damage | Heat | Instability | Min / Max range m | Shots × projectiles | Accuracy mod | Recoil |
|---|---|---|---|---|---|---|---|
| Small / Medium / Large Laser | 20 / 25 / 40 | 6 / 12 / 18 | 0 | 0 / 90, 270, 450 | 1 | -1 | 0–1 |
| ER S / M / L Laser | 30 / 35 / 45 | 10 / 20 / 25 | 0 | 150 / 360 / 570 | 1 | -1 (crit ×1.25) | 1 |
| Pulse S / M / L | 35 / 40 / 55 | 12 / 20 / 30 | 0 | 90 / 270 / 450 | 1 | -2 | 1 |
| PPC / ER PPC | 50 / 60 | 35 / 45 | 20 | 90 min; 540 / 690 | 1 | 0 | 3 |
| AC/2 / 5 / 10 / 20 | 25 / 45 / 60 / 100 | 4 / 8 / 12 / 24 | 5 / 10 / 20 / 40 | min 120/90/0/0; max 720/540/450/270 | 1 | 0 (refire +1/+1/+2/+2) | 1/2/3/4 |
| Gauss | 75 | 5 | 40 | 180 / 660 | 1 | -1 | 4 |
| LRM 5/10/15/20 | 4 per missile | 6/10/14/18 | 2 per missile | 180 / 630 | 5/10/15/20 | 0 | 1 |
| SRM 2/4/6 | 8 per missile | 4/8/12 | 3 per missile | 0 / 270 | 2/4/6 | 0 | 1 |
| Machine gun | 3 × 5 shots | 0 | 0 | 0 / 90 | 5 | 0 (crit ×1.5) | 0 |
| Flamer | 5 (+10 heat to target) | 0 | 0 | 0 / 90 | 1 | 0 | 0 |
**Heat equipment** (same folder): standard heat sink `DissipationCapacity` 3; double heat sink 6. Heat banks: MaxHeat +10/+20/+30 with overheat threshold +5/+10/+15 (T1/T2/T3; values read from the files). Thermal exchangers multiply weapon heat by 0.9 / 0.85 / 0.8. Jump jets are split by chassis weight: 10–55 t, 60–85 t and 90–100 t versions, each `JumpCapacity` 1. — [RogueTech NoUseVanillaOverrides](https://github.com/BattletechModders/RogueTech/tree/HEAD/Core/RogueModuleTech/NoUseVanillaOverrides)
- **Pilot-skill effects** (official skirmish pilot cards, Paradox forum PDF, June 2019): Piloting "Unsteady threshold increased to 60%"; Guts "Overheat threshold increased by 15"; Guts "Ballistic recoil penalties reduced by 1"; Tactics "Called Shots are 2.4x as likely to hit the targeted location"; Gunnery +5% to +20% base hit; Piloting +5% to +20% base melee hit and "+20% Sprint distance". Abilities: Bulwark makes COVER and GUARDED 40% damage reduction each, up from the default 20%, and 60% together. Sure Footing gives +1 evasion charge (can exceed the cap) and ENTRENCHED (50% stability-damage reduction). Sensor Lock removes 2 evasive charges. Coolant Vent removes 50 extra heat this round, then adds 8 heat for 3 rounds, with a 4-round cooldown. Master Tactician gives +1 initiative and "remove one bar of stability damage when Reserving". — [Battletech_Pilots.pdf (Paradox)](https://forumcontent.paradoxplaza.com/public/474734/Battletech_Pilots.pdf)
- HBS heat is the tabletop scale "expanded … on a 3:1 basis", so 0–150 against tabletop 0–50. In vanilla, overheat dealt internal structure damage ("corroding" the mech from inside), and a fall only injured the pilot. The mod's skill-check maths uses `Skill / PilotingDivisor`, default divisor 40, so piloting 5 = +12.5%. — [CBTBehaviorsEnhanced README](https://github.com/BattletechModders/CBTBehaviorsEnhanced) [community/mod docs]
- Vanilla mechs **lose one evasion pip each time they are fired at**; a mod removes this per weight class to "make light mechs more balanced". — [PermanentEvasion README](https://github.com/BattletechModders/PermanentEvasion) [community]
- MechEngineer overrides `DefaultUnsteadyThreshold` to 40 and `InternalHeatSinkCount` to 0. These are mod values, not vanilla. — [MechEngineer CombatGameConstants.json](https://github.com/BattletechModders/MechEngineer/blob/HEAD/StreamingAssets/data/constants/CombatGameConstants.json)

### Inferences
- Instability tracks "big single hits" rather than damage. AC/20 deals 100 damage for 40 instability; a large laser deals 40 damage for 0. Stability is a separate axis that favours ballistic and missile boats. For IRONFEUD, a per-weapon "impact" stat independent of damage creates a real choice. One full LRM-20 volley carries 40 instability, as much as one AC/20 hit.
- Heat in HBS is per turn: sink 3/turn against ML 12 heat, so one ML equals 4 sinks of output. Per-shot heat relative to sustained dissipation is much harsher than in MWO, where an ML is 3 heat against 2.2/s for 10 DHS.
- Losing an evasion pip per attack means lights' defence erodes under focus fire. The mod community's main fix for weak lights was to remove that erosion, which argues for keeping a light's defensive bonus robust under multiple attackers.

### Gaps
- **Vanilla max stability per tonnage, unsteady/knockdown thresholds, stability recovery per turn, melee/DFA damage formulas and evasion pips per hex moved: not verified.** CombatGameConstants.json (vanilla) was not found in public repos (only partial mod overrides). The HBS game wiki (battletech.fandom.com) refused access, and the web-search budget ran out. The "unsteady threshold increased to 60%" card implies a lower default but does not state it. One Nexus forum post (via search) claimed 100 stability HP for a Locust vs 180 for an Atlas *under a mod*; not used.

---

## Armored Core VI: ACS (stagger), impact, direct-hit adjustment, leg types

### Takeaway
AC6 separates "Impact" (fills the ACS/stagger gauge) and "Accumulative Impact" (the part that stays and decays slowly) from "Attack Power". "Direct Hit Adjustment" (140–205% on the sampled weapons) multiplies damage on staggered targets. Leg types trade attitude stability (reverse joint 530 → tank 1630) against jump performance (reverse-joint jump distance 386, height 80, against biped 87/22 and tetrapod 82/15).

### Cited Findings
**Weapon stagger stats** [community wiki, pages last edited Aug–Oct 2023; patch 1.02 changed some weapons]:
| Weapon (class) | Attack power | Impact | Accum. impact | Direct-hit adj. % | Rate | Ideal / eff. range m | Source |
|---|---|---|---|---|---|---|---|
| RF-024 TURNER (rifle) | 105 | 65 | 25 | 185 | 3.4 /s | 160 / 296 | [Fextralife](https://armoredcore6.wiki.fextralife.com/RF-024+TURNER) |
| MG-014 LUDLOW (machine gun) | 42 | 41 | 19 | 195 | 10.0 /s | 115 / 236 | [Fextralife](https://armoredcore6.wiki.fextralife.com/MG-014+LUDLOW) |
| SG-027 ZIMMERMAN (shotgun) | 900 (top box: 800) | 840 (top box: 620) | 420 | 205 | reload 2.0 s | 102 / 184 | [Fextralife](https://armoredcore6.wiki.fextralife.com/SG-027+ZIMMERMAN) (the page conflicts with itself) |
| SONGBIRDS (back grenade) | 754×2 | 745×2 | 585×2 | 140 | reload 6.4 s | 625 eff. | [Fextralife](https://armoredcore6.wiki.fextralife.com/SONGBIRDS) |
Patch 1.02 (11 Sep 2023) example: MA-J-200 RANSETSU-RF Attack Power 194→224, reload 2→1.8 s. — [Fextralife Patch Notes](https://armoredcore6.wiki.fextralife.com/Patch+Notes)

**Leg types** [community wiki]:
| Leg part (type) | AP | Attitude stability | Load limit | Jump dist / height | Weight | Notes |
|---|---|---|---|---|---|---|
| KASUAR 42Z (reverse joint) | 3400 | 530 | 47 820 | 386 / 80 | 19 060 | — [Fextralife](https://armoredcore6.wiki.fextralife.com/KASUAR+42Z) |
| LG-011 MELANDER (biped) | 4150 | 743 | 60 520 | 87 / 22 | 18 700 | — [Fextralife](https://armoredcore6.wiki.fextralife.com/LG-011+MELANDER) |
| LG-033M VERRILL (tetrapod) | 6850 | 1413 | 76 200 | 82 / 15 | 36 200 | — [Fextralife](https://armoredcore6.wiki.fextralife.com/LG-033M+VERRILL) |
| LG-022T BORNEMISSZA (tank) | 10 040 | 1630 | 100 300 | n/a | 49 800 | Built-in drive: thrust 4667 (upward 3667), travel speed 150, high-speed perf. 376, QB thrust 22 150, QB jet duration 0.34 s, QB EN 810, QB reload 0.80 s — [Fextralife](https://armoredcore6.wiki.fextralife.com/LG-022T+BORNEMISSZA) |
Other legs, type not confirmed: VP-424 AS 1366 / AP 5600; EL-TL-11 FORTALEZA AS 872; NACHTREIHER 42E AS 711; RC-2000 SPRING CHICKEN AS 586. — [Fextralife Legs](https://armoredcore6.wiki.fextralife.com/Legs)
- Leg-type traits (qualitative): reverse joint has the lowest load limit and impact resistance and the best jumps. Tetrapod can hover, absorbs recoil while hovering, has a high load limit and is slow in the air. Tank has a high load limit, flat recoil reduction and drifts after a quick boost. — [Fextralife Legs](https://armoredcore6.wiki.fextralife.com/Legs)
- "Attitude Stability" is "the maximum load the ACS can support" (the sum over frame parts). "Attitude Recovery" improves with lower total weight and speeds recovery from residual impact. Total weight sets movement speed. — [Fextralife Stats](https://armoredcore6.wiki.fextralife.com/Stats)

### Inferences
- **[derived]** The stagger gauge is ~500–1600 points (attitude stability) and a shotgun blast adds ~620–840 impact, so one point-blank shotgun hit nearly fills a light frame's gauge. A rifle adds 65 per shot at 3.4/s (~220 impact/s), so it needs ~2–3 s of sustained hits. **Burst weapons stagger; sustained weapons keep the gauge filled.** The accumulative part (40–50% of impact for the shotgun and the grenade launcher, ~38–46% for rifle and MG) is what survives between bursts.
- Tank vs reverse-joint stability differs by ~3× (1630 / 530), comparable to MW5's 3.3× torso-yaw ratio between 20 t and 100 t. Both franchises make "heavy = steadier, light = more agile" a ~3× spread.
- For IRONFEUD: stagger maps onto per-class "stability" plus per-weapon "impact" plus a decaying residual, with a damage multiplier window while staggered. The direct-hit multiplier of 140–205% shows how strong a payoff AC6 makes the stagger window.

### Gaps
- **ACS recovery delay/rate, stagger window duration, the exact direct-hit maths, and ground speed / QB speed per leg type** (other than the tank's built-in drive values) were not found. Fextralife's mechanics pages are qualitative or missing (404), and community spreadsheets could not be located without search. Leg values are from Aug 2023 pages and may predate later patches.

---

## Tabletop BattleTech (Total Warfare): physical attacks, heat scale, shutdown/ammo rolls, movement heat

### Takeaway
Total Warfare damage by attacker tonnage t: punch ⌈t/10⌉, kick ⌊t/5⌋, DFA ⌈3t/10⌉ with the attacker taking ⌈t/5⌉, charge ⌈t/10 × (hexes−1)⌉ with the attacker taking ⌈target t/10⌉. Heat: −1 MP per 5 heat, to-hit +1/+2/+3/+4 at 8/13/17/24, shutdown rolls from 14 heat (target 4+, +2 every 4 heat) and automatic at 30, ammo-explosion rolls at 19/23/28. Movement heat: walk 1, run 2, jump max(3, MP).

### Cited Findings
Source: MegaMek, the open-source implementation of Total Warfare rules (main branch, fetched 2026-11-02) [rules implementation, not the rulebook text]:
- **Punch** = ⌈t/10⌉ (claws ⌈t/7⌉); damage halves per missing upper/lower arm actuator; 0 without a shoulder. — [PunchAttackAction.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/actions/PunchAttackAction.java)
- **Kick** = ⌊t/5⌋; halves per missing upper/lower leg actuator; 0 without a hip. — [KickAttackAction.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/actions/KickAttackAction.java)
- **Death from above** = ⌈(t/10) × 3⌉ to the target (talons ×1.5); the attacker takes ⌈t/5⌉. — [DfaAttackAction.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/actions/DfaAttackAction.java)
- **Charge (TW)** = ⌈(t/10) × (hexesMoved − 1)⌉ (×0.5 if the attacker is in water); the attacker takes ⌈targetTons/10⌉. Optional TacOps: ⌊(tT·tA·hexes/(tT+tA))/10⌋ + MoS. BattleTech Core rules variant: ⌈t/5 × (target-movement-modifier + 1)⌉. — [TWRulesPhysical.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/rules/totalwarfare/TWRulesPhysical.java), [CoreRulesPhysical.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/rules/core/CoreRulesPhysical.java)
- **Heat scale effects** (per heat level): −1 MP at 5–9, −2 at 10–14, −3 at 15–19, −4 at 20–24, −5 at 25–29. To-hit +1 from 8, +2 from 13, +3 from 17, +4 from 24. Shutdown-avoid rolls 4+ at 14, 6+ at 18, 8+ at 22, 10+ at 26; automatic shutdown at 30. Ammo-explosion-avoid rolls 4+ at 19, 6+ at 23, 8+ at 28. — [MegaMek messages.properties (HeatEffects.0–30)](https://github.com/MegaMek/megamek/blob/main/megamek/resources/megamek/client/messages.properties)
- **Shutdown target number** = 4 + 2 × ⌊(heat − 14)/4⌋ on 2d6. The optional "Avoiding Shutdown" rule (TO:AR p.102) adds −5 plus a piloting modifier (skill ≤1: −2, ≤3: −1, ≤5: 0, else +1). The restart roll uses the same target number. — [HeatShutdownTargets.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/rules/HeatShutdownTargets.java)
- With extended heat (to 50): ammo-explosion avoid +2 more at 35/40/45; pilot life-support damage 1/2/3/4/5 at heat 15/25/32/39/47 (torso-mounted cockpit; 32+ needs the extended-heat option). — [HeatResolver.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/server/totalWarfare/HeatResolver.java), [TWRulesHeat.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/rules/totalwarfare/TWRulesHeat.java)
- **Movement heat:** walk 1, run 2 (standard/XL engines; XXL 4/6); jump max(3, MP jumped) (XXL max(6, 2×MP)). — [Engine.java](https://github.com/MegaMek/megamek/blob/main/megamek/src/megamek/common/equipment/Engine.java)

### Inferences
- **[derived] Per class:** a 20 t mech punches for 2, kicks for 4, DFAs for 6. A 100 t mech punches for 10, kicks for 20, DFAs for 30. Melee scales linearly with tonnage, which strongly favours assaults; a light's only melee play is DFA (it needs jump jets). For IRONFEUD, any melee or ramming should scale sublinearly, or give lights a dodge/initiative edge, to avoid "bigger is better".
- **[derived]** Tabletop heat is a graded scale, not a cliff. Movement penalties start at 17% of the auto-shutdown level (5/30), to-hit penalties at 27% (8/30), shutdown risk at 47% (14/30), ammo risk at 63% (19/30). IRONFEUD's 1.0 gun lock / 1.2 shutdown is a cliff. Adding soft penalties from ~0.5–0.7 of the bar (slower legs, aim sway) would give the tabletop-style "running hot" gradient before the lock.
- A 10-sink tabletop mech dissipates 10/turn. Walking costs 1 (10%), running 2 (20%), jumping ≥3 (30%+), so movement eats a modest, readable share of cooling. MWO's full-throttle cost (2 SHS-equivalent ≈ 0.28/s against 1.4–2.2/s for 10 sinks, i.e. 13–20%) is about the same proportion.

### Gaps
- I read rule implementations (MegaMek), not the Total Warfare rulebook text. Page numbers are only as cited in MegaMek comments (TW p.148, p.158, p.160; TO:AR p.102). Piloting-skill fall checks (the 20-damage-in-a-phase check, kick/charge/DFA checks and their modifiers) were not extracted this pass.
