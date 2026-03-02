using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameInitializer : MonoBehaviour
{
    private const string BattleSetupPath = "Configs/BattleSetup";
    private const string BattleUIPrefabPath = "Prefabs/BattleUI";

    private BattleSetupConfig _setupConfig;
    private BattleManager _battleManager;
    private BattleUI _battleUI;
    private List<UnitConfig> _currentDeployed = new List<UnitConfig>();
    private readonly List<GameObject> _modelInstances = new List<GameObject>();

    private void Start()
    {
        SetupCamera();
        SetupLighting();
        EnsureEventSystem();
        CreateGround();

        _setupConfig = Resources.Load<BattleSetupConfig>(BattleSetupPath);
        if (_setupConfig == null)
        {
            Debug.LogError($"[GameInitializer] 找不到战斗配置，请先执行 Tools → 创建战斗配置资产。路径：Resources/{BattleSetupPath}");
            return;
        }

        _battleManager = gameObject.AddComponent<BattleManager>();

        _battleUI = LoadBattleUI();
        if (_battleUI == null) return;

        _battleUI.Setup(HandleRestart, HandleEditFormation);

        // 默认选择前N个单位
        var pool = _setupConfig.playerFormation.unitPool;
        int max = _setupConfig.playerFormation.maxActiveSlots;
        _currentDeployed.Clear();
        for (int i = 0; i < Mathf.Min(max, pool.Count); i++)
            _currentDeployed.Add(pool[i]);

        // 显示编队面板
        _battleUI.ShowFormation(_setupConfig.playerFormation, _currentDeployed, OnFormationConfirmed);
    }

    private void OnFormationConfirmed(List<UnitConfig> deployed)
    {
        _currentDeployed = deployed;
        StartNewBattle();
    }

    private void StartNewBattle()
    {
        CleanupModels();
        _battleManager.StopAllCoroutines();

        _battleManager.Init(_setupConfig, _currentDeployed);

        CreateModelsForUnits(_battleManager.PlayerUnits, true);
        CreateModelsForUnits(_battleManager.EnemyUnits, false);

        _battleUI.InitBattle(_battleManager);
        _battleManager.StartBattle();
    }

    private void HandleRestart()
    {
        StartNewBattle();
    }

    private void HandleEditFormation()
    {
        _battleUI.ShowFormation(_setupConfig.playerFormation, _currentDeployed, deployed =>
        {
            _currentDeployed = deployed;
            StartNewBattle();
        });
    }

    // ========== 3D模型 ==========

    private void CreateModelsForUnits(List<UnitRuntime> units, bool isPlayerSide)
    {
        float xBase = isPlayerSide ? -3f : 3f;
        int count = units.Count;

        for (int i = 0; i < count; i++)
        {
            float zPos = (i - (count - 1) / 2f) * 1.8f;
            var model = CreateCharacterModel(
                units[i].Name,
                new Vector3(xBase, 0.75f, zPos),
                units[i].Config.modelColor
            );
            units[i].Model = model;
            _modelInstances.Add(model.gameObject);
        }
    }

    private void CleanupModels()
    {
        foreach (var go in _modelInstances)
            if (go != null) Destroy(go);
        _modelInstances.Clear();
    }

    // ========== 场景搭建 ==========

    private BattleUI LoadBattleUI()
    {
        var prefab = Resources.Load<GameObject>(BattleUIPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GameInitializer] 找不到战斗UI预制体，请先通过菜单 Tools → 创建战斗UI预制体 生成。");
            return null;
        }
        var instance = Instantiate(prefab);
        instance.name = "BattleUI";
        return instance.GetComponent<BattleUI>();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("MainCamera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0, 5, -10);
        cam.transform.rotation = Quaternion.Euler(25, 0, 0);
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.25f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private void SetupLighting()
    {
        var existingLight = FindAnyObjectByType<Light>();
        if (existingLight != null)
        {
            existingLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            existingLight.intensity = 1.2f;
            return;
        }
        var lightGO = new GameObject("DirectionalLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.95f, 0.85f);
    }

    private Transform CreateCharacterModel(string unitName, Vector3 position, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Model_{unitName}";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

        var renderer = go.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
        return go.transform;
    }

    private void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2, 1, 2);

        var renderer = ground.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.3f, 0.35f, 0.3f);
    }
}
