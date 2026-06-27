using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Генерирует длинный уровень: пары ворот (±×÷), боковые бустеры, подарки,
    // толпы врагов, мини-босс в середине и большой босс в конце.
    public class LevelSpawner : MonoBehaviour
    {
        private enum EvType { GatePair, Crowd, MiniBoss, BigBoss, WeaponPickup, Booster }
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

        [Header("Difficulty")]
        [SerializeField] private float _crowdHpPerUnit = 4f;
        [SerializeField] private int _crowdBase = 8;
        [SerializeField] private float _enemySpeed = 3.5f;

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
                else if (i > 0 && i % 11 == 0) t = EvType.WeaponPickup;
                else t = (i % 2 == 0) ? EvType.GatePair : EvType.Crowd;
                _events.Add(new Ev { z = z, type = t });

                // боковые бустеры периодически (параллельно, не занимают слот пути)
                if (i % 5 == 2) _events.Add(new Ev { z = z + _segment * 0.4f, type = EvType.Booster });

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
                case EvType.Booster: SpawnBoosterAndGift(ev.z); break;
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
            int count = _crowdBase + _level * 3 + Random.Range(0, 6 + _level);
            float x = Random.Range(-1f, 1f);
            var e = Instantiate(_enemyPrefab, new Vector3(x, 0f, z), Quaternion.identity);
            e.transform.localScale = Vector3.one;
            e.InitCrowd(this, count, 1 + _level, _enemySpeed, _crowdHpPerUnit, _enemyColor);
            _alive.Add(e);
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

        private void SpawnBoosterAndGift(float z)
        {
            bool leftSide = Random.value < 0.5f;
            if (_boosterPrefab != null)
            {
                var b = Instantiate(_boosterPrefab, new Vector3(leftSide ? -_sideX : _sideX, 0f, z), Quaternion.identity);
                b.Init(Random.Range(2, 6), 0.5f);
            }
            // иногда подарок на противоположном краю
            if (_giftPrefab != null && Random.value < 0.5f)
            {
                var g = Instantiate(_giftPrefab, new Vector3(leftSide ? _laneX + 1f : -_laneX - 1f, 0.4f, z + 4f), Quaternion.identity);
                g.Init(10, 30);
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
