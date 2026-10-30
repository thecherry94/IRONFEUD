using System.Collections.Generic;
using System.Linq;
using Godot;
using IronFeud.AI;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;
using Vec3 = System.Numerics.Vector3;

namespace IronFeud.Game;

public enum GameMode { Range, Watch1v1, Watch2v2, WatchLights, Play1v1, Play2v2 }

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

    private const string ModeKeys = "F5 range · F6 watch 1v1 · F7 watch 2v2 · F8 watch lights vs medium · F9 play 1v1 · F10 play 2v2";
    private const string PlayHelp =
        "W/S throttle (holds) · X stop · A/D turn legs · mouse twist & aim · arrows fine twist/aim · C center torso · LMB/Space cannon · RMB/F laser\n" +
        "1 chase · 2 cockpit · 3 orbit · 4 overview · Tab inspect · F3 gait debug · F4 AI overlay · L next chassis class · R restart · P pause · O slow-mo · Esc mouse\n" + ModeKeys;
    private const string WatchHelp =
        "Tab next mech · K change its personality · 1 chase · 2 cockpit · 3 orbit · 4 overview (drag / wheel) · F3 gait debug · F4 AI overlay · R restart · P pause · O slow-mo\n" + ModeKeys;

    private GameMode _mode = GameMode.Watch1v1;
    private ChassisClass _playerClass = ChassisClass.Medium;
    private uint _round;
    private bool _rangeDemo, _gaitDebug, _aiOverlay = true;
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

    private bool Watching => _mode is GameMode.Watch1v1 or GameMode.Watch2v2 or GameMode.WatchLights;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // keys work while paused; the session itself is pausable
        GameInput.Ensure();
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            switch (arg)
            {
                case "--range": _mode = GameMode.Range; break;
                case "--range-demo": _mode = GameMode.Range; _rangeDemo = true; break;
                case "--autopilot": _mode = GameMode.Range; break;
                case "--watch=1v1": _mode = GameMode.Watch1v1; break;
                case "--watch=2v2": _mode = GameMode.Watch2v2; break;
                case "--watch=lights": _mode = GameMode.WatchLights; break;
                case "--play=1v1": _mode = GameMode.Play1v1; break;
                case "--play=2v2": _mode = GameMode.Play2v2; break;
                case "--light": _playerClass = ChassisClass.Light; break;
                case "--heavy": _playerClass = ChassisClass.Heavy; break;
                case "--assault": _playerClass = ChassisClass.Assault; break;
                case "--debug": _gaitDebug = true; break;
                case "--camera=chase": _cameraOverride = CameraMode.Chase; break;
                case "--camera=cockpit": _cameraOverride = CameraMode.Cockpit; break;
                case "--camera=orbit": _cameraOverride = CameraMode.Orbit; break;
                case "--camera=overview": _cameraOverride = CameraMode.Overview; break;
            }
            if (arg.StartsWith("--seed=") && uint.TryParse(arg[7..], out uint seed)) ArenaSeed = seed;
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
            battle.AddUnit("Player", 0, MechChassis.Create(_playerClass), Vec3.Zero, 0f);
            for (int i = 0; i < TargetSpawns.Length; i++)
            {
                var (name, pos, _) = TargetSpawns[i];
                battle.AddUnit(name, 1, MechChassis.Create(i % 2 == 0 ? ChassisClass.Medium : ChassisClass.Light), pos, Mathf.Pi);
            }
        }
        else
        {
            battle = new BattleSim(ArenaMap.Arena(ArenaSeed), ArenaSeed * 31u + _round);
            SpawnTeams(battle, mode);
        }
        Arena.Build(_session, battle.Map, ArenaSeed);

        var effects = new Effects { Name = "Effects" };
        _battlefield = new Battlefield { Name = "Battlefield", Effects = effects };
        effects.Battlefield = _battlefield;
        _session.AddChild(effects);
        _session.AddChild(_battlefield);
        _battlefield.Begin(battle, teamColors: mode != GameMode.Range);
        _player = mode is GameMode.Range or GameMode.Play1v1 or GameMode.Play2v2 ? _battlefield.Mechs[0] : null;

        _camera = new CameraRig { Name = "Camera", Mechs = _battlefield.Mechs };
        _session.AddChild(_camera);
        _camera.MakeCurrent();
        _camera.SetMode(_cameraOverride ?? (_player != null ? CameraMode.Chase : CameraMode.Overview));
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

        _gait = new GaitDebugDraw { Name = "GaitDebug", AllMechs = _battlefield.Mechs, Visible = _gaitDebug };
        _session.AddChild(_gait);
        _aiDraw = new AiDebugDraw { Name = "AiDebug", Battle = battle, Player = _player, ShowAi = _aiOverlay && Watching };
        _session.AddChild(_aiDraw);
        _hud = new Hud { Name = "Hud", Mech = _player, Camera = _camera, Pilot = pilot, Battlefield = _battlefield, HelpText = Watching ? WatchHelp : PlayHelp };
        _session.AddChild(_hud);
        _panel = new AiPanel { Name = "AiPanel", Battle = battle, Visible = _aiOverlay && Watching };
        _session.AddChild(_panel);

        Select(_player ?? _battlefield.Mechs[0]);
    }

    private void SpawnTeams(BattleSim b, GameMode mode)
    {
        // Alpha starts at +z facing -z, Bravo at -z facing +z (the prototype's spawns, mirrored into Godot's frame).
        void Ai(string name, int team, ChassisClass c, float x, float z, Personality p) =>
            b.AddUnit(name, team, MechChassis.Create(c), new Vec3(x, 0f, z), team == 0 ? 0f : Mathf.Pi,
                _personalities.GetValueOrDefault($"{mode}:{name}", p));
        void Player(float x, float z) => b.AddUnit("Player", 0, MechChassis.Create(_playerClass), new Vec3(x, 0f, z), 0f);

        const ChassisClass M = ChassisClass.Medium, L = ChassisClass.Light;
        const float z = ArenaMap.ArenaSpawnZ, z2 = z + 17f, x = 30f;
        switch (mode)
        {
            case GameMode.Watch1v1:
                Ai("Alpha-1", 0, M, 0f, z, Personality.Balanced);
                Ai("Bravo-1", 1, M, 0f, -z, Personality.Brawler);
                break;
            case GameMode.Watch2v2:
                Ai("Alpha-1", 0, M, x, z, Personality.Brawler);
                Ai("Alpha-2", 0, M, -x, z2, Personality.Sniper);
                Ai("Bravo-1", 1, M, -x, -z, Personality.Balanced);
                Ai("Bravo-2", 1, M, x, -z2, Personality.Sniper);
                break;
            case GameMode.WatchLights:
                Ai("Alpha-1", 0, L, x, z, Personality.Brawler);
                Ai("Alpha-2", 0, L, -x, z, Personality.Balanced);
                Ai("Bravo-1", 1, M, 0f, -z, Personality.Balanced);
                break;
            case GameMode.Play1v1:
                Player(0f, z);
                Ai("Bravo-1", 1, M, 0f, -z, Personality.Balanced);
                break;
            case GameMode.Play2v2:
                Player(x, z);
                Ai("Alpha-2", 0, M, -x, z2, Personality.Sniper);
                Ai("Bravo-1", 1, M, -x, -z, Personality.Balanced);
                Ai("Bravo-2", 1, M, x, -z2, Personality.Brawler);
                break;
        }
    }

    /// <summary>Scripted shooting for captures: cockpit kill, torso kill, arm and legs off.</summary>
    private void AddRangeDemo()
    {
        var t = _battlefield.Mechs;
        var demo = new RangeDemoPilot { Name = "RangeDemo", Mech = _player };
        demo.Tasks.Add((t[1], MechSection.Cockpit, 0b01u));
        demo.Tasks.Add((t[2], MechSection.Torso, 0b11u));
        demo.Tasks.Add((t[3], MechSection.ArmLeft, 0b10u));
        demo.Tasks.Add((t[3], MechSection.Legs, 0b01u));
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
        if (e.IsAction("cam_chase")) _camera.SetMode(CameraMode.Chase);
        else if (e.IsAction("cam_cockpit")) _camera.SetMode(CameraMode.Cockpit);
        else if (e.IsAction("cam_orbit")) _camera.SetMode(CameraMode.Orbit);
        else if (e.IsAction("cam_overview")) _camera.SetMode(CameraMode.Overview);
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
        else if (e.IsAction("mode_range")) StartSession(GameMode.Range);
        else if (e.IsAction("mode_watch_1v1")) StartSession(GameMode.Watch1v1);
        else if (e.IsAction("mode_watch_2v2")) StartSession(GameMode.Watch2v2);
        else if (e.IsAction("mode_watch_lights")) StartSession(GameMode.WatchLights);
        else if (e.IsAction("mode_play_1v1")) StartSession(GameMode.Play1v1);
        else if (e.IsAction("mode_play_2v2")) StartSession(GameMode.Play2v2);
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
