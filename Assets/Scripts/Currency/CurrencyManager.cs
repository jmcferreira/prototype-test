using System;

/// <summary>
/// Tracks the player's Gold and XP currencies.
/// Static singleton so any system can add/read currencies.
/// </summary>
public static class CurrencyManager
{
    public static int Gold { get; private set; }
    public static int XP { get; private set; }

    /// <summary>Fired whenever Gold or XP changes.</summary>
    public static event Action OnChanged;

    /// <summary>
    /// Reset all currencies and subscribers. Call at start of each game session.
    /// </summary>
    public static void Clear()
    {
        Gold = 0;
        XP = 0;
        OnChanged = null;
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnChanged?.Invoke();
    }

    public static void AddXP(int amount)
    {
        if (amount <= 0) return;
        XP += amount;
        OnChanged?.Invoke();
    }
}
