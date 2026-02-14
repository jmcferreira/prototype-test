using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen modal offering 2-3 scenario choices for the next stage.
/// Shows scenario name, threat level, enemy composition, and loot preview.
/// Shown after the reward screen (if the next stage has multiple options).
/// </summary>
public class ScenarioSelectionUI : MonoBehaviour
{
    private Action<ScenarioDef> _onChosen;

    public void Init() { gameObject.SetActive(false); }

    /// <summary>
    /// Show the scenario selection with the given choices. Calls onChosen after pick.
    /// </summary>
    public void Show(ScenarioDef[] choices, Action<ScenarioDef> onChosen)
    {
        _onChosen = onChosen;
        gameObject.SetActive(true);
        BuildScreen(choices);
    }

    private void BuildScreen(ScenarioDef[] choices)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Full-screen overlay
        var overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;

        // Central container
        var containerGo = new GameObject("Container");
        containerGo.transform.SetParent(transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        float totalWidth = choices.Length * 200f + (choices.Length - 1) * 16f + 40f;
        containerRect.sizeDelta = new Vector2(Mathf.Max(totalWidth, 500f), 380f);

        var containerLayout = containerGo.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 16;
        containerLayout.childAlignment = TextAnchor.MiddleCenter;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(containerGo.transform, false);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 28;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.9f, 0.8f, 0.5f);
        titleTxt.text = "Choose Your Path";
        titleTxt.raycastTarget = false;
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 40f;

        // Stage indicator
        int nextStage = RunState.Current != null ? RunState.Current.ScenariosCompleted + 1 : 1;
        var stageGo = new GameObject("Stage");
        stageGo.transform.SetParent(containerGo.transform, false);
        var stageTxt = stageGo.AddComponent<Text>();
        stageTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stageTxt.fontSize = 14;
        stageTxt.alignment = TextAnchor.MiddleCenter;
        stageTxt.color = new Color(0.6f, 0.6f, 0.6f);
        stageTxt.text = $"Stage {nextStage}";
        stageTxt.raycastTarget = false;
        var stageLe = stageGo.AddComponent<LayoutElement>();
        stageLe.preferredHeight = 20f;

        // Cards row
        var rowGo = new GameObject("ScenariosRow");
        rowGo.transform.SetParent(containerGo.transform, false);
        var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 260f;

        foreach (var scenario in choices)
            BuildScenarioCard(rowGo, scenario);
    }

    private void BuildScenarioCard(GameObject parent, ScenarioDef scenario)
    {
        // Card background
        var cardGo = new GameObject($"Scenario_{scenario.scenarioName}");
        cardGo.transform.SetParent(parent.transform, false);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var cardLe = cardGo.AddComponent<LayoutElement>();
        cardLe.preferredWidth = 190f;
        cardLe.preferredHeight = 260f;

        // Clickable button
        var btn = cardGo.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        var captured = scenario;
        btn.onClick.AddListener(() => OnScenarioPicked(captured));

        // Border colored by threat
        Color borderColor = GetThreatColor(scenario.threatLevel);
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(cardGo.transform, false);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = borderColor;
        borderImg.raycastTarget = false;
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        // Inner panel
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(borderGo.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.13f, 0.97f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        // Vertical layout for content
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 10, 10);
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Threat level (skulls)
        string skulls = new string('\u2620', scenario.threatLevel); // ☠
        AddCardText(innerGo, "Threat", skulls, GetThreatColor(scenario.threatLevel), 16, FontStyle.Normal, 20f);

        // Scenario name
        AddCardText(innerGo, "Name", scenario.scenarioName, Color.white, 17, FontStyle.Bold, 36f);

        // Enemy preview
        string enemies = ScenarioPool.GetEnemyPreview(scenario);
        AddCardText(innerGo, "Enemies", enemies, new Color(0.85f, 0.6f, 0.6f), 13, FontStyle.Normal, 40f);

        // Enemy count
        string countInfo = $"{scenario.enemySpawns.Length} enemies";
        AddCardText(innerGo, "Count", countInfo, new Color(0.7f, 0.7f, 0.7f), 12, FontStyle.Italic, 18f);

        // Total HP
        int totalHP = 0;
        foreach (var spawn in scenario.enemySpawns)
            totalHP += spawn.unitDef.maxHP;
        AddCardText(innerGo, "HP", $"Total HP: {totalHP}", new Color(0.7f, 0.7f, 0.7f), 12, FontStyle.Normal, 18f);

        // Loot preview
        int totalXP = 0;
        foreach (var spawn in scenario.enemySpawns)
            totalXP += spawn.unitDef.xpReward;
        string lootInfo = $"Loot: {scenario.initialLootGold}g  |  XP: {totalXP}";
        AddCardText(innerGo, "Loot", lootInfo, new Color(1f, 0.85f, 0.3f), 12, FontStyle.Normal, 18f);
    }

    private void AddCardText(GameObject parent, string name, string text, Color color,
                             int fontSize, FontStyle style, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.text = text;
        txt.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void OnScenarioPicked(ScenarioDef scenario)
    {
        gameObject.SetActive(false);
        _onChosen?.Invoke(scenario);
    }

    private static Color GetThreatColor(int threat)
    {
        return threat switch
        {
            1 => new Color(0.4f, 0.7f, 0.4f),   // green - easy
            2 => new Color(0.9f, 0.7f, 0.2f),    // yellow - medium
            3 => new Color(0.9f, 0.3f, 0.2f),     // red - hard
            _ => new Color(0.7f, 0.7f, 0.7f),
        };
    }
}
