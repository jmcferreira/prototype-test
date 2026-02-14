using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A deck of Fate cards with draw pile, discard pile, and reshuffle-on-empty.
/// "Draw 2, pick 1, both discarded."
/// Deck is only reshuffled when we need to draw and the draw pile is empty.
/// </summary>
public class FateDeck
{
    private readonly List<FateCardData> _drawPile = new();
    private readonly List<FateCardData> _discardPile = new();

    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;

    public FateDeck(FateCardData[] cards)
    {
        _drawPile.AddRange(cards);
        Shuffle(_drawPile);
    }

    /// <summary>
    /// Draw up to <paramref name="count"/> cards from the draw pile.
    /// If the draw pile runs out, reshuffle the discard pile into it first.
    /// Returns fewer than count if not enough cards exist in the entire deck.
    /// </summary>
    public FateCardData[] Draw(int count)
    {
        var drawn = new List<FateCardData>(count);

        for (int i = 0; i < count; i++)
        {
            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
                    break; // No cards left at all

                Reshuffle();
            }

            int last = _drawPile.Count - 1;
            drawn.Add(_drawPile[last]);
            _drawPile.RemoveAt(last);
        }

        return drawn.ToArray();
    }

    /// <summary>
    /// Discard the given cards (both the picked and unpicked ones).
    /// </summary>
    public void DiscardAll(FateCardData[] cards)
    {
        foreach (var card in cards)
        {
            if (card != null)
                _discardPile.Add(card);
        }
    }

    private void Reshuffle()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
        Debug.Log("Fate deck reshuffled.");
    }

    private static void Shuffle(List<FateCardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
