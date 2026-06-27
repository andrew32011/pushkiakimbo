using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrowdRunner;

// Идемпотентный сборщик сцены Crowd Runner (v2).
// Tools → CrowdRunner → Build Scene.
public static class RunnerSceneBuilder
{
    private const string SystemsRoot = "--- GAME SYSTEMS ---";
    private const string EnvRoot = "--- ENVIRONMENT ---";
    private const string CanvasName = "GameCanvas";
    private const string GenFolder = "Assets/Generated";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string StickmanPath = "Assets/FastMesh/Prefabs/Stickman/Stickman1.prefab";

    // Mini UI sprites
    private const string MiniBtn = "Assets/Mini UI/Buttons/Plain Buttons/256Px Rectangle Plain Button/Long Round Buttons Plain ";
    private const string PanelDark = "Assets/Mini UI/9 Splice Panels/Dark Theme RoundEdge Panels/Dark Theme RoundEdge DARK.png";
    private const string PanelBlue = "Assets/Mini UI/9 Splice Panels/Dark Theme RoundEdge Panels/Dark Theme RoundEdge BLUE.png";
    private const string IconCoin = "Assets/Mini UI/Icons/Golden Coin.png";
    private const string IconGem = "Assets/Mini UI/Icons/Blue Gem.png";
    private const string IconCrown = "Assets/Mini UI/Icons/Crown.png";
    private const string IconPlay = "Assets/Mini UI/UI Icons/Play.png";
    private const string IconCart = "Assets/Mini UI/UI Icons/Cart.png";
    private const string IconGear = "Assets/Mini UI/UI Icons/Gear.png";
    private const string IconHome = "Assets/Mini UI/UI Icons/Home.png";
    private const string IconClose = "Assets/Mini UI/UI Icons/Close.png";
    private const string ProgBg = "Assets/Mini UI/Progress Bar/Progress Bar BG.png";
    private const string ProgFill = "Assets/Mini UI/Progress Bar/Progress bar fill GREEN.png";

