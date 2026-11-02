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
        ("jump", Key.Space), ("release_mouse", Key.Escape),
        // Weapon groups (also LMB / RMB / MMB, and G / F / Q for 1-3); [ ] pick a weapon for group editing.
        ("fire_group_1", Key.Key1), ("fire_group_2", Key.Key2), ("fire_group_3", Key.Key3),
        ("fire_group_4", Key.Key4), ("fire_group_5", Key.Key5), ("fire_group_6", Key.Key6),
        ("weapon_prev", Key.Bracketleft), ("weapon_next", Key.Bracketright),
        // Cameras and inspection (1-4 pick a camera only when watching: piloting, they fire groups; V cycles).
        ("cam_chase", Key.Key1), ("cam_cockpit", Key.Key2), ("cam_orbit", Key.Key3), ("cam_overview", Key.Key4), ("cam_cycle", Key.V),
        ("select_next", Key.Tab), ("cycle_personality", Key.K),
        ("debug_draw", Key.F3), ("ai_overlay", Key.F4),
        // Session
        ("pause", Key.P), ("slow_motion", Key.O), ("restart", Key.R), ("cycle_chassis", Key.L), ("cycle_model", Key.M), ("toggle_terrain", Key.T),
        ("mode_range", Key.F5), ("mode_watch_1v1", Key.F6), ("mode_watch_2v2", Key.F7), ("mode_watch_lance", Key.F8),
        ("mode_play_1v1", Key.F9), ("mode_play_2v2", Key.F10), ("mode_parade", Key.F11),
    };

    public static void Ensure()
    {
        if (InputMap.HasAction("throttle_up")) return;
        foreach (var (action, key) in Keys)
        {
            InputMap.AddAction(action);
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
        InputMap.ActionAddEvent("fire_group_1", new InputEventMouseButton { ButtonIndex = MouseButton.Left });
        InputMap.ActionAddEvent("fire_group_2", new InputEventMouseButton { ButtonIndex = MouseButton.Right });
        InputMap.ActionAddEvent("fire_group_3", new InputEventMouseButton { ButtonIndex = MouseButton.Middle });
        InputMap.ActionAddEvent("fire_group_1", new InputEventKey { PhysicalKeycode = Key.G });
        InputMap.ActionAddEvent("fire_group_2", new InputEventKey { PhysicalKeycode = Key.F });
        InputMap.ActionAddEvent("fire_group_3", new InputEventKey { PhysicalKeycode = Key.Q });
    }
}
