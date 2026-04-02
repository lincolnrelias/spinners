using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    // HP bar data (one per side)
    struct HPBar
    {
        public TMP_Text  label;
        public Image     fill;
        public TMP_Text  nameLabel;
    }

    HPBar    _barA, _barB;
    TMP_Text _countdownText;
    TMP_Text _resultText;

    TopBase _topA, _topB;
    float   _hudTimer;
    const float HUD_THROTTLE = 1f / GameConfig.HudThrottleFps;

    // ── Init ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

        // Canvas
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        BuildHUD();
    }

    void BuildHUD()
    {
        // — Berserker bar (left) —
        _barA = BuildHPBar("BarA",
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            pivot:     new Vector2(0f, 1f),
            pos:       new Vector2(20f, -20f));

        // — Parasita bar (right) —
        _barB = BuildHPBar("BarB",
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            pivot:     new Vector2(1f, 1f),
            pos:       new Vector2(-20f, -20f));

        // — Countdown (center) —
        _countdownText = MakeTMP("Countdown", "",
            fontSize: 120f,
            anchor: new Vector2(0.5f, 0.5f),
            pos:    Vector2.zero,
            size:   new Vector2(400f, 200f),
            align:  TextAlignmentOptions.Center);
        _countdownText.fontStyle = FontStyles.Bold;
        _countdownText.color     = Color.white;

        // — Result (center) —
        _resultText = MakeTMP("Result", "",
            fontSize: 64f,
            anchor: new Vector2(0.5f, 0.5f),
            pos:    Vector2.zero,
            size:   new Vector2(600f, 300f),
            align:  TextAlignmentOptions.Center);
        _resultText.fontStyle = FontStyles.Bold;
        _resultText.color     = Color.yellow;
        _resultText.gameObject.SetActive(false);
    }

    HPBar BuildHPBar(string id, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos)
    {
        // Container
        var container = new GameObject(id);
        container.transform.SetParent(transform, false);
        var crt = container.AddComponent<RectTransform>();
        crt.anchorMin        = anchorMin;
        crt.anchorMax        = anchorMax;
        crt.pivot            = pivot;
        crt.anchoredPosition = pos;
        crt.sizeDelta        = new Vector2(220f, 70f);

        // Name label
        var nameLabel = MakeTMPIn(container.transform, $"{id}_Name", "---",
            fontSize: 18f,
            offset:   new Vector2(0f, 0f),
            size:     new Vector2(220f, 28f),
            align:    TextAlignmentOptions.MidlineLeft);
        nameLabel.fontStyle = FontStyles.Bold;

        // Bar background
        var bg    = new GameObject($"{id}_BG");
        bg.transform.SetParent(container.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        var bgRt  = bgImg.rectTransform;
        bgRt.anchorMin        = new Vector2(0f, 1f);
        bgRt.anchorMax        = new Vector2(0f, 1f);
        bgRt.pivot            = new Vector2(0f, 1f);
        bgRt.anchoredPosition = new Vector2(0f, -30f);
        bgRt.sizeDelta        = new Vector2(220f, 18f);

        // Fill
        var fillGO  = new GameObject($"{id}_Fill");
        fillGO.transform.SetParent(bg.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color      = Color.green;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 1f;
        var fillRt = fillImg.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        // HP text
        var hpText = MakeTMPIn(container.transform, $"{id}_HP", "",
            fontSize: 13f,
            offset:   new Vector2(0f, -50f),
            size:     new Vector2(220f, 22f),
            align:    TextAlignmentOptions.MidlineLeft);
        hpText.color = new Color(0.9f, 0.9f, 0.9f);

        return new HPBar { label = hpText, fill = fillImg, nameLabel = nameLabel };
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void SetTops(TopBase a, TopBase b)
    {
        _topA = a; _topB = b;
        _barA.nameLabel.text  = a.CharacterName.ToUpper();
        _barA.nameLabel.color = a.CharacterColor;
        _barB.nameLabel.text  = b.CharacterName.ToUpper();
        _barB.nameLabel.color = b.CharacterColor;
    }

    public void ShowCountdown(int seconds) => StartCoroutine(CountdownRoutine(seconds));

    IEnumerator CountdownRoutine(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            _countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        _countdownText.text = "FIGHT!";
        yield return new WaitForSeconds(0.5f);
        _countdownText.text = "";
    }

    public void ShowResult(string winnerName)
    {
        _resultText.text = $"{winnerName.ToUpper()}\nVENCEU!";
        _resultText.gameObject.SetActive(true);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (_topA == null || _topB == null) return;
        _hudTimer += Time.deltaTime;
        if (_hudTimer < HUD_THROTTLE) return;
        _hudTimer = 0f;

        RefreshBar(_topA, _barA);
        RefreshBar(_topB, _barB);
    }

    void RefreshBar(TopBase top, HPBar bar)
    {
        float ratio = top.HPRatio;

        Color hpColor = ratio > 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f)
            : Color.Lerp(Color.red, Color.yellow, ratio * 2f);

        bar.fill.fillAmount = ratio;
        bar.fill.color      = hpColor;
        bar.label.text      = $"{NumberFormat.Fmt(top.HP)} / {NumberFormat.Fmt(top.HPMax)}";
    }

    // ── TMP helpers ──────────────────────────────────────────────────────────

    TMP_Text MakeTMP(string id, string text, float fontSize,
        Vector2 anchor, Vector2 pos, Vector2 size, TextAlignmentOptions align)
    {
        var go  = new GameObject(id);
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = align;
        var rt = tmp.rectTransform;
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return tmp;
    }

    TMP_Text MakeTMPIn(Transform parent, string id, string text, float fontSize,
        Vector2 offset, Vector2 size, TextAlignmentOptions align)
    {
        var go  = new GameObject(id);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = align;
        var rt = tmp.rectTransform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta        = size;
        return tmp;
    }
}
