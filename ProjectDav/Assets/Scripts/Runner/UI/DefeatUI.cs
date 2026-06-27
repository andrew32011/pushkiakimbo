using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class DefeatUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _continueButton;  // активна, пока есть продолжения

        public void Show(bool v) { if (_root != null) _root.SetActive(v); }

        public void Set(int coins, int kills, bool canContinue)
        {
            if (_resultText != null) _resultText.text = $"Награда: {coins}\nУбито: {kills}";
            if (_continueButton != null) _continueButton.SetActive(canContinue);
        }

        public void OnContinue() => RunnerGameManager.Instance?.ContinueRun();
        public void OnRetry() => RunnerGameManager.Instance?.RestartLevel();
        public void OnMenu() => RunnerGameManager.Instance?.ShowMenu();
    }
}
