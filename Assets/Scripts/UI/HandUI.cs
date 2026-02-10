using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Displays the player's hand as a horizontal row of card buttons.
/// Pure display layer — contains no gameplay logic.
/// Shows an expanded card breakdown on the left when a card is selected,
/// highlighting the current action step with dividers between steps.
/// </summary>
public class HandUI : MonoBehaviour
{
    public event Action<int> OnCardClicked;
    public event Action OnPassClicked;
    public event Action OnSkipClicked;

    private readonly List<CardUI> _cardUIs = new();
    private Canvas _canvas;

    // Old action step label (kept hidden — replaced by expanded card)
    private GameObject _actionStepGo;
    private Text _actionStepText;
    private GameObject _skipButtonGo;

    // Expanded card panel (left side, below player panel)
    private GameObject _expandedCardGo;
    private readonly List<Image> _stepBgs = new();
    private readonly List<Text> _stepTexts = new();
    private readonly List<Text> _stepIndicators = new();
    private GameObject _expandedSkipGo;

    // ── Colours ────────────────────────────────────────────────────────

    private static readonly Color StepActive   = new Color(0.18f, 0.38f, 0.72f, 0.55f);
    private static readonly Color StepDone     = new Color(0.15f, 0.25f, 0.15f, 0.35f);
    private static readonly Color StepPending  = Color.clear;
    private static readonly Color TextActive   = Color.white;
    private static readonly Color TextDone     = new Color(0.45f, 0.55f, 0.45f);
    private static readonly Color TextPending  = new Color(0.72f, 0.72f, 0.72f);
    private static readonly Color ArrowColor   = new Color(1f, 0.85f, 0.3f);
    private static readonly Color CheckColor   = new Color(0.4f, 0.78f, 0.4f);

    // ── Init ───────────────────────────────────────────────────────────

    /// <summary>
    /// Build the UI from the given hand. Call once after the hand is created.
    /// </summary>
    public void Init(Hand hand)
    {
        // EventSystem is required for UI clicks to work
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Reuse parent canvas if already under one, otherwise create our own
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Dark background strip across the bottom
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(transform, false);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
        bgImage.raycastTarget = false;

        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(0f, 460f);

        // Old action step UI (hidden — replaced by expanded card)
        BuildActionStepUI();

        // Card row — centered on the background strip
        var panelGo = new GameObject("CardPanel");
        panelGo.transform.SetParent(transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 15f);

        var layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create a CardUI for each card in the hand
        for (int i = 0; i < hand.Cards.Count; i++)
        {
            var cardGo = new GameObject($"Card_{i}");
            cardGo.transform.SetParent(panelGo.transform, false);

            var cardRect = cardGo.AddComponent<RectTransform>();
            var le = cardGo.AddComponent<LayoutElement>();
            le.preferredWidth = 400;
            le.preferredHeight = 420;

            var cardUI = cardGo.AddComponent<CardUI>();
            cardUI.Build(hand.Cards[i].Data);

            int index = i; // capture for closure
            cardUI.OnClicked += () => OnCardClicked?.Invoke(index);

            _cardUIs.Add(cardUI);
        }

        // Pass button
        var passGo = new GameObject("PassButton");
        passGo.transform.SetParent(panelGo.transform, false);

        var passRect = passGo.AddComponent<RectTransform>();
        var passLe = passGo.AddComponent<LayoutElement>();
        passLe.preferredWidth = 140;
        passLe.preferredHeight = 280;

        var passBg = passGo.AddComponent<Image>();
        passBg.color = new Color(0.85f, 0.75f, 0.65f);

        var passBtn = passGo.AddComponent<Button>();
        passBtn.targetGraphic = passBg;
        passBtn.onClick.AddListener(() => OnPassClicked?.Invoke());

        var passTextGo = new GameObject("Text");
        passTextGo.transform.SetParent(passGo.transform, false);
        var passText = passTextGo.AddComponent<Text>();
        passText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        passText.fontSize = 32;
        passText.alignment = TextAnchor.MiddleCenter;
        passText.color = Color.black;
        passText.text = "Pass";
        var textRect = passTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Initial refresh
        Refresh(hand);
    }

