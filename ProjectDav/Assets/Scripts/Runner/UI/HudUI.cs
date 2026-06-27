using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _unitsText;
        [SerializeField] private Text _dmgText;
        [SerializeField] private Text _weaponText;
        [SerializeField] private Slider _progress;

        public void Show(bool v) { if (_root != null) _root.SetActive(v); if (v) Refresh(); }

        private void Update()
        {
            // прогресс плавно обновляем каждый кадр во время забега
            var gm = RunnerGameManager.Instance;
            if (_progress != null && gm != null && gm.Phase == GamePhase.Running)
                _progress.value = gm.LevelProgress;
        }

        public void Refresh()
        {
            var gm = RunnerGameManager.Instance;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            var squad = gm.Squad;
            if (squad != null)
            {
                if (_unitsText != null) _unitsText.text = squad.UnitCount.ToString();
                if (_dmgText != null) _dmgText.text = "DMG " + Mathf.RoundToInt(squad.Damage);
                if (_weaponText != null) _weaponText.text = squad.Weapon.ToString();
            }
        }
    }
}
