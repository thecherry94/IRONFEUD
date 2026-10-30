using Godot;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>Walker test bed: arena, one player-piloted mech, weapons, cameras, HUD and gait debug overlay.</summary>
public partial class Main : Node3D
{
    [Export] public uint ArenaSeed { get; set; } = 1337;

    public override void _Ready()
    {
        Arena.Build(this, ArenaSeed);

        var effects = new Effects { Name = "Effects" };
        AddChild(effects);
        var projectiles = new Projectiles { Name = "Projectiles", Effects = effects };
        AddChild(projectiles);

        var mech = new MechNode { Name = "PlayerMech" };
        AddChild(mech);
        mech.Footfall += f => effects.Footfall(f);
        mech.ShotFired += s => projectiles.Fire(s);

        var camera = new CameraRig { Name = "Camera", Target = mech };
        AddChild(camera);
        camera.MakeCurrent();
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--camera=orbit") camera.SetMode(CameraMode.Orbit);
            else if (arg == "--camera=cockpit") camera.SetMode(CameraMode.Cockpit);
        }

        var pilot = new PlayerMechController { Name = "Pilot", Mech = mech, Camera = camera };
        AddChild(pilot);

        var debug = new GaitDebugDraw { Name = "GaitDebug", Mech = mech, Visible = false };
        AddChild(debug);

        AddChild(new Hud { Name = "Hud", Mech = mech, Camera = camera, Pilot = pilot, Debug = debug });
    }
}
