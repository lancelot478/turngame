using UnityEngine;

public enum EnemyAction
{
    Attack,
    Defend
}

public class EnemyCharacter : CharacterBase
{
    private readonly float _attackProbability;

    public EnemyCharacter(EnemyConfig config)
    {
        InitFromConfig(config);
        _attackProbability = config.attackProbability;
    }

    /// <summary>
    /// 70%概率攻击，30%概率防御
    /// </summary>
    public EnemyAction DecideAction()
    {
        return Random.Range(0f, 1f) < _attackProbability ? EnemyAction.Attack : EnemyAction.Defend;
    }
}
