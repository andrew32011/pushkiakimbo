using UnityEngine;

namespace CrowdRunner
{
    // Враг: либо толпа (count), либо босс (HP-бар). Всегда наводится на отряд по X.
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Transform _hpBarFill;   // для босса: масштаб по доле HP

        private LevelSpawner _spawner;
        private bool _isBoss;
        private float _speed;
        private float _homingSpeed = 6f;
        private int _level;

        // толпа
        private float _hp, _maxHp, _hpPerUnit = 4f;
        // босс
        private int _bonusUnits;
        private int _contactDamage;
        private float _hitInterval = 0.5f;
        private float _hitTimer;
        private bool _engaged;   // босс вступил в бой — отряд встал

        public bool IsDead { get; private set; }
        public bool IsBoss => _isBoss;
        public bool IsCrowd => !_isBoss;
        public int Level => _level;
        public int CrowdCount => Mathf.Max(1, Mathf.CeilToInt(_hp / _hpPerUnit));

        public void InitCrowd(LevelSpawner spawner, int count, int level, float speed, float hpPerUnit, Color tint)
        {
            _spawner = spawner; _isBoss = false; _level = level; _speed = speed;
            _hpPerUnit = Mathf.Max(1f, hpPerUnit);
            _maxHp = _hp = count * _hpPerUnit;
            Tint(tint);
            if (_hpBarFill != null) _hpBarFill.gameObject.SetActive(false);
            UpdateLabel();
        }

        public void InitBoss(LevelSpawner spawner, float hp, int level, float speed, int bonusUnits, int contactDamage, float hitInterval, Color tint)
        {
            _spawner = spawner; _isBoss = true; _level = level; _speed = speed;
            _maxHp = _hp = hp; _bonusUnits = bonusUnits; _contactDamage = contactDamage; _hitInterval = hitInterval;
            Tint(tint);
            UpdateLabel();
            UpdateHpBar();
        }

        private void Tint(Color c)
        {
            if (_renderers != null)
                foreach (var r in _renderers)
                    if (r != null && r.material != null) r.material.color = c;
        }

        private void Update()
        {
            if (IsDead) return;
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;

            // движение навстречу + наведение на отряд по X (не проходят мимо)
            Vector3 pos = transform.position;
            pos.z -= _speed * Time.deltaTime;
            var squad = gm.Squad;
            if (squad != null)
                pos.x = Mathf.MoveTowards(pos.x, squad.Center.x, _homingSpeed * Time.deltaTime);
            transform.position = pos;

            // босс бьёт отряд вблизи периодически
            if (_isBoss && squad != null)
            {
                float dz = transform.position.z - squad.Center.z;
                if (dz < 1.8f)
                {
                    if (!_engaged) { _engaged = true; squad.StopRunning(); } // стоп: бой с боссом
                    _hitTimer -= Time.deltaTime;
                    if (_hitTimer <= 0f)
                    {
                        _hitTimer = _hitInterval;
                        squad.RemoveUnits(_contactDamage);
                    }
                    // босс не проходит сквозь отряд
                    if (transform.position.z < squad.Center.z + 1.2f)
                    {
                        var p = transform.position; p.z = squad.Center.z + 1.2f; transform.position = p;
                    }
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;
            _hp -= dmg;
            if (_isBoss)
            {
                EffectsManager.Float(transform.position + Vector3.up * 2.6f, Mathf.CeilToInt(dmg).ToString(), new Color(1f, 0.9f, 0.3f));
                UpdateHpBar();
            }
            if (_hp <= 0f) { Die(true); return; }
            UpdateLabel();
        }

        public void Die(bool reward)
        {
            if (IsDead) return;
            IsDead = true;
            if (_isBoss && _engaged)
            {
                var sq = RunnerGameManager.Instance?.Squad;
                if (sq != null) sq.ResumeRunning(); // бой окончен — отряд бежит дальше
            }
            if (reward)
            {
                RunnerGameManager.Instance?.ReportKill(_isBoss ? _level * 5 : _level);
                AudioController.Instance?.PlayEnemyDie();
                EffectsManager.Burst(transform.position + Vector3.up * 1f, _isBoss ? new Color(0.7f, 0.2f, 0.9f) : new Color(1f, 0.3f, 0.3f), _isBoss ? 2.5f : 1.2f);
                if (_isBoss && _bonusUnits > 0)
                {
                    var squad = RunnerGameManager.Instance?.Squad;
                    if (squad != null) squad.ModifyUnits(GateOp.Add, _bonusUnits);
                }
            }
            _spawner?.NotifyEnemyRemoved(this);
            StartCoroutine(DeathAnim());
        }

        private System.Collections.IEnumerator DeathAnim()
        {
            float t = 0f; Vector3 s0 = transform.localScale;
            while (t < 0.2f) { t += Time.deltaTime; transform.localScale = Vector3.Lerp(s0, Vector3.zero, t / 0.2f); yield return null; }
            Destroy(gameObject);
        }

        private void UpdateLabel()
        {
            if (_label == null) return;
            _label.text = _isBoss ? Mathf.CeilToInt(Mathf.Max(0f, _hp)).ToString() : CrowdCount.ToString();
            _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void UpdateHpBar()
        {
            if (_hpBarFill == null) return;
            float frac = _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 0f;
            var s = _hpBarFill.localScale; s.x = frac; _hpBarFill.localScale = s;
        }
    }
}
