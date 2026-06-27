using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

// Создаёт настройки Addressables (если их нет) и помечает паки + сцены адресуемыми.
// Tools → CrowdRunner → Setup Addressables.
public static class AddressablesSetup
{
    private static readonly string[] PackPaths =
    {
        "Assets/Content/Packs/Pack_Epoch1.asset",
        "Assets/Content/Packs/Pack_Epoch2.asset",
        "Assets/Content/Packs/Pack_Epoch3.asset",
        "Assets/Content/Packs/Pack_Epoch4.asset",
    };
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Levels/Epoch1_Primitive.unity",
        "Assets/Scenes/Levels/Epoch2_Medieval.unity",
        "Assets/Scenes/Levels/Epoch3_Gunpowder.unity",
        "Assets/Scenes/Levels/Epoch4_WW2.unity",
    };

    [MenuItem("Tools/CrowdRunner/Setup Addressables")]
    public static void Setup()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true); // создаст дефолтные, если нет
        if (settings == null) { Debug.LogError("[CrowdRunner] Не удалось получить настройки Addressables."); return; }

        // Каждый пак/сцена — отдельным бандлом, чтобы паки были независимы и грузились по требованию.
        var schema = settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>();
        if (schema != null) schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

        for (int i = 0; i < PackPaths.Length; i++)
            Mark(settings, PackPaths[i], $"Pack_Epoch{i + 1}");
        for (int i = 0; i < ScenePaths.Length; i++)
            Mark(settings, ScenePaths[i], System.IO.Path.GetFileNameWithoutExtension(ScenePaths[i]));

        AssetDatabase.SaveAssets();
        Debug.Log("[CrowdRunner] Addressables настроены: 4 пака + 4 сцены помечены адресуемыми (PackSeparately).");
    }

    private static void Mark(AddressableAssetSettings settings, string assetPath, string address)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) { Debug.LogWarning($"[CrowdRunner] Не найден ассет: {assetPath}"); return; }
        var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        entry.address = address;
    }
}
