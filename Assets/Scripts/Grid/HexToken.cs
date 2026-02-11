using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for tokens/traps placed on hex tiles.
/// Tokens have a trigger condition and an effect.
/// They are not obstacles — units can move through them.
/// </summary>
public abstract class HexToken
{
    public string Name { get; }
    public string Description { get; }
    public Color Color { get; }
    public Unit Owner { get; }

    protected HexToken(string name, string description, Color color, Unit owner)
    {
        Name = name;
        Description = description;
        Color = color;
        Owner = owner;
    }

    /// <summary>
    /// Called when a unit enters the hex this token is on.
    /// Returns true if the token should be consumed (removed) after triggering.
    /// </summary>
    public abstract bool OnUnitEnter(Unit unit, List<Unit> allUnits);
}

/// <summary>
/// Web token: When an enemy (relative to the token owner) moves into this hex,
/// apply Root 1 and immediately stop their movement.
/// </summary>
public class WebToken : HexToken
{
    public WebToken(Unit owner)
        : base("Web", "Enemy entering: Root 1, stop movement.",
               new Color(0.8f, 0.8f, 0.8f, 0.9f), owner) { }

    public override bool OnUnitEnter(Unit unit, List<Unit> allUnits)
    {
        // Only trigger for enemies of the token owner
        if (unit.Team == Owner.Team) return false;
        if (!unit.IsAlive) return false;

        unit.ApplyStatus(StatusEffectType.Root, 1);
        Debug.Log($"  Web: {unit.DisplayName} stepped on a web — Root 1 applied!");
        return true; // consume the token
    }
}
