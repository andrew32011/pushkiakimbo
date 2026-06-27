using UnityEditor;
using UnityEngine;
using CrowdRunner;

// Создаёт/обновляет паки LevelPack из существующих префабов.
// Tools → CrowdRunner → Create Level Packs.
// Без Addressables-API — безопасно для компиляции. Пометку Addressable и loader добавим отдельно.
public static class LevelPackBuilder
{
    private const string ContentFolder = "Assets/Content";
    private const string PackFolder = "Assets/Content/Packs";
    private const string StickmanPath = "Assets/FastMesh/Prefabs/Stickman/Stickman1.prefab";

    private static readonly string[] WeaponPaths =
    {
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)Bat.prefab",
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)Bow.prefab",
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)HuntingRifle.prefab",
        "Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)Rifle.prefab",
    };

    private static readonly string[] EpochNames = { "Первобытность", "Средневековье", "Пороховая эпоха", "Вторая мировая" };
    private static readonly string[] SceneAddr = { "Epoch1_Primitive", "Epoch2_Medieval", "Epoch3_Gunpowder", "Epoch4_WW2" };

    private static readonly Color[] EnemyColors =
    {
        new Color(0.80f, 0.45f, 0.30f), // первобытные
        new Color(0.55f, 0.55f, 0.62f), // рыцари
        new Color(0.45f, 0.30f, 0.25f), // колониальные
        new Color(0.40f, 0.50f, 0.35f), // солдаты
    };
    private static readonly Color[] BossColors =
    {
        new Color(0.6f, 0.35f, 0.2f),
        new Color(0.35f, 0.35f, 0.5f),
        new Color(0.5f, 0.2f, 0.25f),
        new Color(0.3f, 0.4f, 0.3f),
    };

    [MenuItem("Tools/CrowdRunner/Create Level Packs")]
    public static void CreatePacks()
    {
        if (!AssetDatabase.IsValidFolder(ContentFolder)) AssetDatabase.CreateFolder("Assets", "Content");
        if (!AssetDatabase.IsValidFolder(PackFolder)) AssetDatabase.CreateFolder(ContentFolder, "Packs");

        var stickman = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPath);
        if (stickman == null) Debug.LogWarning($"[CrowdRunner] Не найден стикмен по пути {StickmanPath}");
        var weapons = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            weapons[i] = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPaths[i]);
            if (weapons[i] == null) Debug.LogWarning($"[CrowdRunner] Не найдено оружие: {WeaponPaths[i]}");
        }

        for (int e = 0; e < 4; e++)
        {
            string path = $"{PackFolder}/Pack_Epoch{e + 1}.asset";
            var pack = AssetDatabase.LoadAssetAtPath<LevelPack>(path);
            bool create = pack == null;
            if (create) pack = ScriptableObject.CreateInstance<LevelPack>();

            pack.levelName = EpochNames[e];
            pack.epoch = e;
            pack.sceneAddress = SceneAddr[e];
            pack.squadUnitModel = stickman;
            pack.weaponModels = (GameObject[])weapons.Clone();
            pack.enemyModels = new[] { stickman };  // пока заглушка-стикмен; позже свои модели орды
            pack.bossModels = new[] { stickman };
            pack.enemyColor = EnemyColors[e];
            pack.bossColor = BossColors[e];

            if (create) AssetDatabase.CreateAsset(pack, path);
            else EditorUtility.SetDirty(pack);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CrowdRunner] Паки уровней созданы/обновлены в {PackFolder}");
    }
}
