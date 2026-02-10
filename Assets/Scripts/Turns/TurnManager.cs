using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages per-unit turn order: Player → Enemy1 → Enemy2 → Enemy3 → ...
/// Skips dead units. Detects victory/defeat conditions.
/// </summary>
public class TurnManager : MonoBehaviour
{
    private List<Unit> _turnOrder = new();
    private int _currentIndex;

    public Unit CurrentUnit => _turnOrder.Count > 0 ? _turnOrder[_currentIndex] : null;
    public Team CurrentTeam => CurrentUnit != null ? CurrentUnit.Team : Team.Player;
    public int TurnNumber { get; private set; } = 1;

    /// <summary>All units registered in the turn order (alive or dead).</summary>
    public IReadOnlyList<Unit> AllUnits => _turnOrder;

    /// <summary>
    /// Returns a fresh list of all alive units.
    /// </summary>
    public List<Unit> GetAliveUnits()
    {
        var alive = new List<Unit>();
        foreach (var u in _turnOrder)
            if (u.IsAlive) alive.Add(u);
        return alive;
    }

    public void Begin(List<Unit> turnOrder)
    {
        _turnOrder = new List<Unit>(turnOrder);
        _currentIndex = 0;
        TurnNumber = 1;
        Debug.Log($"=== Turn {TurnNumber} — {CurrentUnit.DisplayName}'s turn ===");
        CurrentUnit.OnTurnStart();
    }

    /// <summary>
    /// Called by the active unit when it finishes acting.
    /// </summary>
    public void EndCurrentTurn()
    {
        if (_turnOrder.Count == 0) return;

        CurrentUnit.OnTurnEnd();

        // Check for game over
        bool anyPlayerAlive = false;
        bool anyEnemyAlive = false;
        foreach (var u in _turnOrder)
        {
            if (u.IsAlive && u.Team == Team.Player) anyPlayerAlive = true;
            if (u.IsAlive && u.Team == Team.Enemy) anyEnemyAlive = true;
        }
        if (!anyPlayerAlive || !anyEnemyAlive)
        {
            string result = anyPlayerAlive ? "Victory! All enemies defeated." : "Defeat! Player has fallen.";
            Debug.Log(result);
            return;
        }

        // Advance to next alive unit
        int startIndex = _currentIndex;
        do
        {
            _currentIndex = (_currentIndex + 1) % _turnOrder.Count;
            if (_currentIndex == 0) TurnNumber++;
        } while (!_turnOrder[_currentIndex].IsAlive && _currentIndex != startIndex);

        Debug.Log($"=== Turn {TurnNumber} — {CurrentUnit.DisplayName}'s turn ===");
        CurrentUnit.OnTurnStart();
    }
}
