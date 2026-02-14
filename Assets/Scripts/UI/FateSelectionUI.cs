using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal UI that shows 2 Fate cards and lets the player pick one.
/// Both cards are always discarded; only the picked card's effects apply.
/// </summary>
public class FateSelectionUI : MonoBehaviour
{
    private GameObject _root;
    private Text _headerText;
    private GameObject _card0Go;
    private GameObject _card1Go;
    private Text _card0Name;
    private Text _card0Desc;
    private Text _card1Name;
    private Text _card1Desc;

    private Action<FateCardData> _onPicked;
    private FateCardData _data0;
    private FateCardData _data1;

    public void Init(Transform canvasTransform)
    {
        Build(canvasTransform);
        _root.SetActive(false);
    }

    public void Show(FateCardData card0, FateCardData card1, string header, Action<FateCardData> onPicked)
    {
        _data0 = card0;
        _data1 = card1;
        _onPicked = onPicked;

        _headerText.text = header;
        _card0Name.text = card0.cardName;
        _card0Desc.text = DescribeCard(card0);
        _card1Name.text = card1.cardName;
        _card1Desc.text = DescribeCard(card1);

        _root.SetActive(true);
    }

    private void Pick(FateCardData picked)
    {
        _root.SetActive(false);
        _onPicked?.Invoke(picked);
        _onPicked = null;
    }

    private static string DescribeCard(FateCardData card)
    {
        var lines = new System.Collections.Generic.List<string>();
        if (card.damageBonus > 0) lines.Add($"+{card.damageBonus} Damage");
        if (card.blockBonus > 0) lines.Add($"+{card.blockBonus} Block");
        if (card.dodgeBonus > 0) lines.Add($"+{card.dodgeBonus} Dodge");
        if (card.statusStacks > 0)
        {
            var def = StatusEffectDefs.Get(card.statusEffect);
            lines.Add($"Apply {def.Name} {card.statusStacks}");
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "No effect";
    }

    // ── Build UI ──────────────────────────────────────────────────────────

    private void Build(Transform canvasTransform)
    {
        // Root overlay (dims background)
        _root = new GameObject("FateSelectionRoot");
        _root.transform.SetParent(canvasTransform, false);
        var rootRect = _root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var dimImg = _root.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.6f);
        dimImg.raycastTarget = true; // Block clicks behind

        // Center container
        var containerGo = new GameObject("FateContainer");
        containerGo.transform.SetParent(_root.transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(440f, 280f);

        // Container background
        var containerImg = containerGo.AddComponent<Image>();
        containerImg.color = new Color(0.08f, 0.06f, 0.12f, 0.95f);
        containerImg.raycastTarget = false;

        // Vertical layout
        var vl = containerGo.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(12, 12, 10, 10);
        vl.spacing = 8;
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // Header text
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(containerGo.transform, false);
        _headerText = headerGo.AddComponent<Text>();
        _headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _headerText.fontSize = 18;
        _headerText.fontStyle = FontStyle.Bold;
        _headerText.alignment = TextAnchor.MiddleCenter;
        _headerText.color = new Color(1f, 0.85f, 0.4f);
        _headerText.raycastTarget = false;
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 30;

        // Cards row
        var rowGo = new GameObject("CardsRow");
        rowGo.transform.SetParent(containerGo.transform, false);
        var rowRect = rowGo.AddComponent<RectTransform>();
        var hl = rowGo.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 16;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childForceExpandWidth = true;
        hl.childForceExpandHeight = true;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 200;
        rowLe.flexibleHeight = 1;

        // Build both card panels
        _card0Go = BuildCardPanel(rowGo.transform, out _card0Name, out _card0Desc);
        _card1Go = BuildCardPanel(rowGo.transform, out _card1Name, out _card1Desc);

        // Wire click events
        _card0Go.AddComponent<Button>().onClick.AddListener(() => Pick(_data0));
        _card1Go.AddComponent<Button>().onClick.AddListener(() => Pick(_data1));
    }

    private static GameObject BuildCardPanel(Transform parent, out Text nameText, out Text descText)
    {
        var cardGo = new GameObject("FateCard");
        cardGo.transform.SetParent(parent, false);

        // Background
        var img = cardGo.AddComponent<Image>();
        img.color = new Color(0.15f, 0.12f, 0.2f, 1f);
        img.raycastTarget = true;

        // Vertical layout inside card
        var vl = cardGo.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(8, 8, 8, 8);
        vl.spacing = 6;
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // Card name
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(cardGo.transform, false);
        nameText = nameGo.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = new Color(1f, 0.9f, 0.6f);
        nameText.raycastTarget = false;
        var nameLe = nameGo.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 24;

        // Divider
        var divGo = new GameObject("Divider");
        divGo.transform.SetParent(cardGo.transform, false);
        var divImg = divGo.AddComponent<Image>();
        divImg.color = new Color(0.4f, 0.35f, 0.5f, 0.6f);
        divImg.raycastTarget = false;
        var divLe = divGo.AddComponent<LayoutElement>();
        divLe.preferredHeight = 1;

        // Description
        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(cardGo.transform, false);
        descText = descGo.AddComponent<Text>();
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 14;
        descText.fontStyle = FontStyle.Normal;
        descText.alignment = TextAnchor.UpperCenter;
        descText.color = new Color(0.85f, 0.85f, 0.85f);
        descText.raycastTarget = false;
        descText.verticalOverflow = VerticalWrapMode.Overflow;
        var descLe = descGo.AddComponent<LayoutElement>();
        descLe.flexibleHeight = 1;

        return cardGo;
    }
}
