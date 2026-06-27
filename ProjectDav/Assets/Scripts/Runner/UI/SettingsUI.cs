using UnityEngine;
using UnityEngine.UI;
using YG;

namespace CrowdRunner
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Slider _music;
        [SerializeField] private Slider _sfx;

        private void Awake()
        {
            // Слайдеры подписываем в рантайме (персистентные float-листенеры неудобно
            // ставить из редактора, поэтому вешаем здесь).
            if (_music != null) _music.onValueChanged.AddListener(_ => OnVolumeChanged());
            if (_sfx != null) _sfx.onValueChanged.AddListener(_ => OnVolumeChanged());
        }

        public void Show(bool v)
        {
            if (_root != null) _root.SetActive(v);
            if (v)
            {
                if (_music != null) _music.SetValueWithoutNotify(YG2.saves.musicVolume);
                if (_sfx != null) _sfx.SetValueWithoutNotify(YG2.saves.sfxVolume);
            }
        }

        public void OnVolumeChanged()
        {
            float m = _music != null ? _music.value : 1f;
            float s = _sfx != null ? _sfx.value : 1f;
            RunnerGameManager.Instance?.SetVolumes(m, s);
        }

        public void OnFullscreen()
        {
#if Fullscreen_yg
            YG2.SetFullscreen(!YG2.isFullscreen);
#endif
        }

        public void OnClose() { Show(false); }
    }
}
