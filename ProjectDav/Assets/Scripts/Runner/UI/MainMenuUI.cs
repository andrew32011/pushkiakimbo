using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _crystalsText;
        [SerializeField] private Text _levelText;

        private void OnEnable()
        {
            var gm = RunnerGameManager.Instance;
            if (gm != null) gm.OnEconomyChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            var gm = RunnerGameManager.Instance;
            if (gm != null) gm.OnEconomyChanged -= Refresh;
        }

        public void Show(bool v) { if (_root != null) _root.SetActive(v); if (v) Refresh(); }

        public void Refresh()
        {
            var gm = RunnerGameManager.Instance;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            if (_crystalsText != null) _crystalsText.text = gm.Crystals.ToString();
            if (_levelText != null) _levelText.text = "Уровень " + gm.Level;
        }

        public void OnPlay() => RunnerGameManager.Instance?.StartRun();
        public void OnShop() => RunnerGameManager.Instance?.OpenUpgrades();
        public void OnSettings() => RunnerGameManager.Instance?.OpenSettings();
    }
}
