using System.Collections.Generic;

/// <summary>
/// Maps each CardEffect to its ICardResolver. Resolvers are singletons.
/// </summary>
public static class CardResolverFactory
{
    private static readonly Dictionary<CardEffect, ICardResolver> _resolvers = new()
    {
        { CardEffect.Move,   new MoveResolver() },
        { CardEffect.Dash,   new DashResolver() },
        { CardEffect.Attack, new AttackResolver() },
        { CardEffect.Push,   new PushResolver() },
        { CardEffect.Pull,   new PullResolver() },
        { CardEffect.Heal,   new HealResolver() },
    };

    public static ICardResolver Get(CardEffect effect)
    {
        return _resolvers[effect];
    }
}
