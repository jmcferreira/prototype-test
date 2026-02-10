using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single card styled like a poker/playing card.
/// Header with bold card name, numbered action rows with dividers, cooldown footer.
/// Scales up and brightens on hover. Contains no gameplay logic.
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnClicked;

    private Image _borderImage;
    private Image _innerImage;
    private Text _cooldownText;
    private Button _button;

    // Card state colours
    private static readonly Color BorderReady    = new Color(0.65f, 0.55f, 0.35f);
    private static readonly Color BorderHover    = new Color(0.82f, 0.72f, 0.45f);
    private static readonly Color BorderCooldown = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color BorderSelected = new Color(0.4f, 0.7f, 1f);

    private static readonly Color InnerReady    = new Color(0.96f, 0.93f, 0.84f);
    private static readonly Color InnerCooldown = new Color(0.45f, 0.45f, 0.45f);

    private static readonly Color HeaderBg     = new Color(0.18f, 0.16f, 0.12f);
    private static readonly Color FooterReady  = new Color(0.22f, 0.45f, 0.22f);
    private static readonly Color FooterOnCD   = new Color(0.5f, 0.28f, 0.28f);

    private static readonly Vector3 NormalScale = Vector3.one;
    private static readonly Vector3 HoverScale  = new Vector3(1.08f, 1.08f, 1f);

    private bool _selected;
    private bool _hovered;
    private bool _isReady;

    /// <summary>
    /// Build the card's UI elements from a CardData. Call once at creation.
    /// </summary>
    public void Build(CardData data)
    {
        // Outer border (acts as the card edge / gold rim)
        _borderImage = gameObject.AddComponent<Image>();
        _borderImage.color = BorderReady;

        _button = gameObject.AddComponent<Button>();
        _button.targetGraphic = _borderImage;
        _button.onClick.AddListener(() => OnClicked?.Invoke());

        // Disable automatic button color tint — we manage colours ourselves
        var colors = _button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.disabledColor = Color.white;
        _button.colors = colors;

        // Inner parchment panel (inset from border)
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(transform, false);
        _innerImage = innerGo.AddComponent<Image>();
        _innerImage.color = InnerReady;
        _innerImage.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        // Vertical layout for header + actions + footer
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // ── Header: card name ──
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(innerGo.transform, false);
        var headerImg = headerGo.AddComponent<Image>();
        headerImg.color = HeaderBg;
        headerImg.raycastTarget = false;
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 36f;

        var nameTxt = CreateFillText(headerGo.transform, "CardName", 16, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter, 6f);
        nameTxt.text = data.cardName;

        // ── Action rows with dividers ──
        int actionCount = data.actions != null ? data.actions.Length : 0;
        for (int i = 0; i < actionCount; i++)
        {
            // Divider between header/actions and between actions
            BuildDivider(innerGo.transform);

            var rowGo = new GameObject($"Action_{i}");
            rowGo.transform.SetParent(innerGo.transform, false);
            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = (i % 2 == 0)
                ? new Color(0.92f, 0.89f, 0.80f)
                : new Color(0.88f, 0.85f, 0.76f);
            rowImg.raycastTarget = false;
            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 32f;

            // Step number (left badge)
            var numGo = new GameObject("Num");
            numGo.transform.SetParent(rowGo.transform, false);
            var numTxt = numGo.AddComponent<Text>();
            numTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            numTxt.fontSize = 13;
            numTxt.fontStyle = FontStyle.Bold;
            numTxt.alignment = TextAnchor.MiddleCenter;
            numTxt.color = new Color(0.45f, 0.40f, 0.30f);
            numTxt.text = $"{i + 1}";
            numTxt.raycastTarget = false;
            var numRect = numGo.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0f);
            numRect.anchorMax = new Vector2(0f, 1f);
            numRect.pivot = new Vector2(0f, 0.5f);
            numRect.anchoredPosition = new Vector2(6f, 0f);
            numRect.sizeDelta = new Vector2(18f, 0f);

            // Action description
            var descTxt = CreateFillText(rowGo.transform, "Desc", 14, FontStyle.Normal,
                new Color(0.15f, 0.13f, 0.10f), TextAnchor.MiddleLeft, 28f);
            descTxt.text = Hand.DescribeAction(data.actions[i]);
            // Adjust rect to leave room for the number badge
            var descRect = descTxt.GetComponent<RectTransform>();
            descRect.offsetMin = new Vector2(28f, 0f);
            descRect.offsetMax = new Vector2(-6f, 0f);
        }

        // ── Footer divider + cooldown ──
        BuildDivider(innerGo.transform);

        var footerGo = new GameObject("Footer");
        footerGo.transform.SetParent(innerGo.transform, false);
        var footerImg = footerGo.AddComponent<Image>();
        footerImg.color = FooterReady;
        footerImg.raycastTarget = false;
        var footerLe = footerGo.AddComponent<LayoutElement>();
        footerLe.preferredHeight = 26f;

        _cooldownText = CreateFillText(footerGo.transform, "CD", 13, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter, 4f);
        _cooldownText.text = "Ready";
    }

    /// <summary>
    /// Update the card's cooldown display and interactability.
    /// </summary>
    public void Refresh(int cooldownRemaining, int cooldownMax, bool isReady)
    {
        _isReady = isReady;
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
        // Border color: selected > hovered > default / cooldown
        if (_selected)
            _borderImage.color = BorderSelected;
        else if (_hovered && _isReady)
            _borderImage.color = BorderHover;
        else
            _borderImage.color = _isReady ? BorderReady : BorderCooldown;

        // Inner parchment dims on cooldown
        _innerImage.color = _isReady ? InnerReady : InnerCooldown;

        // Footer color
        var footerImg = _cooldownText.transform.parent.GetComponent<Image>();
        if (footerImg != null)
            footerImg.color = _isReady ? FooterReady : FooterOnCD;

        // Scale pop on hover
        transform.localScale = (_hovered && _isReady) ? HoverScale : NormalScale;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void BuildDivider(Transform parent)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.48f, 0.35f, 0.6f);
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 1f;
    }

    private static Text CreateFillText(Transform parent, string name, int fontSize,
        FontStyle style, Color color, TextAnchor alignment, float hPad)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = alignment;
        txt.color = color;
        txt.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(hPad, 0f);
        r.offsetMax = new Vector2(-hPad, 0f);
        return txt;
    }
}
