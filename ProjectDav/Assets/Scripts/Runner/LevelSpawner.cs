using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Генерирует длинный уровень: пары ворот (±×÷), ОРДЫ отдельных монстров,
    // редкие подъезжающие бонусы, мини-босс в середине и большой босс в конце.
    public class LevelSpawner : MonoBehaviour
    {
        private enum EvType { GatePair, Crowd, MiniBoss, BigBoss, Bonus }
        private struct Ev { public float z; public EvType type; }

        [Header("Prefabs")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private EnemyController _enemyPrefab;
        [SerializeField] private BonusPickup _bonusPrefab;

        [Header("Refs")]
        [SerializeField] private Transform _squad;

        [Header("Layout (длинный уровень)")]
        [SerializeField] private int _segmentsBase = 70;
        [SerializeField] private float _startZ = 14f;
        [SerializeField] private float _segment = 13f;
        [SerializeField] private float _laneX = 2.3f;
        [SerializeField] private float _roadHalf = 3.4f;
        [SerializeField] private float _spawnAhead = 40f;

        [Header("Орда (поток монстров)")]
        [SerializeField] private float _crowdHpPerUnit = 3f;
        [SerializeField] private int _crowdBase = 14;
        [SerializeField] private int _crowdMax = 44;
        [SerializeField] private int _rowSize = 6;
        [SerializeField] private float _enemySpeed = 1.6f;

        [Header("Бонусы (редкие, п.3)")]
        [SerializeField] private int _bonusEvery = 13;

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
                else t = (i % 2 == 0) ? EvType.GatePair : EvType.Crowd;
                _events.Add(new Ev { z = z, type = t });

                // редкие бонусы (подъезжают сбоку, встают в очередь)
                if (i > 0 && i % _bonusEvery == (_bonusEvery / 2))
                    _events.Add(new Ev { z = z + _segment * 0.5f, type = EvType.Bonus });

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
                case EvType.MiniBoss: SpawnBoss(ev.z, false); break;
                case EvType.BigBoss: SpawnBoss(ev.z, true); break;
                case EvType.Bonus: SpawnBonus(ev.z); break;
            }
        }

        private void SpawnGatePair(float z)
        {
            if (_gatePrefab == null) return;
            var left = Instantiate(_gatePrefab, new Vector3(-_laneX, 0f, z), Quaternion.identity);
            var right = Instantiate(_gatePrefab, new Vector3(_laneX, 0f, z), Quaternion.identity);
            ConfigGate(left, true);
            ConfigGate(right, Random.value < 0.5f);
            left.SetPair(right); right.SetPair(left);
        }

        // п.1 — рост отряда замедлен: значения ворот скромные.
        private void ConfigGate(Gate g, bool good)
        {
            if (good)
            {
                if (Random.value < 0.15f) g.Init(GateOp.Multiply, 2, _good2);
                else g.Init(GateOp.Add, Random.Range(2, 7), _good);
            }
            else
            {
                if (Random.value < 0.5f) g.Init(GateOp.Subtract, Random.Range(6, 14), _bad);
                else g.Init(GateOp.Divide, 2, _bad);
            }
        }

        // п.2 — толпа = множество отдельных бегущих монстров (орда).
        private void SpawnCrowd(float z)
        {
            if (_enemyPrefab == null) return;
            int count = Mathf.Min(_crowdMax, _crowdBase + _level * 5 + Random.Range(0, 8));
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-_roadHalf, _roadHalf);
                float dz = (i / _rowSize) * 1.3f + Random.Range(0f, 0.5f);
                var e = Instantiate(_enemyPrefab, new Vector3(x, 0f, z + dz), Quaternion.Euler(0f, 180f, 0f));
                e.transform.localScale = Vector3.one * 0.85f;
                e.InitCrowd(this, 1, 1 + _level, _enemySpeed, _crowdHpPerUnit, _enemyColor, false);
                _alive.Add(e);
            }
        }

        private void SpawnBoss(float z, bool big)
        {
            if (_enemyPrefab == null) return;
            float hp = (big ? 400f : 140f) * (1f + _level * 0.4f);
            int bonus = big ? Random.Range(10, 21) : Random.Range(6, 13);
            int contactDmg = big ? 6 + _level : 4 + _level / 2;
            var boss = Instantiate(_enemyPrefab, new Vector3(0f, 0f, z), Quaternion.Euler(0f, 180f, 0f));
            boss.transform.localScale = Vector3.one * (big ? 2.8f : 1.9f);
            boss.InitBoss(this, hp, (1 + _level) * (big ? 5 : 3), _enemySpeed * 0.7f, bonus, contactDmg, big ? 0.5f : 0.6f, _bossColor);
            _alive.Add(boss);
            if (big) _bigBoss = boss;
        }

        // п.4 — бонусы подъезжают сбоку и встают очередью перед отрядом (обе стороны).
        private void SpawnBonus(float z)
        {
            if (_bonusPrefab == null) return;
            bool weapon = Random.value < 0.3f;
            bool leftSide = Random.value < 0.5f;
            float sx = leftSide ? -_roadHalf : _roadHalf;
            int n = weapon ? 1 : Random.Range(1, 3);
            for (int i = 0; i < n; i++)
            {
                var bp = Instantiate(_bonusPrefab, new Vector3(sx, 0.4f, z + i * 3f), Quaternion.identity);
                if (weapon) bp.InitWeapon(_gold);
                else bp.InitUnits(Random.Range(3, 7), _good);
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
