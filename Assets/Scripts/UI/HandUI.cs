using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Displays the player's hand as a horizontal row of card buttons.
/// Pure display layer — contains no gameplay logic.
/// Step highlighting and skip buttons live directly on each CardUI.
/// </summary>
public class HandUI : MonoBehaviour
{
    public event Action<int> OnCardClicked;
    public event Action OnPassClicked;
    public event Action OnSkipClicked;

    private readonly List<CardUI> _cardUIs = new();
    private Canvas _canvas;
    private int _selectedCardIndex = -1;

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
        bgRect.sizeDelta = new Vector2(0f, 220f);

        // Card row — centered on the background strip
        var panelGo = new GameObject("CardPanel");
        panelGo.transform.SetParent(transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 15f);

        var layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = -60;
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
            cardUI.OnSkipClicked += () => OnSkipClicked?.Invoke();

            _cardUIs.Add(cardUI);
        }

        // End Turn button — anchored at top-center, just below the turn banner
        var passGo = new GameObject("EndTurnButton");
        passGo.transform.SetParent(transform, false);

        var passRect = passGo.AddComponent<RectTransform>();
        passRect.anchorMin = new Vector2(0.5f, 1f);
        passRect.anchorMax = new Vector2(0.5f, 1f);
        passRect.pivot = new Vector2(0.5f, 1f);
        passRect.anchoredPosition = new Vector2(0f, -85f); // below the 70px banner + gap
        passRect.sizeDelta = new Vector2(112f, 51f);

        var passBg = passGo.AddComponent<Image>();
        passBg.color = new Color(0.85f, 0.75f, 0.65f);

        var passBtn = passGo.AddComponent<Button>();
        passBtn.targetGraphic = passBg;
        passBtn.onClick.AddListener(() => OnPassClicked?.Invoke());

        var passTextGo = new GameObject("Text");
        passTextGo.transform.SetParent(passGo.transform, false);
        var passText = passTextGo.AddComponent<Text>();
        passText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        passText.fontSize = 26;
        passText.alignment = TextAnchor.MiddleCenter;
        passText.color = Color.black;
        passText.text = "End Turn";
        var textRect = passTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Initial refresh
        Refresh(hand);
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
    /// </summary>
    public void SetSelectedCard(int index)
    {
        // Clear step highlights on previous card
        if (_selectedCardIndex >= 0 && _selectedCardIndex < _cardUIs.Count)
            _cardUIs[_selectedCardIndex].ClearActiveStep();

        _selectedCardIndex = index;

        for (int i = 0; i < _cardUIs.Count; i++)
            _cardUIs[i].SetSelected(i == index);
    }

    // ── Action Step ─────────────────────────────────────────────────────

    /// <summary>
    /// Highlight the current action step on the selected card.
    /// </summary>
    public void ShowActionStep(int step, int total, string description, bool canSkip = true)
    {
        int zeroStep = step - 1;

        if (_selectedCardIndex >= 0 && _selectedCardIndex < _cardUIs.Count)
            _cardUIs[_selectedCardIndex].SetActiveStep(zeroStep, canSkip);
    }

    /// <summary>
    /// Clear card step highlights.
    /// </summary>
    public void HideActionStep()
    {
        if (_selectedCardIndex >= 0 && _selectedCardIndex < _cardUIs.Count)
            _cardUIs[_selectedCardIndex].ClearActiveStep();
    }

    /// <summary>
    /// No-op kept for API compat — expanded card panel was removed.
    /// </summary>
    public void ShowExpandedCard(CardData data) { }
}
