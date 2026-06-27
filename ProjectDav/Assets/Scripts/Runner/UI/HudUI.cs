using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class HudUI : UIPanel
    {
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _unitsText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _weaponText;
        [SerializeField] private Slider _progress;

        private void Update()
        {
            var gm = GM;
            if (_progress != null && gm != null && gm.Phase == GamePhase.Running)
                _progress.value = gm.LevelProgress;
        }

        public override void Refresh()
        {
            var gm = GM;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            if (_levelText != null) _levelText.text = "Ур. " + gm.Level;
            if (_unitsText != null && gm.Squad != null) _unitsText.text = gm.Squad.UnitCount.ToString();
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

        // Доступ к магазину/настройкам прямо во время боя (игра ставится на паузу).
        public void OnShop() => GM?.OpenUpgrades();
        public void OnSettings() => GM?.OpenSettings();
    }
}
