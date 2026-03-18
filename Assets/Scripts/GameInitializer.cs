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

    // 场景包围盒半宽/半深（根据单位分布留出余量）
    private const float SceneHalfWidth = 5.5f;
    private const float SceneHalfDepth = 4.5f;
    private const float CameraPitchDeg = 40f;
    private const float CameraFOV = 20f;

    private Camera _mainCam;

    private void SetupCamera()
    {
        _mainCam = Camera.main;
        if (_mainCam == null)
        {
            var camGO = new GameObject("MainCamera");
            _mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        _mainCam.fieldOfView = CameraFOV;
        _mainCam.backgroundColor = new Color(0.15f, 0.15f, 0.25f);
        _mainCam.clearFlags = CameraClearFlags.SolidColor;
        _mainCam.nearClipPlane = 0.3f;
        _mainCam.farClipPlane = 100f;

        FitCameraToScene();
    }

    /// <summary>
    /// 斜 40° 俯视 + 根据屏幕宽高比自动拉远距离，保证所有单位可见
    /// </summary>
    private void FitCameraToScene()
    {
        float pitchRad = CameraPitchDeg * Mathf.Deg2Rad;
        float vFovRad = CameraFOV * Mathf.Deg2Rad;
        float aspect = (float)Screen.width / Screen.height;
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * aspect);

        // 水平方向：需要把 x ∈ [-SceneHalfWidth, +SceneHalfWidth] 装进水平 FOV
        float distForWidth = SceneHalfWidth / Mathf.Tan(hFovRad * 0.5f);

        // 垂直方向：地面 z 轴深度经倾斜角投影到视平面后的表观高度
        float apparentHalfHeight = SceneHalfDepth / Mathf.Cos(pitchRad);
        float distForHeight = apparentHalfHeight / Mathf.Tan(vFovRad * 0.5f);

        // 取两者最大值，再加 15% 安全边距
        float dist = Mathf.Max(distForWidth, distForHeight) * 1.15f;

        _mainCam.transform.position = new Vector3(
            0f,
            dist * Mathf.Sin(pitchRad),
            -dist * Mathf.Cos(pitchRad)
        );
        _mainCam.transform.rotation = Quaternion.Euler(CameraPitchDeg, 0f, 0f);
    }

    private void LateUpdate()
    {
        // 运行时屏幕旋转或分辨率变化时重新适配
        if (_mainCam != null)
            FitCameraToScene();
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

       SetMaterialColor(go.GetComponent<Renderer>(), color);
        return go.transform;
    }

    private void CreateGround()
    {
          // 用扁平 Cube 替代 Plane，避免 MeshCollider 在 iOS IL2CPP 下被 Strip
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.25f, 0);
        ground.transform.localScale = new Vector3(20, 0.5f, 20);

        SetMaterialColor(ground.GetComponent<Renderer>(), new Color(0.3f, 0.35f, 0.3f));
    }
    /// <summary>
    /// 通过 renderer.material 自动实例化材质再改色，
    /// 避免 Shader.Find 在 iOS 打包后返回 null
    /// </summary>
    private static void SetMaterialColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        renderer.material.color = color;
    }
}
