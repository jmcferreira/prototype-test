using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move: the caster can move to any hex reachable within range steps.
/// Uses flood fill to find all walkable hexes (no occupied, no off-grid).
/// </summary>
public class MoveResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardData data, Unit caster, Unit enemy, HexGrid grid)
    {
        var reachable = new HashSet<HexCoord>();
        var frontier = new Queue<(HexCoord coord, int steps)>();
        frontier.Enqueue((caster.Coord, 0));
        reachable.Add(caster.Coord);

        while (frontier.Count > 0)
        {
            var (current, steps) = frontier.Dequeue();
            if (steps >= data.range) continue;

            for (int dir = 0; dir < 6; dir++)
            {
                HexCoord next = current.Neighbor(dir);
                if (reachable.Contains(next)) continue;
                if (!grid.TryGetTile(next, out _)) continue;
                if (IsOccupied(next)) continue;

                reachable.Add(next);
                frontier.Enqueue((next, steps + 1));
            }
        }

        // Remove the caster's own hex — can't "move" to where you already are
        reachable.Remove(caster.Coord);
        return new List<HexCoord>(reachable);
    }

    public void Resolve(CardData data, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        Debug.Log($"Move: {caster.Team} moved to {target}.");
        caster.ForceMoveTo(target);
    }

    private static bool IsOccupied(HexCoord coord)
    {
        foreach (var unit in Object.FindObjectsOfType<Unit>())
        {
            if (unit.Coord == coord) return true;
        }
        return false;
    }
}
