using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _weaponText;
        [SerializeField] private Slider _progress;

        public void Show(bool v) { if (_root != null) _root.SetActive(v); if (v) Refresh(); }

        private void Update()
        {
            var gm = RunnerGameManager.Instance;
            if (_progress != null && gm != null && gm.Phase == GamePhase.Running)
                _progress.value = gm.LevelProgress;
        }

        public void Refresh()
        {
            var gm = RunnerGameManager.Instance;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            if (_levelText != null) _levelText.text = "Ур. " + gm.Level;
            if (_weaponText != null && gm.Squad != null) _weaponText.text = WeaponName(gm.Squad.Weapon);
        }

        private static string WeaponName(WeaponType w)
        {
            switch (w)
            {
                case WeaponType.Melee: return "Ручное";
                case WeaponType.Bow: return "Лук";
                case WeaponType.Musket: return "Ружьё";
                case WeaponType.Rifle: return "Винтовка";
            }
            return w.ToString();
        }
    }
}
