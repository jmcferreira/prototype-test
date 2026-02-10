using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen modal shown on victory or defeat.
/// Victory: "Victory!" — Defeat: "You lose...but you can always try again!"
/// Restart button reloads the current scene.
/// </summary>
public class GameOverModalUI : MonoBehaviour
{
    private GameObject _overlay;

    public void Init(TurnManager turnManager)
    {
        turnManager.OnGameOver += Show;
        gameObject.SetActive(false);
    }

    public void Show(bool playerWon)
    {
        gameObject.SetActive(true);
        BuildModal(playerWon);
    }

    private void BuildModal(bool playerWon)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Full-screen darkened overlay
        var overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.65f);
        overlayImg.raycastTarget = true; // blocks clicks to elements behind

        // Central card
        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(transform, false);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(460f, 220f);

        // Border tint
        Color borderColor = playerWon
            ? new Color(0.3f, 0.7f, 0.3f)
            : new Color(0.7f, 0.25f, 0.25f);

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(cardGo.transform, false);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = borderColor;
        borderImg.raycastTarget = false;
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        // Inner dark panel
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(borderGo.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.13f, 0.97f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        // Vertical layout inside inner
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 24, 20);
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Title text
        string title = playerWon ? "Victory!" : "You lose...but you can always try again!";
        Color titleColor = playerWon
            ? new Color(0.4f, 0.9f, 0.4f)
            : new Color(0.9f, 0.35f, 0.35f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(innerGo.transform, false);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = playerWon ? 36 : 22;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = titleColor;
        titleTxt.text = title;
        titleTxt.raycastTarget = false;
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 60f;

        // Restart button
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(innerGo.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = playerWon
            ? new Color(0.2f, 0.55f, 0.2f)
            : new Color(0.55f, 0.2f, 0.2f);
        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 180f;
        btnLe.preferredHeight = 50f;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(RestartGame);

        // Button hover colors
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        var btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnTxt = btnTextGo.AddComponent<Text>();
        btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTxt.fontSize = 24;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = Color.white;
        btnTxt.text = "Restart";
        var btnTxtRect = btnTextGo.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.offsetMin = Vector2.zero;
        btnTxtRect.offsetMax = Vector2.zero;
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
