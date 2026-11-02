using System.Collections.Generic;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;
using IronFeud.Mech;

namespace IronFeud.Game;

public enum CameraMode { Chase, Cockpit, Orbit, Overview }

/// <summary>
/// Chase (over the right shoulder, follows torso yaw), cockpit (head mount), orbit (around the target; drag / wheel)
/// and overview (frames every mech still fighting, drifts slowly around them; drag / wheel).
/// Runs after the battle tick and relies on physics interpolation for smooth rendering.
/// </summary>
public partial class CameraRig : Camera3D
{
    public CameraMode Mode { get; private set; } = CameraMode.Chase;

    /// <summary>Mechs the overview frames.</summary>
    public IReadOnlyList<MechNode>? Mechs;

    private MechNode? _target;
    private Vector3 _chasePos, _chaseLook, _overviewCenter;
    private bool _chaseInit, _overviewInit;
    private float _theta = Mathf.Pi + 1.2f, _phi = 1.3f, _radius = 27f;
    private float _ovTheta = 0.7f, _ovPhi = 0.95f, _ovZoom = 1f, _lastDrag = -99f, _clock;
    private bool _dragging;

    public MechNode? Target
    {
        get => _target;
        set
        {
            if (_target == value) return;
            if (IsInstanceValid(_target) && _target!.IsNodeReady())
            {
                _target.Rig.SetHeadHidden(false);
                _target.Tag.Visible = true;
            }
            _target = value;
            SetMode(Mode);
        }
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ProcessPhysicsPriority = 100;
        Near = 0.1f;
        Far = 1500f;
        SetMode(Mode);
    }

    /// <summary>Places the orbit camera (spherical angles around the target, radius in the target's size units).</summary>
    public void SetOrbit(float theta, float phi, float radius)
    {
        _theta = theta;
        _phi = phi;
        _radius = radius;
    }

    public void SetMode(CameraMode mode)
    {
        Mode = mode;
        Fov = mode == CameraMode.Cockpit ? 68f : mode == CameraMode.Overview ? 50f : 55f;
        bool cockpit = mode == CameraMode.Cockpit;
        CullMask = cockpit ? 0xFFFFFu & ~MechRig.HeadLayer : 0xFFFFFu;
        if (IsInstanceValid(_target) && _target!.IsNodeReady())
        {
            _target.Rig.SetHeadHidden(cockpit);
            _target.Tag.Visible = mode is CameraMode.Orbit or CameraMode.Overview; // no own tag in your face
        }
        _chaseInit = false;
        ResetPhysicsInterpolation();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (Mode is not (CameraMode.Orbit or CameraMode.Overview)) return;
        bool ov = Mode == CameraMode.Overview;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right } b:
                _dragging = b.Pressed;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                if (ov) _ovZoom = Mathf.Clamp(_ovZoom * 0.9f, 0.3f, 2.5f);
                else _radius = Mathf.Clamp(_radius * 0.9f, 8f, 90f);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                if (ov) _ovZoom = Mathf.Clamp(_ovZoom / 0.9f, 0.3f, 2.5f);
                else _radius = Mathf.Clamp(_radius / 0.9f, 8f, 90f);
                break;
            case InputEventMouseMotion m when _dragging && ov:
                _ovTheta -= m.Relative.X * 0.005f;
                _ovPhi = Mathf.Clamp(_ovPhi - m.Relative.Y * 0.004f, 0.2f, 1.45f);
                _lastDrag = _clock;
                break;
            case InputEventMouseMotion m when _dragging:
                _theta -= m.Relative.X * 0.006f;
                _phi = Mathf.Clamp(_phi - m.Relative.Y * 0.005f, 0.25f, 1.53f);
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _clock += dt;
        if (Mode == CameraMode.Overview)
        {
            Overview(dt);
            return;
        }
        if (!IsInstanceValid(_target) || _target!.Locomotion is not { } loco) return;
        var pose = loco.Pose;
        var body = loco.Body;
        Vector3 pelvis = pose.PelvisPosition.ToGodot();
        float k = loco.Chassis.Scale; // framing follows mech size

        switch (Mode)
        {
            case CameraMode.Orbit:
            {
                var c = new Vector3(pelvis.X, body.Position.Y + 5.2f * k, pelvis.Z);
                var offset = new Vector3(Mathf.Sin(_phi) * Mathf.Sin(_theta), Mathf.Cos(_phi), Mathf.Sin(_phi) * Mathf.Cos(_theta));
                LookAtFromPosition(c + offset * (_radius * k), c, Vector3.Up);
                break;
            }
            case CameraMode.Chase:
            {
                float dir = body.Psi + body.Twist + loco.Secondary.TorsoYaw.X * 0.5f;
                Vector3 fwd = MathUtil.Forward(dir).ToGodot(), right = MathUtil.Right(dir).ToGodot();
                Vector3 wantPos = new Vector3(pelvis.X, body.Position.Y, pelvis.Z) - (fwd * 21f - right * 3.5f - Vector3.Up * (13f - body.Aim * 6f)) * k;
                Vector3 wantLook = new Vector3(pelvis.X, body.Position.Y, pelvis.Z) + (fwd * 18f + right * 1.75f + Vector3.Up * (6f + body.Aim * 16f)) * k;
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
                GlobalTransform = _target.Rig.CamMount.GlobalTransform.Orthonormalized();
                break;
        }
    }

    /// <summary>The prototype's overview: centroid of the mechs still fighting, distance grows with their spread.</summary>
    private void Overview(float dt)
    {
        if (Mechs == null || Mechs.Count == 0) return;
        Vector3 sum = Vector3.Zero;
        int n = 0;
        bool anyAlive = false;
        foreach (var m in Mechs) anyAlive |= !m.Sim.IsOut;
        foreach (var m in Mechs)
        {
            if (anyAlive && m.Sim.IsOut) continue;
            sum += m.GlobalPosition;
            n++;
        }
        Vector3 c = sum / n;
        float sep = 0f;
        foreach (var a in Mechs)
        foreach (var b in Mechs)
            if (!anyAlive || !a.Sim.IsOut && !b.Sim.IsOut) sep = Mathf.Max(sep, a.GlobalPosition.DistanceTo(b.GlobalPosition));

        if (!_overviewInit)
        {
            _overviewCenter = c;
            _overviewInit = true;
        }
        _overviewCenter = _overviewCenter.Lerp(c, 1f - Mathf.Exp(-dt * 1.5f));
        float r = Mathf.Clamp(sep * 0.85f + 45f, 55f, 420f) * _ovZoom;
        if (_clock - _lastDrag > 5f) _ovTheta += dt * 0.04f;
        var look = _overviewCenter + Vector3.Up * 4f;
        var offset = new Vector3(Mathf.Sin(_ovPhi) * Mathf.Sin(_ovTheta), Mathf.Cos(_ovPhi), Mathf.Sin(_ovPhi) * Mathf.Cos(_ovTheta));
        LookAtFromPosition(look + offset * r, look, Vector3.Up);
    }
}
