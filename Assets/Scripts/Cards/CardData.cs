using UnityEngine;

/// <summary>
/// Static, immutable definition of a card. Lives as a ScriptableObject asset.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    [Tooltip("Display name of the card")]
    public string cardName;

    [Tooltip("What this card does when played")]
    public CardEffect effect;

    [Tooltip("Range in hexes for targeting (how far the target can be)")]
    [Min(1)] public int range = 1;

    [Tooltip("Damage dealt when this card hits (0 for non-damage cards)")]
    [Min(0)] public int damage = 0;

    [Tooltip("Push/Pull distance in hexes (how far the target is displaced)")]
    [Min(0)] public int pushDistance = 1;

    [Tooltip("Cooldown in turns after being played (1–5)")]
    [Range(1, 5)] public int cooldown = 1;
}
