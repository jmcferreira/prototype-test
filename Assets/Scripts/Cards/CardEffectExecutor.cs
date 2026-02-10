using UnityEngine;

/// <summary>
/// Resolves card effects on the grid. Pure logic, no visuals.
/// All methods return true if the effect was successfully applied.
/// </summary>
public static class CardEffectExecutor
{
    public static bool Execute(CardInstance card, Unit caster, Unit target, HexGrid grid)
    {
        switch (card.Data.effect)
        {
            case CardEffect.Move:
                return ExecuteMove(card.Data, caster, grid);
            case CardEffect.Attack:
                return ExecuteAttack(card.Data, caster, target);
            case CardEffect.Push:
                return ExecutePush(card.Data, caster, target, grid);
            case CardEffect.Pull:
                return ExecutePull(card.Data, caster, target, grid);
            default:
                Debug.Log($"Unknown effect: {card.Data.effect}");
                return false;
        }
    }

    /// <summary>
    /// Move: caster picks a direction (1–6), moves up to card.range hexes in that direction.
    /// For keyboard-only flow, direction is chosen before calling this.
    /// </summary>
    public static bool ExecuteMove(CardData data, Unit caster, HexGrid grid, int direction = -1)
    {
        if (direction < 0 || direction > 5)
        {
            Debug.Log("Move: invalid direction.");
            return false;
        }

        HexCoord current = caster.Coord;
        HexCoord dest = current;

        // Walk up to range hexes in the chosen direction
        for (int i = 0; i < data.range; i++)
        {
            HexCoord next = dest.Neighbor(direction);
            if (!grid.TryGetTile(next, out _)) break;
            if (IsOccupied(next)) break;
            dest = next;
        }

        if (dest == current)
        {
            Debug.Log("Move: nowhere to go in that direction.");
            return false;
        }

        caster.ForceMoveTo(dest);
        Debug.Log($"Move: {caster.Team} dashed to {dest} (range {data.range}).");
        return true;
    }

    /// <summary>
    /// Attack: deal 1 hit to the target if within range. No randomness.
    /// </summary>
    public static bool ExecuteAttack(CardData data, Unit caster, Unit target)
    {
        int dist = caster.Coord.DistanceTo(target.Coord);
        if (dist > data.range)
        {
            Debug.Log($"Attack: target at distance {dist}, need range {data.range}.");
            return false;
        }

        target.TakeHit(1);
        Debug.Log($"Attack: {caster.Team} hit {target.Team} for 1 damage at range {dist}.");
        return true;
    }

    /// <summary>
    /// Push: shove the target 1 hex away from the caster. Must be in range.
    /// </summary>
    public static bool ExecutePush(CardData data, Unit caster, Unit target, HexGrid grid)
    {
        int dist = caster.Coord.DistanceTo(target.Coord);
        if (dist > data.range)
        {
            Debug.Log($"Push: target at distance {dist}, need range {data.range}.");
            return false;
        }

        HexCoord direction = target.Coord - caster.Coord;
        HexCoord dest = target.Coord + direction;

        if (!grid.TryGetTile(dest, out _))
        {
            Debug.Log("Push: target would be pushed off the grid.");
            return false;
        }
        if (IsOccupied(dest))
        {
            Debug.Log("Push: destination is blocked.");
            return false;
        }

        Debug.Log($"Push: {target.Team} pushed from {target.Coord} to {dest}.");
        target.ForceMoveTo(dest);
        return true;
    }

    /// <summary>
    /// Pull: yank the target 1 hex toward the caster. Must be in range.
    /// </summary>
    public static bool ExecutePull(CardData data, Unit caster, Unit target, HexGrid grid)
    {
        int dist = caster.Coord.DistanceTo(target.Coord);
        if (dist > data.range)
        {
            Debug.Log($"Pull: target at distance {dist}, need range {data.range}.");
            return false;
        }
        if (dist <= 1)
        {
            Debug.Log("Pull: target is already adjacent, can't pull closer.");
            return false;
        }

        HexCoord direction = caster.Coord - target.Coord;
        HexCoord dest = target.Coord + direction;

        if (!grid.TryGetTile(dest, out _))
        {
            Debug.Log("Pull: destination tile doesn't exist.");
            return false;
        }
        if (IsOccupied(dest))
        {
            Debug.Log("Pull: destination is blocked.");
            return false;
        }

        Debug.Log($"Pull: {target.Team} pulled from {target.Coord} to {dest}.");
        target.ForceMoveTo(dest);
        return true;
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
