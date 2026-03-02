using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 编辑器工具：一键生成 BattleUI 预制体，保存到 Resources/Prefabs 目录
/// 菜单路径：Tools → 创建战斗UI预制体
/// </summary>
public static class BattleUIPrefabCreator
{
    [MenuItem("Tools/创建战斗UI预制体")]
    public static void CreateBattleUIPrefab()
    {
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        // 创建根Canvas
        var canvasGO = new GameObject("BattleUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 添加 BattleUI 组件
        var battleUI = canvasGO.AddComponent<BattleUI>();
        var so = new SerializedObject(battleUI);

        // ===== 玩家状态面板 =====
        var playerPanel = CreatePanel("PlayerStatus", canvasGO.transform,
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(400, 130),
            new Color(0, 0, 0, 0.7f));

        var playerName = CreateTMP(playerPanel.transform, "PlayerName", "勇者",
            new Vector2(15, -10), new Vector2(200, 30), 22, TextAlignmentOptions.Left);
        so.FindProperty("_playerNameText").objectReferenceValue = playerName;

        CreateTMP(playerPanel.transform, "HPLabel", "HP",
            new Vector2(15, -45), new Vector2(40, 25), 18, TextAlignmentOptions.Left);
        var playerHPSlider = CreateSliderBar(playerPanel.transform, "PlayerHP",
            new Vector2(60, -45), new Vector2(250, 22),
            new Color(0.2f, 0.8f, 0.2f), new Color(0.3f, 0.3f, 0.3f));
        so.FindProperty("_playerHPSlider").objectReferenceValue = playerHPSlider;

        var playerHPText = CreateTMP(playerPanel.transform, "HPValue", "100/100",
            new Vector2(320, -45), new Vector2(80, 25), 16, TextAlignmentOptions.Left);
        so.FindProperty("_playerHPText").objectReferenceValue = playerHPText;

        CreateTMP(playerPanel.transform, "MPLabel", "MP",
            new Vector2(15, -75), new Vector2(40, 25), 18, TextAlignmentOptions.Left);
        var playerMPFill = CreateBar(playerPanel.transform, "PlayerMP",
            new Vector2(60, -75), new Vector2(250, 22),
            new Color(0.3f, 0.5f, 1f), new Color(0.3f, 0.3f, 0.3f));
        so.FindProperty("_playerMPFill").objectReferenceValue = playerMPFill;

        var playerMPText = CreateTMP(playerPanel.transform, "MPValue", "50/50",
            new Vector2(320, -75), new Vector2(80, 25), 16, TextAlignmentOptions.Left);
        so.FindProperty("_playerMPText").objectReferenceValue = playerMPText;

        // ===== 敌人状态面板 =====
        var enemyPanel = CreatePanel("EnemyStatus", canvasGO.transform,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-400, -20), new Vector2(380, 100),
            new Color(0, 0, 0, 0.7f));

        var enemyName = CreateTMP(enemyPanel.transform, "EnemyName", "哥布林",
            new Vector2(15, -10), new Vector2(200, 30), 22, TextAlignmentOptions.Left);
        so.FindProperty("_enemyNameText").objectReferenceValue = enemyName;

        CreateTMP(enemyPanel.transform, "EHPLabel", "HP",
            new Vector2(15, -45), new Vector2(40, 25), 18, TextAlignmentOptions.Left);
        var enemyHPSlider = CreateSliderBar(enemyPanel.transform, "EnemyHP",
            new Vector2(60, -45), new Vector2(250, 22),
            new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.3f, 0.3f));
        so.FindProperty("_enemyHPSlider").objectReferenceValue = enemyHPSlider;

        var enemyHPText = CreateTMP(enemyPanel.transform, "EHPValue", "80/80",
            new Vector2(320, -45), new Vector2(80, 25), 16, TextAlignmentOptions.Left);
        so.FindProperty("_enemyHPText").objectReferenceValue = enemyHPText;

