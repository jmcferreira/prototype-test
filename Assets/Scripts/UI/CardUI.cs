using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single card. Displays name, cooldown, and
/// availability. Fires OnClicked when the button is pressed.
/// Contains no gameplay logic.
/// </summary>
public class CardUI : MonoBehaviour
{
    public event Action OnClicked;

    private Text _nameText;
    private Text _cooldownText;
    private Button _button;
    private Image _background;

    private static readonly Color ReadyColor = new Color(0.95f, 0.92f, 0.78f);
    private static readonly Color CooldownColor = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color SelectedColor = new Color(0.6f, 0.85f, 1f);

    private bool _selected;

    /// <summary>
    /// Build the card's UI elements programmatically.
    /// </summary>
    public void Build()
    {
        _background = gameObject.AddComponent<Image>();
        _background.color = ReadyColor;

        _button = gameObject.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.onClick.AddListener(() => OnClicked?.Invoke());

        // Disable automatic color tint so we control colors ourselves
        var colors = _button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f);
        _button.colors = colors;

        // Card name
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(transform, false);
        _nameText = nameGo.AddComponent<Text>();
        _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.fontSize = 16;
        _nameText.alignment = TextAnchor.MiddleCenter;
        _nameText.color = Color.black;

        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.45f);
        nameRect.anchorMax = new Vector2(1, 1f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // Cooldown text
        var cdGo = new GameObject("Cooldown");
        cdGo.transform.SetParent(transform, false);
        _cooldownText = cdGo.AddComponent<Text>();
        _cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _cooldownText.fontSize = 13;
        _cooldownText.alignment = TextAnchor.MiddleCenter;
        _cooldownText.color = new Color(0.3f, 0.3f, 0.3f);

        var cdRect = cdGo.GetComponent<RectTransform>();
        cdRect.anchorMin = new Vector2(0, 0f);
        cdRect.anchorMax = new Vector2(1, 0.45f);
        cdRect.offsetMin = Vector2.zero;
        cdRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Refresh the card display with current data. Call whenever state changes.
    /// </summary>
    public void Refresh(string cardName, int cooldownRemaining, int cooldownMax, bool isReady)
    {
        _nameText.text = cardName;
        _cooldownText.text = isReady ? "Ready" : $"CD: {cooldownRemaining}/{cooldownMax}";

        _button.interactable = isReady;
        UpdateColor(isReady);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        // Re-derive ready state from button interactable
        UpdateColor(_button.interactable);
    }

    private void UpdateColor(bool isReady)
    {
        if (_selected)
            _background.color = SelectedColor;
        else
            _background.color = isReady ? ReadyColor : CooldownColor;
    }
}
