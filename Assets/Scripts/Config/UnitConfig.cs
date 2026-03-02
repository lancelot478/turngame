using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TurnGame/Config/Unit Config", fileName = "UnitConfig")]
public class UnitConfig : ScriptableObject
{
    [Header("标识")]
    public string unitId;
    public string unitName = "单位";

    [Header("基础属性")]
    public int maxHP = 100;
    public int maxMP = 50;
    public int attack = 10;
    public int defense = 5;
    public int speed = 10;

    [Header("AI配置 (敌方单位)")]
    [Range(0f, 1f)]
    public float attackProbability = 0.7f;

    [Header("外观")]
    public Color modelColor = Color.gray;

    [Header("技能")]
    public List<SkillData> skills = new List<SkillData>();
}
