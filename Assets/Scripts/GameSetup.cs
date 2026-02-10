using UnityEngine;

/// <summary>
/// Bootstraps the game: waits for the grid, spawns units, starts the turn loop.
/// Attach to the same GameObject as HexGrid, or any object in the scene.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private CardData[] starterCards;

    private void Start()
    {
        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();

        // Wait one frame so HexGrid.Start() has run
        Invoke(nameof(Setup), 0f);
    }

    private void Setup()
    {
        var playerCoord = new HexCoord(1, 1);
        var enemyCoord = new HexCoord(5, 1);

        var playerGo = new GameObject();
        var player = playerGo.AddComponent<PlayerUnit>();
        player.Init(Team.Player, playerCoord, hexGrid);

        // Build fallback cards at runtime if none assigned in the Inspector
        if (starterCards == null || starterCards.Length == 0)
            starterCards = CreateDefaultCards();

        var enemyGo = new GameObject();
        var enemy = enemyGo.AddComponent<EnemyUnit>();
        enemy.Init(Team.Enemy, enemyCoord, hexGrid);

        // Hex interaction (hover + click)
        var interactionGo = new GameObject("HexInteraction");
        var hexInteraction = interactionGo.AddComponent<HexInteraction>();

        player.InitHand(starterCards);
        player.SetEnemy(enemy);
        player.SetHexInteraction(hexInteraction);

        var turnManagerGo = new GameObject("TurnManager");
        var turnManager = turnManagerGo.AddComponent<TurnManager>();
        player.SetTurnManager(turnManager);
        turnManager.Begin(player, enemy);

        Debug.Log($"Player placed at {playerCoord}, Enemy placed at {enemyCoord}");
    }

    private static CardData[] CreateDefaultCards()
    {
        var move = ScriptableObject.CreateInstance<CardData>();
        move.cardName = "Dash";
        move.effect = CardEffect.Move;
        move.range = 2;
        move.cooldown = 1;

        var attack = ScriptableObject.CreateInstance<CardData>();
        attack.cardName = "Strike";
        attack.effect = CardEffect.Attack;
        attack.range = 1;
        attack.cooldown = 2;

        var push = ScriptableObject.CreateInstance<CardData>();
        push.cardName = "Shove";
        push.effect = CardEffect.Push;
        push.range = 1;
        push.cooldown = 3;

        var pull = ScriptableObject.CreateInstance<CardData>();
        pull.cardName = "Hook";
        pull.effect = CardEffect.Pull;
        pull.range = 2;
        pull.cooldown = 3;

        return new[] { move, attack, push, pull };
    }
}
