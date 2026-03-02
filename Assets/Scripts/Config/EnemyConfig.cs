using UnityEngine;

[CreateAssetMenu(menuName = "TurnGame/Config/Enemy Config", fileName = "EnemyConfig")]
public class EnemyConfig : CharacterConfig
{
    [Header("AI配置")]
    [Range(0f, 1f)]
    public float attackProbability = 0.7f;
}
