using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BattleUIPrefabCreator
{
    [MenuItem("Tools/创建战斗UI预制体")]
    public static void CreateBattleUIPrefab()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");

        var canvasGO = new GameObject("BattleUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var battleUI = canvasGO.AddComponent<BattleUI>();
        var so = new SerializedObject(battleUI);

        // ===== 玩家单位容器 (左侧) =====
        var playerPanel = CreateContainer("PlayerUnitsPanel", canvasGO.transform,
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10), new Vector2(360, 400));
        AddVerticalLayout(playerPanel, 4);
        so.FindProperty("_playerUnitsContainer").objectReferenceValue = playerPanel.transform;

        // ===== 敌方单位容器 (右侧) =====
        var enemyPanel = CreateContainer("EnemyUnitsPanel", canvasGO.transform,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-370, -10), new Vector2(360, 400));
        AddVerticalLayout(enemyPanel, 4);
        so.FindProperty("_enemyUnitsContainer").objectReferenceValue = enemyPanel.transform;

        // ===== 操作面板 (底部居中) =====
        var actionPanel = CreatePanel("ActionPanel", canvasGO.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-350, 20), new Vector2(700, 110),
            new Color(0, 0, 0, 0.85f));
        actionPanel.SetActive(false);
        so.FindProperty("_actionPanel").objectReferenceValue = actionPanel;

        var unitLabel = CreateTMP(actionPanel.transform, "CurrentUnitLabel", "",
            new Vector2(10, -5), new Vector2(300, 25), 18, TextAlignmentOptions.Left);
        so.FindProperty("_currentUnitLabel").objectReferenceValue = unitLabel;

        var mpLabel = CreateTMP(actionPanel.transform, "MPLabel", "",
            new Vector2(320, -5), new Vector2(150, 25), 16, TextAlignmentOptions.Left);
        so.FindProperty("_mpLabel").objectReferenceValue = mpLabel;

        var btnContainer = new GameObject("ActionButtons");
        btnContainer.transform.SetParent(actionPanel.transform, false);
        var btnRT = btnContainer.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 0);
        btnRT.anchorMax = new Vector2(1, 1);
        btnRT.offsetMin = new Vector2(10, 10);
        btnRT.offsetMax = new Vector2(-10, -35);
        var hlg = btnContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        so.FindProperty("_actionButtonsContainer").objectReferenceValue = btnContainer.transform;

        // ===== 目标提示 =====
        var targetPrompt = CreatePanel("TargetPrompt", canvasGO.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-100, 135), new Vector2(200, 30),
            new Color(0.8f, 0.2f, 0.2f, 0.9f));
        var promptTMP = CreateTMP(targetPrompt.transform, "Text", "← 点击敌方单位选择目标 →",
            Vector2.zero, Vector2.zero, 14, TextAlignmentOptions.Center);
        var promptRT = promptTMP.GetComponent<RectTransform>();
        promptRT.anchorMin = Vector2.zero;
        promptRT.anchorMax = Vector2.one;
        promptRT.offsetMin = Vector2.zero;
        promptRT.offsetMax = Vector2.zero;
        targetPrompt.SetActive(false);
        so.FindProperty("_targetPrompt").objectReferenceValue = targetPrompt;

        // ===== 战斗日志 (右下) =====
        var logPanel = CreatePanel("BattleLog", canvasGO.transform,
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-420, 140), new Vector2(400, 200),
            new Color(0, 0, 0, 0.6f));
        BuildScrollLog(logPanel.transform, so);

        // ===== 结算面板 =====
        var resultPanel = CreatePanel("ResultPanel", canvasGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-220, -130), new Vector2(440, 260),
            new Color(0, 0, 0, 0.88f));
        resultPanel.SetActive(false);
        so.FindProperty("_resultPanel").objectReferenceValue = resultPanel;

        var resultText = CreateTMP(resultPanel.transform, "ResultText", "",
            new Vector2(20, -20), new Vector2(400, 80), 42, TextAlignmentOptions.Center);
        so.FindProperty("_resultText").objectReferenceValue = resultText;

        var restartBtn = CreateButton(resultPanel.transform, "RestartBtn", "再来一次",
            new Vector2(40, -150), new Vector2(160, 50), new Color(0.3f, 0.6f, 0.9f));
        so.FindProperty("_restartButton").objectReferenceValue = restartBtn;

        var editFormBtn = CreateButton(resultPanel.transform, "EditFormationBtn", "编辑编队",
            new Vector2(240, -150), new Vector2(160, 50), new Color(0.6f, 0.5f, 0.2f));
        so.FindProperty("_editFormationButton").objectReferenceValue = editFormBtn;

        // ===== 编队面板 =====
        var formationPanel = CreatePanel("FormationPanel", canvasGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-250, -220), new Vector2(500, 440),
            new Color(0.05f, 0.05f, 0.1f, 0.95f));
        formationPanel.SetActive(false);
        so.FindProperty("_formationPanel").objectReferenceValue = formationPanel;

        CreateTMP(formationPanel.transform, "Title", "编队选择",
            new Vector2(20, -10), new Vector2(200, 35), 26, TextAlignmentOptions.Left);

        var slotsInfo = CreateTMP(formationPanel.transform, "SlotsInfo", "已选 0/3",
            new Vector2(280, -10), new Vector2(200, 35), 20, TextAlignmentOptions.Right);
        so.FindProperty("_slotsInfoText").objectReferenceValue = slotsInfo;

        // 编队单位滚动区
        var formScroll = new GameObject("FormScroll");
        formScroll.transform.SetParent(formationPanel.transform, false);
        var fsRT = formScroll.AddComponent<RectTransform>();
        fsRT.anchorMin = new Vector2(0, 0);
        fsRT.anchorMax = new Vector2(1, 1);
        fsRT.offsetMin = new Vector2(20, 70);
        fsRT.offsetMax = new Vector2(-20, -55);
        formScroll.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
        formScroll.AddComponent<Mask>().showMaskGraphic = true;
        var fScrollRect = formScroll.AddComponent<ScrollRect>();
        fScrollRect.horizontal = false;
        fScrollRect.vertical = true;

        var formContent = new GameObject("FormContent");
        formContent.transform.SetParent(formScroll.transform, false);
        var fcRT = formContent.AddComponent<RectTransform>();
        fcRT.anchorMin = new Vector2(0, 1);
        fcRT.anchorMax = new Vector2(1, 1);
        fcRT.pivot = new Vector2(0, 1);
        fcRT.sizeDelta = new Vector2(0, 0);
        formContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        AddVerticalLayout(formContent, 5);
        fScrollRect.content = fcRT;
        so.FindProperty("_formationUnitsContainer").objectReferenceValue = formContent.transform;

        var startBtn = CreateButton(formationPanel.transform, "StartBattleBtn", "开始战斗",
            new Vector2(160, -385), new Vector2(180, 45), new Color(0.2f, 0.7f, 0.3f));
        so.FindProperty("_startBattleButton").objectReferenceValue = startBtn;

        // 保存
        so.ApplyModifiedPropertiesWithoutUndo();

        const string path = "Assets/Resources/Prefabs/BattleUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
        Object.DestroyImmediate(canvasGO);
        AssetDatabase.Refresh();
        Debug.Log($"[BattleUIPrefabCreator] 战斗UI预制体已生成：{path}");
    }

    // ========== 构建辅助 ==========

    private static void BuildScrollLog(Transform parent, SerializedObject so)
    {
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(parent, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(8, 8);
        scrollRT.offsetMax = new Vector2(-8, -8);
        scrollGO.AddComponent<Image>().color = Color.clear;
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1);
        contentRT.sizeDelta = Vector2.zero;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var logText = CreateTMP(contentGO.transform, "LogText", "",
            Vector2.zero, Vector2.zero, 14, TextAlignmentOptions.TopLeft);
        var logRT = logText.GetComponent<RectTransform>();
        logRT.anchorMin = new Vector2(0, 1);
        logRT.anchorMax = new Vector2(1, 1);
        logRT.pivot = new Vector2(0, 1);
        logRT.offsetMin = Vector2.zero;
        logRT.offsetMax = Vector2.zero;

        scrollRect.content = contentRT;
        so.FindProperty("_battleLogText").objectReferenceValue = logText;
        so.FindProperty("_logScrollRect").objectReferenceValue = scrollRect;
    }

    private static GameObject CreateContainer(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go;
    }

    private static void AddVerticalLayout(GameObject go, float spacing)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
    }

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
        go.AddComponent<Image>().color = bgColor;
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
        var c = btn.colors;
        c.normalColor = color;
        c.highlightedColor = color * 1.2f;
        c.pressedColor = color * 0.8f;
        c.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        btn.colors = c;

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

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var idx = folder.LastIndexOf('/');
        var parent = folder.Substring(0, idx);
        var current = folder.Substring(idx + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, current);
    }
}
