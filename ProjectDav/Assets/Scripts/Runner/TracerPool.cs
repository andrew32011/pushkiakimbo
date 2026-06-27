using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Пул трассеров для hitscan-стрельбы: короткая линия дуло→цель с затуханием.
    // Переиспользует LineRenderer'ы — без Instantiate/Destroy на каждый выстрел.
    public class TracerPool : MonoBehaviour
    {
        public static TracerPool Instance { get; private set; }

        [SerializeField] private int _prewarm = 96;

        private Material _mat;
        private readonly Stack<LineRenderer> _pool = new Stack<LineRenderer>();
        private readonly List<Active> _active = new List<Active>();

        private struct Active { public LineRenderer lr; public float t; public float life; public Color color; }

        private void Awake()
        {
            Instance = this;
            _mat = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < _prewarm; i++) _pool.Push(Create());
        }

        private LineRenderer Create()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _mat;
            lr.positionCount = 2;
            lr.numCapVertices = 0;
            lr.textureMode = LineTextureMode.Stretch;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            return lr;
        }

        public void Spawn(Vector3 a, Vector3 b, Color color, float width, float life)
        {
            var lr = _pool.Count > 0 ? _pool.Pop() : Create();
            lr.startWidth = lr.endWidth = width;
            lr.startColor = lr.endColor = color;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.enabled = true;
            _active.Add(new Active { lr = lr, t = 0f, life = Mathf.Max(0.01f, life), color = color });
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var a = _active[i];
                a.t += Time.deltaTime;
                if (a.t >= a.life)
                {
                    a.lr.enabled = false;
                    _pool.Push(a.lr);
                    _active.RemoveAt(i);
                    continue;
                }
                var c = a.color;
                c.a = 1f - a.t / a.life; // затухание
                a.lr.startColor = a.lr.endColor = c;
                _active[i] = a;
            }
        }
    }
}
