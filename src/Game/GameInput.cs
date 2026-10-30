using Godot;

namespace IronFeud.Game;

/// <summary>Input actions for the test bed, registered in code so the project file stays untouched.</summary>
public static class GameInput
{
    private static readonly (string Action, Key Key)[] Keys =
    {
        // Piloting
        ("throttle_up", Key.W), ("throttle_down", Key.S), ("full_stop", Key.X),
        ("turn_left", Key.A), ("turn_right", Key.D), ("center_torso", Key.C),
        ("twist_left", Key.Left), ("twist_right", Key.Right), ("aim_up", Key.Up), ("aim_down", Key.Down),
        ("fire_cannon", Key.Space), ("fire_laser", Key.F), ("release_mouse", Key.Escape),
        // Cameras and inspection
        ("cam_chase", Key.Key1), ("cam_cockpit", Key.Key2), ("cam_orbit", Key.Key3), ("cam_overview", Key.Key4),
        ("select_next", Key.Tab), ("cycle_personality", Key.K),
        ("debug_draw", Key.F3), ("ai_overlay", Key.F4),
        // Session
        ("pause", Key.P), ("slow_motion", Key.O), ("restart", Key.R), ("toggle_light", Key.L),
        ("mode_range", Key.F5), ("mode_watch_1v1", Key.F6), ("mode_watch_2v2", Key.F7), ("mode_watch_lights", Key.F8),
        ("mode_play_1v1", Key.F9), ("mode_play_2v2", Key.F10),
    };

    public static void Ensure()
    {
        if (InputMap.HasAction("throttle_up")) return;
        foreach (var (action, key) in Keys)
        {
            InputMap.AddAction(action);
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
        InputMap.ActionAddEvent("fire_cannon", new InputEventMouseButton { ButtonIndex = MouseButton.Left });
        InputMap.ActionAddEvent("fire_laser", new InputEventMouseButton { ButtonIndex = MouseButton.Right });
    }
}
