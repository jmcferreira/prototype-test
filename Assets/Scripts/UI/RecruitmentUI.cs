using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Between-round recruitment UI for PVP. Each player picks up to 3 creatures
/// to recruit, paying gold. P1 recruits first, then P2.
/// Creatures spawn on hexes adjacent to the player's base tower.
/// </summary>
public class RecruitmentUI : MonoBehaviour
{
    private PvpTurnManager _turnManager;
    private HexGrid _grid;
    private List<TowerUnit> _towers;

    private GameObject _panel;
    private Text _titleText;
    private Text _goldText;
    private GameObject _buttonContainer;

    private int _recruitingPlayer; // 0 or 1
    private int _recruited;
    private const int MaxRecruits = 3;

    public void Init(PvpTurnManager turnManager, HexGrid grid, List<TowerUnit> towers)
    {
        _turnManager = turnManager;
        _grid = grid;
        _towers = towers;

        _panel = CreatePanel();
        _panel.SetActive(false);

        turnManager.OnRoundEnd += OnRoundEnd;
    }

    private void OnRoundEnd(int roundNumber)
    {
        var match = PvpMatchState.Current;
        if (match == null) return;
        match.RoundNumber = roundNumber;

        // P1 recruits first
        _recruitingPlayer = 0;
        _recruited = 0;
        ShowForPlayer(0);
    }

    private void ShowForPlayer(int playerIndex)
    {
        _recruitingPlayer = playerIndex;
        _recruited = 0;
        _panel.SetActive(true);

        var match = PvpMatchState.Current;
        var factionId = match.FactionIds[playerIndex];
        var faction = FactionLibrary.Get(factionId);

        _titleText.text = $"Player {playerIndex + 1} — Recruit ({faction.factionName})";
        RefreshButtons(faction, match, playerIndex);
    }

    private void RefreshButtons(FactionDef faction, PvpMatchState match, int playerIndex)
    {
        _goldText.text = $"Gold: {match.Gold[playerIndex]}";

        // Clear old buttons
        foreach (Transform child in _buttonContainer.transform)
            Destroy(child.gameObject);

        if (_recruited >= MaxRecruits)
        {
            CreateDoneMessage("Max recruits reached!");
            CreateDoneButton();
            return;
        }

        float yOffset = 50f;
        foreach (var entry in faction.creatures)
        {
            bool canAfford = match.Gold[playerIndex] >= entry.goldCost;
            CreateCreatureButton(entry, playerIndex, canAfford, yOffset);
            yOffset -= 80f;
        }

        // Skip / Done button
        CreateDoneButton();
    }

    private void CreateCreatureButton(CreatureEntry entry, int playerIndex, bool canAfford, float yOffset)
    {
        var btnGo = new GameObject($"Btn_{entry.unitDef.displayName}");
        btnGo.transform.SetParent(_buttonContainer.transform, false);

        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(0, yOffset);
        btnRt.sizeDelta = new Vector2(450, 65);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = canAfford ? new Color(0.2f, 0.35f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable = canAfford;

        btn.onClick.AddListener(() => RecruitCreature(entry, playerIndex));

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 0);
        textRt.offsetMax = new Vector2(-10, 0);
        var text = textGo.AddComponent<Text>();
        text.text = $"{entry.unitDef.displayName} ({entry.unitDef.maxHP} HP) — {entry.goldCost}g";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }

    private void RecruitCreature(CreatureEntry entry, int playerIndex)
    {
        var match = PvpMatchState.Current;
        if (!match.SpendGold(playerIndex, entry.goldCost)) return;

        // Find spawn position adjacent to base tower
        HexCoord basePos = playerIndex == 0
            ? match.Arena.p1BaseTowerPos
            : match.Arena.p2BaseTowerPos;

        HexCoord spawnPos = FindFreeAdjacentHex(basePos);

        // Spawn the creature as EnemyUnit (AI-controlled)
        var go = new GameObject();
        var creature = go.AddComponent<EnemyUnit>();
        creature.Init(Team.Enemy, spawnPos, _grid,
                      entry.unitDef.displayName, entry.unitDef.maxHP, entry.unitDef.iconLetter);
        creature.InitCards(CardLibrary.GetDeck(entry.unitDef.deckId));
        creature.SetPassive(PassiveFactory.Create(entry.unitDef.passiveType));
        creature.SetAlliance(playerIndex);
        creature.SetTurnManager(_turnManager);
        creature.SetFateDeck(new FateDeck(FateCardLibrary.GetBasicEnemyDeck()));

        // Alliance-based chip colors
        if (playerIndex == 0)
            creature.SetChipColors(new Color(0.2f, 0.7f, 0.6f), new Color(0.1f, 0.35f, 0.3f));
        else
            creature.SetChipColors(new Color(0.8f, 0.5f, 0.15f), new Color(0.4f, 0.25f, 0.08f));

        // Register with turn manager and match state
        _turnManager.AddCreature(creature);
        match.Creatures[playerIndex].Add(creature);

        _recruited++;
        Debug.Log($"P{playerIndex + 1} recruited {entry.unitDef.displayName} at {spawnPos} ({entry.goldCost}g)");
        BattleLog.AddAction($"P{playerIndex + 1} recruited {entry.unitDef.displayName}");

        // Refresh UI
        var faction = FactionLibrary.Get(match.FactionIds[playerIndex]);
        RefreshButtons(faction, match, playerIndex);
    }

