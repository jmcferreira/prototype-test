using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bootstraps the game from a ScenarioDef: spawns units, builds UI, starts the turn loop.
/// Supports multiple player characters (party). All content comes from data definitions.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;

    private void Start()
    {
        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();

        // If no game mode selected yet, show main menu
        if (GameModeState.CurrentMode == GameMode.None)
        {
            ShowMainMenu();
            return;
        }

        Invoke(nameof(Setup), 0f);
    }

    private void ShowMainMenu()
    {
        var menuCanvas = new GameObject("MenuCanvas");
        var canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        menuCanvas.AddComponent<CanvasScaler>();
        menuCanvas.AddComponent<GraphicRaycaster>();

        var menuGo = new GameObject("MainMenu");
        menuGo.transform.SetParent(menuCanvas.transform, false);
        var menu = menuGo.AddComponent<MainMenuUI>();
        menu.Init();
        menu.OnModeSelected += (mode) =>
        {
            Destroy(menuCanvas);
            Invoke(nameof(Setup), 0f);
        };
    }

    private void Setup()
    {
        // Clear static state from previous play sessions
        BattleLog.Clear();
        CurrencyManager.Clear();
        FateCombatContext.Clear();

        if (GameModeState.CurrentMode == GameMode.PVP)
        {
            SetupPVP();
            return;
        }

        SetupCampaign();
    }

    private void SetupCampaign()
    {
        // Ensure ScenarioManager singleton exists (persists across scene reloads)
        if (ScenarioManager.Instance == null)
        {
            var smGo = new GameObject("ScenarioManager");
            smGo.AddComponent<ScenarioManager>();
        }

        // Initialize run if needed (first load or after run complete)
        if (RunState.Current == null)
        {
            var defaultParty = new[]
            {
                new UnitDef
                {
                    displayName = "Ranger",
                    maxHP = 10,
                    iconLetter = "R",
                    deckId = "player_default",
                    passiveType = PassiveType.Setup,
                },
                new UnitDef
                {
                    displayName = "Guardian",
                    maxHP = 12,
                    iconLetter = "G",
                    deckId = "player_guardian",
                    passiveType = PassiveType.Guardian,
                },
            };
            RunState.NewRun(defaultParty);
        }

        var run = RunState.Current;
        var scenario = run.GetNextScenario() ?? ScenarioPool.GetScenario(1);
        run.ChosenNextScenario = null; // clear after loading

        // Reconfigure grid if scenario requires different dimensions
        if (scenario.gridColumns != hexGrid.Columns || scenario.gridRows != hexGrid.Rows)
            hexGrid.Regenerate(scenario.gridColumns, scenario.gridRows);

        // --- Spawn player party from run state ---
        var players = SpawnParty(run, scenario);

        // Apply relic bonuses from the run to all players
        ApplyRelics(players, run);

        var enemies = SpawnEnemies(scenario);

        // --- Fate decks ---
        for (int i = 0; i < players.Length; i++)
            players[i].SetFateDeck(new FateDeck(run.PlayerFateDecks[i]));
        foreach (var enemy in enemies)
            enemy.SetFateDeck(new FateDeck(FateCardLibrary.GetBasicEnemyDeck()));

        // --- Token manager ---
        var tokenManagerGo = new GameObject("TokenManager");
        tokenManagerGo.AddComponent<TokenManager>();

        // --- Enemy defeat: XP reward + loot drop ---
        SetupEnemyRewards(enemies);

        // --- Initial loot tokens ---
        SpawnInitialLoot(hexGrid, players, enemies, scenario.initialLootGold);

        // --- HexInteraction (shared by all players) ---
        var interactionGo = new GameObject("HexInteraction");
        var hexInteraction = interactionGo.AddComponent<HexInteraction>();
        foreach (var player in players)
            player.SetHexInteraction(hexInteraction);

        // --- Shared UI canvas ---
        var uiGo = new GameObject("GameUI");
        var canvas = uiGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        uiGo.AddComponent<CanvasScaler>();
        uiGo.AddComponent<GraphicRaycaster>();

        // Hand UI (card bar at bottom — switches between active player's hand)
        var handUIGo = new GameObject("HandUI");
        handUIGo.transform.SetParent(uiGo.transform, false);
        var handRect = handUIGo.AddComponent<RectTransform>();
        handRect.anchorMin = Vector2.zero;
        handRect.anchorMax = Vector2.one;
        handRect.offsetMin = Vector2.zero;
        handRect.offsetMax = Vector2.zero;
        var handUI = handUIGo.AddComponent<HandUI>();
        handUI.Init(players[0].Hand);
        foreach (var player in players)
            player.SetHandUI(handUI);

        // --- Turn manager ---
        var turnManagerGo = new GameObject("TurnManager");
        var turnManager = turnManagerGo.AddComponent<TurnManager>();
        foreach (var player in players)
            player.SetTurnManager(turnManager);
        foreach (var enemy in enemies)
            enemy.SetTurnManager(turnManager);

        // --- Side panels ---
        // Players stacked on the left
        for (int i = 0; i < players.Length; i++)
        {
            var panelGo = new GameObject($"PlayerPanel_{players[i].DisplayName}");
            panelGo.transform.SetParent(uiGo.transform, false);
            var panel = panelGo.AddComponent<UnitInfoPanel>();
            panel.Init(players[i], turnManager, true, i);
        }

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

        // Reward screen (hidden until victory transition)
        var rewardGo = new GameObject("RewardScreen");
        rewardGo.transform.SetParent(uiGo.transform, false);
        var rewardScreen = rewardGo.AddComponent<RewardScreenUI>();
        rewardScreen.Init();

        // Scenario selection screen (hidden until between-scenario transition)
        var scenarioSelGo = new GameObject("ScenarioSelection");
        scenarioSelGo.transform.SetParent(uiGo.transform, false);
        var scenarioSelection = scenarioSelGo.AddComponent<ScenarioSelectionUI>();
        scenarioSelection.Init();

        // Game-over modal (hidden until victory/defeat)
        var modalGo = new GameObject("GameOverModal");
        modalGo.transform.SetParent(uiGo.transform, false);
        var modal = modalGo.AddComponent<GameOverModalUI>();
        modal.Init(turnManager, players, rewardScreen, scenarioSelection);

        // Battle log (scrollable panel below player panels)
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
        var turnOrder = new List<Unit>();
        turnOrder.AddRange(players);
        turnOrder.AddRange(enemies);
        turnManager.Begin(turnOrder);

        // Camera controls (zoom + pan)
        if (Camera.main != null && Camera.main.GetComponent<CameraController>() == null)
            Camera.main.gameObject.AddComponent<CameraController>();

        Debug.Log($"Scenario '{scenario.scenarioName}' loaded — {players.Length} players, {enemies.Length} enemies" +
                  $", threat {scenario.threatLevel} | Stage {run.ScenariosCompleted + 1}");
    }

    // ── Relic application ──────────────────────────────────────────────────

    private static void ApplyRelics(PlayerUnit[] players, RunState run)
    {
        if (run.Relics.Count == 0) return;

        int blockBonus = run.GetRelicTotal(RelicEffect.StartingBlock);
        int dmgBonus = run.GetRelicTotal(RelicEffect.DamageBonus);
        int goldBonus = run.GetRelicTotal(RelicEffect.GoldBonus);

        foreach (var player in players)
        {
            if (blockBonus > 0) player.RelicStartingBlock = blockBonus;
            if (dmgBonus > 0) player.RelicDamageBonus = dmgBonus;
        }
        if (goldBonus > 0) CurrencyManager.AddGold(goldBonus);

        Debug.Log($"Relics applied: {run.Relics.Count} total" +
                  (blockBonus > 0 ? $" | Block +{blockBonus}/turn" : "") +
                  (dmgBonus > 0 ? $" | Dmg +{dmgBonus}" : "") +
                  (goldBonus > 0 ? $" | Gold +{goldBonus}" : ""));
    }

    // ── Unit spawning ────────────────────────────────────────────────────

    private PlayerUnit[] SpawnParty(RunState run, ScenarioDef scenario)
    {
        var players = new PlayerUnit[run.PartySize];
        for (int i = 0; i < run.PartySize; i++)
        {
            var def = run.PlayerDefs[i];
            var spawnPos = i < scenario.playerSpawns.Length
                ? scenario.playerSpawns[i]
                : scenario.playerSpawns[0]; // fallback to first spawn

            var go = new GameObject();
            var player = go.AddComponent<PlayerUnit>();
            player.Init(Team.Player, spawnPos, hexGrid, def.displayName, def.maxHP, def.iconLetter);
            player.InitHand(CardLibrary.GetDeck(def.deckId));
            player.SetPassive(PassiveFactory.Create(def.passiveType));

            // Restore HP from run state (may be less than max if continuing a run)
            if (run.ScenariosCompleted > 0)
                player.SetCurrentHP(run.PlayerCurrentHPs[i]);

            players[i] = player;
        }
        return players;
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
            var e = enemy;
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

    private static void SpawnInitialLoot(HexGrid grid, PlayerUnit[] players, EnemyUnit[] enemies, int goldTotal)
    {
        var occupied = new HashSet<HexCoord>();
        foreach (var p in players)
            occupied.Add(p.Coord);
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

    // ── PVP setup (filled in Phase 6) ─────────────────────────────────────

    private void SetupPVP()
    {
        Debug.Log("PVP mode — setup not yet implemented.");
    }
}
