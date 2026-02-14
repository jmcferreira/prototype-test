using System.Collections.Generic;

/// <summary>
/// Runtime state for a PVP match. Tracks per-player gold, faction,
/// creatures, round number, and tower references.
/// </summary>
public class PvpMatchState
{
    public static PvpMatchState Current { get; set; }

    public PvpArenaDef Arena { get; }
    public int RoundNumber { get; set; } = 1;

    // Per-player state (index 0 = P1, index 1 = P2)
    public string[] FactionIds { get; } = new string[2];
    public int[] Gold { get; } = new int[2];
    public List<Unit>[] Creatures { get; } = { new List<Unit>(), new List<Unit>() };

    public PvpMatchState(PvpArenaDef arena)
    {
        Arena = arena;
        Gold[0] = arena.startingGold;
        Gold[1] = arena.startingGold;
    }

    public void AddGold(int playerIndex, int amount)
    {
        Gold[playerIndex] += amount;
    }

    public bool SpendGold(int playerIndex, int amount)
    {
        if (Gold[playerIndex] < amount) return false;
        Gold[playerIndex] -= amount;
        return true;
    }

    public static void NewMatch(PvpArenaDef arena)
    {
        Current = new PvpMatchState(arena);
    }
}
