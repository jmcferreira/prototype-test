/// <summary>
/// Which game mode is active. Set before GameSetup runs.
/// </summary>
public enum GameMode
{
    None,
    Campaign,
    PVP,
}

/// <summary>
/// Static state tracking the current game mode.
/// GameSetup reads this to decide between Campaign and PVP flows.
/// </summary>
public static class GameModeState
{
    public static GameMode CurrentMode { get; set; } = GameMode.None;
}
