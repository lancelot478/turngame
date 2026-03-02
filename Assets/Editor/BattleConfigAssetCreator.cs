using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BattleConfigAssetCreator
{
    [MenuItem("Tools/创建战斗配置资产")]
    public static void CreateBattleConfigAssets()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Configs");

        const string playerPath = "Assets/Resources/Configs/PlayerConfig.asset";
        const string enemyPath = "Assets/Resources/Configs/EnemyConfig.asset";

        var playerConfig = AssetDatabase.LoadAssetAtPath<PlayerConfig>(playerPath);
        if (playerConfig == null)
        {
            playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            playerConfig.characterName = "勇者";
            playerConfig.maxHP = 100;
            playerConfig.attack = 20;
            playerConfig.defense = 8;
            playerConfig.maxMP = 50;
            playerConfig.skills = new List<SkillData>
            {
                new SkillData("重击", SkillType.Attack, 1.8f, 10, "消耗10MP，造成1.8倍攻击伤害"),
                new SkillData("治疗", SkillType.Heal, 1.5f, 15, "消耗15MP，恢复自身生命值")
            };
            AssetDatabase.CreateAsset(playerConfig, playerPath);
        }

        var enemyConfig = AssetDatabase.LoadAssetAtPath<EnemyConfig>(enemyPath);
        if (enemyConfig == null)
        {
            enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            enemyConfig.characterName = "哥布林";
            enemyConfig.maxHP = 80;
            enemyConfig.attack = 15;
            enemyConfig.defense = 5;
            enemyConfig.attackProbability = 0.7f;
            AssetDatabase.CreateAsset(enemyConfig, enemyPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattleConfigAssetCreator] 配置资产创建完成：Assets/Resources/Configs");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        var slashIndex = folder.LastIndexOf('/');
        var parent = folder.Substring(0, slashIndex);
        var current = folder.Substring(slashIndex + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, current);
    }
}
