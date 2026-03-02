using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TurnGame/Config/Player Config", fileName = "PlayerConfig")]
public class PlayerConfig : CharacterConfig
{
    [Header("玩家资源")]
    public int maxMP = 50;

    [Header("技能配置")]
    public List<SkillData> skills = new List<SkillData>();
}
