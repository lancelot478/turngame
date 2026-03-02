using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUI : MonoBehaviour
{
    [Header("玩家状态")]
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private Slider _playerHPSlider;
    [SerializeField] private TextMeshProUGUI _playerHPText;
    [SerializeField] private Image _playerMPFill;
    [SerializeField] private TextMeshProUGUI _playerMPText;

    [Header("敌人状态")]
    [SerializeField] private TextMeshProUGUI _enemyNameText;
    [SerializeField] private Slider _enemyHPSlider;
    [SerializeField] private TextMeshProUGUI _enemyHPText;

    [Header("操作按钮")]
    [SerializeField] private GameObject _actionPanel;
    [SerializeField] private Button _attackButton;
    [SerializeField] private Button _skill1Button;
    [SerializeField] private Button _skill2Button;

    [Header("战斗日志")]
    [SerializeField] private TextMeshProUGUI _battleLogText;
    [SerializeField] private ScrollRect _logScrollRect;

    [Header("结算面板")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Button _restartButton;

    private BattleManager _battleManager;
    private Transform _playerModel;
    private Transform _enemyModel;
    private Vector3 _playerOriginalPos;
    private Vector3 _enemyOriginalPos;

    private readonly List<string> _logMessages = new List<string>();
    private const int MaxLogLines = 50;

    public void Init(BattleManager battleManager, Transform playerModel, Transform enemyModel)
    {
        _battleManager = battleManager;
        _playerModel = playerModel;
        _enemyModel = enemyModel;
        _playerOriginalPos = playerModel.position;
        _enemyOriginalPos = enemyModel.position;

        _resultPanel.SetActive(false);
        BindEvents();
    }

    private void OnDestroy()
    {
        if (_battleManager == null) return;
        _battleManager.OnBattleLog -= AddLog;
        _battleManager.OnStateChanged -= RefreshUI;
        _battleManager.OnCharacterHit -= PlayHitEffect;
        _battleManager.OnBattleEnd -= ShowResult;
    }

    private void BindEvents()
    {
        _attackButton.onClick.AddListener(() => _battleManager.PlayerNormalAttack());
        _skill1Button.onClick.AddListener(() => _battleManager.PlayerUseSkill(0));
        _skill2Button.onClick.AddListener(() => _battleManager.PlayerUseSkill(1));
        _restartButton.onClick.AddListener(OnRestartClicked);

        _battleManager.OnBattleLog += AddLog;
        _battleManager.OnStateChanged += RefreshUI;
        _battleManager.OnCharacterHit += PlayHitEffect;
        _battleManager.OnBattleEnd += ShowResult;
    }

    private void RefreshUI()
    {
        var player = _battleManager.Player;
        var enemy = _battleManager.Enemy;

        _playerNameText.text = player.Name;
        SetSliderValue(_playerHPSlider, player.HPPercent);
        _playerHPText.text = $"{player.CurrentHP}/{player.MaxHP} ({Mathf.RoundToInt(player.HPPercent * 100f)}%)";
        SetImageFill(_playerMPFill, player.MPPercent);
        _playerMPText.text = $"{player.CurrentMP}/{player.MaxMP} ({Mathf.RoundToInt(player.MPPercent * 100f)}%)";

        _enemyNameText.text = enemy.IsDefending ? $"{enemy.Name} [防御中]" : enemy.Name;
        SetSliderValue(_enemyHPSlider, enemy.HPPercent);
        _enemyHPText.text = $"{enemy.CurrentHP}/{enemy.MaxHP} ({Mathf.RoundToInt(enemy.HPPercent * 100f)}%)";

        SetButtonLabel(_skill1Button, player.Skills.Count > 0 ? player.Skills[0].Name : "技能1");
        SetButtonLabel(_skill2Button, player.Skills.Count > 1 ? player.Skills[1].Name : "技能2");

        bool isPlayerTurn = _battleManager.State == BattleState.PlayerTurn;
        _attackButton.interactable = isPlayerTurn;
        _skill1Button.interactable = isPlayerTurn && player.Skills.Count > 0 && player.CanUseSkill(player.Skills[0]);
        _skill2Button.interactable = isPlayerTurn && player.Skills.Count > 1 && player.CanUseSkill(player.Skills[1]);
    }

    private void AddLog(string message)
    {
        _logMessages.Add(message);
        if (_logMessages.Count > MaxLogLines)
            _logMessages.RemoveAt(0);

        _battleLogText.text = string.Join("\n", _logMessages);
        Canvas.ForceUpdateCanvases();
        _logScrollRect.verticalNormalizedPosition = 0f;
    }

    private void PlayHitEffect(CharacterBase target)
    {
        Transform model = (target is EnemyCharacter) ? _enemyModel : _playerModel;
        if (model != null)
            StartCoroutine(HitAnimation(model));
    }

    private IEnumerator HitAnimation(Transform model)
    {
        var renderer = model.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Color originalColor = renderer.material.color;
        renderer.material.color = Color.red;

        Vector3 originalPos = model.position;
        float elapsed = 0f;
        const float duration = 0.3f;

        while (elapsed < duration)
        {
            float offset = Mathf.Sin(elapsed * 40f) * 0.15f;
            model.position = originalPos + new Vector3(offset, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        model.position = originalPos;
        renderer.material.color = originalColor;
    }

    private void ShowResult(BattleState result)
    {
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

    private void OnRestartClicked()
    {
        _resultPanel.SetActive(false);
        _logMessages.Clear();
        _battleLogText.text = "";

        if (_playerModel != null) _playerModel.position = _playerOriginalPos;
        if (_enemyModel != null) _enemyModel.position = _enemyOriginalPos;

        _battleManager.RestartBattle();
    }

    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
    }

    private static void SetImageFill(Image fillImage, float percent)
    {
        if (fillImage == null) return;

        // 兜底处理：确保 fillAmount 生效
        if (fillImage.type != Image.Type.Filled)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
        }

        fillImage.fillAmount = Mathf.Clamp01(percent);
    }

    private static void SetSliderValue(Slider slider, float percent)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(percent);
    }
}
