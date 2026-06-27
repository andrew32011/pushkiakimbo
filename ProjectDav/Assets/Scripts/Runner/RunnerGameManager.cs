using System;
using UnityEngine;
using YG;

namespace CrowdRunner
{
    // Центральный хаб игры: состояние, экономика, апгрейды, поток забега, мост к Yandex SDK.
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
        [SerializeField] private GameOverUI _gameOverUI;
        [SerializeField] private WinUI _winUI;
        [SerializeField] private UpgradeUI _upgradeUI;
        [SerializeField] private SettingsUI _settingsUI;

        [Header("Squad base params")]
        [SerializeField] private int _baseStartUnits = 2;
        [SerializeField] private float _baseDamage = 1f;
        [SerializeField] private float _damagePerLevel = 1f;
        [SerializeField] private float _baseFireInterval = 0.5f;
        [SerializeField] private float _fireRateStep = 0.12f;

        [Header("Upgrade economy")]
        [SerializeField] private int[] _upgBaseCost = { 50, 40, 60, 80 }; // по UpgradeType
        [SerializeField] private int _upgMaxLevel = 30;

        [Header("Monetization ids")]
        [SerializeField] private string _reviveAdId = "revive";
        [SerializeField] private string _doubleAdId = "double_reward";
        [SerializeField] private string _freeUpgradeAdId = "free_upgrade";

        [Header("Revive")]
        [SerializeField] private int _maxRevives = 3;
        [SerializeField] private int _reviveUnits = 5;

        public GamePhase Phase { get; private set; } = GamePhase.Menu;
        public int CurrentEpoch => Mathf.Clamp(saves.epochSelected, 0, 3);

