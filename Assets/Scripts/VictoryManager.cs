using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Creates and shows a simple victory UI at runtime.
public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance;

    private GameObject canvasGO;
    private bool shown = false;
    private Font cachedFont = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowVictory(string title = "You Win!", string subtitle = "Level Complete")
    {
        if (shown) return;
        shown = true;

        // Pause game
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Build a simple Canvas hierarchy
        canvasGO = new GameObject("VictoryCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background Panel
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Attempt to get a readable font safely. Use LegacyRuntime.ttf as the reliable builtin font.
        Font uiFont = null;
        try
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            uiFont = null;
        }

        // If still null, try to find any font present in the project
        if (uiFont == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts != null && allFonts.Length > 0)
                uiFont = allFonts[0];
        }

        // Last-resort: try to create a dynamic font from OS
        if (uiFont == null)
        {
            try { uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { uiFont = null; }
        }

        // Cache for use by CreateButton
        cachedFont = uiFont;

        // Title Text
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleText = titleGO.AddComponent<Text>();
        titleText.text = title;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = uiFont;
        titleText.color = Color.white;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyle.Bold;
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        var trt = titleGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.6f);
        trt.anchorMax = new Vector2(0.9f, 0.85f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var titleShadow = titleGO.AddComponent<UnityEngine.UI.Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        titleShadow.effectDistance = new Vector2(2f, -2f);

        // Subtitle Text
        GameObject subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(panelGO.transform, false);
        var subText = subGO.AddComponent<Text>();
        subText.text = subtitle;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.font = uiFont;
        subText.color = Color.white;
        subText.fontSize = 24;
        subText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subText.verticalOverflow = VerticalWrapMode.Truncate;
        var srt = subGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.2f, 0.45f);
        srt.anchorMax = new Vector2(0.8f, 0.6f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        var subShadow = subGO.AddComponent<UnityEngine.UI.Shadow>();
        subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        subShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // No buttons (Restart/Quit) — UI is informational only
    }

    // Show defeat screen with a single Restart button
    public void ShowDefeat(string title = "You Died", string subtitle = "Try Again")
    {
        if (shown) return;
        shown = true;

        // Pause game
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Build a simple Canvas hierarchy
        canvasGO = new GameObject("VictoryCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background Panel
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Use cachedFont if available or attempt to get one
        Font uiFont = cachedFont;
        if (uiFont == null)
        {
            try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { uiFont = null; }
            if (uiFont == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) uiFont = allFonts[0];
            }
        }

        // Title Text
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleText = titleGO.AddComponent<Text>();
        titleText.text = title;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = uiFont;
        titleText.color = Color.white;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyle.Bold;
        var trt = titleGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.6f);
        trt.anchorMax = new Vector2(0.9f, 0.85f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var titleShadow = titleGO.AddComponent<UnityEngine.UI.Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        titleShadow.effectDistance = new Vector2(2f, -2f);

        // Subtitle Text
        GameObject subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(panelGO.transform, false);
        var subText = subGO.AddComponent<Text>();
        subText.text = subtitle;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.font = uiFont;
        subText.color = Color.white;
        subText.fontSize = 24;
        var srt = subGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.2f, 0.45f);
        srt.anchorMax = new Vector2(0.8f, 0.6f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        var subShadow = subGO.AddComponent<UnityEngine.UI.Shadow>();
        subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        subShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // Create a Restart button centered under the subtitle
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(panelGO.transform, false);
        var imgBtn = btnGO.AddComponent<Image>();
        imgBtn.color = new Color(1f, 1f, 1f, 0.95f);
        var rtBtn = btnGO.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.4f, 0.15f);
        rtBtn.anchorMax = new Vector2(0.6f, 0.25f);
        rtBtn.offsetMin = Vector2.zero;
        rtBtn.offsetMax = Vector2.zero;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        // Button text
        GameObject t = new GameObject("Text");
        t.transform.SetParent(btnGO.transform, false);
        var txt = t.AddComponent<Text>();
        txt.text = "Restart";
        txt.alignment = TextAnchor.MiddleCenter;
        if (uiFont != null) txt.font = uiFont;
        txt.color = Color.black;
        var tr = t.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0);
        tr.anchorMax = new Vector2(1, 1);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }

    private void CreateButton(Transform parent, string text, Vector2 anchorPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGO = new GameObject(text + "Button");
        btnGO.transform.SetParent(parent, false);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        // Text child
        GameObject t = new GameObject("Text");
        t.transform.SetParent(btnGO.transform, false);
        var txt = t.AddComponent<Text>();
        txt.text = text;
        txt.alignment = TextAnchor.MiddleCenter;
        // use cached font when available
        if (cachedFont != null) txt.font = cachedFont;
        else
        {
            try { txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { /* leave null, Unity will fallback in editor if possible */ }
        }
        txt.color = Color.black;
        var tr = t.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0);
        tr.anchorMax = new Vector2(1, 1);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }
}
