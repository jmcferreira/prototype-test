using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dash: move in a straight line up to range hexes in one of 6 directions.
/// Returns all reachable hexes along each direction (stops at edge/occupied).
/// </summary>
public class DashResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        for (int dir = 0; dir < 6; dir++)
        {
            HexCoord current = caster.Coord;
            for (int step = 0; step < action.range; step++)
            {
                HexCoord next = current.Neighbor(dir);
                if (!grid.TryGetTile(next, out _)) break;
                if (IsOccupied(next)) break;
                targets.Add(next);
                current = next;
            }
        }

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        Debug.Log($"Dash: {caster.Team} dashed to {target}.");
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
