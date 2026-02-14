using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type of tower structure in PVP mode.
/// </summary>
public enum TowerType
{
    Base,   // Destroy to win
    Guard,  // Provides gold + deals damage to nearby enemies
}

/// <summary>
/// Tower unit for PVP mode. Towers occupy hexes, have HP, and can be
/// attacked, but never take turns themselves. Guard towers deal damage
/// to nearby enemies during the tower phase.
/// </summary>
public class TowerUnit : Unit
{
    public TowerType TowerType { get; private set; }
    public int OwnerAlliance { get; private set; }
    public int AttackDamage { get; private set; }
    public int AttackRange { get; private set; }

    public void InitTower(TowerType type, int alliance, Team team, HexCoord pos,
                          HexGrid grid, int hp, int attackDamage = 0, int attackRange = 0)
    {
        TowerType = type;
        OwnerAlliance = alliance;
        AttackDamage = attackDamage;
        AttackRange = attackRange;

        string towerName = type == TowerType.Base
            ? $"P{alliance + 1} Base Tower"
            : $"P{alliance + 1} Guard Tower";

        string icon = type == TowerType.Base ? "\u25c6" : "\u25a0"; // ◆ or ■

        Init(team, pos, grid, towerName, hp, icon);
        SetAlliance(alliance);

        // Override the default chip visuals with tower-specific look
        RecolorTower(alliance);
    }

    /// <summary>
    /// Guard tower fires at all enemies in range. Called during tower phase.
    /// Returns number of targets hit.
    /// </summary>
    public int FireAtEnemies(List<Unit> allUnits)
    {
        if (TowerType != TowerType.Guard || !IsAlive) return 0;
        if (AttackDamage <= 0 || AttackRange <= 0) return 0;

        int hits = 0;
        foreach (var unit in allUnits)
        {
            if (!unit.IsAlive || IsAlly(unit)) continue;
            if (unit is TowerUnit) continue; // towers don't shoot towers
            if (Coord.DistanceTo(unit.Coord) > AttackRange) continue;

            unit.TakeHit(AttackDamage);
            Debug.Log($"{DisplayName} fires at {unit.DisplayName} for {AttackDamage} damage!");
            BattleLog.AddAction($"{DisplayName} hits {unit.DisplayName} for {AttackDamage}");
            hits++;
        }
        return hits;
    }

    private void RecolorTower(int alliance)
    {
        // Alliance 0 = teal, Alliance 1 = orange
        Color bright = alliance == 0
            ? new Color(0.2f, 0.7f, 0.6f)
            : new Color(0.8f, 0.5f, 0.15f);
        Color dark = alliance == 0
            ? new Color(0.1f, 0.35f, 0.3f)
            : new Color(0.4f, 0.25f, 0.08f);

        // Make towers visually larger
        float scale = TowerType == TowerType.Base ? 1.3f : 1.1f;

        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (r.name == "ChipRim")
            {
                r.material.color = dark;
                r.transform.localScale = new Vector3(0.78f * scale, 0.06f, 0.78f * scale);
            }
            else if (r.name == "ChipFace")
            {
                r.material.color = bright;
                r.transform.localScale = new Vector3(0.64f * scale, 0.065f, 0.64f * scale);
            }
        }
    }

    // Towers never take turns
    public override void OnTurnStart() { }
    public override void OnTurnEnd() { }
}
