using UnityEngine;

/// <summary>
/// Bootstraps the game: waits for the grid, spawns units, starts the turn loop.
/// Assign card assets in the Inspector to tweak stats without code changes.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;

    [Header("Player Cards (drag CardData assets here)")]
    [SerializeField] private CardData[] playerCards;

    [Header("Enemy Cards (drag CardData assets here, priority order)")]
    [SerializeField] private CardData[] enemyCards;

    private void Start()
    {
        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();

        Invoke(nameof(Setup), 0f);
    }

    private void Setup()
    {
        var playerCoord = new HexCoord(1, 1);
        var enemyCoord = new HexCoord(5, 1);

        var playerGo = new GameObject();
        var player = playerGo.AddComponent<PlayerUnit>();
        player.Init(Team.Player, playerCoord, hexGrid);

        // Load from Inspector or fall back to Resources/Cards
        if (playerCards == null || playerCards.Length == 0)
            playerCards = Resources.LoadAll<CardData>("Cards/Player");
        if (enemyCards == null || enemyCards.Length == 0)
            enemyCards = Resources.LoadAll<CardData>("Cards/Enemy");

        // Final fallback: create in code
        if (playerCards.Length == 0) playerCards = CreateDefaultPlayerCards();
        if (enemyCards.Length == 0) enemyCards = CreateDefaultEnemyCards();

        var enemyGo = new GameObject();
        var enemy = enemyGo.AddComponent<EnemyUnit>();
        enemy.Init(Team.Enemy, enemyCoord, hexGrid);
        enemy.InitCards(enemyCards);
        enemy.SetPlayer(player);

        var interactionGo = new GameObject("HexInteraction");
        var hexInteraction = interactionGo.AddComponent<HexInteraction>();

        player.InitHand(playerCards);
        player.SetEnemy(enemy);
        player.SetHexInteraction(hexInteraction);

        var handUIGo = new GameObject("HandUI");
        var handUI = handUIGo.AddComponent<HandUI>();
        handUI.Init(player.Hand);
        player.SetHandUI(handUI);

        var turnManagerGo = new GameObject("TurnManager");
        var turnManager = turnManagerGo.AddComponent<TurnManager>();
        player.SetTurnManager(turnManager);
        enemy.SetTurnManager(turnManager);
        turnManager.Begin(player, enemy);

        Debug.Log($"Player placed at {playerCoord}, Enemy placed at {enemyCoord}");
    }

    private static CardData[] CreateDefaultPlayerCards()
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
        attack.damage = 1;
        attack.cooldown = 2;

        var push = ScriptableObject.CreateInstance<CardData>();
        push.cardName = "Shove";
        push.effect = CardEffect.Push;
        push.range = 1;
        push.pushDistance = 1;
        push.cooldown = 3;

        var pull = ScriptableObject.CreateInstance<CardData>();
        pull.cardName = "Hook";
        pull.effect = CardEffect.Pull;
        pull.range = 3;
        pull.pushDistance = 2;
        pull.cooldown = 3;

        return new[] { move, attack, push, pull };
    }

    private static CardData[] CreateDefaultEnemyCards()
    {
        var attack = ScriptableObject.CreateInstance<CardData>();
        attack.cardName = "Claw";
        attack.effect = CardEffect.Attack;
        attack.range = 1;
        attack.damage = 1;
        attack.cooldown = 1;

        var move = ScriptableObject.CreateInstance<CardData>();
        move.cardName = "Advance";
        move.effect = CardEffect.Move;
        move.range = 1;
        move.cooldown = 1;

        return new[] { attack, move };
    }
}
