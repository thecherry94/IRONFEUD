using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Keyboard/mouse pilot. Writes only <see cref="MechInput"/>, exactly like an AI pilot will.
/// W/S ramp a held throttle, X zeroes it, A/D turn the legs, the mouse position sets twist and aim.
/// LMB/Space hold the cannon trigger, RMB/F the laser. Weapons converge where the torso aim ray hits the world.
/// </summary>
public partial class PlayerMechController : Node
{
    public MechNode? Mech;
    public CameraRig? Camera;

    private float _throttle, _twistTarget, _aimTarget;

    private static readonly (string Action, Key Key)[] Bindings =
    {
        ("throttle_up", Key.W), ("throttle_down", Key.S), ("full_stop", Key.X),
        ("turn_left", Key.A), ("turn_right", Key.D), ("center_torso", Key.C),
        ("twist_left", Key.Left), ("twist_right", Key.Right), ("aim_up", Key.Up), ("aim_down", Key.Down),
        ("cam_chase", Key.Key1), ("cam_cockpit", Key.Key2), ("cam_orbit", Key.Key3),
        ("debug_draw", Key.F3), ("pause", Key.P), ("slow_motion", Key.O), ("release_mouse", Key.Escape),
        ("fire_cannon", Key.Space), ("fire_laser", Key.F),
    };

    private const float MaxConvergence = 400f, MinConvergence = 15f;

    /// <summary>Where the weapons converge this tick (world space), for the reticle.</summary>
    public Vector3 AimPoint { get; private set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ProcessPhysicsPriority = -100;
        foreach (var (action, key) in Bindings)
        {
            if (InputMap.HasAction(action)) continue;
            InputMap.AddAction(action);
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
        InputMap.ActionAddEvent("fire_cannon", new InputEventMouseButton { ButtonIndex = MouseButton.Left });
        InputMap.ActionAddEvent("fire_laser", new InputEventMouseButton { ButtonIndex = MouseButton.Right });
        Input.MouseMode = Input.MouseModeEnum.Confined;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && Camera?.Mode != CameraMode.Orbit && Input.MouseMode == Input.MouseModeEnum.Confined)
        {
            // Absolute mouse position maps to twist/aim targets, as in the prototype.
            var t = Mech?.Locomotion.Tuning;
            if (t == null) return;
            Vector2 size = GetViewport().GetVisibleRect().Size;
            float nx = motion.Position.X / size.X * 2f - 1f, ny = motion.Position.Y / size.Y * 2f - 1f;
            _twistTarget = Mathf.Clamp(-(Mathf.Abs(nx) < 0.04f ? 0f : nx) * t.TwistMax * 1.1f, -t.TwistMax, t.TwistMax);
            _aimTarget = Mathf.Clamp(-ny * 0.55f + 0.05f, t.AimMin, t.AimMax);
            return;
        }
        if (e is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Confined)
        {
            Input.MouseMode = Input.MouseModeEnum.Confined;
            return;
        }
        if (!e.IsPressed() || e.IsEcho()) return;

        if (e.IsAction("full_stop")) _throttle = 0f;
        else if (e.IsAction("center_torso")) _twistTarget = 0f;
        else if (e.IsAction("cam_chase")) Camera?.SetMode(CameraMode.Chase);
        else if (e.IsAction("cam_cockpit")) Camera?.SetMode(CameraMode.Cockpit);
        else if (e.IsAction("cam_orbit")) Camera?.SetMode(CameraMode.Orbit);
        else if (e.IsAction("pause")) GetTree().Paused = !GetTree().Paused;
        else if (e.IsAction("slow_motion")) Engine.TimeScale = Engine.TimeScale < 1.0 ? 1.0 : 0.25;
        else if (e.IsAction("release_mouse")) Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Mech == null || GetTree().Paused) return;
        var t = Mech.Locomotion.Tuning;
        float h = (float)delta;

        if (Input.IsActionPressed("throttle_up")) _throttle = Mathf.Min(1f, _throttle + 0.8f * h);
        if (Input.IsActionPressed("throttle_down")) _throttle = Mathf.Max(-1f, _throttle - 0.8f * h);
        float turn = Input.GetAxis("turn_right", "turn_left");
        _twistTarget = Mathf.Clamp(_twistTarget + Input.GetAxis("twist_right", "twist_left") * 1.4f * h, -t.TwistMax, t.TwistMax);
        _aimTarget = Mathf.Clamp(_aimTarget + Input.GetAxis("aim_down", "aim_up") * 0.5f * h, t.AimMin, t.AimMax);

        bool triggersLive = Camera?.Mode != CameraMode.Orbit && Input.MouseMode == Input.MouseModeEnum.Confined;
        uint fire = 0;
        if (triggersLive && Input.IsActionPressed("fire_cannon")) fire |= 1u << 0;
        if (triggersLive && Input.IsActionPressed("fire_laser")) fire |= 1u << 1;

        if (_autopilot)
        {
            // Scripted walk for unattended captures: accelerate, weave, stop, settle.
            _autopilotTime += h;
            _throttle = _autopilotTime < 14f ? Mathf.Min(1f, _autopilotTime * 0.25f) : 0f;
            turn = _autopilotTime is > 6f and < 11f ? Mathf.Sin((_autopilotTime - 6f) * 1.2f) : 0f;
            _twistTarget = 0.5f * Mathf.Sin(_autopilotTime * 0.4f);
            fire = _autopilotTime is > 3f and < 9f ? 0b11u : 0u;
        }

        // Converge on whatever the torso aim ray hits (last tick's pose; one tick of lag is invisible).
        var pose = Mech.Sim.Pose;
        Vector3 from = pose.AimOrigin.ToGodot(), dir = pose.AimDirection.ToGodot();
        var hit = Mech.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, from + dir * MaxConvergence));
        float convergence = hit.Count > 0 ? Mathf.Max(MinConvergence, from.DistanceTo((Vector3)hit["position"])) : MaxConvergence;
        AimPoint = from + dir * convergence;

        Mech.PilotInput = new MechInput
        {
            Throttle = _throttle,
            Turn = turn,
            TwistTarget = _twistTarget,
            AimTarget = _aimTarget,
            FireMask = fire,
            Convergence = convergence,
        };
    }

    private readonly bool _autopilot = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--autopilot") >= 0;
    private float _autopilotTime;

    public float Throttle => _throttle;
}
