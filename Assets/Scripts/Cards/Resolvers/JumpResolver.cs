using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jump: move to any hex within range, ignoring obstacles in the path.
/// Only the destination must be unoccupied (not intermediate hexes).
/// Swift status adds bonus range.
/// </summary>
public class JumpResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        // Root prevents all movement
        if (caster.HasStatus(StatusEffectType.Root))
            return new List<HexCoord>();

        int bonusRange = caster.GetStatusStacks(StatusEffectType.Swift);
        int totalRange = action.range + bonusRange;

        var targets = new List<HexCoord>();

        foreach (var kvp in grid.Tiles)
        {
            var coord = kvp.Key;
            if (coord == caster.Coord) continue;
            if (caster.Coord.DistanceTo(coord) > totalRange) continue;
            if (IsOccupied(coord, allUnits)) continue;
            targets.Add(coord);
        }

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        Debug.Log($"Jump: {caster.DisplayName} jumped to {target}.");
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
