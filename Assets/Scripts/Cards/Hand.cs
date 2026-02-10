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
            Debug.Log($"  [{i + 1}] {c.Data.cardName} ({c.Data.effect}, range:{c.Data.range}) — {status}");
        }
    }
}
