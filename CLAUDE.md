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
3. **Readable, fallible enemies, not perfect ones.** Aim error, reaction time, visible intent.

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
# Captures: --autopilot (range, scripted walk, fires 3-9 s), --range-demo (range, scripted shooting);
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
    Campaign/       (later) the campaign sim
  Core/             Godot-side helpers: GodotConv (System.Numerics <-> Godot), ProceduralTextures
  Mech/             MechNode (view of a BattleUnit), MechRig (builds the node hierarchy), GaitDebugDraw
  AI/               AiDebugDraw (overlay), AiPanel (inspector)
  Audio/            (later) SfxBank, ProceduralServo, WarningVoice
  Game/             Main (modes/sessions), Battlefield (steps + renders BattleSim), Arena (renders ArenaMap),
                    CameraRig (chase/cockpit/orbit/overview), PlayerMechController, Hud, Effects, GameInput
scenes/             main.tscn (thin: just the Main script)
tests/              xUnit project referencing IronFeud.Logic
docs/               design notes (read on demand)
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

## Working agreements for Claude Code
- Small, reviewable commits; one concern per commit. Run `dotnet build IRONFEUD.sln` and the tests before saying done.
- When tuning feel, expose constants via `[Export]` or a `Resource` (e.g. a chassis `.tres`) rather than hard-coding.
- When behavior differs from the prototype, say so explicitly and explain why.
- Debug visualisation is a first-class feature: every AI decision and gait target should be drawable.
