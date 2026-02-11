using System.Collections.Generic;

/// <summary>
/// Maps each CardEffect to its ICardResolver. Resolvers are singletons.
/// </summary>
public static class CardResolverFactory
{
    private static readonly Dictionary<CardEffect, ICardResolver> _resolvers = new()
    {
        { CardEffect.Move,           new MoveResolver() },
        { CardEffect.Dash,           new DashResolver() },
        { CardEffect.Attack,         new AttackResolver() },
        { CardEffect.Push,           new PushResolver() },
        { CardEffect.Pull,           new PullResolver() },
        { CardEffect.Heal,           new HealResolver() },
        { CardEffect.Status,         new StatusResolver() },
        { CardEffect.Jump,           new JumpResolver() },
        { CardEffect.AttackAoE,      new AttackAoEResolver() },
        { CardEffect.AttackLine,     new AttackLineResolver() },
        { CardEffect.PushAoE,        new PushAoEResolver() },
        { CardEffect.ReduceCooldown, new ReduceCooldownResolver() },
        { CardEffect.PlaceWeb,       new PlaceWebResolver() },
    };

    public static ICardResolver Get(CardEffect effect)
    {
        return _resolvers[effect];
    }
}