    private HexCoord FindFreeAdjacentHex(HexCoord center)
    {
        var allUnits = _turnManager.GetAliveUnits();
        var occupied = new HashSet<HexCoord>();
        foreach (var u in allUnits)
            occupied.Add(u.Coord);

        // Try all 6 neighbors
        for (int i = 0; i < 6; i++)
        {
            var neighbor = center.Neighbor(i);
            if (_grid.TryGetTile(neighbor, out _) && !occupied.Contains(neighbor))
                return neighbor;
        }

        // Try 2-ring neighbors
        for (int i = 0; i < 6; i++)
        {
            var n1 = center.Neighbor(i);
            for (int j = 0; j < 6; j++)
            {
                var n2 = n1.Neighbor(j);
                if (_grid.TryGetTile(n2, out _) && !occupied.Contains(n2))
                    return n2;
            }
        }

        // Fallback to center (shouldn't happen)
        return center;
    }

    private void OnDoneClicked()
    {
        if (_recruitingPlayer == 0)
        {
            // P2's turn to recruit
            ShowForPlayer(1);
        }
        else
        {
            // Both done — resume the match
            _panel.SetActive(false);
            _turnManager.ResumeAfterRecruitment();
        }
    }

    private void CreateDoneMessage(string msg)
    {
        var msgGo = new GameObject("DoneMsg");
        msgGo.transform.SetParent(_buttonContainer.transform, false);
        var msgRt = msgGo.AddComponent<RectTransform>();
        msgRt.anchorMin = new Vector2(0.5f, 0.5f);
        msgRt.anchorMax = new Vector2(0.5f, 0.5f);
        msgRt.anchoredPosition = new Vector2(0, 60);
        msgRt.sizeDelta = new Vector2(400, 40);
        var text = msgGo.AddComponent<Text>();
        text.text = msg;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.8f, 0.3f);
    }

    private void CreateDoneButton()
    {
        var btnGo = new GameObject("DoneBtn");
        btnGo.transform.SetParent(_buttonContainer.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(0, -220);
        btnRt.sizeDelta = new Vector2(200, 50);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.4f, 0.3f, 0.2f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnDoneClicked);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = _recruited > 0 ? "Done" : "Skip";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private GameObject CreatePanel()
    {
        var panel = new GameObject("RecruitPanel");
        panel.transform.SetParent(transform, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.82f);
        titleRt.anchorMax = new Vector2(0.5f, 0.82f);
        titleRt.sizeDelta = new Vector2(600, 50);
        _titleText = titleGo.AddComponent<Text>();
        _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _titleText.fontSize = 32;
        _titleText.alignment = TextAnchor.MiddleCenter;
        _titleText.color = Color.white;

        // Gold display
        var goldGo = new GameObject("GoldText");
        goldGo.transform.SetParent(panel.transform, false);
        var goldRt = goldGo.AddComponent<RectTransform>();
        goldRt.anchorMin = new Vector2(0.5f, 0.73f);
        goldRt.anchorMax = new Vector2(0.5f, 0.73f);
        goldRt.sizeDelta = new Vector2(300, 35);
        _goldText = goldGo.AddComponent<Text>();
        _goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _goldText.fontSize = 24;
        _goldText.alignment = TextAnchor.MiddleCenter;
        _goldText.color = new Color(1f, 0.85f, 0.3f);

        // Button container
        _buttonContainer = new GameObject("Buttons");
        _buttonContainer.transform.SetParent(panel.transform, false);
        var containerRt = _buttonContainer.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.4f);
        containerRt.anchorMax = new Vector2(0.5f, 0.4f);
        containerRt.sizeDelta = new Vector2(500, 400);

        return panel;
    }
}
