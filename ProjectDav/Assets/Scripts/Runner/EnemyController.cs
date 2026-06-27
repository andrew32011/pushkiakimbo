using UnityEngine;

namespace CrowdRunner
{
    // Враг: либо толпа (count), либо босс (HP-бар). Всегда наводится на отряд по X.
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Transform _hpBarFill;   // для босса: масштаб по доле HP
        [SerializeField] private Transform _modelRoot;   // держатель визуальной модели (для подмены из пака)
        [SerializeField] private float _modelHeight = 1.7f;

        private LevelSpawner _spawner;
        private bool _isBoss;
        private float _speed;
        private float _homingSpeed = 6f;
        private int _level;

        // толпа
        private float _hp, _maxHp, _hpPerUnit = 4f;
        // босс
        private int _contactDamage;
        private float _hitInterval = 0.5f;
        private float _hitTimer;
        private bool _engaged;   // вступил в бой
        private float _stopDist = 1.5f; // на каком расстоянии перед отрядом встаёт
        private float _homingDist = 999f; // с какой дистанции начинает наводиться на отряд по X

        public bool IsDead { get; private set; }
        public bool IsBoss => _isBoss;
        public bool IsCrowd => !_isBoss;
        public int Level => _level;
        public int CrowdCount => Mathf.Max(1, Mathf.CeilToInt(_hp / _hpPerUnit));

        public void InitCrowd(LevelSpawner spawner, int count, int level, float speed, float hpPerUnit, int contactDamage, float hitInterval, float stopDist, float homingDist, GameObject model, Color tint)
        {
            _spawner = spawner; _isBoss = false; _level = level; _speed = speed;
            _hpPerUnit = Mathf.Max(1f, hpPerUnit);
            _maxHp = _hp = count * _hpPerUnit;
            _contactDamage = Mathf.Max(1, contactDamage);
            _hitInterval = Mathf.Max(0.2f, hitInterval);
            _stopDist = stopDist;
            _homingDist = homingDist;
            SetModel(model, tint);
            if (_hpBarFill != null) _hpBarFill.gameObject.SetActive(false);
            if (_label != null) _label.gameObject.SetActive(count > 1); // у орды из одиночек цифру не показываем
            UpdateLabel();
        }

        public void InitBoss(LevelSpawner spawner, float hp, int level, float speed, int bonusUnits, int contactDamage, float hitInterval, float homingDist, GameObject model, Color tint)
        {
            _spawner = spawner; _isBoss = true; _level = level; _speed = speed;
            _maxHp = _hp = hp; _contactDamage = contactDamage; _hitInterval = hitInterval; // bonusUnits больше не используется
            _stopDist = 1.2f; _homingDist = homingDist;
            SetModel(model, tint);
            UpdateLabel();
            UpdateHpBar();
        }

        private void Tint(Color c)
        {
            if (_renderers != null)
                foreach (var r in _renderers)
                    if (r != null && r.material != null) r.material.color = c;
        }

        // Модель из пака (если задана) подменяет запечённую; иначе оставляем запечённую и тонируем.
        private void SetModel(GameObject modelPrefab, Color tint)
        {
            if (modelPrefab == null || _modelRoot == null) { Tint(tint); return; }
            for (int i = _modelRoot.childCount - 1; i >= 0; i--) Destroy(_modelRoot.GetChild(i).gameObject);
            ModelUtil.Wrap(modelPrefab, _modelRoot, _modelHeight, tint, out var rends);
            _renderers = rends;
        }

        private void Update()
        {
            if (IsDead) return;
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;

            var squad = gm.Squad;
            Vector3 pos = transform.position;

            // движемся навстречу, но НЕ проходим мимо игрока — встаём на _stopDist перед ним
            float stopZ = squad != null ? squad.Center.z + _stopDist : float.NegativeInfinity;
            pos.z = Mathf.Max(pos.z - _speed * Time.deltaTime, stopZ);
            // наведение по X включаем только вблизи (на линии блоков), и пока не в бою —
            // чтобы орда не схлопывалась в точку издалека, а шла прямо по полосе
            if (squad != null && !_engaged && (pos.z - squad.Center.z) <= _homingDist)
                pos.x = Mathf.MoveTowards(pos.x, squad.Center.x, _homingSpeed * Time.deltaTime);
            transform.position = pos;

            if (squad == null) return;

            // вблизи отряда: встаём в бой и периодически отнимаем юнитов
            bool near = (transform.position.z - squad.Center.z) <= _stopDist + 0.35f;
            if (near)
            {
                if (!_engaged)
                {
                    _engaged = true;
                    if (_isBoss) squad.StopRunning(); // на боссе отряд останавливается
                }
                _hitTimer -= Time.deltaTime;
                if (_hitTimer <= 0f)
                {
                    _hitTimer = _hitInterval;
                    squad.RemoveUnits(_contactDamage);
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
                // доп. юниты за босса убраны — босс теперь чистая угроза без награды отрядом
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