    private void BuildActionStepUI()
    {
        // Kept for backwards compat but hidden — expanded card replaces this
        _actionStepGo = new GameObject("ActionStep");
        _actionStepGo.transform.SetParent(transform, false);

        var stepRect = _actionStepGo.AddComponent<RectTransform>();
        stepRect.anchorMin = new Vector2(0.5f, 0f);
        stepRect.anchorMax = new Vector2(0.5f, 0f);
        stepRect.pivot = new Vector2(0.5f, 0f);
        stepRect.anchoredPosition = new Vector2(0f, 250f);

        var stepLayout = _actionStepGo.AddComponent<HorizontalLayoutGroup>();
        stepLayout.spacing = 14;
        stepLayout.childAlignment = TextAnchor.MiddleCenter;
        stepLayout.childForceExpandWidth = false;
        stepLayout.childForceExpandHeight = false;

        var stepFitter = _actionStepGo.AddComponent<ContentSizeFitter>();
        stepFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        stepFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(_actionStepGo.transform, false);
        _actionStepText = labelGo.AddComponent<Text>();
        _actionStepText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _actionStepText.fontSize = 24;
        _actionStepText.alignment = TextAnchor.MiddleCenter;
        _actionStepText.color = Color.white;
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.preferredHeight = 40;

        _skipButtonGo = new GameObject("SkipButton");
        _skipButtonGo.transform.SetParent(_actionStepGo.transform, false);

        var skipLe = _skipButtonGo.AddComponent<LayoutElement>();
        skipLe.preferredWidth = 100;
        skipLe.preferredHeight = 40;

        var skipBg = _skipButtonGo.AddComponent<Image>();
        skipBg.color = new Color(0.7f, 0.55f, 0.55f);

        var skipBtn = _skipButtonGo.AddComponent<Button>();
        skipBtn.targetGraphic = skipBg;
        skipBtn.onClick.AddListener(() => OnSkipClicked?.Invoke());

        var skipTextGo = new GameObject("Text");
        skipTextGo.transform.SetParent(_skipButtonGo.transform, false);
        var skipText = skipTextGo.AddComponent<Text>();
        skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipText.fontSize = 20;
        skipText.alignment = TextAnchor.MiddleCenter;
        skipText.color = Color.white;
        skipText.text = "Skip";
        var skipTR = skipTextGo.GetComponent<RectTransform>();
        skipTR.anchorMin = Vector2.zero;
        skipTR.anchorMax = Vector2.one;
        skipTR.offsetMin = Vector2.zero;
        skipTR.offsetMax = Vector2.zero;

        _actionStepGo.SetActive(false);
    }

    // ── Refresh ────────────────────────────────────────────────────────

    /// <summary>
    /// Update all card displays from current hand state.
    /// </summary>
    public void Refresh(Hand hand)
    {
        for (int i = 0; i < _cardUIs.Count && i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            _cardUIs[i].Refresh(card.CooldownRemaining, card.Data.cooldown, card.IsReady);
        }
    }

    /// <summary>
    /// Highlight the card at the given index as selected. Pass -1 to clear.
    /// Also hides the expanded card when deselecting.
    /// </summary>
    public void SetSelectedCard(int index)
    {
        for (int i = 0; i < _cardUIs.Count; i++)
            _cardUIs[i].SetSelected(i == index);

        if (index < 0)
            HideExpandedCard();
    }

    // ── Action Step (delegates to expanded card) ───────────────────────

    /// <summary>
    /// Show and highlight the current action step on the expanded card.
    /// </summary>
    public void ShowActionStep(int step, int total, string description, bool canSkip = true)
    {
        // Update expanded card highlight (step is 1-indexed, convert to 0-indexed)
        UpdateExpandedStep(step - 1, canSkip);
    }

    /// <summary>
    /// Hide the action step indicator.
    /// </summary>
    public void HideActionStep()
    {
        if (_actionStepGo != null)
            _actionStepGo.SetActive(false);
    }

    // ── Expanded Card Panel ────────────────────────────────────────────

    /// <summary>
    /// Build an expanded card breakdown panel on the left side (below the player panel).
    /// Shows the card name and each action step as a separate row with dividers.
    /// Call this once when a card is selected, then use UpdateExpandedStep to highlight.
    /// </summary>
    public void ShowExpandedCard(CardData data)
    {
        HideExpandedCard();

        _stepBgs.Clear();
        _stepTexts.Clear();
        _stepIndicators.Clear();

        _expandedCardGo = new GameObject("ExpandedCard");
        _expandedCardGo.transform.SetParent(transform, false);

        var rect = _expandedCardGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -65f); // below player info panel
        rect.sizeDelta = new Vector2(220f, 10f); // height driven by ContentSizeFitter

        // Outer border
        var borderImg = _expandedCardGo.AddComponent<Image>();
        borderImg.color = new Color(0.35f, 0.45f, 0.65f, 0.9f);
        borderImg.raycastTarget = false;

