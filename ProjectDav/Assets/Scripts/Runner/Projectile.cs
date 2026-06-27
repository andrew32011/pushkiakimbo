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

        public void Launch(float damage, float speed, float life, bool pierce)
        {
            _damage = damage;
            _speed = speed;
            _life = life;
            _pierce = pierce;
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
            if (enemy == null || enemy.IsDead) return;
            enemy.TakeDamage(_damage);
            if (!_pierce) Destroy(gameObject);
        }
    }
}
