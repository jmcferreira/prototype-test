using UnityEngine;

/// <summary>
/// One step in a card's action sequence.
/// A card can have multiple actions resolved in order.
/// </summary>
[System.Serializable]
public class CardAction
{
    public CardEffect effect;

    [Min(1)] public int range = 1;
    [Min(0)] public int damage = 0;
    [Min(0)] public int pushDistance = 1;
}
