using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Displays the player's hand as a horizontal row of card buttons.
/// Pure display layer — contains no gameplay logic.
/// Shows an action step label + Skip button during multi-action resolution.
/// </summary>
public class HandUI : MonoBehaviour
{
    public event Action<int> OnCardClicked;
    public event Action OnPassClicked;
    public event Action OnSkipClicked;

    private readonly List<CardUI> _cardUIs = new();
    private Canvas _canvas;
    private GameObject _actionStepGo;
    private Text _actionStepText;
    private GameObject _skipButtonGo;

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

        // Screen-space overlay canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

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
        bgRect.sizeDelta = new Vector2(0f, 110f);

        // Action step label + Skip button (above cards, hidden by default)
        BuildActionStepUI();

        // Card row — centered on the background strip
        var panelGo = new GameObject("CardPanel");
        panelGo.transform.SetParent(transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 10f);

        var layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
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
            le.preferredWidth = 100;
            le.preferredHeight = 70;

            var cardUI = cardGo.AddComponent<CardUI>();
            cardUI.Build();

            int index = i; // capture for closure
            cardUI.OnClicked += () => OnCardClicked?.Invoke(index);

            _cardUIs.Add(cardUI);
        }

        // Pass button
        var passGo = new GameObject("PassButton");
        passGo.transform.SetParent(panelGo.transform, false);

        var passRect = passGo.AddComponent<RectTransform>();
        var passLe = passGo.AddComponent<LayoutElement>();
        passLe.preferredWidth = 60;
        passLe.preferredHeight = 70;

        var passBg = passGo.AddComponent<Image>();
        passBg.color = new Color(0.85f, 0.75f, 0.65f);

        var passBtn = passGo.AddComponent<Button>();
        passBtn.targetGraphic = passBg;
        passBtn.onClick.AddListener(() => OnPassClicked?.Invoke());

        var passTextGo = new GameObject("Text");
        passTextGo.transform.SetParent(passGo.transform, false);
        var passText = passTextGo.AddComponent<Text>();
        passText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        passText.fontSize = 14;
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
        _actionStepGo = new GameObject("ActionStep");
        _actionStepGo.transform.SetParent(transform, false);

        var stepRect = _actionStepGo.AddComponent<RectTransform>();
        stepRect.anchorMin = new Vector2(0.5f, 0f);
        stepRect.anchorMax = new Vector2(0.5f, 0f);
        stepRect.pivot = new Vector2(0.5f, 0f);
        stepRect.anchoredPosition = new Vector2(0f, 85f);

        var stepLayout = _actionStepGo.AddComponent<HorizontalLayoutGroup>();
        stepLayout.spacing = 10;
        stepLayout.childAlignment = TextAnchor.MiddleCenter;
        stepLayout.childForceExpandWidth = false;
        stepLayout.childForceExpandHeight = false;

        var stepFitter = _actionStepGo.AddComponent<ContentSizeFitter>();
        stepFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        stepFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Action label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(_actionStepGo.transform, false);
        _actionStepText = labelGo.AddComponent<Text>();
        _actionStepText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _actionStepText.fontSize = 14;
        _actionStepText.alignment = TextAnchor.MiddleCenter;
        _actionStepText.color = Color.white;
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.preferredHeight = 24;

        // Skip button
        _skipButtonGo = new GameObject("SkipButton");
        _skipButtonGo.transform.SetParent(_actionStepGo.transform, false);
        var skipGo = _skipButtonGo;

        var skipLe = skipGo.AddComponent<LayoutElement>();
        skipLe.preferredWidth = 50;
        skipLe.preferredHeight = 24;

        var skipBg = skipGo.AddComponent<Image>();
        skipBg.color = new Color(0.7f, 0.55f, 0.55f);

        var skipBtn = skipGo.AddComponent<Button>();
        skipBtn.targetGraphic = skipBg;
        skipBtn.onClick.AddListener(() => OnSkipClicked?.Invoke());

        var skipTextGo = new GameObject("Text");
        skipTextGo.transform.SetParent(skipGo.transform, false);
        var skipText = skipTextGo.AddComponent<Text>();
        skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipText.fontSize = 12;
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

    /// <summary>
    /// Update all card displays from current hand state.
    /// </summary>
    public void Refresh(Hand hand)
    {
        for (int i = 0; i < _cardUIs.Count && i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            _cardUIs[i].Refresh(
                card.Data.cardName,
                Hand.DescribeActions(card.Data),
                card.CooldownRemaining,
                card.Data.cooldown,
                card.IsReady
            );
        }
    }

    /// <summary>
    /// Highlight the card at the given index as selected. Pass -1 to clear.
    /// </summary>
    public void SetSelectedCard(int index)
    {
        for (int i = 0; i < _cardUIs.Count; i++)
            _cardUIs[i].SetSelected(i == index);
    }

    /// <summary>
    /// Show the current action step indicator above the cards.
    /// </summary>
    public void ShowActionStep(int step, int total, string description, bool canSkip = true)
    {
        _actionStepText.text = $"Step {step}/{total}: {description}";
        _skipButtonGo.SetActive(canSkip);
        _actionStepGo.SetActive(true);
    }

    /// <summary>
    /// Hide the action step indicator.
    /// </summary>
    public void HideActionStep()
    {
        if (_actionStepGo != null)
            _actionStepGo.SetActive(false);
    }
}
