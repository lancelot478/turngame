using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : CharacterBase
{
    public int MaxMP { get; private set; }
    public int CurrentMP { get; set; }
    public List<SkillData> Skills { get; private set; }

    public float MPPercent => (float)CurrentMP / MaxMP;

    public PlayerCharacter(PlayerConfig config)
    {
        InitFromConfig(config);
        MaxMP = config.maxMP;
        CurrentMP = config.maxMP;
        Skills = CloneSkills(config.skills);
    }

    public bool CanUseSkill(SkillData skill)
    {
        return CurrentMP >= skill.MPCost;
    }

    public void ConsumeMP(int amount)
    {
        CurrentMP = Mathf.Max(0, CurrentMP - amount);
    }

    private static List<SkillData> CloneSkills(List<SkillData> source)
    {
        var result = new List<SkillData>();
        if (source == null) return result;

        foreach (var skill in source)
        {
            if (skill == null) continue;
            result.Add(new SkillData(skill.Name, skill.Type, skill.Multiplier, skill.MPCost, skill.Description));
        }

        return result;
    }
}
