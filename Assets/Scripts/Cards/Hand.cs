using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hand of cards. Holds CardInstances, manages cooldowns, and validates plays.
/// </summary>
public class Hand
{
    private readonly List<CardInstance> _cards = new();

    public IReadOnlyList<CardInstance> Cards => _cards;

    public Hand(IEnumerable<CardData> cardDatas)
    {
        foreach (var data in cardDatas)
            _cards.Add(new CardInstance(data));
    }

    /// <summary>
    /// Tick all cooldowns by 1. Call at the start of the owning player's turn.
    /// </summary>
    public void TickCooldowns()
    {
        foreach (var card in _cards)
            card.TickCooldown();
    }

    /// <summary>
    /// Log current hand state to the console.
    /// </summary>
    public void LogHand()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            var c = _cards[i];
            string status = c.IsReady ? "READY" : $"cd:{c.CooldownRemaining}";
            string actions = DescribeActions(c.Data);
            Debug.Log($"  [{i + 1}] {c.Data.cardName} ({actions}) — {status}");
        }
    }

    public static string DescribeActions(CardData data)
    {
        if (data.actions == null || data.actions.Length == 0) return "no actions";
        var parts = new List<string>();
        foreach (var a in data.actions)
            parts.Add(DescribeAction(a));
        return string.Join(" > ", parts);
    }

    public static string DescribeAction(CardAction action)
    {
        switch (action.effect)
        {
            case CardEffect.Move:      return $"Move {action.range}";
            case CardEffect.Dash:      return $"Dash {action.range}";
            case CardEffect.Attack:    return $"Atk {action.damage} Rng {action.range}";
            case CardEffect.Push:      return $"Push {action.pushDistance} Rng {action.range}";
            case CardEffect.Pull:      return $"Pull {action.pushDistance} Rng {action.range}";
            case CardEffect.Heal:      return $"Heal {action.damage}";
            case CardEffect.Jump:      return $"Jump {action.range}";
            case CardEffect.AttackAoE: return $"AoE {action.damage} Rng {action.range}";
            case CardEffect.Status:
                var def = StatusEffectDefs.Get(action.statusEffect);
                string self = action.targetSelf ? " (self)" : "";
                return $"{def.Name} {action.statusStacks}{self} Rng {action.range}";
            default:                return action.effect.ToString();
        }
    }
}
