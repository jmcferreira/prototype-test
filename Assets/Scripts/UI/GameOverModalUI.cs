using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen modal shown on victory or defeat.
/// Victory flow: stats → "Next Scenario" → heal all players → reward screen → scenario selection → reload.
/// Defeat: "Restart" button starts a fresh run.
/// Supports multiple player characters (party).
/// </summary>
public class GameOverModalUI : MonoBehaviour
{
    private PlayerUnit[] _players;
    private RewardScreenUI _rewardScreen;
    private ScenarioSelectionUI _scenarioSelection;

    public void Init(TurnManager turnManager, PlayerUnit[] players,
                     RewardScreenUI rewardScreen, ScenarioSelectionUI scenarioSelection)
    {
        _players = players;
        _rewardScreen = rewardScreen;
        _scenarioSelection = scenarioSelection;
        turnManager.OnGameOver += Show;
        gameObject.SetActive(false);
    }

    public void Show(bool playerWon)
    {
        gameObject.SetActive(true);
        BuildModal(playerWon);
    }

    private void BuildModal(bool playerWon)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Full-screen darkened overlay
        var overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.65f);
        overlayImg.raycastTarget = true;

        // Central card — taller to accommodate multiple player stats
        int partySize = _players != null ? _players.Length : 1;
        float cardHeight = playerWon ? 260f + partySize * 30f : 220f;

        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(transform, false);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(460f, cardHeight);

        // Border tint
        Color borderColor = playerWon
            ? new Color(0.3f, 0.7f, 0.3f)
            : new Color(0.7f, 0.25f, 0.25f);

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

        // Inner dark panel
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(borderGo.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.13f, 0.97f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        // Vertical layout
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 24, 20);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Title
        string title = playerWon ? "Victory!" : "You lose...but you can always try again!";
        Color titleColor = playerWon
            ? new Color(0.4f, 0.9f, 0.4f)
            : new Color(0.9f, 0.35f, 0.35f);
        AddText(innerGo, "Title", title, titleColor, playerWon ? 36 : 22, FontStyle.Bold, 50f);

        if (playerWon)
        {
            // Per-player HP stats
            foreach (var player in _players)
            {
                string hpInfo = $"{player.DisplayName}: {player.HP}/{player.MaxHP} HP";
                Color hpColor = player.IsAlive
                    ? new Color(0.8f, 0.8f, 0.8f)
                    : new Color(0.5f, 0.3f, 0.3f);
                AddText(innerGo, $"HP_{player.DisplayName}", hpInfo, hpColor, 16, FontStyle.Normal, 22f);
            }

            // Currency summary
            int gold = CurrencyManager.Gold;
            int xp = CurrencyManager.XP;
            AddText(innerGo, "Currency", $"Gold: +{gold}  |  XP: +{xp}",
                new Color(0.85f, 0.75f, 0.4f), 16, FontStyle.Normal, 22f);

            // Check if there's a next stage (ScenariosCompleted not yet incremented)
            bool hasNext = RunState.Current != null &&
                ScenarioPool.HasStage(RunState.Current.ScenariosCompleted + 2);

            if (hasNext)
            {
                // Show heal preview for each player
                var run = RunState.Current;
                for (int i = 0; i < _players.Length && i < run.PartySize; i++)
                {
                    int healAmount = Mathf.Max(1, Mathf.CeilToInt(run.PlayerDefs[i].maxHP * run.HealPercent));
                    int healedHP = Mathf.Min(_players[i].HP + healAmount, run.PlayerDefs[i].maxHP);
                    if (healedHP > _players[i].HP)
                    {
                        string healInfo = $"{_players[i].DisplayName}: +{healedHP - _players[i].HP} HP " +
                                          $"({healedHP}/{run.PlayerDefs[i].maxHP})";
                        AddText(innerGo, $"Heal_{i}", healInfo,
                            new Color(0.4f, 0.85f, 0.4f), 14, FontStyle.Italic, 20f);
                    }
                }

                AddButton(innerGo, "Next Scenario", new Color(0.2f, 0.55f, 0.2f), OnNextScenarioClicked);
            }
            else
            {
                AddText(innerGo, "Complete", "All scenarios completed!",
                    new Color(1f, 0.85f, 0.3f), 20, FontStyle.Bold, 30f);
                AddButton(innerGo, "New Run", new Color(0.2f, 0.45f, 0.6f), OnRunCompleteClicked);
            }
        }
        else
        {
            AddButton(innerGo, "Restart", new Color(0.55f, 0.2f, 0.2f), OnRestartClicked);
        }
    }

    private void AddText(GameObject parent, string name, string text, Color color,
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

    private void AddButton(GameObject parent, string label, Color bgColor,
                           UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject($"{label}Button");
        btnGo.transform.SetParent(parent.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;
        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredWidth = 200f;
        le.preferredHeight = 50f;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var txt = textGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        var txtRect = textGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Collect current HP from all players as an array for RunState.
    /// </summary>
    private int[] GetPlayerHPs()
    {
        var hps = new int[_players.Length];
        for (int i = 0; i < _players.Length; i++)
            hps[i] = _players[i].HP;
        return hps;
    }

    /// <summary>
    /// "Next Scenario" clicked — process victory (heal all), show reward, then scenario selection.
    /// </summary>
    private void OnNextScenarioClicked()
    {
        ScenarioManager.Instance?.OnScenarioVictory(
            GetPlayerHPs(), CurrencyManager.Gold, CurrencyManager.XP);

        gameObject.SetActive(false);
        if (_rewardScreen != null)
        {
            _rewardScreen.Show(AfterRewardPicked);
        }
        else
        {
            AfterRewardPicked();
        }
    }

    /// <summary>
    /// After relic is picked, check if the next stage has multiple scenario choices.
    /// </summary>
    private void AfterRewardPicked()
    {
        if (RunState.Current == null)
        {
            ReloadScene();
            return;
        }

        var choices = ScenarioPool.GetChoices(RunState.Current.ScenariosCompleted + 1);

        if (choices != null && choices.Length > 1 && _scenarioSelection != null)
        {
            _scenarioSelection.Show(choices, OnScenarioChosen);
        }
        else
        {
            ReloadScene();
        }
    }

    private void OnScenarioChosen(ScenarioDef chosen)
    {
        if (RunState.Current != null)
            RunState.Current.ChosenNextScenario = chosen;
        ReloadScene();
    }

    /// <summary>
    /// "New Run" clicked (run complete) — no reward needed, just transition.
    /// </summary>
    private void OnRunCompleteClicked()
    {
        ScenarioManager.Instance?.OnScenarioVictory(
            GetPlayerHPs(), CurrencyManager.Gold, CurrencyManager.XP);
        ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnRestartClicked()
    {
        ScenarioManager.Instance?.OnScenarioDefeat();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
