using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Отряд стикменов: бежит вперёд, двигается по X, держит строй, стреляет, принимает урон.
    [RequireComponent(typeof(Rigidbody))]
    public class SquadController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject _unitPrefab;     // модель стикмена (FastMesh)
        [SerializeField] private Transform _formationRoot;   // родитель юнитов (обычно this)
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private BoxCollider _sensor;        // триггер-сенсор спереди
        [SerializeField] private RunnerInput _input;

        [Header("Movement")]
        [SerializeField] private float _runSpeed = 6f;
        [SerializeField] private float _roadHalfWidth = 2.5f;
        [SerializeField] private float _horizontalLerp = 12f;

        [Header("Formation")]
        [SerializeField] private float _spacing = 0.45f;
        [SerializeField] private int _maxUnits = 80;
        [SerializeField] private int _maxFiringUnits = 40;

        [Header("Fire")]
        [SerializeField] private float _projLife = 2f;
        [SerializeField] private float _muzzleHeight = 0.6f;

        private readonly List<UnitController> _units = new List<UnitController>();
        private float _unitDamage = 1f;
        private float _fireInterval = 0.5f;
        private int _volley = 1;
        private WeaponType _weapon = WeaponType.Stick;
        private float _fireTimer;
        private float _targetX;
        private bool _active;

        // визуал/баллистика снаряда по типу оружия
        private float _projSpeed = 14f;
        private float _projScale = 0.18f;
        private Color _projColor = Color.white;
        private bool _projPierce;

        public int UnitCount => _units.Count;
        public float Damage => _unitDamage;
        public WeaponType Weapon => _weapon;

        public void Setup(int count, float damage, float fireInterval, int volley, WeaponType weapon)
        {
            if (_formationRoot == null) _formationRoot = transform;

            // очистить старых
            for (int i = _units.Count - 1; i >= 0; i--)
                if (_units[i] != null) Destroy(_units[i].gameObject);
            _units.Clear();

            _unitDamage = Mathf.Max(1f, damage);
            _fireInterval = Mathf.Max(0.05f, fireInterval);
            _volley = Mathf.Max(1, volley);
            _fireTimer = 0f;
            _targetX = 0f;

            transform.position = new Vector3(0f, transform.position.y, 0f);
            SetWeapon(weapon);
            AddUnits(Mathf.Max(1, count));
            _active = true;
        }

        public void Revive(int units)
        {
            AddUnits(Mathf.Max(1, units));
            _active = true;
        }

        private void Update()
        {
            if (!_active || RunnerGameManager.Instance == null) return;
            if (RunnerGameManager.Instance.Phase != GamePhase.Running) return;

            // бег вперёд
            transform.position += Vector3.forward * (_runSpeed * Time.deltaTime);

            // горизонталь по вводу
            if (_input != null && !_input.Locked)
                _targetX = Mathf.Clamp(_targetX + _input.ConsumeDeltaX(), -_roadHalfWidth, _roadHalfWidth);
            Vector3 p = transform.position;
            p.x = Mathf.Lerp(p.x, _targetX, _horizontalLerp * Time.deltaTime);
            transform.position = p;

            // стрельба
            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = _fireInterval;
                Fire();
            }
        }

        private void Fire()
        {
            if (_projectilePrefab == null || _units.Count == 0) return;
            int firing = Mathf.Min(_units.Count, _maxFiringUnits);
            for (int i = 0; i < firing; i++)
            {
                var u = _units[i];
                if (u == null) continue;
                Vector3 baseMuzzle = u.transform.position + Vector3.up * _muzzleHeight;
                for (int v = 0; v < _volley; v++)
                {
                    float offset = (_volley == 1) ? 0f : (v - (_volley - 1) * 0.5f) * 0.18f;
                    Vector3 spawn = baseMuzzle + Vector3.right * offset;
                    var proj = Instantiate(_projectilePrefab, spawn, Quaternion.identity);
                    proj.transform.localScale = Vector3.one * _projScale;
                    var rend = proj.GetComponentInChildren<Renderer>();
                    if (rend != null && rend.material != null) rend.material.color = _projColor;
                    proj.Launch(_unitDamage, _projSpeed, _projLife, _projPierce);
                }
            }
            AudioController.Instance?.PlayShot();
        }

        // ---------- Ворота ----------
        public void ModifyDamage(GateOp op, int value)
        {
            switch (op)
            {
                case GateOp.Add: _unitDamage += value; break;
                case GateOp.Multiply: _unitDamage *= Mathf.Max(1, value); break;
                case GateOp.Subtract: _unitDamage -= value; break;
            }
            _unitDamage = Mathf.Max(1f, _unitDamage);
            RunnerGameManager.Instance?.RefreshHud();
        }

        public void ModifyUnits(GateOp op, int value)
        {
            int cur = _units.Count;
            int target = cur;
            switch (op)
            {
                case GateOp.Add: target = cur + value; break;
                case GateOp.Multiply: target = cur * Mathf.Max(1, value); break;
                case GateOp.Subtract: target = cur - value; break;
            }
            target = Mathf.Clamp(target, 0, _maxUnits);
            if (target > cur) AddUnits(target - cur);
            else if (target < cur) RemoveUnits(cur - target);
        }

        public void SetWeapon(WeaponType weapon)
        {
            _weapon = weapon;
            switch (weapon)
            {
                case WeaponType.Stick:
                    _projSpeed = 11f; _projScale = 0.22f; _projColor = new Color(0.5f, 0.35f, 0.2f); _projPierce = false; break;
                case WeaponType.Bow:
                    _projSpeed = 16f; _projScale = 0.16f; _projColor = new Color(0.85f, 0.8f, 0.5f); _projPierce = false; break;
                case WeaponType.Musket:
                    _projSpeed = 22f; _projScale = 0.18f; _projColor = new Color(0.7f, 0.7f, 0.75f); _projPierce = false; break;
                case WeaponType.Rifle:
                    _projSpeed = 30f; _projScale = 0.15f; _projColor = new Color(1f, 0.85f, 0.2f); _projPierce = true; break;
            }
            RunnerGameManager.Instance?.RefreshHud();
        }

        // ---------- Юниты ----------
        private void AddUnits(int n)
        {
            for (int i = 0; i < n && _units.Count < _maxUnits; i++)
            {
                GameObject go;
                if (_unitPrefab != null) go = Instantiate(_unitPrefab, _formationRoot);
                else { go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.transform.SetParent(_formationRoot); }
                go.name = "Unit";
                var uc = go.GetComponent<UnitController>();
                if (uc == null) uc = go.AddComponent<UnitController>();
                _units.Add(uc);
            }
            Reformat();
            RunnerGameManager.Instance?.RefreshHud();
        }

        public void RemoveUnits(int n)
        {
            for (int i = 0; i < n && _units.Count > 0; i++)
            {
                int last = _units.Count - 1;
                if (_units[last] != null) Destroy(_units[last].gameObject);
                _units.RemoveAt(last);
            }
            Reformat();
            RunnerGameManager.Instance?.RefreshHud();

            if (_units.Count == 0)
            {
                _active = false;
                AudioController.Instance?.PlayLose();
                RunnerGameManager.Instance?.OnSquadWiped();
            }
        }

        private void Reformat()
        {
            int count = _units.Count;
            if (count == 0) { if (_sensor != null) _sensor.size = new Vector3(0.6f, 1.2f, 0.6f); return; }

            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                float x = (col - (cols - 1) * 0.5f) * _spacing;
                float z = -row * _spacing;
                if (_units[i] != null)
                    _units[i].transform.localPosition = new Vector3(x, 0f, z);
            }

            // сенсор по ширине строя
            if (_sensor != null)
            {
                float width = Mathf.Max(0.8f, cols * _spacing + 0.4f);
                _sensor.size = new Vector3(width, 1.4f, 0.8f);
                _sensor.center = new Vector3(0f, 0.6f, 0.6f);
            }
        }

        // ---------- Столкновения (сенсор) ----------
        private void OnTriggerEnter(Collider other)
        {
            var gate = other.GetComponentInParent<Gate>();
            if (gate != null) { gate.ApplyTo(this); return; }

            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null && !enemy.IsDead)
            {
                int loss = enemy.UnitsOnContact();
                enemy.Die(false); // контактный враг не даёт награды
                AudioController.Instance?.PlayHit();
                RemoveUnits(loss);
            }
        }
    }
}
