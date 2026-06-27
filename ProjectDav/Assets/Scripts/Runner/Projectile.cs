using UnityEngine;

namespace CrowdRunner
{
    // Снаряд отряда. Летит вперёд (+Z), наносит урон врагу/боссу при касании.
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        private float _damage;
        private float _speed;
        private float _life;
        private bool _pierce;
        private Color _color = Color.white;

        public void Launch(float damage, float speed, float life, bool pierce, Color color)
        {
            _damage = damage;
            _speed = speed;
            _life = life;
            _pierce = pierce;
            _color = color;
        }

        private void Update()
        {
            transform.position += Vector3.forward * (_speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(_damage);
                EffectsManager.Burst(transform.position, _color, 0.5f);
                if (!_pierce) Destroy(gameObject);
                return;
            }

            var booster = other.GetComponentInParent<Booster>();
            if (booster != null && !booster.IsDead)
            {
                booster.TakeDamage(_damage);
                EffectsManager.Burst(transform.position, _color, 0.5f);
                if (!_pierce) Destroy(gameObject);
            }
        }
    }
}
