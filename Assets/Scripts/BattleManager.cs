using System;
using System.Collections;
using UnityEngine;

public enum BattleState
{
    Start,
    PlayerTurn,
    EnemyTurn,
    Won,
    Lost
}

public class BattleManager : MonoBehaviour
{
    private const string PlayerConfigPath = "Configs/PlayerConfig";
    private const string EnemyConfigPath = "Configs/EnemyConfig";

    public PlayerCharacter Player { get; private set; }
    public EnemyCharacter Enemy { get; private set; }
    public BattleState State { get; private set; }

    private PlayerConfig _playerConfig;
    private EnemyConfig _enemyConfig;

    public event Action<string> OnBattleLog;
    public event Action OnStateChanged;
    public event Action<CharacterBase> OnCharacterHit;
    public event Action<BattleState> OnBattleEnd;

    public void Init()
    {
        _playerConfig = Resources.Load<PlayerConfig>(PlayerConfigPath);
        _enemyConfig = Resources.Load<EnemyConfig>(EnemyConfigPath);

        if (_playerConfig == null || _enemyConfig == null)
        {
            Debug.LogError($"[BattleManager] 配置缺失，请确保存在 Resources/{PlayerConfigPath}.asset 和 Resources/{EnemyConfigPath}.asset");
            return;
        }

        Player = new PlayerCharacter(_playerConfig);
        Enemy = new EnemyCharacter(_enemyConfig);
    }

    public void StartBattle()
    {
        if (Player == null || Enemy == null)
        {
            Debug.LogError("[BattleManager] 未初始化成功，无法开始战斗。");
            return;
        }

        State = BattleState.Start;
        Log($"战斗开始！{Player.Name} VS {Enemy.Name}");
        StartCoroutine(BeginPlayerTurn());
    }

    private IEnumerator BeginPlayerTurn()
    {
        yield return new WaitForSeconds(0.5f);
        State = BattleState.PlayerTurn;
        Player.IsDefending = false;
        Log($"--- {Player.Name} 的回合 ---");
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 普通攻击
    /// </summary>
    public void PlayerNormalAttack()
    {
        if (State != BattleState.PlayerTurn) return;
        State = BattleState.Start; // 锁定输入
        OnStateChanged?.Invoke();

        int damage = Enemy.TakeDamage(Player.Attack);
        Log($"{Player.Name} 发动普通攻击，对 {Enemy.Name} 造成 {damage} 点伤害！");
        OnCharacterHit?.Invoke(Enemy);

        StartCoroutine(AfterPlayerAction());
    }

    /// <summary>
    /// 使用技能
    /// </summary>
    public void PlayerUseSkill(int skillIndex)
    {
        if (State != BattleState.PlayerTurn) return;
        if (skillIndex < 0 || skillIndex >= Player.Skills.Count) return;

        SkillData skill = Player.Skills[skillIndex];
        if (!Player.CanUseSkill(skill))
        {
            Log("MP不足，无法使用该技能！");
            return;
        }

        State = BattleState.Start;
        OnStateChanged?.Invoke();
        Player.ConsumeMP(skill.MPCost);

        switch (skill.Type)
        {
            case SkillType.Attack:
                int rawDamage = Mathf.RoundToInt(Player.Attack * skill.Multiplier);
                int damage = Enemy.TakeDamage(rawDamage);
                Log($"{Player.Name} 使用【{skill.Name}】，对 {Enemy.Name} 造成 {damage} 点伤害！");
                OnCharacterHit?.Invoke(Enemy);
                break;

            case SkillType.Heal:
                int healAmount = Mathf.RoundToInt(Player.Attack * skill.Multiplier);
                int actualHeal = Player.Heal(healAmount);
                Log($"{Player.Name} 使用【{skill.Name}】，恢复了 {actualHeal} 点生命值！");
                break;
        }

        StartCoroutine(AfterPlayerAction());
    }

    private IEnumerator AfterPlayerAction()
    {
        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(0.8f);

        if (!Enemy.IsAlive)
        {
            State = BattleState.Won;
            Log($"{Enemy.Name} 被击败了！");
            Log("=== 战斗胜利！===");
            OnBattleEnd?.Invoke(BattleState.Won);
            OnStateChanged?.Invoke();
            yield break;
        }

        StartCoroutine(DoEnemyTurn());
    }

    private IEnumerator DoEnemyTurn()
    {
        State = BattleState.EnemyTurn;
        Log($"--- {Enemy.Name} 的回合 ---");
        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(0.8f);

        EnemyAction action = Enemy.DecideAction();

        switch (action)
        {
            case EnemyAction.Attack:
                int damage = Player.TakeDamage(Enemy.Attack);
                Log($"{Enemy.Name} 发动攻击，对 {Player.Name} 造成 {damage} 点伤害！");
                OnCharacterHit?.Invoke(Player);
                break;

            case EnemyAction.Defend:
                Enemy.IsDefending = true;
                Log($"{Enemy.Name} 摆出了防御姿态！");
                break;
        }

        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(0.8f);

        if (!Player.IsAlive)
        {
            State = BattleState.Lost;
            Log($"{Player.Name} 倒下了...");
            Log("=== 战斗失败... ===");
            OnBattleEnd?.Invoke(BattleState.Lost);
            OnStateChanged?.Invoke();
            yield break;
        }

        StartCoroutine(BeginPlayerTurn());
    }

    public void RestartBattle()
    {
        StopAllCoroutines();
        Player = new PlayerCharacter(_playerConfig);
        Enemy = new EnemyCharacter(_enemyConfig);
        OnStateChanged?.Invoke();
        StartBattle();
    }

    private void Log(string message)
    {
        OnBattleLog?.Invoke(message);
    }
}
