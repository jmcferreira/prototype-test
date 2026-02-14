using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen main menu with Campaign and PVP Match buttons.
/// Shown at startup when no game mode has been selected.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    public event Action<GameMode> OnModeSelected;

    public void Init()
    {
        var rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Dark background
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.7f);
        titleRt.anchorMax = new Vector2(0.5f, 0.7f);
        titleRt.sizeDelta = new Vector2(600, 80);
        var titleText = titleGo.AddComponent<Text>();
        titleText.text = "TACTICAL RPG";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // Campaign button
        CreateMenuButton("CampaignBtn", "Start Campaign", 0.5f, GameMode.Campaign);

        // PVP button
        CreateMenuButton("PVPBtn", "PVP Match", 0.38f, GameMode.PVP);
    }

    private void CreateMenuButton(string name, string label, float yAnchor, GameMode mode)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(transform, false);

        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, yAnchor);
        btnRt.anchorMax = new Vector2(0.5f, yAnchor);
        btnRt.sizeDelta = new Vector2(300, 60);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = mode == GameMode.Campaign
            ? new Color(0.2f, 0.5f, 0.3f, 1f)
            : new Color(0.5f, 0.2f, 0.2f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() =>
        {
            GameModeState.CurrentMode = mode;
            OnModeSelected?.Invoke(mode);
        });

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }
}
