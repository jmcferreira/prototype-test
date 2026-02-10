using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's hand as a horizontal row of card buttons.
/// Pure display layer — contains no gameplay logic.
/// Fires OnCardClicked(index) when a card button is pressed.
/// Fires OnPassClicked when the pass button is pressed.
/// </summary>
public class HandUI : MonoBehaviour
{
    public event Action<int> OnCardClicked;
    public event Action OnPassClicked;

    private readonly List<CardUI> _cardUIs = new();
    private Canvas _canvas;

    /// <summary>
    /// Build the UI from the given hand. Call once after the hand is created.
    /// </summary>
    public void Init(Hand hand)
    {
        // Screen-space overlay canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Bottom-center panel
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
}