    private static readonly string[] WeaponPrefabPaths =
    {
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)Bat.prefab",
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)Bow.prefab",
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)HuntingRifle.prefab",
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)Rifle.prefab",
    };

    private static Font _font;
    private static Font UiFont => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    [MenuItem("Tools/CrowdRunner/Build Scene")]
    public static void BuildScene()
    {
        var active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DestroyByName(SystemsRoot);
        DestroyByName(EnvRoot);
        DestroyByName(CanvasName);
        EnsureFolder();

        // ---- Префабы ----
        GameObject unitPrefab = BuildUnitPrefab();
        EnemyController enemyPrefab = BuildEnemyPrefab();
        Gate gatePrefab = BuildGatePrefab();
        Booster boosterPrefab = BuildBoosterPrefab();
        GiftBox giftPrefab = BuildGiftPrefab();
        Projectile projPrefab = BuildProjectilePrefab();
        FloatingText floatPrefab = BuildFloatingTextPrefab();
        ParticleSystem burstPrefab = BuildBurstPrefab();
        GameObject[] weaponModels = LoadWeaponModels();

        BuildEnvironment();

        var systems = new GameObject(SystemsRoot).transform;

        var audio = new GameObject("AudioController").AddComponent<AudioController>();
        audio.transform.SetParent(systems);

        var effects = new GameObject("EffectsManager").AddComponent<EffectsManager>();
        effects.transform.SetParent(systems);
        SetRef(effects, "_burstPrefab", burstPrefab);
        SetRef(effects, "_floatingPrefab", floatPrefab);

        var input = new GameObject("RunnerInput").AddComponent<RunnerInput>();
        input.transform.SetParent(systems);

        var squad = BuildSquad(unitPrefab, projPrefab, input, weaponModels);
        squad.transform.SetParent(systems, true);

        var cam = BuildCamera(squad.transform);

        var spawnerGO = new GameObject("LevelSpawner");
        spawnerGO.transform.SetParent(systems);
        var spawner = spawnerGO.AddComponent<LevelSpawner>();
        SetRef(spawner, "_gatePrefab", gatePrefab);
        SetRef(spawner, "_enemyPrefab", enemyPrefab);
        SetRef(spawner, "_boosterPrefab", boosterPrefab);
        SetRef(spawner, "_giftPrefab", giftPrefab);
        SetRef(spawner, "_squad", squad.transform);

        var ui = BuildCanvas();

        var gm = new GameObject("RunnerGameManager").AddComponent<RunnerGameManager>();
        gm.transform.SetParent(systems);
        SetRef(gm, "_squad", squad);
        SetRef(gm, "_spawner", spawner);
        SetRef(gm, "_input", input);
        SetRef(gm, "_camera", cam);
        SetRef(gm, "_menuUI", ui.menu);
        SetRef(gm, "_hudUI", ui.hud);
        SetRef(gm, "_defeatUI", ui.defeat);
        SetRef(gm, "_victoryUI", ui.victory);
        SetRef(gm, "_upgradeUI", ui.upgrade);
        SetRef(gm, "_settingsUI", ui.settings);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[CrowdRunner] Scene built successfully (v2).");
    }

    // ================= ПРЕФАБЫ =================

    private static GameObject BuildUnitPrefab()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPath);
        var root = new GameObject("UnitStickman");
        WrapModel(src, root.transform, 1.6f, new Color(0.3f, 0.6f, 1f));
        var uc = root.AddComponent<UnitController>();

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(root.transform); muzzle.localPosition = new Vector3(0f, 0.7f, 0.35f);
        SetRef(uc, "_muzzle", muzzle);
        var mount = new GameObject("WeaponMount").transform;
        mount.SetParent(root.transform); mount.localPosition = new Vector3(0.22f, 0.75f, 0.2f);
        SetRef(uc, "_weaponMount", mount);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/UnitStickman.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static EnemyController BuildEnemyPrefab()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPath);
        var root = new GameObject("EnemyStickman");
        var model = WrapModel(src, root.transform, 1.7f, new Color(0.85f, 0.25f, 0.25f));

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.85f, 0f);
        col.size = new Vector3(0.9f, 1.7f, 0.9f);

        var ec = root.AddComponent<EnemyController>();
        var label = MakeTextMesh(root.transform, "10", new Vector3(0f, 2.15f, 0f), 0.2f, Color.white);

        // HP-бар (одиночная полоса с пивотом слева)
        var pivot = new GameObject("HpBarPivot").transform;
        pivot.SetParent(root.transform); pivot.localPosition = new Vector3(-0.5f, 2.55f, 0f);
        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "HpFill"; fill.transform.SetParent(pivot);
        Object.DestroyImmediate(fill.GetComponent<Collider>());
        fill.transform.localScale = new Vector3(1f, 0.12f, 0.06f);
        fill.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        fill.GetComponent<Renderer>().sharedMaterial = SolidMat(new Color(0.3f, 1f, 0.35f));

        SetRef(ec, "_label", label.GetComponent<TextMesh>());
        SetRef(ec, "_hpBarFill", pivot);
        SetRefArray(ec, "_renderers", model.GetComponentsInChildren<Renderer>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/EnemyStickman.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<EnemyController>();
    }

    private static Gate BuildGatePrefab()
    {
        var root = new GameObject("Gate");
        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "Background"; bg.transform.SetParent(root.transform);
        bg.transform.localScale = new Vector3(1.5f, 2.4f, 0.12f);
        bg.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        Object.DestroyImmediate(bg.GetComponent<Collider>());
        var bgRend = bg.GetComponent<Renderer>();
        bgRend.sharedMaterial = SolidMat(Color.white);

        var trigger = root.AddComponent<BoxCollider>();
        trigger.center = new Vector3(0f, 1.2f, 0f);
        trigger.size = new Vector3(1.5f, 2.4f, 0.5f);
        trigger.isTrigger = true;

        var gate = root.AddComponent<Gate>();
        var label = MakeTextMesh(root.transform, "+10", new Vector3(0f, 1.2f, -0.12f), 0.4f, Color.white);
        SetRef(gate, "_label", label.GetComponent<TextMesh>());
        SetRef(gate, "_background", bgRend);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/Gate.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Gate>();
    }

    private static Booster BuildBoosterPrefab()
    {
        var root = new GameObject("Booster");
        // разрушаемый блок-бонус
        var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "Crate"; crate.transform.SetParent(root.transform);
        crate.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
        crate.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        Object.DestroyImmediate(crate.GetComponent<Collider>());
        crate.GetComponent<Renderer>().sharedMaterial = SolidMat(new Color(0.5f, 0.85f, 0.6f));

        // компактный триггер вокруг блока — ловит снаряды и касание отряда
        var zone = root.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 0.8f, 0f);
        zone.size = new Vector3(1.3f, 1.7f, 1.3f);

        var booster = root.AddComponent<Booster>();
        var label = MakeTextMesh(root.transform, "+5", new Vector3(0f, 2.0f, 0f), 0.35f, new Color(0.5f, 1f, 0.6f));
        SetRef(booster, "_label", label.GetComponent<TextMesh>());
        SetRef(booster, "_zone", zone);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/Booster.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Booster>();
    }

    private static GiftBox BuildGiftPrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "GiftBox";
        root.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        var col = root.GetComponent<BoxCollider>();
        col.isTrigger = true; col.size = Vector3.one * 1.4f;
        root.GetComponent<Renderer>().sharedMaterial = SolidMat(new Color(1f, 0.8f, 0.25f));
        root.AddComponent<GiftBox>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/GiftBox.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<GiftBox>();
    }

    private static Projectile BuildProjectilePrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Projectile";
        root.GetComponent<Collider>().isTrigger = true;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        root.GetComponent<Renderer>().sharedMaterial = SolidMat(Color.white);
        root.AddComponent<Projectile>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/Projectile.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<Projectile>();
    }

    private static FloatingText BuildFloatingTextPrefab()
    {
        var root = new GameObject("FloatingText");
        var ft = root.AddComponent<FloatingText>();
        var tm = MakeTextMesh(root.transform, "+0", Vector3.zero, 0.28f, Color.white).GetComponent<TextMesh>();
        SetRef(ft, "_text", tm);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/FloatingText.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<FloatingText>();
    }

    private static ParticleSystem BuildBurstPrefab()
    {
        var root = new GameObject("Burst");
        var ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.4f; main.loop = false;
        main.startLifetime = 0.4f; main.startSpeed = 4f;
        main.startSize = 0.25f; main.maxParticles = 40;
        main.playOnAwake = false;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)18) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.15f;
        var rend = root.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, GenFolder + "/Burst.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<ParticleSystem>();
    }

    private static GameObject[] LoadWeaponModels()
    {
        var arr = new GameObject[4];
        for (int i = 0; i < 4; i++)
            arr[i] = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPaths[i]);
        return arr;
    }

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
            var c = model.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
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
            model.transform.localPosition = new Vector3(0f, -b.min.y * scale, 0f);
            var mat = SolidMat(tint);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        }
        return model;
    }

    // ================= ОКРУЖЕНИЕ =================

    private static void BuildEnvironment()
    {
        var env = new GameObject(EnvRoot).transform;
        float len = 2200f, midZ = 1000f;

        MakeStrip(env, "Road", 0f, -0.05f, 12f, len, midZ, new Color(0.42f, 0.43f, 0.48f));
        MakeStrip(env, "GrassL", -12f, -0.1f, 12f, len, midZ, new Color(0.3f, 0.55f, 0.32f));
        MakeStrip(env, "GrassR", 12f, -0.1f, 12f, len, midZ, new Color(0.3f, 0.55f, 0.32f));
        MakeStrip(env, "CurbL", -6.1f, 0.1f, 0.2f, len, midZ, new Color(0.85f, 0.85f, 0.9f));
        MakeStrip(env, "CurbR", 6.1f, 0.1f, 0.2f, len, midZ, new Color(0.85f, 0.85f, 0.9f));

        // ограждения между центральной и боковыми дорожками: об них гасятся пули.
        // Тянутся вдаль и ОБРЫВАЮТСЯ на линии, где можно атаковать бонусы вблизи.
        float vulnLine = 7f;
        float farZ = midZ + len * 0.5f;
        var fenceColor = new Color(0.7f, 0.72f, 0.78f);
        MakeWall(env, "FenceL", -7.5f, vulnLine, farZ, fenceColor);
        MakeWall(env, "FenceR", 7.5f, vulnLine, farZ, fenceColor);

        var light = Object.FindObjectOfType<Light>();
        if (light == null) { light = new GameObject("Directional Light").AddComponent<Light>(); light.type = LightType.Directional; }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.transform.SetParent(env);
    }

    private static void MakeStrip(Transform parent, string name, float x, float y, float width, float length, float midZ, Color color)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s.name = name; s.transform.SetParent(parent);
        s.transform.localScale = new Vector3(width, 0.1f, length);
        s.transform.localPosition = new Vector3(x, y, midZ);
        Object.DestroyImmediate(s.GetComponent<Collider>());
        s.GetComponent<Renderer>().sharedMaterial = SolidMat(color);
    }

    private static void MakeWall(Transform parent, string name, float x, float startZ, float endZ, Color color)
    {
        float length = Mathf.Max(0.1f, endZ - startZ);
        float midZ = (startZ + endZ) * 0.5f;
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name; w.transform.SetParent(parent);
        w.transform.localScale = new Vector3(0.2f, 1.4f, length);
        w.transform.localPosition = new Vector3(x, 0.7f, midZ);
        w.AddComponent<LaneWall>();   // коллайдер оставляем — об него гасятся пули
        w.GetComponent<Renderer>().sharedMaterial = SolidMat(color);
    }

    // ================= ОТРЯД / КАМЕРА =================

    private static SquadController BuildSquad(GameObject unitPrefab, Projectile projPrefab, RunnerInput input, GameObject[] weaponModels)
    {
        var go = new GameObject("Squad");
        go.transform.position = Vector3.zero;
        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var sensor = go.AddComponent<BoxCollider>();
        sensor.isTrigger = true; sensor.center = new Vector3(0f, 0.7f, 0.6f); sensor.size = new Vector3(1f, 1.6f, 0.9f);

        var countText = MakeTextMesh(go.transform, "0", new Vector3(0f, 2.6f, 0.5f), 0.5f, Color.white).GetComponent<TextMesh>();

        var squad = go.AddComponent<SquadController>();
        SetRef(squad, "_unitPrefab", unitPrefab);
        SetRef(squad, "_formationRoot", go.transform);
        SetRef(squad, "_projectilePrefab", projPrefab);
        SetRef(squad, "_sensor", sensor);
        SetRef(squad, "_input", input);
        SetRef(squad, "_countText", countText);
        SetRefArray(squad, "_weaponModels", weaponModels);
        return squad;
    }

    private static CameraFollow BuildCamera(Transform target)
    {
        var camGO = Camera.main != null ? Camera.main.gameObject : null;
        if (camGO == null) { camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera"; camGO.AddComponent<Camera>(); }
        var cam = camGO.GetComponent<Camera>();
        cam.fieldOfView = 56f; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.5f, 0.72f, 0.95f);
        var follow = camGO.GetComponent<CameraFollow>() ?? camGO.AddComponent<CameraFollow>();
        SetRef(follow, "_target", target);
        return follow;
    }

    // ================= UI =================

    private struct UiRefs
    {
        public MainMenuUI menu; public HudUI hud; public DefeatUI defeat;
        public VictoryUI victory; public UpgradeUI upgrade; public SettingsUI settings;
    }

    private static UiRefs BuildCanvas()
    {
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
        var root = canvas.transform;

        var refs = new UiRefs();

        // ---------- HUD ----------
        var hudPanel = Panel(root, "HUD", new Color(0, 0, 0, 0));
        var hud = canvasGO.AddComponent<HudUI>();
        SetRef(hud, "_root", hudPanel);
        var coinsHud = Counter(hudPanel.transform, IconCoin, new Vector2(0, 1), new Vector2(40, -50), "0");
        var levelHud = Label(hudPanel.transform, "Ур. 1", new Vector2(-40, -50), new Vector2(300, 60), 34, TextAnchor.MiddleRight, new Vector2(1, 1));
        var wpnHud = Label(hudPanel.transform, "Ручное", new Vector2(-40, -110), new Vector2(300, 50), 28, TextAnchor.MiddleRight, new Vector2(1, 1));
        var progress = SpriteSlider(hudPanel.transform, new Vector2(0, -140), new Vector2(760, 44), new Vector2(0.5f, 1), ProgBg, ProgFill);
        progress.interactable = false; progress.value = 0f;
        Icon(hudPanel.transform, IconPlay, new Vector2(-400, -140), new Vector2(46, 46), new Vector2(0.5f, 1));
        Icon(hudPanel.transform, IconCrown, new Vector2(400, -140), new Vector2(52, 52), new Vector2(0.5f, 1));
        SetRef(hud, "_coinsText", coinsHud);
        SetRef(hud, "_levelText", levelHud);
        SetRef(hud, "_weaponText", wpnHud);
        SetRef(hud, "_progress", progress);
        refs.hud = hud;

        // ---------- MAIN MENU ----------
        var menuPanel = SpritePanel(root, "MainMenu", PanelDark, new Color(0.12f, 0.14f, 0.22f, 1f));
        Stretch(menuPanel.GetComponent<RectTransform>());
        var menu = canvasGO.AddComponent<MainMenuUI>();
        SetRef(menu, "_root", menuPanel);
        Label(menuPanel.transform, "CROWD RUNNER", new Vector2(0, 640), new Vector2(1000, 130), 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        Label(menuPanel.transform, "Epochs of War", new Vector2(0, 545), new Vector2(900, 70), 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var coinsMenu = Counter(menuPanel.transform, IconCoin, new Vector2(0.5f, 0.5f), new Vector2(-170, 780), "0");
        var gemMenu = Counter(menuPanel.transform, IconGem, new Vector2(0.5f, 0.5f), new Vector2(120, 780), "0");
        var levelMenu = Label(menuPanel.transform, "Уровень 1", new Vector2(0, 230), new Vector2(700, 80), 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var playBtn = IconButton(menuPanel.transform, "ИГРАТЬ", "GREEN", IconPlay, new Vector2(0, 40), new Vector2(620, 170), 52);
        var shopBtn = IconButton(menuPanel.transform, "Магазин", "BLUE", IconCart, new Vector2(0, -160), new Vector2(620, 130), 40);
        var setBtn = IconButton(menuPanel.transform, "Настройки", "GREY", IconGear, new Vector2(0, -310), new Vector2(620, 130), 40);
        SetRef(menu, "_coinsText", coinsMenu);
        SetRef(menu, "_crystalsText", gemMenu);
        SetRef(menu, "_levelText", levelMenu);
        Wire(playBtn, menu.OnPlay); Wire(shopBtn, menu.OnShop); Wire(setBtn, menu.OnSettings);
        refs.menu = menu;

        // ---------- DEFEAT ----------
        var defOverlay = Panel(root, "Defeat", new Color(0, 0, 0, 0.6f));
        var defeat = canvasGO.AddComponent<DefeatUI>();
        SetRef(defeat, "_root", defOverlay);
        var defCard = SpritePanel(defOverlay.transform, "Card", PanelDark, new Color(0.5f, 0.18f, 0.2f, 1f));
        Card(defCard, new Vector2(880, 1080));
        Label(defCard.transform, "Вы проиграли", new Vector2(0, 360), new Vector2(800, 110), 58, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var defResult = Label(defCard.transform, "Награда: 0", new Vector2(0, 190), new Vector2(800, 120), 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var contBtn = IconButton(defCard.transform, "Продолжить (реклама)", "GREEN", IconPlay, new Vector2(0, 20), new Vector2(720, 150), 36);
        var retryBtn = IconButton(defCard.transform, "Заново", "YELLOW", null, new Vector2(0, -150), new Vector2(620, 130), 40);
        var defHome = IconButton(defCard.transform, "В меню", "GREY", IconHome, new Vector2(0, -300), new Vector2(620, 120), 38);
        SetRef(defeat, "_resultText", defResult);
        SetRef(defeat, "_continueButton", contBtn.gameObject);
        Wire(contBtn, defeat.OnContinue); Wire(retryBtn, defeat.OnRetry); Wire(defHome, defeat.OnMenu);
        refs.defeat = defeat;

        // ---------- VICTORY ----------
        var vicOverlay = Panel(root, "Victory", new Color(0, 0, 0, 0.6f));
        var victory = canvasGO.AddComponent<VictoryUI>();
        SetRef(victory, "_root", vicOverlay);
        var vicCard = SpritePanel(vicOverlay.transform, "Card", PanelDark, new Color(0.16f, 0.5f, 0.24f, 1f));
        Card(vicCard, new Vector2(880, 1080));
        Label(vicCard.transform, "Победа!", new Vector2(0, 360), new Vector2(800, 120), 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var vicResult = Label(vicCard.transform, "Выжило: 0", new Vector2(0, 150), new Vector2(800, 220), 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var nextBtn = IconButton(vicCard.transform, "Дальше", "GREEN", IconPlay, new Vector2(0, -60), new Vector2(680, 150), 44);
        var x2Btn = IconButton(vicCard.transform, "x2 награда (реклама)", "YELLOW", null, new Vector2(0, -230), new Vector2(720, 130), 36);
        var vicHome = IconButton(vicCard.transform, "В меню", "GREY", IconHome, new Vector2(0, -370), new Vector2(560, 110), 36);
        SetRef(victory, "_resultText", vicResult);
        Wire(nextBtn, victory.OnNext); Wire(x2Btn, victory.OnDouble); Wire(vicHome, victory.OnMenu);
        refs.victory = victory;

        // ---------- UPGRADES (SHOP) ----------
        var upgOverlay = Panel(root, "Upgrades", new Color(0, 0, 0, 0.6f));
        var upg = canvasGO.AddComponent<UpgradeUI>();
        SetRef(upg, "_root", upgOverlay);
        var upgCard = SpritePanel(upgOverlay.transform, "Card", PanelDark, new Color(0.14f, 0.16f, 0.26f, 1f));
        Card(upgCard, new Vector2(960, 1500));
        Label(upgCard.transform, "МАГАЗИН", new Vector2(0, 640), new Vector2(800, 100), 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var upgCoins = Counter(upgCard.transform, IconCoin, new Vector2(0.5f, 0.5f), new Vector2(0, 540), "0");
        string[] names = { "Урон", "Старт. юниты", "Скорострельность", "Залп" };
        var lvlTexts = new Text[4]; var costTexts = new Text[4]; var buyBtns = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float y = 380 - i * 180;
            Label(upgCard.transform, names[i], new Vector2(-220, y), new Vector2(520, 60), 36, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f));
            lvlTexts[i] = Label(upgCard.transform, "Ур. 0", new Vector2(-220, y - 55), new Vector2(520, 46), 28, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f));
            buyBtns[i] = IconButton(upgCard.transform, "", "GREEN", IconCoin, new Vector2(250, y), new Vector2(320, 120), 34);
            costTexts[i] = Label(buyBtns[i].transform, "0", new Vector2(30, 0), new Vector2(200, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        }
        SetRefArray(upg, "_levelTexts", lvlTexts);
        SetRefArray(upg, "_costTexts", costTexts);
        SetRef(upg, "_coinsText", upgCoins);
        Wire(buyBtns[0], upg.OnBuyDamage); Wire(buyBtns[1], upg.OnBuyStartUnits);
        Wire(buyBtns[2], upg.OnBuyFireRate); Wire(buyBtns[3], upg.OnBuyVolley);
        var freeBtn = IconButton(upgCard.transform, "Бесплатно (реклама)", "YELLOW", null, new Vector2(0, -480), new Vector2(720, 120), 36);
        var upgClose = IconButton(upgCard.transform, "Закрыть", "RED", IconClose, new Vector2(0, -620), new Vector2(520, 110), 38);
        Wire(freeBtn, upg.OnFreeUpgrade); Wire(upgClose, upg.OnClose);
        refs.upgrade = upg;

        // ---------- SETTINGS ----------
        var setOverlay = Panel(root, "Settings", new Color(0, 0, 0, 0.6f));
        var settings = canvasGO.AddComponent<SettingsUI>();
        SetRef(settings, "_root", setOverlay);
        var setCard = SpritePanel(setOverlay.transform, "Card", PanelDark, new Color(0.14f, 0.16f, 0.26f, 1f));
        Card(setCard, new Vector2(880, 900));
        Label(setCard.transform, "НАСТРОЙКИ", new Vector2(0, 320), new Vector2(800, 100), 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        Label(setCard.transform, "Музыка", new Vector2(0, 170), new Vector2(700, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var musicSlider = SpriteSlider(setCard.transform, new Vector2(0, 100), new Vector2(640, 44), new Vector2(0.5f, 0.5f), ProgBg, ProgFill);
        Label(setCard.transform, "Звуки", new Vector2(0, -20), new Vector2(700, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var sfxSlider = SpriteSlider(setCard.transform, new Vector2(0, -90), new Vector2(640, 44), new Vector2(0.5f, 0.5f), ProgBg, ProgFill);
        var setClose = IconButton(setCard.transform, "Закрыть", "RED", IconClose, new Vector2(0, -300), new Vector2(520, 110), 38);
        SetRef(settings, "_music", musicSlider);
        SetRef(settings, "_sfx", sfxSlider);
        Wire(setClose, settings.OnClose);
        refs.settings = settings;

        hudPanel.SetActive(false);
        defOverlay.SetActive(false); vicOverlay.SetActive(false);
        upgOverlay.SetActive(false); setOverlay.SetActive(false);
        menuPanel.SetActive(true);
        return refs;
    }

    // ================= UI ХЕЛПЕРЫ =================

    private static Sprite LoadSprite(string path) => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static Material SolidMat(Color c) => new Material(Shader.Find("Standard")) { color = c };

    private static GameObject Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        Stretch(rt);
        var img = go.GetComponent<Image>(); img.color = color;
        if (color.a == 0f) img.raycastTarget = false;
        return go;
    }

    private static GameObject SpritePanel(Transform parent, string name, string spritePath, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        var sp = LoadSprite(spritePath);
        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
        img.color = color;
        return go;
    }

    private static void Card(GameObject card, Vector2 size)
    {
        var rt = card.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
    }

    private static Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, TextAnchor anchor, Vector2 pivot)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = pivot;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.text = text; t.font = UiFont; t.fontSize = fontSize; t.color = Color.white;
        t.alignment = anchor; t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        return t;
    }

    private static Image Icon(Transform parent, string spritePath, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = pivot;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>(); img.raycastTarget = false;
        var sp = LoadSprite(spritePath);
        if (sp != null) img.sprite = sp; else img.color = new Color(1, 1, 1, 0.4f);
        return img;
    }

    // Счётчик: иконка + текст, привязка по pivot.
    private static Text Counter(Transform parent, string iconPath, Vector2 pivot, Vector2 pos, string value)
    {
        var holder = new GameObject("Counter", typeof(RectTransform));
        var hrt = holder.GetComponent<RectTransform>(); hrt.SetParent(parent, false);
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = pivot;
        hrt.sizeDelta = new Vector2(260, 70); hrt.anchoredPosition = pos;
        Icon(holder.transform, iconPath, new Vector2(35, 0), new Vector2(60, 60), new Vector2(0, 0.5f));
        var t = Label(holder.transform, value, new Vector2(105, 0), new Vector2(160, 60), 40, TextAnchor.MiddleLeft, new Vector2(0, 0.5f));
        return t;
    }

    private static Button IconButton(Transform parent, string text, string colorName, string iconPath, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        var sp = LoadSprite(MiniBtn + colorName + ".png");
        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.25f, 0.5f, 0.85f);
        var btn = go.AddComponent<Button>();

        if (!string.IsNullOrEmpty(iconPath))
            Icon(go.transform, iconPath, new Vector2(-size.x * 0.5f + 60, 0), new Vector2(56, 56), new Vector2(0.5f, 0.5f));
        if (!string.IsNullOrEmpty(text))
        {
            var label = Label(go.transform, text, Vector2.zero, size, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
            label.color = new Color(0.15f, 0.12f, 0.05f);
        }
        return btn;
    }

    private static Slider SpriteSlider(Transform parent, Vector2 pos, Vector2 size, Vector2 anchor, string bgPath, string fillPath)
    {
        var go = new GameObject("Slider", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var slider = go.AddComponent<Slider>();

        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.GetComponent<RectTransform>().SetParent(rt, false); Stretch(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        var bgSp = LoadSprite(bgPath);
        if (bgSp != null) { bgImg.sprite = bgSp; bgImg.type = Image.Type.Sliced; } else bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.GetComponent<RectTransform>().SetParent(rt, false); Stretch(fillArea.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.GetComponent<RectTransform>().SetParent(fillArea.transform, false); Stretch(fill.GetComponent<RectTransform>());
        var fillImg = fill.GetComponent<Image>();
        var fillSp = LoadSprite(fillPath);
        if (fillSp != null) { fillImg.sprite = fillSp; fillImg.type = Image.Type.Sliced; } else fillImg.color = new Color(0.3f, 0.7f, 1f);

        slider.targetGraphic = bgImg; slider.fillRect = fill.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        return slider;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Wire(Button btn, UnityAction action) => UnityEventTools.AddVoidPersistentListener(btn.onClick, action);

    // ================= 3D ТЕКСТ =================

    private static GameObject MakeTextMesh(Transform parent, string text, Vector3 localPos, float charSize, Color color)
    {
        var go = new GameObject("Label3D");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.characterSize = charSize; tm.fontSize = 64; tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.font = UiFont;
        go.GetComponent<MeshRenderer>().sharedMaterial = UiFont.material;
        return go;
    }

    // ================= УТИЛИТЫ =================

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(GenFolder)) AssetDatabase.CreateFolder("Assets", "Generated");
    }

    private static void DestroyByName(string name)
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) Object.DestroyImmediate(go);
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
        for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
