using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen faction selection for PVP. P1 picks first, then P2.
/// Fires OnBothSelected when both players have chosen.
/// </summary>
public class FactionSelectionUI : MonoBehaviour
{
    public event Action<string, string> OnBothSelected; // (p1FactionId, p2FactionId)

    private string _p1Faction;
    private string _p2Faction;
    private int _selectingPlayer = 1;

    // UI references
    private Text _titleText;
    private GameObject _buttonContainer;

    public void Init()
    {
        var rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Dark background
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.1f, 1f);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.75f);
        titleRt.anchorMax = new Vector2(0.5f, 0.75f);
        titleRt.sizeDelta = new Vector2(600, 60);
        _titleText = titleGo.AddComponent<Text>();
        _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _titleText.fontSize = 36;
        _titleText.alignment = TextAnchor.MiddleCenter;
        _titleText.color = Color.white;

        // Button container
        _buttonContainer = new GameObject("Buttons");
        _buttonContainer.transform.SetParent(transform, false);
        var containerRt = _buttonContainer.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.45f);
        containerRt.anchorMax = new Vector2(0.5f, 0.45f);
        containerRt.sizeDelta = new Vector2(600, 200);

        ShowForPlayer(1);
    }

    private void ShowForPlayer(int playerNum)
    {
        _selectingPlayer = playerNum;
        _titleText.text = $"Player {playerNum} — Choose Your Faction";

        // Clear old buttons
        foreach (Transform child in _buttonContainer.transform)
            Destroy(child.gameObject);

        // Create a button for each faction
        float yOffset = 40f;
        foreach (var faction in FactionLibrary.All)
        {
            CreateFactionButton(faction, yOffset);
            yOffset -= 90f;
        }
    }

    private void CreateFactionButton(FactionDef faction, float yOffset)
    {
        var btnGo = new GameObject($"Btn_{faction.factionId}");
        btnGo.transform.SetParent(_buttonContainer.transform, false);

        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(0, yOffset);
        btnRt.sizeDelta = new Vector2(400, 70);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = faction.factionId == "darkside"
            ? new Color(0.35f, 0.12f, 0.35f, 1f)
            : new Color(0.15f, 0.3f, 0.5f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        string fId = faction.factionId;
        btn.onClick.AddListener(() => OnFactionPicked(fId));

        // Label
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 0);
        textRt.offsetMax = new Vector2(-10, 0);
        var text = textGo.AddComponent<Text>();
        text.text = $"{faction.factionName}\nChampion: {faction.championDef.displayName} ({faction.championDef.maxHP} HP)";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private void OnFactionPicked(string factionId)
    {
        if (_selectingPlayer == 1)
        {
            _p1Faction = factionId;
            Debug.Log($"P1 chose faction: {factionId}");
            ShowForPlayer(2);
        }
        else
        {
            _p2Faction = factionId;
            Debug.Log($"P2 chose faction: {factionId}");
            gameObject.SetActive(false);
            OnBothSelected?.Invoke(_p1Faction, _p2Faction);
        }
    }
}
