using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TurnGame/Config/Team Formation", fileName = "TeamFormation")]
public class TeamFormationConfig : ScriptableObject
{
    [Header("编队设置")]
    public int maxActiveSlots = 3;

    [Header("可用单位池")]
    public List<UnitConfig> unitPool = new List<UnitConfig>();
}
