namespace YG
{
    // Поля облачного сохранения Crowd Runner. Дополняют partial-класс SavesYG из SDK.
    // Значения по умолчанию = состояние новой игры (применяются при первом запуске).
    public partial class SavesYG
    {
        // --- Экономика ---
        public int coins = 0;        // мягкая валюта
        public int crystals = 0;     // твёрдая валюта

        // --- Прогрессия эпох ---
        public int epochUnlocked = 0;   // макс. открытая эпоха (индекс Epoch)
        public int epochSelected = 0;   // выбранная для забега эпоха

        // --- Улучшения (уровни, 0..N) ---
        public int upgDamage = 0;       // +базовый урон
        public int upgStartUnits = 0;   // +стартовые юниты
        public int upgFireRate = 0;     // скорость стрельбы
        public int upgVolley = 0;       // залповость

        public int startWeapon = 0;     // выбранный стартовый WeaponType

        // --- Настройки ---
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool adsDisabled = false;

        public bool tutorialDone = false;
    }
}
