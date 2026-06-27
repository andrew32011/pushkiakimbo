using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Отряд стикменов: авто-бег вперёд, страйф по X, строй-сетка, стрельба, число над толпой.
    [RequireComponent(typeof(Rigidbody))]
    public class SquadController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private Transform _formationRoot;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private BoxCollider _sensor;
        [SerializeField] private RunnerInput _input;
        [SerializeField] private TextMesh _countText;
        [SerializeField] private GameObject[] _weaponModels = new GameObject[4]; // по WeaponType

        [Header("Movement")]
        [SerializeField] private float _runSpeed = 0f; // отряд стоит на месте и отстреливается
        [SerializeField] private float _roadHalfWidth = 7f; // включает боковые дорожки (бонусы)
        [SerializeField] private float _strafeSpeed = 10f;

        [Header("Formation")]
        [SerializeField] private int _perRow = 5;
        [SerializeField] private float _spacing = 0.55f;
        [SerializeField] private int _maxUnits = 120;
        [SerializeField] private int _maxFiringUnits = 30;

        [Header("Fire")]
        [SerializeField] private float _muzzleHeight = 0.6f;

        private readonly List<UnitController> _units = new List<UnitController>();
        private float _baseDamage = 1f;
        private float _fireInterval = 0.5f;
        private int _volley = 1;
        private WeaponType _weapon = WeaponType.Melee;
        private float _fireTimer;
        private float _targetX;
        private bool _active;
        private bool _running;

        // баллистика по типу оружия
        private float _projSpeed, _projLife, _projScale, _dmgMul;
        private bool _projPierce;
        private Color _projColor = Color.white;

        public int UnitCount => _units.Count;
        public float Damage => _baseDamage * _dmgMul;
        public WeaponType Weapon => _weapon;
        public Vector3 Center => transform.position;

        public void Setup(int count, float damage, float fireInterval, int volley, WeaponType weapon)
        {
            if (_formationRoot == null) _formationRoot = transform;
            for (int i = _units.Count - 1; i >= 0; i--)
                if (_units[i] != null) Destroy(_units[i].gameObject);
            _units.Clear();

            _baseDamage = Mathf.Max(1f, damage);
            _fireInterval = Mathf.Max(0.05f, fireInterval);
            _volley = Mathf.Max(1, volley);
            _fireTimer = 0f;
            _targetX = 0f;

            transform.position = new Vector3(0f, transform.position.y, 0f);
            SetWeapon(weapon);
            AddUnits(Mathf.Max(1, count), false);
            _active = true;
            _running = true;
        }

        public void Revive(int units)
        {
            AddUnits(Mathf.Max(1, units), true);
            _active = true;
            _running = true;
        }

        public void StopRunning() { _running = false; }
        public void ResumeRunning() { _running = true; }

        private void Update()
        {
            if (!_active || RunnerGameManager.Instance == null) return;
            if (RunnerGameManager.Instance.Phase != GamePhase.Running) return;

            if (_running)
                transform.position += Vector3.forward * (_runSpeed * Time.deltaTime);

            if (_input != null && !_input.Locked)
                _targetX = Mathf.Clamp(_targetX + _input.ConsumeDeltaX(), -_roadHalfWidth, _roadHalfWidth);
            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, _targetX, _strafeSpeed * Time.deltaTime);
            transform.position = p;

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f) { _fireTimer = _fireInterval; Fire(); }
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
                    float offset = (_volley == 1) ? 0f : (v - (_volley - 1) * 0.5f) * 0.2f;
                    var proj = Instantiate(_projectilePrefab, baseMuzzle + Vector3.right * offset, Quaternion.identity);
                    proj.transform.localScale = Vector3.one * _projScale;
                    var rend = proj.GetComponentInChildren<Renderer>();
                    if (rend != null && rend.material != null) rend.material.color = _projColor;
                    proj.Launch(Damage, _projSpeed, _projLife, _projPierce, _projColor);
                }
            }
            AudioController.Instance?.PlayShot();
        }

        // ---------- Изменение числа ----------
        public void ModifyUnits(GateOp op, int value)
        {
            int cur = _units.Count;
            int target = cur;
            switch (op)
            {
                case GateOp.Add: target = cur + value; break;
                case GateOp.Multiply: target = cur * Mathf.Max(1, value); break;
                case GateOp.Subtract: target = cur - value; break;
                case GateOp.Divide: target = Mathf.FloorToInt(cur / (float)Mathf.Max(1, value)); break;
            }
            target = Mathf.Clamp(target, 0, _maxUnits);
            int delta = target - cur;
            if (delta > 0) AddUnits(delta, true);
            else if (delta < 0) RemoveUnits(-delta);
        }

        public void SetWeapon(WeaponType weapon)
        {
            _weapon = weapon;
            switch (weapon)
            {
                case WeaponType.Melee:
                    _projSpeed = 13f; _projLife = 1.35f; _projScale = 0.28f; _dmgMul = 1f; _projPierce = false; _projColor = new Color(0.6f, 0.6f, 0.6f); break;
                case WeaponType.Bow:
                    _projSpeed = 22f; _projLife = 3.9f; _projScale = 0.18f; _dmgMul = 1.6f; _projPierce = false; _projColor = new Color(0.85f, 0.75f, 0.45f); break;
                case WeaponType.Musket:
                    _projSpeed = 30f; _projLife = 5.1f; _projScale = 0.2f; _dmgMul = 2.6f; _projPierce = false; _projColor = new Color(0.7f, 0.75f, 0.85f); break;
                case WeaponType.Rifle:
                    _projSpeed = 40f; _projLife = 6.6f; _projScale = 0.16f; _dmgMul = 4f; _projPierce = true; _projColor = new Color(1f, 0.85f, 0.2f); break;
            }
            EquipWeaponModels();
            RunnerGameManager.Instance?.RefreshHud();
        }

        // Апгрейд оружия на ступень выше (пикап золотого оружия).
        public void UpgradeWeapon()
        {
            if ((int)_weapon < 3) SetWeapon((WeaponType)((int)_weapon + 1));
        }

        private void EquipWeaponModels()
        {
            GameObject model = null;
            if (_weaponModels != null && (int)_weapon < _weaponModels.Length) model = _weaponModels[(int)_weapon];
            foreach (var u in _units) if (u != null) u.EquipWeapon(model);
        }

        // ---------- Юниты ----------
        private void AddUnits(int n, bool feedback)
        {
            int before = _units.Count;
            GameObject model = (_weaponModels != null && (int)_weapon < _weaponModels.Length) ? _weaponModels[(int)_weapon] : null;
            for (int i = 0; i < n && _units.Count < _maxUnits; i++)
            {
                GameObject go;
                if (_unitPrefab != null) go = Instantiate(_unitPrefab, _formationRoot);
                else { go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.transform.SetParent(_formationRoot); }
                go.name = "Unit";
                var uc = go.GetComponent<UnitController>();
                if (uc == null) uc = go.AddComponent<UnitController>();
                uc.EquipWeapon(model);
                _units.Add(uc);
            }
            Reformat();
            UpdateCountText();
            int added = _units.Count - before;
            if (feedback && added > 0)
                EffectsManager.Float(transform.position + Vector3.up * 2.5f, "+" + added, new Color(0.4f, 1f, 0.4f));
            RunnerGameManager.Instance?.RefreshHud();
        }

        public void RemoveUnits(int n)
        {
            int before = _units.Count;
            for (int i = 0; i < n && _units.Count > 0; i++)
            {
                int last = _units.Count - 1;
                if (_units[last] != null)
                {
                    EffectsManager.Burst(_units[last].transform.position + Vector3.up * 0.5f, new Color(1f, 0.3f, 0.3f), 0.5f);
                    Destroy(_units[last].gameObject);
                }
                _units.RemoveAt(last);
            }
            Reformat();
            UpdateCountText();
            int removed = before - _units.Count;
            if (removed > 0)
                EffectsManager.Float(transform.position + Vector3.up * 2.5f, "-" + removed, new Color(1f, 0.35f, 0.35f));
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
            if (count == 0) { if (_sensor != null) _sensor.size = new Vector3(0.6f, 1.4f, 0.8f); return; }

            int cols = Mathf.Min(_perRow, count);
            for (int i = 0; i < count; i++)
            {
                int row = i / _perRow;
                int col = i % _perRow;
                int rowCount = Mathf.Min(_perRow, count - row * _perRow);
                float x = (col - (rowCount - 1) * 0.5f) * _spacing;
                float z = -row * _spacing;
                if (_units[i] != null) _units[i].transform.localPosition = new Vector3(x, 0f, z);
            }

            if (_sensor != null)
            {
                float width = Mathf.Max(0.8f, cols * _spacing + 0.4f);
                _sensor.size = new Vector3(width, 1.6f, 0.9f);
                _sensor.center = new Vector3(0f, 0.7f, 0.6f);
            }
        }

        private void UpdateCountText()
        {
            if (_countText != null)
            {
                _countText.text = _units.Count.ToString();
                _countText.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        // ---------- Сенсор: ворота / бустеры / подарки / толпа врагов ----------
        private void OnTriggerEnter(Collider other)
        {
            var gate = other.GetComponentInParent<Gate>();
            if (gate != null) { gate.ApplyTo(this); return; }

            var gift = other.GetComponentInParent<GiftBox>();
            if (gift != null) { gift.Collect(this); return; }

            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null && !enemy.IsDead && enemy.IsCrowd)
            {
                int loss = enemy.CrowdCount;
                enemy.Die(false);
                AudioController.Instance?.PlayHit();
                RemoveUnits(loss);
            }
        }
    }
}
