using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Отряд стоит на месте. Волны орд накатывают по центральной дорожке во времени,
    // бонусы (юниты/оружие) по одному выезжают по боковым дорожкам и встают в очередь.
    public class LevelSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private EnemyController _enemyPrefab;
        [SerializeField] private Booster _boosterPrefab;
        [SerializeField] private GiftBox _giftPrefab;

        [Header("Refs")]
        [SerializeField] private Transform _squad;

        [Header("Поток врагов / длина волны")]
        [SerializeField] private int _enemyTotalBase = 150;    // длинная волна (1-й уровень)
        [SerializeField] private int _enemyTotalPerLevel = 90; // + к длине волны за уровень
        [SerializeField] private float _spawnMin = 0.2f;       // мин/макс пауза между подспавнами (плотнее)
        [SerializeField] private float _spawnMax = 0.55f;
        [SerializeField] private int _spawnGroupMax = 7;       // база группы (растёт со временем)
        [SerializeField] private float _spawnDistance = 45f;
        [SerializeField] private float _laneHalfWidth = 6f;

        [Header("Кривая сложности")]
        [SerializeField] private float _levelGrowth = 1.85f;       // геометрический рост между уровнями
        [SerializeField] private float _rampStepTime = 12f;        // период усиления во времени (сек)
        [SerializeField] private float _hpRampStep = 1.6f;         // ×HP мобов за каждый шаг времени (ГЕОМЕТРИЧЕСКИ)
        [SerializeField] private float _densityStepFactor = 0.25f; // +плотность спавна за шаг (линейно)
        [SerializeField] private int _miniBossCount = 3;           // промежуточных боссов за уровень

        [Header("Орда")]
        [SerializeField] private float _crowdHpPerUnit = 12f;   // база HP моба (дальше растёт геометрически во времени)
        [SerializeField] private float _enemySpeed = 3.1f;      // скорость приближения (не трогаем)
        [SerializeField] private float _enemyStop = 2.2f;
        [SerializeField] private float _enemyHomingDist = 6f;
        [SerializeField] private int _enemyContactDamage = 2;   // больнее отъедают отряд
        [SerializeField] private float _enemyHitInterval = 1.1f;

        [Header("Бонусы (боковые дорожки)")]
        [SerializeField] private float _sideLaneX = 9f;
        [SerializeField] private float _boosterInterval = 2.0f;
        [SerializeField] private float _boosterGap = 2.2f;
        [SerializeField] private float _boosterHpBase = 12f;   // HP первого бонуса в уровне
        [SerializeField] private float _boosterHpStep = 7f;    // +HP за каждый следующий бонус (линейно)
        [SerializeField] private float _boosterSpeed = 3.5f;
        [SerializeField] private float _boosterStop = 5f;

        [Header("Colors")]
        [SerializeField] private Color _enemyColor = new Color(0.8f, 0.25f, 0.25f);
        [SerializeField] private Color _bossColor = new Color(0.55f, 0.15f, 0.6f);

        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private int _enemyTotal, _spawnedCount, _level, _miniBossesDone, _boosterIndex;
        private float _spawnTimer, _boosterTimer, _levelTime;
        private bool _boosterLeft, _bossSpawned, _bossDefeated, _running;
        private EnemyController _bigBoss;

        // Прогресс уровня = доля выпущенных врагов (а не пройденная дистанция).
        public float SpawnProgress01 => _enemyTotal > 0 ? Mathf.Clamp01((float)_spawnedCount / _enemyTotal) : 0f;

        // Геометрический рост между уровнями + рост ВО ВРЕМЕНИ внутри уровня.
        private float Diff => Mathf.Pow(_levelGrowth, Mathf.Max(0, _level - 1));
        private int RampSteps => Mathf.FloorToInt(_levelTime / Mathf.Max(1f, _rampStepTime));
        private float HpRamp => Mathf.Pow(_hpRampStep, RampSteps);          // HP — геометрически во времени
        private float DensityRamp => 1f + RampSteps * _densityStepFactor;  // плотность — линейно во времени

        private GameObject _enemyModel, _bossModel; // модели из пака (null = запечённая)

        // Берём палитру и модели орды/боссов из пака уровня. Фолбэк — значения/меши билдера.
        public void ApplyPack(LevelPack pack)
        {
            if (pack == null) return;
            _enemyColor = pack.enemyColor;
            _bossColor = pack.bossColor;
            _enemyModel = (pack.enemyModels != null && pack.enemyModels.Length > 0) ? pack.enemyModels[0] : null;
            _bossModel = (pack.bossModels != null && pack.bossModels.Length > 0) ? pack.bossModels[0] : null;
        }

        public void BeginLevel(int level)
        {
            _level = Mathf.Max(1, level);
            ClearAllEnemies();
            _enemyTotal = _enemyTotalBase + (_level - 1) * _enemyTotalPerLevel;
            _spawnedCount = 0;
            _miniBossesDone = 0;
            _boosterIndex = 0;
            _levelTime = 0f;
            _spawnTimer = 0.8f;
            _boosterTimer = 0.5f;
            _bossSpawned = _bossDefeated = false;
            _bigBoss = null;
            _running = true;
        }

        private void Update()
        {
            if (!_running || _squad == null) return;
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;

            _levelTime += Time.deltaTime; // время в уровне → ступенчатое усиление мобов

            // рандомный капельный поток врагов
            if (_spawnedCount < _enemyTotal)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = Random.Range(_spawnMin, _spawnMax) / DensityRamp; // со временем — чаще
                    SpawnTrickle();
                    // несколько промежуточных боссов за уровень, равномерно по волне
                    int threshold = (_miniBossesDone + 1) * _enemyTotal / (_miniBossCount + 1);
                    if (_miniBossesDone < _miniBossCount && _spawnedCount >= threshold)
                    { _miniBossesDone++; SpawnBoss(false); }
                }
            }
            else if (!_bossSpawned)
            {
                _bossSpawned = true;
                SpawnBoss(true);
            }

            // поток бонусов по краям (по одному, поочерёдно слева/справа)
            _boosterTimer -= Time.deltaTime;
            if (_boosterTimer <= 0f)
            {
                _boosterTimer = _boosterInterval;
                SpawnBoosterBlock(_boosterLeft ? -_sideLaneX : _sideLaneX);
                _boosterLeft = !_boosterLeft;
            }

            bool allSpawned = _spawnedCount >= _enemyTotal && _bossSpawned;
            if (allSpawned && _bossDefeated && _alive.Count == 0)
            {
                _running = false;
                AudioController.Instance?.PlayWin();
                RunnerGameManager.Instance?.OnLevelCleared();
            }
        }

        // Рандомный подспавн небольшой группы врагов по всей ширине дорожки.
        private void SpawnTrickle()
        {
            if (_enemyPrefab == null) return;
            float diff = Diff, hpRamp = HpRamp, density = DensityRamp;
            int groupMax = Mathf.Max(1, Mathf.RoundToInt(_spawnGroupMax * density)); // больше мобов со временем
            int n = Mathf.Min(Random.Range(1, groupMax + 1), _enemyTotal - _spawnedCount);
            float hpPerUnit = _crowdHpPerUnit * diff * hpRamp;                       // HP — геометрически во времени
            int contact = Mathf.Max(1, Mathf.RoundToInt(_enemyContactDamage * diff * (1f + RampSteps * 0.3f)));
            for (int k = 0; k < n; k++)
            {
                float x = Random.Range(-_laneHalfWidth, _laneHalfWidth);
                float z = _squad.position.z + _spawnDistance + Random.Range(-3f, 6f);
                var e = Instantiate(_enemyPrefab, new Vector3(x, 0f, z), Quaternion.identity);
                e.transform.localScale = Vector3.one;
                e.InitCrowd(this, 1, 1 + _level, _enemySpeed, hpPerUnit,
                            contact, _enemyHitInterval, _enemyStop, _enemyHomingDist, _enemyModel, _enemyColor);
                _alive.Add(e);
                _spawnedCount++;
            }
        }

        private void SpawnBoss(bool big)
        {
            if (_enemyPrefab == null) return;
            float hp = (big ? 420f : 150f) * Diff * HpRamp; // боссы тоже тяжелеют во времени (геометрически)
            int bonus = big ? Random.Range(30, 51) : Random.Range(12, 22);
            int contactDmg = Mathf.RoundToInt((big ? 6f : 4f) * Diff) + _level;
            float z = _squad.position.z + _spawnDistance;
            var boss = Instantiate(_enemyPrefab, new Vector3(0f, 0f, z), Quaternion.identity);
            boss.transform.localScale = Vector3.one * (big ? 2.8f : 1.9f);
            boss.InitBoss(this, hp, (1 + _level) * (big ? 5 : 3), _enemySpeed * 0.7f, bonus, contactDmg, big ? 0.5f : 0.6f, _enemyHomingDist, _bossModel, _bossColor);
            _alive.Add(boss);
            if (big) _bigBoss = boss;
        }

        // Левая дорожка — только оружие, правая — только +юниты.
        // Каждый следующий бонус требует больше HP (линейно в сессии), масштаб по уровню.
        private void SpawnBoosterBlock(float x)
        {
            if (_boosterPrefab == null) return;
            float z = _squad.position.z + _spawnDistance;
            var b = Instantiate(_boosterPrefab, new Vector3(x, 0f, z), Quaternion.identity);
            float hp = (_boosterHpBase + _boosterIndex * _boosterHpStep) * Diff;
            _boosterIndex++;
            if (x < 0f) b.InitWeapon(hp, _boosterSpeed, _boosterStop, _boosterGap);
            else b.Init(Random.Range(4, 10), hp, _boosterSpeed, _boosterStop, _boosterGap);
        }

        public void NotifyEnemyRemoved(EnemyController e)
        {
            _alive.Remove(e);
            if (e != null && e == _bigBoss) _bossDefeated = true;
        }

        public void ClearNearbyEnemies()
        {
            float z = _squad != null ? _squad.position.z : 0f;
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var e = _alive[i];
                if (e == null) { _alive.RemoveAt(i); continue; }
                if (e.IsBoss) continue;
                if (Mathf.Abs(e.transform.position.z - z) < 18f) e.Die(false);
            }
        }

        private void ClearAllEnemies()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
                if (_alive[i] != null) Destroy(_alive[i].gameObject);
            _alive.Clear();
        }
    }
}
