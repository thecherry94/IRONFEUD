using System.Collections.Generic;
using System.Linq;
using Godot;
using IronFeud.AI;
using IronFeud.Audio;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;
using Vec3 = System.Numerics.Vector3;

namespace IronFeud.Game;

public enum GameMode { Range, Watch1v1, Watch2v2, WatchLance, Play1v1, Play2v2, Parade, Lineup }

/// <summary>Battle maps for the watch and play modes (T cycles). The parade uses open ground, hilly unless Flat.</summary>
public enum MapKind { NoMansLand, Hills, Flat }

/// <summary>
/// Test bed. Each mode builds a session: an <see cref="ArenaMap"/> + <see cref="BattleSim"/>, their Godot views,
/// cameras, HUD and debug overlays. Watch modes are AI vs AI (spectator); play modes put the player on team Alpha;
/// the firing range has target mechs and no AI. Switching mode or restarting rebuilds the session.
/// </summary>
public partial class Main : Node3D
{
    [Export] public uint ArenaSeed { get; set; } = 7;

    private static readonly (string Name, Vec3 Position, bool Circle)[] TargetSpawns =
    {
        ("Target A", new(0f, 0f, -60f), false),
        ("Target B", new(-22f, 0f, -95f), false),
        ("Target C", new(24f, 0f, -80f), false),
        ("Target D", new(10f, 0f, -135f), true),
    };

    private const string ModeKeys = "F5 range · F6 watch 1v1 · F7 watch 2v2 · F8 watch lance 4v4 · F9 play 1v1 · F10 play 2v2 · F11 gait parade";
    private const string PlayHelp =
        "W/S throttle (holds) · X stop · A/D turn legs · mouse twist & aim · arrows fine twist/aim · C center torso · Space jump jets · LMB/RMB/MMB or 1-6 weapon groups · [ ] pick weapon · Shift+N in/out of group N · Ctrl+N chain/alpha\n" +
        "V camera · Tab inspect · F3 gait debug · F4 AI overlay · L next class · M next model · R restart · T next map (no man's land / hills / flat) · P pause · O slow-mo · Esc mouse · hold the reticle on a target to lock arc missiles\n" + ModeKeys;
    private const string WatchHelp =
        "Tab next mech · K change its personality · 1 chase · 2 cockpit · 3 orbit · 4 overview (drag / wheel) · F3 gait debug · F4 AI overlay · R restart · T next map (no man's land / hills / flat) · P pause · O slow-mo\n" + ModeKeys;
    private const string ParadeHelp =
        "Tab next mech · 1 chase · 2 cockpit · 3 orbit (drag / wheel) · 4 overview · F3 gait debug · O slow-mo (¼ speed) · P pause · R restart · T next map (no man's land / hills / flat)\n" + ModeKeys;

    private GameMode _mode = GameMode.WatchLance;
    private ChassisClass _playerClass = ChassisClass.Medium;
    private MechModel _playerModel = MechModel.Bascinet;
    private (float Theta, float Phi, float Radius)? _orbit;
    private uint _round;
    private bool _rangeDemo, _gaitDebug, _aiOverlay = true;
    private MapKind _map = MapKind.NoMansLand;
    private CameraMode? _cameraOverride;
    private readonly Dictionary<string, Personality> _personalities = new();

    private Node3D? _session;
    private Battlefield _battlefield = null!;
    private CameraRig _camera = null!;
    private Hud _hud = null!;
    private AiPanel _panel = null!;
    private AiDebugDraw _aiDraw = null!;
    private GaitDebugDraw _gait = null!;
    private MechNode? _player, _selected;
    private double _clock, _restartAt = -1;
    private int _selectAtStart = -1;

    private bool Watching => _mode is GameMode.Watch1v1 or GameMode.Watch2v2 or GameMode.WatchLance;

    /// <summary>
    /// Gait parade lanes, inner to outer: every design in its own class, slow classes inside, lights (widest turning
    /// circles) outside.
    /// </summary>
    private static readonly (ChassisClass Class, MechModel Model, float Radius)[] ParadeLanes =
    {
        (ChassisClass.Assault, MechModel.Barbican, 100f), (ChassisClass.Heavy, MechModel.Harrow, 126f),
        (ChassisClass.Medium, MechModel.Brigandine, 152f), (ChassisClass.Medium, MechModel.Bascinet, 178f),
        (ChassisClass.Light, MechModel.Bodkin, 208f), (ChassisClass.Light, MechModel.Sicklefoot, 238f),
    };

