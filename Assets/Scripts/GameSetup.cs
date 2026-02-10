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
        player.Init(Team.Player, playerCoord, hexGrid, "Player 1");

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
        enemy.Init(Team.Enemy, enemyCoord, hexGrid, "Spider");
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
        // Card 1 — Dash Strike: Dash 2 (mandatory) → Attack 1
        var dashStrike = ScriptableObject.CreateInstance<CardData>();
        dashStrike.cardName = "Dash Strike";
        dashStrike.actions = new[]
        {
            new CardAction { effect = CardEffect.Dash, range = 2, mandatory = true },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 1 },
        };
        dashStrike.cooldown = 2;

        // Card 2 — Fireshot: Attack 1 Range 3 → Burn 1 Range 3
        var fireshot = ScriptableObject.CreateInstance<CardData>();
        fireshot.cardName = "Fireshot";
        fireshot.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 1 },
            new CardAction { effect = CardEffect.Status, range = 3, statusEffect = StatusEffectType.Burn, statusStacks = 1 },
        };
        fireshot.cooldown = 2;

        // Card 3 — Exorcize: Attack 1 → Push 2
        var exorcize = ScriptableObject.CreateInstance<CardData>();
        exorcize.cardName = "Exorcize";
        exorcize.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 1 },
            new CardAction { effect = CardEffect.Push, range = 1, pushDistance = 2 },
        };
        exorcize.cooldown = 2;

        // Card 4 — Grappling Hook: Pull 2 Range 3 → Heal 1
        var hook = ScriptableObject.CreateInstance<CardData>();
        hook.cardName = "Grappling Hook";
        hook.actions = new[]
        {
            new CardAction { effect = CardEffect.Pull, range = 3, pushDistance = 2 },
            new CardAction { effect = CardEffect.Heal, range = 1, damage = 1 },
        };
        hook.cooldown = 3;

        return new[] { dashStrike, fireshot, exorcize, hook };
    }

    private static CardData[] CreateDefaultEnemyCards()
    {
        // Claw: Attack 1
        var claw = ScriptableObject.CreateInstance<CardData>();
        claw.cardName = "Claw";
        claw.actions = new[] { new CardAction { effect = CardEffect.Attack, range = 1, damage = 1 } };
        claw.cooldown = 1;

        // Advance: Move 1
        var advance = ScriptableObject.CreateInstance<CardData>();
        advance.cardName = "Advance";
        advance.actions = new[] { new CardAction { effect = CardEffect.Move, range = 1 } };
        advance.cooldown = 1;

        // Poison Spit: Poison 1 Range 2
        var spit = ScriptableObject.CreateInstance<CardData>();
        spit.cardName = "Poison Spit";
        spit.actions = new[]
        {
            new CardAction { effect = CardEffect.Status, range = 2, statusEffect = StatusEffectType.Poison, statusStacks = 1 },
        };
        spit.cooldown = 2;

        return new[] { claw, advance, spit };
    }
}
