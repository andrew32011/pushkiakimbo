using System;
using UnityEngine;
using YG;

namespace CrowdRunner
{
    // Центральный хаб игры: состояние, экономика, уровни, апгрейды, мост к Yandex SDK.
    public class RunnerGameManager : MonoBehaviour
    {
        public static RunnerGameManager Instance { get; private set; }

        [Header("Scene refs")]
        [SerializeField] private SquadController _squad;
        [SerializeField] private LevelSpawner _spawner;
        [SerializeField] private RunnerInput _input;
        [SerializeField] private CameraFollow _camera;

        [Header("UI refs")]
        [SerializeField] private MainMenuUI _menuUI;
        [SerializeField] private HudUI _hudUI;
        [SerializeField] private DefeatUI _defeatUI;
        [SerializeField] private VictoryUI _victoryUI;
        [SerializeField] private UpgradeUI _upgradeUI;
        [SerializeField] private SettingsUI _settingsUI;
        [SerializeField] private CasesUI _casesUI;
        [SerializeField] private LevelSelectUI _levelSelectUI;
        [SerializeField] private IapStubUI _bonusesUI;
        [SerializeField] private IapStubUI _skinsUI;
        [SerializeField] private IapStubUI _adFreeUI;

        [Header("Squad base params")]
        [SerializeField] private int _baseStartUnits = 12;
        [SerializeField] private float _baseDamage = 2f;
        [SerializeField] private float _damagePerLevel = 1f;
        [SerializeField] private float _baseFireInterval = 0.16f;
        [SerializeField] private float _fireRateStep = 0.12f;

        [Header("Upgrade economy")]
        [SerializeField] private int[] _upgBaseCost = { 50, 40, 60, 80 };
        [SerializeField] private int _upgMaxLevel = 30;

        [Header("Monetization ids")]
        [SerializeField] private string _continueAdId = "continue";
        [SerializeField] private string _doubleAdId = "double_reward";
        [SerializeField] private string _freeUpgradeAdId = "free_upgrade";

        [Header("Continue")]
        [SerializeField] private int _maxContinues = 3; // по диздоку: до 3 воскрешений за забег
        [SerializeField] private int _continueUnits = 10;

        [Header("Epochs")]
        [SerializeField] private int _levelsPerEpoch = 5;

        public GamePhase Phase { get; private set; } = GamePhase.Menu;
        public int Level => Mathf.Max(1, saves.level);
        public int CurrentEpoch => (Level - 1) % 4;

        // ---------- Эпохи / выбор уровня ----------
        public const int EpochCount = 4;
        public bool IsEpochUnlocked(int epoch) => epoch <= 0 || saves.maxLevel > epoch * _levelsPerEpoch;
        public int EpochStartLevel(int epoch) => Mathf.Max(1, epoch * _levelsPerEpoch + 1);

        public void SelectEpoch(int epoch)
        {
            if (!IsEpochUnlocked(epoch)) return;
            saves.level = EpochStartLevel(epoch);
            YG2.SaveProgress();
            StartRun();
        }

        private SavesYG saves => YG2.saves;
        private int _runScore, _runKills, _continuesUsed, _pendingCoins;

        public event Action OnEconomyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() { YG2.onGetSDKData += OnSdkData; }
        private void OnDisable() { YG2.onGetSDKData -= OnSdkData; }

        private void Start()
        {
            // Стартуем с главного меню; если SDK уже готов — сразу инициализируемся.
            ShowMenu();
            if (YG2.isSDKEnabled) OnSdkData();
        }

        private void OnSdkData()
        {
            ApplyVolumes();
            if (Phase == GamePhase.Menu) ShowMenu();
            OnEconomyChanged?.Invoke();
        }

        // ---------- Экономика ----------
        public int Coins => saves.coins;
        public int Crystals => saves.crystals;

        public void AddCoins(int amount) { saves.coins = Mathf.Max(0, saves.coins + amount); YG2.SaveProgress(); OnEconomyChanged?.Invoke(); }
        public void AddCrystals(int amount) { saves.crystals = Mathf.Max(0, saves.crystals + amount); YG2.SaveProgress(); OnEconomyChanged?.Invoke(); }
        public bool TrySpendCoins(int amount)
        {
            if (saves.coins < amount) return false;
            saves.coins -= amount; YG2.SaveProgress(); OnEconomyChanged?.Invoke(); return true;
        }

