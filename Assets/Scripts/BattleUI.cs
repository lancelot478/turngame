using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUI : MonoBehaviour
{
    [Header("单位容器")]
    [SerializeField] private Transform _playerUnitsContainer;
    [SerializeField] private Transform _enemyUnitsContainer;

    [Header("操作面板")]
    [SerializeField] private GameObject _actionPanel;
    [SerializeField] private TextMeshProUGUI _currentUnitLabel;
    [SerializeField] private TextMeshProUGUI _mpLabel;
    [SerializeField] private Transform _actionButtonsContainer;

    [Header("目标选择")]
    [SerializeField] private GameObject _targetPrompt;

    [Header("战斗日志")]
    [SerializeField] private TextMeshProUGUI _battleLogText;
    [SerializeField] private ScrollRect _logScrollRect;

    [Header("结算面板")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _editFormationButton;

    [Header("编队面板")]
    [SerializeField] private GameObject _formationPanel;
    [SerializeField] private TextMeshProUGUI _slotsInfoText;
    [SerializeField] private Transform _formationUnitsContainer;
    [SerializeField] private Button _startBattleButton;

    // 运行时数据
    private BattleManager _battleManager;
    private readonly List<UnitCardUI> _playerCards = new List<UnitCardUI>();
    private readonly List<UnitCardUI> _enemyCards = new List<UnitCardUI>();
    private readonly List<GameObject> _actionButtons = new List<GameObject>();
    private readonly List<string> _logMessages = new List<string>();
    private const int MaxLogLines = 50;

    private Action _onRestart;
    private Action _onEditFormation;

    // 编队运行时
    private Action<List<UnitConfig>> _formationConfirmCallback;
    private TeamFormationConfig _formationConfig;
    private readonly List<UnitConfig> _formationSelected = new List<UnitConfig>();
    private readonly List<FormationSlotUI> _formationSlots = new List<FormationSlotUI>();

    // ========== 初始化 ==========

    public void Setup(Action onRestart, Action onEditFormation)
    {
        _onRestart = onRestart;
        _onEditFormation = onEditFormation;
        _restartButton.onClick.AddListener(() => _onRestart?.Invoke());
        _editFormationButton.onClick.AddListener(() => _onEditFormation?.Invoke());

        _actionPanel.SetActive(false);
        _resultPanel.SetActive(false);
        _formationPanel.SetActive(false);
        if (_targetPrompt != null) _targetPrompt.SetActive(false);
    }

    public void InitBattle(BattleManager battleManager)
    {
        UnsubscribeEvents();
        _battleManager = battleManager;

        ClearCards();
        ClearLog();

        foreach (var u in _battleManager.PlayerUnits)
            _playerCards.Add(CreateUnitCard(_playerUnitsContainer, u, false));
        foreach (var u in _battleManager.EnemyUnits)
            _enemyCards.Add(CreateUnitCard(_enemyUnitsContainer, u, true));

        _actionPanel.SetActive(false);
        _resultPanel.SetActive(false);
        _formationPanel.SetActive(false);
        if (_targetPrompt != null) _targetPrompt.SetActive(false);

        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (_battleManager == null) return;
        _battleManager.OnBattleLog += AddLog;
        _battleManager.OnStateChanged += RefreshUI;
        _battleManager.OnUnitHit += PlayHitEffect;
        _battleManager.OnBattleEnd += ShowResult;
    }

    private void UnsubscribeEvents()
    {
        if (_battleManager == null) return;
        _battleManager.OnBattleLog -= AddLog;
        _battleManager.OnStateChanged -= RefreshUI;
        _battleManager.OnUnitHit -= PlayHitEffect;
        _battleManager.OnBattleEnd -= ShowResult;
    }

    // ========== UI刷新 ==========

    private void RefreshUI()
    {
        RefreshAllCards();
        RefreshActionPanel();
        RefreshTargetSelection();
    }

    private void RefreshAllCards()
    {
        foreach (var card in _playerCards)
            RefreshCard(card);
        foreach (var card in _enemyCards)
            RefreshCard(card);
    }

    private void RefreshCard(UnitCardUI card)
    {
        var u = card.Unit;

        card.NameText.text = u.IsDefending ? $"{u.Name}[防]" : u.Name;
        card.HPSlider.value = u.HPPercent;
        card.HPText.text = $"{u.CurrentHP}/{u.MaxHP}";
        card.ATBFill.fillAmount = u.ATBPercent;

        bool isActive = _battleManager.CurrentActiveUnit == u;
        bool isDead = !u.IsAlive;

        if (isDead)
            card.Background.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        else if (isActive)
            card.Background.color = new Color(0.9f, 0.75f, 0.1f, 0.9f);
        else
            card.Background.color = card.DefaultBgColor;

        card.NameText.color = isDead ? Color.gray : Color.white;
    }

    private void RefreshActionPanel()
    {
        bool showAction = _battleManager.State == BattleState.WaitingForInput &&
                          _battleManager.CurrentActiveUnit != null;

        _actionPanel.SetActive(showAction);

        if (!showAction) return;

        var unit = _battleManager.CurrentActiveUnit;
        _currentUnitLabel.text = $"{unit.Name} 的回合";
        _mpLabel.text = $"MP: {unit.CurrentMP}/{unit.MaxMP}";

        RebuildActionButtons(unit);
    }

    private void RebuildActionButtons(UnitRuntime unit)
    {
        foreach (var go in _actionButtons)
            Destroy(go);
        _actionButtons.Clear();

        _actionButtons.Add(CreateActionButton("普通攻击", new Color(0.8f, 0.4f, 0.1f),
            () => _battleManager.PlayerChooseAttack()));

        for (int i = 0; i < unit.Skills.Count; i++)
        {
            var skill = unit.Skills[i];
            int idx = i;
            bool canUse = unit.CanUseSkill(skill);
            string label = skill.Type == SkillType.Heal
                ? $"{skill.Name}(回复)"
                : $"{skill.Name}({skill.MPCost}MP)";

            Color color = skill.Type == SkillType.Heal
                ? new Color(0.2f, 0.7f, 0.4f)
                : new Color(0.6f, 0.2f, 0.8f);

            var btn = CreateActionButton(label, color, () => _battleManager.PlayerChooseSkill(idx));
            btn.GetComponent<Button>().interactable = canUse;
            _actionButtons.Add(btn);
        }

        _actionButtons.Add(CreateActionButton("取消", new Color(0.5f, 0.5f, 0.5f),
            () => { }));
        _actionButtons[_actionButtons.Count - 1].SetActive(false);
    }

    private void RefreshTargetSelection()
    {
        bool isSelecting = _battleManager.State == BattleState.SelectingTarget;

        if (_targetPrompt != null)
            _targetPrompt.SetActive(isSelecting);

        foreach (var card in _enemyCards)
        {
            if (card.CardButton == null) continue;
            card.CardButton.interactable = isSelecting && card.Unit.IsAlive;

            if (isSelecting && card.Unit.IsAlive)
                card.Background.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);
        }

        // 显示取消按钮
        if (_actionButtons.Count > 0)
        {
            var cancelBtn = _actionButtons[_actionButtons.Count - 1];
            cancelBtn.SetActive(isSelecting);
            if (isSelecting)
            {
                cancelBtn.GetComponent<Button>().onClick.RemoveAllListeners();
                cancelBtn.GetComponent<Button>().onClick.AddListener(() => _battleManager.CancelTargetSelection());
            }
        }

        _actionPanel.SetActive(
            _battleManager.State == BattleState.WaitingForInput ||
            _battleManager.State == BattleState.SelectingTarget);
    }

    // ========== 战斗日志 ==========

    private void AddLog(string message)
    {
        _logMessages.Add(message);
        if (_logMessages.Count > MaxLogLines)
            _logMessages.RemoveAt(0);
        _battleLogText.text = string.Join("\n", _logMessages);
        Canvas.ForceUpdateCanvases();
        _logScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ClearLog()
    {
        _logMessages.Clear();
        _battleLogText.text = "";
    }

    // ========== 受击动画 ==========

    private void PlayHitEffect(UnitRuntime target)
    {
        if (target.Model != null)
            StartCoroutine(HitAnimation(target.Model));
    }

    private IEnumerator HitAnimation(Transform model)
    {
        var renderer = model.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Color originalColor = renderer.material.color;
        renderer.material.color = Color.red;
        Vector3 originalPos = model.position;
        float elapsed = 0f;

        while (elapsed < 0.3f)
        {
            float offset = Mathf.Sin(elapsed * 40f) * 0.15f;
            model.position = originalPos + new Vector3(offset, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        model.position = originalPos;
        renderer.material.color = originalColor;
    }

    // ========== 结算 ==========

    private void ShowResult(BattleState result)
    {
        _actionPanel.SetActive(false);
        _resultPanel.SetActive(true);
        if (result == BattleState.Won)
        {
            _resultText.text = "胜 利！";
            _resultText.color = new Color(1f, 0.85f, 0.2f);
        }
        else
        {
            _resultText.text = "失 败...";
            _resultText.color = new Color(0.8f, 0.2f, 0.2f);
        }
    }

    // ========== 编队面板 ==========

    public void ShowFormation(TeamFormationConfig config, List<UnitConfig> currentDeployed,
        Action<List<UnitConfig>> onConfirm)
    {
        _formationConfig = config;
        _formationConfirmCallback = onConfirm;
        _formationSelected.Clear();
        _formationSelected.AddRange(currentDeployed);

        _resultPanel.SetActive(false);
        _actionPanel.SetActive(false);
        _formationPanel.SetActive(true);

        RebuildFormationSlots();
        RefreshFormationUI();

        _startBattleButton.onClick.RemoveAllListeners();
        _startBattleButton.onClick.AddListener(OnFormationConfirm);
    }

    private void RebuildFormationSlots()
    {
        foreach (var slot in _formationSlots)
            Destroy(slot.Root);
        _formationSlots.Clear();

        if (_formationConfig == null) return;

        foreach (var uc in _formationConfig.unitPool)
        {
            if (uc == null) continue;
            var slot = CreateFormationSlot(uc);
            _formationSlots.Add(slot);
        }
    }

    private void RefreshFormationUI()
    {
        int max = _formationConfig != null ? _formationConfig.maxActiveSlots : 3;
        _slotsInfoText.text = $"已选 {_formationSelected.Count}/{max}";

        foreach (var slot in _formationSlots)
        {
            bool selected = _formationSelected.Contains(slot.Config);
            slot.Toggle.SetIsOnWithoutNotify(selected);

            var colors = slot.Toggle.colors;
            colors.normalColor = selected ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
            slot.Toggle.colors = colors;
            slot.Background.color = selected ? new Color(0.15f, 0.4f, 0.2f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        _startBattleButton.interactable = _formationSelected.Count > 0;
    }

    private void OnFormationToggle(UnitConfig config, bool isOn)
    {
        int max = _formationConfig != null ? _formationConfig.maxActiveSlots : 3;

        if (isOn)
        {
            if (_formationSelected.Count >= max)
            {
                AddLog($"上场位已满（最多{max}个单位）！");
                RefreshFormationUI();
                return;
            }
            if (!_formationSelected.Contains(config))
                _formationSelected.Add(config);
        }
        else
        {
            _formationSelected.Remove(config);
        }

        RefreshFormationUI();
    }

    private void OnFormationConfirm()
    {
        if (_formationSelected.Count == 0)
        {
            AddLog("至少选择1个单位！");
            return;
        }
        _formationPanel.SetActive(false);
        _formationConfirmCallback?.Invoke(new List<UnitConfig>(_formationSelected));
    }

    // ========== UI构建辅助 ==========

    private void ClearCards()
    {
        foreach (var c in _playerCards) Destroy(c.Root);
        foreach (var c in _enemyCards) Destroy(c.Root);
        _playerCards.Clear();
        _enemyCards.Clear();

        foreach (var go in _actionButtons) Destroy(go);
        _actionButtons.Clear();
    }

    private UnitCardUI CreateUnitCard(Transform container, UnitRuntime unit, bool isEnemy)
    {
        Color defaultBg = isEnemy
            ? new Color(0.3f, 0.1f, 0.1f, 0.8f)
            : new Color(0.1f, 0.1f, 0.3f, 0.8f);

        var card = new UnitCardUI { Unit = unit, DefaultBgColor = defaultBg };

        var root = new GameObject($"Card_{unit.Name}");
        root.transform.SetParent(container, false);
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(340, 60);
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 60;
        layout.preferredWidth = 340;
        card.Root = root;

        card.Background = root.AddComponent<Image>();
        card.Background.color = defaultBg;

        // 名字
        card.NameText = CreateTMP(root.transform, "Name", unit.Name,
            new Vector2(8, -3), new Vector2(150, 18), 14, TextAlignmentOptions.Left);

        // HP Slider
        card.HPSlider = CreateMiniSlider(root.transform, "HP",
            new Vector2(8, -22), new Vector2(240, 14),
            new Color(0.2f, 0.8f, 0.2f), new Color(0.25f, 0.25f, 0.25f));

        card.HPText = CreateTMP(root.transform, "HPText", "",
            new Vector2(255, -22), new Vector2(80, 14), 11, TextAlignmentOptions.Left);

        // ATB Bar
        var atbBg = new GameObject("ATBBG");
        atbBg.transform.SetParent(root.transform, false);
        var atbBgRT = atbBg.AddComponent<RectTransform>();
        atbBgRT.anchorMin = new Vector2(0, 1);
        atbBgRT.anchorMax = new Vector2(0, 1);
        atbBgRT.pivot = new Vector2(0, 1);
        atbBgRT.anchoredPosition = new Vector2(8, -40);
        atbBgRT.sizeDelta = new Vector2(240, 10);
        atbBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var atbFillGO = new GameObject("ATBFill");
        atbFillGO.transform.SetParent(atbBg.transform, false);
        var atbFillRT = atbFillGO.AddComponent<RectTransform>();
        atbFillRT.anchorMin = Vector2.zero;
        atbFillRT.anchorMax = Vector2.one;
        atbFillRT.offsetMin = Vector2.zero;
        atbFillRT.offsetMax = Vector2.zero;
        card.ATBFill = atbFillGO.AddComponent<Image>();
        card.ATBFill.color = new Color(1f, 0.8f, 0.2f);
        card.ATBFill.type = Image.Type.Filled;
        card.ATBFill.fillMethod = Image.FillMethod.Horizontal;
        card.ATBFill.fillAmount = 0f;

        CreateTMP(root.transform, "ATBLabel", "ATB",
            new Vector2(255, -40), new Vector2(40, 10), 9, TextAlignmentOptions.Left);

        if (isEnemy)
        {
            card.CardButton = root.AddComponent<Button>();
            card.CardButton.targetGraphic = card.Background;
            card.CardButton.interactable = false;
            var unitRef = unit;
            card.CardButton.onClick.AddListener(() => _battleManager.PlayerSelectTarget(unitRef));
        }

        return card;
    }

    private GameObject CreateActionButton(string label, Color color, Action onClick)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(_actionButtonsContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(130, 45);
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = 130;
        layout.preferredHeight = 45;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor = color;
        c.highlightedColor = color * 1.2f;
        c.pressedColor = color * 0.8f;
        c.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        btn.colors = c;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    private FormationSlotUI CreateFormationSlot(UnitConfig config)
    {
        var slot = new FormationSlotUI { Config = config };

        var root = new GameObject($"Slot_{config.unitName}");
        root.transform.SetParent(_formationUnitsContainer, false);
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 50);
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 50;
        layout.preferredWidth = 400;
        slot.Root = root;

        slot.Background = root.AddComponent<Image>();
        slot.Background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        slot.Toggle = root.AddComponent<Toggle>();
        slot.Toggle.targetGraphic = slot.Background;
        slot.Toggle.isOn = _formationSelected.Contains(config);
        var configRef = config;
        slot.Toggle.onValueChanged.AddListener(isOn => OnFormationToggle(configRef, isOn));

        string info = $"{config.unitName}  HP:{config.maxHP}  ATK:{config.attack}  DEF:{config.defense}  SPD:{config.speed}";
        slot.Label = CreateTMP(root.transform, "Label", info,
            new Vector2(10, -5), new Vector2(380, 40), 16, TextAlignmentOptions.Left);

        return slot;
    }

    // ========== 低级UI工具方法 ==========

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

    private static Slider CreateMiniSlider(Transform parent, string name,
        Vector2 position, Vector2 size, Color fillColor, Color bgColor)
    {
        var root = new GameObject($"{name}Slider");
        root.transform.SetParent(parent, false);
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0, 1);
        rootRT.anchorMax = new Vector2(0, 1);
        rootRT.pivot = new Vector2(0, 1);
        rootRT.anchoredPosition = position;
        rootRT.sizeDelta = size;

        var bg = new GameObject("BG");
        bg.transform.SetParent(root.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = bgColor;

        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(root.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

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
        slider.fillRect = fillRT;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        return slider;
    }

    // ========== 内部数据类 ==========

    private class UnitCardUI
    {
        public UnitRuntime Unit;
        public GameObject Root;
        public Image Background;
        public Color DefaultBgColor;
        public TextMeshProUGUI NameText;
        public Slider HPSlider;
        public TextMeshProUGUI HPText;
        public Image ATBFill;
        public Button CardButton;
    }

    private class FormationSlotUI
    {
        public UnitConfig Config;
        public GameObject Root;
        public Image Background;
        public Toggle Toggle;
        public TextMeshProUGUI Label;
    }
}
