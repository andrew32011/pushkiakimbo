using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _reviveButton;   // активна, пока есть воскрешения
        [SerializeField] private GameObject _resultBlock;    // блок с наградой/удвоением
        [SerializeField] private Text _reviveCounter;

        public void Show(bool v) { if (_root != null) _root.SetActive(v); }

        // Режим до фиксации результата: можно воскреснуть.
        public void SetCanRevive(bool canRevive, int used, int max)
        {
            if (_title != null) _title.text = canRevive ? "Отряд разбит!" : "Забег окончен";
            if (_reviveButton != null) _reviveButton.SetActive(canRevive);
            if (_resultBlock != null) _resultBlock.SetActive(!canRevive);
            if (_reviveCounter != null) _reviveCounter.text = $"Воскрешение {used}/{max}";
        }

        public void SetResult(int coins, int kills)
        {
            if (_resultText != null) _resultText.text = $"Награда: {coins}\nУбито: {kills}";
        }

        // ---- Кнопки ----
        public void OnRevive() => RunnerGameManager.Instance?.Revive();
        public void OnDecline() => RunnerGameManager.Instance?.FinishLose();
        public void OnDouble() => RunnerGameManager.Instance?.DoubleReward();
        public void OnMenu() => RunnerGameManager.Instance?.ShowMenu();
    }
}
