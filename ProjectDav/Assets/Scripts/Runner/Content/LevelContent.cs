using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CrowdRunner
{
    // Ленивая загрузка паков уровней через Addressables. Паки кэшируются; текущий доступен как Current.
    // Грузим по требованию — меню/эпоха 1 при старте, остальные эпохи при переходе.
    public static class LevelContent
    {
        private static readonly Dictionary<int, LevelPack> _loaded = new Dictionary<int, LevelPack>();

        public static LevelPack Current { get; private set; }
        public static bool IsLoaded(int epoch) => _loaded.ContainsKey(epoch);
        public static LevelPack GetLoaded(int epoch) => _loaded.TryGetValue(epoch, out var p) ? p : null;

        // Загружает пак эпохи (адрес "Pack_Epoch{n}") и вызывает onDone по готовности (или с null при ошибке).
        public static void LoadPack(int epoch, Action<LevelPack> onDone = null)
        {
            if (_loaded.TryGetValue(epoch, out var cached))
            {
                Current = cached;
                onDone?.Invoke(cached);
                return;
            }

            string address = $"Pack_Epoch{epoch + 1}";
            try
            {
                Addressables.LoadAssetAsync<LevelPack>(address).Completed += op =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                    {
                        _loaded[epoch] = op.Result;
                        Current = op.Result;
                        onDone?.Invoke(op.Result);
                    }
                    else
                    {
                        Debug.LogWarning($"[CrowdRunner] Пак '{address}' не загружен (запусти Tools → CrowdRunner → Setup Addressables).");
                        onDone?.Invoke(null);
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CrowdRunner] Addressables ещё не настроены: {ex.Message}");
                onDone?.Invoke(null);
            }
        }
    }
}
