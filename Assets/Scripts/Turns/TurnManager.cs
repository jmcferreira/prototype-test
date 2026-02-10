using UnityEngine;

/// <summary>
/// Manages strict Player → Enemy turn alternation.
/// Player presses Space to end their turn; enemy turn ends immediately (no AI yet).
/// </summary>
public class TurnManager : MonoBehaviour
{
    public Team CurrentTeam { get; private set; } = Team.Player;
    public int TurnNumber { get; private set; } = 1;

    private bool _waitingForPlayer;

    public void Begin()
    {
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

        if (team == Team.Player)
        {
            Debug.Log($"=== Turn {TurnNumber} — PLAYER turn === (press Space to end)");
        }
        else
        {
            Debug.Log($"=== Turn {TurnNumber} — ENEMY turn === (auto-ending, no AI yet)");
            // Enemy has no logic yet — end immediately next frame
            Invoke(nameof(EndTurn), 0.1f);
        }
    }

    private void EndTurn()
    {
        _waitingForPlayer = false;

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
