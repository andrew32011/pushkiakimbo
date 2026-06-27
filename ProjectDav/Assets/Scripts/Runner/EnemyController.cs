using UnityEngine;

namespace CrowdRunner
{
    // Враг-монстр. Идёт навстречу отряду, имеет HP. При касании отряда отнимает юнитов.
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;       // число HP над головой
        [SerializeField] private Renderer[] _renderers; // для тонировки/боссов

        private float _maxHp;
        private float _hp;
        private int _level;
        private int _strength;   // сколько юнитов отнимает при полном HP
        private float _speed;
        private bool _isBoss;

        public bool IsDead { get; private set; }
        public int Level => _level;
        public bool IsBoss => _isBoss;

        private LevelSpawner _spawner;

        public void Init(LevelSpawner spawner, float hp, int level, int strength, float speed, bool isBoss, Color tint)
        {
            _spawner = spawner;
            _maxHp = _hp = hp;
            _level = level;
            _strength = Mathf.Max(1, strength);
            _speed = speed;
            _isBoss = isBoss;
            IsDead = false;

            if (_renderers != null)
                foreach (var r in _renderers)
                    if (r != null && r.material != null) r.material.color = tint;

            UpdateLabel();
        }

        private void Update()
        {
            if (IsDead) return;
            if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.Phase != GamePhase.Running) return;
            // Движение навстречу отряду (-Z).
            transform.position += Vector3.back * (_speed * Time.deltaTime);
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;
            _hp -= dmg;
            if (_hp <= 0f) { Die(true); return; }
            UpdateLabel();
        }

        // Урон отряду при столкновении: доля силы пропорционально остатку HP.
        public int UnitsOnContact()
        {
            float frac = _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 1f;
            return Mathf.Max(1, Mathf.CeilToInt(_strength * frac));
        }

        public void Die(bool reward)
        {
            if (IsDead) return;
            IsDead = true;
            if (reward)
            {
                // Очки забега копятся для итоговой формулы монет (диздок 5.1), без сейва на каждом килле.
                RunnerGameManager.Instance?.ReportKill(_level);
                AudioController.Instance?.PlayEnemyDie();
            }
            _spawner?.NotifyEnemyRemoved(this);
            // простая «анимация смерти» — схлопывание
            StartCoroutine(DeathAnim());
        }

        private System.Collections.IEnumerator DeathAnim()
        {
            float t = 0f;
            Vector3 s0 = transform.localScale;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(s0, Vector3.zero, t / 0.18f);
                yield return null;
            }
            Destroy(gameObject);
        }

        private void UpdateLabel()
        {
            if (_label != null) _label.text = Mathf.CeilToInt(Mathf.Max(0f, _hp)).ToString();
        }
    }
}
