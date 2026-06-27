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
    private const string IconLock = "Assets/Mini UI/Icons/Lock.png";
    private const string IconMap = "Assets/Mini UI/Icons/Map.png";
    private const string IconChest = "Assets/Mini UI/Icons/Chest 1.png";
    private const string IconChestCoin = "Assets/Mini UI/Icons/Chest Of Coin.png";
    private const string IconChestGem = "Assets/Mini UI/Icons/Chest Of Gem.png";
    private const string IconGift = "Assets/Mini UI/Icons/Gift.png";
    private const string IconShield = "Assets/Mini UI/Icons/Shield.png";
    private const string IconStar = "Assets/Mini UI/Icons/Star Yellow.png";
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
        SetRef(gm, "_shopUI", ui.shop);
        SetRef(gm, "_casesUI", ui.cases);
        SetRef(gm, "_levelSelectUI", ui.levelSelect);

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
        public ShopUI shop; public CasesUI cases; public LevelSelectUI levelSelect;
    }

    private static UiRefs BuildCanvas()
    {
        // EventSystem пересоздаём заново. ВАЖНО: вычищаем ВСЕ существующие, включая
        // неактивные (иначе они копятся дубликатами и ломают ввод).
        foreach (var es in Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true))
            Object.DestroyImmediate(es.gameObject);
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var canvasGO = new GameObject(CanvasName);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // поверх возможных рантайм-оверлеев SDK, чтобы UI ловил клики
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        // match=1 (по высоте): вся вертикаль портретного дизайна (1920) влезает при любой
        // ширине, поэтому в Free Aspect/горизонтали ничего не обрезается сверху/снизу.
        scaler.matchWidthOrHeight = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var root = canvas.transform;

        var refs = new UiRefs();

        // ---------- HUD ----------
        var hudPanel = Panel(root, "HUD", new Color(0, 0, 0, 0));
        var hud = hudPanel.AddComponent<HudUI>();
        SetRef(hud, "_root", hudPanel);
        var coinsHud = Counter(hudPanel.transform, IconCoin, new Vector2(0, 1), new Vector2(40, -50), "0");
        var unitsHud = Counter(hudPanel.transform, IconCrown, new Vector2(0, 1), new Vector2(40, -130), "0"); // размер отряда
        var levelHud = Label(hudPanel.transform, "Ур. 1", new Vector2(-40, -50), new Vector2(300, 60), 34, TextAnchor.MiddleRight, new Vector2(1, 1));
        var wpnHud = Label(hudPanel.transform, "Ручное", new Vector2(-40, -110), new Vector2(300, 50), 28, TextAnchor.MiddleRight, new Vector2(1, 1));
        var progress = SpriteSlider(hudPanel.transform, new Vector2(0, -140), new Vector2(760, 44), new Vector2(0.5f, 1), ProgBg, ProgFill);
        progress.interactable = false; progress.value = 0f;
        // кнопки доступа во время боя (низ-лево)
        var shopHud = IconButton(hudPanel.transform, "Магазин", "BLUE", IconCart, new Vector2(-340, -820), new Vector2(320, 120), 32);
        var setHud = IconButton(hudPanel.transform, "Настройки", "GREY", IconGear, new Vector2(-340, -690), new Vector2(320, 120), 32);
        SetRef(hud, "_coinsText", coinsHud);
        SetRef(hud, "_unitsText", unitsHud);
        SetRef(hud, "_levelText", levelHud);
        SetRef(hud, "_weaponText", wpnHud);
        SetRef(hud, "_progress", progress);
        Wire(shopHud, hud.OnShop); Wire(setHud, hud.OnSettings);
        refs.hud = hud;

        // ---------- MAIN MENU ----------
        // Прозрачная вуаль — за меню виден реальный 3D-отряд с выбранным оружием.
        var menuPanel = Panel(root, "MainMenu", new Color(0.06f, 0.07f, 0.12f, 0.18f));
        var menu = menuPanel.AddComponent<MainMenuUI>();
        SetRef(menu, "_root", menuPanel);

        // Верх-центр: заголовок, валюты, уровень (привязаны к верхнему краю — адаптивно)
        var titleLbl = Label(menuPanel.transform, "CROWD RUNNER", Vector2.zero, new Vector2(900, 100), 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));
        AnchorRT(titleLbl.transform, new Vector2(0.5f, 1f), new Vector2(0, -30));
        var coinsMenu = Counter(menuPanel.transform, IconCoin, new Vector2(0.5f, 0.5f), Vector2.zero, "0");
        AnchorRT(coinsMenu.transform.parent, new Vector2(0.5f, 1f), new Vector2(-180, -140));
        var gemMenu = Counter(menuPanel.transform, IconGem, new Vector2(0.5f, 0.5f), Vector2.zero, "0");
        AnchorRT(gemMenu.transform.parent, new Vector2(0.5f, 1f), new Vector2(120, -140));
        var levelMenu = Label(menuPanel.transform, "Уровень 1", Vector2.zero, new Vector2(700, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));
        AnchorRT(levelMenu.transform, new Vector2(0.5f, 1f), new Vector2(0, -230));

        // Левая колонка квадратных кнопок — у левого края (адаптивно)
        var upgBtn = SquareButton(menuPanel.transform, "Апгрейды", "GREEN", IconCrown, Vector2.zero, 190);
        AnchorRT(upgBtn.transform, new Vector2(0f, 0.5f), new Vector2(120, 230));
        var shopBtn = SquareButton(menuPanel.transform, "Магазин", "BLUE", IconCart, Vector2.zero, 190);
        AnchorRT(shopBtn.transform, new Vector2(0f, 0.5f), new Vector2(120, 0));
        var casesBtn = SquareButton(menuPanel.transform, "Кейсы", "YELLOW", IconGem, Vector2.zero, 190);
        AnchorRT(casesBtn.transform, new Vector2(0f, 0.5f), new Vector2(120, -230));

        // Правая колонка — у правого края (адаптивно)
        var lvlBtn = SquareButton(menuPanel.transform, "Уровни", "BLUE", IconHome, Vector2.zero, 190);
        AnchorRT(lvlBtn.transform, new Vector2(1f, 0.5f), new Vector2(-120, 230));
        var setBtn = SquareButton(menuPanel.transform, "Настройки", "GREY", IconGear, Vector2.zero, 190);
        AnchorRT(setBtn.transform, new Vector2(1f, 0.5f), new Vector2(-120, 0));

        // «ЗАБЕГ» — низ-центр (адаптивно к нижнему краю)
        var playBtn = IconButton(menuPanel.transform, "ЗАБЕГ", "GREEN", IconPlay, Vector2.zero, new Vector2(620, 170), 54);
        AnchorRT(playBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 80));

        // ВРЕМЕННО: сброс прогресса — низ-лево
        var resetBtn = IconButton(menuPanel.transform, "СБРОС", "RED", null, Vector2.zero, new Vector2(230, 90), 28);
        AnchorRT(resetBtn.transform, new Vector2(0f, 0f), new Vector2(135, 80));

        SetRef(menu, "_coinsText", coinsMenu);
        SetRef(menu, "_crystalsText", gemMenu);
        SetRef(menu, "_levelText", levelMenu);
        Wire(playBtn, menu.OnPlay);
        Wire(upgBtn, menu.OnUpgrades); Wire(shopBtn, menu.OnShop); Wire(casesBtn, menu.OnCases);
        Wire(lvlBtn, menu.OnLevelSelect); Wire(setBtn, menu.OnSettings);
        Wire(resetBtn, menu.OnResetProgress);
        refs.menu = menu;

        // ---------- DEFEAT ----------
        var defOverlay = Panel(root, "Defeat", new Color(0, 0, 0, 0.6f));
        var defeat = defOverlay.AddComponent<DefeatUI>();
        SetRef(defeat, "_root", defOverlay);
        var defCard = SpritePanel(defOverlay.transform, "Card", PanelDark, new Color(0.5f, 0.18f, 0.2f, 1f));
        Card(defCard, new Vector2(880, 1080));
        Label(defCard.transform, "Вы проиграли", new Vector2(0, 360), new Vector2(800, 110), 58, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var defResult = Label(defCard.transform, "Награда: 0", new Vector2(0, 190), new Vector2(800, 120), 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var contBtn = IconButton(defCard.transform, "Продолжить (реклама)", "GREEN", IconPlay, new Vector2(0, 20), new Vector2(720, 150), 36);
        var defDouble = IconButton(defCard.transform, "Удвоить награду (реклама)", "YELLOW", null, new Vector2(0, 20), new Vector2(720, 150), 32);
        var retryBtn = IconButton(defCard.transform, "Заново", "YELLOW", null, new Vector2(0, -150), new Vector2(620, 130), 40);
        var defHome = IconButton(defCard.transform, "В меню", "GREY", IconHome, new Vector2(0, -300), new Vector2(620, 120), 38);
        SetRef(defeat, "_resultText", defResult);
        SetRef(defeat, "_continueButton", contBtn.gameObject);
        SetRef(defeat, "_doubleButton", defDouble.gameObject);
        SetRef(defeat, "_slideRect", defCard.GetComponent<RectTransform>());
        Wire(contBtn, defeat.OnContinue); Wire(defDouble, defeat.OnDouble); Wire(retryBtn, defeat.OnRetry); Wire(defHome, defeat.OnMenu);
        refs.defeat = defeat;

        // ---------- VICTORY ----------
        var vicOverlay = Panel(root, "Victory", new Color(0, 0, 0, 0.6f));
        var victory = vicOverlay.AddComponent<VictoryUI>();
        SetRef(victory, "_root", vicOverlay);
        var vicCard = SpritePanel(vicOverlay.transform, "Card", PanelDark, new Color(0.16f, 0.5f, 0.24f, 1f));
        Card(vicCard, new Vector2(880, 1080));
        Label(vicCard.transform, "Победа!", new Vector2(0, 360), new Vector2(800, 120), 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var vicResult = Label(vicCard.transform, "Выжило: 0", new Vector2(0, 150), new Vector2(800, 220), 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var nextBtn = IconButton(vicCard.transform, "Дальше", "GREEN", IconPlay, new Vector2(0, -60), new Vector2(680, 150), 44);
        var x2Btn = IconButton(vicCard.transform, "x2 награда (реклама)", "YELLOW", null, new Vector2(0, -230), new Vector2(720, 130), 36);
        var vicHome = IconButton(vicCard.transform, "В меню", "GREY", IconHome, new Vector2(0, -370), new Vector2(560, 110), 36);
        SetRef(victory, "_resultText", vicResult);
        SetRef(victory, "_doubleButton", x2Btn.gameObject);
        SetRef(victory, "_slideRect", vicCard.GetComponent<RectTransform>());
        Wire(nextBtn, victory.OnNext); Wire(x2Btn, victory.OnDouble); Wire(vicHome, victory.OnMenu);
        refs.victory = victory;

        // ---------- UPGRADES (SHOP) ----------
        var upgOverlay = Panel(root, "Upgrades", new Color(0, 0, 0, 0.6f));
        var upg = upgOverlay.AddComponent<UpgradeUI>();
        SetRef(upg, "_root", upgOverlay);
        var upgCard = SpritePanel(upgOverlay.transform, "Card", PanelDark, new Color(0.14f, 0.16f, 0.26f, 1f));
        Card(upgCard, new Vector2(960, 1500));
        Label(upgCard.transform, "МАГАЗИН", new Vector2(0, 640), new Vector2(800, 100), 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var upgCoins = Counter(upgCard.transform, IconCoin, new Vector2(0.5f, 0.5f), new Vector2(0, 540), "0");
        // селектор стартового оружия
        var upgWeapon = Label(upgCard.transform, "Ручное", new Vector2(0, 470), new Vector2(360, 60), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var wPrev = IconButton(upgCard.transform, "‹", "BLUE", null, new Vector2(-240, 470), new Vector2(90, 90), 48);
        var wNext = IconButton(upgCard.transform, "›", "BLUE", null, new Vector2(240, 470), new Vector2(90, 90), 48);
        SetRef(upg, "_weaponLabel", upgWeapon);
        Wire(wPrev, upg.OnPrevWeapon); Wire(wNext, upg.OnNextWeapon);
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
        SetRef(upg, "_slideRect", upgCard.GetComponent<RectTransform>());
        Wire(buyBtns[0], upg.OnBuyDamage); Wire(buyBtns[1], upg.OnBuyStartUnits);
        Wire(buyBtns[2], upg.OnBuyFireRate); Wire(buyBtns[3], upg.OnBuyVolley);
        var freeBtn = IconButton(upgCard.transform, "Бесплатно (реклама)", "YELLOW", null, new Vector2(0, -480), new Vector2(720, 120), 36);
        var upgClose = IconButton(upgCard.transform, "Закрыть", "RED", IconClose, new Vector2(-210, -630), new Vector2(440, 110), 34);
        var upgMenu = IconButton(upgCard.transform, "В меню", "GREY", IconHome, new Vector2(250, -630), new Vector2(420, 110), 34);
        Wire(freeBtn, upg.OnFreeUpgrade); Wire(upgClose, upg.OnClose); Wire(upgMenu, upg.OnMenu);
        refs.upgrade = upg;

        // ---------- SETTINGS ----------
        var setOverlay = Panel(root, "Settings", new Color(0, 0, 0, 0.6f));
        var settings = setOverlay.AddComponent<SettingsUI>();
        SetRef(settings, "_root", setOverlay);
        var setCard = SpritePanel(setOverlay.transform, "Card", PanelDark, new Color(0.14f, 0.16f, 0.26f, 1f));
        Card(setCard, new Vector2(880, 900));
        Label(setCard.transform, "НАСТРОЙКИ", new Vector2(0, 320), new Vector2(800, 100), 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        Label(setCard.transform, "Музыка", new Vector2(0, 170), new Vector2(700, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var musicSlider = SpriteSlider(setCard.transform, new Vector2(0, 100), new Vector2(640, 44), new Vector2(0.5f, 0.5f), ProgBg, ProgFill);
        Label(setCard.transform, "Звуки", new Vector2(0, -20), new Vector2(700, 60), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var sfxSlider = SpriteSlider(setCard.transform, new Vector2(0, -90), new Vector2(640, 44), new Vector2(0.5f, 0.5f), ProgBg, ProgFill);
        var setClose = IconButton(setCard.transform, "Закрыть", "RED", IconClose, new Vector2(-180, -300), new Vector2(380, 110), 34);
        var setMenu = IconButton(setCard.transform, "В меню", "GREY", IconHome, new Vector2(220, -300), new Vector2(380, 110), 34);
        SetRef(settings, "_music", musicSlider);
        SetRef(settings, "_sfx", sfxSlider);
        SetRef(settings, "_slideRect", setCard.GetComponent<RectTransform>());
        Wire(setClose, settings.OnClose); Wire(setMenu, settings.OnMenu);
        refs.settings = settings;

        // ---------- SHOP (IAP) / CASES / LEVEL SELECT ----------
        refs.shop = BuildStubOverlay<ShopUI>(root, "Shop", "МАГАЗИН",
            new[] { "Стартовые бонусы", "Скины", "Кейсы", "Отключить рекламу" },
            new[] { IconGift, IconShield, IconChest, IconClose });
        refs.cases = BuildStubOverlay<CasesUI>(root, "Cases", "КЕЙСЫ",
            new[] { "Открыть за монеты", "Открыть за кристаллы" },
            new[] { IconChestCoin, IconChestGem });
        refs.levelSelect = BuildLevelSelect(root);

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

    // Привязка элемента к краю экрана (адаптивно к разрешению/аспекту).
    private static void AnchorRT(Transform t, Vector2 anchor, Vector2 pos)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
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

    // Квадратная кнопка для бокового меню: иконка сверху, подпись снизу.
    private static Button SquareButton(Transform parent, string caption, string colorName, string iconPath, Vector2 pos, float size)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        var sp = LoadSprite(MiniBtn + colorName + ".png");
        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.25f, 0.5f, 0.85f);
        var btn = go.AddComponent<Button>();

        if (!string.IsNullOrEmpty(iconPath))
            Icon(go.transform, iconPath, new Vector2(0, size * 0.14f), new Vector2(size * 0.5f, size * 0.5f), new Vector2(0.5f, 0.5f));
        var label = Label(go.transform, caption, new Vector2(0, -size * 0.32f), new Vector2(size - 8, 52), 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        label.color = new Color(0.15f, 0.12f, 0.05f);
        return btn;
    }

    // Каркас оверлея с выезжающей карточкой: заголовок + строки-кнопки с иконками + Закрыть/В меню.
    private static T BuildStubOverlay<T>(Transform root, string name, string title, string[] lines, string[] icons) where T : UIPanel
    {
        var overlay = Panel(root, name, new Color(0, 0, 0, 0.6f));
        var comp = overlay.AddComponent<T>();
        SetRef(comp, "_root", overlay);
        var card = SpritePanel(overlay.transform, "Card", PanelDark, new Color(0.14f, 0.16f, 0.26f, 1f));
        Card(card, new Vector2(900, 1300));
        Label(card.transform, title, new Vector2(0, 560), new Vector2(820, 100), 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        for (int i = 0; i < lines.Length; i++)
        {
            string ic = (icons != null && i < icons.Length) ? icons[i] : null;
            IconButton(card.transform, lines[i], "BLUE", ic, new Vector2(0, 360 - i * 150), new Vector2(740, 120), 32); // заглушки
        }
        var close = IconButton(card.transform, "Закрыть", "RED", IconClose, new Vector2(-200, -560), new Vector2(400, 110), 34);
        var toMenu = IconButton(card.transform, "В меню", "GREY", IconHome, new Vector2(220, -560), new Vector2(400, 110), 34);
        SetRef(comp, "_slideRect", card.GetComponent<RectTransform>());
        Wire(close, comp.OnClose); Wire(toMenu, comp.OnMenu);
        overlay.SetActive(false);
        return comp;
    }

    // Экран выбора эпохи: 4 эпохи, замок на закрытых, попап с условием разблокировки.
    private static LevelSelectUI BuildLevelSelect(Transform root)
    {
        var overlay = Panel(root, "LevelSelect", new Color(0, 0, 0, 0.6f));
        var comp = overlay.AddComponent<LevelSelectUI>();
        SetRef(comp, "_root", overlay);
        var card = SpritePanel(overlay.transform, "Card", PanelDark, new Color(0.14f, 0.16f, 0.26f, 1f));
        Card(card, new Vector2(980, 1400));
        Label(card.transform, "ВЫБОР ЭПОХИ", new Vector2(0, 600), new Vector2(820, 100), 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

        string[] epochs = { "Первобытность", "Средневековье", "Пороховая эпоха", "Вторая мировая" };
        string[] colors = { "GREEN", "BLUE", "YELLOW", "RED" };
        var btns = new Button[4];
        var locks = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            btns[i] = IconButton(card.transform, $"Эпоха {i + 1}: {epochs[i]}", colors[i], IconMap, new Vector2(0, 380 - i * 175), new Vector2(820, 150), 30);
            locks[i] = Icon(btns[i].transform, IconLock, new Vector2(330, 0), new Vector2(74, 74), new Vector2(0.5f, 0.5f)).gameObject;
        }
        Wire(btns[0], comp.OnEpoch1); Wire(btns[1], comp.OnEpoch2); Wire(btns[2], comp.OnEpoch3); Wire(btns[3], comp.OnEpoch4);

        var close = IconButton(card.transform, "Закрыть", "RED", IconClose, new Vector2(-200, -580), new Vector2(400, 110), 34);
        var toMenu = IconButton(card.transform, "В меню", "GREY", IconHome, new Vector2(220, -580), new Vector2(400, 110), 34);
        Wire(close, comp.OnClose); Wire(toMenu, comp.OnMenu);

        // Попап-подсказка по замку (перекрывает карточку)
        var popup = Panel(card.transform, "LockPopup", new Color(0.05f, 0.06f, 0.1f, 0.92f));
        var popupText = Label(popup.transform, "", new Vector2(0, 90), new Vector2(820, 320), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var ok = IconButton(popup.transform, "Понятно", "GREEN", null, new Vector2(0, -180), new Vector2(380, 120), 36);
        Wire(ok, comp.OnLockOk);
        popup.SetActive(false);

        SetRefArray(comp, "_lockIcons", locks);
        SetRef(comp, "_lockPopup", popup);
        SetRef(comp, "_lockPopupText", popupText);
        SetRef(comp, "_slideRect", card.GetComponent<RectTransform>());
        overlay.SetActive(false);
        return comp;
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
