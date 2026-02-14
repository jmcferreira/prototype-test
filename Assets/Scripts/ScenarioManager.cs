using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that coordinates the run lifecycle:
///   New Run → Scenario 1 → Win → Heal → Scenario 2 → Win → ... → Run Complete
/// Persists across scene reloads via DontDestroyOnLoad.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Start a brand new run with the given party definitions.
    /// </summary>
    public void StartNewRun(UnitDef[] playerDefs)
    {
        RunState.NewRun(playerDefs);
        LoadCurrentScenario();
    }

    /// <summary>
    /// Called when the party wins the current scenario.
    /// Accepts HP array (one per party member).
    /// </summary>
    public void OnScenarioVictory(int[] playerHPs, int goldEarned, int xpEarned)
    {
        var run = RunState.Current;
        if (run == null) return;

        run.OnScenarioWon(playerHPs, goldEarned, xpEarned);

        string hpSummary = "";
        for (int i = 0; i < run.PartySize; i++)
            hpSummary += $" {run.PlayerDefs[i].displayName}:{run.PlayerCurrentHPs[i]}/{run.PlayerDefs[i].maxHP}";

        Debug.Log($"Scenario complete!{hpSummary} | Gold: {run.TotalGold} | XP: {run.TotalXP} " +
                  $"| Stages: {run.ScenariosCompleted}");

        var next = run.GetNextScenario();
        if (next == null)
        {
            Debug.Log("Run complete! All scenarios finished.");
            RunState.Current = null;
        }
        else
        {
            Debug.Log($"Next scenario: {next.scenarioName}");
        }
        // Scene reload is handled by GameOverModalUI button click
    }

    /// <summary>
    /// Called by GameSetup when the player loses.
    /// </summary>
    public void OnScenarioDefeat()
    {
        Debug.Log("Run ended — player defeated.");
        RunState.Current = null;
        // Scene stays on game-over modal; restart button will start a fresh run
    }

    private void LoadCurrentScenario()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Get the scenario to load. If a run is active, returns the current run's scenario.
    /// Otherwise returns the original test scenario as fallback.
    /// </summary>
    public static ScenarioDef GetCurrentScenario()
    {
        if (RunState.Current != null)
        {
            var scenario = RunState.Current.GetNextScenario();
            if (scenario == null)
                scenario = ScenarioPool.GetScenario(1); // fallback
            return scenario;
        }

        // No active run — this is the first load. Start a new run.
        return null;
    }
}
