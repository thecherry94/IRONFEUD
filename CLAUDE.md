# CLAUDE.md

Hobby project: a single-player mech combat game built in **Godot 4 (.NET / C#)**.
Developer: Philipp. Robotics engineer (ROS2, MoveIt2, C#/.NET, C/C++), about 20 years of programming.
Treat him as a senior engineer: be direct, skip the basics, and push back when an approach is wrong.

## What makes this game special (don't lose this)
1. **Weight comes from procedural motion, not canned animation.** Gait phase → planted foot targets →
   analytic 2-bone IK → spring-driven secondary motion with impulses on every footfall.
   Read `docs/locomotion.md` before touching anything that moves.
2. **AI pilots use exactly the same `MechInput` the player uses.** They get no shortcuts in physics, heat or turn rates,
   and they only perceive what a pilot could see. Read `docs/combat-ai.md` before touching AI.
3. **Readable, fallible enemies, not perfect ones, and never a pattern to farm.** Aim error, reaction time, visible
   intent; but decisions are situational and varied enough that players think "this AI is actually smart" and can't
   learn to play it like a fiddle. Prefer teamwork, terrain use and adapting to the situation over cheats.
4. **Every weight class stays viable to the end of the campaign.** Bigger must never simply be better (the genre's
   endgame "lance of assaults" is the anti-goal). Lights and mediums keep jobs assaults can't do, through mission
   design, campaign constraints (tonnage/budget caps, upkeep, transport), in-battle mechanics (facing armour, twist
   and turn rates, sensors and spotting, terrain access) and per-model quirks. Verify with headless runs over a
   mission-type × lance-composition matrix: no composition may win every mission type.

## Long-term vision (shapes architecture now, built later)
A sandbox mech game in space, structured like an overworld-plus-battles RPG: a galaxy strategy layer
(star systems, travel, trade, contracts, factions with AI leaders; the player can join or found a faction)
on top of tactical mech battles. Two layers, one hard boundary:
- **Campaign sim**: pure C#, deterministic, tick-based, fully serializable, no Godot types. Owns the galaxy,
  economy, factions, the player's company (mechs, pilots, money, reputation).
- **Tactical battle**: Godot scene. Input `BattleSetup` (map, units with *persistent* component damage,
  loadouts, pilot skills, objectives) → output `BattleResult` (damage, salvage, kills, injuries, outcome).
- **Auto-resolve** for battles the player isn't in must be calibrated against real headless AI-vs-AI battles.
- Design consequences now: mech/pilot state lives in data records, not only in nodes; damage persists between battles;
  AI squads accept orders (the player will command lancemates).

## Hard rules
- **Original IP only.** No names, mech designs, factions, places, weapon names, sound or voice lines from any
  existing mech franchise. Generic genre concepts (heat, torso twist, cockpit warning voice) are fine.
  If a design looks recognisably like an existing franchise mech, change it.
- **Never edit** `.godot/`, `*.import` or `*.uid` files by hand, and never commit `.godot/`.
- Keep gameplay math in **plain C# classes with no Godot dependency** wherever possible (gait solver, IK,
  springs, utility scoring, heat/damage). Nodes are thin adapters around them. That keeps the logic unit-testable
  with `dotnet test` and the logic-to-render boundary explicit.
- Simulation runs on a **fixed 120 Hz physics tick** (`Engine.PhysicsTicksPerSecond = 120`). Gameplay logic goes in
  `_PhysicsProcess`; only cosmetic interpolation goes in `_Process`.
- Units: meters, seconds, radians, kg. Damage/heat are unitless (heat 0..1+).
- Before hand-editing a `.tscn`/`.tres`, read the whole file. Prefer building procedural/complex
  hierarchies (the mech rig) in C# over large hand-written scenes.

## Coordinate conventions (important: differs from the web prototype)
- Godot: right-handed, +Y up, **forward = -Z** (`-Basis.Z`). Mechs face -Z in their local frame.
- Heading `psi` = `Rotation.Y`. World forward = `(-sin psi, 0, -cos psi)`; mech's **left = -X local**.
- The web prototype in `reference/web-prototype/` uses **+Z forward and +X = left**, with forward = `(sin psi, 0, cos psi)`.
  When porting formulas, flip signs deliberately and add a unit test that pins the convention.

## Commands
```bash
dotnet build IRONFEUD.sln          # compile everything (bare `dotnet build` fails: .sln and .csproj share the root)
godot --path . &                   # run the editor/game (Godot .NET build on PATH)
godot --path . --headless --quit   # import/validate project in CI or before commits
dotnet test tests/                 # pure-logic unit tests (xUnit)
# Modes: --watch=lance|1v1|2v2 (AI vs AI, default lance: one of each class per side), --play=1v1|2v2, --range (firing range),
# --parade (all chassis classes running laps, for gait work); in game F5-F11.
# Captures: --autopilot (range, scripted walk, fires 3-9 s, jumps at 2 / 7 / 12 s if the chassis has jets), --range-demo (range, scripted shooting);
# --camera=chase|cockpit|orbit|overview; --debug shows gait and hit spheres; --light|--heavy|--assault and --sicklefoot pick the player's class and model (L / M cycle in game);
# --select=N starts on mech N (Tab order); --map=nml|hills|flat picks the battle map (T cycles; No Man's Land is the default); --seed=N varies it:
godot --path . --resolution 960x540 --fixed-fps 30 --write-movie /tmp/ironfeud-cap/f.png --quit-after 600 -- --watch=2v2
dotnet test tests/ --filter AiDuels --logger "console;verbosity=detailed"   # headless AI duel summary
```
Pinned: `Godot 4.7.2 .NET` (Godot.NET.Sdk 4.7.2), all projects target `net8.0` (built with .NET SDK 10).

## Layout (create as needed, keep it shallow)
```
src/
  IronFeud.Logic/   separate class library, NO Godot reference (enforced by the compiler). Hidden from Godot via .gdignore.
    Core/           MathUtil, Rot (Godot-convention quaternions), Spring, DeterministicRng
    Mech/           MechInput, MechChassis (classes), BodyKinematics, GaitController, LegIK, SecondaryMotion,
                    MechPoseSolver, MechLocomotion, MechSim (whole mech), AimSolver; Weapons/, Damage/
    Battle/         ArenaMap (blocks, cover, rays), BattleSim (headless battle: units, shots, damage, collision)
    AI/             AiPilot (the prototype's utility AI), Personality
    Audio/          SynthKit (one-shot synthesis), ServoSynth (joint-speed motor whine); pure DSP, no Godot
    Campaign/       (later) the campaign sim
  Core/             Godot-side helpers: GodotConv (System.Numerics <-> Godot), ProceduralTextures
  Mech/             MechNode (view of a BattleUnit), MechRig (builds the node hierarchy), GaitDebugDraw
  AI/               AiDebugDraw (overlay), AiPanel (inspector)
  Audio/            AudioDirector (pooled 3D one-shots, sample sets in assets/audio/<set>/ override synthesis), ServoVoice
  Game/             Main (modes/sessions), Battlefield (steps + renders BattleSim), Arena (renders ArenaMap),
                    CameraRig (chase/cockpit/orbit/overview), PlayerMechController, Hud, Effects, GameInput
scenes/             main.tscn (thin: just the Main script)
tests/              xUnit project referencing IronFeud.Logic
docs/               design notes (read on demand); docs/research/mech-genre/report.md = genre research + roadmap
reference/web-prototype/   working three.js prototypes; the behavioral source of truth for feel
```
Logic uses `System.Numerics` in Godot's axis convention, so conversion is a plain field copy (`GodotConv`).
Semantic angles (pitch + = down, roll + = lean right) become Godot Euler angles only in `MechPoseSolver`.

## Roadmap (vertical slice first; don't build content before the loop is fun)
1. Port the walker: rig + gait + IK + springs, driven by keyboard/mouse. Must match the prototype's feel.
2. Weapons (cannon projectile, hitscan laser), heat, convergence on torso aim, recoil.
3. Damage model per section (torso, arms carry weapons, legs → speed cap), wreck state.
4. Arena with cover + the utility AI from the prototype, spectator/debug camera and overlay.
5. Terrain: raycast foot placement + hip height/slope adaptation (the prototype assumes flat ground).
6. Audio: layered footfalls, procedural servo whine from joint velocity, comms-filtered warning voice.
Then: more weapons, mech classes, missions.

**Status (2026-10-31):** steps 1-5 done, plus the pulled-forward "mech classes" item: four weight classes at genre
speeds (light 130 / medium 80 / heavy 60 / assault 40 km/h) with a running gait, two body models (Bascinet biped,
Sicklefoot digitigrade), a weapon roster with class loadouts, class roles in the AI, and a No Man's Land arena.
Since then (2026-11-01): audio (synthesised placeholders), burning/pulse lasers, facing armour, jump jets + jump AI,
stabilised weapons, weapon ranges (optimal -> zero at max), missiles (rocket pod / strike rack / guided arc missiles)
and point defense, support calls, symmetric maps, armour over fragile internal structure (critical effects),
sensor-lock scans shared per lance (the AI decides on enemy state only from scans), machine guns / flamethrower /
incendiary cannon / particle cannon with per-model loadouts, heat shutdowns, signature by size, designator marks
(lights) and ECM (Sicklefoot light), a tiny recessed cockpit, Bascinet shoulder cover plates, arc missiles steering on
a 1 s lock track (a weaving light shakes them) with missile evasion, steering that sees low/sunken blocks and wrecks
and keeps to one detour side (no more mechs stuck on block corners). A restart after a heat shutdown flushes the
coolant (no more flamer stun-locks). Open items: own guns can overshoot heat 1 into a shutdown (a 0.32 bolt fired at
0.99), the hills medium duel favours the Bascinet ~60-40, chassis `.tres` for editor tuning,
spring frequencies don't scale with size, heavy/assault are scaled copies of the medium body, heavies still lead
kills in lances (0.7-1.1/game vs 0.35-0.55), the siege cannon is the top killer of flamer lights, light counterplay
at range (re-measure: firing now reveals a light at full size), squad tactics (2v2+ pick cover independently),
real sounds for the new weapons.

## Working agreements for Claude Code
- Small, reviewable commits; one concern per commit. Run `dotnet build IRONFEUD.sln` and the tests before saying done.
- When tuning feel, expose constants via `[Export]` or a `Resource` (e.g. a chassis `.tres`) rather than hard-coding.
- When behavior differs from the prototype, say so explicitly and explain why.
- Debug visualisation is a first-class feature: every AI decision and gait target should be drawable.
- Check `dotnet test` by its exit code / summary line before committing, never by chaining on piped output.

## Lessons learned (read before changing locomotion, AI or balance)
- **Measure before fixing, then pin it.** Every "looks janky / feels wrong" report so far had a measurable cause
  (hip roll flipping to 180°, a 58 m/s pelvis, 16 steps/s shuffling, torso pitching 57°, a stale assembly). Write a
  throwaway diagnostic test that prints metrics, fix, then turn the metrics into a regression test
  (`AcrossNoMansLand_PosesStayNatural`, `AtTopSpeed_StrideIsSetByLegReach_NotByAShuffle`, the evasion tests).
- **Exact IK, cheated inputs.** Keep the analytic solvers exact; make it look right by feeding them well-posed targets
  (clamp below the hip / within roll and reach, foothold selection, smoothed support height, eased crouch, gyro
  torso, rough-ground speed cap). Never hard-clamp the pelvis per tick (pendulum) or trust raw terrain targets.
- **Prototype numbers don't survive genre speeds.** Anything tuned for 4 m/s (stride caps, accel lean 0.06·acc, fire
  tolerance 0.02 + 1.6/d, back-up rules, crouch rates) needs re-checking when speed, size or weapon change.
- **Balance with the headless reports, not by eye.** One watched match conflated personality, model and spawn side;
  the data showed the model (Sicklefoot's smaller, lower hit spheres) was the cause. Run
  `IRONFEUD_BALANCE=1 dotnet test tests/ --filter BalanceTests --logger "console;verbosity=detailed"` (~3 min) with
  both spawn sides and 20+ seeds per matchup; 12 games is noise.
- **Design intent: distinct complementary roles, not parity;** prefer buffing the weaker option over nerfing.
- **Statistical tests need samples:** several seeds and a margin, or a one-number change reshuffles RNG and flips them.
- **Captures:** Godot runs the assembly from the last `dotnet build IRONFEUD.sln`; rebuild the solution, not just the
  logic library, or captures show old behaviour. `--write-movie` runs far below real time and its window can take
  keyboard focus (stray keys corrupt a capture: re-run if frames look wrong). Don't `pkill -f` a pattern that matches
  your own command line. To let the user watch, launch `godot --path . -- --watch=lance` in the background without
  the movie writer.
