using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Weapons;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Keyboard/mouse pilot. Writes only <see cref="MechInput"/>, exactly like an AI pilot will.
/// W/S ramp a held throttle, X zeroes it, A/D turn the legs, the mouse position sets twist and aim.
/// Weapon groups: 1-6 (LMB / RMB / MMB = 1 / 2 / 3) hold a group's trigger; [ ] pick a weapon, Shift+N puts it in or
/// takes it out of group N, Ctrl+N switches group N between alpha and chain fire. Weapons converge where the torso aim
/// ray hits the world.
/// </summary>
public partial class PlayerMechController : Node
{
    public MechNode? Mech;
    public CameraRig? Camera;
    public Battlefield? Battlefield;

    private float _throttle, _twistTarget, _aimTarget;

    /// <summary>The pilot's weapon groups (defaults from the loadout).</summary>
    public WeaponGroups Groups { get; private set; } = new();

    /// <summary>The weapon picked for group editing (index into the mounts).</summary>
    public int SelectedWeapon { get; private set; }

    private const float MaxConvergence = 800f, MinConvergence = 15f;

    /// <summary>Where the weapons converge this tick (world space), for the reticle.</summary>
    public Vector3 AimPoint => AimHit.Point;

    /// <summary>What the torso aim ray is on: geometry, or a mech section.</summary>
    public BattleHit AimHit { get; private set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ProcessPhysicsPriority = -100;
        GameInput.Ensure();
        Input.MouseMode = Input.MouseModeEnum.Confined;
        if (Mech != null) Groups = WeaponGroups.Default(Mech.Sim.Weapons);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && FlyingView && Input.MouseMode == Input.MouseModeEnum.Confined)
        {
            // Absolute mouse position maps to twist/aim targets, as in the prototype.
            var t = Mech?.Locomotion.Chassis;
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

        int weapons = Mech?.Sim.Weapons.Mounts.Count ?? 0;
        if (e is InputEventKey { PhysicalKeycode: >= Key.Key1 and <= Key.Key6 } key && (key.ShiftPressed || key.CtrlPressed) && weapons > 0)
        {
            int group = (int)(key.PhysicalKeycode - Key.Key1);
            if (key.CtrlPressed) Groups.ToggleChain(group);
            else Groups.Toggle(group, SelectedWeapon);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e.IsAction("weapon_next") && weapons > 0) SelectedWeapon = (SelectedWeapon + 1) % weapons;
        else if (e.IsAction("weapon_prev") && weapons > 0) SelectedWeapon = (SelectedWeapon + weapons - 1) % weapons;
        else if (e.IsAction("full_stop")) _throttle = 0f;
        else if (e.IsAction("center_torso")) _twistTarget = 0f;
        else if (e.IsAction("release_mouse")) Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Mech == null || GetTree().Paused || Mech.Sim.IsOut) return;
        var t = Mech.Locomotion.Chassis;
        float h = (float)delta;

        if (Input.IsActionPressed("throttle_up")) _throttle = Mathf.Min(1f, _throttle + 0.8f * h);
        if (Input.IsActionPressed("throttle_down")) _throttle = Mathf.Max(-1f, _throttle - 0.8f * h);
        float turn = Input.GetAxis("turn_right", "turn_left");
        _twistTarget = Mathf.Clamp(_twistTarget + Input.GetAxis("twist_right", "twist_left") * 1.4f * h, -t.TwistMax, t.TwistMax);
        _aimTarget = Mathf.Clamp(_aimTarget + Input.GetAxis("aim_down", "aim_up") * 0.5f * h, t.AimMin, t.AimMax);

        bool triggersLive = FlyingView && Input.MouseMode == Input.MouseModeEnum.Confined;
        bool jump = Input.IsActionPressed("jump");
        // Held group triggers (not while Shift or Ctrl edits the groups).
        uint held = 0;
        if (triggersLive && !Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Ctrl))
            for (int g = 0; g < WeaponGroups.Count; g++)
                if (Input.IsActionPressed($"fire_group_{g + 1}")) held |= 1u << g;
        uint fire = Groups.FireMask(held, Mech.Sim.Weapons, h);

        if (_autopilot)
        {
            // Scripted walk for unattended captures: accelerate, weave, stop, settle.
            _autopilotTime += h;
            _throttle = _autopilotTime < 14f ? Mathf.Min(1f, _autopilotTime * 0.25f) : 0f;
            turn = _autopilotTime is > 6f and < 11f ? Mathf.Sin((_autopilotTime - 6f) * 1.2f) : 0f;
            _twistTarget = 0.5f * Mathf.Sin(_autopilotTime * 0.4f);
            fire = _autopilotTime is > 3f and < 9f ? (1u << Mech.Sim.Weapons.Mounts.Count) - 1u : 0u;
            // A standing hop at 2 s, a running jump at 7 s and a long burn at 12 s (if the chassis has jets).
            jump = _autopilotTime is > 2f and < 2.4f or > 7f and < 7.7f or > 12f and < 13.2f;
        }

        // Converge on whatever the torso aim ray hits, mechs included (last tick's pose; one tick of lag is invisible).
        var pose = Mech.Sim.Pose;
        Vector3 from = pose.AimOrigin.ToGodot(), dir = pose.AimDirection.ToGodot();
        BattleHit aim;
        if (Battlefield != null) Battlefield.Raycast(from, dir, MaxConvergence, Mech, out aim);
        else aim = new BattleHit { Point = from + dir * MaxConvergence, Distance = MaxConvergence };
        AimHit = aim;
        float convergence = Mathf.Max(MinConvergence, aim.Distance);

        Mech.PilotInput = new MechInput
        {
            Throttle = _throttle,
            Turn = turn,
            TwistTarget = _twistTarget,
            AimTarget = _aimTarget,
            FireMask = fire,
            Convergence = convergence,
            Jump = jump,
        };

        // Missile lock: whatever enemy mech is under the reticle, kept for 2 s after it slips off.
        if (aim.Mech is { } under && under.Unit.Team != Mech.Unit.Team && !under.Sim.IsOut)
        {
            _lockCandidate = under;
            _lockSeen = 0f;
        }
        else if ((_lockSeen += h) > 2f) _lockCandidate = null;
        Mech.Unit.LockCandidate = _lockCandidate?.Unit;
    }

    private MechNode? _lockCandidate;
    private float _lockSeen;

    private readonly bool _autopilot = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--autopilot") >= 0;
    private float _autopilotTime;

    public float Throttle => _throttle;

    /// <summary>Mouse and triggers fly the mech only from the chase and cockpit views of this mech.</summary>
    private bool FlyingView => Camera == null || Camera.Mode is CameraMode.Chase or CameraMode.Cockpit && Camera.Target == Mech;
}
