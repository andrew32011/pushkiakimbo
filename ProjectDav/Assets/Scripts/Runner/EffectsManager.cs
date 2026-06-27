using UnityEngine;

namespace CrowdRunner
{
    // Лёгкий хаб эффектов: партикл-вспышки и всплывающий текст.
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }

        [SerializeField] private ParticleSystem _burstPrefab;
        [SerializeField] private FloatingText _floatingPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public static void Burst(Vector3 pos, Color color, float scale = 1f)
        {
            if (Instance == null || Instance._burstPrefab == null) return;
            var ps = Instantiate(Instance._burstPrefab, pos, Quaternion.identity);
            ps.transform.localScale = Vector3.one * scale;
            var main = ps.main;
            main.startColor = color;
            ps.Play();
            Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.2f);
        }

        public static void Float(Vector3 pos, string text, Color color)
        {
            if (Instance == null || Instance._floatingPrefab == null) return;
            var ft = Instantiate(Instance._floatingPrefab, pos, Quaternion.identity);
            ft.Show(text, color);
        }
    }
}
