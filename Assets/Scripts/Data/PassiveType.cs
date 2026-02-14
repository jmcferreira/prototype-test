/// <summary>
/// Identifies which passive skill a unit uses.
/// Maps to concrete PassiveSkill subclasses via PassiveFactory.
/// </summary>
public enum PassiveType
{
    None,
    Setup,
    Guardian,
    Horde,
    DarkPact,
}
