using UnityEngine;

/// <summary>
/// Manages strict Player → Enemy turn alternation.
/// Player presses Space to end their turn; enemy turn ends immediately (no AI yet).
/// Notifies units via OnTurnStart / OnTurnEnd callbacks.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public Team CurrentTeam { get; private set; } = Team.Player;
    public int TurnNumber { get; private set; } = 1;

    private Unit _playerUnit;
    private Unit _enemyUnit;
    private bool _waitingForPlayer;

    public void Begin(Unit playerUnit, Unit enemyUnit)
    {
        _playerUnit = playerUnit;
        _enemyUnit = enemyUnit;
        TurnNumber = 1;
        StartTurn(Team.Player);
    }

    private void Update()
    {
        if (_waitingForPlayer && Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
        }
    }

    private void StartTurn(Team team)
    {
        CurrentTeam = team;
        _waitingForPlayer = team == Team.Player;

        Unit active = team == Team.Player ? _playerUnit : _enemyUnit;
        active.OnTurnStart();

        if (team == Team.Player)
        {
            Debug.Log($"=== Turn {TurnNumber} — PLAYER turn === (click adjacent hex to move, Space to skip)");
        }
        else
        {
            Debug.Log($"=== Turn {TurnNumber} — ENEMY turn === (auto-ending, no AI yet)");
            Invoke(nameof(EndTurn), 0.1f);
        }
    }

    private void EndTurn()
    {
        _waitingForPlayer = false;

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
