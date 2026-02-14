using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Game over modal for PVP matches. Shows winner and offers rematch or menu.
/// </summary>
public class PvpGameOverUI : MonoBehaviour
{
    private GameObject _panel;

    public void Init(PvpTurnManager turnManager)
    {
        // Hidden until game over
        _panel = CreatePanel();
        _panel.SetActive(false);

        turnManager.OnGameOver += (p1Won) => Show(p1Won);
    }

    private void Show(bool p1Won)
    {
        _panel.SetActive(true);

        var titleText = _panel.transform.Find("Title")?.GetComponent<Text>();
        if (titleText != null)
        {
            titleText.text = p1Won ? "Player 1 Wins!" : "Player 2 Wins!";
            titleText.color = p1Won ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.3f);
        }
    }

    private GameObject CreatePanel()
    {
        var panel = new GameObject("PvpGameOverPanel");
        panel.transform.SetParent(transform, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.65f);
        titleRt.anchorMax = new Vector2(0.5f, 0.65f);
        titleRt.sizeDelta = new Vector2(500, 60);
        var titleText = titleGo.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // Rematch button
        CreateButton(panel, "Rematch", new Vector2(0, -20), new Color(0.2f, 0.5f, 0.3f), () =>
        {
            GameModeState.CurrentMode = GameMode.PVP;
            PvpMatchState.Current = null;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        // Main menu button
        CreateButton(panel, "Main Menu", new Vector2(0, -90), new Color(0.4f, 0.3f, 0.2f), () =>
        {
            GameModeState.CurrentMode = GameMode.None;
            PvpMatchState.Current = null;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        return panel;
    }

    private void CreateButton(GameObject parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject($"Btn_{label}");
        btnGo.transform.SetParent(parent.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = pos;
        btnRt.sizeDelta = new Vector2(250, 50);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = color;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }
}
