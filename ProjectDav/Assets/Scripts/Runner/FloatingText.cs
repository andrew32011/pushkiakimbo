using UnityEngine;

namespace CrowdRunner
{
    // Всплывающее 3D-число (урон / +N / −N). Поднимается и растворяется.
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMesh _text;
        [SerializeField] private float _life = 0.8f;
        [SerializeField] private float _rise = 2.2f;

        private float _t;
        private Color _color;

        public void Show(string s, Color color)
        {
            if (_text == null) _text = GetComponentInChildren<TextMesh>();
            if (_text != null) { _text.text = s; _text.color = color; }
            _color = color;
            _t = 0f;
            // всегда лицом к камере (камера со стороны -Z)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * (_rise * Time.deltaTime);
            float k = 1f - Mathf.Clamp01(_t / _life);
            if (_text != null)
            {
                var c = _color; c.a = k; _text.color = c;
            }
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
