using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    // Выбор эпохи: открытые эпохи запускают забег, закрытые показывают попап с условием.
    public class LevelSelectUI : UIPanel
    {
        [SerializeField] private GameObject[] _lockIcons = new GameObject[4]; // замок на закрытых
        [SerializeField] private GameObject _lockPopup;
        [SerializeField] private Text _lockPopupText;

        public override void Show(bool visible)
        {
            base.Show(visible);
            if (visible && _lockPopup != null) _lockPopup.SetActive(false);
        }

        public override void Refresh()
        {
            var gm = GM;
            if (gm == null || _lockIcons == null) return;
            for (int i = 0; i < _lockIcons.Length; i++)
                if (_lockIcons[i] != null) _lockIcons[i].SetActive(!gm.IsEpochUnlocked(i));
        }

        private void TrySelect(int epoch)
        {
            var gm = GM;
            if (gm == null) return;
            if (gm.IsEpochUnlocked(epoch))
            {
                if (_lockPopup != null) _lockPopup.SetActive(false);
                Show(false);
                gm.SelectEpoch(epoch);
            }
            else
            {
                if (_lockPopupText != null)
                    _lockPopupText.text = $"Эпоха {epoch + 1} закрыта.\nДойдите до уровня {gm.EpochStartLevel(epoch)},\nчтобы открыть её.";
                if (_lockPopup != null) _lockPopup.SetActive(true);
            }
        }

        public void OnEpoch1() => TrySelect(0);
        public void OnEpoch2() => TrySelect(1);
        public void OnEpoch3() => TrySelect(2);
        public void OnEpoch4() => TrySelect(3);
        public void OnLockOk() { if (_lockPopup != null) _lockPopup.SetActive(false); }
    }
}
