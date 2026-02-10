using UnityEngine;

/// <summary>
/// Bootstraps the game: waits for the grid, spawns units, starts the turn loop.
/// Attach to the same GameObject as HexGrid, or any object in the scene.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;

    private void Start()
    {
        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();

        // Wait one frame so HexGrid.Start() has run
        Invoke(nameof(Setup), 0f);
    }

    private void Setup()
    {
        float size = hexGrid.HexSize;

        // Place player near bottom-left, enemy near top-right
        var playerCoord = new HexCoord(1, 1);
        var enemyCoord = new HexCoord(5, 1);

        var playerGo = new GameObject();
        var player = playerGo.AddComponent<Unit>();
        player.Init(Team.Player, playerCoord, size);

        var enemyGo = new GameObject();
        var enemy = enemyGo.AddComponent<Unit>();
        enemy.Init(Team.Enemy, enemyCoord, size);

        // Create and start turn manager
        var turnManagerGo = new GameObject("TurnManager");
        var turnManager = turnManagerGo.AddComponent<TurnManager>();
        turnManager.Begin();

        Debug.Log($"Player placed at {playerCoord}, Enemy placed at {enemyCoord}");
    }
}
