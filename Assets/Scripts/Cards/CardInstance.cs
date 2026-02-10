using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime state for one card in a player's hand.
/// Wraps a CardData asset and tracks cooldown.
/// Provides per-action access for multi-action resolution.
/// </summary>
public class CardInstance
{
    public CardData Data { get; }
    public int CooldownRemaining { get; private set; }

    public bool IsReady => CooldownRemaining <= 0;
    public int ActionCount => Data.actions != null ? Data.actions.Length : 0;

    public CardInstance(CardData data)
    {
        Data = data;
        CooldownRemaining = 0;
    }

    public CardAction GetAction(int index)
    {
        return Data.actions[index];
    }

    /// <summary>
    /// Returns valid target hexes for a specific action.
    /// </summary>
    public List<HexCoord> GetValidTargetsForAction(int actionIndex, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var action = Data.actions[actionIndex];
        var resolver = CardResolverFactory.Get(action.effect);
        return resolver.GetValidTargets(action, caster, allUnits, grid);
    }

    /// <summary>
    /// Resolve a specific action on the chosen target.
    /// </summary>
    public void ResolveAction(int actionIndex, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        var action = Data.actions[actionIndex];
        var resolver = CardResolverFactory.Get(action.effect);
        resolver.Resolve(action, caster, allUnits, grid, target);
    }

    /// <summary>
    /// Apply cooldown. Called once after all actions are done (resolved or skipped).
    /// </summary>
    public void StartCooldown()
    {
        CooldownRemaining = Data.cooldown;
        Debug.Log($"[{Data.cardName}] done. Cooldown set to {Data.cooldown}.");
    }

    /// <summary>
    /// Tick cooldown down by 1. Called at the start of the owning player's turn.
    /// </summary>
    public void TickCooldown()
    {
        if (CooldownRemaining > 0)
        {
            CooldownRemaining--;
            Debug.Log($"[{Data.cardName}] cooldown ticked: {CooldownRemaining} turns left.");
        }
    }
}
