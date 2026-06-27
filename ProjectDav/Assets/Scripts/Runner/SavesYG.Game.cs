namespace YG
{
    // Поля облачного сохранения Crowd Runner. Дополняют partial-класс SavesYG из SDK.
    public partial class SavesYG
    {
        // --- Экономика ---
        public int coins = 0;
        public int crystals = 0;

        // --- Прогрессия уровней (циклично, сложность растёт) ---
        public int level = 1;       // текущий уровень (1-based)
        public int maxLevel = 1;    // максимально достигнутый

        // --- Мета-улучшения ---
        public int upgDamage = 0;
        public int upgStartUnits = 0;
        public int upgFireRate = 0;
        public int upgVolley = 0;

        public int startWeapon = 0; // выбранный стартовый WeaponType (по умолчанию Melee)

        // --- Настройки ---
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool adsDisabled = false;
        public bool tutorialDone = false;
    }
}
