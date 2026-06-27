using UnityEngine;
using UnityEngine.UI;
using YG;

namespace CrowdRunner
{
    public class SettingsUI : UIPanel
    {
        [SerializeField] private Slider _music;
        [SerializeField] private Slider _sfx;

        protected override void Awake()
        {
            base.Awake();
            // Слайдеры подписываем в рантайме (персистентные float-листенеры из редактора неудобны).
            if (_music != null) _music.onValueChanged.AddListener(_ => OnVolumeChanged());
            if (_sfx != null) _sfx.onValueChanged.AddListener(_ => OnVolumeChanged());
        }

        public override void Show(bool visible)
        {
            base.Show(visible);
            if (visible)
            {
                if (_music != null) _music.SetValueWithoutNotify(YG2.saves.musicVolume);
                if (_sfx != null) _sfx.SetValueWithoutNotify(YG2.saves.sfxVolume);
            }
        }

        public void OnVolumeChanged()
        {
            float m = _music != null ? _music.value : 1f;
            float s = _sfx != null ? _sfx.value : 1f;
            GM?.SetVolumes(m, s);
        }

        public void OnFullscreen()
        {
#if Fullscreen_yg
            YG2.SetFullscreen(!YG2.isFullscreen);
#endif
        }
        // OnClose/OnMenu — из базового UIPanel.
    }
}
