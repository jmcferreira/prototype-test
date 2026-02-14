/// <summary>
/// Creates PassiveSkill instances from PassiveType enum values.
/// </summary>
public static class PassiveFactory
{
    public static PassiveSkill Create(PassiveType type)
    {
        return type switch
        {
            PassiveType.Setup    => new SetupPassive(),
            PassiveType.Horde    => new HordePassive(),
            PassiveType.DarkPact => new DarkPactPassive(),
            _                    => null,
        };
    }
}
