using System;
using System.Numerics;
using IronFeud.Logic.Mech;

namespace IronFeud.Tests;

/// <summary>Arm weapons aim within a cone, faster than the torso twists; torso weapons fire along the torso.</summary>
public class ArmAimTests
{
    private const float H = 1f / 120f;

    private static float Yaw(Vector3 dir) => MathF.Atan2(-dir.X, -dir.Z);

    [Fact]
    public void Arms_LeadTheTorso_WithinTheirCone_ThenCentre()
    {
        var sim = new MechSim(MechChassis.Medium());
        var c = sim.Chassis;
        var input = new MechInput { TwistTarget = 1f };
        float maxArm = 0f;
        for (int i = 0; i < 12; i++) sim.Step(input, H); // 0.1 s
        var body = sim.Locomotion.Body;
        Assert.True(body.ArmYaw > 0.5f * body.Twist, $"arms {body.ArmYaw:0.00} vs torso {body.Twist:0.00}");
        Assert.True(Yaw(sim.Pose.ArmAimDirection) > Yaw(sim.Pose.AimDirection) + 0.05f, "the arm ray leads");
        for (int i = 0; i < 240; i++)
        {
            sim.Step(input, H);
            maxArm = MathF.Max(maxArm, MathF.Abs(sim.Locomotion.Body.ArmYaw));
        }
        Assert.True(maxArm <= c.ArmYawMax + 1e-4f, $"arm yaw reached {maxArm:0.000}");
        Assert.Equal(1f, sim.Locomotion.Body.Twist, 3);
        Assert.Equal(0f, sim.Locomotion.Body.ArmYaw, 3); // the torso caught up: the arms are centred again
    }

    [Fact]
    public void ArmWeapons_FireAlongTheArms_TorsoWeapons_AlongTheTorso()
    {
        var c = MechChassis.Medium(); // heavy cannon right arm (0), laser left arm (1), strike rack on the shoulder (2)
        c.HeatDissipation = 10f;
        var sim = new MechSim(c);
        var input = new MechInput { TwistTarget = 1.2f, Convergence = 300f };
        for (int i = 0; i < 18; i++) sim.Step(input, H); // mid-swing
        input.FireMask = 0b101u;
        sim.Step(input, H);
        float arm = Yaw(sim.Pose.ArmAimDirection), torso = Yaw(sim.Pose.AimDirection);
        Assert.True(arm > torso + 0.1f);
        foreach (var shot in sim.Shots)
        {
            float yaw = Yaw(shot.Direction);
            if (shot.Weapon == 0) Assert.True(MathF.Abs(yaw - arm) < MathF.Abs(yaw - torso), "the cannon follows the arm");
            if (shot.Weapon == 2) Assert.True(MathF.Abs(yaw - torso) < MathF.Abs(yaw - arm), "the rack follows the torso");
        }
        Assert.Equal(2, sim.Shots.Count);
    }
}
