using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        // --- Player ---
        var playerCoord = new HexCoord(1, 1);
        var playerGo = new GameObject();
        var player = playerGo.AddComponent<PlayerUnit>();
        player.Init(Team.Player, playerCoord, hexGrid, "Player 1", 3, "P1");

        // Load from Inspector or fall back to Resources/Cards
        if (playerCards == null || playerCards.Length == 0)
            playerCards = Resources.LoadAll<CardData>("Cards/Player");
        if (playerCards.Length == 0) playerCards = CreateDefaultPlayerCards();

        player.InitHand(playerCards);

        // --- Enemies ---
        var spiderCards = CreateSpiderCards();
        var orcCards = CreateOrcCards();

        // Spider 1
        var spider1Go = new GameObject();
        var spider1 = spider1Go.AddComponent<EnemyUnit>();
        spider1.Init(Team.Enemy, new HexCoord(4, 0), hexGrid, "Spider 1", 3, "S");
        spider1.InitCards(spiderCards);

        // Spider 2
        var spider2Go = new GameObject();
        var spider2 = spider2Go.AddComponent<EnemyUnit>();
        spider2.Init(Team.Enemy, new HexCoord(5, -1), hexGrid, "Spider 2", 3, "S");
        spider2.InitCards(CreateSpiderCards()); // separate instances

        // Orc
        var orcGo = new GameObject();
        var orc = orcGo.AddComponent<EnemyUnit>();
        orc.Init(Team.Enemy, new HexCoord(3, 3), hexGrid, "Orc", 6, "O");
        orc.InitCards(orcCards);

        // --- HexInteraction ---
        var interactionGo = new GameObject("HexInteraction");
        var hexInteraction = interactionGo.AddComponent<HexInteraction>();
        player.SetHexInteraction(hexInteraction);

        // --- Shared UI canvas ---
        var uiGo = new GameObject("GameUI");
        var canvas = uiGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        uiGo.AddComponent<CanvasScaler>();
        uiGo.AddComponent<GraphicRaycaster>();

        // Hand UI (card bar at bottom)
        var handUIGo = new GameObject("HandUI");
        handUIGo.transform.SetParent(uiGo.transform, false);
        var handRect = handUIGo.AddComponent<RectTransform>();
        handRect.anchorMin = Vector2.zero;
        handRect.anchorMax = Vector2.one;
        handRect.offsetMin = Vector2.zero;
        handRect.offsetMax = Vector2.zero;
        var handUI = handUIGo.AddComponent<HandUI>();
        handUI.Init(player.Hand);
        player.SetHandUI(handUI);

        // --- Turn manager ---
        var turnManagerGo = new GameObject("TurnManager");
        var turnManager = turnManagerGo.AddComponent<TurnManager>();
        player.SetTurnManager(turnManager);
        spider1.SetTurnManager(turnManager);
        spider2.SetTurnManager(turnManager);
        orc.SetTurnManager(turnManager);

        // --- Side panels ---
        // Player on the left
        var playerPanelGo = new GameObject("PlayerInfoPanel");
        playerPanelGo.transform.SetParent(uiGo.transform, false);
        var playerPanel = playerPanelGo.AddComponent<UnitInfoPanel>();
        playerPanel.Init(player, turnManager, true);

        // Enemies stacked on the right
        var enemies = new EnemyUnit[] { spider1, spider2, orc };
        for (int i = 0; i < enemies.Length; i++)
        {
            var panelGo = new GameObject($"EnemyPanel_{enemies[i].DisplayName}");
            panelGo.transform.SetParent(uiGo.transform, false);
            var panel = panelGo.AddComponent<UnitInfoPanel>();
            panel.Init(enemies[i], turnManager, false, i);
        }

        // Turn banner (top of screen)
        var bannerGo = new GameObject("TurnBanner");
        bannerGo.transform.SetParent(uiGo.transform, false);
        var banner = bannerGo.AddComponent<TurnBannerUI>();
        banner.Init(turnManager);

        // --- Start the game ---
        var turnOrder = new List<Unit> { player, spider1, spider2, orc };
        turnManager.Begin(turnOrder);

        // Camera controls (zoom + pan)
        if (Camera.main != null && Camera.main.GetComponent<CameraController>() == null)
            Camera.main.gameObject.AddComponent<CameraController>();

        Debug.Log($"Player at {playerCoord}, Spider 1 at (4,0), Spider 2 at (5,-1), Orc at (3,3)");
    }

    // ── Default card definitions ────────────────────────────────────────

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

    private static CardData[] CreateSpiderCards()
    {
        // Card 1 — Web Leap: Jump 2
        var webLeap = ScriptableObject.CreateInstance<CardData>();
        webLeap.cardName = "Web Leap";
        webLeap.actions = new[]
        {
            new CardAction { effect = CardEffect.Jump, range = 2 },
        };
        webLeap.cooldown = 1;

        // Card 2 — Venomous Bite: Attack 1 Range 3 → Poison 1 Range 3
        var venomBite = ScriptableObject.CreateInstance<CardData>();
        venomBite.cardName = "Venomous Bite";
        venomBite.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 1 },
            new CardAction { effect = CardEffect.Status, range = 3, statusEffect = StatusEffectType.Poison, statusStacks = 1 },
        };
        venomBite.cooldown = 2;

        return new[] { webLeap, venomBite };
    }

    private static CardData[] CreateOrcCards()
    {
        // Card 1 — War March: Move 2 → Swift 1 (self)
        var warMarch = ScriptableObject.CreateInstance<CardData>();
        warMarch.cardName = "War March";
        warMarch.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Status, range = 0, statusEffect = StatusEffectType.Swift, statusStacks = 1, targetSelf = true },
        };
        warMarch.cooldown = 1;

        // Card 2 — Cleave: AttackAoE 3 Range 1 (all adjacent targets)
        var cleave = ScriptableObject.CreateInstance<CardData>();
        cleave.cardName = "Cleave";
        cleave.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackAoE, range = 1, damage = 3 },
        };
        cleave.cooldown = 2;

        return new[] { warMarch, cleave };
    }
}
