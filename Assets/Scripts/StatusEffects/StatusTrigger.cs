/// <summary>
/// When a status effect's damage is applied.
/// </summary>
public enum StatusTrigger
{
    None,       // Passive modifier only (e.g. Blind)
    OnAction,   // Triggers each time the afflicted unit resolves an action (e.g. Burn)
    OnTurnEnd,  // Triggers at the end of the afflicted unit's turn (e.g. Poison)
}
