using System.Collections.Generic;
using UnityEngine;

public enum UnitTeam
{
    Player,
    Enemy
}

public enum EnemyAction
{
    Attack,
    Defend
}

public class UnitRuntime
{
    public UnitConfig Config { get; }
    public UnitTeam Team { get; }

    public string Name => Config.unitName;
    public int MaxHP => Config.maxHP;
    public int MaxMP => Config.maxMP;
    public int Attack => Config.attack;
    public int Defense => Config.defense;
    public int Speed => Config.speed;

    public int CurrentHP { get; set; }
    public int CurrentMP { get; set; }
    public float ATBGauge { get; set; }
    public bool IsDefending { get; set; }

    public bool IsAlive => CurrentHP > 0;
    public bool IsATBReady => ATBGauge >= 100f;
    public float HPPercent => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
    public float MPPercent => MaxMP > 0 ? (float)CurrentMP / MaxMP : 0f;
    public float ATBPercent => Mathf.Clamp01(ATBGauge / 100f);

    public List<SkillData> Skills { get; }

    public Transform Model { get; set; }

    public UnitRuntime(UnitConfig config, UnitTeam team)
    {
        Config = config;
        Team = team;
        CurrentHP = config.maxHP;
        CurrentMP = config.maxMP;
        ATBGauge = 0f;
        Skills = CloneSkills(config.skills);
    }

    public void TickATB(float delta, float scale)
    {
        if (!IsAlive) return;
        ATBGauge += Speed * delta * scale;
    }

    public void ConsumeATB()
    {
        ATBGauge -= 100f;
    }

    /// <summary>
    /// 防御状态下防御力翻倍
    /// </summary>
    public int TakeDamage(int rawDamage)
    {
        int effectiveDefense = IsDefending ? Defense * 2 : Defense;
        int damage = Mathf.Max(1, rawDamage - effectiveDefense);
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        IsDefending = false;
        return damage;
    }

    public int Heal(int amount)
    {
        int actualHeal = Mathf.Min(amount, MaxHP - CurrentHP);
        CurrentHP += actualHeal;
        return actualHeal;
    }

    public bool CanUseSkill(SkillData skill)
    {
        return CurrentMP >= skill.MPCost;
    }

    public void ConsumeMP(int amount)
    {
        CurrentMP = Mathf.Max(0, CurrentMP - amount);
    }

    public EnemyAction DecideAction()
    {
        return Random.Range(0f, 1f) < Config.attackProbability
            ? EnemyAction.Attack
            : EnemyAction.Defend;
    }

    private static List<SkillData> CloneSkills(List<SkillData> source)
    {
        var result = new List<SkillData>();
        if (source == null) return result;
        foreach (var s in source)
        {
            if (s == null) continue;
            result.Add(new SkillData(s.Name, s.Type, s.Multiplier, s.MPCost, s.Description));
        }
        return result;
    }
}
