using UnityEngine;

namespace CrowdRunner
{
    // Простой хаб звуков. Клипы назначаются билдером (пока могут быть пустыми).
    public class AudioController : MonoBehaviour
    {
        public static AudioController Instance { get; private set; }

        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _shot;
        [SerializeField] private AudioClip _hit;
        [SerializeField] private AudioClip _gate;
        [SerializeField] private AudioClip _enemyDie;
        [SerializeField] private AudioClip _lose;
        [SerializeField] private AudioClip _win;

        private float _shotCooldown;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        private void Update()
        {
            if (_shotCooldown > 0f) _shotCooldown -= Time.deltaTime;
        }

        public void SetSfxVolume(float v) { if (_sfxSource != null) _sfxSource.volume = Mathf.Clamp01(v); }

        private void Play(AudioClip c)
        {
            if (c != null && _sfxSource != null) _sfxSource.PlayOneShot(c);
        }

        public void PlayShot()
        {
            // троттлинг, чтобы залп из десятков юнитов не рвал уши
            if (_shotCooldown > 0f) return;
            _shotCooldown = 0.05f;
            Play(_shot);
        }
        public void PlayHit() => Play(_hit);
        public void PlayGate() => Play(_gate);
        public void PlayEnemyDie() => Play(_enemyDie);
        public void PlayLose() => Play(_lose);
        public void PlayWin() => Play(_win);
    }
}
