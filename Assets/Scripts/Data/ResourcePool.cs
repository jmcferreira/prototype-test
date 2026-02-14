using UnityEngine;

/// <summary>
/// A unit's pool for a single resource type (e.g. 3/5 Mana).
/// Tracks current and max values with per-turn regeneration.
/// </summary>
[System.Serializable]
public class ResourcePool
{
    public ResourceType Type { get; }
    public int Current { get; private set; }
    public int Max { get; private set; }
    public int RegenPerTurn { get; }

    public ResourcePool(ResourceType type, int max, int regenPerTurn = 0)
    {
        Type = type;
        Max = max;
        Current = max;
        RegenPerTurn = regenPerTurn;
    }

    public bool CanAfford(int amount) => Current >= amount;

    public void Spend(int amount)
    {
        Current = Mathf.Max(0, Current - amount);
    }

    public void Regenerate()
    {
        if (RegenPerTurn > 0)
            Current = Mathf.Min(Max, Current + RegenPerTurn);
    }

    public void Restore(int amount)
    {
        Current = Mathf.Min(Max, Current + amount);
    }

    public void SetMax(int newMax)
    {
        Max = newMax;
        if (Current > Max)
            Current = Max;
    }
}
