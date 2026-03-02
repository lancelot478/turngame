using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂载到场景中任意 GameObject 上，运行后自动创建战斗场景并加载战斗UI预制体
/// </summary>
public class GameInitializer : MonoBehaviour
{
    private const string BattleUIPrefabPath = "Prefabs/BattleUI";

    private BattleManager _battleManager;
    private BattleUI _battleUI;

    private void Start()
    {
        SetupCamera();
        SetupLighting();
        EnsureEventSystem();
        Transform playerModel = CreateCharacterModel("PlayerModel", new Vector3(-3, 0.75f, 0), Color.blue);
        Transform enemyModel = CreateCharacterModel("EnemyModel", new Vector3(3, 0.75f, 0), new Color(0.7f, 0.1f, 0.1f));
        CreateGround();

        _battleManager = gameObject.AddComponent<BattleManager>();
        _battleUI = LoadBattleUI();
        if (_battleUI == null) return;

        _battleManager.Init();
        _battleUI.Init(_battleManager, playerModel, enemyModel);
        _battleManager.StartBattle();
    }

    private BattleUI LoadBattleUI()
    {
        var prefab = Resources.Load<GameObject>(BattleUIPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GameInitializer] 找不到战斗UI预制体，请先通过菜单 Tools → 创建战斗UI预制体 生成。路径：Resources/{BattleUIPrefabPath}");
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
        cam.transform.position = new Vector3(0, 4, -8);
        cam.transform.rotation = Quaternion.Euler(20, 0, 0);
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

    private Transform CreateCharacterModel(string name, Vector3 position, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = new Vector3(1, 1.2f, 1);

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
        ground.transform.localScale = new Vector3(2, 1, 1);

        var renderer = ground.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.3f, 0.35f, 0.3f);
    }
}