        // ===== 操作按钮面板 =====
        var actionPanel = CreatePanel("ActionPanel", canvasGO.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-250, 20), new Vector2(500, 80),
            new Color(0, 0, 0, 0.8f));
        so.FindProperty("_actionPanel").objectReferenceValue = actionPanel;

        var attackBtn = CreateButton(actionPanel.transform, "AttackBtn", "普通攻击",
            new Vector2(20, -15), new Vector2(140, 50), new Color(0.8f, 0.4f, 0.1f));
        so.FindProperty("_attackButton").objectReferenceValue = attackBtn;

        var skill1Btn = CreateButton(actionPanel.transform, "Skill1Btn", "重击",
            new Vector2(175, -15), new Vector2(140, 50), new Color(0.6f, 0.2f, 0.8f));
        so.FindProperty("_skill1Button").objectReferenceValue = skill1Btn;

        var skill2Btn = CreateButton(actionPanel.transform, "Skill2Btn", "治疗",
            new Vector2(330, -15), new Vector2(140, 50), new Color(0.2f, 0.7f, 0.4f));
        so.FindProperty("_skill2Button").objectReferenceValue = skill2Btn;

        // ===== 战斗日志面板 =====
        var logPanel = CreatePanel("BattleLog", canvasGO.transform,
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-420, 20), new Vector2(400, 200),
            new Color(0, 0, 0, 0.6f));

        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(logPanel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -10);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = Color.clear;
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1);
        contentRT.sizeDelta = Vector2.zero;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var logText = CreateTMP(contentGO.transform, "LogText", "",
            Vector2.zero, Vector2.zero, 16, TextAlignmentOptions.TopLeft);
        var logRT = logText.GetComponent<RectTransform>();
        logRT.anchorMin = new Vector2(0, 1);
        logRT.anchorMax = new Vector2(1, 1);
        logRT.pivot = new Vector2(0, 1);
        logRT.offsetMin = Vector2.zero;
        logRT.offsetMax = Vector2.zero;

        scrollRect.content = contentRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        so.FindProperty("_battleLogText").objectReferenceValue = logText;
        so.FindProperty("_logScrollRect").objectReferenceValue = scrollRect;

        // ===== 结算面板 =====
        var resultPanel = CreatePanel("ResultPanel", canvasGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-200, -100), new Vector2(400, 250),
            new Color(0, 0, 0, 0.85f));
        resultPanel.SetActive(false);
        so.FindProperty("_resultPanel").objectReferenceValue = resultPanel;

        var resultText = CreateTMP(resultPanel.transform, "ResultText", "",
            new Vector2(0, -30), new Vector2(380, 100), 42, TextAlignmentOptions.Center);
        so.FindProperty("_resultText").objectReferenceValue = resultText;

        var restartBtn = CreateButton(resultPanel.transform, "RestartBtn", "再来一次",
            new Vector2(120, -160), new Vector2(160, 50), new Color(0.3f, 0.6f, 0.9f));
        so.FindProperty("_restartButton").objectReferenceValue = restartBtn;

        // 应用序列化
        so.ApplyModifiedPropertiesWithoutUndo();

        // 保存为预制体
        const string path = "Assets/Resources/Prefabs/BattleUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[BattleUIPrefabCreator] 战斗UI预制体已生成：{path}");
    }

    // ========== 构建辅助方法 ==========

    private static GameObject CreatePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }

    private static Image CreateBar(Transform parent, string name,
        Vector2 position, Vector2 size, Color fillColor, Color bgColor)
    {
        var bgGO = new GameObject(name + "BG");
        bgGO.transform.SetParent(parent, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 1);
        bgRT.anchorMax = new Vector2(0, 1);
        bgRT.pivot = new Vector2(0, 1);
        bgRT.anchoredPosition = position;
        bgRT.sizeDelta = size;
        bgGO.AddComponent<Image>().color = bgColor;

        var fillGO = new GameObject(name + "Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        return fillImg;
    }

    private static Slider CreateSliderBar(Transform parent, string name,
        Vector2 position, Vector2 size, Color fillColor, Color bgColor)
    {
        var root = new GameObject(name + "Slider");
        root.transform.SetParent(parent, false);
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0, 1);
        rootRT.anchorMax = new Vector2(0, 1);
        rootRT.pivot = new Vector2(0, 1);
        rootRT.anchoredPosition = position;
        rootRT.sizeDelta = size;

        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;

        var slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.targetGraphic = fillImg;
        slider.fillRect = fillRT;
        slider.interactable = false;

        return slider;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        go.AddComponent<Image>().color = color;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        btn.colors = colors;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }
}
