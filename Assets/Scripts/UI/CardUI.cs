using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single card styled like a poker/playing card.
/// Header with bold card name, numbered action rows with dividers.
/// Dims and shows cooldown turns remaining when on cooldown.
/// Scales up and brightens on hover. Contains no gameplay logic.
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnClicked;

    private Image _borderImage;
    private Image _innerImage;
    private Button _button;

    // Cooldown overlay (shown when on cooldown)
    private GameObject _cdOverlayGo;
    private Text _cdNumberText;

    // Action row references for step highlighting
    private readonly System.Collections.Generic.List<Image> _actionRowImages = new();
    private readonly System.Collections.Generic.List<Text> _actionNumTexts = new();
    private readonly System.Collections.Generic.List<Text> _actionDescTexts = new();
    private readonly System.Collections.Generic.List<Color> _actionRowBaseColors = new();
    private int _activeStep = -1;

    // Card state colours
    private static readonly Color BorderReady    = new Color(0.65f, 0.55f, 0.35f);
    private static readonly Color BorderHover    = new Color(0.82f, 0.72f, 0.45f);
    private static readonly Color BorderCooldown = new Color(0.30f, 0.30f, 0.30f);
    private static readonly Color BorderSelected = new Color(0.4f, 0.7f, 1f);

    private static readonly Color InnerReady    = new Color(0.96f, 0.93f, 0.84f);
    private static readonly Color InnerCooldown = new Color(0.38f, 0.38f, 0.38f);

    private static readonly Color HeaderBg = new Color(0.18f, 0.16f, 0.12f);

    // Step highlight colours (on the card itself)
    private static readonly Color RowActive     = new Color(0.30f, 0.55f, 0.90f, 0.85f);
    private static readonly Color RowDone       = new Color(0.25f, 0.45f, 0.25f, 0.60f);
    private static readonly Color NumActive     = Color.white;
    private static readonly Color DescActive    = Color.white;
    private static readonly Color NumDone       = new Color(0.70f, 0.85f, 0.70f);
    private static readonly Color DescDone      = new Color(0.70f, 0.85f, 0.70f);

    private static readonly Vector3 NormalScale = new Vector3(0.7f, 0.7f, 1f);
    private static readonly Vector3 HoverScale  = Vector3.one;

    private bool _selected;
    private bool _hovered;
    private bool _isReady;

    /// <summary>
    /// Build the card's UI elements from a CardData. Call once at creation.
    /// </summary>
    public void Build(CardData data)
    {
        transform.localScale = NormalScale;

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
        innerRect.offsetMin = new Vector2(5f, 5f);
        innerRect.offsetMax = new Vector2(-5f, -5f);

        // Vertical layout for header + actions
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
        headerLe.preferredHeight = 64f;

        var nameTxt = CreateFillText(headerGo.transform, "CardName", 28, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter, 10f);
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
            Color baseRowColor = (i % 2 == 0)
                ? new Color(0.92f, 0.89f, 0.80f)
                : new Color(0.88f, 0.85f, 0.76f);
            rowImg.color = baseRowColor;
            rowImg.raycastTarget = false;
            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 58f;
            _actionRowImages.Add(rowImg);
            _actionRowBaseColors.Add(baseRowColor);

            // Step number (left badge)
            var numGo = new GameObject("Num");
            numGo.transform.SetParent(rowGo.transform, false);
            var numTxt = numGo.AddComponent<Text>();
            numTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            numTxt.fontSize = 24;
            numTxt.fontStyle = FontStyle.Bold;
            numTxt.alignment = TextAnchor.MiddleCenter;
            numTxt.color = new Color(0.45f, 0.40f, 0.30f);
            numTxt.text = $"{i + 1}";
            numTxt.raycastTarget = false;
            var numRect = numGo.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0f);
            numRect.anchorMax = new Vector2(0f, 1f);
            numRect.pivot = new Vector2(0f, 0.5f);
            numRect.anchoredPosition = new Vector2(10f, 0f);
            numRect.sizeDelta = new Vector2(32f, 0f);
            _actionNumTexts.Add(numTxt);

            // Action description
            var descTxt = CreateFillText(rowGo.transform, "Desc", 24, FontStyle.Normal,
                new Color(0.15f, 0.13f, 0.10f), TextAnchor.MiddleLeft, 48f);
            descTxt.text = Hand.DescribeAction(data.actions[i]);
            var descRect = descTxt.GetComponent<RectTransform>();
            descRect.offsetMin = new Vector2(48f, 0f);
            descRect.offsetMax = new Vector2(-10f, 0f);
            _actionDescTexts.Add(descTxt);
        }

        // ── Cooldown overlay (hidden when ready) ──
        BuildCooldownOverlay();
    }

    /// <summary>
    /// Update the card's cooldown display and interactability.
    /// </summary>
    public void Refresh(int cooldownRemaining, int cooldownMax, bool isReady)
    {
        _isReady = isReady;

        if (isReady)
        {
            _cdOverlayGo.SetActive(false);
        }
        else
        {
            _cdOverlayGo.SetActive(true);
            _cdNumberText.text = cooldownRemaining.ToString();
        }

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

        // Scale pop on hover
        transform.localScale = (_hovered && _isReady) ? HoverScale : NormalScale;
    }

    // ── Step highlighting ─────────────────────────────────────────────────

    /// <summary>
    /// Highlight the given action step (0-indexed) on the card.
    /// Steps before it are marked done; steps after are default.
    /// Pass -1 to clear all highlights.
    /// </summary>
    public void SetActiveStep(int step)
    {
        _activeStep = step;
        for (int i = 0; i < _actionRowImages.Count; i++)
        {
            bool isActive = (i == step);
            bool isDone = (step >= 0 && i < step);

            if (isActive)
            {
                _actionRowImages[i].color = RowActive;
                _actionNumTexts[i].color = NumActive;
                _actionNumTexts[i].text = "\u25B6"; // ▶
                _actionDescTexts[i].color = DescActive;
            }
            else if (isDone)
            {
                _actionRowImages[i].color = RowDone;
                _actionNumTexts[i].color = NumDone;
                _actionNumTexts[i].text = "\u2713"; // ✓
                _actionDescTexts[i].color = DescDone;
            }
            else
            {
                _actionRowImages[i].color = _actionRowBaseColors[i];
                _actionNumTexts[i].color = new Color(0.45f, 0.40f, 0.30f);
                _actionNumTexts[i].text = $"{i + 1}";
                _actionDescTexts[i].color = new Color(0.15f, 0.13f, 0.10f);
            }
        }
    }

    public void ClearActiveStep()
    {
        SetActiveStep(-1);
    }

    // ── Builders ─────────────────────────────────────────────────────────

    private void BuildCooldownOverlay()
    {
        _cdOverlayGo = new GameObject("CooldownOverlay");
        _cdOverlayGo.transform.SetParent(transform, false);

        var overlayImg = _cdOverlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.raycastTarget = false;
        var overlayRect = _cdOverlayGo.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var numGo = new GameObject("CDNumber");
        numGo.transform.SetParent(_cdOverlayGo.transform, false);
        _cdNumberText = numGo.AddComponent<Text>();
        _cdNumberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _cdNumberText.fontSize = 72;
        _cdNumberText.fontStyle = FontStyle.Bold;
        _cdNumberText.alignment = TextAnchor.MiddleCenter;
        _cdNumberText.color = new Color(1f, 0.85f, 0.85f, 0.9f);
        _cdNumberText.raycastTarget = false;
        var numRect = numGo.GetComponent<RectTransform>();
        numRect.anchorMin = Vector2.zero;
        numRect.anchorMax = Vector2.one;
        numRect.offsetMin = Vector2.zero;
        numRect.offsetMax = Vector2.zero;

        _cdOverlayGo.SetActive(false);
    }

    private static void BuildDivider(Transform parent)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.48f, 0.35f, 0.6f);
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
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
