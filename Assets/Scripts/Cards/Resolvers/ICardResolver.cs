using System.Collections.Generic;

/// <summary>
/// A card resolver knows which hexes are valid targets and how to
/// resolve the effect when one is chosen. No input handling, no turn logic.
/// </summary>
public interface ICardResolver
{
    /// <summary>
    /// Return every hex the player is allowed to pick right now.
    /// </summary>
    List<HexCoord> GetValidTargets(CardAction action, Unit caster, Unit enemy, HexGrid grid);

    /// <summary>
    /// Execute the effect on the chosen target hex. Only called with
    /// a coord that was in the GetValidTargets list.
    /// </summary>
    void Resolve(CardAction action, Unit caster, Unit enemy, HexGrid grid, HexCoord target);
}
