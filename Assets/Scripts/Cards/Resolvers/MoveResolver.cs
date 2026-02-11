using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move: the caster can move to any hex reachable within range steps.
/// Uses flood fill to find all walkable hexes (no occupied, no off-grid).
/// Swift status adds bonus range.
/// </summary>
public class MoveResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        // Root prevents all movement
        if (caster.HasStatus(StatusEffectType.Root))
            return new List<HexCoord>();

        int bonusRange = caster.GetStatusStacks(StatusEffectType.Swift);
        int totalRange = action.range + bonusRange;

        var reachable = new HashSet<HexCoord>();
        var frontier = new Queue<(HexCoord coord, int steps)>();
        frontier.Enqueue((caster.Coord, 0));
        reachable.Add(caster.Coord);

        while (frontier.Count > 0)
        {
            var (current, steps) = frontier.Dequeue();
            if (steps >= totalRange) continue;

            for (int dir = 0; dir < 6; dir++)
            {
                HexCoord next = current.Neighbor(dir);
                if (reachable.Contains(next)) continue;
                if (!grid.TryGetTile(next, out _)) continue;
                if (IsOccupied(next, allUnits)) continue;

                reachable.Add(next);
                frontier.Enqueue((next, steps + 1));
            }
        }

        // Remove the caster's own hex — can't "move" to where you already are
        reachable.Remove(caster.Coord);
        return new List<HexCoord>(reachable);
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        Debug.Log($"Move: {caster.DisplayName} moved to {target}.");
        caster.ForceMoveTo(target);
        caster.NotifyMoved();
        TokenManager.Instance?.OnUnitEnterHex(caster, target, allUnits);
    }

    private static bool IsOccupied(HexCoord coord, List<Unit> allUnits)
    {
        foreach (var unit in allUnits)
        {
            if (unit.IsAlive && unit.Coord == coord) return true;
        }
        return false;
    }
}
