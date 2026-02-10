using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime state for one card in a player's hand.
/// Wraps a CardData asset, tracks cooldown, and delegates to its resolver.
/// </summary>
public class CardInstance
{
    public CardData Data { get; }
    public ICardResolver Resolver { get; }
    public int CooldownRemaining { get; private set; }

    public bool IsReady => CooldownRemaining <= 0;

    public CardInstance(CardData data)
    {
        Data = data;
        Resolver = CardResolverFactory.Get(data.effect);
        CooldownRemaining = 0;
    }

    /// <summary>
    /// Returns valid target hexes for this card given current game state.
    /// </summary>
    public List<HexCoord> GetValidTargets(Unit caster, Unit enemy, HexGrid grid)
    {
        return Resolver.GetValidTargets(Data, caster, enemy, grid);
    }

    /// <summary>
    /// Resolve the card effect on the chosen target. Starts cooldown.
    /// </summary>
    public void Resolve(Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        Resolver.Resolve(Data, caster, enemy, grid, target);
        CooldownRemaining = Data.cooldown;
        Debug.Log($"[{Data.cardName}] resolved! Cooldown set to {Data.cooldown}.");
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
