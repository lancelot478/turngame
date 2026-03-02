using UnityEngine;

public abstract class CharacterBase
{
    public string Name { get; protected set; }
    public int MaxHP { get; protected set; }
    public int CurrentHP { get; set; }
    public int Attack { get; protected set; }
    public int Defense { get; protected set; }

    public bool IsAlive => CurrentHP > 0;
    public bool IsDefending { get; set; }

    protected void InitFromConfig(CharacterConfig config)
    {
        Name = config.characterName;
        MaxHP = config.maxHP;
        CurrentHP = config.maxHP;
        Attack = config.attack;
        Defense = config.defense;
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

    public float HPPercent => (float)CurrentHP / MaxHP;
}
