using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Idle,
    ATBRunning,
    WaitingForInput,
    SelectingTarget,
    Executing,
    Won,
    Lost
}

public class BattleManager : MonoBehaviour
{
    public BattleState State { get; private set; }

    public List<UnitRuntime> PlayerUnits { get; private set; } = new List<UnitRuntime>();
    public List<UnitRuntime> EnemyUnits { get; private set; } = new List<UnitRuntime>();
    public List<UnitRuntime> AllUnits { get; private set; } = new List<UnitRuntime>();

    public UnitRuntime CurrentActiveUnit { get; private set; }

    public event Action<string> OnBattleLog;
    public event Action OnStateChanged;
    public event Action<UnitRuntime> OnUnitHit;
    public event Action<BattleState> OnBattleEnd;

    private BattleSetupConfig _setupConfig;
    private List<UnitConfig> _playerDeployed;
    private int _pendingSkillIndex = -1;

    public float ATBTickScale => _setupConfig != null ? _setupConfig.atbTickScale : 10f;

    public void Init(BattleSetupConfig config, List<UnitConfig> playerDeployed)
    {
        _setupConfig = config;
        _playerDeployed = new List<UnitConfig>(playerDeployed);
        BuildUnits();
    }

    private void BuildUnits()
    {
        PlayerUnits.Clear();
        EnemyUnits.Clear();
        AllUnits.Clear();

        foreach (var uc in _playerDeployed)
        {
            if (uc == null) continue;
            PlayerUnits.Add(new UnitRuntime(uc, UnitTeam.Player));
        }

        if (_setupConfig.enemyFormation != null)
        {
            foreach (var uc in _setupConfig.enemyFormation.unitPool)
            {
                if (uc == null) continue;
                EnemyUnits.Add(new UnitRuntime(uc, UnitTeam.Enemy));
            }
        }

        AllUnits.AddRange(PlayerUnits);
        AllUnits.AddRange(EnemyUnits);
    }

    public void StartBattle()
    {
        if (AllUnits.Count == 0)
        {
            Debug.LogError("[BattleManager] 没有可用单位，无法开始战斗。");
            return;
        }

        foreach (var u in AllUnits)
        {
            u.ATBGauge = 0f;
            u.IsDefending = false;
        }

        State = BattleState.ATBRunning;
        CurrentActiveUnit = null;
        _pendingSkillIndex = -1;
        Log("=== 战斗开始！===");
        OnStateChanged?.Invoke();
    }

    private void Update()
    {
        if (State != BattleState.ATBRunning) return;

        float delta = Time.deltaTime;
        foreach (var u in AllUnits)
            u.TickATB(delta, ATBTickScale);

        OnStateChanged?.Invoke();

        var readyUnit = GetNextReadyUnit();
        if (readyUnit == null) return;

        readyUnit.ConsumeATB();
        CurrentActiveUnit = readyUnit;

        if (readyUnit.Team == UnitTeam.Player)
        {
            State = BattleState.WaitingForInput;
            readyUnit.IsDefending = false;
            Log($"--- {readyUnit.Name} 的回合 ---");
            OnStateChanged?.Invoke();
        }
        else
        {
            State = BattleState.Executing;
            OnStateChanged?.Invoke();
            StartCoroutine(ExecuteEnemyAction(readyUnit));
        }
    }

    private UnitRuntime GetNextReadyUnit()
    {
        UnitRuntime best = null;
        foreach (var u in AllUnits)
        {
            if (!u.IsAlive || !u.IsATBReady) continue;
            if (best == null ||
                u.ATBGauge > best.ATBGauge ||
                (Mathf.Approximately(u.ATBGauge, best.ATBGauge) && u.Speed > best.Speed))
            {
                best = u;
            }
        }
        return best;
    }

    // ========== Player Actions ==========

    public void PlayerChooseAttack()
    {
        if (State != BattleState.WaitingForInput) return;
        _pendingSkillIndex = -1;
        State = BattleState.SelectingTarget;
        OnStateChanged?.Invoke();
    }

