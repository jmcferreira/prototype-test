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
        // Clear static state from previous play sessions
        BattleLog.Clear();
        CurrencyManager.Clear();

        // --- Player ---
        var playerCoord = new HexCoord(1, 1);
        var playerGo = new GameObject();
        var player = playerGo.AddComponent<PlayerUnit>();
        player.Init(Team.Player, playerCoord, hexGrid, "Player 1", 10, "P1");

        // Load from Inspector or fall back to Resources/Cards
        if (playerCards == null || playerCards.Length == 0)
            playerCards = Resources.LoadAll<CardData>("Cards/Player");
        if (playerCards.Length == 0) playerCards = CreateDefaultPlayerCards();

        player.InitHand(playerCards);

        // --- Enemies ---
        var spiderCards = CreateSpiderCards();
        var cultistCards = CreateCultistCards();

        // Spider 1 (2 XP, 1 Gold loot)
        var spider1Go = new GameObject();
        var spider1 = spider1Go.AddComponent<EnemyUnit>();
        spider1.Init(Team.Enemy, new HexCoord(4, 0), hexGrid, "Spider 1", 3, "S", xpReward: 2, goldReward: 1);
        spider1.InitCards(spiderCards);

        // Spider 2 (2 XP, 1 Gold loot)
        var spider2Go = new GameObject();
        var spider2 = spider2Go.AddComponent<EnemyUnit>();
        spider2.Init(Team.Enemy, new HexCoord(5, -1), hexGrid, "Spider 2", 3, "S", xpReward: 2, goldReward: 1);
        spider2.InitCards(CreateSpiderCards()); // separate instances

        // Cultist (4 XP, 2 Gold loot)
        var cultistGo = new GameObject();
        var cultist = cultistGo.AddComponent<EnemyUnit>();
        cultist.Init(Team.Enemy, new HexCoord(3, 3), hexGrid, "Cultist", 8, "C", xpReward: 4, goldReward: 2);
        cultist.InitCards(cultistCards);

        // --- Passive skills ---
        player.SetPassive(new SetupPassive());
        spider1.SetPassive(new NestingPassive());
        spider2.SetPassive(new NestingPassive());
        cultist.SetPassive(new DarkPactPassive());

        // --- Token manager ---
        var tokenManagerGo = new GameObject("TokenManager");
        tokenManagerGo.AddComponent<TokenManager>();

        // --- Enemy defeat: XP reward + loot drop ---
        var allEnemies = new EnemyUnit[] { spider1, spider2, cultist };
        foreach (var enemy in allEnemies)
        {
            var e = enemy; // capture for closure
            e.OnDefeated += (unit) =>
            {
                // Grant XP
                if (unit.XPReward > 0)
                {
                    CurrencyManager.AddXP(unit.XPReward);
                    BattleLog.AddAction($"Gained {unit.XPReward} XP");
                    Debug.Log($"{unit.DisplayName} defeated — gained {unit.XPReward} XP");
                }
                // Drop loot token at death location
                if (unit.GoldReward > 0 && TokenManager.Instance != null)
                {
                    var loot = new LootToken(unit.GoldReward);
                    TokenManager.Instance.PlaceToken(loot, unit.Coord, hexGrid);
                    BattleLog.AddAction($"{unit.DisplayName} dropped {unit.GoldReward} Gold loot");
                    Debug.Log($"{unit.DisplayName} dropped loot worth {unit.GoldReward} Gold at {unit.Coord}");
                }
            };
        }

        // --- Initial loot tokens (5 Gold spread across the map) ---
        SpawnInitialLoot(hexGrid, player, allEnemies);

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
        cultist.SetTurnManager(turnManager);

        // --- Side panels ---
        // Player on the left
        var playerPanelGo = new GameObject("PlayerInfoPanel");
        playerPanelGo.transform.SetParent(uiGo.transform, false);
        var playerPanel = playerPanelGo.AddComponent<UnitInfoPanel>();
        playerPanel.Init(player, turnManager, true);

        // Enemies stacked on the right
        var enemies = new EnemyUnit[] { spider1, spider2, cultist };
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

        // Game-over modal (hidden until victory/defeat)
        var modalGo = new GameObject("GameOverModal");
        modalGo.transform.SetParent(uiGo.transform, false);
        var modal = modalGo.AddComponent<GameOverModalUI>();
        modal.Init(turnManager);

        // Battle log (scrollable panel below player panel)
        var battleLogGo = new GameObject("BattleLog");
        battleLogGo.transform.SetParent(uiGo.transform, false);
        var battleLogUI = battleLogGo.AddComponent<BattleLogUI>();
        battleLogUI.Init();

        // Currency display (top-left)
        var currencyGo = new GameObject("CurrencyUI");
        currencyGo.transform.SetParent(uiGo.transform, false);
        var currencyUI = currencyGo.AddComponent<CurrencyUI>();
        currencyUI.Init();

        // --- Start the game ---
        var turnOrder = new List<Unit> { player, spider1, spider2, cultist };
        turnManager.Begin(turnOrder);

        // Camera controls (zoom + pan)
        if (Camera.main != null && Camera.main.GetComponent<CameraController>() == null)
            Camera.main.gameObject.AddComponent<CameraController>();

        Debug.Log($"Player at {playerCoord}, Spider 1 at (4,0), Spider 2 at (5,-1), Cultist at (3,3)");
    }

    // ── Initial loot placement ─────────────────────────────────────────

    /// <summary>
    /// Scatter loot tokens worth a total of 5 Gold across random empty hexes.
    /// Avoids hexes occupied by units or existing tokens.
    /// </summary>
    private static void SpawnInitialLoot(HexGrid grid, Unit player, EnemyUnit[] enemies)
    {
        // Collect occupied hexes (units)
        var occupied = new HashSet<HexCoord> { player.Coord };
        foreach (var e in enemies)
            occupied.Add(e.Coord);

        // Build list of candidate hexes
        var candidates = new List<HexCoord>();
        foreach (var kvp in grid.Tiles)
        {
            if (!occupied.Contains(kvp.Key))
                candidates.Add(kvp.Key);
        }

        // Shuffle candidates
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        // Place loot tokens totalling 5 Gold: mix of 1-Gold and 2-Gold tokens
        int goldRemaining = 5;
        int placed = 0;
        while (goldRemaining > 0 && placed < candidates.Count)
        {
            var coord = candidates[placed];
            if (TokenManager.Instance.HasToken(coord))
            {
                placed++;
                continue;
            }

            // Alternate 1 and 2 Gold tokens for variety
            int value = goldRemaining >= 2 && placed % 2 == 0 ? 2 : 1;
            if (value > goldRemaining) value = goldRemaining;

            var loot = new LootToken(value);
            TokenManager.Instance.PlaceToken(loot, coord, grid);
            goldRemaining -= value;
            placed++;
        }
    }

    // ── Default card definitions ────────────────────────────────────────

    private static CardData[] CreateDefaultPlayerCards()
    {
        // Card 1 — Volley: Move 2 → Attack 1 Range 2 (up to 2 targets)
        var volley = ScriptableObject.CreateInstance<CardData>();
        volley.cardName = "Volley";
        volley.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 2, damage = 1, maxTargets = 2 },
        };
        volley.cooldown = 0;

        // Card 2 — Firebolt: Line Attack 2 Range 3 + Burn 2 → Move 1
        var firebolt = ScriptableObject.CreateInstance<CardData>();
        firebolt.cardName = "Firebolt";
        firebolt.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackLine, range = 3, damage = 2,
                             statusEffect = StatusEffectType.Burn, statusStacks = 2 },
            new CardAction { effect = CardEffect.Move, range = 1 },
        };
        firebolt.cooldown = 1;

        // Card 3 — Quick Maneuver: Push All 1 Range 1 → Move 2
        var quickManeuver = ScriptableObject.CreateInstance<CardData>();
        quickManeuver.cardName = "Quick Maneuver";
        quickManeuver.actions = new[]
        {
            new CardAction { effect = CardEffect.PushAoE, range = 1, pushDistance = 1 },
            new CardAction { effect = CardEffect.Move, range = 2 },
        };
        quickManeuver.cooldown = 2;

        // Card 4 — Crippling Shot: Attack 2 Range 2 + Root 1 → Reduce CD on Headshot
        var cripplingShot = ScriptableObject.CreateInstance<CardData>();
        cripplingShot.cardName = "Crippling Shot";
        cripplingShot.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 2, damage = 2,
                             statusEffect = StatusEffectType.Root, statusStacks = 1 },
            new CardAction { effect = CardEffect.ReduceCooldown, targetCardIndex = 4 },
        };
        cripplingShot.cooldown = 3;

        // Card 5 — Headshot! (Ultimate): Move 2 → Attack 3 Range 3 (5 at max range)
        // Starts on cooldown 6
        var headshot = ScriptableObject.CreateInstance<CardData>();
        headshot.cardName = "Headshot!";
        headshot.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 3,
                             bonusDamage = 2, bonusDamageAtMaxRange = true },
        };
        headshot.cooldown = 6;
        headshot.startCooldown = 6;

        return new[] { volley, firebolt, quickManeuver, cripplingShot, headshot };
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

        // Card 2 — Venomous Bite: Attack 0 Range 3 → Poison 2 Range 3
        var venomBite = ScriptableObject.CreateInstance<CardData>();
        venomBite.cardName = "Venomous Bite";
        venomBite.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 0 },
            new CardAction { effect = CardEffect.Status, range = 3, statusEffect = StatusEffectType.Poison, statusStacks = 2 },
        };
        venomBite.cooldown = 2;

        return new[] { webLeap, venomBite };
    }

    private static CardData[] CreateCultistCards()
    {
        // Card 1 — Dark Bolt: Move 1 → Attack 2 Range 3 (consistent ranged pressure)
        var darkBolt = ScriptableObject.CreateInstance<CardData>();
        darkBolt.cardName = "Dark Bolt";
        darkBolt.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 1 },
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 2 },
        };
        darkBolt.cooldown = 0;

        // Card 2 — Cleave: AttackAoE 3 Range 1 → Strength 1 (self)
        var cleave = ScriptableObject.CreateInstance<CardData>();
        cleave.cardName = "Cleave";
        cleave.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackAoE, range = 1, damage = 3 },
            new CardAction { effect = CardEffect.Status, range = 0, statusEffect = StatusEffectType.Strength, statusStacks = 1, targetSelf = true },
        };
        cleave.cooldown = 1;

        return new[] { darkBolt, cleave };
    }
}
