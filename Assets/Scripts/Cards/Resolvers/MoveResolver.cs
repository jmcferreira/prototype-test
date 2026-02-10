using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move: the caster dashes up to range hexes in a straight line.
/// Valid targets are the farthest reachable hex in each of the 6 directions.
/// </summary>
public class MoveResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardData data, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        for (int dir = 0; dir < 6; dir++)
        {
            HexCoord dest = WalkDirection(caster.Coord, dir, data.range, grid);
            if (dest != caster.Coord)
                targets.Add(dest);
        }

        return targets;
    }

    public void Resolve(CardData data, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        Debug.Log($"Move: {caster.Team} dashed to {target}.");
        caster.ForceMoveTo(target);
    }

    private static HexCoord WalkDirection(HexCoord origin, int direction, int range, HexGrid grid)
    {
        HexCoord current = origin;
        for (int i = 0; i < range; i++)
        {
            HexCoord next = current.Neighbor(direction);
            if (!grid.TryGetTile(next, out _)) break;
            if (IsOccupied(next)) break;
            current = next;
        }
        return current;
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