        private SavesYG saves => YG2.saves;
        private int _runScore;       // Σ (уровень убитого врага)
        private int _runKills;
        private int _revivesUsed;
        private int _pendingCoins;   // монеты за текущий забег (начисляются в конце)

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
            // Меню показываем сразу (не блокируем UI ожиданием SDK).
            ShowMenu();
            if (YG2.isSDKEnabled) OnSdkData();
        }

        private void OnSdkData()
        {
            // Данные облака пришли — применяем громкость и обновляем экономику в меню.
            ApplyVolumes();
            if (Phase == GamePhase.Menu) ShowMenu();
            OnEconomyChanged?.Invoke();
        }

        // ---------- Экономика ----------
        public int Coins => saves.coins;
        public int Crystals => saves.crystals;

        public void AddCoins(int amount)
        {
            saves.coins = Mathf.Max(0, saves.coins + amount);
            YG2.SaveProgress();
            OnEconomyChanged?.Invoke();
        }

        public void AddCrystals(int amount)
        {
            saves.crystals = Mathf.Max(0, saves.crystals + amount);
            YG2.SaveProgress();
            OnEconomyChanged?.Invoke();
        }

        public bool TrySpendCoins(int amount)
        {
            if (saves.coins < amount) return false;
            saves.coins -= amount;
            YG2.SaveProgress();
            OnEconomyChanged?.Invoke();
            return true;
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

        public bool IsUpgradeMax(UpgradeType t)
        {
            // StartUnits — без жёсткого максимума (диздок 4.1), остальные ограничены.
            if (t == UpgradeType.StartUnits) return false;
            return GetUpgradeLevel(t) >= _upgMaxLevel;
        }

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
            YG2.SaveProgress();
            OnEconomyChanged?.Invoke();
            return true;
        }

        public void GrantFreeUpgrade()
        {
            // Бесплатный случайный апгрейд за rewarded (диздок 6.1).
            YG2.RewardedAdvShow(_freeUpgradeAdId, () =>
            {
                UpgradeType t = (UpgradeType)UnityEngine.Random.Range(0, 4);
                SetUpgradeLevel(t, GetUpgradeLevel(t) + 1);
                YG2.SaveProgress();
                OnEconomyChanged?.Invoke();
            });
        }

        // ---------- Производные параметры отряда ----------
        public int StartUnits => _baseStartUnits + saves.upgStartUnits;
        public float UnitDamage => _baseDamage + saves.upgDamage * _damagePerLevel;
        public float FireInterval => _baseFireInterval / (1f + saves.upgFireRate * _fireRateStep);
        public int Volley => 1 + saves.upgVolley;
        public WeaponType StartWeapon => (WeaponType)Mathf.Clamp(saves.startWeapon, 0, CurrentEpoch);

        // ---------- Поток забега ----------
        public void ShowMenu()
        {
            Phase = GamePhase.Menu;
            Time.timeScale = 1f;
            if (_input != null) _input.Locked = true;
            _menuUI?.Show(true);
            _hudUI?.Show(false);
            _gameOverUI?.Show(false);
            _winUI?.Show(false);
            OnEconomyChanged?.Invoke();
        }

        public void StartRun()
        {
            Phase = GamePhase.Running;
            Time.timeScale = 1f;
            _runScore = 0;
            _runKills = 0;
            _revivesUsed = 0;
            _pendingCoins = 0;

            _menuUI?.Show(false);
            _gameOverUI?.Show(false);
            _winUI?.Show(false);
            _hudUI?.Show(true);

            _squad.Setup(StartUnits, UnitDamage, FireInterval, Volley, StartWeapon);
            _camera?.SnapToTarget();
            _spawner.BeginLevel(CurrentEpoch);
            if (_input != null) _input.Locked = false;

            YG2.GameplayStart();
            _hudUI?.Refresh();
        }

        public void ReportKill(int enemyLevel)
        {
            _runKills++;
            _runScore += Mathf.Max(1, enemyLevel);
            _hudUI?.Refresh();
        }

        public void RefreshHud() => _hudUI?.Refresh();
        public SquadController Squad => _squad;
        public float LevelProgress => _spawner != null ? _spawner.Progress01 : 0f;

        // Вызывается спавнером, когда враги/боссы кончились и босс повержен.
        public void OnLevelCleared()
        {
            if (Phase != GamePhase.Running) return;
            Phase = GamePhase.Win;
            Time.timeScale = 0f;
            if (_input != null) _input.Locked = true;

            // Открыть следующую эпоху.
            int next = CurrentEpoch + 1;
            if (next <= 3 && next > saves.epochUnlocked) saves.epochUnlocked = next;

            _pendingCoins = ComputeRunCoins(true);
            AddCoins(_pendingCoins);
            AddCrystals(UnityEngine.Random.Range(1, 11) + 20); // полное прохождение: +20 (диздок 5.2)
            YG2.GameplayStop();

            _winUI?.Show(true);
            _winUI?.Set(_pendingCoins, _runKills);

            // Межстраничная реклама — только на переходе между уровнями (диздок 6.2).
            YG2.InterstitialAdvShow();
        }

        // Все юниты погибли.
        public void OnSquadWiped()
        {
            if (Phase != GamePhase.Running) return;

            if (_revivesUsed < _maxRevives)
            {
                Phase = GamePhase.GameOver;
                if (_input != null) _input.Locked = true;
                Time.timeScale = 0f;
                _gameOverUI?.Show(true);
                _gameOverUI?.SetCanRevive(true, _revivesUsed, _maxRevives);
            }
            else
            {
                FinishLose();
            }
        }

        public void Revive()
        {
            YG2.RewardedAdvShow(_reviveAdId, () =>
            {
                _revivesUsed++;
                _gameOverUI?.Show(false);
                Phase = GamePhase.Running;
                Time.timeScale = 1f;
                _squad.Revive(_reviveUnits);
                _spawner.ClearNearbyEnemies();
                if (_input != null) _input.Locked = false;
            });
        }

        // Игрок отказался воскрешаться — фиксируем результат.
        public void FinishLose()
        {
            Phase = GamePhase.GameOver;
            Time.timeScale = 0f;
            if (_input != null) _input.Locked = true;
            _pendingCoins = ComputeRunCoins(false);
            AddCoins(_pendingCoins);
            AddCrystals(UnityEngine.Random.Range(1, 11)); // незавершённый уровень: 1-10 (диздок 5.2)
            YG2.GameplayStop();
            _gameOverUI?.SetCanRevive(false, _revivesUsed, _maxRevives);
            _gameOverUI?.SetResult(_pendingCoins, _runKills);
            _gameOverUI?.Show(true);
        }

        public void DoubleReward()
        {
            // Удвоение награды за rewarded (диздок 6.1).
            YG2.RewardedAdvShow(_doubleAdId, () =>
            {
                AddCoins(_pendingCoins);
                _gameOverUI?.SetResult(_pendingCoins * 2, _runKills);
                _winUI?.Set(_pendingCoins * 2, _runKills);
            });
        }

        private int ComputeRunCoins(bool fullClear)
        {
            // (Σ убитых*уровень) * прогресс(1..2) * (полное прохождение: 1 или 5) — диздок 5.1.
            float progress = _spawner != null ? Mathf.Lerp(1f, 2f, _spawner.Progress01) : 1f;
            float completion = fullClear ? 5f : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(_runScore * progress * completion));
        }

        // ---------- Эпохи ----------
        public void SelectEpoch(int epoch)
        {
            saves.epochSelected = Mathf.Clamp(epoch, 0, saves.epochUnlocked);
            YG2.SaveProgress();
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
            if (AudioController.Instance != null)
                AudioController.Instance.SetSfxVolume(saves.sfxVolume);
        }

        // UI-кнопки
        public void OpenUpgrades() => _upgradeUI?.Show(true);
        public void OpenSettings() => _settingsUI?.Show(true);
    }
}
