using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;
using IronFeud.Mech;

namespace IronFeud.Game;

public enum CameraMode { Chase, Cockpit, Orbit }

/// <summary>
/// Chase (over the right shoulder, follows torso yaw), cockpit (head mount) and orbit (mouse drag / wheel) views.
/// Runs after the mech's physics tick and relies on physics interpolation for smooth rendering.
/// </summary>
public partial class CameraRig : Camera3D
{
    public MechNode? Target;
    public CameraMode Mode { get; private set; } = CameraMode.Chase;

    private Vector3 _chasePos, _chaseLook;
    private bool _chaseInit;
    private float _theta = Mathf.Pi + 1.2f, _phi = 1.3f, _radius = 27f;
    private bool _dragging;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ProcessPhysicsPriority = 100;
        Near = 0.1f;
        Far = 600f;
        SetMode(CameraMode.Chase);
    }

    public void SetMode(CameraMode mode)
    {
        Mode = mode;
        Fov = mode == CameraMode.Cockpit ? 68f : 55f;
        CullMask = mode == CameraMode.Cockpit ? 0xFFFFFu & ~MechRig.HeadLayer : 0xFFFFFu;
        _chaseInit = false;
        ResetPhysicsInterpolation();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (Mode != CameraMode.Orbit) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right } b:
                _dragging = b.Pressed;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                _radius = Mathf.Clamp(_radius * 0.9f, 8f, 90f);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                _radius = Mathf.Clamp(_radius / 0.9f, 8f, 90f);
                break;
            case InputEventMouseMotion m when _dragging:
                _theta -= m.Relative.X * 0.006f;
                _phi = Mathf.Clamp(_phi - m.Relative.Y * 0.005f, 0.25f, 1.53f);
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Target?.Locomotion is not { } loco) return;
        float dt = (float)delta;
        var pose = loco.Pose;
        var body = loco.Body;
        Vector3 pelvis = pose.PelvisPosition.ToGodot();

        switch (Mode)
        {
            case CameraMode.Orbit:
            {
                var c = new Vector3(pelvis.X, 5.2f, pelvis.Z);
                var offset = new Vector3(Mathf.Sin(_phi) * Mathf.Sin(_theta), Mathf.Cos(_phi), Mathf.Sin(_phi) * Mathf.Cos(_theta));
                LookAtFromPosition(c + offset * _radius, c, Vector3.Up);
                break;
            }
            case CameraMode.Chase:
            {
                float dir = body.Psi + body.Twist + loco.Secondary.TorsoYaw.X * 0.5f;
                Vector3 fwd = MathUtil.Forward(dir).ToGodot(), right = MathUtil.Right(dir).ToGodot();
                Vector3 wantPos = new Vector3(pelvis.X, 0, pelvis.Z) - fwd * 21f + right * 3.5f + Vector3.Up * (13f - body.Aim * 6f);
                Vector3 wantLook = new Vector3(pelvis.X, 0, pelvis.Z) + fwd * 18f + right * 1.75f + Vector3.Up * (6f + body.Aim * 16f);
                if (!_chaseInit)
                {
                    _chasePos = wantPos;
                    _chaseLook = wantLook;
                    _chaseInit = true;
                }
                _chasePos = _chasePos.Lerp(wantPos, 1f - Mathf.Exp(-dt * 4f));
                _chaseLook = _chaseLook.Lerp(wantLook, 1f - Mathf.Exp(-dt * 6f));
                LookAtFromPosition(_chasePos + Vector3.Up * (0.35f * pose.Jank), _chaseLook, Vector3.Up);
                RotateObjectLocal(Vector3.Back, 0.006f * pose.Jank);
                break;
            }
            case CameraMode.Cockpit:
                GlobalTransform = Target.Rig.CamMount.GlobalTransform;
                break;
        }
    }
}
