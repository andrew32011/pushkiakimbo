using UnityEngine;

namespace CrowdRunner
{
    // Пак контента эпохи/уровня. Грузится через Addressables по мере прохождения.
    // Сам пак — Addressable-ассет; его прямые ссылки на модели приезжают вместе с его бандлом,
    // поэтому несвязанные паки не тянут друг друга.
    [CreateAssetMenu(fileName = "LevelPack", menuName = "CrowdRunner/Level Pack")]
    public class LevelPack : ScriptableObject
    {
        [Header("Идентификация")]
        public string levelName = "Эпоха";
        public int epoch;
        public string sceneAddress; // Addressable-ключ сцены уровня (грузим аддитивно)

        [Header("Отряд")]
        public GameObject squadUnitModel;
        public GameObject[] weaponModels = new GameObject[4]; // по WeaponType

        [Header("Враги")]
        public GameObject[] enemyModels;
        public GameObject[] bossModels;

        [Header("Палитра (опционально)")]
        public Color enemyColor = new Color(0.8f, 0.25f, 0.25f);
        public Color bossColor = new Color(0.55f, 0.15f, 0.6f);
    }
}