        // ---------- Апгрейды ----------
        public int GetUpgradeLevel(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Damage: return saves.upgDamage;
                case UpgradeType.StartUnits: return saves.upgStartUnits;
                case UpgradeType.FireRate: return saves.upgFireRate;
                case UpgradeType.Volley: return saves.upgVolley;
            }
            return 0;
        }
        private void SetUpgradeLevel(UpgradeType t, int v)
        {
            switch (t)
            {
                case UpgradeType.Damage: saves.upgDamage = v; break;
                case UpgradeType.StartUnits: saves.upgStartUnits = v; break;
                case UpgradeType.FireRate: saves.upgFireRate = v; break;
                case UpgradeType.Volley: saves.upgVolley = v; break;
            }
        }
        public bool IsUpgradeMax(UpgradeType t) => t != UpgradeType.StartUnits && GetUpgradeLevel(t) >= _upgMaxLevel;
        public int GetUpgradeCost(UpgradeType t)
        {
            int lvl = GetUpgradeLevel(t);
            int baseCost = (int)t < _upgBaseCost.Length ? _upgBaseCost[(int)t] : 50;
            return Mathf.RoundToInt(baseCost * (lvl + 1) * (1f + lvl * 0.35f));
        }
        public bool TryBuyUpgrade(UpgradeType t)
        {
            if (IsUpgradeMax(t)) return false;
            int cost = GetUpgradeCost(t);
            if (!TrySpendCoins(cost)) return false;
            SetUpgradeLevel(t, GetUpgradeLevel(t) + 1);
            YG2.SaveProgress(); OnEconomyChanged?.Invoke();
            return true;
        }
        public void GrantFreeUpgrade()
        {
            YG2.RewardedAdvShow(_freeUpgradeAdId, () =>
            {
                SetUpgradeLevel((UpgradeType)UnityEngine.Random.Range(0, 4), GetUpgradeLevel((UpgradeType)UnityEngine.Random.Range(0, 4)) + 1);
                YG2.SaveProgress(); OnEconomyChanged?.Invoke();
            });
        }

        // ---------- Производные параметры ----------
        public int StartUnits => _baseStartUnits + saves.upgStartUnits;
        public float UnitDamage => _baseDamage + saves.upgDamage * _damagePerLevel;
        public float FireInterval => _baseFireInterval / (1f + saves.upgFireRate * _fireRateStep);
        public int Volley => 1 + saves.upgVolley;
        public WeaponType StartWeapon => (WeaponType)Mathf.Clamp(saves.startWeapon, 0, 3);
        public string StartWeaponName => WeaponDisplayName(StartWeapon);

        // Переключение стартового оружия (пока 0..3 в рамках открытой эпохи).
        public void CycleStartWeapon(int dir)
        {
            saves.startWeapon = Mathf.Clamp(saves.startWeapon + dir, 0, 3);
            YG2.SaveProgress();
            OnEconomyChanged?.Invoke();
        }

        public static string WeaponDisplayName(WeaponType w)
        {
            switch (w)
            {
                case WeaponType.Melee: return "Ручное";
                case WeaponType.Bow: return "Лук";
                case WeaponType.Musket: return "Ружьё";
                case WeaponType.Rifle: return "Винтовка";
            }
            return w.ToString();
        }

        // ---------- Поток ----------
        public void ShowMenu()
        {
            Phase = GamePhase.Menu;
            Time.timeScale = 1f;
            //if (_input != null) _input.Locked = true;
            _menuUI?.Show(true);
            _hudUI?.Show(false);
            _defeatUI?.Show(false);
            _victoryUI?.Show(false);
            _upgradeUI?.Show(false);
            _settingsUI?.Show(false);
            _casesUI?.Show(false);
            _levelSelectUI?.Show(false);
            _bonusesUI?.Show(false);
            _skinsUI?.Show(false);
            _adFreeUI?.Show(false);

            // Превью отряда с выбранным оружием за прозрачным меню (отряд стоит — Phase=Menu).
            if (_squad != null)
            {
                _squad.Setup(StartUnits, UnitDamage, FireInterval, Volley, StartWeapon);
                _camera?.SnapToTarget();
            }
            OnEconomyChanged?.Invoke();
        }

        public void StartRun()
        {
            Phase = GamePhase.Running;
            Time.timeScale = 1f;
            _runScore = 0; _runKills = 0; _continuesUsed = 0; _pendingCoins = 0;

            _menuUI?.Show(false);
            _defeatUI?.Show(false);
            _victoryUI?.Show(false);
            _hudUI?.Show(true);

            _squad.Setup(StartUnits, UnitDamage, FireInterval, Volley, StartWeapon);
            _camera?.SnapToTarget();
            _spawner.BeginLevel(Level);
            if (_input != null) _input.Locked = false;

            YG2.GameplayStart();
            _hudUI?.Refresh();
        }

        public void RestartLevel() => StartRun();

        public void NextLevel()
        {
            saves.level = Level + 1;
            if (saves.level > saves.maxLevel) saves.maxLevel = saves.level;
            YG2.SaveProgress();
            StartRun();
        }

