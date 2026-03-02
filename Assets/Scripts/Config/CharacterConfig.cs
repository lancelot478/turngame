using UnityEngine;

public abstract class CharacterConfig : ScriptableObject
{
    [Header("基础属性")]
    public string characterName = "角色";
    public int maxHP = 100;
    public int attack = 10;
    public int defense = 5;
}
