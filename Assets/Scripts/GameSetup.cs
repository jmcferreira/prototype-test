using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bootstraps the game from a ScenarioDef: spawns units, builds UI, starts the turn loop.
/// All content (units, cards, passives, loot) comes from data definitions.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;

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
        FateCombatContext.Clear();

        // Load scenario definition
        var scenario = Scenarios.CreateTestScenario();

        // Reconfigure grid if scenario requires different dimensions
        if (scenario.gridColumns != hexGrid.Columns || scenario.gridRows != hexGrid.Rows)
            hexGrid.Regenerate(scenario.gridColumns, scenario.gridRows);

        // --- Spawn units from scenario data ---
        var player = SpawnPlayer(scenario);
        var enemies = SpawnEnemies(scenario);

        // --- Fate decks ---
        player.SetFateDeck(new FateDeck(FateCardLibrary.GetStarterDeck()));
        foreach (var enemy in enemies)
            enemy.SetFateDeck(new FateDeck(FateCardLibrary.GetBasicEnemyDeck()));

        // --- Token manager ---
        var tokenManagerGo = new GameObject("TokenManager");
        tokenManagerGo.AddComponent<TokenManager>();

        // --- Enemy defeat: XP reward + loot drop ---
        SetupEnemyRewards(enemies);

        // --- Initial loot tokens ---
        SpawnInitialLoot(hexGrid, player, enemies, scenario.initialLootGold);

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
        foreach (var enemy in enemies)
            enemy.SetTurnManager(turnManager);

        // --- Side panels ---
        // Player on the left
        var playerPanelGo = new GameObject("PlayerInfoPanel");
        playerPanelGo.transform.SetParent(uiGo.transform, false);
        var playerPanel = playerPanelGo.AddComponent<UnitInfoPanel>();
        playerPanel.Init(player, turnManager, true);

        // Enemies stacked on the right
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

        // Fate card selection UI (modal, hidden until draw)
        var fateUIGo = new GameObject("FateSelectionUI");
        fateUIGo.transform.SetParent(uiGo.transform, false);
        var fateSelectionUI = fateUIGo.AddComponent<FateSelectionUI>();
        fateSelectionUI.Init(uiGo.transform);

        // Fate manager (singleton orchestrator)
        var fateManagerGo = new GameObject("FateManager");
        var fateManager = fateManagerGo.AddComponent<FateManager>();
        fateManager.SetSelectionUI(fateSelectionUI);

        // --- Start the game ---
        var turnOrder = new List<Unit> { player };
        turnOrder.AddRange(enemies);
        turnManager.Begin(turnOrder);

        // Camera controls (zoom + pan)
        if (Camera.main != null && Camera.main.GetComponent<CameraController>() == null)
            Camera.main.gameObject.AddComponent<CameraController>();

        Debug.Log($"Scenario '{scenario.scenarioName}' loaded — {enemies.Length} enemies, threat {scenario.threatLevel}");
    }

    // ── Unit spawning ────────────────────────────────────────────────────

    private PlayerUnit SpawnPlayer(ScenarioDef scenario)
    {
        var def = scenario.playerDef;
        var go = new GameObject();
        var player = go.AddComponent<PlayerUnit>();
        player.Init(Team.Player, scenario.playerSpawn, hexGrid, def.displayName, def.maxHP, def.iconLetter);
        player.InitHand(CardLibrary.GetDeck(def.deckId));
        player.SetPassive(PassiveFactory.Create(def.passiveType));
        return player;
    }

    private EnemyUnit[] SpawnEnemies(ScenarioDef scenario)
    {
        var enemies = new EnemyUnit[scenario.enemySpawns.Length];
        for (int i = 0; i < scenario.enemySpawns.Length; i++)
        {
            var spawn = scenario.enemySpawns[i];
            var def = spawn.unitDef;

            var go = new GameObject();
            var enemy = go.AddComponent<EnemyUnit>();
            enemy.Init(Team.Enemy, spawn.position, hexGrid,
                       def.displayName, def.maxHP, def.iconLetter,
                       xpReward: def.xpReward, goldReward: def.goldReward);
            enemy.InitCards(CardLibrary.GetDeck(def.deckId));
            enemy.SetPassive(PassiveFactory.Create(def.passiveType));

            enemies[i] = enemy;
        }
        return enemies;
    }

    private void SetupEnemyRewards(EnemyUnit[] enemies)
    {
        foreach (var enemy in enemies)
        {
            var e = enemy; // capture for closure
            e.OnDefeated += (unit) =>
            {
                if (unit.XPReward > 0)
                {
                    CurrencyManager.AddXP(unit.XPReward);
                    BattleLog.AddAction($"Gained {unit.XPReward} XP");
                    Debug.Log($"{unit.DisplayName} defeated — gained {unit.XPReward} XP");
                }
                if (unit.GoldReward > 0 && TokenManager.Instance != null)
                {
                    var loot = new LootToken(unit.GoldReward);
                    TokenManager.Instance.PlaceToken(loot, unit.Coord, hexGrid);
                    BattleLog.AddAction($"{unit.DisplayName} dropped {unit.GoldReward} Gold loot");
                    Debug.Log($"{unit.DisplayName} dropped loot worth {unit.GoldReward} Gold at {unit.Coord}");
                }
            };
        }
    }

    // ── Initial loot placement ───────────────────────────────────────────

    private static void SpawnInitialLoot(HexGrid grid, Unit player, EnemyUnit[] enemies, int goldTotal)
    {
        var occupied = new HashSet<HexCoord> { player.Coord };
        foreach (var e in enemies)
            occupied.Add(e.Coord);

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

        // Place loot tokens: mix of 1-Gold and 2-Gold tokens
        int goldRemaining = goldTotal;
        int placed = 0;
        while (goldRemaining > 0 && placed < candidates.Count)
        {
            var coord = candidates[placed];
            if (TokenManager.Instance.HasToken(coord))
            {
                placed++;
                continue;
            }

            int value = goldRemaining >= 2 && placed % 2 == 0 ? 2 : 1;
            if (value > goldRemaining) value = goldRemaining;

            var loot = new LootToken(value);
            TokenManager.Instance.PlaceToken(loot, coord, grid);
            goldRemaining -= value;
            placed++;
        }
    }
}
