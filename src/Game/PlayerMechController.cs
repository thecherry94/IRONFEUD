using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Weapons;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Keyboard/mouse pilot. Writes only <see cref="MechInput"/>, exactly like an AI pilot will.
/// W/S ramp a held throttle, X zeroes it, A/D turn the legs.
/// Aiming (the genre's scheme): the mouse turns the pilot's aim direction (relative motion, captured cursor); the chase
/// and cockpit cameras look exactly along it, so the aim point stays at the centre of the screen and never jumps. The
/// torso and arms then turn toward the point it lands on at their own rates; the HUD shows where the guns really point.
/// The aim stays fixed in the world while the legs turn under it, and can't swing past what twist plus the arm cone reach.
/// Weapon groups: 1-6 (LMB / RMB / MMB = 1 / 2 / 3) hold a group's trigger; [ ] pick a weapon, Shift+N puts it in or
/// takes it out of group N, Ctrl+N switches group N between alpha and chain fire.
/// </summary>
public partial class PlayerMechController : Node
{
    public MechNode? Mech;
    public CameraRig? Camera;
    public Battlefield? Battlefield;

    private float _throttle, _twistTarget, _aimTarget;

    /// <summary>The pilot's aim: world yaw (+ = left) and pitch (+ = up), radians; NaN until the first tick.</summary>
    private float _aimYaw = float.NaN, _aimPitch;

    /// <summary>Distance to the aim point, smoothed (a ray sliding off a mech onto far ground doesn't snap the guns).</summary>
    private float _aimDistance = 200f;

    private const float MouseRadPerPixel = 0.0022f;

    /// <summary>The pilot's weapon groups (defaults from the loadout).</summary>
    public WeaponGroups Groups { get; private set; } = new();

    /// <summary>The weapon picked for group editing (index into the mounts).</summary>
    public int SelectedWeapon { get; private set; }

    private const float MaxConvergence = 800f, MinConvergence = 15f;

    /// <summary>What the pilot's aim is on (centre of the screen): geometry, or a mech section.</summary>
    public BattleHit AimHit { get; private set; }

    /// <summary>The pilot's aim direction (world): the cameras look along it.</summary>
    public Vector3 AimDirection => Dir(float.IsNaN(_aimYaw) ? 0f : _aimYaw, _aimPitch);

    /// <summary>The pilot's aim point (smoothed distance): the centre of the screen in the chase and cockpit views.</summary>
    public Vector3 PilotAimPoint { get; private set; }

    /// <summary>Where the torso weapons point (on their ray, at the aim distance): the torso reticle.</summary>
    public Vector3 TorsoAimPoint { get; private set; }

    /// <summary>Where the arm weapons point: the arm reticle.</summary>
    public Vector3 ArmAimPoint { get; private set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ProcessPhysicsPriority = -100;
        GameInput.Ensure();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (Mech != null) Groups = WeaponGroups.Default(Mech.Sim.Weapons);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && FlyingView && Input.MouseMode == Input.MouseModeEnum.Captured && !float.IsNaN(_aimYaw) && !_autopilot)
        {
            // Capturing the cursor can report one huge jump (the pointer warped to the window centre): not a hand.
            if (motion.Relative.LengthSquared() > 250f * 250f) return;
            _aimYaw -= motion.Relative.X * MouseRadPerPixel;
            _aimPitch -= motion.Relative.Y * MouseRadPerPixel;
            return;
        }
        if (e is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
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
        else if (e.IsAction("center_torso") && Mech != null) _aimYaw = Mech.Locomotion.Body.Psi;
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
        var body = Mech.Locomotion.Body;
        if (float.IsNaN(_aimYaw))
        {
            _aimYaw = body.Psi + body.Twist;
            _aimPitch = body.Aim;
        }
        _aimYaw += Input.GetAxis("twist_right", "twist_left") * 1.4f * h;
        _aimPitch += Input.GetAxis("aim_down", "aim_up") * 0.5f * h;
        // The aim can't swing past what the torso twist plus the arm cone reach; past that it rides along with the legs.
        float reach = t.TwistMax + t.ArmYawMax, rel = Mathf.Wrap(_aimYaw - body.Psi, -Mathf.Pi, Mathf.Pi);
        if (Mathf.Abs(rel) > reach) _aimYaw = body.Psi + Mathf.Sign(rel) * reach;
        _aimPitch = Mathf.Clamp(_aimPitch, t.AimMin - t.ArmPitchMax, t.AimMax + t.ArmPitchMax);

        bool triggersLive = FlyingView && Input.MouseMode == Input.MouseModeEnum.Captured;
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

        // The aim point: along the pilot's aim from the camera when it is flying this mech (so it sits at the centre of
        // the screen), else from the mech's aim origin. Mechs included; last tick's pose (one tick of lag is invisible).
        var pose = Mech.Sim.Pose;
        Vector3 origin = pose.AimOrigin.ToGodot();
        Vector3 eye = FlyingView && Camera != null ? Camera.GlobalPosition : origin, dir = AimDirection;
        var aim = AimHit = Cast(eye, dir);
        _aimDistance = Mathf.Lerp(_aimDistance, aim.Distance, 1f - Mathf.Exp(-8f * h));
        Vector3 aimPoint = eye + dir * _aimDistance, toAim = aimPoint - origin;
        PilotAimPoint = aimPoint;
        float convergence = Mathf.Clamp(toAim.Length(), MinConvergence, MaxConvergence);
        if (!_autopilot)
        {
            // Torso and arms turn toward the aim point (the targets may lie past the torso's twist: the arms reach on).
            Vector3 want = toAim.Normalized();
            _twistTarget = Mathf.Wrap(Mathf.Atan2(-want.X, -want.Z) - body.Psi, -Mathf.Pi, Mathf.Pi);
            _aimTarget = Mathf.Asin(Mathf.Clamp(want.Y, -1f, 1f));
        }
        TorsoAimPoint = origin + pose.AimDirection.ToGodot() * convergence;
        ArmAimPoint = origin + pose.ArmAimDirection.ToGodot() * convergence;

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

    private static Vector3 Dir(float yaw, float pitch) =>
        new Vector3(-Mathf.Sin(yaw), 0f, -Mathf.Cos(yaw)) * Mathf.Cos(pitch) + Vector3.Up * Mathf.Sin(pitch);

    private BattleHit Cast(Vector3 from, Vector3 dir)
    {
        if (Battlefield != null && Battlefield.Raycast(from, dir, MaxConvergence, Mech, out var hit)) return hit;
        return new BattleHit { Point = from + dir * MaxConvergence, Distance = MaxConvergence };
    }

    private MechNode? _lockCandidate;
    private float _lockSeen;

    private readonly bool _autopilot = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--autopilot") >= 0;
    private float _autopilotTime;

    public float Throttle => _throttle;

    /// <summary>Mouse and triggers fly the mech only from the chase and cockpit views of this mech.</summary>
    private bool FlyingView => Camera == null || Camera.Mode is CameraMode.Chase or CameraMode.Cockpit && Camera.Target == Mech;
}
