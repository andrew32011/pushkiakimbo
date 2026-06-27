using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class MainMenuUI : UIPanel
    {
        [SerializeField] private Text _titleText;   // название уровня (вместо названия игры)
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _crystalsText;
        [SerializeField] private Text _levelText;

        public override void Refresh()
        {
            var gm = GM;
            if (gm == null) return;
            if (_titleText != null) _titleText.text = gm.LevelTitle;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            if (_crystalsText != null) _crystalsText.text = gm.Crystals.ToString();
            if (_levelText != null) _levelText.text = "Уровень " + gm.Level;
        }

        public void OnPlay() => GM?.StartRun();
        public void OnUpgrades() => GM?.OpenUpgrades();
        public void OnCases() => GM?.OpenCases();
        public void OnLevelSelect() => GM?.OpenLevelSelect();
        public void OnBonuses() => GM?.OpenBonuses();
        public void OnSkins() => GM?.OpenSkins();
        public void OnAdFree() => GM?.OpenAdFree();
        public void OnSettings() => GM?.OpenSettings();
        public void OnResetProgress() => GM?.ResetProgress(); // временно
    }
}
