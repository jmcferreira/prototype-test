using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single card. Displays name, cooldown, and
/// availability. Fires OnClicked when the button is pressed.
/// Scales up and brightens on hover. Contains no gameplay logic.
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnClicked;

    private Text _nameText;
    private Text _descText;
    private Text _cooldownText;
    private Button _button;
    private Image _background;

    private static readonly Color ReadyColor = new Color(0.95f, 0.92f, 0.78f);
    private static readonly Color HoverColor = new Color(1f, 0.97f, 0.85f);
    private static readonly Color CooldownColor = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color SelectedColor = new Color(0.6f, 0.85f, 1f);

    private static readonly Vector3 NormalScale = Vector3.one;
    private static readonly Vector3 HoverScale = new Vector3(1.15f, 1.15f, 1f);

    private bool _selected;
    private bool _hovered;

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
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.disabledColor = Color.white;
        _button.colors = colors;

        // Card name (top third)
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(transform, false);
        _nameText = nameGo.AddComponent<Text>();
        _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.fontSize = 22;
        _nameText.fontStyle = FontStyle.Bold;
        _nameText.alignment = TextAnchor.MiddleCenter;
        _nameText.color = Color.black;

        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.66f);
        nameRect.anchorMax = new Vector2(1, 1f);
        nameRect.offsetMin = new Vector2(4, 0);
        nameRect.offsetMax = new Vector2(-4, 0);

        // Effect description (middle third)
        var descGo = new GameObject("Description");
        descGo.transform.SetParent(transform, false);
        _descText = descGo.AddComponent<Text>();
        _descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _descText.fontSize = 18;
        _descText.alignment = TextAnchor.MiddleCenter;
        _descText.color = new Color(0.2f, 0.2f, 0.2f);

        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.33f);
        descRect.anchorMax = new Vector2(1, 0.66f);
        descRect.offsetMin = new Vector2(4, 0);
        descRect.offsetMax = new Vector2(-4, 0);

        // Cooldown text (bottom third)
        var cdGo = new GameObject("Cooldown");
        cdGo.transform.SetParent(transform, false);
        _cooldownText = cdGo.AddComponent<Text>();
        _cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _cooldownText.fontSize = 16;
        _cooldownText.alignment = TextAnchor.MiddleCenter;
        _cooldownText.color = new Color(0.35f, 0.35f, 0.35f);

        var cdRect = cdGo.GetComponent<RectTransform>();
        cdRect.anchorMin = new Vector2(0, 0f);
        cdRect.anchorMax = new Vector2(1, 0.33f);
        cdRect.offsetMin = new Vector2(4, 0);
        cdRect.offsetMax = new Vector2(-4, 0);
    }

    /// <summary>
    /// Refresh the card display with current data. Call whenever state changes.
    /// </summary>
    public void Refresh(string cardName, string description, int cooldownRemaining, int cooldownMax, bool isReady)
    {
        _nameText.text = cardName;
        _descText.text = description;
        _cooldownText.text = isReady ? "Ready" : $"CD: {cooldownRemaining}/{cooldownMax}";

        _button.interactable = isReady;
        UpdateVisuals();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        bool isReady = _button.interactable;

        // Color: selected > hovered > default
        if (_selected)
            _background.color = SelectedColor;
        else if (_hovered && isReady)
            _background.color = HoverColor;
        else
            _background.color = isReady ? ReadyColor : CooldownColor;

        // Scale: pop up on hover when interactable
        transform.localScale = (_hovered && isReady) ? HoverScale : NormalScale;
    }
}