    /// <summary>Parade lane selected at the start (the Bascinet medium, the reference body).</summary>
    private const int ParadeStartLane = 3;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // keys work while paused; the session itself is pausable
        GameInput.Ensure();
        var viewport = GetViewport();
        viewport.Msaa3D = Viewport.Msaa.Msaa4X; // mechs are all hard edges: without MSAA they shimmer and stair-step
        viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            switch (arg)
            {
                case "--range": _mode = GameMode.Range; break;
                case "--range-demo": _mode = GameMode.Range; _rangeDemo = true; break;
                case "--autopilot": _mode = GameMode.Range; break;
                case "--watch=1v1": _mode = GameMode.Watch1v1; break;
                case "--watch=2v2": _mode = GameMode.Watch2v2; break;
                case "--watch=lance": _mode = GameMode.WatchLance; break;
                case "--play=1v1": _mode = GameMode.Play1v1; break;
                case "--play=2v2": _mode = GameMode.Play2v2; break;
                case "--parade": _mode = GameMode.Parade; break;
                case "--lineup": _mode = GameMode.Lineup; break;
                case "--light": _playerClass = ChassisClass.Light; break;
                case "--heavy": _playerClass = ChassisClass.Heavy; break;
                case "--assault": _playerClass = ChassisClass.Assault; break;
                case "--sicklefoot": _playerModel = MechModel.Sicklefoot; break;
                case "--debug": _gaitDebug = true; break;
                case "--flat" or "--map=flat": _map = MapKind.Flat; break;
                case "--map=hills": _map = MapKind.Hills; break;
                case "--map=nml": _map = MapKind.NoMansLand; break;
                case "--camera=chase": _cameraOverride = CameraMode.Chase; break;
                case "--camera=cockpit": _cameraOverride = CameraMode.Cockpit; break;
                case "--camera=orbit": _cameraOverride = CameraMode.Orbit; break;
                case "--camera=overview": _cameraOverride = CameraMode.Overview; break;
            }
            if (arg.StartsWith("--seed=") && uint.TryParse(arg[7..], out uint seed)) ArenaSeed = seed;
            if (arg.StartsWith("--select=") && int.TryParse(arg[9..], out int sel)) _selectAtStart = sel;
            // --orbit=theta,phi,radius: a fixed orbit camera for captures (radians, radius in the target's size units).
            if (arg.StartsWith("--orbit=") && arg[8..].Split(',') is { Length: 3 } o
                && float.TryParse(o[0], System.Globalization.CultureInfo.InvariantCulture, out float th)
                && float.TryParse(o[1], System.Globalization.CultureInfo.InvariantCulture, out float ph)
                && float.TryParse(o[2], System.Globalization.CultureInfo.InvariantCulture, out float ra))
                _orbit = (th, ph, ra);
        }
        StartSession(_mode);
    }

    private void StartSession(GameMode mode)
    {
        if (_session != null)
        {
            RemoveChild(_session);
            _session.QueueFree();
        }
        _mode = mode;
        _round++;
        _restartAt = -1;
        _session = new Node3D { Name = "Session", ProcessMode = ProcessModeEnum.Pausable };
        AddChild(_session);

        BattleSim battle;
        if (mode == GameMode.Range)
        {
            var map = ArenaMap.Scatter(1337, 160f, 150, (-45f, -170f, 45f, -5f));
            battle = new BattleSim(map, _round) { TrackOutcome = false };
            battle.AddUnit("Player", 0, MechChassis.Create(_playerClass, _playerModel), Vec3.Zero, 0f);
            for (int i = 0; i < TargetSpawns.Length; i++)
            {
                var (name, pos, _) = TargetSpawns[i];
                battle.AddUnit(name, 1, i % 2 == 0 ? MechChassis.Medium() : MechChassis.Light(MechModel.Sicklefoot), pos, Mathf.Pi);
            }
        }
        else if (mode == GameMode.Lineup)
        {
            // Silhouette check: every design in its own class, standing in a row on flat ground, facing +z.
            battle = new BattleSim(ArenaMap.Open(ArenaSeed, false), _round) { TrackOutcome = false };
            var designs = System.Enum.GetValues<MechModel>();
            for (int i = 0; i < designs.Length; i++)
            {
                var c = MechChassis.Design(designs[i]);
                battle.AddUnit(c.Name, 0, c, new Vec3((i - (designs.Length - 1) / 2f) * 22f, 0f, 0f), Mathf.Pi);
            }
        }
        else if (mode == GameMode.Parade)
        {
            battle = new BattleSim(ArenaMap.Open(ArenaSeed, _map != MapKind.Flat), _round) { TrackOutcome = false };
            foreach (var (cls, model, radius) in ParadeLanes)
            {
                var c = MechChassis.Create(cls, model);
                battle.AddUnit(c.Name, 0, c, new Vec3(radius, 0f, 0f), 0f);
            }
        }
        else
        {
            var map = _map == MapKind.NoMansLand ? ArenaMap.NoMansLand(ArenaSeed) : ArenaMap.Arena(ArenaSeed, hills: _map == MapKind.Hills);
            battle = new BattleSim(map, ArenaSeed * 31u + _round);
            SpawnTeams(battle, mode);
        }
        Arena.Build(_session, battle.Map, ArenaSeed);

        var effects = new Effects { Name = "Effects" };
        var audio = new AudioDirector { Name = "Audio" };
        _battlefield = new Battlefield { Name = "Battlefield", Effects = effects, Audio = audio };
        effects.Battlefield = _battlefield;
        _session.AddChild(effects);
        _session.AddChild(audio);
        _session.AddChild(_battlefield);
        _battlefield.Begin(battle, teamColors: mode != GameMode.Range);
        _player = mode is GameMode.Range or GameMode.Play1v1 or GameMode.Play2v2 ? _battlefield.Mechs[0] : null;

        _camera = new CameraRig { Name = "Camera", Mechs = _battlefield.Mechs };
        _session.AddChild(_camera);
        _camera.MakeCurrent();
        _camera.SetMode(_cameraOverride ?? (_player != null ? CameraMode.Chase : mode is GameMode.Parade or GameMode.Lineup ? CameraMode.Orbit : CameraMode.Overview));
        if (_orbit is { } orbit) _camera.SetOrbit(orbit.Theta, orbit.Phi, orbit.Radius);
        _cameraOverride = null;

        PlayerMechController? pilot = null;
        if (_player != null)
        {
            pilot = new PlayerMechController { Name = "Pilot", Mech = _player, Camera = _camera, Battlefield = _battlefield };
            _session.AddChild(pilot);
        }
        else Input.MouseMode = Input.MouseModeEnum.Visible;

        if (mode == GameMode.Range)
        {
            for (int i = 0; i < TargetSpawns.Length; i++)
                _battlefield.Mechs[i + 1].AddChild(new TargetDummy { Mech = _battlefield.Mechs[i + 1], Circle = TargetSpawns[i].Circle });
            if (_rangeDemo) AddRangeDemo();
        }
        if (mode == GameMode.Parade)
        {
            var parade = new ParadeDirector { Name = "Parade" };
            for (int i = 0; i < ParadeLanes.Length; i++) parade.Lanes.Add((_battlefield.Mechs[i], ParadeLanes[i].Radius));
            _session.AddChild(parade);
        }

        _gait = new GaitDebugDraw { Name = "GaitDebug", AllMechs = _battlefield.Mechs, Visible = _gaitDebug };
        _session.AddChild(_gait);
        _aiDraw = new AiDebugDraw { Name = "AiDebug", Battle = battle, Player = _player, ShowAi = _aiOverlay && Watching };
        _session.AddChild(_aiDraw);
        _hud = new Hud { Name = "Hud", Mech = _player, Camera = _camera, Pilot = pilot, Battlefield = _battlefield, HelpText = mode == GameMode.Parade ? ParadeHelp : Watching ? WatchHelp : PlayHelp };
        _session.AddChild(_hud);
        _panel = new AiPanel { Name = "AiPanel", Battle = battle, Visible = _aiOverlay && Watching };
        _session.AddChild(_panel);

        int first = _selectAtStart >= 0 ? Mathf.Min(_selectAtStart, _battlefield.Mechs.Count - 1) : mode == GameMode.Parade ? ParadeStartLane : 0;
        Select(_player ?? _battlefield.Mechs[first]); // parade: start on the medium Sicklefoot
    }

    private void SpawnTeams(BattleSim b, GameMode mode)
    {
        // Alpha starts at +z facing -z, Bravo at -z facing +z (the prototype's spawns, mirrored into Godot's frame).
        void Ai(string name, int team, ChassisClass c, float x, float z, Personality p, MechModel model = MechModel.Bascinet) =>
            b.AddUnit(name, team, MechChassis.Create(c, model), new Vec3(x, 0f, z), team == 0 ? 0f : Mathf.Pi,
                _personalities.GetValueOrDefault($"{mode}:{name}", p));
        void Player(float x, float z) => b.AddUnit("Player", 0, MechChassis.Create(_playerClass, _playerModel), new Vec3(x, 0f, z), 0f);

        const ChassisClass M = ChassisClass.Medium, L = ChassisClass.Light;
        const MechModel SF = MechModel.Sicklefoot, BR = MechModel.Brigandine;
        const float z = ArenaMap.ArenaSpawnZ, z2 = z + 17f, x = 30f;
        switch (mode)
        {
            case GameMode.Watch1v1:
                Ai("Alpha-1", 0, M, 0f, z, Personality.Balanced);
                Ai("Bravo-1", 1, M, 0f, -z, Personality.Brawler, BR);
                break;
            case GameMode.Watch2v2:
                Ai("Alpha-1", 0, M, x, z, Personality.Brawler);
                Ai("Alpha-2", 0, M, -x, z2, Personality.Sniper, BR);
                Ai("Bravo-1", 1, M, -x, -z, Personality.Balanced, BR);
                Ai("Bravo-2", 1, M, x, -z2, Personality.Sniper);
                break;
            case GameMode.WatchLance:
                // One of each class per side: light skirmisher, medium, heavy line, assault anchor, each its class's
                // dedicated design. Both sides field the same lance on a point-symmetric map: a fair match (mirrored model
                // mixes tilted it ~59-41). The light and medium designs alternate with each restart.
                bool first = _round % 2 == 1;
                MechModel light = first ? SF : MechModel.Bodkin, medium = first ? MechModel.Bascinet : MechModel.Brigandine;
                foreach (var (team, sign, name) in new[] { (0, 1f, "Alpha"), (1, -1f, "Bravo") })
                {
                    Ai($"{name}-1", team, L, -45f * sign, sign * z, Personality.Balanced, light);
                    Ai($"{name}-2", team, M, -15f * sign, sign * z, Personality.Balanced, medium);
                    Ai($"{name}-3", team, ChassisClass.Heavy, 15f * sign, sign * z2, Personality.Balanced, MechModel.Harrow);
                    Ai($"{name}-4", team, ChassisClass.Assault, 45f * sign, sign * z2, Personality.Sniper, MechModel.Barbican);
                }
                break;
            case GameMode.Play1v1:
                Player(0f, z);
                Ai("Bravo-1", 1, M, 0f, -z, Personality.Balanced, BR);
                break;
            case GameMode.Play2v2:
                Player(x, z);
                Ai("Alpha-2", 0, M, -x, z2, Personality.Sniper);
                Ai("Bravo-1", 1, M, -x, -z, Personality.Balanced);
                Ai("Bravo-2", 1, M, x, -z2, Personality.Brawler, BR);
                break;
        }
    }

    /// <summary>Scripted shooting for captures: cockpit kill, torso kill, arm and a leg off.</summary>
    private void AddRangeDemo()
    {
        var t = _battlefield.Mechs;
        var demo = new RangeDemoPilot { Name = "RangeDemo", Mech = _player };
        demo.Tasks.Add((t[1], MechSection.Cockpit, 0b01u));
        demo.Tasks.Add((t[2], MechSection.Torso, 0b11u));
        demo.Tasks.Add((t[3], MechSection.ArmLeft, 0b10u));
        demo.Tasks.Add((t[3], MechSection.LegLeft, 0b01u));
        _session!.AddChild(demo);
    }

    private void Select(MechNode m)
    {
        _selected = m;
        _gait.Mech = m;
        _aiDraw.Selected = m;
        _panel.Selected = m;
        _camera.Target = _player ?? m;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsPressed() || e.IsEcho()) return;
        // 1-4 pick a camera only when watching: a pilot's number keys fire weapon groups. V cycles in every mode.
        if (_player == null && e.IsAction("cam_chase")) _camera.SetMode(CameraMode.Chase);
        else if (_player == null && e.IsAction("cam_cockpit")) _camera.SetMode(CameraMode.Cockpit);
        else if (_player == null && e.IsAction("cam_orbit")) _camera.SetMode(CameraMode.Orbit);
        else if (_player == null && e.IsAction("cam_overview")) _camera.SetMode(CameraMode.Overview);
        else if (e.IsAction("cam_cycle")) _camera.SetMode((CameraMode)(((int)_camera.Mode + 1) % 4));
        else if (e.IsAction("select_next"))
        {
            var mechs = _battlefield.Mechs.ToList();
            Select(mechs[(mechs.IndexOf(_selected!) + 1) % mechs.Count]);
        }
        else if (e.IsAction("cycle_personality") && _selected?.Unit.Ai is { } ai)
        {
            int i = System.Array.IndexOf(Personality.All, ai.Personality);
            ai.Personality = Personality.All[(i + 1) % Personality.All.Length];
            _personalities[$"{_mode}:{_selected.Unit.Name}"] = ai.Personality;
        }
        else if (e.IsAction("debug_draw")) _gait.Visible = _gaitDebug = !_gait.Visible;
        else if (e.IsAction("ai_overlay"))
        {
            _aiOverlay = !_aiDraw.ShowAi;
            _aiDraw.ShowAi = _panel.Visible = _aiOverlay;
        }
        else if (e.IsAction("pause")) GetTree().Paused = !GetTree().Paused;
        else if (e.IsAction("slow_motion")) Engine.TimeScale = Engine.TimeScale < 1.0 ? 1.0 : 0.25;
        else if (e.IsAction("restart")) StartSession(_mode);
        else if (e.IsAction("cycle_chassis"))
        {
            _playerClass = (ChassisClass)(((int)_playerClass + 1) % System.Enum.GetValues<ChassisClass>().Length);
            if (_player != null) StartSession(_mode);
        }
        else if (e.IsAction("toggle_terrain"))
        {
            _map = (MapKind)(((int)_map + 1) % System.Enum.GetValues<MapKind>().Length);
            if (_mode != GameMode.Range) StartSession(_mode);
        }
        else if (e.IsAction("cycle_model"))
        {
            _playerModel = (MechModel)(((int)_playerModel + 1) % System.Enum.GetValues<MechModel>().Length);
            if (_player != null) StartSession(_mode);
        }
        else if (e.IsAction("mode_range")) StartSession(GameMode.Range);
        else if (e.IsAction("mode_watch_1v1")) StartSession(GameMode.Watch1v1);
        else if (e.IsAction("mode_watch_2v2")) StartSession(GameMode.Watch2v2);
        else if (e.IsAction("mode_watch_lance")) StartSession(GameMode.WatchLance);
        else if (e.IsAction("mode_play_1v1")) StartSession(GameMode.Play1v1);
        else if (e.IsAction("mode_play_2v2")) StartSession(GameMode.Play2v2);
        else if (e.IsAction("mode_parade")) StartSession(GameMode.Parade);
    }

    public override void _Process(double delta)
    {
        if (GetTree().Paused) return;
        _clock += delta;
        var battle = _battlefield.Battle;
        if (battle.Over && _restartAt < 0)
        {
            _restartAt = _clock + 9.0;
            string text = battle.Winner < 0 ? "DRAW" : $"{Battlefield.TeamName[battle.Winner]} WINS";
            _hud.ShowBanner(text, battle.Winner < 0 ? Colors.White : Battlefield.TeamColor[battle.Winner],
                "next round in a few seconds · R to restart now");
        }
        else if (_restartAt > 0 && _clock > _restartAt) StartSession(_mode);
    }
}