        // Inner container (for the dark inset)
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(_expandedCardGo.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.13f, 0.97f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        // Vertical layout inside inner
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // ContentSizeFitter on the outer panel
        var fitter = _expandedCardGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Header: card name ──
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(innerGo.transform, false);
        var headerBg = headerGo.AddComponent<Image>();
        headerBg.color = new Color(0.15f, 0.17f, 0.22f);
        headerBg.raycastTarget = false;
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 34f;

        var titleTxt = CreateFillText(headerGo.transform, "Title", 16, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter);
        titleTxt.text = data.cardName;

        // ── Action rows with dividers ──
        int actionCount = data.actions != null ? data.actions.Length : 0;
        for (int i = 0; i < actionCount; i++)
        {
            // Divider
            BuildDivider(innerGo.transform, $"Div_{i}");

            // Row
            var rowGo = new GameObject($"Step_{i}");
            rowGo.transform.SetParent(innerGo.transform, false);
            var rowBg = rowGo.AddComponent<Image>();
            rowBg.color = StepPending;
            rowBg.raycastTarget = false;
            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 38f;
            _stepBgs.Add(rowBg);

            // Arrow / check indicator (left side)
            var indGo = new GameObject("Indicator");
            indGo.transform.SetParent(rowGo.transform, false);
            var indText = indGo.AddComponent<Text>();
            indText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            indText.fontSize = 14;
            indText.alignment = TextAnchor.MiddleCenter;
            indText.color = ArrowColor;
            indText.text = "\u25B6"; // ▶
            indText.raycastTarget = false;
            var indRect = indGo.GetComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0f);
            indRect.anchorMax = new Vector2(0f, 1f);
            indRect.pivot = new Vector2(0f, 0.5f);
            indRect.anchoredPosition = new Vector2(6f, 0f);
            indRect.sizeDelta = new Vector2(18f, 0f);
            indText.gameObject.SetActive(false);
            _stepIndicators.Add(indText);

            // Step number + description text
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(rowGo.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 15;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = TextPending;
            txt.text = $"{i + 1}. {Hand.DescribeAction(data.actions[i])}";
            txt.raycastTarget = false;
            var txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(26f, 0f);
            txtRect.offsetMax = new Vector2(-6f, 0f);
            _stepTexts.Add(txt);
        }

        // ── Bottom divider + Skip button ──
        BuildDivider(innerGo.transform, "SkipDiv");

        _expandedSkipGo = new GameObject("SkipRow");
        _expandedSkipGo.transform.SetParent(innerGo.transform, false);
        var skipBg = _expandedSkipGo.AddComponent<Image>();
        skipBg.color = new Color(0.55f, 0.32f, 0.32f);
        var skipLe = _expandedSkipGo.AddComponent<LayoutElement>();
        skipLe.preferredHeight = 30f;

        var skipBtn = _expandedSkipGo.AddComponent<Button>();
        skipBtn.targetGraphic = skipBg;
        skipBtn.onClick.AddListener(() => OnSkipClicked?.Invoke());

        // Hover color for button
        var skipColors = skipBtn.colors;
        skipColors.normalColor = Color.white;
        skipColors.highlightedColor = new Color(1.1f, 1.05f, 1.05f);
        skipColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        skipBtn.colors = skipColors;

        var skipTxtGo = new GameObject("Text");
        skipTxtGo.transform.SetParent(_expandedSkipGo.transform, false);
        var skipTxt = skipTxtGo.AddComponent<Text>();
        skipTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipTxt.fontSize = 14;
        skipTxt.fontStyle = FontStyle.Bold;
        skipTxt.alignment = TextAnchor.MiddleCenter;
        skipTxt.color = Color.white;
        skipTxt.text = "Skip Step";
        FillRect(skipTxtGo);

        _expandedSkipGo.SetActive(false);
    }

    /// <summary>
    /// Update the expanded card to highlight the given step (0-indexed).
    /// Steps before activeStep are marked as completed; after are pending.
    /// </summary>
    public void UpdateExpandedStep(int activeStep, bool canSkip)
    {
        for (int i = 0; i < _stepBgs.Count; i++)
        {
            bool isActive = (i == activeStep);
            bool isDone = (i < activeStep);

            // Row background
            if (isActive)
                _stepBgs[i].color = StepActive;
            else if (isDone)
                _stepBgs[i].color = StepDone;
            else
                _stepBgs[i].color = StepPending;

            // Text colour
            if (isActive)
                _stepTexts[i].color = TextActive;
            else if (isDone)
                _stepTexts[i].color = TextDone;
            else
                _stepTexts[i].color = TextPending;

            // Indicator: arrow for active, checkmark for done, hidden for pending
            if (isActive)
            {
                _stepIndicators[i].gameObject.SetActive(true);
                _stepIndicators[i].text = "\u25B6"; // ▶
                _stepIndicators[i].color = ArrowColor;
            }
            else if (isDone)
            {
                _stepIndicators[i].gameObject.SetActive(true);
                _stepIndicators[i].text = "\u2713"; // ✓
                _stepIndicators[i].color = CheckColor;
            }
            else
            {
                _stepIndicators[i].gameObject.SetActive(false);
            }
        }

        // Show/hide skip button
        if (_expandedSkipGo != null)
            _expandedSkipGo.SetActive(canSkip);
    }

    /// <summary>
    /// Destroy the expanded card panel.
    /// </summary>
    public void HideExpandedCard()
    {
        if (_expandedCardGo != null)
        {
            Destroy(_expandedCardGo);
            _expandedCardGo = null;
        }
        _stepBgs.Clear();
        _stepTexts.Clear();
        _stepIndicators.Clear();
        _expandedSkipGo = null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static void BuildDivider(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.38f, 0.7f);
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 1f;
    }

    private static Text CreateFillText(Transform parent, string name, int fontSize,
        FontStyle style, Color color, TextAnchor alignment)
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
        FillRect(go);
        return txt;
    }

    private static void FillRect(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