    public void PlayerChooseSkill(int skillIndex)
    {
        if (State != BattleState.WaitingForInput || CurrentActiveUnit == null) return;

        var skills = CurrentActiveUnit.Skills;
        if (skillIndex < 0 || skillIndex >= skills.Count) return;

        var skill = skills[skillIndex];
        if (!CurrentActiveUnit.CanUseSkill(skill))
        {
            Log("MP不足！");
            return;
        }

        if (skill.Type == SkillType.Heal)
        {
            State = BattleState.Executing;
            OnStateChanged?.Invoke();
            CurrentActiveUnit.ConsumeMP(skill.MPCost);
            int healAmount = Mathf.RoundToInt(CurrentActiveUnit.Attack * skill.Multiplier);
            int actual = CurrentActiveUnit.Heal(healAmount);
            Log($"{CurrentActiveUnit.Name} 使用【{skill.Name}】，恢复了 {actual} 点生命值！");
            StartCoroutine(AfterAction());
            return;
        }

        _pendingSkillIndex = skillIndex;
        State = BattleState.SelectingTarget;
        OnStateChanged?.Invoke();
    }

    public void PlayerSelectTarget(UnitRuntime target)
    {
        if (State != BattleState.SelectingTarget || CurrentActiveUnit == null) return;
        if (target == null || !target.IsAlive || target.Team != UnitTeam.Enemy) return;

        State = BattleState.Executing;
        OnStateChanged?.Invoke();

        if (_pendingSkillIndex < 0)
        {
            int damage = target.TakeDamage(CurrentActiveUnit.Attack);
            Log($"{CurrentActiveUnit.Name} 攻击 {target.Name}，造成 {damage} 点伤害！");
            OnUnitHit?.Invoke(target);
        }
        else
        {
            var skill = CurrentActiveUnit.Skills[_pendingSkillIndex];
            CurrentActiveUnit.ConsumeMP(skill.MPCost);
            int rawDmg = Mathf.RoundToInt(CurrentActiveUnit.Attack * skill.Multiplier);
            int damage = target.TakeDamage(rawDmg);
            Log($"{CurrentActiveUnit.Name} 使用【{skill.Name}】攻击 {target.Name}，造成 {damage} 点伤害！");
            OnUnitHit?.Invoke(target);
        }

        StartCoroutine(AfterAction());
    }

    public void CancelTargetSelection()
    {
        if (State != BattleState.SelectingTarget) return;
        _pendingSkillIndex = -1;
        State = BattleState.WaitingForInput;
        OnStateChanged?.Invoke();
    }

    // ========== Enemy AI ==========

    private IEnumerator ExecuteEnemyAction(UnitRuntime enemy)
    {
        Log($"--- {enemy.Name} 的回合 ---");
        yield return new WaitForSeconds(0.4f);

        var action = enemy.DecideAction();

        if (action == EnemyAction.Attack)
        {
            var target = GetRandomAliveUnit(PlayerUnits);
            if (target != null)
            {
                int damage = target.TakeDamage(enemy.Attack);
                Log($"{enemy.Name} 攻击 {target.Name}，造成 {damage} 点伤害！");
                OnUnitHit?.Invoke(target);
            }
        }
        else
        {
            enemy.IsDefending = true;
            Log($"{enemy.Name} 摆出了防御姿态！");
        }

        yield return StartCoroutine(AfterAction());
    }

    // ========== Post-action ==========

    private IEnumerator AfterAction()
    {
        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(0.5f);

        bool allEnemyDead = true;
        foreach (var u in EnemyUnits)
            if (u.IsAlive) { allEnemyDead = false; break; }

        bool allPlayerDead = true;
        foreach (var u in PlayerUnits)
            if (u.IsAlive) { allPlayerDead = false; break; }

        if (allEnemyDead)
        {
            State = BattleState.Won;
            Log("=== 战斗胜利！===");
            OnBattleEnd?.Invoke(BattleState.Won);
            OnStateChanged?.Invoke();
            yield break;
        }

        if (allPlayerDead)
        {
            State = BattleState.Lost;
            Log("=== 战斗失败... ===");
            OnBattleEnd?.Invoke(BattleState.Lost);
            OnStateChanged?.Invoke();
            yield break;
        }

        CurrentActiveUnit = null;
        _pendingSkillIndex = -1;
        State = BattleState.ATBRunning;
        OnStateChanged?.Invoke();
    }

    private static UnitRuntime GetRandomAliveUnit(List<UnitRuntime> units)
    {
        var alive = new List<UnitRuntime>();
        foreach (var u in units)
            if (u.IsAlive) alive.Add(u);
        return alive.Count > 0 ? alive[UnityEngine.Random.Range(0, alive.Count)] : null;
    }

    public void RestartBattle(List<UnitConfig> newDeployed = null)
    {
        StopAllCoroutines();
        if (newDeployed != null)
            _playerDeployed = new List<UnitConfig>(newDeployed);
        BuildUnits();
    }

    private void Log(string msg)
    {
        OnBattleLog?.Invoke(msg);
    }
}
