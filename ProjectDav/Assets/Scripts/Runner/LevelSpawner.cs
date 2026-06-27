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

        [Header("Поток врагов (рандомный)")]
        [SerializeField] private int _enemyTotalBase = 50;     // всего врагов за уровень
        [SerializeField] private int _enemyTotalPerLevel = 20;
        [SerializeField] private float _spawnMin = 0.35f;      // мин/макс пауза между подспавнами
        [SerializeField] private float _spawnMax = 1.1f;
        [SerializeField] private int _spawnGroupMax = 4;       // сколько врагов за один подспавн
        [SerializeField] private float _spawnDistance = 45f;   // где появляются впереди
        [SerializeField] private float _laneHalfWidth = 6f;    // ширина центральной дорожки

        [Header("Орда")]
        [SerializeField] private float _crowdHpPerUnit = 4f;
        [SerializeField] private float _enemySpeed = 2.6f;
        [SerializeField] private float _enemyStop = 2.2f;        // встаёт перед отрядом
        [SerializeField] private float _enemyHomingDist = 6f;    // с какой дистанции наводится на отряд
        [SerializeField] private int _enemyContactDamage = 1;
        [SerializeField] private float _enemyHitInterval = 1.5f;

        [Header("Бонусы (боковые дорожки, по одному)")]
        [SerializeField] private float _sideLaneX = 9f;
        [SerializeField] private float _boosterInterval = 2.0f; // как часто выезжает новый блок
        [SerializeField] private float _boosterGap = 2.2f;      // дистанция в очереди
        [SerializeField] private float _boosterHp = 14f;
        [SerializeField] private float _boosterSpeed = 3.5f;
        [SerializeField] private float _boosterStop = 5f;       // где встаёт головной блок

        [Header("Colors")]
        [SerializeField] private Color _enemyColor = new Color(0.8f, 0.25f, 0.25f);
        [SerializeField] private Color _bossColor = new Color(0.55f, 0.15f, 0.6f);

        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private int _enemyTotal, _spawnedCount, _level;
        private float _spawnTimer, _boosterTimer;
        private bool _boosterLeft, _miniBossDone, _bossSpawned, _bossDefeated, _running;
        private EnemyController _bigBoss;

        // Прогресс уровня = доля выпущенных врагов (а не пройденная дистанция).
        public float SpawnProgress01 => _enemyTotal > 0 ? Mathf.Clamp01((float)_spawnedCount / _enemyTotal) : 0f;

        public void BeginLevel(int level)
        {
            _level = Mathf.Max(1, level);
            ClearAllEnemies();
            _enemyTotal = _enemyTotalBase + _level * _enemyTotalPerLevel;
            _spawnedCount = 0;
            _spawnTimer = 0.8f;
            _boosterTimer = 0.5f;
            _miniBossDone = _bossSpawned = _bossDefeated = false;
            _bigBoss = null;
            _running = true;
        }

        private void Update()
        {
            if (!_running || _squad == null) return;
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;

            // рандомный капельный поток врагов
            if (_spawnedCount < _enemyTotal)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = Random.Range(_spawnMin, _spawnMax);
                    SpawnTrickle();
                    if (!_miniBossDone && _spawnedCount >= _enemyTotal / 2) { _miniBossDone = true; SpawnBoss(false); }
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
            int n = Mathf.Min(Random.Range(1, _spawnGroupMax + 1), _enemyTotal - _spawnedCount);
            for (int k = 0; k < n; k++)
            {
                float x = Random.Range(-_laneHalfWidth, _laneHalfWidth);
                float z = _squad.position.z + _spawnDistance + Random.Range(-3f, 6f);
                var e = Instantiate(_enemyPrefab, new Vector3(x, 0f, z), Quaternion.identity);
                e.transform.localScale = Vector3.one;
                e.InitCrowd(this, 1, 1 + _level, _enemySpeed, _crowdHpPerUnit,
                            _enemyContactDamage, _enemyHitInterval, _enemyStop, _enemyHomingDist, _enemyColor);
                _alive.Add(e);
                _spawnedCount++;
            }
        }

        private void SpawnBoss(bool big)
        {
            if (_enemyPrefab == null) return;
            float hp = (big ? 400f : 140f) * (1f + _level * 0.4f);
            int bonus = big ? Random.Range(30, 51) : Random.Range(15, 26);
            int contactDmg = big ? 6 + _level : 4 + _level / 2;
            float z = _squad.position.z + _spawnDistance;
            var boss = Instantiate(_enemyPrefab, new Vector3(0f, 0f, z), Quaternion.identity);
            boss.transform.localScale = Vector3.one * (big ? 2.8f : 1.9f);
            boss.InitBoss(this, hp, (1 + _level) * (big ? 5 : 3), _enemySpeed * 0.7f, bonus, contactDmg, big ? 0.5f : 0.6f, _enemyHomingDist, _bossColor);
            _alive.Add(boss);
            if (big) _bigBoss = boss;
        }

        // Левая дорожка — только оружие, правая — только +юниты.
        private void SpawnBoosterBlock(float x)
        {
            if (_boosterPrefab == null) return;
            float z = _squad.position.z + _spawnDistance;
            var b = Instantiate(_boosterPrefab, new Vector3(x, 0f, z), Quaternion.identity);
            float hp = _boosterHp + _level * 2f;
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
