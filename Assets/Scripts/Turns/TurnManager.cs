using UnityEngine;

/// <summary>
/// Manages strict Player → Enemy turn alternation.
/// Units call EndCurrentTurn() when they're done acting.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public Team CurrentTeam { get; private set; } = Team.Player;
    public int TurnNumber { get; private set; } = 1;

    private Unit _playerUnit;
    private Unit _enemyUnit;

    public void Begin(Unit playerUnit, Unit enemyUnit)
    {
        _playerUnit = playerUnit;
        _enemyUnit = enemyUnit;
        TurnNumber = 1;
        StartTurn(Team.Player);
    }

    private void StartTurn(Team team)
    {
        CurrentTeam = team;

        Unit active = team == Team.Player ? _playerUnit : _enemyUnit;
        active.OnTurnStart();

        if (team == Team.Player)
        {
            Debug.Log($"=== Turn {TurnNumber} — PLAYER turn ===");
        }
        else
        {
            Debug.Log($"=== Turn {TurnNumber} — ENEMY turn === (auto-ending, no AI yet)");
            Invoke(nameof(EndCurrentTurn), 0.1f);
        }
    }

    /// <summary>
    /// Called by the active unit (or auto-invoked for enemy) to end the current turn.
    /// </summary>
    public void EndCurrentTurn()
    {
        Unit active = CurrentTeam == Team.Player ? _playerUnit : _enemyUnit;
        active.OnTurnEnd();

        if (CurrentTeam == Team.Player)
        {
            Debug.Log("Player ended their turn.");
            StartTurn(Team.Enemy);
        }
        else
        {
            Debug.Log("Enemy ended their turn.");
            TurnNumber++;
            StartTurn(Team.Player);
        }
    }
}
