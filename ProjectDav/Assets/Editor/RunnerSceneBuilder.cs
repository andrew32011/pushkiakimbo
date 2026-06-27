using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrowdRunner;

// Идемпотентный сборщик сцены Crowd Runner.
// Tools → CrowdRunner → Build Scene — полностью пересобирает окружение, системы, префабы и UI.
public static class RunnerSceneBuilder
{
    private const string SystemsRoot = "--- GAME SYSTEMS ---";
    private const string EnvRoot = "--- ENVIRONMENT ---";
    private const string CanvasName = "GameCanvas";
    private const string GenFolder = "Assets/Generated";
    private const string StickmanPath = "Assets/FastMesh/Prefabs/Stickman/Stickman1.prefab";

    private static Font _font;
    private static Font UiFont => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/CrowdRunner/Build Scene")]
    public static void BuildScene()
    {
        // гарантируем, что собираем в SampleScene (в т.ч. при запуске из batch)
        var active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // чистим предыдущую генерацию
        DestroyByName(SystemsRoot);
        DestroyByName(EnvRoot);
        DestroyByName(CanvasName);

        EnsureFolder();

        // ---- Префабы ----
        GameObject unitPrefab = BuildUnitPrefab();
        EnemyController enemyPrefab = BuildEnemyPrefab();
        Gate gatePrefab = BuildGatePrefab();
        Projectile projPrefab = BuildProjectilePrefab();

        // ---- Окружение ----
        BuildEnvironment();

        // ---- Системы ----
        var systems = new GameObject(SystemsRoot).transform;

        // Audio
        var audioGO = new GameObject("AudioController");
        audioGO.transform.SetParent(systems);
        var audio = audioGO.AddComponent<AudioController>();

        // Input
        var inputGO = new GameObject("RunnerInput");
        inputGO.transform.SetParent(systems);
        var input = inputGO.AddComponent<RunnerInput>();

        // Squad
        var squad = BuildSquad(unitPrefab, projPrefab, input);
        squad.transform.SetParent(systems, true);

        // Camera
        var cam = BuildCamera(squad.transform);

        // Spawner
        var spawnerGO = new GameObject("LevelSpawner");
        spawnerGO.transform.SetParent(systems);
        var spawner = spawnerGO.AddComponent<LevelSpawner>();
        SetRef(spawner, "_gatePrefab", gatePrefab);
        SetRef(spawner, "_enemyPrefab", enemyPrefab);
        SetRef(spawner, "_squad", squad.transform);

        // ---- UI ----
        var ui = BuildCanvas();

        // ---- Manager ----
        var gmGO = new GameObject("RunnerGameManager");
        gmGO.transform.SetParent(systems);
        var gm = gmGO.AddComponent<RunnerGameManager>();
        SetRef(gm, "_squad", squad);
        SetRef(gm, "_spawner", spawner);
        SetRef(gm, "_input", input);
        SetRef(gm, "_camera", cam);
        SetRef(gm, "_menuUI", ui.menu);
        SetRef(gm, "_hudUI", ui.hud);
        SetRef(gm, "_gameOverUI", ui.gameOver);
        SetRef(gm, "_winUI", ui.win);
        SetRef(gm, "_upgradeUI", ui.upgrade);
        SetRef(gm, "_settingsUI", ui.settings);

        // ориентация под портрет
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[CrowdRunner] Scene built successfully.");
    }

    // ================= ПРЕФАБЫ =================

