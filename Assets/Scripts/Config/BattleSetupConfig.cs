using UnityEngine;

[CreateAssetMenu(menuName = "TurnGame/Config/Battle Setup", fileName = "BattleSetup")]
public class BattleSetupConfig : ScriptableObject
{
    [Header("玩家编队")]
    public TeamFormationConfig playerFormation;

    [Header("敌方编队")]
    public TeamFormationConfig enemyFormation;

    [Header("ATB设置")]
    [Tooltip("ATB累积速率倍率，越大行动越频繁")]
    public float atbTickScale = 10f;
}
