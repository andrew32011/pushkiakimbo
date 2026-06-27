using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class MainMenuUI : UIPanel
    {
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _crystalsText;
        [SerializeField] private Text _levelText;

        public override void Refresh()
        {
            var gm = GM;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            if (_crystalsText != null) _crystalsText.text = gm.Crystals.ToString();
            if (_levelText != null) _levelText.text = "Уровень " + gm.Level;
        }

        public void OnPlay() => GM?.StartRun();
        public void OnUpgrades() => GM?.OpenUpgrades();
        public void OnShop() => GM?.OpenShop();
        public void OnCases() => GM?.OpenCases();
        public void OnLevelSelect() => GM?.OpenLevelSelect();
        public void OnSettings() => GM?.OpenSettings();
    }
}