    private static GameObject BuildUnitPrefab()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPath);
        var root = new GameObject("UnitStickman");
        var model = WrapModel(src, root.transform, 1.6f, new Color(0.3f, 0.6f, 1f));
        var uc = root.AddComponent<UnitController>();
        // muzzle
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(root.transform);
        muzzle.localPosition = new Vector3(0f, 0.7f, 0.3f);
        SetRef(uc, "_muzzle", muzzle);
        var mount = new GameObject("WeaponMount").transform;
        mount.SetParent(root.transform);
        mount.localPosition = new Vector3(0.2f, 0.7f, 0.2f);
        SetRef(uc, "_weaponMount", mount);

        string path = GenFolder + "/UnitStickman.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static EnemyController BuildEnemyPrefab()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPath);
        var root = new GameObject("EnemyStickman");
        var model = WrapModel(src, root.transform, 1.7f, new Color(0.85f, 0.25f, 0.25f));
        root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // лицом к отряду

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.85f, 0f);
        col.size = new Vector3(0.9f, 1.7f, 0.9f);

        var ec = root.AddComponent<EnemyController>();
        var label = MakeTextMesh(root.transform, "10", new Vector3(0f, 2.1f, 0f), 0.18f, Color.white);
        SetRef(ec, "_label", label.GetComponent<TextMesh>());
        SetRefArray(ec, "_renderers", model.GetComponentsInChildren<Renderer>());

        string path = GenFolder + "/EnemyStickman.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<EnemyController>();
    }

    private static Gate BuildGatePrefab()
    {
        var root = new GameObject("Gate");
        // фон-панель ворот
        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "Background";
        bg.transform.SetParent(root.transform);
        bg.transform.localScale = new Vector3(1.4f, 2.2f, 0.12f);
        bg.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        var bgCol = bg.GetComponent<Collider>();
        Object.DestroyImmediate(bgCol);
        var bgRend = bg.GetComponent<Renderer>();
        bgRend.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(1f, 1f, 1f, 1f) };

        // триггер-зона ворот
        var trigger = root.AddComponent<BoxCollider>();
        trigger.center = new Vector3(0f, 1.1f, 0f);
        trigger.size = new Vector3(1.4f, 2.2f, 0.5f);
        trigger.isTrigger = true;

        var gate = root.AddComponent<Gate>();
        var label = MakeTextMesh(root.transform, "+1", new Vector3(0f, 1.1f, -0.1f), 0.3f, Color.white);
        SetRef(gate, "_label", label.GetComponent<TextMesh>());
        SetRef(gate, "_background", bgRend);

        string path = GenFolder + "/Gate.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Gate>();
    }

    private static Projectile BuildProjectilePrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Projectile";
        var col = root.GetComponent<Collider>();
        col.isTrigger = true;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var rend = root.GetComponent<Renderer>();
        rend.sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.white };
        root.AddComponent<Projectile>();

        string path = GenFolder + "/Projectile.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Projectile>();
    }

    // Оборачивает модель в корень, нормализуя высоту и ставя «ноги» на y=0.
    private static GameObject WrapModel(GameObject src, Transform parent, float targetHeight, Color tint)
    {
        GameObject model;
        if (src != null)
        {
            model = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var c = model.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        model.transform.SetParent(parent, false);

        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            float h = Mathf.Max(0.01f, b.size.y);
            float scale = targetHeight / h;
            model.transform.localScale = Vector3.one * scale;
            // поставить на пол
            float minY = b.min.y * scale; // приблизительно (bounds в мире при scale=1)
            model.transform.localPosition = new Vector3(0f, -b.min.y * scale, 0f);

            // тонировка
            var mat = new Material(Shader.Find("Standard")) { color = tint };
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
        }
        return model;
    }

    // ================= ОКРУЖЕНИЕ =================

    private static void BuildEnvironment()
    {
        var env = new GameObject(EnvRoot).transform;

        // три полосы разного цвета
        MakeLane(env, -1.7f, new Color(0.85f, 0.55f, 0.3f), "LaneLeft");   // оружие
        MakeLane(env, 0f, new Color(0.45f, 0.45f, 0.5f), "LaneCenter");    // враги
        MakeLane(env, 1.7f, new Color(0.3f, 0.5f, 0.8f), "LaneRight");     // юниты

        // бортики
        MakeWall(env, -2.7f);
        MakeWall(env, 2.7f);

        // свет
        var light = Object.FindObjectOfType<Light>();
        if (light == null)
        {
            var lgo = new GameObject("Directional Light");
            light = lgo.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.transform.SetParent(env);
    }

    private static void MakeLane(Transform parent, float x, Color color, string name)
    {
        var lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = name;
        lane.transform.SetParent(parent);
        lane.transform.localScale = new Vector3(1.7f, 0.1f, 800f);
        lane.transform.localPosition = new Vector3(x, -0.05f, 380f);
        Object.DestroyImmediate(lane.GetComponent<Collider>());
        lane.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
    }

    private static void MakeWall(Transform parent, float x)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.localScale = new Vector3(0.15f, 0.6f, 800f);
        wall.transform.localPosition = new Vector3(x, 0.3f, 380f);
        Object.DestroyImmediate(wall.GetComponent<Collider>());
        wall.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.2f, 0.22f) };
    }

    // ================= ОТРЯД =================

    private static SquadController BuildSquad(GameObject unitPrefab, Projectile projPrefab, RunnerInput input)
    {
        var squadGO = new GameObject("Squad");
        squadGO.transform.position = Vector3.zero;

        var rb = squadGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var sensor = squadGO.AddComponent<BoxCollider>();
        sensor.isTrigger = true;
        sensor.center = new Vector3(0f, 0.6f, 0.6f);
        sensor.size = new Vector3(1f, 1.4f, 0.8f);

        var squad = squadGO.AddComponent<SquadController>();
        SetRef(squad, "_unitPrefab", unitPrefab);
        SetRef(squad, "_formationRoot", squadGO.transform);
        SetRef(squad, "_projectilePrefab", projPrefab);
        SetRef(squad, "_sensor", sensor);
        SetRef(squad, "_input", input);
        return squad;
    }

    private static CameraFollow BuildCamera(Transform target)
    {
        var camGO = Camera.main != null ? Camera.main.gameObject : null;
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.AddComponent<Camera>();
        }
        var cam = camGO.GetComponent<Camera>();
        cam.fieldOfView = 55f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.5f, 0.7f, 0.95f);
        var follow = camGO.GetComponent<CameraFollow>();
        if (follow == null) follow = camGO.AddComponent<CameraFollow>();
        SetRef(follow, "_target", target);
        return follow;
    }

    // ================= UI =================

    private struct UiRefs
    {
        public MainMenuUI menu; public HudUI hud; public GameOverUI gameOver;
        public WinUI win; public UpgradeUI upgrade; public SettingsUI settings;
    }

    private static UiRefs BuildCanvas()
    {
        // EventSystem
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvasGO = new GameObject(CanvasName);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var refs = new UiRefs();

        // ---------- HUD ----------
        var hudPanel = Panel(canvas.transform, "HUD", new Color(0, 0, 0, 0));
        var hud = canvasGO.AddComponent<HudUI>();
        SetRef(hud, "_root", hudPanel);
        var coinsHud = Label(hudPanel.transform, "Монеты: 0", new Vector2(0, -60), new Vector2(400, 60), 34, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(40, 0));
        var unitsHud = Label(hudPanel.transform, "0", new Vector2(0, -130), new Vector2(400, 60), 40, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(40, 0));
        var dmgHud = Label(hudPanel.transform, "DMG 1", new Vector2(0, -200), new Vector2(400, 50), 30, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(40, 0));
        var wpnHud = Label(hudPanel.transform, "Stick", new Vector2(0, -200), new Vector2(400, 50), 30, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(-40, 0));
        var progress = MakeSlider(hudPanel.transform, new Vector2(0, -30), new Vector2(700, 30), new Vector2(0.5f, 1));
        progress.interactable = false;
        progress.value = 0f;
        SetRef(hud, "_coinsText", coinsHud);
        SetRef(hud, "_unitsText", unitsHud);
        SetRef(hud, "_dmgText", dmgHud);
        SetRef(hud, "_weaponText", wpnHud);
        SetRef(hud, "_progress", progress);
        refs.hud = hud;

        // ---------- MAIN MENU ----------
        var menuPanel = Panel(canvas.transform, "MainMenu", new Color(0.1f, 0.12f, 0.18f, 0.96f));
        var menu = canvasGO.AddComponent<MainMenuUI>();
        SetRef(menu, "_root", menuPanel);
        Label(menuPanel.transform, "CROWD RUNNER", new Vector2(0, 600), new Vector2(900, 120), 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var coinsMenu = Label(menuPanel.transform, "0", new Vector2(-150, 760), new Vector2(280, 60), 36, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero);
        var crystMenu = Label(menuPanel.transform, "0", new Vector2(170, 760), new Vector2(280, 60), 36, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero);
        var epochLbl = Label(menuPanel.transform, "Первобытность", new Vector2(0, 250), new Vector2(700, 70), 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var prevBtn = MakeButton(menuPanel.transform, "<", new Vector2(-330, 250), new Vector2(110, 110));
        var nextBtn = MakeButton(menuPanel.transform, ">", new Vector2(330, 250), new Vector2(110, 110));
        var playBtn = MakeButton(menuPanel.transform, "ЗАБЕГ", new Vector2(0, 50), new Vector2(560, 140));
        var upgBtn = MakeButton(menuPanel.transform, "Улучшения", new Vector2(0, -130), new Vector2(560, 110));
        var setBtn = MakeButton(menuPanel.transform, "Настройки", new Vector2(0, -270), new Vector2(560, 110));
        SetRef(menu, "_coinsText", coinsMenu);
        SetRef(menu, "_crystalsText", crystMenu);
        SetRef(menu, "_epochText", epochLbl);
        SetRef(menu, "_levelsPrev", prevBtn);
        SetRef(menu, "_levelsNext", nextBtn);
        Wire(playBtn, menu.OnPlay);
        Wire(upgBtn, menu.OnUpgrades);
        Wire(setBtn, menu.OnSettings);
        Wire(prevBtn, menu.OnPrevEpoch);
        Wire(nextBtn, menu.OnNextEpoch);
        refs.menu = menu;

        // ---------- GAME OVER ----------
        var goPanel = Panel(canvas.transform, "GameOver", new Color(0.15f, 0.05f, 0.05f, 0.95f));
        var go = canvasGO.AddComponent<GameOverUI>();
        SetRef(go, "_root", goPanel);
        var goTitle = Label(goPanel.transform, "Отряд разбит!", new Vector2(0, 450), new Vector2(900, 120), 60, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var reviveCounter = Label(goPanel.transform, "Воскрешение 0/3", new Vector2(0, 320), new Vector2(700, 60), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var reviveBtn = MakeButton(goPanel.transform, "Воскресить (реклама)", new Vector2(0, 150), new Vector2(640, 130));
        var declineBtn = MakeButton(goPanel.transform, "Отказаться", new Vector2(0, 0), new Vector2(560, 100));
        var resultBlock = Panel(goPanel.transform, "ResultBlock", new Color(0, 0, 0, 0));
        var goResult = Label(resultBlock.transform, "Награда: 0", new Vector2(0, 120), new Vector2(800, 120), 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var doubleBtn = MakeButton(resultBlock.transform, "x2 награда (реклама)", new Vector2(0, -40), new Vector2(640, 120));
        var goMenuBtn = MakeButton(resultBlock.transform, "В меню", new Vector2(0, -190), new Vector2(560, 100));
        SetRef(go, "_title", goTitle);
        SetRef(go, "_reviveCounter", reviveCounter);
        SetRef(go, "_reviveButton", reviveBtn.gameObject);
        SetRef(go, "_resultBlock", resultBlock);
        SetRef(go, "_resultText", goResult);
        Wire(reviveBtn, go.OnRevive);
        Wire(declineBtn, go.OnDecline);
        Wire(doubleBtn, go.OnDouble);
        Wire(goMenuBtn, go.OnMenu);
        refs.gameOver = go;

        // ---------- WIN ----------
        var winPanel = Panel(canvas.transform, "Win", new Color(0.05f, 0.15f, 0.07f, 0.95f));
        var win = canvasGO.AddComponent<WinUI>();
        SetRef(win, "_root", winPanel);
        var winResult = Label(winPanel.transform, "Эпоха пройдена!", new Vector2(0, 250), new Vector2(900, 260), 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var winDouble = MakeButton(winPanel.transform, "x2 награда (реклама)", new Vector2(0, -50), new Vector2(640, 120));
        var winMenu = MakeButton(winPanel.transform, "Продолжить", new Vector2(0, -210), new Vector2(560, 110));
        SetRef(win, "_resultText", winResult);
        Wire(winDouble, win.OnDouble);
        Wire(winMenu, win.OnMenu);
        refs.win = win;

        // ---------- UPGRADES ----------
        var upgPanel = Panel(canvas.transform, "Upgrades", new Color(0.1f, 0.12f, 0.18f, 0.98f));
        var upg = canvasGO.AddComponent<UpgradeUI>();
        SetRef(upg, "_root", upgPanel);
        Label(upgPanel.transform, "УЛУЧШЕНИЯ", new Vector2(0, 720), new Vector2(800, 100), 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var upgCoins = Label(upgPanel.transform, "0", new Vector2(0, 620), new Vector2(500, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        SetRef(upg, "_coinsText", upgCoins);
        string[] names = { "Урон", "Старт. юниты", "Скорость стрельбы", "Залп" };
        var lvlTexts = new Text[4];
        var costTexts = new Text[4];
        var buyBtns = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float y = 440 - i * 180;
            Label(upgPanel.transform, names[i], new Vector2(-260, y), new Vector2(500, 60), 34, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero);
            lvlTexts[i] = Label(upgPanel.transform, "Ур. 0", new Vector2(-260, y - 55), new Vector2(500, 50), 28, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero);
            buyBtns[i] = MakeButton(upgPanel.transform, "Купить", new Vector2(260, y), new Vector2(300, 110));
            costTexts[i] = Label(buyBtns[i].transform, "0", new Vector2(0, -70), new Vector2(300, 40), 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        }
        SetRefArray(upg, "_levelTexts", lvlTexts);
        SetRefArray(upg, "_costTexts", costTexts);
        Wire(buyBtns[0], upg.OnBuyDamage);
        Wire(buyBtns[1], upg.OnBuyStartUnits);
        Wire(buyBtns[2], upg.OnBuyFireRate);
        Wire(buyBtns[3], upg.OnBuyVolley);
        var freeUpgBtn = MakeButton(upgPanel.transform, "Бесплатно (реклама)", new Vector2(0, -560), new Vector2(640, 110));
        var upgClose = MakeButton(upgPanel.transform, "Закрыть", new Vector2(0, -700), new Vector2(420, 100));
        Wire(freeUpgBtn, upg.OnFreeUpgrade);
        Wire(upgClose, upg.OnClose);
        refs.upgrade = upg;

        // ---------- SETTINGS ----------
        var setPanel = Panel(canvas.transform, "Settings", new Color(0.1f, 0.12f, 0.18f, 0.98f));
        var settings = canvasGO.AddComponent<SettingsUI>();
        SetRef(settings, "_root", setPanel);
        Label(setPanel.transform, "НАСТРОЙКИ", new Vector2(0, 500), new Vector2(800, 100), 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        Label(setPanel.transform, "Музыка", new Vector2(0, 280), new Vector2(700, 60), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var musicSlider = MakeSlider(setPanel.transform, new Vector2(0, 200), new Vector2(700, 40), new Vector2(0.5f, 0.5f));
        Label(setPanel.transform, "Звуки", new Vector2(0, 80), new Vector2(700, 60), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        var sfxSlider = MakeSlider(setPanel.transform, new Vector2(0, 0), new Vector2(700, 40), new Vector2(0.5f, 0.5f));
        SetRef(settings, "_music", musicSlider);
        SetRef(settings, "_sfx", sfxSlider);
        // слайдеры подписываются в рантайме внутри SettingsUI.Awake
        var setClose = MakeButton(setPanel.transform, "Закрыть", new Vector2(0, -250), new Vector2(420, 100));
        Wire(setClose, settings.OnClose);
        refs.settings = settings;

        // стартовые состояния
        hudPanel.SetActive(false);
        goPanel.SetActive(false);
        winPanel.SetActive(false);
        upgPanel.SetActive(false);
        setPanel.SetActive(false);
        menuPanel.SetActive(true);

        return refs;
    }

    // ================= UI ХЕЛПЕРЫ =================

    private static GameObject Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        if (color.a == 0f) go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    private static Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize,
        TextAnchor anchor, Vector2 anchorPivot, Vector2 extraOffset)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorPivot; rt.anchorMax = anchorPivot; rt.pivot = anchorPivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos + extraOffset;
        var t = go.AddComponent<Text>();
        t.text = text; t.font = UiFont; t.fontSize = fontSize; t.color = Color.white;
        t.alignment = anchor; t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        return t;
    }

    private static Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.25f, 0.45f, 0.85f, 1f);
        var btn = go.AddComponent<Button>();
        var label = Label(go.transform, text, Vector2.zero, size, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);
        label.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        label.GetComponent<RectTransform>().anchorMax = Vector2.one;
        label.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        label.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        return btn;
    }

    private static Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        var go = new GameObject("Slider", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var slider = go.AddComponent<Slider>();

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.GetComponent<RectTransform>().SetParent(rt, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.GetComponent<RectTransform>().SetParent(rt, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.GetComponent<RectTransform>().SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = new Color(0.3f, 0.7f, 1f, 1f);

        slider.targetGraphic = bg.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        return slider;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Wire(Button btn, UnityAction action)
    {
        UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
    }

    // ================= 3D ТЕКСТ =================

    private static GameObject MakeTextMesh(Transform parent, string text, Vector3 localPos, float charSize, Color color)
    {
        var go = new GameObject("Label3D");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // лицом к камере (камера со стороны -Z)
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.characterSize = charSize; tm.fontSize = 64; tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.font = UiFont;
        go.GetComponent<MeshRenderer>().sharedMaterial = UiFont.material;
        // билборд через простую ориентацию к камере не делаем — статичная панель
        return go;
    }

    // ================= УТИЛИТЫ =================

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(GenFolder))
            AssetDatabase.CreateFolder("Assets", "Generated");
    }

    private static void DestroyByName(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) Object.DestroyImmediate(root);
    }

    private static void SetRef(Component comp, string field, Object value)
    {
        var so = new SerializedObject(comp);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[CrowdRunner] Поле '{field}' не найдено в {comp.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefArray(Component comp, string field, Object[] values)
    {
        var so = new SerializedObject(comp);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[CrowdRunner] Массив '{field}' не найден в {comp.GetType().Name}"); return; }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
