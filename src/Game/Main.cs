using System.Collections.Generic;
using Godot;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Test bed: arena with an open firing range, the player's mech, target mechs (R respawns them), weapons, cameras,
/// HUD and the debug overlay.
/// </summary>
public partial class Main : Node3D
{
    [Export] public uint ArenaSeed { get; set; } = 1337;

    private static readonly Rect2 FiringRange = new(new Vector2(-45f, -170f), new Vector2(90f, 165f));

    private static readonly (string Name, Vector3 Position, bool Circle)[] TargetSpawns =
    {
        ("Target A", new Vector3(0f, 0f, -60f), false),
        ("Target B", new Vector3(-22f, 0f, -95f), false),
        ("Target C", new Vector3(24f, 0f, -80f), false),
        ("Target D", new Vector3(10f, 0f, -135f), true),
    };

    private readonly Battlefield _battlefield = new();
    private readonly List<MechNode> _targets = new();
    private Effects _effects = null!;
    private Projectiles _projectiles = null!;

    public override void _Ready()
    {
        Arena.Build(this, ArenaSeed, FiringRange);

        _effects = new Effects { Name = "Effects", Battlefield = _battlefield };
        AddChild(_effects);
        _projectiles = new Projectiles { Name = "Projectiles", Battlefield = _battlefield, Effects = _effects };
        AddChild(_projectiles);
        _battlefield.Damaged += (mech, report, at, _) => _effects.OnDamaged(mech, report, at);

        var player = SpawnMech("Player", Vector3.Zero, 0f, seed: 1);

        var camera = new CameraRig { Name = "Camera", Target = player };
        AddChild(camera);
        camera.MakeCurrent();

        var pilot = new PlayerMechController { Name = "Pilot", Mech = player, Camera = camera, Battlefield = _battlefield };
        AddChild(pilot);

        SpawnTargets();

        var debug = new GaitDebugDraw { Name = "GaitDebug", Mech = player, AllMechs = _battlefield.Mechs, Visible = false };
        AddChild(debug);

        AddChild(new Hud { Name = "Hud", Mech = player, Camera = camera, Pilot = pilot, Debug = debug, Battlefield = _battlefield });

        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--camera=orbit") camera.SetMode(CameraMode.Orbit);
            else if (arg == "--camera=cockpit") camera.SetMode(CameraMode.Cockpit);
            else if (arg == "--range-demo") AddRangeDemo(player);
            else if (arg == "--debug") debug.Visible = true;
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.R }) SpawnTargets();
    }

    private MechNode SpawnMech(string name, Vector3 position, float heading, uint seed)
    {
        var mech = new MechNode { Name = name, Position = position, Rotation = new Vector3(0f, heading, 0f), Seed = seed };
        AddChild(mech);
        _battlefield.Add(mech);
        mech.Footfall += f => _effects.Footfall(f);
        mech.ShotFired += s => _projectiles.Fire(mech, s);
        return mech;
    }

    private void SpawnTargets()
    {
        foreach (var t in _targets)
        {
            _battlefield.Remove(t);
            t.QueueFree();
        }
        _targets.Clear();

        uint seed = 100;
        foreach (var (name, pos, circle) in TargetSpawns)
        {
            var mech = SpawnMech(name, pos, Mathf.Pi, seed++); // facing the player
            _targets.Add(mech);
            var dummy = new TargetDummy { Mech = mech, Circle = circle };
            mech.AddChild(dummy);
        }
    }

    /// <summary>Scripted shooting for captures: cockpit kill, torso kill, arm and legs off.</summary>
    private void AddRangeDemo(MechNode player)
    {
        var demo = new RangeDemoPilot { Name = "RangeDemo", Mech = player };
        demo.Tasks.Add((_targets[0], MechSection.Cockpit, 0b01u));
        demo.Tasks.Add((_targets[1], MechSection.Torso, 0b11u));
        demo.Tasks.Add((_targets[2], MechSection.ArmLeft, 0b10u));
        demo.Tasks.Add((_targets[2], MechSection.Legs, 0b01u));
        AddChild(demo);
    }
}
