using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Генерирует и спавнит наполнение уровня вдоль дороги: пары ворот, волны врагов,
    // мини-босса в середине и большого босса в конце (диздок п.3).
    public class LevelSpawner : MonoBehaviour
    {
        private enum EvType { GatePair, EnemyCluster, MiniBoss, BigBoss }
        private struct Ev { public float z; public EvType type; }

        [Header("Prefabs")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private EnemyController _enemyPrefab;

        [Header("Refs")]
        [SerializeField] private Transform _squad;

        [Header("Layout")]
        [SerializeField] private float _startZ = 12f;       // первое событие
        [SerializeField] private float _segment = 9f;       // шаг между событиями
        [SerializeField] private float _laneX = 1.6f;       // X левой/правой полосы
        [SerializeField] private float _spawnAhead = 28f;   // на сколько вперёд спавнить

        [Header("Difficulty (epoch-scaled)")]
        [SerializeField] private int _segmentsBase = 14;    // событий в эпохе
        [SerializeField] private float _enemyHpBase = 6f;
        [SerializeField] private int _clusterBase = 3;
        [SerializeField] private float _enemySpeedBase = 2.2f;

        [Header("Colors")]
        [SerializeField] private Color _weaponColor = new Color(0.95f, 0.5f, 0.2f);
        [SerializeField] private Color _unitColor = new Color(0.25f, 0.7f, 1f);
        [SerializeField] private Color _enemyColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color _bossColor = new Color(0.5f, 0.1f, 0.5f);

        private readonly List<Ev> _events = new List<Ev>();
        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private int _index;
        private int _epoch;
        private float _totalLength = 1f;
        private bool _bossDefeated;
        private bool _allSpawned;
        private EnemyController _bigBoss;
        private bool _running;

        public float Progress01
        {
            get
            {
                if (_squad == null || _totalLength <= 0f) return 0f;
                return Mathf.Clamp01((_squad.position.z) / _totalLength);
            }
        }

        public void BeginLevel(int epoch)
        {
            _epoch = epoch;
            _events.Clear();
            ClearAllEnemies();
            _index = 0;
            _bossDefeated = false;
            _allSpawned = false;
            _bigBoss = null;

            int segments = _segmentsBase + epoch * 3;
            float z = _startZ;
            int midIndex = segments / 2;

            for (int i = 0; i < segments; i++)
            {
                EvType t;
                if (i == midIndex) t = EvType.MiniBoss;
                else t = (i % 2 == 0) ? EvType.GatePair : EvType.EnemyCluster;
                _events.Add(new Ev { z = z, type = t });
                z += _segment;
            }
            // финальный большой босс
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

            // условие победы
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
                case EvType.EnemyCluster: SpawnCluster(ev.z, _clusterBase + _epoch, 1f); break;
                case EvType.MiniBoss: SpawnBoss(ev.z, false); break;
                case EvType.BigBoss: SpawnBoss(ev.z, true); break;
            }
        }

        private void SpawnGatePair(float z)
        {
            if (_gatePrefab == null) return;

            // Левая полоса — оружие/урон.
            var left = Instantiate(_gatePrefab, new Vector3(-_laneX, 0f, z), Quaternion.identity);
            bool swap = Random.value < 0.3f && _epoch > 0;
            if (swap)
            {
                WeaponType w = (WeaponType)Random.Range(1, _epoch + 2); // доступное по эпохе
                left.Init(GateType.Weapon, GateOp.Add, 0, true, w, _weaponColor);
            }
            else
            {
                bool mult = Random.value < 0.25f;
                left.Init(GateType.Weapon, mult ? GateOp.Multiply : GateOp.Add,
                          mult ? 2 : Random.Range(1, 4 + _epoch), false, WeaponType.Stick, _weaponColor);
            }

            // Правая полоса — пополнение отряда.
            var right = Instantiate(_gatePrefab, new Vector3(_laneX, 0f, z), Quaternion.identity);
            bool rmult = Random.value < 0.3f;
            if (rmult)
                right.Init(GateType.Units, GateOp.Multiply, 2, false, WeaponType.Stick, _unitColor);
            else
                right.Init(GateType.Units, GateOp.Add, Random.Range(3, 8 + _epoch), false, WeaponType.Stick, _unitColor);
        }

        private void SpawnCluster(float z, int count, float strengthScale)
        {
            if (_enemyPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-1.2f, 1.2f, count == 1 ? 0.5f : (float)i / (count - 1));
                float dz = Random.Range(0f, 3f);
                SpawnEnemy(new Vector3(x, 0f, z + dz),
                    hp: _enemyHpBase * (1f + _epoch * 0.6f) * Random.Range(0.8f, 1.3f),
                    level: 1 + _epoch,
                    strength: Mathf.RoundToInt((1 + _epoch) * strengthScale),
                    speed: _enemySpeedBase,
                    isBoss: false,
                    big: false);
            }
        }

        private void SpawnBoss(float z, bool big)
        {
            if (_enemyPrefab == null) return;
            float hpMul = big ? 10f : 3f;       // диздок п.10
            int strength = big ? (8 + _epoch * 4) : (4 + _epoch * 2);
            var boss = SpawnEnemy(new Vector3(0f, 0f, z),
                hp: _enemyHpBase * (1f + _epoch * 0.6f) * hpMul,
                level: (1 + _epoch) * (big ? 5 : 3),
                strength: strength,
                speed: _enemySpeedBase * 0.7f,
                isBoss: true,
                big: big);
            if (big) _bigBoss = boss;
        }

        private EnemyController SpawnEnemy(Vector3 pos, float hp, int level, int strength, float speed, bool isBoss, bool big)
        {
            var e = Instantiate(_enemyPrefab, pos, Quaternion.identity);
            float scale = isBoss ? (big ? 2.6f : 1.8f) : 1f;
            e.transform.localScale = Vector3.one * scale;
            e.Init(this, hp, level, strength, speed, isBoss, isBoss ? _bossColor : _enemyColor);
            _alive.Add(e);
            return e;
        }

        public void NotifyEnemyRemoved(EnemyController e)
        {
            _alive.Remove(e);
            if (e != null && e == _bigBoss) _bossDefeated = true;
        }

        // На воскрешении — расчистить пространство вокруг отряда.
        public void ClearNearbyEnemies()
        {
            float z = _squad != null ? _squad.position.z : 0f;
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var e = _alive[i];
                if (e == null) { _alive.RemoveAt(i); continue; }
                if (e.IsBoss) continue; // боссов не убираем
                if (Mathf.Abs(e.transform.position.z - z) < 14f)
                    e.Die(false);
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
