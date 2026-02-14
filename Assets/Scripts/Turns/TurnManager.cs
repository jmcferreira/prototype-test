using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages per-unit turn order: Player → Enemy1 → Enemy2 → Enemy3 → ...
/// Skips dead units. Detects victory/defeat conditions.
/// </summary>
public class TurnManager : MonoBehaviour
{
    protected List<Unit> _turnOrder = new();
    protected int _currentIndex;

    public Unit CurrentUnit => _turnOrder.Count > 0 ? _turnOrder[_currentIndex] : null;
    public Team CurrentTeam => CurrentUnit != null ? CurrentUnit.Team : Team.Player;
    public int TurnNumber { get; protected set; } = 1;

    /// <summary>Fired when the active unit changes (at game start and after each turn ends).</summary>
    public event Action<Unit> OnTurnChanged;

    /// <summary>Fired when the game ends. True = player victory, false = defeat.</summary>
    public event Action<bool> OnGameOver;

    /// <summary>All units registered in the turn order (alive or dead).</summary>
    public IReadOnlyList<Unit> AllUnits => _turnOrder;

    /// <summary>
    /// Returns a fresh list of all alive units.
    /// </summary>
    public virtual List<Unit> GetAliveUnits()
    {
        var alive = new List<Unit>();
        foreach (var u in _turnOrder)
            if (u.IsAlive) alive.Add(u);
        return alive;
    }

    public virtual void Begin(List<Unit> turnOrder)
    {
        _turnOrder = new List<Unit>(turnOrder);
        _currentIndex = 0;
        TurnNumber = 1;
        Debug.Log($"=== Turn {TurnNumber} — {CurrentUnit.DisplayName}'s turn ===");
        BattleLog.AddTurnHeader(CurrentUnit.DisplayName, TurnNumber);
        OnTurnChanged?.Invoke(CurrentUnit);
        CurrentUnit.OnTurnStart();
    }

    /// <summary>
    /// Called by the active unit when it finishes acting.
    /// </summary>
    public virtual void EndCurrentTurn()
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
            bool playerWon = anyPlayerAlive;
            string result = playerWon ? "Victory! All enemies defeated." : "Defeat! Player has fallen.";
            Debug.Log(result);
            FireGameOver(playerWon);
            return;
        }

        // Advance to next alive unit
        AdvanceToNextUnit();
    }

    protected void AdvanceToNextUnit()
    {
        int startIndex = _currentIndex;
        do
        {
            _currentIndex = (_currentIndex + 1) % _turnOrder.Count;
            if (_currentIndex == 0) TurnNumber++;
        } while (!_turnOrder[_currentIndex].IsAlive && _currentIndex != startIndex);

        Debug.Log($"=== Turn {TurnNumber} — {CurrentUnit.DisplayName}'s turn ===");
        BattleLog.AddTurnHeader(CurrentUnit.DisplayName, TurnNumber);
        FireTurnChanged(CurrentUnit);
        CurrentUnit.OnTurnStart();
    }

    protected void FireTurnChanged(Unit unit) => OnTurnChanged?.Invoke(unit);
    protected void FireGameOver(bool playerWon) => OnGameOver?.Invoke(playerWon);
}
