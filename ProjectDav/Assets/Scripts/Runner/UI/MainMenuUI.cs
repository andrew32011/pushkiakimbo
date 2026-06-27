using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class MainMenuUI : UIPanel
    {
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _levelText;

        public override void Refresh()
        {
            var gm = GM;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            if (_levelText != null) _levelText.text = "Уровень " + gm.Level;
        }

        public void OnPlay() => GM?.StartRun();
        public void OnShop() => GM?.OpenUpgrades();
        public void OnSettings() => GM?.OpenSettings();
    }
}
