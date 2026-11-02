# Sound design for giant machines: footfalls, servos, cockpit audio, weapons, warning voice

Scope note: researched 2026-11-02 for IRONFEUD (Godot 4 .NET, synthesised placeholder audio today: `SynthKit` one-shots,
`ServoSynth` joint-velocity whine, per-class footfalls, speed-of-sound delay already in `AudioDirector`, a hard limiter
on Master, 48 pooled `AudioStreamPlayer3D` voices with `MaxDistance = 900`). Primary developer sources on giant-machine
audio are thin on the open web: many "interviews" are video-only (SoundWorks Collection, A Sound Effect video
tutorials) or aggregator summaries. Where only a secondary source exists, that is said. Nothing below is a franchise
voice line or asset; all of it is technique.

## 1. How shipped games and films make huge machines sound heavy (footstep layers, distance, occlusion)

### Takeaway
The recurring recipe is: real, large, physically recorded impacts (metal and industrial sources, not library "robot"
sounds) for the body of the sound; a synthesised sine/sub layer for weight; a low-mid "chest" thump; a texture/debris
layer that is the main carrier of scale; and a long tail that fills the long gap between giant steps. Character comes
from a concept ("militarised farm machinery", "animal + high tech") applied consistently per faction/class, with
light/agile machines given higher, quicker, "whistly" movement and heavies given guttural, growly servos. Mixing and
occlusion (attenuation plus filtering) matter as much as the assets for readability.