        public void ReportKill(int score) { _runKills++; _runScore += Mathf.Max(1, score); _hudUI?.Refresh(); }
        public void RefreshHud() => _hudUI?.Refresh();
        public SquadController Squad => _squad;
        public float LevelProgress => _spawner != null ? _spawner.SpawnProgress01 : 0f;

        public void OnLevelCleared()
        {
            if (Phase != GamePhase.Running) return;
            Phase = GamePhase.Victory;
            Time.timeScale = 0f;
            if (_input != null) _input.Locked = true;
            _squad?.StopRunning();

            int survivors = _squad != null ? _squad.UnitCount : 0;
            _pendingCoins = ComputeRunCoins(true) + survivors;
            AddCoins(_pendingCoins);
            AddCrystals(UnityEngine.Random.Range(1, 11) + 20);

            YG2.GameplayStop();
            _victoryUI?.Show(true);
            _victoryUI?.Set(survivors, _pendingCoins, true);
            YG2.InterstitialAdvShow();
        }

        public void OnSquadWiped()
        {
            if (Phase != GamePhase.Running) return;
            Phase = GamePhase.Defeat;
            Time.timeScale = 0f;
            if (_input != null) _input.Locked = true;

            _pendingCoins = ComputeRunCoins(false);
            AddCoins(_pendingCoins);
            YG2.GameplayStop();

            _defeatUI?.Show(true);
            _defeatUI?.Set(_pendingCoins, _runKills, _continuesUsed < _maxContinues);
        }

        public void ContinueRun()
        {
            YG2.RewardedAdvShow(_continueAdId, () =>
            {
                _continuesUsed++;
                _defeatUI?.Show(false);
                Phase = GamePhase.Running;
                Time.timeScale = 1f;
                _squad.Revive(_continueUnits);
                _spawner.ClearNearbyEnemies();
                YG2.GameplayStart();
                if (_input != null) _input.Locked = false;
            });
        }

        public void DoubleReward()
        {
            YG2.RewardedAdvShow(_doubleAdId, () =>
            {
                AddCoins(_pendingCoins);
                _victoryUI?.Set(_squad != null ? _squad.UnitCount : 0, _pendingCoins * 2, false); // x2 уже получен — прячем
            });
        }

        // Удвоение награды на экране поражения (после исчерпания воскрешений).
        public void DoubleDefeatReward()
        {
            YG2.RewardedAdvShow(_doubleAdId, () =>
            {
                AddCoins(_pendingCoins);
                _defeatUI?.SetDoubled(_pendingCoins * 2);
            });
        }

        private int ComputeRunCoins(bool fullClear)
        {
            float progress = _spawner != null ? Mathf.Lerp(1f, 2f, _spawner.SpawnProgress01) : 1f;
            float completion = fullClear ? 5f : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(_runScore * progress * completion * 0.5f));
        }

        // ---------- Звук / настройки ----------
        public void SetVolumes(float music, float sfx)
        {
            saves.musicVolume = Mathf.Clamp01(music);
            saves.sfxVolume = Mathf.Clamp01(sfx);
            ApplyVolumes();
            YG2.SaveProgress();
        }
        private void ApplyVolumes()
        {
            AudioListener.volume = Mathf.Clamp01(saves.musicVolume);
            if (AudioController.Instance != null) AudioController.Instance.SetSfxVolume(saves.sfxVolume);
        }

        public void OpenUpgrades() { _upgradeUI?.Show(true); PauseForOverlay(true); }
        public void OpenSettings() { _settingsUI?.Show(true); PauseForOverlay(true); }
        public void OpenCases() { _casesUI?.Show(true); PauseForOverlay(true); }
        public void OpenLevelSelect() { _levelSelectUI?.Show(true); PauseForOverlay(true); }
        public void OpenBonuses() { _bonusesUI?.Show(true); PauseForOverlay(true); }
        public void OpenSkins() { _skinsUI?.Show(true); PauseForOverlay(true); }
        public void OpenAdFree() { _adFreeUI?.Show(true); PauseForOverlay(true); }
        public void CloseOverlay() => PauseForOverlay(false);

        // ВРЕМЕННО: сброс прогресса для тестирования.
        public void ResetProgress()
        {
            YG2.SetDefaultSaves();
            YG2.SaveProgress();
            ShowMenu(); // пере-применит превью-отряд и обновит UI под сброшенные параметры
        }

        // Пауза только во время боя; в меню время и так идёт нормально.
        private void PauseForOverlay(bool paused)
        {
            if (Phase == GamePhase.Running) Time.timeScale = paused ? 0f : 1f;
        }
    }
}
