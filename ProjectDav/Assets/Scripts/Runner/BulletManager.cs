using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrowdRunner
{
    // Бесфизичный менеджер пуль: видимые летящие пули как пул простых мешей (без Rigidbody/триггеров).
    // Один Update двигает все пули и проверяет попадание коротким рейкастом на длину шага.
    // Тысячи пуль обходятся дёшево — нет Instantiate/Destroy, нет симуляции тел, нет GC.
    public class BulletManager : MonoBehaviour
    {
        public static BulletManager Instance { get; private set; }

        [SerializeField] private int _prewarm = 256;

        private Material _mat;
        private MaterialPropertyBlock _mpb;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private readonly Stack<Renderer> _pool = new Stack<Renderer>();
        private readonly List<Bullet> _active = new List<Bullet>();
        private static readonly RaycastHit[] _hits = new RaycastHit[8];

        private struct Bullet { public Transform tr; public Renderer rend; public float speed; public float dmg; public float life; }

        private void Awake()
        {
            Instance = this;
            _mat = new Material(Shader.Find("Standard"));
            _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < _prewarm; i++) _pool.Push(Create());
        }

        private Renderer Create()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bullet";
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col); // без физики
            go.transform.SetParent(transform);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = _mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            return r;
        }

        public void Spawn(Vector3 pos, float speed, float dmg, float life, Color color, float scale)
        {
            var r = _pool.Count > 0 ? _pool.Pop() : Create();
            var tr = r.transform;
            tr.position = pos;
            tr.localScale = Vector3.one * scale;
            _mpb.SetColor(ColorId, color);
            r.SetPropertyBlock(_mpb);
            r.gameObject.SetActive(true);
            _active.Add(new Bullet { tr = tr, rend = r, speed = speed, dmg = dmg, life = Mathf.Max(0.05f, life) });
        }

        private void Recycle(Bullet b)
        {
            b.rend.gameObject.SetActive(false);
            _pool.Push(b.rend);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var b = _active[i];
                float step = b.speed * dt;
                Vector3 p = b.tr.position;

                // попадание на отрезке шага (ближайшая цель, без пробивания)
                int n = Physics.RaycastNonAlloc(p, Vector3.forward, _hits, step, ~0, QueryTriggerInteraction.Collide);
                int best = -1; float bestDist = float.MaxValue;
                for (int h = 0; h < n; h++)
                {
                    var col = _hits[h].collider;
                    if (col == null) continue;
                    if (col.GetComponentInParent<SquadController>() != null) continue; // не по себе
                    if (_hits[h].distance < bestDist) { bestDist = _hits[h].distance; best = h; }
                }

                if (best >= 0)
                {
                    var hit = _hits[best];
                    if (hit.collider.GetComponentInParent<LaneWall>() == null) // об стену — только гасим
                    {
                        var enemy = hit.collider.GetComponentInParent<EnemyController>();
                        if (enemy != null && !enemy.IsDead) enemy.TakeDamage(b.dmg);
                        else
                        {
                            var booster = hit.collider.GetComponentInParent<Booster>();
                            if (booster != null && !booster.IsDead) booster.TakeDamage(b.dmg);
                        }
                    }
                    Recycle(b); _active.RemoveAt(i); continue;
                }

                b.life -= dt;
                if (b.life <= 0f) { Recycle(b); _active.RemoveAt(i); continue; }
                b.tr.position = p + Vector3.forward * step;
                _active[i] = b;
            }
        }
    }
}
