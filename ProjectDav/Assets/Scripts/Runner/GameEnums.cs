namespace CrowdRunner
{
    // Визуальная тема секции уровня (диздок п.3).
    public enum Epoch
    {
        Primitive = 0,
        Medieval = 1,
        Gunpowder = 2,
        WorldWar = 3
    }

    // Операция ворот над числом юнитов N.
    public enum GateOp
    {
        Add = 0,        // N += value
        Multiply = 1,   // N *= value
        Subtract = 2,   // N -= value
        Divide = 3      // N = floor(N / value)
    }

    // Класс/тип оружия. Старт — ручное (худшее). Апгрейд через пикапы.
    public enum WeaponType
    {
        Melee = 0,      // ручное (бита/меч) — короткая дистанция, слабый урон
        Bow = 1,        // лук — средняя дистанция
        Musket = 2,     // мушкет/ружьё — большая дистанция
        Rifle = 3       // винтовка/пулемёт — максимум
    }

    // Меню-улучшения (мета-прогрессия).
    public enum UpgradeType
    {
        Damage = 0,
        StartUnits = 1,
        FireRate = 2,
        Volley = 3
    }

    public enum GamePhase
    {
        Menu = 0,
        Running = 1,
        Defeat = 2,
        Victory = 3
    }
}
