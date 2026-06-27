using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class DefeatUI : UIPanel
    {
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _continueButton;  // активна, пока есть воскрешения
        [SerializeField] private GameObject _doubleButton;    // показывается, когда воскрешения кончились

        public void Set(int coins, int kills, bool canContinue)
        {
            if (_resultText != null) _resultText.text = $"Награда: {coins}\nУбито: {kills}";
            if (_continueButton != null) _continueButton.SetActive(canContinue);
            if (_doubleButton != null) _doubleButton.SetActive(!canContinue);
        }

        public void SetDoubled(int coins)
        {
            if (_resultText != null) _resultText.text = $"Награда: {coins}";
            if (_doubleButton != null) _doubleButton.SetActive(false);
        }

        public void OnContinue() => GM?.ContinueRun();
        public void OnDouble() => GM?.DoubleDefeatReward();
        public void OnRetry() => GM?.RestartLevel();
        public void OnMenu() => GM?.ShowMenu();
    }
}
