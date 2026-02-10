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

    [Tooltip("Cooldown in turns after being played (0 = no cooldown)")]
    [Min(0)] public int cooldown = 1;

    [Tooltip("If > 0, the card starts the game on this many turns of cooldown")]
    [Min(0)] public int startCooldown = 0;
}
