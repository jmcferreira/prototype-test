using UnityEngine;

/// <summary>
/// Static definition of a card. Each card has a name, a cooldown,
/// and an ordered list of actions to resolve sequentially.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    [Tooltip("Display name of the card")]
    public string cardName;

    [Tooltip("Ordered list of actions — resolved top to bottom, each can be skipped")]
    public CardAction[] actions;

    [Tooltip("Cooldown in turns after being played (1–5)")]
    [Range(1, 5)] public int cooldown = 1;
}
