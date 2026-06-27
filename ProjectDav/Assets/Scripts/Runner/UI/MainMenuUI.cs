using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _crystalsText;
        [SerializeField] private Text _epochText;
        [SerializeField] private Button _levelsPrev;
        [SerializeField] private Button _levelsNext;

        private static readonly string[] EpochNames =
            { "Первобытность", "Средневековье", "Пороховая эпоха", "Вторая мировая" };

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
            if (_epochText != null) _epochText.text = EpochNames[Mathf.Clamp(gm.CurrentEpoch, 0, 3)];
        }

        // ---- Кнопки ----
        public void OnPlay() => RunnerGameManager.Instance?.StartRun();
        public void OnUpgrades() => RunnerGameManager.Instance?.OpenUpgrades();
        public void OnSettings() => RunnerGameManager.Instance?.OpenSettings();

        public void OnPrevEpoch()
        {
            var gm = RunnerGameManager.Instance; if (gm == null) return;
            gm.SelectEpoch(gm.CurrentEpoch - 1); Refresh();
        }
        public void OnNextEpoch()
        {
            var gm = RunnerGameManager.Instance; if (gm == null) return;
            gm.SelectEpoch(gm.CurrentEpoch + 1); Refresh();
        }
    }
}
