using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class DefeatUI : UIPanel
    {
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _continueButton;  // активна, пока есть продолжения

        public void Set(int coins, int kills, bool canContinue)
        {
            if (_resultText != null) _resultText.text = $"Награда: {coins}\nУбито: {kills}";
            if (_continueButton != null) _continueButton.SetActive(canContinue);
        }

        public void OnContinue() => GM?.ContinueRun();
        public void OnRetry() => GM?.RestartLevel();
        public void OnMenu() => GM?.ShowMenu();
    }
}
