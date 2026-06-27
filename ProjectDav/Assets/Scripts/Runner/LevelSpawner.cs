using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Генерирует длинный уровень: пары ворот (±×÷), боковые бустеры, подарки,
    // толпы врагов, мини-босс в середине и большой босс в конце.
    public class LevelSpawner : MonoBehaviour
    {
        private enum EvType { GatePair, Crowd, MiniBoss, BigBoss, WeaponPickup, BoosterRow, Gift }
        private struct Ev { public float z; public EvType type; }

        [Header("Prefabs")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private EnemyController _enemyPrefab;
        [SerializeField] private Booster _boosterPrefab;
        [SerializeField] private GiftBox _giftPrefab;

        [Header("Refs")]
        [SerializeField] private Transform _squad;

        [Header("Layout (длинный уровень)")]
        [SerializeField] private int _segmentsBase = 70;
        [SerializeField] private float _startZ = 14f;
        [SerializeField] private float _segment = 13f;
        [SerializeField] private float _laneX = 2.3f;
        [SerializeField] private float _sideX = 5.2f;
        [SerializeField] private float _spawnAhead = 40f;

        [Header("Боковые бонусы (две боковые дорожки)")]
        [SerializeField] private float _sideLaneX = 6f;      // x двух боковых дорожек
        [SerializeField] private float _boosterGap = 3.5f;   // дистанция между бонусами в очереди
        [SerializeField] private float _boosterHp = 14f;
        [SerializeField] private float _boosterSpeed = 3.5f;
        [SerializeField] private float _boosterStop = 6f;    // дистанция остановки перед игроком

        [Header("Difficulty")]
        [SerializeField] private float _crowdHpPerUnit = 4f;
        [SerializeField] private int _crowdBase = 18;
        [SerializeField] private int _crowdMaxPerWave = 45;
        [SerializeField] private float _enemySpeed = 2.6f;
        [SerializeField] private float _enemyStop = 2.2f;        // дистанция остановки перед отрядом
        [SerializeField] private int _enemyContactDamage = 1;
        [SerializeField] private float _enemyHitInterval = 1.5f;

        [Header("Colors")]
        [SerializeField] private Color _good = new Color(0.3f, 0.8f, 0.4f);
        [SerializeField] private Color _good2 = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _bad = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color _gold = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color _enemyColor = new Color(0.8f, 0.25f, 0.25f);
        [SerializeField] private Color _bossColor = new Color(0.55f, 0.15f, 0.6f);

        private readonly List<Ev> _events = new List<Ev>();
        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private int _index, _level;
        private float _totalLength = 1f;
        private bool _bossDefeated, _allSpawned, _running;
        private EnemyController _bigBoss;

        public float Progress01
        {
            get
            {
                if (_squad == null || _totalLength <= 0f) return 0f;
                return Mathf.Clamp01(_squad.position.z / _totalLength);
            }
        }

        public void BeginLevel(int level)
        {
            _level = Mathf.Max(1, level);
            _events.Clear();
            ClearAllEnemies();
            _index = 0; _bossDefeated = false; _allSpawned = false; _bigBoss = null;

            int segments = _segmentsBase + _level * 6;
            int mid = segments / 2;
            float z = _startZ;

            for (int i = 0; i < segments; i++)
            {
                EvType t;
                if (i == mid) t = EvType.MiniBoss;
                else if (i > 0 && i % 9 == 0) t = EvType.WeaponPickup; // оружие оставляем
                else t = EvType.Crowd;                                  // всё остальное — орды
                _events.Add(new Ev { z = z, type = t });

                // бонусы едут по боковым дорожкам (часто, по обоим краям)
                if (i > 1 && i % 3 == 0) _events.Add(new Ev { z = z + _segment * 0.3f, type = EvType.BoosterRow });

                z += _segment;
            }
            z += _segment;
            _events.Add(new Ev { z = z, type = EvType.BigBoss });

            _totalLength = z + 6f;
            _running = true;
        }

        private void Update()
        {
            if (!_running || _squad == null) return;

            while (_index < _events.Count && _squad.position.z >= _events[_index].z - _spawnAhead)
            {
                Spawn(_events[_index]);
                _index++;
            }
            if (_index >= _events.Count) _allSpawned = true;

            if (_allSpawned && _bossDefeated && _alive.Count == 0)
            {
                _running = false;
                AudioController.Instance?.PlayWin();
                RunnerGameManager.Instance?.OnLevelCleared();
            }
        }

        private void Spawn(Ev ev)
        {
            switch (ev.type)
            {
                case EvType.GatePair: SpawnGatePair(ev.z); break;
                case EvType.Crowd: SpawnCrowd(ev.z); break;
                case EvType.WeaponPickup: SpawnWeaponPickup(ev.z); break;
                case EvType.MiniBoss: SpawnBoss(ev.z, false); break;
                case EvType.BigBoss: SpawnBoss(ev.z, true); break;
                case EvType.BoosterRow: SpawnBoosterRow(ev.z); break;
            }
        }

        private void SpawnGatePair(float z)
        {
            if (_gatePrefab == null) return;
            var left = Instantiate(_gatePrefab, new Vector3(-_laneX, 0f, z), Quaternion.identity);
            var right = Instantiate(_gatePrefab, new Vector3(_laneX, 0f, z), Quaternion.identity);

            // одна сторона хорошая, другая может быть хуже/плохой
            ConfigGate(left, true);
            ConfigGate(right, Random.value < 0.5f);
            left.SetPair(right); right.SetPair(left);
        }

        private void ConfigGate(Gate g, bool good)
        {
            if (good)
            {
                if (Random.value < 0.4f) g.Init(GateOp.Multiply, Random.Range(2, 4), _good2);
                else g.Init(GateOp.Add, Random.Range(8, 20 + _level * 2), _good);
            }
            else
            {
                if (Random.value < 0.5f) g.Init(GateOp.Subtract, Random.Range(8, 18), _bad);
                else g.Init(GateOp.Divide, 2, _bad);
            }
        }

        private void SpawnWeaponPickup(float z)
        {
            if (_gatePrefab == null) return;
            var g = Instantiate(_gatePrefab, new Vector3(0f, 0f, z), Quaternion.identity);
            g.InitWeaponPickup(_gold);
        }

        private void SpawnCrowd(float z)
        {
            if (_enemyPrefab == null) return;
            int count = _crowdBase + _level * 2 + Random.Range(0, 5 + _level);
            count = Mathf.Clamp(count, 4, _crowdMaxPerWave);

            // Орда: множество отдельных врагов рядами поперёк дороги, каждый = 1 юнит.
            const int perRow = 5;
            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                int rowCount = Mathf.Min(perRow, count - row * perRow);
                float x = (col - (rowCount - 1) * 0.5f) * 0.9f + Random.Range(-0.12f, 0.12f);
                float ez = z + row * 1.1f + Random.Range(-0.2f, 0.2f);
                var e = Instantiate(_enemyPrefab, new Vector3(x, 0f, ez), Quaternion.identity);
                e.transform.localScale = Vector3.one;
                e.InitCrowd(this, 1, 1 + _level, _enemySpeed, _crowdHpPerUnit,
                            _enemyContactDamage, _enemyHitInterval, _enemyStop, _enemyColor);
                _alive.Add(e);
            }
        }

        private void SpawnBoss(float z, bool big)
        {
            if (_enemyPrefab == null) return;
            float hp = (big ? 400f : 140f) * (1f + _level * 0.4f);
            int bonus = big ? Random.Range(30, 51) : Random.Range(15, 26);
            int contactDmg = big ? 6 + _level : 4 + _level / 2;
            var boss = Instantiate(_enemyPrefab, new Vector3(0f, 0f, z), Quaternion.identity);
            boss.transform.localScale = Vector3.one * (big ? 2.8f : 1.9f);
            boss.InitBoss(this, hp, (1 + _level) * (big ? 5 : 3), _enemySpeed * 0.6f, bonus, contactDmg, big ? 0.5f : 0.6f, _bossColor);
            _alive.Add(boss);
            if (big) _bigBoss = boss;
        }

        // Бонусы едут по ОБЕИМ боковым дорожкам и встают в очередь перед игроком.
        // Игрок не успеет собрать всё — приходится выбирать между бонусами и ордой.
        private void SpawnBoosterRow(float z)
        {
            if (_boosterPrefab == null) return;
            SpawnBoosterLane(-_sideLaneX, z);
            SpawnBoosterLane(_sideLaneX, z);
        }

        private void SpawnBoosterLane(float x, float z)
        {
            int n = Random.Range(3, 6);
            float hp = _boosterHp + _level * 2f;
            for (int i = 0; i < n; i++)
            {
                var b = Instantiate(_boosterPrefab, new Vector3(x, 0f, z + i * _boosterGap), Quaternion.identity);
                // чем дальше в ряду — тем дальше встаёт, чтобы выстроиться друг за другом
                b.Init(Random.Range(4, 10), hp, _boosterSpeed, _boosterStop + i * _boosterGap);
            }
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
