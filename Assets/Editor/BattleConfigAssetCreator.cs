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
        EnsureFolder("Assets/Resources/Configs/Units");

        // ===== 玩家单位 =====
        var warrior = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_Warrior.asset",
            "warrior", "战士", 120, 40, 25, 10, 8,
            new Color(0.2f, 0.4f, 0.9f),
            new List<SkillData>
            {
                new SkillData("重击", SkillType.Attack, 1.8f, 10, "消耗10MP，造成1.8倍攻击伤害"),
                new SkillData("治疗", SkillType.Heal, 1.5f, 15, "消耗15MP，恢复自身生命值"),
            });

        var mage = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_Mage.asset",
            "mage", "法师", 80, 60, 30, 3, 12,
            new Color(0.6f, 0.2f, 0.9f),
            new List<SkillData>
            {
                new SkillData("火球术", SkillType.Attack, 2.2f, 15, "消耗15MP，造成2.2倍攻击伤害"),
                new SkillData("冥想", SkillType.Heal, 1.0f, 5, "消耗5MP，恢复少量生命值"),
            });

        var archer = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_Archer.asset",
            "archer", "弓手", 90, 35, 22, 5, 15,
            new Color(0.2f, 0.8f, 0.3f),
            new List<SkillData>
            {
                new SkillData("连射", SkillType.Attack, 1.5f, 8, "消耗8MP，造成1.5倍攻击伤害"),
            });

        var healer = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_Healer.asset",
            "healer", "牧师", 85, 80, 12, 6, 9,
            new Color(1f, 0.9f, 0.5f),
            new List<SkillData>
            {
                new SkillData("治愈之光", SkillType.Heal, 2.5f, 20, "消耗20MP，恢复大量生命值"),
            });

        // ===== 敌方单位 =====
        var goblin = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_Goblin.asset",
            "goblin", "哥布林", 60, 0, 12, 4, 11, new Color(0.4f, 0.7f, 0.2f), null, 0.8f);

        var orc = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_Orc.asset",
            "orc", "兽人", 100, 0, 18, 8, 7, new Color(0.6f, 0.3f, 0.1f), null, 0.6f);

        var darkMage = CreateUnitIfMissing("Assets/Resources/Configs/Units/Unit_DarkMage.asset",
            "dark_mage", "暗黑法师", 70, 0, 25, 3, 10, new Color(0.3f, 0.1f, 0.4f), null, 0.75f);

        // ===== 玩家编队 =====
        var playerFormation = CreateFormationIfMissing(
            "Assets/Resources/Configs/PlayerFormation.asset",
            3, new[] { warrior, mage, archer, healer });

        // ===== 敌方编队 =====
        var enemyFormation = CreateFormationIfMissing(
            "Assets/Resources/Configs/EnemyFormation.asset",
            3, new[] { goblin, orc, darkMage });

        // ===== 战斗设定 =====
        CreateBattleSetupIfMissing(
            "Assets/Resources/Configs/BattleSetup.asset",
            playerFormation, enemyFormation);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattleConfigAssetCreator] 所有配置资产创建完成！路径：Assets/Resources/Configs/");
    }

    private static UnitConfig CreateUnitIfMissing(string path, string id, string unitName,
        int hp, int mp, int atk, int def, int spd, Color color,
        List<SkillData> skills = null, float atkProb = 0.7f)
    {
        var existing = AssetDatabase.LoadAssetAtPath<UnitConfig>(path);
        if (existing != null) return existing;

        var config = ScriptableObject.CreateInstance<UnitConfig>();
        config.unitId = id;
        config.unitName = unitName;
        config.maxHP = hp;
        config.maxMP = mp;
        config.attack = atk;
        config.defense = def;
        config.speed = spd;
        config.modelColor = color;
        config.attackProbability = atkProb;
        config.skills = skills ?? new List<SkillData>();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private static TeamFormationConfig CreateFormationIfMissing(string path, int maxSlots, UnitConfig[] units)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TeamFormationConfig>(path);
        if (existing != null) return existing;

        var config = ScriptableObject.CreateInstance<TeamFormationConfig>();
        config.maxActiveSlots = maxSlots;
        config.unitPool = new List<UnitConfig>(units);
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private static void CreateBattleSetupIfMissing(string path,
        TeamFormationConfig playerFormation, TeamFormationConfig enemyFormation)
    {
        if (AssetDatabase.LoadAssetAtPath<BattleSetupConfig>(path) != null) return;

        var config = ScriptableObject.CreateInstance<BattleSetupConfig>();
        config.playerFormation = playerFormation;
        config.enemyFormation = enemyFormation;
        config.atbTickScale = 10f;
        AssetDatabase.CreateAsset(config, path);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var idx = folder.LastIndexOf('/');
        var parent = folder.Substring(0, idx);
        var current = folder.Substring(idx + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, current);
    }
}