### Cited Findings
**Titanfall / Titanfall 2 (Respawn)**
- Brad Snyder's Atlas titan "set the template for how all Titans should sound". His concept: frontier people as farmers,
  "What if this was farm technology that they had militarized?"; he blended familiar machine sounds such as pallet jacks
  with energy-based elements — [Post Magazine, Audio For Games (2014)](https://www.postmagazine.com/Publications/Post-Magazine/2014/March-1-2014/Audio-For-Games.aspx)
- Per-class character in Titanfall 1: the Ogre (heaviest) "sounds heavy and lumbering", Tyler Parsons gave it a guttural,
  bestial quality with "growly servos", a "tight chassis sound" when walking and a snarling roar on dash, "meant to sound
  as intimidating as possible"; the agile Stryder was designed by Erik Kraber to sound "cricket-like", its dash "more
  nimble and less lumbering" with a "'wheeeee' sound" built from a descending pitch, whistles and pitch-shifted hydraulic
  elements — [Post Magazine](https://www.postmagazine.com/Publications/Post-Magazine/2014/March-1-2014/Audio-For-Games.aspx)
- Kraber's team wanted no "pew pew" laser sounds, and every weapon needed a distinct identity so players could identify
  its threat level from sound — [Post Magazine](https://www.postmagazine.com/Publications/Post-Magazine/2014/March-1-2014/Audio-For-Games.aspx)
- Titanfall 2: Rick Hernandez (designed most Titans) recorded his own metal layers rather than library sounds: hitting,
  scraping and hammering sheet metal, anvils, bolts, thin and thick metal — [A Sound Effect, Titanfall 2 sound](https://www.asoundeffect.com/titanfall-2-sound/)
- Tyler Parsons used contact-mic recordings of factory machinery to add "realism and weight to certain robotic or
  big-mechanical sounds"; the contact tracks gave "an interior-esque heaviness that could be used to suggest chassis
  resonance", used for example on Reaper footsteps — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Servos: Hernandez drove a real hobby servo from an Arduino, automated it from Ableton, recorded many takes, panned takes
  hard left/right for width, then pitched them down and processed them — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Layers were fused with morphing tools (SoundMorph TimeFlux, Wave Warper) or stacked conventionally; robot "vocals" were
  made by convolving drones, scrapes and metal hits with teletype and radio dialogue (Prosoniq/Zynaptiq Morph); Reapers
  got "animal and massively distorted sine wave elements" — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Parsons: Titans should be "massive, imposing, sometimes threatening", and robot sounds must feel believable though their
  sources don't exist — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Occlusion "consisting of attenuation plus filtering was a huge help in improving situational awareness" — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- A review of a Titanfall 2 pre-alpha build said the titan sound design "has a Transformers feel to it" (listener
  impression, not developer statement) — [The Sound Architect](https://www.thesoundarchitect.co.uk/titanfall-2-pre-alpha-tech-test-audio-review/)

**Pacific Rim (2013, supervising sound editor/sound designer Scott Martin Gershin)**
- Del Toro "didn't want the Jaegers and Kaijus to sound electronic or sci-fi; the technology isn't that far in the
  future" — [Broadcast Beat (via search summary; direct fetch returned the site homepage)](https://www.broadcastbeat.com/?p=94778)
- Gershin's team spent a day at the Port of Long Beach dropping cargo containers onto each other for metal footstep and
  punch sounds: "They were very big. They made a sound like cannons." — [Broadcast Beat (search summary)](https://www.broadcastbeat.com/?p=94778);
  corroborated by [HPA profile of Gershin](https://hpaonline.com/scott-gershin-takes-the-audience-on-a-sonic-journey/)
- A home-theatre review singled out "the Jaeger footsteps were tremendous" (opinion, no method) — [Sound & Vision](https://www.soundandvision.com/content/movie-premiere-pacific-rim)
- Correction to the brief: Erik Aadahl and Ethan Van der Ryn are credited on *Pacific Rim Uprising* (2018), not the
  2013 film — [Wikipedia, Erik Aadahl](https://en.wikipedia.org/wiki/Erik_Aadahl)

**Transformers / Godzilla (Aadahl & Van der Ryn)**
- Transformers: Revenge of the Fallen: the crew found the character of one new robot from the sound of two small magnets;
  the article gives no layering detail (the source is a SoundWorks video) — [/Film](https://www.slashfilm.com/507194/the-sound-design-of-transformers-revenge-of-the-fallen/)
- Godzilla (2014): they started from the contrabass-and-glove technique of the original roar and remixed it; they played
  material through very large speakers ("The sound that we were playing actually traveled over 3 miles") — [NPR/Public Radio East](https://www.publicradioeast.org/us/2014-05-18/whats-in-a-roar-crafting-godzillas-iconic-sound).
  No footstep specifics found.

**Horizon Zero Dawn (Guerrilla)**
- Pinar Temiz: "the core idea of giving them an animalistic and a high tech quality was born early on"; sources blended
  "electronic sounds, real-life animal sounds and various kinds of material, from synthetic to organic", and changed
  "based on the size of the machine, its function in the world... and its animation" — [PlayStation Blog](https://blog.playstation.com/2017/08/31/completing-the-experience-3d-audio-and-the-sound-of-horizon-zero-dawn/)
- Temiz used very little metal for regular machine movement; she "can't listen to metal impacts or scratches for too
  long" (listener-fatigue argument) — [PlayStation Blog](https://blog.playstation.com/2017/08/31/completing-the-experience-3d-audio-and-the-sound-of-horizon-zero-dawn/)
- Movement layers were designed to "stand out just enough for players to be able to recognize the type of the machine",
  signalling weight, size, whether it carries liquids, whether it has elemental weapons; recognisability plus "readable
  cues for gameplay-critical moments" — [PlayStation Blog](https://blog.playstation.com/2017/08/31/completing-the-experience-3d-audio-and-the-sound-of-horizon-zero-dawn/)
- GDC talk exists: "Giving a Voice to the Machines of 'Horizon Zero Dawn'" (variety vs scalable workflow); not read — [GDC Vault](https://gdcvault.com/play/1025143/Giving-a-Voice-to-the)

**MechWarrior 5 (PGI)**
- MW5 Clans audio lead Sean Kolton: footsteps and engines carry "a lot of energy in the low/low mid frequency range", as do
  ballistics, explosions and impacts, so managing the low end took "a ton of effort"; mechs fire several weapons at once
  ("multiply that by 3 or 4" for lancemates) and enemy fire must stay audible; the mix prioritises what gives "the player
  the most feedback in battle"; Wwise is the middleware — [MW5 Clans news, "Flash Storm Frequencies"](https://mw5mercs.com/news/2025/04/147-flash-storm-frequencies-audio-in-mechwarrior-5-clans)
- A Steam comment on an MW5 audio mod suggests reduced footstep sound "may be more realistic from inside a cockpit" (anecdotal;
  context for the earlier finding that MW footfalls were criticised) — [Steam workshop comments](https://steamcommunity.com/sharedfiles/filedetails/comments/3030426126)
- MWO: a developer Q&A states all audio was generated new rather than reused; no footstep design details found — [GameBanshee MWO Q&A](https://gamebanshee.com/kpy64)

**Generic giant-footstep recipe (tutorial source, dinosaurs)**
- Layer 1: a sub-bass onset synthesised from pure sine waves centred ~60-100 Hz ("will provide the strongest bass
  response"); layer 2: a car-door-slam-like low-mid thump at 100-200 Hz, "felt more in the chest"; layer 3 (called the most
  important): rock/debris crunch, or metal/industrial for city-destroying creatures; optional high end from subtle
  distortion rather than an HF boost; a long reverb and/or distortion tail, because bigger creatures have longer gaps
  between steps and the tail fills them; pitch-shift/EQ the bass to scale weight to size; offset layer start times
  slightly per trigger for variety — [Krotos, dinosaur footsteps](https://krotos.studio/blog/dinosaur-footsteps-sound-effects-made-easy)
- A Sound Effect's free mech workshop (Chase Steele) is structured as pneumatics, gun mech, servos, foot impact, big metal,
  motor, "vibe track", gun blast (video only; content not readable) — [A Sound Effect, how to design robot sound effects](https://asoundeffect.com/how-to-design-robot-sound-effects)

**Shadow of the Colossus / Armored Core VI**
- No developer source found on colossus footsteps; reviews praise "every earth-shaking stomp" as instilling "fearful
  respect" — [GameSkinny retro review](https://www.gameskinny.com/6rj8f/retro-review-shadow-of-the-colossus)
- Armored Core VI: FromSoftware described sound/game-design alignment as broad themes ("dark", "old-school sci-fi",
  loneliness and nostalgia); no SFX technique interview found — [Everyeye (relaying PC Gamer)](https://www.everyeye.it/notizie/armored-core-6-gioco-dark-sci-fi-ost-dettagli-fromsoftware-662182.html)

**Distance**
- Speed-of-sound delay: one Godot dev proposes delaying by distance/343; another warns it can look out of sync, best kept
  to big events paired with a visual cue — [Godot forum](https://forum.godotengine.org/t/how-do-i-make-every-single-node-simulate-the-speed-of-sound-with-delays/143645).
  Unreal ships a "Delay Based On Distance" source effect with a speed-of-sound parameter — [Unreal docs](https://dev.epicgames.com/documentation/unreal-engine/BlueprintAPI/Utilities/Struct/MakeSourceEffectSimpleDelaySetti-)
- Listeners praised Battlefield 3's distant-gunfire timing on large maps and noted echo changing near buildings (listener
  impressions) — [Designing Sound, BF3 vs MW3 gun sounds](https://designingsound.org/2011/11/14/battlefield-3-vs-modern-warfare-3-gun-sounds/)
- Arma players complained explosions were effectively inaudible at distance / fell off too quickly — [Bohemia feedback tracker](https://feedback.bistudio.com/T78801)
- Godot's `AudioStreamPlayer3D` has a built-in distance low-pass: `attenuation_filter_cutoff_hz` (default 5000 Hz;
  20500 disables) and `attenuation_filter_db` (default -24 dB); `attenuation_model` inverse / inverse-square / log /
  disabled; `unit_size` (default 10) sets how far a sound carries; `max_distance` always falls off linearly and saves CPU;
  `emission_angle_*` gives directional emitters; `doppler_tracking` can run on physics frames; `area_mask` routes
  sounds inside Area3Ds to other buses/reverbs — [Godot class reference, AudioStreamPlayer3D](https://docs.godotengine.org/en/stable/classes/class_audiostreamplayer3d.html)

### Inferences
- IRONFEUD's current footfall (sub drop + octave punch + noise burst + debris, no metal ring) matches layers 1, 2 and
  part of 3 of the giant-footstep recipe. What it lacks, compared with every shipped reference, is a *recorded* body
  layer: Titanfall used hand-recorded metal and contact-miked factory machinery, Pacific Rim dropped shipping containers.
  Pure synthesis gives weight but not "material"; this is the likely reason it still sounds "far from" the target. The
  Sonniss GDC archive (heavy metal impacts, industrial machinery, container/dumpster hits, demolition, rock debris) is
  the natural source for a recorded mid "chassis" layer and a debris layer, with the synthesised sub kept as the
  per-class scalable part.
- A complete mech footfall, as an inferred layer stack (each a separate variant pool, triggered together with small random
  offsets):
  1. **Sub** (synth, 30-100 Hz, length and pitch by class) — weight; felt, not heard.
  2. **Chest thump** (100-200 Hz, recorded or synth) — punch.
  3. **Chassis/metal stress** (recorded heavy metal hit, low-passed, contact-mic character; not a bright ring) — "it's a
     machine". HZD's warning about metal fatigue suggests keeping this short and dark.
  4. **Ground/debris** (switched by surface: dirt, rock, rubble, concrete, water) — scale and context; this is the
     layer that should change most with terrain, and the one that should *continue after* the impact (settling grit).
  5. **Hydraulic/servo settle** (short hiss or servo stop after the weight transfer, slightly later than the impact) —
     sells that the leg absorbed load; can be driven from the existing spring/secondary-motion compression on landing.
  6. **Tail** (environment-dependent, longer for heavier classes, fills the inter-step gap).
- Per-class identity can follow the Titanfall pattern: lights quick, higher, whistly/pneumatic, little sub; assaults
  guttural, slow, growly servos and long tails. This supports pillar 4 (each class distinct) audibly, not only visually.
- Distance: the project already delays by distance. The cited cautions suggest keeping the delay for big events (footfalls
  of distant heavies, cannon reports, explosions) and adding a distance-dependent low-pass per voice (Godot's attenuation
  filter), plus swapping to a "far" variant (no transient crack, more rumble) beyond a range, rather than only attenuating.

### Gaps
- No primary source found for the exact layer breakdown of a Titanfall titan footstep, a MechWarrior footstep, or a
  Pacific Rim Jaeger footstep beyond "containers". The SoundWorks Collection videos (Transformers, Godzilla) and the HZD
  GDC talk would likely have this but are video/paywalled.
- No developer source on Shadow of the Colossus or Armored Core VI SFX; nothing on kaiju games specifically.
- Battlefield vehicle-specific design (tanks' tracks, engine perspective) was not found in readable form.

## 2. Procedural / parametric audio for machines, and what is feasible in Godot

### Takeaway
Shipped engine/machine audio is mostly *parametric sample playback* (looped or granular recordings whose pitch, volume,
filter and blend are driven by game parameters such as RPM/load/speed), not pure synthesis. Pure procedural synthesis
(Farnell) is a strong method for understanding and for scalable layers, but the industry pattern is "recorded material,
procedurally steered". Godot supports the needed primitives (`AudioStreamGenerator` from C#, bus effects, per-player
filters), so IRONFEUD's servo whine can become a granular/looped hybrid driven by joint velocity and load.

### Cited Findings
- Audiokinetic's loop-based engine tutorial: simplest method is to control the pitch of a looping sound in real time from
  RPM; source loops are steady-RPM recordings, each "as static a pitch as possible"; RPM arrives as an RTPC and must be
  clamped game-side — [Audiokinetic, loop based car engine design with Wwise part 1](https://audiokinetic.com/loop-based-car-engine-design-with-wwise-part-1)
- Granular engines (Crankcase REV) use "granular synthesis techniques to eliminate looping" plus "very sophisticated signal
  processing"; SoundSeed Grain notes suggest binding grain *position* in a recorded sweep to RPM, with window width
  optionally following RPM — [Audiokinetic, Crankcase REV](https://www.audiokinetic.com/products/plug-ins/crankcase-rev/); [Audiokinetic, The making of SoundSeed Grain](https://blog.audiokinetic.com/the-making-of-soundseed-grain/)
- Krotos Igniter blends granular, loops and synthesis and can switch between loops and granular across RPM ranges — [Krotos, Igniter Live for Wwise](https://www.krotosaudio.com/?p=132321)
- Wreckfest example (via Audiokinetic search summary): RPM automates voice volume of blend-track layers — [Audiokinetic, Engine sound modeling (403 on direct fetch; search summary only)](https://www.audiokinetic.com/engine-sound-modeling-from-sampling-to-granular-synthesis-in-wwise)
- Titanfall 2 moved to Miles 10 for reverb, filters and runtime parameter control driven by game data such as speed,
  angle, distance and health — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Titanfall 2's servo source was a real hobby servo recorded in many takes, then pitched down — i.e. small real motor,
  scaled — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Wwise footstep practice: surface material is a Switch, defaulting to a fallback ("Concrete") on unknown materials; walk/run
  can be an RTPC-driven switch; only send the switch when the surface changes, not every tick — [Audiokinetic, Footsteps material management](https://www.audiokinetic.com/en/footsteps-material-management-using-wwise-unreal-engine-4-unity-3d)
- Farnell's *Designing Sound* (MIT Press) builds each sound from physics to model to Pure Data patch; its practicals
  include a "Motors" chapter (Ch. 44, Practical 21) and a four-cylinder car engine model, per third-party code listings — [sccode.org machine tag](https://sccode.org/tag/category/machine); book overview [Cycling '74 review](https://cycling74.com/articles/book-review-designing-sound/replies/1)
- Godot `AudioStreamGeneratorPlayback`: from C# the bulk `push_buffer()` is usually faster than per-frame `push_frame()`
  (the opposite in GDScript); `get_frames_available()` / `can_push_buffer()` manage fill; `get_skips()` counts underruns
  and is the metric for tuning lookahead — [Godot docs, AudioStreamGeneratorPlayback](https://github.com/godotengine/godot-docs/blob/master/classes/class_audiostreamgeneratorplayback.rst)

### Inferences
- Servo whine: a pure oscillator driven by joint speed tends to sound like a synth. The shipped pattern suggests recording
  (or picking from Sonniss) a few real motor/servo/hydraulic sweeps, then driving *grain position* or loop pitch from
  joint angular speed and *layer blend/filter* from load (e.g. leg IK effort or hip spring compression). Separate
  parameters: speed -> pitch, load -> low-mid strain layer and grit, direction reversals -> a short "clunk"/stop
  one-shot, sudden accel -> hydraulic hiss. This mirrors RPM/load in engine models.
- Keep the DSP in `IronFeud.Logic/Audio` as pure C# (grain scheduler, envelopes) and push blocks via `push_buffer` from the
  Godot side; monitor `get_skips()` in the debug overlay (consistent with the project's "debug visualisation is
  first-class" rule).
- Footstep surface handling can follow the Wwise switch pattern: the terrain/foothold code already knows the surface;
  choose the debris pool per footfall, with a default pool.
- Godot bus effects available for a mix (from the Godot class reference, not re-fetched this session): low-pass/high-pass
  filters, EQ, compressor with a sidechain bus, distortion, reverb, limiter. A sidechained compressor is enough for simple
  ducking; an HDR-style system (section 3) would need to be written in C# over logical loudness values.

### Gaps
- No readable source on granular *hydraulics/servo* specifically (only engines); the Farnell motor chapter content was not
  verified first-hand.
- No Godot-specific performance numbers for C# `AudioStreamGenerator` at many simultaneous voices.

## 3. Cockpit perspective, interior vs exterior mix, priority (HDR), alarms

### Takeaway
The best reference is Titanfall 2's treatment of BT's voice: the same line rendered through three synchronised
perspective layers (cockpit small speaker with tight reflections / exterior PA with bass, distortion and distance slap /
far radio), crossfaded by game state. For the overall mix, DICE's HDR audio treats authored loudness as priority, keeps a
moving window of what is audible and culls what falls below it, which suits a sandbox where you cannot predict what
matters. Battlefield 4 notably *excluded the player's own foley from HDR* so explosions never cull footsteps: a direct
lesson for a mech game where the player's own footfalls carry the sense of weight. Aviation alerting guidance gives the
rules for alarms: a priority hierarchy, no overlapping voice alerts of equal priority, repeat until resolved only for
critical states, and one-shot for informational ones.

### Cited Findings
- BT voice in Titanfall 2 (Joshua Nelson): two stages, a core voice character ("clear, large, friendly, and pleasant to
  listen to", "just enough grit") then environmental colouring. In cockpit: small-speaker sound with tight reverb as if
  reflecting off cockpit surfaces. Outside nearby: external PA with extra bass, more distortion and a distance-dependent
  slap delay. Farther: crossfades into a radio-style version from the pilot's helmet comms. Three WAV layers are triggered
  in sync per line; game conditions set each layer's volume and EQ in real time — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Titanfall 2's procedural mix ranks sounds by friend/foe, whether a weapon is aimed at the player, and player health, and
  emphasises important sounds via level, EQ and distance modelling — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- DICE HDR audio (Battlefield: Bad Company, Frostbite): loudness is measured at the listener and all sources are scaled
  accordingly, like HDR lighting; it works on logical loudness values without touching waveforms; it is a culling algorithm
  where priority is set by loudness ("a good approximation of importance, especially in FPS games"); the dynamic range of
  hearing is "some 130 dB"; "Play the right sounds, not all sounds"; duckers, snapshot mixers and scene defines don't
  solve an open destructible sandbox; HDR is "not a silver bullet" and first-person focus still needs special handling — [DICE slides, HDR audio / adaptive mixing in Frostbite](https://www.slideshare.net/slideshow/adaptive-mixing-in-frostbite/3128152)
- HDR in Wwise explained: "every sound is important, but not at the same time"; a sliding window adapts to what is
  playing; numerically close to a limiter on logical volumes, preserving relative levels; infinite ratio makes non-overlapping
  loud sounds end up at the same level (losing real dynamic range), so a finite ratio can be better; without envelope
  tracking a loud sound's decaying tail holds the window too long and causes an unnatural lull; editing an explosion's
  envelope up *before* its energy creates a brief silence before the blast; start from a traditional mix and push only a
  handful of sounds above threshold; prefer HDR over sidechaining when you can't know in advance what matters; bad
  attack/release causes pumping — [Designing Sound, Finding your way with HDR audio in Wwise](https://designingsound.org/2013/06/21/finding-your-way-with-high-dynamic-range-audio-in-wwise/)
- A Battlefield 6 player complaint: a distant tank audible while a nearby enemy was not (anecdotal evidence that
  loudness-as-priority can misfire on gameplay-relevant quiet sounds) — [EA forums](https://forums.ea.com/discussions/battlefield-6-general-discussion-en/am-i-crazy-or-does-bf6-sound-flat-where-did-the-battlefield-feeling-go/13608183/replies/13610985)
- Battlefield 4 (Ben Minto): player foley removed from HDR, so explosions no longer cull footsteps, keeping "a constant
  link between the character and the world"; systems extended to "events near you, and then on you"; G-force strains
  in-cockpit; hip vs ADS firing sounds differ; gun tails chosen by "Area Type"; many context mixers combined by a
  "hierarchical dominance rule-set" — [MCV, Heard about Battlefield 4](https://mcvuk.com/development-news/heard-about-battlefield-4/)
- Interior/exterior in other games: a Wwise racing project uses a camera RTPC (0-0.3 cockpit = centred mono, 0.3-0.6 hood
  = subtle stereo, 0.6-1 third person = full stereo) — [Audiokinetic, building a car racing system](https://blog.audiokinetic.com/building-a-car-racing-system-using-wwise/);
  Unreal Ambient Zones apply a low-pass multiplier and fade time to sounds on the other side of an interior boundary — [Unreal docs, Ambient Zones](https://dev.epicgames.com/documentation/unreal-engine/ambient-zones-in-unreal-engine);
  players complain when cockpit views lose bass or become "incredibly quiet" — [GTPlanet Driveclub thread](https://www.gtplanet.net/forum/threads/driveclub-sound-thread.321626/latest), [Forza forums](https://forums.forza.net/t/why-do-road-cars-sound-like-stripped-down-race-cars-in-cockpit-view/716751)
- Aviation alerting (FAA AC 25.1322-1 as summarised by AOPA): alerts must get attention through at least two senses;
  an active aural alert should finish before another of equal or lower urgency begins, higher urgency may interrupt, and
  an interrupted alert may repeat afterwards if the condition persists; multiword voice alerts are "more likely jumbled and
  truncated if there are competing alerts"; two simultaneous tones are especially confusing; an aural warning must be
  repeated because a single unexpected sound may not be consciously recognised; stall/gear warnings repeat and should not
  be cancellable until fixed, while autopilot-disconnect and cabin-altitude are one-time calls; under stress "extraneous
  beeps, buzzes, clicks, and horns may be disregarded or mistakenly attributed" (example: a pilot repeatedly silencing an
  unrelated beeper ended in a gear-up landing); AC 25-23 gives a 21-level priority table, wind shear top, traffic
  advisory bottom — [AOPA, Warning systems (2016)](https://www.aopa.org/news-and-media/all-news/2016/july/pilot/warning-systems);
  source document [FAA AC 25.1322-1](https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_25.1322-1.pdf)

### Inferences
- Cockpit mix for IRONFEUD (inferred design): in cockpit camera, route the player's own mech through a "hull" bus:
  exterior world low-passed (Unreal-style interior LPF), own footfalls *gain* sub and contact-mic chassis resonance (the
  Titanfall "interior-esque heaviness") rather than losing level, a tight early-reflection reverb, and a narrow/centred
  image for own-mech sounds. Chase camera: full stereo, less sub, more exterior detail. This addresses the MechWarrior
  criticism directly: in cockpit, footfalls should be *felt* (sub + structure-borne thud + rattle of loose cockpit
  fittings), not quieter.
- Exclude the player mech's footfalls and servos from any ducking/HDR culling (Battlefield 4 lesson), so weight is
  never lost in a firefight.
- An HDR-lite in C#: each one-shot gets an authored loudness (dB SPL-like) and a priority bonus (aimed at player, from
  current target, incoming missile); compute a window top from the loudest active logical loudness with a release
  (envelope-tracked); attenuate everything relative to it; cull voices below window-bottom so the 48-voice pool is spent
  on what matters. Add a gameplay override so a nearby *threat* is never culled by a distant loud sound (the BF6 complaint).
- Warnings follow the aviation hierarchy: critical (shutdown imminent, missile lock/launch, critical damage to a leg)
  repeat until resolved or acknowledged; cautions (heat high, ammo low, weapon destroyed) are one-shot; no two voice lines
  overlap; a higher priority line interrupts, lower ones queue or are dropped if stale; every voice warning is paired
  with a HUD indicator (two-sense rule).

### Gaps
- No primary source on how any mech game separates cockpit and exterior footfall mixes (MW5 only says the player is
  "always inside the mech").
- Exact HDR window size/release numbers from DICE were not in the readable slides; the GDC 2009 talk itself was not accessible.

## 4. Weapon audio language: recognisable families, near/far tails, incoming-fire cues

### Takeaway
Recognisability comes from giving each weapon family a distinct concept and spectral/temporal signature (avoid generic
"pew"), building weapons from separable components (mechanics, low end, close detail, far detail, tail) so distance and
environment swap the right parts, and treating incoming fire as its own sound class with distinct cues (whizz vs crack
radii, beep-rate escalation for locks).

### Cited Findings
- Titanfall: no "pew pew" lasers; each weapon a distinct identity so players can read threat level from sound — [Post Magazine](https://www.postmagazine.com/Publications/Post-Magazine/2014/March-1-2014/Audio-For-Games.aspx)
- Titanfall 2: "apply the same rules of physics to many of the completely artificial energy-based weapons" via both asset
  design and real-time processing; reverb and asset swapping model room/environment types for weapon tails and explosions;
  organic layers for character (processed mastiff barks in a shotgun) — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Titanfall 1 examples of unusual sources for sci-fi weapons: malfunctioning escalator, grocery box crusher and a processed
  motorcycle rev for an orbital railgun; portcullis sounds plus synth for a shield wall; canister pop + electrical ripple for
  electric smoke; titan weapons made "bigger and more exciting" than pilot weapons — [Post Magazine](https://www.postmagazine.com/Publications/Post-Magazine/2014/March-1-2014/Audio-For-Games.aspx)
- Weapon component breakdown used in game audio: mechanics, low end, close detail, far detail, tail; tails isolated so they
  can be swapped by environment — [Splice, how to design a weapon sound for video games](https://splice.com/blog/?p=16617)
- Battlefield: tails chosen by area type; hip vs ADS sounds differ — [MCV, Ben Minto](https://mcvuk.com/development-news/heard-about-battlefield-4/);
  DICE recorded weapons, choppers and tanks close up to help players judge approach/recession (secondary wiki) — [Giant Bomb, Frostbite 2.0](https://giantbomb.com/wiki/Concepts/Frostbite_2_0)
- Incoming fire: PUBG gives the whizz and the crack different audible radii (whizz from farther), so hearing both means a
  very close pass; later added a velocity check so subsonic rounds only whizz — [PC Gamer, PUBG gunshot sounds](https://www.pcgamer.com/au/how-gunshot-sounds-work-in-playerunknowns-battlegrounds).
  Players of Hell Let Loose found too many cracks and suggested cracks only for near misses — [Steam discussion](https://steamcommunity.com/app/686810/discussions/0/2967272951897658847);
  others found snaps too subtle — [Six Days forum](https://forums.sixdays.com/main/viewtopic.php?p=2058). Tuning frequency and loudness of near-miss cues is a recurring problem.
- Lock/launch warning conventions: a radar-receiver patent argues a variable *beep rate* is easier to map to range/urgency
  than a steadily rising pitch, which "can be unsettling" — [US patent 4182990](https://patents.google.com/patent/US4182990);
  a flight-sim RWR (Falcon BMS, simulation convention, not verified hardware) uses a 1000 Hz tone 100 ms on/100 ms off for
  2 s for missile launch, chirped 3000 Hz for airborne and 500 Hz for surface threats — [Falcon BMS forum](https://forum.falcon-bms.com/post/166741);
  Cold War crews: higher pitch generally meant greater threat — [Bournemouth Univ. oral history](https://histru.bournemouth.ac.uk/CHiDE/Oral_History_of_Defence_Electronics/Oh_those_darn_tapes.htm)
- MW5: with 3-4 mechs each firing several weapons, keeping enemy fire audible is the hard mix problem — [MW5 Clans audio](https://mw5mercs.com/news/2025/04/147-flash-storm-frequencies-audio-in-mechwarrior-5-clans)

### Inferences
- A family language for IRONFEUD (inferred; matches the existing SynthKit intent): lasers = sustained tonal drones with
  sizzle, no fast pitch sweep (burning) or heavy "thud" bolts (pulse); autocannons/cannons = transient crack + falling-pitch
  boom + long rumble; missiles = ignition thump + rising-band whoosh, then an in-flight motor loop with Doppler;
  particle cannon = tearing electric texture + thunder; machine guns = high rate, small low end; flamer = roaring broadband
  with crackle. Give each family a *reserved* frequency/temporal niche so they stay identifiable when stacked (the MW5 low-end
  problem): e.g. lasers mid-high tonal, ballistics low transient, missiles noise band.
- Far vs near: author each report in near and far versions (far = no crack/mechanics, low-passed, more rumble and slap
  echo), crossfade by distance on top of the existing delay; swap tails by map/area (open field vs canyon/urban cover).
- Incoming cues as a separate class addressed to the player: projectile flyby whoosh/whizz within a near radius, a crack
  only for very near misses, impact-on-own-hull sounds through the cockpit bus (structure-borne, with a short HUD rattle),
  missile lock = slow beep, launch = fast beep/continuous tone escalating with time-to-impact (beep-rate, not pitch ramp).
  This fits pillar 2 (AI and player share inputs) only on the perception side: the player hears; the AI uses scans.

### Gaps
- No developer source on how a mech game (MW5, MWO, Armored Core) differentiates laser vs PPC-type vs autocannon families.
- Real military missile-approach-warner audio specifications were not found; only simulator conventions and a patent.

## 5. Warning voice design ("Betty"-style), comms filtering, line lists, callout frequency

### Takeaway
Voice warnings exist because they skip a mental translation step and are hard to ignore; what makes them work is
*distinctness from surrounding speech* and *delivery style* (monotone or urgent, calm and firm), not the voice's sex per
se. In noisy speech-heavy conditions a male voice in monotone/urgent style tested best in one study; in quiet, sex made
no difference but intonation did. Keep vocabulary small, standardised and terse; never overlap lines; repeat only
critical states. Give the voice an in-world source and process it like Titanfall's BT (cockpit speaker, tight reflections).

### Cited Findings
- History: 1970s F-15 testing showed pilots reacted too slowly to warning lights; USAF testing found a verbal warning more
  effective; a human voice conveys urgency and "clear, unambiguous directions at the point of need"; actress Kim Crow
  recorded the first digitised F-15 words; female voice "purportedly" chosen to stand out to male pilots (unconfirmed);
  nicknames "Bitching Betty", "Sexy Sally" (B-58), "Barking Bob" (male) — [Nautilus, How talking machines got their voices](https://nautil.us/how-talking-machines-got-their-voices-1202193/)
- Leslie Shook voiced the F/A-18 warning system for ~20 years, retiring 2016 — [Marine Corps Times](https://www.marinecorpstimes.com/story/military/2016/03/14/woman-behind-hornets-bitchin-betty-voice-retires/81767046)
- Arrabito (2009), "Effects of Talker Sex and Voice Style of Verbal Cockpit Warnings on Performance", *Human Factors* 51(1):
  3-20: aimed to make warnings distinct from flight-deck speech; with simulated background radio traffic, a male voice in
  monotone or urgent style gave the most correct and fastest identifications regardless of listener gender — [Wikipedia, Voice warning system](https://en.wikipedia.org/wiki/Voice_warning_system);
  "If there are no competing background sounds, it doesn't matter the sex of the talker... But when you start to introduce
  speech babble, the sex of the talker... becomes" relevant; effective use of speech parameters and word semantics increases
  warning salience — [DRDC, Defence researches audio warning in the cockpit](https://www.canada.ca/en/defence-research-development/news/articles/defence-researches-audio-warning-cockpit.html);
  in quiet, sex made no difference to reaction time but intonation did — [Yahoo/CNN summary](https://ca.news.yahoo.com/facts-male-female-voices-being-110000631.html)
- An earlier (1996) UK DRA report recorded pilots asking for female voices for "greater clarity" — in tension with 2009 — [Yahoo/CNN summary](https://ca.news.yahoo.com/facts-male-female-voices-being-110000631.html)
- Related study on vocal emotion and gender conveying urgency of verbal auditory warnings exists — [Canadian Acoustics, Journal of the CAA](https://jcaa.caa-aca.ca/index.php/jcaa/article/view/1387) (not read in full)
- Voice "eliminates a vital mental step—translating a warning into words", is "more difficult to disregard", but vocabulary
  must be standardised; concerns are "standardization, priority, repetition, generic annunciation, and the ability or
  inability to cancel"; multiword voice alerts get jumbled when competing — [AOPA](https://www.aopa.org/news-and-media/all-news/2016/july/pilot/warning-systems)
- Titanfall 2 processing chain for an in-world machine voice: core character then environment colouring; cockpit =
  small-speaker + tight reverb; outside = PA with bass, distortion, distance slap; far = helmet radio — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)
- Titanfall 2 robot voices were also made by convolving drones/metal with teletype and radio dialogue (a machine timbre
  without a franchise reference) — [A Sound Effect](https://www.asoundeffect.com/titanfall-2-sound/)

### Inferences
- For IRONFEUD's original warning voice (inferred): one consistent cockpit computer voice, firm and calm, slightly monotone,
  clearly distinct from lancemate radio chatter (different sex/timbre from the most common lancemate voices, *and*
  different processing: lancemates through band-limited radio with noise/squelch; the cockpit voice through a clean-ish
  small cockpit speaker with tight early reflections). Distinctness from other speech is what the research measures.
- Line list, kept terse (2-3 words, keyword first) and original, e.g. "Heat critical", "Shutdown imminent", "Coolant flush",
  "Missile lock", "Missile launch", "Left leg critical", "Armour breach, torso", "Weapon offline", "Ammunition low",
  "Sensor lock", "Jump jets ready"/"Jets depleted", "Reactor restart". Pair each with a HUD element.
- Callout rules: critical (heat shutdown imminent, missile launch) repeat with a minimum interval (e.g. every 3-5 s) until
  resolved; cautions fire once per episode with hysteresis (re-arm only after the value has clearly recovered, so heat
  oscillating near a threshold doesn't nag); a global voice cooldown; a priority queue where a higher priority interrupts
  and stale low-priority lines are dropped; a tone precedes the critical callouts (attention-getter) so the voice can stay
  calm. These numbers are design suggestions, not sourced.
- Production with the local toolchain: Kokoro TTS for lines, then chain: high-pass ~150-300 Hz and low-pass ~4-6 kHz for a
  small-speaker colour, light saturation, a very short (~10-30 ms) cockpit reflection, and for lancemates an additional
  band-limit, noise bed, squelch clicks and keyed start/stop. Generate several voices and takes and let the user pick by
  ear (he has already rejected one "HR lady" voice).

### Gaps
- Exact Arrabito effect sizes and the full abstract were not retrieved; findings come via DRDC press release and Wikipedia.
- No source found on recommended callout intervals or cooldowns in games; aviation guidance gives repetition principles only.
- No sourced analysis of what players find annoying in game warning voices (e.g. forum surveys on MechWarrior's Betty).
