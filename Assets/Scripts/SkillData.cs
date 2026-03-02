using UnityEngine;

public enum SkillType
{
    Attack,
    Heal
}

[System.Serializable]
public class SkillData
{
    public string Name;
    public SkillType Type;
    public float Multiplier;
    public int MPCost;
    public string Description;

    public SkillData(string name, SkillType type, float multiplier, int mpCost, string description)
    {
        Name = name;
        Type = type;
        Multiplier = multiplier;
        MPCost = mpCost;
        Description = description;
    }
}
